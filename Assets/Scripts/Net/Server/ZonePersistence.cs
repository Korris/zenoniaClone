using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ZenoniaProto.Api;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Real characters on a dedicated server (--api-url): loads them from server-api against a one-time ticket and writes them back through CharacterSaveQueue, so nothing here ever blocks a tick.
    /// Written: every --save-interval seconds (also the heartbeat of the one-session lock), on leaving, on a map change, on a level-up, an epic pickup, a boss kill, on a trade (both sides together, before anything moves), on server shutdown.
    /// A state the API will not take is never just dropped: it goes to a file in the spool folder with a loud error, because a person has to look at it.
    /// The client only ever sends a ticket: name, class, stats and goods come from the API. Dev cheats (--dev-*) do not exist on this path.
    /// </summary>
    public sealed class ZonePersistence
    {
        public const string ServiceKeyVariable = "ZENONIA_SERVICE_KEY";
        const float ReadyRetrySeconds = 2f;

        public static ZonePersistence Active { get; private set; }

        /// <summary>Answer to a redeemed ticket.</summary>
        public sealed class LoadedCharacter
        {
            public bool Ok;
            public RejectReason Refusal;
            public string Text;
            public string CharacterId, SessionId, Name;
            /// <summary>This character was loaded from a file a stopping zone left, not from the API's copy.</summary>
            public string KeptPath;
            public int Rev;
            public HeroClassDef Def;
            public SaveData Save;
            public PlayerState State;
        }

        [Serializable] class RedeemDto { public string sessionId; public CharacterDetail character; }
        [Serializable] class KeptDto { public string characterId; public int rev; public string why; public SaveData state; }

        /// <summary>A kept state that was loaded instead of what the API had: its file goes away once the API has it.</summary>
        sealed class Reloaded { public SavedCharacter Saved; public int Rev; public string Path; }

        // KeepWhatIsLeft: the zone went down holding the newest state with no API to write it to. Nothing is wrong with the state itself.
        // A refused or lost state is not on this list: that one really does need a person.
        const string WhyStopped = "server stopped", WhyDestroyed = "server object destroyed";

        readonly ZoneApiHttp http;
        readonly CharacterSaveQueue queue;
        readonly string spool;
        readonly Dictionary<string, NetPlayer> here = new Dictionary<string, NetPlayer>();
        readonly List<Reloaded> reloaded = new List<Reloaded>();
        NetStats stats;
        Action<NetPlayer> onLost;
        float nextReadyAttempt;
        bool resetting;

        /// <summary>The locks of an earlier run are released, or the API said this zone has none: tickets can be taken.</summary>
        public bool Ready { get; private set; }
        public bool ApiHealthy { get { return queue.ApiHealthy; } }
        /// <summary>Nothing left to write and nobody's lock left to give back.</summary>
        public bool Idle { get { return queue.Characters == 0; } }

        /// <summary>Reads the config. False (and an error in the log) means the server must not start: running without saves by accident would be worse.</summary>
        public static bool TryCreate(out ZonePersistence p)
        {
            p = null;
            string url = NetLaunchArgs.ApiUrl;
            string key = Environment.GetEnvironmentVariable(ServiceKeyVariable);
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) { Debug.LogError("[ZenoniaProto] save: --api-url must start with http:// or https://"); return false; }
            if (string.IsNullOrEmpty(key) || key.Length < 16) { Debug.LogError("[ZenoniaProto] save: the environment variable " + ServiceKeyVariable + " must hold server-api's SERVICE_KEY (at least 16 characters)"); return false; }
            if (NetLaunchArgs.DevLevel + NetLaunchArgs.DevGold + NetLaunchArgs.DevItems + NetLaunchArgs.DevHurtPct > 0 || !string.IsNullOrEmpty(NetLaunchArgs.DevKey))
                Debug.LogWarning("[ZenoniaProto] save: --dev-* options are ignored with --api-url: dev cheats never apply to real accounts");
            p = new ZonePersistence(new ZoneApiHttp(url, key));
            return true;
        }

        ZonePersistence(ZoneApiHttp api)
        {
            http = api;
            queue = new CharacterSaveQueue(http, NetLaunchArgs.SaveIntervalSeconds * 1000L, null);
            spool = !string.IsNullOrEmpty(NetLaunchArgs.SaveSpool) ? NetLaunchArgs.SaveSpool : Path.Combine(Application.persistentDataPath, "save-failed");
        }

        public void Start(NetStats netStats, Action<NetPlayer> lostHandler)
        {
            stats = netStats;
            onLost = lostHandler;
            Active = this;
            queue.Count += (name, n) => stats.Add(name, n);
            queue.Lost += OnLost;
            queue.Rejected += OnRejected;
            ReportSpool();
            Debug.Log("[ZenoniaProto] save: real accounts on, zone " + NetLaunchArgs.ZoneId + ", saving every " + NetLaunchArgs.SaveIntervalSeconds + " s");
        }

        public void Stop()
        {
            if (Active == this) Active = null;
            http.Dispose();
        }

        /// <summary>Once per tick, on the main thread.</summary>
        public void Tick(long nowMs)
        {
            http.Pump();
            queue.Tick(nowMs);
            if (reloaded.Count > 0) RetireReloadedFiles();
            if (!Ready && !resetting && Time.realtimeSinceStartup >= nextReadyAttempt) ResetOldSessions();
        }

        void ResetOldSessions()
        {
            resetting = true;
            http.ResetSessions(NetLaunchArgs.ZoneId, (status, text) =>
            {
                resetting = false;
                nextReadyAttempt = Time.realtimeSinceStartup + ReadyRetrySeconds;
                if (status >= 200 && status < 300) { Ready = true; Debug.Log("[ZenoniaProto] save: the API answers, zone sessions reset"); }
                else if (status == 404) { Ready = true; Debug.LogError("[ZenoniaProto] save: the API does not know zone " + NetLaunchArgs.ZoneId + " (its ZONE_ID differs): a crash will hold characters for 90 s"); }
                else if (status == 401 || status == 403) Debug.LogError("[ZenoniaProto] save: the API refuses this service key");
            });
        }

        void ReportSpool()
        {
            try
            {
                int kept = Directory.Exists(spool) ? Directory.GetFiles(spool, "*.json").Length : 0;
                if (kept > 0) Debug.LogError("[ZenoniaProto] save: " + kept + " character state(s) from earlier runs could not be written and wait in " + spool + ": one left by a stopping zone is loaded when its player next joins, unless the character has changed since; the others need a person to look at them");
            }
            catch (Exception) { /* only a warning that could not be given */ }
        }

        /// <summary>
        /// The newest state a stopping zone left on disk for this character, if it is safe to use: the API's rev is still the one that state was built on, so nothing was written after it, and it is newer than what the API holds.
        /// Any other rev means a write happened since (a trade, an enhance, another save): the file stays for a person.
        /// </summary>
        bool TryReadKept(string characterId, int rev, out SaveData state, out string path)
        {
            state = null; path = null;
            try
            {
                if (!Directory.Exists(spool)) return false;
                string[] files = Directory.GetFiles(spool, characterId + "-*.json");
                if (files.Length == 0) return false;
                Array.Sort(files, StringComparer.Ordinal); // the names end in the time they were written
                string newest = files[files.Length - 1];
                KeptDto kept = JsonUtility.FromJson<KeptDto>(File.ReadAllText(newest));
                if (kept == null || kept.characterId != characterId || kept.state == null || (kept.why != WhyStopped && kept.why != WhyDestroyed)) return false;
                if (kept.rev != rev)
                {
                    Debug.LogError("[ZenoniaProto] save: kept state " + newest + " is not used: built on rev " + kept.rev + ", the API is at " + rev);
                    return false;
                }
                state = SaveSerializer.Parse(JsonUtility.ToJson(kept.state), FrostHollowLayout.RowCount);
                path = newest;
                return state != null;
            }
            catch (Exception e)
            {
                Debug.LogError("[ZenoniaProto] save: could not read the kept states of character " + characterId + ": " + e.GetType().Name);
                return false;
            }
        }

        /// <summary>The API took a write after the reload: the file has done its job. It is renamed, not deleted, in case somebody wants to see what happened.</summary>
        void RetireReloadedFiles()
        {
            for (int i = reloaded.Count - 1; i >= 0; i--)
            {
                Reloaded r = reloaded[i];
                if (r.Saved.Rev <= r.Rev) continue;
                reloaded.RemoveAt(i);
                try { File.Move(r.Path, r.Path + ".reloaded"); Debug.Log("[ZenoniaProto] save: kept state of character " + r.Saved.Id + " is written back"); }
                catch (Exception e) { Debug.LogError("[ZenoniaProto] save: written back, but " + r.Path + " could not be renamed: " + e.GetType().Name); }
            }
        }

        // ---- joining ----

        public void Redeem(string ticket, Action<LoadedCharacter> done)
        {
            http.Redeem(ticket, (status, text) => done(ReadRedeem(status, text)));
        }

        LoadedCharacter ReadRedeem(int status, string text)
        {
            if (status < 200 || status >= 300)
            {
                ZoneApiReply r = ZoneApiHttp.Reply(status, text);
                if (status == 401 && r.Code == "invalid_ticket" || status == 400) return Refused(RejectReason.BadToken, "Bad or expired ticket: log in again");
                if (status == 409) return Refused(RejectReason.AlreadyOnline, "This character is already online");
                if (status == 401 || status == 403) Debug.LogError("[ZenoniaProto] save: the API refuses this service key");
                else if (status == 404) return Refused(RejectReason.BadToken, "That character no longer exists");
                return Refused(RejectReason.Unavailable, "The login server cannot be reached, try again");
            }
            RedeemDto dto = null;
            try { dto = JsonUtility.FromJson<RedeemDto>(text); }
            catch (Exception) { /* below */ }
            SaveData save = dto != null && dto.character != null && dto.character.state != null ? SaveSerializer.Parse(JsonUtility.ToJson(dto.character.state), FrostHollowLayout.RowCount) : null;
            HeroClassDef def = save != null ? DefOf(dto.character.classId) : null;
            if (def == null || save.classId != dto.character.classId)
            {
                Debug.LogError("[ZenoniaProto] save: unreadable character " + (dto != null && dto.character != null ? dto.character.id : "?") + " from the API");
                if (dto != null && dto.character != null && !string.IsNullOrEmpty(dto.sessionId)) http.EndSession(dto.character.id, dto.sessionId, r => { });
                return Refused(RejectReason.Unavailable, "This character could not be loaded");
            }
            SaveData keptState;
            string keptPath;
            if (TryReadKept(dto.character.id, dto.character.rev, out keptState, out keptPath) && keptState.classId == dto.character.classId)
            {
                save = keptState;
                Debug.Log("[ZenoniaProto] save: character " + dto.character.id + " continues from the state kept in " + keptPath);
                stats.Add("savesReloadedFromDisk", 1);
            }
            else keptPath = null;
            return new LoadedCharacter
            {
                KeptPath = keptPath,
                Ok = true, CharacterId = dto.character.id, SessionId = dto.sessionId, Rev = dto.character.rev, Name = DevLoginStub.SanitizeName(dto.character.name),
                Def = def, Save = save, State = SaveSerializer.Restore(save),
            };
        }

        static LoadedCharacter Refused(RejectReason why, string text) { return new LoadedCharacter { Refusal = why, Text = text }; }

        static HeroClassDef DefOf(string classId)
        {
            foreach (HeroClassDef d in HeroClassDef.All) if (RpgSession.ClassId(d.Id) == classId) return d;
            return null;
        }

        /// <summary>This character is in the world here, or still being written out.</summary>
        public bool IsHere(string characterId)
        {
            if (here.ContainsKey(characterId)) return true;
            foreach (SavedCharacter c in queue.All) if (c.Id == characterId) return true;
            return false;
        }

        /// <summary>A ticket was redeemed but the player is not coming (left meanwhile, full): nothing changed, only the lock goes back.</summary>
        public void ReleaseUnused(LoadedCharacter c) { http.EndSession(c.CharacterId, c.SessionId, r => { }); }

        public void Attach(NetPlayer p, LoadedCharacter c)
        {
            p.LastX = c.Save.x; p.LastY = c.Save.y; p.LastMapId = c.Save.mapId;
            p.Saved = queue.Register(c.CharacterId, c.SessionId, c.Rev, () => Capture(p));
            p.Saved.Owner = p;
            here[c.CharacterId] = p;
            p.Rpg.ExpGained += (exp, levels, at) => { if (levels > 0) RequestSave(p); };
            p.Rpg.BossKilled += () => RequestSave(p);
            p.Rpg.LootPicked += (e, r) => { if (r.Ok && e.Kind == LootKind.Equipment && e.Item != null && e.Item.rarity == "epic") RequestSave(p); };
            if (c.KeptPath != null)
            {
                reloaded.Add(new Reloaded { Saved = p.Saved, Rev = c.Rev, Path = c.KeptPath });
                RequestSave(p); // the API gets it now; until it has it, the file stays
            }
        }

        /// <summary>The character as it is right now. After the hero has gone, it stands where it left.</summary>
        static string Capture(NetPlayer p)
        {
            HeroController h = p.Hero;
            if (h != null)
            {
                p.LastX = h.Position.x; p.LastY = h.Position.y;
                if (h.HomeZone != null && h.HomeZone.Map != null) p.LastMapId = h.HomeZone.Map.Id;
            }
            return JsonUtility.ToJson(SaveSerializer.Capture(p.Rpg.State, true, p.LastX, p.LastY, p.Bosses.KingDefeated, 0, p.LastMapId, p.Bosses.OwnBossMaps));
        }

        // ---- while playing ----

        public void RequestSave(NetPlayer p) { if (p.Saved != null) queue.RequestSave(p.Saved); }

        public void OnLeave(NetPlayer p)
        {
            if (p.Saved == null) return;
            Capture(p);
            here.Remove(p.Saved.Id);
            queue.Leave(p.Saved);
        }

        public void CommitTrade(NetPlayer a, NetPlayer b, TradeCommitRequest c, Action<bool> done)
        {
            queue.CommitTrade(a.Saved, b.Saved, c.AGaveUids, c.AGaveGold, c.BGaveUids, c.BGaveGold, done);
        }

        /// <summary>The API rolls it; the answer changes the item here and goes to the client, which is simply waiting for its request to be answered. The player leaving meanwhile changes nothing: the last save comes after it.</summary>
        public void Enhance(ServerReplication repl, NetPlayer p, InvActionMsg m)
        {
            Item target = FindItem(p.Rpg.State, m.ItemUid);
            if (target == null || !EnhanceStats.IsEnhanceable(target)) { repl.SendRpgState(p, m.ReqId, false, "Weapons only"); p.RpgDirty = false; return; }
            if (p.EnhancePending) { repl.SendRpgState(p, m.ReqId, false, "Wait for the last attempt"); p.RpgDirty = false; return; }
            p.EnhancePending = true;
            string uid = m.ItemUid;
            queue.Enhance(p.Saved, uid, r => OnEnhanceAnswer(repl, p, uid, m.ReqId, r));
        }

        static void OnEnhanceAnswer(ServerReplication repl, NetPlayer p, string uid, uint reqId, ZoneApiReply r)
        {
            p.EnhancePending = false;
            PlayerState state = p.Rpg.State;
            string before = p.Hero != null ? ServerInventoryActions.LookKey(p) : null;
            bool ok = false;
            string text = "Cannot enhance";
            if (r.Ok && r.Attempted)
            {
                state.UpgradeStone = Math.Max(0, state.UpgradeStone - 1);
                Item item = FindItem(state, uid);
                if (item != null) item.enhance = r.Level;
                state.RecomputeStats();
                ok = r.Success;
                text = r.Success ? "Enhanced to +" + r.Level : "Enhance failed (+" + r.Level + " kept)";
            }
            if (p.Hero == null) return;
            Zone.Enter(p.Hero.HomeZone);
            if (ServerInventoryActions.LookKey(p) != before) ServerInventoryActions.LookChanged(p);
            repl.SendRpgState(p, reqId, ok, text);
            p.RpgDirty = false;
        }

        static Item FindItem(PlayerState s, string uid)
        {
            Item i = s.Inventory.Find(x => x.uid == uid);
            if (i != null) return i;
            foreach (string slot in Slots.All) { Item e = s.GetEquipped(slot); if (e != null && e.uid == uid) return e; }
            return null;
        }

        // ---- when a write cannot happen ----

        void OnLost(SavedCharacter c, string code, string state)
        {
            Keep(c, code, state);
            Debug.LogError("[ZenoniaProto] save: character " + c.Id + " lost its session (" + code + "): this zone no longer owns it, the player is sent out");
            var p = c.Owner as NetPlayer;
            if (p == null) return;
            here.Remove(c.Id);
            p.Saved = null;
            onLost(p);
        }

        void OnRejected(SavedCharacter c, string code, string message, string state)
        {
            Keep(c, code, state);
            Debug.LogError("[ZenoniaProto] save: the API refused the state of character " + c.Id + " (" + code + "): " + message);
        }

        /// <summary>Whatever could not be written before the process goes down goes to disk, with an error per character.</summary>
        public void KeepWhatIsLeft(string why)
        {
            if (queue.Characters == 0) return;
            foreach (SavedCharacter c in queue.All) Keep(c, why, c.CaptureNow());
            queue.Abandon();
        }

        void Keep(SavedCharacter c, string why, string state)
        {
            stats.Add("savesKeptOnDisk", 1);
            try
            {
                Directory.CreateDirectory(spool);
                string safeWhy = (why ?? "unknown").Replace('"', '\'').Replace('\\', '/');
                string path = Path.Combine(spool, c.Id + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".json");
                File.WriteAllText(path, "{\"characterId\":\"" + c.Id + "\",\"rev\":" + c.Rev + ",\"why\":\"" + safeWhy + "\",\"state\":" + state + "}");
                Debug.LogError("[ZenoniaProto] save: state of character " + c.Id + " kept in " + path + " (" + safeWhy + ")");
            }
            catch (Exception e) { Debug.LogError("[ZenoniaProto] save: PROGRESS LOST for character " + c.Id + ", could not even be written to disk: " + e.Message); }
        }
    }
}
