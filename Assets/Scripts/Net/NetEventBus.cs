using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Server side collector of gameplay events (attack swings, bolts, hits, deaths, level-ups ...). Gameplay code calls the typed Emit helpers
    /// at the moment something visible happens; they cost one branch when no server runs (offline / client). ZoneServer drains Pending once per
    /// tick and routes each item: zone-wide, owner only, or to every client whose area of interest contains the event position.
    /// </summary>
    public static class NetEventBus
    {
        public struct Queued { public NetEvent Ev; public Vector2 At; public ushort OwnerHero; public bool ZoneWide; }

        /// <summary>True while a ZoneServer runs in this process (server / host).</summary>
        public static bool Active;
        public static readonly List<Queued> Pending = new List<Queued>(128);
        static ushort boltSeq;

        public static void ResetState() { Active = false; Pending.Clear(); boltSeq = 0; }

        static void Add(NetEvent e, Vector2 at, ushort owner = 0, bool zoneWide = false)
        {
            Pending.Add(new Queued { Ev = e, At = at, OwnerHero = owner, ZoneWide = zoneWide });
        }

        static ushort Clamp16(float v) { return (ushort)Mathf.Clamp(Mathf.RoundToInt(v), 0, 65535); }
        static sbyte Knock(float v) { return (sbyte)Mathf.Clamp(Mathf.RoundToInt(v * 0.5f), -127, 127); }

        /// <summary>A combo step started hitting (melee swing FX); echo = the second stab of a double stab.</summary>
        public static void AttackStep(HeroController h, int step, bool echo)
        {
            if (!Active) return;
            Add(new NetEvent { Type = NetEventType.AttackStep, Id = h.NetId, Step = (byte)(step | (echo ? 0x80 : 0)), Facing = (byte)h.Facing }, h.Position);
        }

        /// <summary>A ranged shot left the hero; returns the bolt id the matching BoltEnd carries (0 when no server).</summary>
        public static ushort BoltFired(HeroController h, int step, Vector2 pos, float angleDeg, bool finisher)
        {
            if (!Active) return 0;
            if (++boltSeq == 0) boltSeq = 1;
            Add(new NetEvent
            {
                Type = NetEventType.BoltFired, Id = boltSeq, Other = h.NetId, Step = (byte)step, X = NetQuantize.Pos(pos.x), Y = NetQuantize.Pos(pos.y),
                Angle = NetQuantize.Angle(angleDeg), Flags = (byte)(finisher ? 1 : 0),
            }, pos);
            return boltSeq;
        }

        /// <summary>A bolt ended: Flags bit0 = burst, bit1 = expired in the air (no impact picture).</summary>
        public static void BoltEnd(ushort id, Vector2 at, bool exploded, bool expired = false)
        {
            if (!Active || id == 0) return;
            Add(new NetEvent { Type = NetEventType.BoltEnd, Id = id, X = NetQuantize.Pos(at.x), Y = NetQuantize.Pos(at.y), Flags = (byte)((exploded ? 1 : 0) | (expired ? 2 : 0)) }, at);
        }

        public static void MonsterHit(Monster m, HeroController src, int dmg, Vector2 knock, bool crit, bool finisher, bool quiet)
        {
            if (!Active) return;
            byte flags = (byte)((crit ? HitFlags.Crit : 0) | (finisher ? HitFlags.Finisher : 0) | (quiet ? HitFlags.Quiet : 0));
            Add(new NetEvent
            {
                Type = NetEventType.MonsterHit, Id = m.NetId, Other = src != null ? src.NetId : (ushort)0, Dmg = Clamp16(dmg),
                HpLeft = Clamp16(Mathf.Ceil(Mathf.Max(0f, m.Hp))), Flags = flags, KnockX = Knock(knock.x), KnockY = Knock(knock.y),
            }, m.Feet);
        }

        public static void HeroHit(HeroController h, int dmg)
        {
            if (!Active) return;
            Add(new NetEvent { Type = NetEventType.HeroHit, Id = h.NetId, Dmg = Clamp16(dmg) }, h.Position);
        }

        /// <summary>Tree skill cast (see SkillCastCode): `salt` = the input step that pressed the key, shared by every side so rain drops land alike.</summary>
        public static void SkillCast(HeroController h, int index, int rank, int salt)
        {
            if (!Active) return;
            Vector2 at = h.Position;
            Add(new NetEvent
            {
                Type = NetEventType.SkillCast, Id = h.NetId, Step = SkillCastCode.Pack(index, rank), X = NetQuantize.Pos(at.x), Y = NetQuantize.Pos(at.y), Facing = (byte)h.Facing,
                Other = (ushort)(salt & 0xFFFF),
            }, at);
        }

        public static void HeroDied(HeroController h)
        {
            if (!Active) return;
            Add(new NetEvent { Type = NetEventType.HeroDied, Id = h.NetId, X = NetQuantize.Pos(h.Position.x), Y = NetQuantize.Pos(h.Position.y) }, h.Position);
        }

        public static void HeroRespawned(HeroController h)
        {
            if (!Active) return;
            Add(new NetEvent { Type = NetEventType.HeroRespawned, Id = h.NetId, X = NetQuantize.Pos(h.Position.x), Y = NetQuantize.Pos(h.Position.y) }, h.Position);
        }

        public static void ExpGain(HeroController h, int exp, int levels)
        {
            if (!Active) return;
            Add(new NetEvent { Type = NetEventType.ExpGain, Id = h.NetId, Value = (uint)Mathf.Max(0, exp), Step = (byte)Mathf.Clamp(levels, 0, 255) }, h.Position, h.NetId);
        }

        public static void LevelUp(HeroController h, int level)
        {
            if (!Active) return;
            Add(new NetEvent { Type = NetEventType.LevelUp, Id = h.NetId, Step = (byte)Mathf.Clamp(level, 0, 255) }, h.Position);
        }

        public static void Potion(HeroController h, bool hp)
        {
            if (!Active) return;
            Add(new NetEvent { Type = NetEventType.PotionUsed, Id = h.NetId, Step = (byte)(hp ? 1 : 0) }, h.Position);
        }

        public static void Appearance(HeroController h)
        {
            if (!Active) return;
            Add(new NetEvent { Type = NetEventType.AppearanceChanged, Id = h.NetId, Look = AppearanceOf(h) }, h.Position);
        }

        public static void Toast(HeroController h, string text, Color color)
        {
            if (!Active || h == null) return;
            Color32 c = color;
            Add(new NetEvent { Type = NetEventType.Toast, Text = text, Value = ((uint)c.r << 24) | ((uint)c.g << 16) | ((uint)c.b << 8) | c.a }, h.Position, h.NetId);
        }

        public static void BossSpawned(Monster m)
        {
            if (!Active) return;
            Add(new NetEvent { Type = NetEventType.BossSpawned, Id = m.NetId }, m.Feet, 0, true);
        }

        public static void BossDied(Monster m)
        {
            if (!Active) return;
            Add(new NetEvent { Type = NetEventType.BossDied, Id = m.NetId }, m.Feet, 0, true);
        }

        /// <summary>Paper-doll data of a hero: art tier + enhance level of the weapon / offhand (255 = nothing in that hand).</summary>
        public static NetAppearance AppearanceOf(HeroController h)
        {
            PlayerState p = h.Rpg;
            Item w = p.GetEquipped(Slots.Weapon), o = p.GetEquipped(Slots.Offhand);
            return new NetAppearance
            {
                WeaponTier = w != null ? (byte)ItemInfo.ArtTier(w) : (byte)255,
                OffTier = o != null ? (byte)ItemInfo.ArtTier(o) : (byte)255,
                EnhanceBand = (byte)(Mathf.Clamp(w != null ? w.enhance : 0, 0, 15) | (Mathf.Clamp(o != null ? o.enhance : 0, 0, 15) << 4)),
                Flags = (byte)(h.Class.Id == HeroClass.Mechanic && ClassPalette.MechanicBlue ? 1 : 0),
            };
        }

        /// <summary>Stub equipment items for HeroRenderer.SetEquipmentGear built from replicated appearance data (null = empty hand).</summary>
        public static void ItemsFromAppearance(NetAppearance a, string classId, out Item weapon, out Item offhand)
        {
            weapon = a.WeaponTier == 255 ? null : new Item { slot = Slots.Weapon, classId = classId, itemLevel = a.WeaponTier * 10, enhance = a.EnhanceBand & 15, rarity = "common" };
            offhand = a.OffTier == 255 ? null : new Item { slot = Slots.Offhand, classId = classId, itemLevel = a.OffTier * 10, enhance = a.EnhanceBand >> 4, rarity = "common" };
        }
    }
}
