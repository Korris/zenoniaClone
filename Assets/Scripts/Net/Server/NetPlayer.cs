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
        /// <summary>Zone changes so far (wraps): stamped on snapshots, required on input packets, so neither side acts on a packet of the zone before.</summary>
        public byte Epoch;
        /// <summary>When this hero may use a portal, and which bosses it has killed (a boss gate is per hero; lost with the session until accounts are wired).</summary>
        public readonly PortalGate Portals = new PortalGate();
        public readonly BossProgress Bosses = new BossProgress();
        /// <summary>Snapshots as sent to this client (delta baselines) and the id lookup of the baseline in use.</summary>
        public readonly SnapshotRing SentRing = new SnapshotRing();
        public readonly SnapshotLookup BaselineLookup = new SnapshotLookup();
        public bool RpgDirty;
        public float LastRpgSentMs, LastSelfSentMs;
        public SelfStateMsg LastSelf;
        public float RateWindowStartMs;
        public int PacketsInWindow;
        public float ConnectedAtRealtime;

        public bool Alive { get { return Hero != null; } }

        /// <summary>Folds the 16-bit snapshot ack of an input packet into the 32-bit sequence; acks from the future or older than the current one are ignored.</summary>
        public void AcceptAck(ushort ack16)
        {
            ushort back = (ushort)(SnapSeq - ack16);
            if (back > SnapSeq) return; // claims a snapshot that was never sent
            uint ack = SnapSeq - back;
            if (ack != 0 && !NetQuantize.IsNewer(ack, SnapSeq) && (AckSnapSeq == 0 || NetQuantize.IsNewer(ack, AckSnapSeq))) AckSnapSeq = ack;
        }

        /// <summary>
        /// The hero just entered another zone: what this client knew of the old one is void. Entity ids are per zone, so the visible set and every delta baseline start over (the sequence
        /// numbers go on), and the input queued so far was pressed on the old map.
        /// </summary>
        public void OnZoneTransfer()
        {
            Epoch++;
            Interest.Clear();
            OutEvents.Clear();
            SentRing.Clear();
            AckSnapSeq = 0;
            HeroCursor = MonsterCursor = 0;
            Input.State.RestartStream();
            Portals.Disarm();
        }

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
            Rpg.BossKilled += OnBossKilled;
        }

        /// <summary>Same rule as RpgSession offline, per hero: this hero hit the boss, so the gate is open for it; the client learns from the next RpgState (pushed at once).</summary>
        void OnBossKilled()
        {
            string map = Hero != null && Hero.HomeZone != null && Hero.HomeZone.Map != null ? Hero.HomeZone.Map.Id : null;
            if (map == null || !Bosses.Mark(map, MapSpawnTable.HasOwnBoss(map))) return;
            RpgDirty = true;
            LastRpgSentMs = -10000f;
            if (MapSpawnTable.HasBossGate(map)) NetEventBus.Toast(Hero, "The way forward is open", new Color(0.5f, 0.95f, 1f));
        }
    }
}
