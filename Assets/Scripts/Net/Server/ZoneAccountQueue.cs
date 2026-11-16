using System;
using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The zone server's writer for one player's account (server-api `accounts`, plans/261116-0040-pet-companion-and-fashion-gacha). One job at a time, in order:
    /// compute on a copy of the cached account -> POST account/write with the cached rev and the job's reqId -> on 2xx adopt the copy and the new rev, then answer.
    /// 409 (another hero of the account wrote first): fetch the account, adopt it, compute once more. No answer: send the same reqId and state again (server-api
    /// replays a reqId it has seen, so a retry can never apply twice), up to MaxSends. Anything else refuses the job; the cached account stays as it was.
    /// </summary>
    public sealed class ZoneAccountQueue : IAccountBackend
    {
        const int MaxSends = 3;

        [Serializable] class RevDto { public int rev = -1; }
        [Serializable] class AccountDto { public int rev = -1; public AccountState state; }

        readonly ZoneApiHttp http;
        readonly string characterId, sessionId;
        readonly Queue<AccountJob> jobs = new Queue<AccountJob>();
        AccountState live;
        int rev;
        bool busy;

        public ZoneAccountQueue(ZoneApiHttp api, string character, string session, int startRev)
        {
            http = api; characterId = character; sessionId = session; rev = startRev;
        }

        public void Submit(AccountState liveState, AccountJob job)
        {
            live = liveState;
            jobs.Enqueue(job);
            if (!busy) Next();
        }

        void Next()
        {
            if (jobs.Count == 0) { busy = false; return; }
            busy = true;
            Attempt(jobs.Dequeue(), false);
        }

        void Attempt(AccountJob job, bool reloaded)
        {
            AccountState copy = live.Clone();
            ActionResult r;
            try { r = job.Compute(copy); }
            catch (Exception e) { Debug.LogError("[ZenoniaProto] account: " + job.ReqId + " threw " + e.Message); r = ActionResult.Fail("Something went wrong"); }
            if (!r.Ok) { Finish(job, r); return; }
            Send(job, copy, r, JsonUtility.ToJson(copy), 1, reloaded);
        }

        void Send(AccountJob job, AccountState copy, ActionResult r, string json, int sends, bool reloaded)
        {
            http.AccountWrite(characterId, sessionId, rev, job.ReqId, json, (status, text) =>
            {
                if (status >= 200 && status < 300)
                {
                    RevDto d = Parse<RevDto>(text);
                    if (d != null && d.rev >= 0) rev = d.rev;
                    live.CopyFrom(copy);
                    Finish(job, r);
                }
                else if (status == 409 && !reloaded) Reload(job);
                else if (status == 0 && sends < MaxSends) Send(job, copy, r, json, sends + 1, reloaded);
                else
                {
                    Debug.LogError("[ZenoniaProto] account: write " + job.ReqId + " of character " + characterId + " refused (" + status + "): " + (text ?? "no answer"));
                    Finish(job, ActionResult.Fail(status == 0 ? "The server is busy, try again" : "Could not save, try again"));
                }
            });
        }

        /// <summary>Someone else wrote the account: adopt what server-api holds, then run the job again on it.</summary>
        void Reload(AccountJob job)
        {
            http.AccountGet(characterId, sessionId, (status, text) =>
            {
                AccountDto d = status >= 200 && status < 300 ? Parse<AccountDto>(text) : null;
                if (d == null || d.state == null || d.rev < 0) { Finish(job, ActionResult.Fail("Could not save, try again")); return; }
                rev = d.rev;
                live.CopyFrom(AccountState.Sanitize(d.state));
                Attempt(job, true);
            });
        }

        void Finish(AccountJob job, ActionResult r)
        {
            try { if (job.Done != null) job.Done(r); }
            catch (Exception e) { Debug.LogError("[ZenoniaProto] account: answer of " + job.ReqId + " threw " + e.Message); }
            Next();
        }

        static T Parse<T>(string text) where T : class
        {
            try { return string.IsNullOrEmpty(text) ? null : JsonUtility.FromJson<T>(text); }
            catch (Exception) { return null; }
        }
    }
}
