using LiteNetLib;
using LiteNetLib.Utils;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// Upstream input stream: one frame per 1/30 s sim step, the last 5 frames repeated in every packet (16 B + transport header, ~0.6 KB/s) so a lost
    /// datagram never drops a key or a press. The frames come from the local hero's predictor; when prediction is off the sender steps by itself with the
    /// same sampler. Packets can go through the lag / loss simulator of the test harness.
    /// </summary>
    public sealed class NetInputSender
    {
        readonly InputRedundancy redundancy = new InputRedundancy();
        readonly InputFrame[] frames = new InputFrame[NetProtocol.MaxInputFrames];
        readonly NetDataWriter w = new NetDataWriter();
        readonly InputSampler sampler = new InputSampler();
        readonly NetStats stats;
        readonly NetLinkSimulator link;
        float timer;
        byte epoch;
        bool muted;

        /// <summary>Set by the client every frame: the connection and the newest snapshot sequence it decoded (low 16 bits go up as the ack).</summary>
        public NetPeer Peer;
        public uint Ack;

        /// <summary>The hero is in another zone now: from here on packets carry its epoch, none repeats a frame of the old map, and nothing is sent until Resume.</summary>
        public void BeginEpoch(byte newEpoch)
        {
            epoch = newEpoch;
            muted = true;
            timer = 0f;
            redundancy.Clear();
        }

        /// <summary>The new map is up: the controls count again.</summary>
        public void Resume() { muted = false; }

        public NetInputSender(NetStats netStats)
        {
            stats = netStats;
            if (NetLinkSimulator.Wanted) link = new NetLinkSimulator();
        }

        /// <summary>Stamps the sim step's input bits with a sequence number and sends the packet. Returns the stamped frame.</summary>
        public InputFrame Push(ushort bits)
        {
            if (muted) return new InputFrame { Seq = redundancy.NextSeq, Bits = 0 }; // fading between maps: not a frame of either zone
            InputFrame f = redundancy.Push(bits);
            if (Peer == null || Peer.ConnectionState != ConnectionState.Connected) return f;
            int n = redundancy.Fill(frames);
            NetMessageWriter.Input(w, epoch, (ushort)Ack, frames, n);
            if (link != null) link.Send(Peer, w, DeliveryMethod.Unreliable, NetClient.ClientMs);
            else Peer.Send(w, NetProtocol.ChannelUnreliable, DeliveryMethod.Unreliable);
            stats.Add("inputPacketsSent", 1);
            return f;
        }

        /// <summary>Every frame: releases delayed packets (simulator) and, when the hero does not predict, produces the frames itself.</summary>
        public void Tick(float dt, bool predicting)
        {
            if (link != null) link.Flush(Peer, NetClient.ClientMs);
            if (predicting || muted) return;
            sampler.Poll();
            timer += dt;
            int n = 0;
            while (timer >= HeroController.FixedDt && n++ < 4) { timer -= HeroController.FixedDt; Push(sampler.Take()); }
            if (timer >= HeroController.FixedDt) timer = 0f;
        }
    }
}
