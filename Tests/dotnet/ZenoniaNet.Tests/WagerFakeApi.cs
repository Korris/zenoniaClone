using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using ZenoniaProto.Net;

/// <summary>
/// A stand-in for server-api's Card Wager (a C# model of services/card-wager-service.js: same checks, same answers, one transaction per call) plus a fake HTTP layer that can lose a request,
/// lose its answer or hold it back, and a small world that runs the REAL CharacterSaveQueue + WagerApiClient + StakeMatch against it. The tests (WagerConservationTests) ask one thing of every scenario:
/// no card is ever made or lost, for both characters, whatever the network did. A character's cards are a count per card id plus the deck list; "state" travels as {"cards":{id:n},"deck":[id]}.
/// </summary>
public sealed class FakeApiServer
{
    public sealed class Doc
    {
        public string Id, Session;
        public int Rev;
        public Dictionary<string, int> Cards = new Dictionary<string, int>();
        public List<string> Deck = new List<string>();
    }

    public sealed class Match
    {
        public string Id, Status = "held", Result;
        public string[] Char = new string[2];
        public string[][] Deck = new string[2][];
        public long ExpiresAt;
    }

    public readonly Dictionary<string, Doc> Docs = new Dictionary<string, Doc>();
    public readonly Dictionary<string, Match> Matches = new Dictionary<string, Match>();
    public long Now;
    public bool WagerEnabled = true;
    public const long HoldMs = 1800 * 1000L;
    int sessions;

    public Doc Add(string id, Dictionary<string, int> cards, List<string> deck)
    {
        var d = new Doc { Id = id, Cards = new Dictionary<string, int>(cards), Deck = new List<string>(deck) };
        Docs[id] = d;
        return d;
    }

    public string Lock(string id) { Docs[id].Session = "s" + (++sessions) + "-" + id; return Docs[id].Session; }

    /// <summary>Cards in the stored characters plus the decks held in escrow: the number that must never change except by what really dropped.</summary>
    public int Total()
    {
        int n = Docs.Values.Sum(d => d.Cards.Values.Sum());
        return n + Matches.Values.Where(m => m.Status == "held").Sum(m => m.Deck[0].Length + m.Deck[1].Length);
    }

    public static string StateJson(Dictionary<string, int> cards, IEnumerable<string> deck)
    {
        return "{\"cards\":{" + string.Join(",", cards.Where(c => c.Value > 0).Select(c => "\"" + c.Key + "\":" + c.Value)) + "},\"deck\":[" + string.Join(",", deck.Select(x => "\"" + x + "\"")) + "]}";
    }

    static (Dictionary<string, int>, List<string>) ReadState(JsonElement st)
    {
        var cards = new Dictionary<string, int>();
        foreach (JsonProperty p in st.GetProperty("cards").EnumerateObject()) cards[p.Name] = p.Value.GetInt32();
        var deck = new List<string>();
        foreach (JsonElement e in st.GetProperty("deck").EnumerateArray()) deck.Add(e.GetString());
        return (cards, deck);
    }

    static (int, string) Err(int status, string code, int currentRev = -1, string side = null)
    {
        string details = currentRev >= 0 ? "\"currentRev\":" + currentRev + (side != null ? ",\"side\":\"" + side + "\"" : "") : side != null ? "\"side\":\"" + side + "\"" : null;
        return (status, "{\"error\":{\"code\":\"" + code + "\",\"message\":\"" + code + "\"" + (details != null ? ",\"details\":{" + details + "}" : "") + "}}");
    }

    static void Add(Dictionary<string, int> cards, string id) { int n; cards.TryGetValue(id, out n); cards[id] = n + 1; }

    static void Trim(Doc d)
    {
        var left = new Dictionary<string, int>(d.Cards);
        for (int i = 0; i < d.Deck.Count; i++)
        {
            int n; left.TryGetValue(d.Deck[i], out n);
            if (n < 1) { d.Deck.RemoveAt(i); i--; } else left[d.Deck[i]] = n - 1;
        }
    }

    // ---- calls ----

