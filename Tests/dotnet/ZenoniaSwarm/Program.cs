using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using ZenoniaProto.Net;
using NetEvent = ZenoniaProto.Net.NetEvent;

/// <summary>
/// Load test without Unity: N light protocol clients in one process against a running zone server. Each bot joins, sends one input frame per 1/30 s
/// (like the game client), decodes every delta snapshot against its own baseline ring and acks it, and runs a simple brain:
/// fighters walk to the nearest monster in their snapshots and press attack, wanderers walk randomly and swing now and then, idlers stand still.
///   dotnet run -c Release -- --host 127.0.0.1 --port 7790 --key k --clients 200 --seconds 600 [--stagger-ms 40] [--mix 50:30:20] [--stats out.json]
///        [--sim-latency MS --sim-jitter MS --sim-loss PCT] [--version N] [--hair vary|N] [--no-attack]
/// --tokens FILE: one zone Hello token per line (server-api tickets for a zone run with --api-url, Tools/wanted-boss-crowd-playtest.ps1); the bot count is the line count.
/// --goal X,Y: fighters attack the live monster nearest to that point (e.g. the wanted boss's spawn) and walk back to it when nothing lives within 260 units of it (a crowd waits for the boss to respawn instead of scattering after mobs).
/// --map ID (with --goal): walk back to the goal along the map's corridors; --free-ring ID ...: print spawn tiles around a point and exit (Program.Map.cs).
/// --no-attack: wanderers never swing (crowd look captures: Tools/net-hair-crowd-shots.ps1).
/// --hair: head layer style each bot asks for after joining (InvOp.HairStyle); vary (default) = (index / 5) % 5, so every class gets all 5 styles; N = every bot N (0 = the original heads).
/// </summary>
static partial class Program
{
    enum Brain { Fighter, Wanderer, Idler }

    sealed class Bot
    {
        public int Index;
        public byte Hair;
        public Brain Brain;
        public NetManager Net;
        public NetPeer Peer;
        public bool Joined, Rejected, EverJoined, Lost;
        public string RejectText = "";
        public ushort HeroId;
        public byte Epoch;
        public int ZoneChanges, StaleSnapshots;
        public int SnapshotCount, FullSnapshots, SnapshotMax, HeroesMax, MonstersMax, EventBatches, Spawns, Despawns, RpgStates, BaselineMisses, Malformed;
        public long SnapshotBytes;
        public readonly SnapshotMsg Snap = new SnapshotMsg();
        public readonly SnapshotRing Ring = new SnapshotRing();
        public readonly SnapshotLookup Lookup = new SnapshotLookup();
        public readonly InputRedundancy Redundancy = new InputRedundancy();
        public readonly InputFrame[] Frames = new InputFrame[NetProtocol.MaxInputFrames];
        public readonly NetDataWriter W = new NetDataWriter();
        public double NextSend, NextTurn, NextAttack;
        public ushort Move;
        public uint Ack;
        public float X, Y;
        public bool HasPos;
        public readonly List<MonsterSnap> Monsters = new List<MonsterSnap>();
        public Stopwatch Connected = new Stopwatch();
        public double DownKBps, UpKBps;
        public string Token;
        // party mode (Program.Party.cs)
        public ushort PlayerId;
        public string Name = "";
        public readonly Dictionary<ushort, string> HeroNames = new Dictionary<ushort, string>();
        public readonly Queue<byte> LearnQueue = new Queue<byte>();
        public double NextLearn, NextInvite, CastFrom, NextCastTry, NextHeal, NextBless, NextAegis;
        public int Mp, MaxMp, BuffCount;
        public float LeadDist = -1f;
        public readonly ushort[] Cds = new ushort[8];
        public byte MyHpPct = 100;
        public double NextPotion, StuckUntil, StuckCheck;
        public float StuckX, StuckY;
        public ushort Detour;
    }

    static bool noAttack, hasGoal;
    static readonly Random brainRng = new Random(4321);
    static float goalX, goalY;
    const float GoalRadius = 260f; // the boss chases its attackers: it may stand well off its spawn tile

