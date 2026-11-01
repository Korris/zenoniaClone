using System;
using System.Collections.Generic;

namespace ZenoniaProto.Net
{
    /// <summary>What server-api answered. Status 0 = no answer at all (timeout, connection lost): the request may or may not have been applied.</summary>
    public struct ZoneApiReply
    {
        public int Status;
        public string Code, Message;
        /// <summary>The character's rev after the write; for a trade Rev is side A and RevB side B.</summary>
        public int Rev, RevB;
        /// <summary>From a rev_conflict; -1 when not given.</summary>
        public int CurrentRev;
        /// <summary>Enhance only: Attempted = a stone was spent. Level = the item's level afterwards.</summary>
        public bool Attempted, Success;
        public int Level;

        public bool Ok { get { return Status >= 200 && Status < 300; } }
        /// <summary>Worth trying again as it is: nothing about the request itself was refused. Includes 401 / 403, a wrong service key is a bad config that gets fixed, not a reason to drop a character.</summary>
        public bool Transient { get { return Status == 0 || Status >= 500 || Status == 429 || Status == 401 || Status == 403; } }
        /// <summary>The server may have done it, the answer just never came.</summary>
        public bool OutcomeUnknown { get { return Status == 0 || Status >= 500; } }
    }

    /// <summary>One side of a trade commit: the state the zone holds now and what this side gives.</summary>
    public struct TradeCommitSide
    {
        public string CharacterId, SessionId, StateJson;
        public int Rev, GiveGold;
        public string[] GiveUids;
    }

    /// <summary>The API calls a zone server writes with. Each returns at once and calls `done` later, on the thread that runs the queue (the real one queues the answers for ZoneApiHttp.Pump).</summary>
    public interface IZoneApi
    {
        void Save(string characterId, string sessionId, int rev, string stateJson, Action<ZoneApiReply> done);
        void CommitTrade(string tradeId, TradeCommitSide a, TradeCommitSide b, Action<ZoneApiReply> done);
        void Enhance(string characterId, string sessionId, int rev, string reqId, string itemUid, Action<ZoneApiReply> done);
        void EndSession(string characterId, string sessionId, Action<ZoneApiReply> done);
    }

    /// <summary>One character the zone holds the lock of. Capture is asked for the state at the moment a write leaves, so a write is never older than it has to be.</summary>
    public sealed class SavedCharacter
    {
        public readonly string Id, SessionId;
        public int Rev { get; internal set; }
        public bool Leaving { get; internal set; }
        public object Owner;
        internal readonly Func<string> Capture;
        internal readonly List<CharacterSaveQueue.Job> Jobs = new List<CharacterSaveQueue.Job>();
        internal bool Busy, Uncertain;
        internal long NextPeriodicMs;

        internal SavedCharacter(string id, string sessionId, int rev, Func<string> capture) { Id = id; SessionId = sessionId; Rev = rev; Capture = capture; }

        public string CaptureNow() { return Capture(); }
    }

    /// <summary>
    /// Every write of a zone server to server-api, without ever blocking the game loop. Per character the writes go out one at a time and in order, so a leave save can never overtake the trade
    /// before it, and each carries the rev the last one came back with. What it does with an answer:
    /// no answer or a server error = keep the job and try again with backoff, for as long as it takes (nothing here drops state because the API is down);
    /// rev_conflict after an unknown outcome = our own write did land, adopt its rev and write again, memory is the newer one;
    /// rev_conflict with nothing unknown, session_mismatch, not_found = somebody else owns this character: Lost, and this zone must stop playing it;
    /// any other refusal of a save = Rejected, loudly, because the data itself is wrong and a retry would not help.
    /// A trade waits until it is first in line for both characters, and only a real answer ends it: after a lost answer it is retried under the same id, which the API can never apply twice.
    /// </summary>
    public sealed class CharacterSaveQueue
    {
        public const int MaxInFlight = 16;
        const long BackoffBaseMs = 500, BackoffMaxMs = 30000;

        internal enum JobKind { Save, Trade, Enhance, End }

        internal sealed class Job
        {
            public JobKind Kind;
            public SavedCharacter A, B;
            public string Id, Uid;           // trade id or enhance request id; enhance item
            public string[] AUids, BUids;
            public int AGold, BGold;
            public Action<bool> TradeDone;
            public Action<ZoneApiReply> EnhanceDone;
            public int Attempts;
            public long NextAttemptMs;
            public bool Started;
        }

        readonly IZoneApi api;
        readonly List<SavedCharacter> characters = new List<SavedCharacter>();
        readonly long periodicMs;
        long now;
        int inFlight;
        ulong nextId;
        readonly string idPrefix;