    public (int, string) Save(string id, string body)
    {
        Doc d;
        if (!Docs.TryGetValue(id, out d)) return Err(404, "not_found");
        using (JsonDocument j = JsonDocument.Parse(body))
        {
            JsonElement r = j.RootElement;
            if (d.Session == null || d.Session != r.GetProperty("sessionId").GetString()) return Err(409, "session_mismatch");
            if (d.Rev != r.GetProperty("rev").GetInt32()) return Err(409, "rev_conflict", d.Rev);
            var (cards, deck) = ReadState(r.GetProperty("state"));
            d.Cards = cards; d.Deck = deck; d.Rev++;
            return (200, "{\"rev\":" + d.Rev + ",\"savedAt\":0}");
        }
    }

    public (int, string) End(string sessionId, string body)
    {
        using (JsonDocument j = JsonDocument.Parse(body))
        {
            Doc d;
            bool released = Docs.TryGetValue(j.RootElement.GetProperty("characterId").GetString(), out d) && d.Session == sessionId;
            if (released) d.Session = null;
            return (200, "{\"released\":" + (released ? "true" : "false") + "}");
        }
    }

    string Side(Doc d) { return "{\"rev\":" + d.Rev + ",\"state\":" + StateJson(d.Cards, d.Deck) + "}"; }

    public (int, string) Escrow(string body)
    {
        using (JsonDocument j = JsonDocument.Parse(body))
        {
            JsonElement r = j.RootElement;
            string mid = r.GetProperty("matchId").GetString();
            JsonElement[] side = { r.GetProperty("a"), r.GetProperty("b") };
            var doc = new Doc[2];
            for (int s = 0; s < 2; s++)
                if (!Docs.TryGetValue(side[s].GetProperty("characterId").GetString(), out doc[s])) return Err(404, "not_found");
            Match prior;
            if (Matches.TryGetValue(mid, out prior)) return (200, "{\"replayed\":true,\"status\":\"" + prior.Status + "\",\"a\":" + Side(doc[0]) + ",\"b\":" + Side(doc[1]) + "}"); // a replay: no session / rev checks, no kill switch
            if (!WagerEnabled) return Err(403, "wager_disabled");
            for (int s = 0; s < 2; s++)
            {
                if (doc[s].Session == null || doc[s].Session != side[s].GetProperty("sessionId").GetString()) return Err(409, "session_mismatch", -1, s == 0 ? "a" : "b");
                if (doc[s].Rev != side[s].GetProperty("rev").GetInt32()) return Err(409, "rev_conflict", doc[s].Rev, s == 0 ? "a" : "b");
            }
            var m = new Match { Id = mid, ExpiresAt = Now + HoldMs };
            var taken = new (Dictionary<string, int>, List<string>)[2];
            for (int s = 0; s < 2; s++)
            {
                var (cards, deck) = ReadState(side[s].GetProperty("state"));
                if (deck.Count != 8) return Err(422, "wager_deck");
                foreach (string id in deck)
                {
                    int n; cards.TryGetValue(id, out n);
                    if (n < 1) return Err(422, "wager_deck");
                    if (n == 1) cards.Remove(id); else cards[id] = n - 1;
                }
                taken[s] = (cards, deck);
                m.Char[s] = doc[s].Id; m.Deck[s] = deck.ToArray();
            }
            for (int s = 0; s < 2; s++) { doc[s].Cards = taken[s].Item1; doc[s].Deck = taken[s].Item2; doc[s].Rev++; }
            Matches[mid] = m;
            return (200, "{\"replayed\":false,\"status\":\"held\",\"a\":" + Side(doc[0]) + ",\"b\":" + Side(doc[1]) + "}");
        }
    }

    string Credited(Match m, string[][] plan, Doc[] doc)
    {
        var items = new List<string>();
        for (int s = 0; s < 2; s++)
            if (doc[s] != null) items.Add("{\"side\":\"" + (s == 0 ? "a" : "b") + "\",\"characterId\":\"" + doc[s].Id + "\",\"cards\":[" + string.Join(",", plan[s].Select(x => "\"" + x + "\"")) + "],\"rev\":" + doc[s].Rev + ",\"state\":" + StateJson(doc[s].Cards, doc[s].Deck) + "}");
        return string.Join(",", items);
    }