    static string Arg(string[] a, string k, string d) { int i = Array.IndexOf(a, k); return i >= 0 && i + 1 < a.Length ? a[i + 1] : d; }
    static string F(double v, int d = 2) { return v.ToString("F" + d, CultureInfo.InvariantCulture); }

    static int Main(string[] args)
    {
        if (FreeRing(args)) return 0;
        string host = Arg(args, "--host", "127.0.0.1"), key = Arg(args, "--key", "k");
        int port = int.Parse(Arg(args, "--port", "7790")), n = int.Parse(Arg(args, "--clients", "50")), stagger = int.Parse(Arg(args, "--stagger-ms", "40"));
        double seconds = double.Parse(Arg(args, "--seconds", "40"), CultureInfo.InvariantCulture);
        int version = int.Parse(Arg(args, "--version", NetProtocol.Version.ToString()));
        int simLat = int.Parse(Arg(args, "--sim-latency", "0")), simJit = int.Parse(Arg(args, "--sim-jitter", "0")), simLoss = int.Parse(Arg(args, "--sim-loss", "0"));
        string statsPath = Arg(args, "--stats", null);
        int[] mix = Arg(args, "--mix", "50:30:20").Split(':').Select(int.Parse).ToArray();
        string hair = Arg(args, "--hair", "vary");
        noAttack = Array.IndexOf(args, "--no-attack") >= 0;
        ReadPartyArgs(args);
        string tokensPath = Arg(args, "--tokens", null);
        string[] tokens = tokensPath != null ? File.ReadAllLines(tokensPath).Where(l => l.Trim().Length > 0).Select(l => l.Trim()).ToArray() : null;
        if (tokens != null) n = tokens.Length;
        string goal = Arg(args, "--goal", null);
        if (goal != null) { string[] g = goal.Split(','); goalX = float.Parse(g[0], CultureInfo.InvariantCulture); goalY = float.Parse(g[1], CultureInfo.InvariantCulture); hasGoal = true; }
        ReadMapArgs(args);
        var rng = new Random(1234);
        var bots = new List<Bot>();
        var clock = Stopwatch.StartNew();

        for (int i = 0; i < n; i++)
        {
            int roll = i % 100;
            var b = new Bot { Index = i, Hair = (byte)(hair == "vary" ? (i / 5) % 5 : int.Parse(hair)), Brain = roll < mix[0] ? Brain.Fighter : roll < mix[0] + mix[1] ? Brain.Wanderer : Brain.Idler, Token = tokens != null ? tokens[i] : key };
            var listener = new EventBasedNetListener();
            listener.PeerConnectedEvent += peer =>
            {
                b.Peer = peer; b.Connected.Restart();
                NetMessageWriter.Hello(b.W, new HelloMsg { Version = (ushort)version, Name = "Swarm" + b.Index, HeroClass = (byte)(b.Index % 5), Token = b.Token });
                peer.Send(b.W, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
            };
            listener.PeerDisconnectedEvent += (peer, info) =>
            {
                RejectMsg rej; byte op;
                if (info.AdditionalData != null && info.AdditionalData.AvailableBytes > 0 && NetMessageReader.ReadOpcode(info.AdditionalData, out op) && op == NetOp.Reject && NetMessageReader.TryReject(info.AdditionalData, out rej))
                { b.Rejected = true; b.RejectText = rej.Reason + ": " + rej.Text; }
                else if (b.Joined) b.Lost = true;
                b.Joined = false;
            };
            listener.NetworkReceiveEvent += (peer, r, ch, m) => OnReceive(b, r);
            b.Net = new NetManager(listener) { UpdateTime = 15, ChannelsCount = 3, AutoRecycle = true, EnableStatistics = true, IPv6Enabled = false, DisconnectTimeout = 8000 };
            if (simLat > 0) { b.Net.SimulateLatency = true; b.Net.SimulationMinLatency = Math.Max(0, simLat - simJit); b.Net.SimulationMaxLatency = simLat + simJit + 1; }
            if (simLoss > 0) { b.Net.SimulatePacketLoss = true; b.Net.SimulationPacketLossChance = simLoss; }
            b.Net.Start();
            b.Net.Connect(host, port, "zenonia-net");
            bots.Add(b);
            if (InParty(b)) partyBotsList.Add(b);
            Pump(bots, rng, clock, stagger);
        }

        double end = clock.Elapsed.TotalSeconds + seconds;
        double nextReport = clock.Elapsed.TotalSeconds + 30;
        while (clock.Elapsed.TotalSeconds < end)
        {
            Pump(bots, rng, clock, 1);
            if (clock.Elapsed.TotalSeconds >= nextReport)
            {
                nextReport += 30;
                Console.WriteLine("[" + F(clock.Elapsed.TotalSeconds, 0) + " s] joined " + bots.Count(x => x.Joined) + "/" + n + " lost " + bots.Count(x => x.Lost) + (hasGoal ? GoalReport(bots) : "") + PartyReport());
            }
        }

        var good = bots.Where(b => b.Peer != null && b.SnapshotCount > 0).ToList();
        foreach (Bot b in good)
        {
            double secs = Math.Max(1.0, b.Connected.Elapsed.TotalSeconds);
            b.DownKBps = b.Peer.Statistics.BytesReceived / 1024.0 / secs; b.UpKBps = b.Peer.Statistics.BytesSent / 1024.0 / secs;
        }
        int rejected = bots.Count(b => b.Rejected), lost = bots.Count(b => b.Lost), stillJoined = bots.Count(b => b.Joined);
        Console.WriteLine("swarm result: clients=" + n + " receivingSnapshots=" + good.Count + " stillJoined=" + stillJoined + " lostConnection=" + lost + " rejected=" + rejected);
        string json = "{}";
        if (good.Count > 0)
        {
            var down = good.Select(b => b.DownKBps).OrderBy(v => v).ToList(); var up = good.Select(b => b.UpKBps).OrderBy(v => v).ToList();
            double avgSnap = good.Sum(b => b.SnapshotBytes) / (double)Math.Max(1, good.Sum(b => b.SnapshotCount));
            Console.WriteLine("  down KB/s per client: avg " + F(down.Average()) + " p95 " + F(down[(int)(down.Count * 0.95)]) + " max " + F(down.Last()));
            Console.WriteLine("  up   KB/s per client: avg " + F(up.Average()) + " p95 " + F(up[(int)(up.Count * 0.95)]) + " max " + F(up.Last()));
            Console.WriteLine("  snapshot bytes: avg " + F(avgSnap, 0) + " max " + good.Max(b => b.SnapshotMax) + " (cap " + NetProtocol.SnapshotMaxBytes + "), full snapshots " + good.Sum(b => b.FullSnapshots) + " of " + good.Sum(b => b.SnapshotCount)
                + ", baseline misses " + good.Sum(b => b.BaselineMisses) + ", malformed " + good.Sum(b => b.Malformed));
            Console.WriteLine("  most heroes in one snapshot " + good.Max(b => b.HeroesMax) + ", most monsters " + good.Max(b => b.MonstersMax));
            Console.WriteLine("  zone changes " + good.Sum(b => b.ZoneChanges) + ", snapshots of an earlier zone dropped " + good.Sum(b => b.StaleSnapshots));
            var sb = new StringBuilder();
            sb.Append("{\n  \"clients\": " + n + ", \"receivingSnapshots\": " + good.Count + ", \"stillJoined\": " + stillJoined + ", \"lostConnection\": " + lost + ", \"rejected\": " + rejected + ",\n");
            sb.Append("  \"downKBps\": { \"avg\": " + F(down.Average(), 3) + ", \"p95\": " + F(down[(int)(down.Count * 0.95)], 3) + ", \"max\": " + F(down.Last(), 3) + " },\n");
            sb.Append("  \"upKBps\": { \"avg\": " + F(up.Average(), 3) + ", \"p95\": " + F(up[(int)(up.Count * 0.95)], 3) + ", \"max\": " + F(up.Last(), 3) + " },\n");
            sb.Append("  \"snapshotAvgBytes\": " + F(avgSnap, 1) + ", \"snapshotMaxBytes\": " + good.Max(b => b.SnapshotMax) + ", \"snapshotsFull\": " + good.Sum(b => b.FullSnapshots) + ", \"snapshotsTotal\": " + good.Sum(b => b.SnapshotCount) + ",\n");
            sb.Append("  \"baselineMisses\": " + good.Sum(b => b.BaselineMisses) + ", \"malformed\": " + good.Sum(b => b.Malformed) + ", \"heroesInSnapshotMax\": " + good.Max(b => b.HeroesMax) + " }\n");
            json = sb.ToString();
        }
        foreach (Bot b in bots) if (b.Rejected) Console.WriteLine("  rejected: bot " + b.Index + " -> " + b.RejectText);
        if (statsPath != null) File.WriteAllText(statsPath, json);
        foreach (Bot b in bots) b.Net.Stop();
        return lost > 0 ? 3 : 0;
    }

    static void Pump(List<Bot> bots, Random rng, Stopwatch clock, int sleepMs)
    {
        double now = clock.Elapsed.TotalSeconds;
        foreach (Bot b in bots)
        {
            b.Net.PollEvents();
            if (!b.Joined || b.Peer == null) continue;
            if (b.NextSend == 0) b.NextSend = now;
            if (InParty(b))
            {
                if (b.CastFrom < 0 && b.LearnQueue.Count == 0) { var clerics = partyBotsList.Where(IsCleric).ToList(); b.CastFrom = now + 2.0 + CastEvery / clerics.Count * clerics.IndexOf(b); } // Clerics evenly staggered
                PartyChores(b, now);
            }
            for (int catchUp = 0; now >= b.NextSend && catchUp < 3; catchUp++) // input at 30 Hz like the game client, one frame per step
            {
                b.NextSend += 1.0 / 30.0;
                ushort bits = Think(b, rng, now);
                b.Redundancy.Push(bits);
                int cnt = b.Redundancy.Fill(b.Frames);
                NetMessageWriter.Input(b.W, b.Epoch, (ushort)b.Ack, b.Frames, cnt);
                b.Peer.Send(b.W, NetProtocol.ChannelUnreliable, DeliveryMethod.Unreliable);
            }
        }
        Thread.Sleep(sleepMs);
    }

    /// <summary>One input frame for the bot's brain.</summary>
    static ushort Think(Bot b, Random rng, double now)
    {
        ushort bits = 0;
        switch (b.Brain)
        {
            case Brain.Idler: return 0;
            case Brain.Wanderer:
                if (now >= b.NextTurn)
                {
                    b.NextTurn = now + 0.8 + rng.NextDouble() * 1.7;
                    int d = rng.Next(0, 6);
                    b.Move = d == 0 ? InputBits.Up : d == 1 ? InputBits.Down : d == 2 ? InputBits.Left : d == 3 ? InputBits.Right : (ushort)0;
                }
                bits = b.Move;
                if (!noAttack && rng.NextDouble() < 0.03) bits |= InputBits.Attack;
                return bits;
            default:
                if (InParty(b)) return PartyBits(b, now);
                if (hasGoal) // crowd mode: drink a large HP potion below 55 % (the boost gave 80), slide around walls on the way back after a respawn
                {
                    ushort extra = 0;
                    if (b.MyHpPct > 0 && b.MyHpPct < 55 && now >= b.NextPotion) { extra = InputBits.HpPotion; b.NextPotion = now + 1.2; }
                    if (now < b.StuckUntil) return (ushort)(b.Detour | extra);
                    ushort plan = FighterBits(b, now);
                    bool walking = (plan & (InputBits.Up | InputBits.Down | InputBits.Left | InputBits.Right)) != 0;
                    if (now >= b.StuckCheck)
                    {
                        bool stuck = walking && b.HasPos && Math.Abs(b.X - b.StuckX) + Math.Abs(b.Y - b.StuckY) < 4f;
                        b.StuckX = b.X; b.StuckY = b.Y; b.StuckCheck = now + 0.8;
                        if (stuck)
                        {
                            int d = rng.Next(0, 4);
                            b.Detour = (ushort)(1 << d);
                            b.StuckUntil = now + 0.6 + rng.NextDouble() * 1.4;
                            return (ushort)(b.Detour | extra);
                        }
                    }
                    return (ushort)(plan | extra);
                }
                return FighterBits(b, now);
        }
    }

    /// <summary>The fighter brain: walk to the target monster (with --goal: the one nearest the goal) and swing once in reach.</summary>
    static ushort FighterBits(Bot b, double now)
    {
                ushort bits = 0;
                if (hasGoal && b.HasPos && b.Monsters.Count == 0) return GoalStep(b);
                if (!b.HasPos || b.Monsters.Count == 0)
                {
                    if (now >= b.NextTurn) { b.NextTurn = now + 1.0 + brainRng.NextDouble(); int d = brainRng.Next(0, 4); b.Move = (ushort)(1 << d); }
                    return b.Move;
                }
                MonsterSnap best = default(MonsterSnap); float bd = float.MaxValue;
                foreach (MonsterSnap m in b.Monsters)
                {
                    float ox = hasGoal ? goalX : b.X, oy = hasGoal ? goalY : b.Y; // with --goal: the monster nearest to the goal (the boss), not to the bot
                    float dx = NetQuantize.Pos(m.X) - ox, dy = NetQuantize.Pos(m.Y) - oy, d2 = dx * dx + dy * dy;
                    if (m.DeathProgress == 0 && d2 < bd && (!hasGoal || d2 < GoalRadius * GoalRadius)) { bd = d2; best = m; }
                }
                if (bd == float.MaxValue) return hasGoal ? GoalStep(b) : b.Move;
                float tx = NetQuantize.Pos(best.X) - b.X, ty = NetQuantize.Pos(best.Y) - b.Y;
                bd = tx * tx + ty * ty; // distance from the bot itself decides walk or swing
                // Ranged classes (Wizard 1, Mechanic 2, Cleric 4: HeroClass = Index % 5) stop once the monster is inside their bolt range
                // (55-70 units, ComboConfigs) and shoot from there; melee walks up to 28 units.
                int cls = b.Index % 5;
                float reach = cls == 1 || cls == 2 || cls == 4 ? 45f : 28f;
                if (bd > reach * reach)
                {
                    if (tx > 3f) bits |= InputBits.Right; else if (tx < -3f) bits |= InputBits.Left;
                    if (ty > 3f) bits |= InputBits.Up; else if (ty < -3f) bits |= InputBits.Down;
                }
                else if (now >= b.NextAttack) { bits |= InputBits.Attack; b.NextAttack = now + 0.3; }
                return bits;
    }

    /// <summary>With --goal: HP % (snapshot byte, from the server) of the live monster nearest the goal as one bot sees it, and how many bots are within 260 units of the goal.</summary>
    static string GoalReport(List<Bot> bots)
    {
        int near = bots.Count(x => x.Joined && x.HasPos && (x.X - goalX) * (x.X - goalX) + (x.Y - goalY) * (x.Y - goalY) < GoalRadius * GoalRadius);
        string hp = "-";
        Bot seer = bots.FirstOrDefault(x => x.Joined && x.Monsters.Count > 0);
        if (seer != null)
        {
            float bd = float.MaxValue;
            foreach (MonsterSnap m in seer.Monsters)
            {
                float dx = NetQuantize.Pos(m.X) - goalX, dy = NetQuantize.Pos(m.Y) - goalY, d2 = dx * dx + dy * dy;
                if (m.DeathProgress == 0 && d2 < bd && d2 < GoalRadius * GoalRadius) { bd = d2; hp = m.HpPct + "%"; }
            }
        }
        return ", bots near the goal " + near + ", goal monster hp " + hp;
    }

    /// <summary>Direction bits toward an offset (3 units dead zone), no attack.</summary>
    static ushort StepTo(float tx, float ty)
    {
        ushort bits = 0;
        if (tx > 3f) bits |= InputBits.Right; else if (tx < -3f) bits |= InputBits.Left;
        if (ty > 3f) bits |= InputBits.Up; else if (ty < -3f) bits |= InputBits.Down;
        return bits;
    }

    static void OnReceive(Bot b, NetPacketReader r)
    {
        int size = r.AvailableBytes;
        byte op;
        if (!NetMessageReader.ReadOpcode(r, out op)) return;
        switch (op)
        {
            case NetOp.Welcome:
                WelcomeMsg wm;
                if (NetMessageReader.TryWelcome(r, out wm))
                {
                    b.HeroId = wm.HeroNetId; b.Epoch = wm.Epoch; b.Joined = b.EverJoined = true; b.PlayerId = wm.PlayerId;
                    if (InParty(b) && IsCleric(b)) b.CastFrom = -1;
                    OnPartyBotJoined(b);
                    if (b.Hair != 0) { NetMessageWriter.InvAction(b.W, new InvActionMsg { ReqId = 1, Op = InvOp.HairStyle, ItemUid = "", Slot = b.Hair }); b.Peer.Send(b.W, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered); }
                }
                break;
            case NetOp.ZoneChanged: // a wanderer walked through a portal: same rules as the game client, no fade to wait for
                ZoneChangedMsg zc;
                if (!NetMessageReader.TryZoneChanged(r, out zc)) { b.Malformed++; break; }
                b.Epoch = zc.Epoch; b.HeroId = zc.HeroNetId; b.Ack = 0; b.ZoneChanges++;
                b.Ring.Clear(); b.Redundancy.Clear(); b.Monsters.Clear(); b.HasPos = false;
                break;
            case NetOp.Snapshot:
                SnapshotReadResult res = SnapshotPacker.TryRead(r, b.Snap, b.Ring, b.Lookup, b.Epoch);
                if (res == SnapshotReadResult.StaleEpoch) { b.StaleSnapshots++; break; }
                if (res == SnapshotReadResult.MissingBaseline) { b.BaselineMisses++; break; }
                if (res != SnapshotReadResult.Ok) { b.Malformed++; break; }
                SnapshotPacker.Remember(b.Ring, b.Snap);
                if (b.Ack == 0 || NetQuantize.IsNewer(b.Snap.Seq, b.Ack)) b.Ack = b.Snap.Seq;
                b.SnapshotCount++; b.SnapshotBytes += size; b.SnapshotMax = Math.Max(b.SnapshotMax, size);
                if (b.Snap.BaselineSeq == 0) b.FullSnapshots++;
                b.HeroesMax = Math.Max(b.HeroesMax, b.Snap.Heroes.Count); b.MonstersMax = Math.Max(b.MonstersMax, b.Snap.Monsters.Count);
                b.HasPos = false;
                foreach (HeroSnap h in b.Snap.Heroes) if (h.Id == b.HeroId) { b.X = NetQuantize.Pos(h.X); b.Y = NetQuantize.Pos(h.Y); b.MyHpPct = h.HpPct; b.HasPos = true; break; }
                b.Monsters.Clear(); b.Monsters.AddRange(b.Snap.Monsters);
                break;
            case NetOp.EventBatch: b.EventBatches++; break;
            case NetOp.Spawn:
                b.Spawns++;
                SpawnMsg sp;
                if (partyWith != null && SpawnMsg.TryRead(r, out sp)) { OnSpawn(b, sp); if (sp.Kind == EntityKind.Hero && sp.Id == b.HeroId) b.Name = sp.Name; }
                break;
            case NetOp.PartyNotice:
                PartyNoticeMsg pn;
                if (partyWith != null && PartyNoticeMsg.TryRead(r, out pn)) OnPartyNotice(b, pn);
                break;
            case NetOp.SelfState:
                SelfStateMsg ss;
                if (partyWith != null && NetMessageReader.TrySelfState(r, out ss)) OnSelfState(b, ss);
                break;
            case NetOp.Despawn: b.Despawns++; break;
            case NetOp.RpgState: b.RpgStates++; break;
        }
    }
}
