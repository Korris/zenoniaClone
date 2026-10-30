using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ZenoniaProto
{
    /// <summary>
    /// Test harness: simulated latency, jitter and loss for the UPSTREAM packets of a client (LiteNetLib's own simulation only touches what a manager
    /// receives, i.e. the downstream on a client). `--sim-latency 50 --sim-jitter 10 --sim-loss 2` = 40..60 ms each way, 2 % lost each way.
    /// </summary>
    public sealed class NetLinkSimulator
    {
        struct Pending { public byte[] Data; public int Length; public double DueMs; public DeliveryMethod Method; }

        readonly int minMs, maxMs, lossPct;
        readonly System.Random rng = new System.Random(NetLaunchArgs.SimSeed * 7919 + System.Environment.TickCount); // every bot process gets its own loss pattern
        readonly List<Pending> pending = new List<Pending>();

        public static bool Wanted { get { return NetLaunchArgs.SimLatencyMs > 0 || NetLaunchArgs.SimLossPct > 0; } }

        public NetLinkSimulator()
        {
            minMs = System.Math.Max(0, NetLaunchArgs.SimLatencyMs - NetLaunchArgs.SimJitterMs);
            maxMs = NetLaunchArgs.SimLatencyMs + NetLaunchArgs.SimJitterMs + 1;
            lossPct = NetLaunchArgs.SimLossPct;
        }

        /// <summary>Applies the downstream half of the simulation to a client manager (LiteNetLib keeps this in the receive path).</summary>
        public static void ConfigureReceive(NetManager net)
        {
            if (!Wanted) return;
            int lat = NetLaunchArgs.SimLatencyMs, jit = NetLaunchArgs.SimJitterMs;
            net.SimulateLatency = lat > 0;
            net.SimulationMinLatency = System.Math.Max(0, lat - jit);
            net.SimulationMaxLatency = lat + jit + 1;
            net.SimulatePacketLoss = NetLaunchArgs.SimLossPct > 0;
            net.SimulationPacketLossChance = NetLaunchArgs.SimLossPct;
        }

        public void Send(NetPeer peer, NetDataWriter w, DeliveryMethod method, double nowMs)
        {
            if (lossPct > 0 && rng.NextDouble() * 100.0 < lossPct) return;
            if (maxMs <= 1) { peer.Send(w, method); return; }
            var p = new Pending { Data = new byte[w.Length], Length = w.Length, DueMs = nowMs + rng.Next(minMs, maxMs), Method = method };
            System.Buffer.BlockCopy(w.Data, 0, p.Data, 0, w.Length);
            pending.Add(p);
        }

        /// <summary>Sends everything whose delay has passed (call every frame).</summary>
        public void Flush(NetPeer peer, double nowMs)
        {
            for (int i = 0; i < pending.Count; i++)
            {
                if (pending[i].DueMs > nowMs) continue;
                if (peer != null && peer.ConnectionState == ConnectionState.Connected) peer.Send(pending[i].Data, 0, pending[i].Length, pending[i].Method);
                pending.RemoveAt(i--);
            }
        }
    }
}