    public (int, string) Settle(string matchId, string body)
    {
        using (JsonDocument j = JsonDocument.Parse(body))
        {
            JsonElement r = j.RootElement;
            Match m;
            if (!Matches.TryGetValue(matchId, out m)) return Err(404, "not_found");
            string result = r.GetProperty("result").GetString();
            string pick = r.TryGetProperty("pick", out JsonElement pe) ? pe.GetString() : null;
            var doc = new Doc[2];
            for (int s = 0; s < 2; s++) Docs.TryGetValue(m.Char[s], out doc[s]);
            var plan = new string[2][];
            if (result == "draw") { if (pick != null) return Err(422, "wager_pick_invalid"); plan[0] = m.Deck[0]; plan[1] = m.Deck[1]; }
            else
            {
                int w = result == "a" ? 0 : 1, l = 1 - w;
                if (pick == null) return Err(422, "wager_pick_required");
                int at = Array.IndexOf(m.Deck[l], pick);
                if (at < 0) return Err(422, "wager_pick_invalid");
                plan[w] = m.Deck[w].Concat(new[] { pick }).ToArray();
                plan[l] = m.Deck[l].Where((_, k) => k != at).ToArray();
            }
            if (m.Status == "settled")
            {
                if (m.Result != result) return Err(409, "wager_already_settled");
                return (200, "{\"replayed\":true,\"status\":\"settled\",\"credited\":[" + Credited(m, plan, doc) + "]}");
            }
            if (m.Status != "held") return Err(409, "wager_refunded");
            // everything is checked before anything is written (one transaction)
            var base0 = new (Dictionary<string, int>, List<string>)[2];
            var sentSession = new string[2];
            for (int s = 0; s < 2; s++)
            {
                if (doc[s] == null) continue;
                string key = s == 0 ? "a" : "b";
                if (r.TryGetProperty(key, out JsonElement sup))
                {
                    if (doc[s].Session == null || doc[s].Session != sup.GetProperty("sessionId").GetString()) return Err(409, "session_mismatch", -1, key);
                    if (doc[s].Rev != sup.GetProperty("rev").GetInt32()) return Err(409, "rev_conflict", doc[s].Rev, key);
                    base0[s] = ReadState(sup.GetProperty("state"));
                    sentSession[s] = doc[s].Session;
                }
                else
                {
                    if (doc[s].Session != null) return Err(409, "receiver_online");
                    base0[s] = (new Dictionary<string, int>(doc[s].Cards), new List<string>(doc[s].Deck));
                }
            }
            for (int s = 0; s < 2; s++)
            {
                if (doc[s] == null) continue;
                doc[s].Cards = base0[s].Item1; doc[s].Deck = base0[s].Item2;
                foreach (string id in plan[s]) Add(doc[s].Cards, id);
                Trim(doc[s]);
                doc[s].Rev++;
            }
            m.Status = "settled"; m.Result = result;
            return (200, "{\"replayed\":false,\"status\":\"settled\",\"credited\":[" + Credited(m, plan, doc) + "]}");
        }
    }

    // ---- what the API does on its own ----

    /// <summary>The sweep / redeem refund: both sides get their own deck back when neither has a live session. A side whose character is gone is skipped.</summary>
    public bool Refund(string matchId, bool ignoreLive = false)
    {
        Match m;
        if (!Matches.TryGetValue(matchId, out m) || m.Status != "held") return false;
        var doc = new Doc[2];
        for (int s = 0; s < 2; s++) Docs.TryGetValue(m.Char[s], out doc[s]);
        if (!ignoreLive && doc.Any(d => d != null && d.Session != null)) return false;
        for (int s = 0; s < 2; s++)
        {
            if (doc[s] == null) continue;
            foreach (string id in m.Deck[s]) Add(doc[s].Cards, id);
            Trim(doc[s]);
            doc[s].Rev++;
        }
        m.Status = "refunded";
        return true;
    }

    public int SweepExpired()
    {
        int n = 0;
        foreach (Match m in Matches.Values.Where(x => x.Status == "held" && x.ExpiresAt <= Now).ToArray()) if (Refund(m.Id)) n++;
        return n;
    }

    /// <summary>A zone process restarted: its sessions are freed and its held matches refunded.</summary>
    public int ResetZone()
    {
        foreach (Doc d in Docs.Values) d.Session = null;
        int n = 0;
        foreach (Match m in Matches.Values.Where(x => x.Status == "held").ToArray()) if (Refund(m.Id)) n++;
        return n;
    }

    /// <summary>Deleting a character is refused while a match of it is held (409 wager_held).</summary>
    public bool Delete(string id)
    {
        if (Matches.Values.Any(m => m.Status == "held" && (m.Char[0] == id || m.Char[1] == id))) return false;
        if (Docs[id].Session != null) return false;
        Docs.Remove(id);
        return true;
    }
}

