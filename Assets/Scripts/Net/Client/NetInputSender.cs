using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
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
        readonly NetDataWriter hintW = new NetDataWriter();
        ushort hintSent;   // the aim hint the server has (0 = none)
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
            hintSent = 0; // the server clears its hint with the zone change
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
        public InputFrame Push(ushort bits) { return Push(bits, false, Vector2.zero); }

        /// <summary>Same, with the Ground skill point of the frame's skill press (protocol 38).</summary>
        public InputFrame Push(ushort bits, bool hasAim, Vector2 aim)
        {
            if (muted) return new InputFrame { Seq = redundancy.NextSeq, Bits = 0 }; // fading between maps: not a frame of either zone
            InputFrame f = redundancy.Push(bits, hasAim, aim.x, aim.y);
            if (Peer == null || Peer.ConnectionState != ConnectionState.Connected) return f;
            SendAimHint();
            int n = redundancy.Fill(frames);
            NetMessageWriter.Input(w, epoch, (ushort)Ack, frames, n);
            if (link != null) link.Send(Peer, w, DeliveryMethod.Unreliable, NetClient.ClientMs);
            else Peer.Send(w, NetProtocol.ChannelUnreliable, DeliveryMethod.Unreliable);
            stats.Add("inputPacketsSent", 1);
            return f;
        }

        /// <summary>Tells the server which monster the player clicked (ClickMove) when that changes, so its bolts aim where the client's do (reliable, ~5 B per change).</summary>
        void SendAimHint()
        {
            Monster m = ClickMove.Target;
            ushort id = m != null && !m.IsDying ? m.NetId : (ushort)0;
            if (id == hintSent) return;
            hintSent = id;
            new AimHintMsg { Epoch = epoch, MonsterId = id }.Write(hintW);
            Peer.Send(hintW, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
        }

        /// <summary>Every frame: releases delayed packets (simulator) and, when the hero does not predict, produces the frames itself.</summary>
        public void Tick(float dt, bool predicting)
        {
            if (link != null) link.Flush(Peer, NetClient.ClientMs);
            if (predicting || muted) return;
            sampler.Poll();
            timer += dt;
            int n = 0;
            while (timer >= HeroController.FixedDt && n++ < 4) { timer -= HeroController.FixedDt; ushort b = sampler.Take(); Push(b, sampler.LastHasAim, sampler.LastAim); }
            if (timer >= HeroController.FixedDt) timer = 0f;
        }
    }
}
