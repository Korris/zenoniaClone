using System;
using System.Collections.Generic;
using System.Text;

namespace ZenoniaProto.Net
{
    /// <summary>One side of a wager call: the character as the zone holds it now (state = full save JSON, as for a trade) and the card it puts in.</summary>
    public struct WagerSide
    {
        public string CharacterId, SessionId, StateJson, CardId;
        public int Rev;
    }

    /// <summary>A settle's payout to one receiver: Side 0 = a, 1 = b; Rev = the character's rev at the API afterwards. Apply = the zone's own rev was behind, so the zone must add the cards to its copy now.</summary>
    public struct WagerCredit
    {
        public int Side, Rev;
        public string CharacterId;
        public string[] Cards;
        public bool Apply;
    }

    /// <summary>
    /// The two wager calls to server-api (docs: phase 06 "Hợp đồng HTTP"), over any `post(path, json, done)` that answers (status, body) with status 0 = no answer. Pure and with no HTTP in it, so the tests drive it
    /// with canned answers; ZoneApiHttp gives it the real POST. The key header is the transport's business and never appears here.
    /// </summary>
    public sealed class WagerApiClient
    {
        readonly Action<string, string, Action<int, string>> post;

        public WagerApiClient(Action<string, string, Action<int, string>> postJson) { post = postJson; }

        public void Escrow(string matchId, WagerSide a, WagerSide b, Action<ZoneApiReply> done)
        {
            post("/internal/card-wagers/escrow", EscrowJson(matchId, a, b), (s, t) => done(EscrowReply(s, t)));
        }

        /// <summary>`result` = "a" / "b" / "draw". `a` / `b` = the receivers that are online on this zone (null = not online here: the API writes the card straight to the character).</summary>
        public void Settle(string matchId, string result, WagerSide? a, WagerSide? b, Action<ZoneApiReply> done)
        {
            post("/internal/card-wagers/" + Uri.EscapeDataString(matchId) + "/settle", SettleJson(result, a, b), (s, t) => done(SettleReply(s, t)));
        }

        public static string EscrowJson(string matchId, WagerSide a, WagerSide b)
        {
            return "{\"matchId\":" + ZoneApiReplyParser.Str(matchId) + ",\"a\":" + Side(a, true) + ",\"b\":" + Side(b, true) + "}";
        }

        public static string SettleJson(string result, WagerSide? a, WagerSide? b)
        {
            var sb = new StringBuilder("{\"result\":" + ZoneApiReplyParser.Str(result));
            if (a.HasValue) sb.Append(",\"a\":").Append(Side(a.Value, false));
            if (b.HasValue) sb.Append(",\"b\":").Append(Side(b.Value, false));
            return sb.Append('}').ToString();
        }

        static string Side(WagerSide s, bool withCard)
        {
            string json = "{" + (withCard ? "\"characterId\":" + ZoneApiReplyParser.Str(s.CharacterId) + "," : "") + "\"sessionId\":" + ZoneApiReplyParser.Str(s.SessionId) + ",\"rev\":" + s.Rev + ",\"state\":" + s.StateJson;
            if (withCard) json += ",\"cardId\":" + ZoneApiReplyParser.Str(s.CardId);
            return json + "}";
        }

        /// <summary>200: Rev = side a's rev, RevB = side b's (the cards are already out of both characters). Otherwise the standard refusal fields.</summary>
        public static ZoneApiReply EscrowReply(int status, string text)
        {
            ZoneApiReply r = ZoneApiReplyParser.Reply(status, text);
            if (!r.Ok) return r;
            Dictionary<string, string> top = ZoneApiReplyParser.Members(text);
            if (top == null || !Rev(top, "a", out r.Rev) || !Rev(top, "b", out r.RevB)) return Garbled(r);
            r.Replayed = top.ContainsKey("replayed") && top["replayed"] == "true";
            return r;
        }

        /// <summary>200: Credits = who got which cards at which rev. Otherwise the standard refusal fields.</summary>
        public static ZoneApiReply SettleReply(int status, string text)
        {
            ZoneApiReply r = ZoneApiReplyParser.Reply(status, text);
            if (!r.Ok) return r;
            Dictionary<string, string> top = ZoneApiReplyParser.Members(text);
            string list;
            if (top == null || !top.TryGetValue("credited", out list)) return Garbled(r);
            List<string> items = ZoneApiReplyParser.Elements(list);
            if (items == null) return Garbled(r);
            var credits = new List<WagerCredit>();
            foreach (string item in items)
            {
                Dictionary<string, string> m = ZoneApiReplyParser.Members(item);
                string side, id, cards;
                if (m == null || !m.TryGetValue("side", out side) || !m.TryGetValue("characterId", out id) || !m.TryGetValue("cards", out cards)) return Garbled(r);
                var ids = new List<string>();
                List<string> raw = ZoneApiReplyParser.Elements(cards);
                if (raw == null) return Garbled(r);
                foreach (string c in raw) { string u = ZoneApiReplyParser.Unquote(c); if (u != null) ids.Add(u); }
                string sideName = ZoneApiReplyParser.Unquote(side);
                credits.Add(new WagerCredit { Side = sideName == "b" ? 1 : 0, CharacterId = ZoneApiReplyParser.Unquote(id), Cards = ids.ToArray(), Rev = m.ContainsKey("rev") ? ZoneApiReplyParser.ToInt(m["rev"], -1) : -1 });
            }
            r.Credits = credits.ToArray();
            r.Replayed = top.ContainsKey("replayed") && top["replayed"] == "true";
            return r;
        }

        static bool Rev(Dictionary<string, string> top, string side, out int rev)
        {
            rev = -1;
            string raw;
            if (!top.TryGetValue(side, out raw)) return false;
            Dictionary<string, string> m = ZoneApiReplyParser.Members(raw);
            string v;
            if (m == null || !m.TryGetValue("rev", out v)) return false;
            rev = ZoneApiReplyParser.ToInt(v, -1);
            return rev >= 0;
        }

        // a 200 whose body is not what the contract says: not a success we can act on (the card may have moved, so it is a "no answer")
        static ZoneApiReply Garbled(ZoneApiReply r) { r.Status = 0; r.Code = "bad_reply"; r.Rev = r.RevB = -1; return r; }
    }
}