public enum Fault { None, LoseRequest, LoseResponse, Hold }

/// <summary>The network between the zone and the API: a call is answered on the next Pump (as ZoneApiHttp does), and a rule may lose the request, lose the answer after the API did it, or hold the answer back.</summary>
public sealed class FakeHttp
{
    readonly FakeApiServer server;
    readonly List<Action> inbox = new List<Action>();
    readonly List<Action> held = new List<Action>();
    int calls;
    /// <summary>(path, call number) -> what happens to the call.</summary>
    public Func<string, int, Fault> Rule = (p, n) => Fault.None;
    public readonly List<string> Log = new List<string>();

    public FakeHttp(FakeApiServer api) { server = api; }

    public void Post(string path, string json, Action<int, string> done)
    {
        Log.Add(path);
        Fault f = Rule(path, ++calls);
        if (f == Fault.LoseRequest) { inbox.Add(() => done(0, null)); return; }
        (int status, string text) = Route(path, json);
        if (f == Fault.LoseResponse) inbox.Add(() => done(0, null));
        else if (f == Fault.Hold) held.Add(() => done(status, text));
        else inbox.Add(() => done(status, text));
    }

    (int, string) Route(string path, string json)
    {
        string[] p = path.Split('/'); // "", internal, ...
        if (path == "/internal/card-wagers/escrow") return server.Escrow(json);
        if (p.Length == 5 && p[2] == "card-wagers" && p[4] == "settle") return server.Settle(Uri.UnescapeDataString(p[3]), json);
        if (p.Length == 5 && p[2] == "characters" && p[4] == "save") return server.Save(p[3], json);
        if (p.Length == 5 && p[2] == "sessions" && p[4] == "end") return server.End(p[3], json);
        throw new InvalidOperationException("fake API has no route " + path);
    }

    public void Pump()
    {
        Action[] now = inbox.ToArray();
        inbox.Clear();
        foreach (Action a in now) a();
    }

    /// <summary>Delivers the answers that were held back.</summary>
    public void ReleaseHeld() { inbox.AddRange(held); held.Clear(); }

    public int Count(string pathPart) { return Log.Count(x => x.Contains(pathPart)); }
}

/// <summary>The zone's side of the fake: IZoneApi over the fake HTTP. The wager calls are the real WagerApiClient (JSON in, JSON out).</summary>
public sealed class FakeZoneApi : IZoneApi
{
    readonly FakeHttp http;
    readonly WagerApiClient wagers;

    public FakeZoneApi(FakeHttp h) { http = h; wagers = new WagerApiClient(h.Post); }

    public void Save(string characterId, string sessionId, int rev, string stateJson, Action<ZoneApiReply> done)
    {
        http.Post("/internal/characters/" + characterId + "/save", "{\"sessionId\":\"" + sessionId + "\",\"rev\":" + rev + ",\"state\":" + stateJson + "}", (s, t) =>
        {
            ZoneApiReply r = ZoneApiReplyParser.Reply(s, t);
            if (r.Ok) r.Rev = ZoneApiReplyParser.IntAfter(t, "\"rev\":");
            done(r);
        });
    }

    public void EndSession(string characterId, string sessionId, Action<ZoneApiReply> done)
    {
        http.Post("/internal/sessions/" + sessionId + "/end", "{\"characterId\":\"" + characterId + "\"}", (s, t) => done(ZoneApiReplyParser.Reply(s, t)));
    }

    public void CommitTrade(string tradeId, TradeCommitSide a, TradeCommitSide b, Action<ZoneApiReply> done) { throw new NotSupportedException(); }
    public void Enhance(string characterId, string sessionId, int rev, string reqId, string itemUid, Action<ZoneApiReply> done) { throw new NotSupportedException(); }
    public void EscrowWager(string matchId, WagerSide a, WagerSide b, Action<ZoneApiReply> done) { wagers.Escrow(matchId, a, b, done); }
    public void SettleWager(string matchId, string result, string pick, WagerSide? a, WagerSide? b, Action<ZoneApiReply> done) { wagers.Settle(matchId, result, pick, a, b, done); }
}

/// <summary>One character as the zone holds it: its copy of the cards, which the queue captures for every write. Hero or connection do not exist here, which is the point: escrow and payout must not need them.</summary>
public sealed class FakePlayer : IStakeHolder
{
    public readonly string Id;
    public readonly Dictionary<string, int> Cards;
    public readonly List<string> Deck;
    public SavedCharacter Saved;
    public int Given, Taken;

