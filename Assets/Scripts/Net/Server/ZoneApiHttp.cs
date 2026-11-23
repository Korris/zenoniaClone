using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// The zone server's calls to server-api, with the service key. Requests run on the thread pool, so the game loop never waits for one; their answers wait in a queue for Pump, on the main thread.
    /// The key goes in a header and is never written anywhere else. A timeout counts as "no answer", which the save queue treats as "may have happened".
    /// </summary>
    public sealed class ZoneApiHttp : IZoneApi, IDisposable
    {
        const int TimeoutSeconds = 10;
        const int MaxErrorText = 400;

        readonly HttpClient http;
        readonly string baseUrl, serviceKey;
        readonly ConcurrentQueue<Action> answers = new ConcurrentQueue<Action>();

        public ZoneApiHttp(string url, string key)
        {
            baseUrl = url.TrimEnd('/');
            serviceKey = key;
            http = new HttpClient { Timeout = TimeSpan.FromSeconds(TimeoutSeconds) };
            Wagers = new WagerApiClient(Post);
        }

        /// <summary>The Card Wager calls (escrow / settle) on this connection.</summary>
        public readonly WagerApiClient Wagers;

        public void Dispose() { http.Dispose(); }

        /// <summary>Runs the answers that came in, on the calling (main) thread.</summary>
        public void Pump()
        {
            Action a;
            while (answers.TryDequeue(out a)) a();
        }

        // ---- IZoneApi ----

        public void Save(string characterId, string sessionId, int rev, string stateJson, Action<ZoneApiReply> done)
        {
            Post("/internal/characters/" + characterId + "/save", "{\"sessionId\":" + Str(sessionId) + ",\"rev\":" + rev + ",\"state\":" + stateJson + "}", (s, t) =>
            {
                ZoneApiReply r = Reply(s, t);
                if (r.Ok) r.Rev = Parsed<RevDto>(t).rev;
                done(r);
            });
        }

        public void CommitTrade(string tradeId, TradeCommitSide a, TradeCommitSide b, Action<ZoneApiReply> done)
        {
            Post("/internal/trades/commit", "{\"tradeId\":" + Str(tradeId) + ",\"a\":" + Side(a) + ",\"b\":" + Side(b) + "}", (s, t) =>
            {
                ZoneApiReply r = Reply(s, t);
                if (r.Ok) { TradeDto d = Parsed<TradeDto>(t); r.Rev = d.a.rev; r.RevB = d.b.rev; }
                done(r);
            });
        }

        public void Enhance(string characterId, string sessionId, int rev, string reqId, string itemUid, Action<ZoneApiReply> done)
        {
            Post("/internal/characters/" + characterId + "/enhance", "{\"sessionId\":" + Str(sessionId) + ",\"rev\":" + rev + ",\"reqId\":" + Str(reqId) + ",\"itemUid\":" + Str(itemUid) + "}", (s, t) =>
            {
                ZoneApiReply r = Reply(s, t);
                if (r.Ok) { EnhanceDto d = Parsed<EnhanceDto>(t); r.Rev = d.rev; r.Attempted = d.ok; r.Success = d.success; r.Level = d.level; }
                done(r);
            });
        }

        public void EscrowWager(string matchId, WagerSide a, WagerSide b, Action<ZoneApiReply> done) { Wagers.Escrow(matchId, a, b, done); }

        public void SettleWager(string matchId, string result, WagerSide? a, WagerSide? b, Action<ZoneApiReply> done) { Wagers.Settle(matchId, result, a, b, done); }

        public void EndSession(string characterId, string sessionId, Action<ZoneApiReply> done)
        {
            Post("/internal/sessions/" + sessionId + "/end", "{\"characterId\":" + Str(characterId) + "}", (s, t) => done(Reply(s, t)));
        }

        // ---- the account (Pet Companion + pet gacha, ZoneAccountQueue) ----

        /// <summary>Stores a whole account state built on `rev`; `done` gets status (0 = no answer) and body ({rev}). 409 = someone wrote first.</summary>
        public void AccountWrite(string characterId, string sessionId, int rev, string reqId, string stateJson, Action<int, string> done)
        {
            Post("/internal/characters/" + characterId + "/account/write", "{\"sessionId\":" + Str(sessionId) + ",\"rev\":" + rev + ",\"reqId\":" + Str(reqId) + ",\"state\":" + stateJson + "}", done);
        }

        /// <summary>The account as server-api has it ({rev, state}), after a 409.</summary>
        public void AccountGet(string characterId, string sessionId, Action<int, string> done)
        {
            Post("/internal/characters/" + characterId + "/account/get", "{\"sessionId\":" + Str(sessionId) + "}", done);
        }

        // ---- the rest of what a zone asks ----

        /// <summary>Consumes a ticket: `done` gets the HTTP status (0 = no answer) and the body.</summary>
        public void Redeem(string ticket, Action<int, string> done) { Post("/internal/tickets/redeem", "{\"ticket\":" + Str(ticket) + "}", done); }

        /// <summary>This zone just started: frees the locks a crashed run of it left behind.</summary>
        public void ResetSessions(string zoneId, Action<int, string> done) { Post("/internal/zones/" + Uri.EscapeDataString(zoneId) + "/reset-sessions", "{}", done); }

        // ---- plumbing ----

        static string Side(TradeCommitSide s)
        {
            var uids = new StringBuilder("[");
            for (int i = 0; i < s.GiveUids.Length; i++) { if (i > 0) uids.Append(','); uids.Append(Str(s.GiveUids[i])); }
            uids.Append(']');
            return "{\"characterId\":" + Str(s.CharacterId) + ",\"sessionId\":" + Str(s.SessionId) + ",\"rev\":" + s.Rev + ",\"state\":" + s.StateJson + ",\"giveUids\":" + uids + ",\"giveGold\":" + s.GiveGold + "}";
        }

        void Post(string path, string json, Action<int, string> done) { _ = PostAsync(path, json, done); }

        async Task PostAsync(string path, string json, Action<int, string> done)
        {
            int status = 0;
            string text = null;
            try
            {
                using (var req = new HttpRequestMessage(HttpMethod.Post, baseUrl + path))
                {
                    req.Headers.Add("x-service-key", serviceKey);
                    req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    using (HttpResponseMessage res = await http.SendAsync(req).ConfigureAwait(false))
                    {
                        text = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                        status = (int)res.StatusCode;
                    }
                }
            }
            catch (Exception) { status = 0; text = null; } // timeout, refused, reset: no answer. The queue's backoff and the stats say the rest.
            answers.Enqueue(() => done(status, text));
        }

        /// <summary>The fields every caller needs (ZoneApiReplyParser: errors are read by hand).</summary>
        public static ZoneApiReply Reply(int status, string text) { return ZoneApiReplyParser.Reply(status, text); }

        static T Parsed<T>(string text) where T : new()
        {
            try { return JsonUtility.FromJson<T>(text); }
            catch (Exception) { return new T(); }
        }

        static string Str(string s) { return ZoneApiReplyParser.Str(s); }

        [Serializable] class RevDto { public int rev = -1; }
        [Serializable] class TradeDto { public RevDto a = new RevDto(), b = new RevDto(); }
        [Serializable] class EnhanceDto { public int rev = -1, level; public bool ok, success; }
    }
}
