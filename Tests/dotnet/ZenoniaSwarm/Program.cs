using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using ZenoniaProto.Net;
using NetEvent = ZenoniaProto.Net.NetEvent;

/// <summary>
/// Scale smoke without Unity: --clients N light bots (hello, join, random walk + attack presses at 30 Hz, parse every snapshot / event) against a running
/// zone server; prints per-client bandwidth, snapshot sizes and rejections. Pair it with a headless server: its --net-stats gives the tick time.
///   dotnet run -c Release -- --host 127.0.0.1 --port 7790 --key k --clients 50 --seconds 40 [--stagger-ms 40] [--version N]
/// </summary>
static class Program
{
    sealed class Bot
    {
        public int Index;
        public NetManager Net;
        public NetPeer Peer;
        public bool Joined, Rejected;
        public string RejectText = "";
        public int SnapshotCount, SnapshotMax, HeroesMax, MonstersMax, EventBatches, Spawns, Despawns, RpgStates;
        public long SnapshotBytes, EventBytes;
        public readonly SnapshotMsg Snap = new SnapshotMsg();
        public readonly List<NetEvent> Events = new List<NetEvent>();
        public readonly InputRedundancy Redundancy = new InputRedundancy();
        public readonly InputFrame[] Frames = new InputFrame[NetProtocol.MaxInputFrames];
        public readonly NetDataWriter W = new NetDataWriter();
        public byte Held, AttackCnt;
        public double NextTurn, NextSend;
        public uint LastSnapSeq;
        public Stopwatch Connected = new Stopwatch();
    }

    static string Arg(string[] a, string k, string d) { int i = Array.IndexOf(a, k); return i >= 0 && i + 1 < a.Length ? a[i + 1] : d; }