    public FakePlayer(string id, Dictionary<string, int> cards, List<string> deck) { Id = id; Cards = new Dictionary<string, int>(cards); Deck = new List<string>(deck); }

    public string Capture() { return FakeApiServer.StateJson(Cards, Deck); }
    public int Total { get { return Cards.Values.Sum(); } }
    public void Drop(string id) { int n; Cards.TryGetValue(id, out n); Cards[id] = n + 1; }

    public List<string> TakeDeck(int day)
    {
        Taken++;
        foreach (string id in Deck.ToArray())
        {
            int n; Cards.TryGetValue(id, out n);
            if (n < 1) continue;
            if (n == 1) Cards.Remove(id); else Cards[id] = n - 1;
        }
        return new List<string>();
    }

    public void GiveCards(string[] cards, IList<string> lockedKinds, string[] deck)
    {
        Given++;
        foreach (string id in cards) { int n; Cards.TryGetValue(id, out n); Cards[id] = n + 1; }
        Deck.Clear(); Deck.AddRange(deck);
        var left = new Dictionary<string, int>(Cards);
        for (int i = 0; i < Deck.Count; i++)
        {
            int n; left.TryGetValue(Deck[i], out n);
            if (n < 1) { Deck.RemoveAt(i); i--; } else left[Deck[i]] = n - 1;
        }
    }
}

/// <summary>The API, the network, the real write queue and the players that are logged in.</summary>
public sealed class WagerWorld
{
    public readonly FakeApiServer Server = new FakeApiServer();
    public readonly FakeHttp Http;
    public readonly CharacterSaveQueue Queue;
    public readonly Dictionary<string, FakePlayer> Players = new Dictionary<string, FakePlayer>();
    public long Now = 1000;
    public readonly List<string> Lost = new List<string>();

    public WagerWorld(long periodicSaveMs = 1000)
    {
        Http = new FakeHttp(Server);
        Queue = new CharacterSaveQueue(new FakeZoneApi(Http), periodicSaveMs, "w");
        Queue.Lost += (c, code, state) => Lost.Add(c.Id + ":" + code);
        Queue.Tick(Now);
    }

    public static Dictionary<string, int> Own(string prefix, bool withSpare = true)
    {
        var d = new Dictionary<string, int>();
        for (int i = 1; i <= 8; i++) d[prefix + i] = 1;
        if (withSpare) d[prefix + "spare"] = 1;
        return d;
    }

    public static List<string> DeckOf(string prefix) { return Enumerable.Range(1, 8).Select(i => prefix + i).ToList(); }

    public FakePlayer Join(string id, string prefix)
    {
        Server.Add(id, Own(prefix), DeckOf(prefix));
        return Login(id, Own(prefix), DeckOf(prefix));
    }

    /// <summary>Logs the character in on this zone with the cards given (a new login loads whatever the API has).</summary>
    public FakePlayer Login(string id, Dictionary<string, int> cards, List<string> deck)
    {
        var p = new FakePlayer(id, cards, deck);
        string session = Server.Lock(id);
        p.Saved = Queue.Register(id, session, Server.Docs[id].Rev, p.Capture);
        Players[id] = p;
        return p;
    }

    public FakePlayer Relog(string id)
    {
        FakeApiServer.Doc d = Server.Docs[id];
        return Login(id, d.Cards, d.Deck);
    }

    /// <summary>Time passes: the queue ticks, the answers arrive, every 100 ms.</summary>
    public void Run(long ms)
    {
        for (long t = 0; t < ms; t += 100)
        {
            Now += 100; Server.Now = Now;
            Queue.Tick(Now);
            Http.Pump();
        }
    }

    public bool IsHere(string id) { return Queue.All.Any(c => c.Id == id); }

    /// <summary>The player closes the game: the last write, then the lock goes. Runs until it is done (or `ms` passes).</summary>
    public void Leave(string id, long ms = 20000)
    {
        Queue.Leave(Players[id].Saved);
        for (long t = 0; t < ms && IsHere(id); t += 100) Run(100);
    }

    /// <summary>What the player will really have: the zone's copy while online (its next save writes it), else what the API stores.</summary>
    public int Effective(string id) { return IsHere(id) ? Players[id].Total : Server.Docs.ContainsKey(id) ? Server.Docs[id].Cards.Values.Sum() : 0; }
}
