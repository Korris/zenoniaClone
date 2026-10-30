using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// ISkillBody over a HeroController (public API only). Cast lock, MP and cooldowns are HeroController.TryCastSkill's job, not the body's.
    /// </summary>
    public sealed class HeroSkillBody : ISkillBody
    {
        public const float BoxW = 10f, BoxH = 8f; // HeroController.BoxW / BoxH (feet-anchored hit box)
        static readonly Vector2[] DirVec = { new Vector2(0f, -1f), new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };
        readonly HeroController hero;

        public HeroSkillBody(HeroController hero) { this.hero = hero; }

        public HeroController Hero { get { return hero; } }
        public Vector2 Feet { get { return hero.Position; } }
        public int Facing { get { return hero.Facing; } }
        public ushort NetId { get { return hero.NetId; } }
        public string ClassId { get { return hero.Rpg != null ? hero.Rpg.ClassId : hero.Class.IconClass; } }
        public ClassPalette Palette { get { return hero.Class.ActivePalette; } }
        public bool IsDead { get { return hero == null || hero.IsDead; } }
        public bool IsLocal { get { return hero == LocalPlayer.Hero; } }
        public float Atk { get { return hero.AtkStat; } } // AtkStat already includes the buffs
        public BuffState Buffs { get { return HeroBuffs.For(hero); } }

        public static int FacingOf(Vector2 dir)
        {
            return Mathf.Abs(dir.x) >= Mathf.Abs(dir.y) ? (dir.x >= 0f ? 3 : 2) : (dir.y >= 0f ? 1 : 0);
        }

        public static Vector2 DirOf(int facing) { return DirVec[Mathf.Clamp(facing, 0, 3)]; }

        public void Face(Vector2 dir) { if (dir.sqrMagnitude > 0.0001f) hero.Teleport(hero.Position, FacingOf(dir)); }

        public bool TryBlink(Vector2 dest)
        {
            if (MapRegistry.Current.RectBlocked(dest.x - BoxW * 0.5f, dest.y, BoxW, BoxH)) return false;
            hero.Teleport(dest, hero.Facing);
            return true;
        }

        public void BeginForcedMove(Vector2 dir, float units, float ms) { SkillMotion.Begin(this, dir, units, ms); }

        public void AddInvuln(float ms) { hero.AddInvuln(ms); }

        public float Heal(float amount)
        {
            if (hero.IsDead || amount <= 0f) return 0f;
            float healed = Mathf.Min(hero.MaxHp - hero.Hp, amount);
            if (healed > 0.01f) hero.HealPulse(healed / hero.MaxHp);
            return Mathf.Max(0f, healed);
        }
    }
}