        /// <summary>Somebody else holds this character now (code = why). Its jobs are gone; the state it had is passed along so it can still be kept.</summary>
        public event Action<SavedCharacter, string, string> Lost;
        /// <summary>The API refused this state for good. The job is gone, the character is not; the state and the reason are passed along so it can be kept.</summary>
        public event Action<SavedCharacter, string, string, string> Rejected;
        /// <summary>A left character is fully written and its lock released.</summary>
        public event Action<SavedCharacter> Finished;
        /// <summary>Counters, for the server stats (name, amount).</summary>
        public event Action<string, int> Count;

        /// <summary>False from the first call that got no answer until the next answer. A trade is not even started while this is false: it would hang until the API returns.</summary>
        public bool ApiHealthy { get; private set; } = true;
        public int Characters { get { return characters.Count; } }
        public IReadOnlyList<SavedCharacter> All { get { return characters; } }

        public CharacterSaveQueue(IZoneApi zoneApi, long periodicSaveMs, string uniquePrefix)
        {
            api = zoneApi; periodicMs = periodicSaveMs;
            idPrefix = string.IsNullOrEmpty(uniquePrefix) ? Guid.NewGuid().ToString("N") : uniquePrefix;
        }

        void Add(string name, int n = 1) { if (Count != null) Count(name, n); }

        string NewId(string kind) { return kind + "-" + idPrefix + "-" + (++nextId); }

        // ---- what the zone asks for ----

        public SavedCharacter Register(string id, string sessionId, int rev, Func<string> capture)
        {
            var c = new SavedCharacter(id, sessionId, rev, capture) { NextPeriodicMs = now + periodicMs };
            characters.Add(c);
            return c;
        }

        /// <summary>A write soon. Several requests before it leaves are one write; it captures the state when it leaves.</summary>
        public void RequestSave(SavedCharacter c)
        {
            if (c.Leaving || !characters.Contains(c)) return;
            AddSave(c);
        }

        void AddSave(SavedCharacter c)
        {
            if (c.Jobs.Count > 0)
            {
                Job last = c.Jobs[c.Jobs.Count - 1];
                if (last.Kind == JobKind.Save && !last.Started) return;
            }
            c.Jobs.Add(new Job { Kind = JobKind.Save, A = c });
        }

        /// <summary>The character left the world: one last write of everything, then the lock goes. Whatever is ahead in its line (a trade) goes first.</summary>
        public void Leave(SavedCharacter c)
        {
            if (c.Leaving || !characters.Contains(c)) return;
            c.Leaving = true;
            AddSave(c);
            c.Jobs.Add(new Job { Kind = JobKind.End, A = c });
        }

        /// <summary>Both characters change together at the API, or neither. `done` says which, and is only called on a definite answer. The goods must not leave the holders until then.</summary>
        public void CommitTrade(SavedCharacter a, SavedCharacter b, string[] aUids, int aGold, string[] bUids, int bGold, Action<bool> done)
        {
            var job = new Job { Kind = JobKind.Trade, A = a, B = b, Id = NewId("trade"), AUids = aUids, BUids = bUids, AGold = aGold, BGold = bGold, TradeDone = done };
            a.Jobs.Add(job);
            b.Jobs.Add(job);
        }

        /// <summary>One enhance attempt, rolled by the API: first the state is written, because the API works on what it has stored. `done` gets the definite answer.</summary>
        public void Enhance(SavedCharacter c, string itemUid, Action<ZoneApiReply> done)
        {
            AddSave(c);
            c.Jobs.Add(new Job { Kind = JobKind.Enhance, A = c, Id = NewId("enh"), Uid = itemUid, EnhanceDone = done });
        }

        // ---- the loop ----

        /// <summary>Once per tick: starts what can start. Cheap when there is nothing to do.</summary>
        public void Tick(long nowMs)
        {
            now = nowMs;
            for (int i = 0; i < characters.Count && inFlight < MaxInFlight; i++)
            {
                SavedCharacter c = characters[i];
                if (!c.Leaving && c.Jobs.Count == 0 && now >= c.NextPeriodicMs) AddSave(c);
                if (c.Busy || c.Jobs.Count == 0) continue;
                Job job = c.Jobs[0];
                if (now < job.NextAttemptMs) continue;
                if (job.Kind == JobKind.Trade && !(job.A.Jobs[0] == job && job.B.Jobs[0] == job && !job.A.Busy && !job.B.Busy)) continue; // first in line on both sides
                Send(job);
                if (i >= characters.Count) break;
            }
        }

        void Send(Job job)
        {
            job.Started = true;
            job.A.Busy = true;
            if (job.B != null) job.B.Busy = true;
            inFlight++;
            Action<ZoneApiReply> done = r => OnReply(job, r);
            SavedCharacter c = job.A;
            switch (job.Kind)
            {
                case JobKind.Save:
                    c.NextPeriodicMs = now + periodicMs;
                    api.Save(c.Id, c.SessionId, c.Rev, c.Capture(), done);
                    break;
                case JobKind.Enhance: api.Enhance(c.Id, c.SessionId, c.Rev, job.Id, job.Uid, done); break;
                case JobKind.End: api.EndSession(c.Id, c.SessionId, done); break;
                case JobKind.Trade:
                    api.CommitTrade(job.Id,
                        new TradeCommitSide { CharacterId = job.A.Id, SessionId = job.A.SessionId, Rev = job.A.Rev, StateJson = job.A.Capture(), GiveUids = job.AUids, GiveGold = job.AGold },
                        new TradeCommitSide { CharacterId = job.B.Id, SessionId = job.B.SessionId, Rev = job.B.Rev, StateJson = job.B.Capture(), GiveUids = job.BUids, GiveGold = job.BGold }, done);
                    break;
            }
        }