    static int Main(string[] args)
    {
        string host = Arg(args, "--host", "127.0.0.1"), key = Arg(args, "--key", "k");
        int port = int.Parse(Arg(args, "--port", "7790")), n = int.Parse(Arg(args, "--clients", "50")), stagger = int.Parse(Arg(args, "--stagger-ms", "40"));
        double seconds = double.Parse(Arg(args, "--seconds", "40"), CultureInfo.InvariantCulture);
        int version = int.Parse(Arg(args, "--version", NetProtocol.Version.ToString()));
        var rng = new Random(1234);
        var bots = new List<Bot>();
        var clock = Stopwatch.StartNew();

        for (int i = 0; i < n; i++)
        {
            var b = new Bot { Index = i };
            var listener = new EventBasedNetListener();
            listener.PeerConnectedEvent += peer =>
            {
                b.Peer = peer; b.Connected.Restart();
                NetMessageWriter.Hello(b.W, new HelloMsg { Version = (ushort)version, Name = "Swarm" + b.Index, HeroClass = (byte)(b.Index % 5), Token = key });
                peer.Send(b.W, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
            };
            listener.PeerDisconnectedEvent += (peer, info) =>
            {
                RejectMsg rej; byte op;
                if (info.AdditionalData != null && info.AdditionalData.AvailableBytes > 0 && NetMessageReader.ReadOpcode(info.AdditionalData, out op) && op == NetOp.Reject && NetMessageReader.TryReject(info.AdditionalData, out rej))
                { b.Rejected = true; b.RejectText = rej.Reason + ": " + rej.Text; }
                b.Joined = false;
            };
            listener.NetworkReceiveEvent += (peer, r, ch, m) => OnReceive(b, r);
            b.Net = new NetManager(listener) { UpdateTime = 15, ChannelsCount = 3, AutoRecycle = true, EnableStatistics = true, IPv6Enabled = false, DisconnectTimeout = 8000 };
            b.Net.Start();
            b.Net.Connect(host, port, "zenonia-net");
            bots.Add(b);
            Pump(bots, rng, clock, stagger);
        }

        double end = clock.Elapsed.TotalSeconds + seconds;
        while (clock.Elapsed.TotalSeconds < end) Pump(bots, rng, clock, 15);

        int joined = 0, rejected = 0;
        double downSum = 0, upSum = 0, downMax = 0, upMax = 0, avgSnap = 0;
        int snapMax = 0, heroesMax = 0;
        foreach (Bot b in bots)
        {
            if (b.Rejected) rejected++;
            if (b.Peer == null || b.SnapshotCount == 0) continue;
            joined++;
            double secs = Math.Max(1.0, b.Connected.Elapsed.TotalSeconds);
            double down = b.Peer.Statistics.BytesReceived / 1024.0 / secs, up = b.Peer.Statistics.BytesSent / 1024.0 / secs;
            downSum += down; upSum += up; downMax = Math.Max(downMax, down); upMax = Math.Max(upMax, up);
            snapMax = Math.Max(snapMax, b.SnapshotMax); heroesMax = Math.Max(heroesMax, b.HeroesMax);
            avgSnap += b.SnapshotBytes / (double)Math.Max(1, b.SnapshotCount);
        }
        Console.WriteLine("swarm result: clients=" + n + " receivingSnapshots=" + joined + " rejected=" + rejected);
        if (joined > 0)
        {
            Console.WriteLine("  down KB/s per client: avg " + (downSum / joined).ToString("F2", CultureInfo.InvariantCulture) + " max " + downMax.ToString("F2", CultureInfo.InvariantCulture));
            Console.WriteLine("  up   KB/s per client: avg " + (upSum / joined).ToString("F2", CultureInfo.InvariantCulture) + " max " + upMax.ToString("F2", CultureInfo.InvariantCulture));
            Console.WriteLine("  snapshot bytes: avg " + (avgSnap / joined).ToString("F0", CultureInfo.InvariantCulture) + " max " + snapMax + " (cap " + NetProtocol.SnapshotMaxBytes + "), most heroes in one snapshot " + heroesMax);
        }
        foreach (Bot b in bots) if (b.Rejected) { Console.WriteLine("  rejected: bot " + b.Index + " -> " + b.RejectText); }
        foreach (Bot b in bots) b.Net.Stop();
        return 0;
    }

    static void Pump(List<Bot> bots, Random rng, Stopwatch clock, int sleepMs)
    {
        double now = clock.Elapsed.TotalSeconds;
        foreach (Bot b in bots)
        {
            b.Net.PollEvents();
            if (!b.Joined || b.Peer == null || now < b.NextSend) continue;
            b.NextSend = now + 1.0 / 30.0; // input at 30 Hz like the game client
            if (now >= b.NextTurn) // new random walk direction every 0.8 - 2 s, press attack now and then
            {
                b.NextTurn = now + 0.8 + rng.NextDouble() * 1.2;
                int d = rng.Next(0, 6);
                b.Held = d == 0 ? InputBits.Up : d == 1 ? InputBits.Down : d == 2 ? InputBits.Left : d == 3 ? InputBits.Right : (byte)0;
            }
            if (rng.NextDouble() < 0.08) b.AttackCnt++;
            b.Redundancy.Push(new InputFrame { Held = b.Held, AttackCnt = b.AttackCnt, Facing = 255 });
            int cnt = b.Redundancy.Fill(b.Frames);
            NetMessageWriter.Input(b.W, b.LastSnapSeq, b.Frames, cnt);
            b.Peer.Send(b.W, NetProtocol.ChannelUnreliable, DeliveryMethod.Unreliable);
        }
        Thread.Sleep(sleepMs);
    }

    static void OnReceive(Bot b, NetPacketReader r)
    {
        int size = r.AvailableBytes;
        byte op;
        if (!NetMessageReader.ReadOpcode(r, out op)) return;
        switch (op)
        {
            case NetOp.Welcome: b.Joined = true; break;
            case NetOp.Snapshot:
                if (!SnapshotPacker.TryRead(r, b.Snap)) { Console.WriteLine("bot " + b.Index + ": malformed snapshot"); break; }
                b.SnapshotCount++; b.SnapshotBytes += size; b.SnapshotMax = Math.Max(b.SnapshotMax, size);
                b.HeroesMax = Math.Max(b.HeroesMax, b.Snap.Heroes.Count); b.MonstersMax = Math.Max(b.MonstersMax, b.Snap.Monsters.Count);
                b.LastSnapSeq = b.Snap.Seq;
                break;
            case NetOp.EventBatch: b.EventBatches++; b.EventBytes += size; break;
            case NetOp.Spawn: b.Spawns++; break;
            case NetOp.Despawn: b.Despawns++; break;
            case NetOp.RpgState: b.RpgStates++; break;
        }
    }
}
