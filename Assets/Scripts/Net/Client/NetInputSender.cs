using LiteNetLib;
using LiteNetLib.Utils;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// Turns the local controls (keyboard, touch buttons or a scripted bot, all through InputAdapter) into the 30 Hz input stream: held keys as a
    /// bitmask, one-shot presses as wrapping counters, the last 3 frames repeated in every packet so a lost datagram never drops a press.
    /// With the bag open or no hero the stream carries zero input (the hero stands still).
    /// </summary>
    public sealed class NetInputSender
    {
        const float SendInterval = 1f / 30f;
        readonly InputRedundancy redundancy = new InputRedundancy();
        readonly InputFrame[] frames = new InputFrame[NetProtocol.MaxInputFrames];
        readonly NetDataWriter w = new NetDataWriter();
        readonly NetStats stats;
        byte attackCnt, dashCnt, skillCnt, hpCnt, mpCnt;
        float timer, sinceSend;
        InputFrame lastSent;
        bool hasLast;
        const float KeepAliveInterval = 0.1f; // an unchanged input is repeated at 10 Hz (the server releases keys after 250 ms of silence)

        public NetInputSender(NetStats netStats) { stats = netStats; }

        static bool Same(InputFrame a, InputFrame b)
        {
            return a.Held == b.Held && a.AttackCnt == b.AttackCnt && a.DashCnt == b.DashCnt && a.SkillCnt == b.SkillCnt && a.HpPotCnt == b.HpPotCnt && a.MpPotCnt == b.MpPotCnt;
        }

        /// <summary>Called every frame: counts presses as they happen, sends a packet when the 30 Hz timer is due.</summary>
        public void Tick(float dt, NetPeer server, uint ackSnapshotSeq)
        {
            bool live = LocalPlayer.Hero != null && !InventoryPanel.IsOpen;
            if (live)
            {
                if (InputAdapter.Pressed(GameKey.Attack)) attackCnt++;
                if (InputAdapter.Pressed(GameKey.Dash)) dashCnt++;
                if (InputAdapter.Pressed(GameKey.Skill)) skillCnt++;
                if (InputAdapter.Pressed(GameKey.HpPotion)) hpCnt++;
                if (InputAdapter.Pressed(GameKey.MpPotion)) mpCnt++;
            }
            timer += dt;
            if (timer < SendInterval) return;
            timer -= SendInterval;
            if (timer > SendInterval) timer = 0f; // a long frame hitch: do not burst-send
            var f = new InputFrame { AttackCnt = attackCnt, DashCnt = dashCnt, SkillCnt = skillCnt, HpPotCnt = hpCnt, MpPotCnt = mpCnt, Facing = 255 };
            if (live)
            {
                if (InputAdapter.Held(GameKey.Up)) f.Held |= InputBits.Up;
                if (InputAdapter.Held(GameKey.Down)) f.Held |= InputBits.Down;
                if (InputAdapter.Held(GameKey.Left)) f.Held |= InputBits.Left;
                if (InputAdapter.Held(GameKey.Right)) f.Held |= InputBits.Right;
                if (InputAdapter.Held(GameKey.Attack)) f.Held |= InputBits.Attack;
                if (InputAdapter.Held(GameKey.Dash)) f.Held |= InputBits.Dash;
                if (InputAdapter.Held(GameKey.Skill)) f.Held |= InputBits.Skill;
            }
            sinceSend += SendInterval;
            if (hasLast && sinceSend < KeepAliveInterval && Same(f, lastSent)) return; // nothing new: save the bandwidth
            sinceSend = 0f;
            lastSent = f; hasLast = true;
            redundancy.Push(f);
            int n = redundancy.Fill(frames);
            NetMessageWriter.Input(w, ackSnapshotSeq, frames, n);
            if (server != null && server.ConnectionState == ConnectionState.Connected) { server.Send(w, NetProtocol.ChannelUnreliable, DeliveryMethod.Unreliable); stats.Add("inputPacketsSent", 1); }
        }
    }
}
