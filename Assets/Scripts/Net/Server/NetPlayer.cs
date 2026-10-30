using LiteNetLib;
using UnityEngine;
using ZenoniaProto.Net;
using NetEvent = ZenoniaProto.Net.NetEvent;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>One connected player on the server: the peer, its hero + RPG state, what it can see (InterestSet) and its send bookkeeping.</summary>
    public sealed class NetPlayer
    {
        public NetPeer Peer;
        public ushort PlayerId;
        public string Name;
        public HeroController Hero;
        public PlayerRpg Rpg;
        public readonly NetInputDriver Input = new NetInputDriver();
        public readonly InterestSet Interest = new InterestSet();
        public readonly System.Collections.Generic.List<NetEvent> OutEvents = new System.Collections.Generic.List<NetEvent>(32);
        public int HeroCursor, MonsterCursor;
        public uint SnapSeq, AckSnapSeq;
        public bool RpgDirty;
        public float LastRpgSentMs, LastSelfSentMs;
        public SelfStateMsg LastSelf;
        public float RateWindowStartMs;
        public int PacketsInWindow;
        public float ConnectedAtRealtime;

        public bool Alive { get { return Hero != null; } }

        /// <summary>Wires the RPG events of this player to network events (EXP, level-up ring, pickup / death toasts) and the dirty flag for RpgState pushes.</summary>
        public void BindRpgEvents()
        {
            Rpg.Changed += () => RpgDirty = true;
            Rpg.ExpGained += (exp, levels, at) =>
            {
                NetEventBus.ExpGain(Hero, exp, levels);
                if (levels > 0) NetEventBus.LevelUp(Hero, Rpg.State.Level);
            };
            Rpg.LootPicked += (e, r) =>
            {
                if (!r.Ok) { if (e.Kind == LootKind.Equipment) NetEventBus.Toast(Hero, "Inventory full", new Color(1f, 0.55f, 0.45f)); return; }
                if (!string.IsNullOrEmpty(r.Text)) NetEventBus.Toast(Hero, r.Text, e.Kind == LootKind.Equipment ? LootSprites.RarityColor(e.Item) : new Color(1f, 0.9f, 0.5f));
            };
            Rpg.Revived += lost => { if (lost > 0) NetEventBus.Toast(Hero, "You were defeated: -" + lost + " EXP", new Color(1f, 0.5f, 0.45f)); };
        }
    }
}