        void OnReply(Job job, ZoneApiReply r)
        {
            inFlight--;
            job.A.Busy = false;
            if (job.B != null) job.B.Busy = false;
            if (r.Transient) { Retry(job, r); return; }
            ApiHealthy = true;
            switch (job.Kind)
            {
                case JobKind.Save: OnSaveAnswer(job, r); break;
                case JobKind.Trade: OnTradeAnswer(job, r); break;
                case JobKind.Enhance: OnEnhanceAnswer(job, r); break;
                case JobKind.End: Finish(job); break;
            }
        }

        void Retry(Job job, ZoneApiReply r)
        {
            if (r.Status == 0 || r.Status >= 502) ApiHealthy = false; // the API itself is away; a 500 or a 429 on one character says nothing about the others
            if (r.OutcomeUnknown) { job.A.Uncertain = true; if (job.B != null) job.B.Uncertain = true; }
            job.Attempts++;
            job.NextAttemptMs = now + Math.Min(BackoffMaxMs, BackoffBaseMs << Math.Min(job.Attempts - 1, 10));
            Add("apiRetries");
        }

        void Pop(Job job)
        {
            job.A.Jobs.Remove(job);
            if (job.B != null) job.B.Jobs.Remove(job);
        }

        void OnSaveAnswer(Job job, ZoneApiReply r)
        {
            SavedCharacter c = job.A;
            if (r.Ok)
            {
                c.Rev = r.Rev; c.Uncertain = false;
                Pop(job);
                Add("savesWritten");
                return;
            }
            if (AdoptOwnWrite(c, r)) return; // same job goes again, with the rev our lost write produced
            if (r.Code == "rev_conflict" || r.Code == "session_mismatch" || r.Status == 404) { Lose(c, r.Code); return; }
            string state = c.Capture();
            Pop(job);
            c.Uncertain = false;
            Add("savesRejected");
            if (Rejected != null) Rejected(c, r.Code, r.Message, state);
        }

        /// <summary>A rev_conflict straight after a write whose answer was lost: that write landed. Only this session can write, so memory is still the newest.</summary>
        bool AdoptOwnWrite(SavedCharacter c, ZoneApiReply r)
        {
            if (r.Code != "rev_conflict" || !c.Uncertain || r.CurrentRev < 0) return false;
            c.Rev = r.CurrentRev;
            c.Uncertain = false;
            Add("savesRevAdopted");
            return true;
        }

        void OnTradeAnswer(Job job, ZoneApiReply r)
        {
            Pop(job);
            job.A.Uncertain = job.B.Uncertain = false;
            if (r.Ok) { job.A.Rev = r.Rev; job.B.Rev = r.RevB; Add("tradesCommitted"); }
            else Add("tradesRefused");
            job.TradeDone(r.Ok);
        }

        void OnEnhanceAnswer(Job job, ZoneApiReply r)
        {
            SavedCharacter c = job.A;
            if (AdoptOwnWrite(c, r)) return;
            if (r.Code == "session_mismatch") { Lose(c, r.Code); return; }
            Pop(job);
            c.Uncertain = false;
            if (r.Ok && r.Rev >= 0) c.Rev = r.Rev;
            job.EnhanceDone(r);
        }

        void Finish(Job job)
        {
            SavedCharacter c = job.A;
            characters.Remove(c);
            Add("sessionsEnded");
            if (Finished != null) Finished(c);
        }

        void Lose(SavedCharacter c, string code)
        {
            var jobs = new List<Job>(c.Jobs);
            c.Jobs.Clear();
            characters.Remove(c);
            Add("charactersLost");
            foreach (Job j in jobs)
            {
                if (j.Kind == JobKind.Trade) { j.A.Jobs.Remove(j); j.B.Jobs.Remove(j); j.TradeDone(false); }
                else if (j.Kind == JobKind.Enhance) j.EnhanceDone(new ZoneApiReply { Status = 409, Code = code, CurrentRev = -1, Rev = -1 });
            }
            if (Lost != null) Lost(c, code, c.Capture());
        }

        /// <summary>Forgets everything still waiting (the process is going down, ZonePersistence keeps a copy on disk first).</summary>
        public void Abandon()
        {
            foreach (SavedCharacter c in characters) c.Jobs.Clear();
            characters.Clear();
        }
    }
}
