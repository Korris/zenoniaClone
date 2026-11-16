using System;
using System.IO;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>One account change: Compute mutates a COPY of the account (Ok = store it), Done runs once it is stored or refused. Compute may run twice (a 409 reloads and retries).</summary>
    public sealed class AccountJob
    {
        public string ReqId;
        public Func<AccountState, ActionResult> Compute;
        public Action<ActionResult> Done;
    }

    /// <summary>Where account changes are stored: a local file (offline), RAM (dev host without server-api) or server-api through the zone's queue (ZoneAccountQueue).</summary>
    public interface IAccountBackend { void Submit(AccountState live, AccountJob job); }

    /// <summary>
    /// The account behind one player's PlayerRpg (Pet Companion + pet gacha): the live AccountState every hero of the account points at, and the backend that stores changes.
    /// Run never changes the live object before the backend has stored the copy, so a refused write leaves nothing behind.
    /// </summary>
    public sealed class AccountLink
    {
        public readonly AccountState Live;
        readonly IAccountBackend backend;

        public AccountLink(AccountState live, IAccountBackend store) { Live = live ?? new AccountState(); backend = store; }

        /// <summary>A new request id: unique across every hero of every account, so server-api's replay check can never confuse two changes.</summary>
        public static string NewReqId(string kind) { return kind + "-" + Guid.NewGuid().ToString("N").Substring(0, 20); }

        public void Run(string kind, Func<AccountState, ActionResult> compute, Action<ActionResult> done)
        {
            backend.Submit(Live, new AccountJob { ReqId = NewReqId(kind), Compute = compute, Done = done });
        }

        /// <summary>Offline: the account file next to the class saves. Dev host: nothing on disk.</summary>
        public static AccountLink Local(bool persist) { return new AccountLink(persist ? AccountStore.Load() : new AccountState(), new LocalAccountBackend(persist)); }
    }

    /// <summary>Stores at once: offline into zenonia-account.json (AccountStore), on a dev host only in RAM.</summary>
    public sealed class LocalAccountBackend : IAccountBackend
    {
        readonly bool persist;
        public LocalAccountBackend(bool persistToFile) { persist = persistToFile; }

        public void Submit(AccountState live, AccountJob job)
        {
            AccountState copy = live.Clone();
            ActionResult r;
            try { r = job.Compute(copy); }
            catch (Exception e) { Debug.LogError("[ZenoniaProto] account: " + job.ReqId + " failed: " + e.Message); r = ActionResult.Fail("Something went wrong"); }
            if (r.Ok)
            {
                live.CopyFrom(copy);
                if (persist) AccountStore.Write(live);
            }
            if (job.Done != null) job.Done(r);
        }
    }

    /// <summary>
    /// The offline account: one JSON file (zenonia-account.json) in Application.persistentDataPath, shared by every class save, written through a temp file like SaveStore.
    /// Unreadable data is kept as .bad and a fresh account starts. Off when SaveStore is off (online play, capture runs).
    /// </summary>
    public static class AccountStore
    {
        public static string PathOf { get { return Path.Combine(SaveStore.Root, "zenonia-account.json"); } }

        public static AccountState Load()
        {
            if (!SaveStore.Enabled) return new AccountState();
            try
            {
                if (!File.Exists(PathOf)) return new AccountState();
                AccountState raw = JsonUtility.FromJson<AccountState>(File.ReadAllText(PathOf));
                if (raw != null) return AccountState.Sanitize(raw);
                File.Copy(PathOf, PathOf + ".bad", true);
            }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] account load failed: " + e.Message); }
            return new AccountState();
        }

        public static void Write(AccountState a)
        {
            if (!SaveStore.Enabled || a == null) return;
            try
            {
                string tmp = PathOf + ".tmp";
                Directory.CreateDirectory(Path.GetDirectoryName(PathOf));
                File.WriteAllText(tmp, JsonUtility.ToJson(a, true));
                if (File.Exists(PathOf)) File.Delete(PathOf);
                File.Move(tmp, PathOf);
            }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] account save failed: " + e.Message); }
        }
    }
}
