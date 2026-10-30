using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Buff state per hero (BuffState) plus the tiny ticker that expires them. Lives as a child of the world root, so a rebuilt world starts clean.
    /// HeroController hooks: AtkStat x AtkMul, combo speed x AtkSpeedMul, TakeDamage -> Absorb; OwnerSim carries the attack-speed buff to the predicting owner.
    /// </summary>
    public sealed class HeroBuffs : MonoBehaviour
    {
        static readonly Dictionary<HeroController, BuffState> States = new Dictionary<HeroController, BuffState>();
        static HeroBuffs ticker;
        static readonly List<HeroController> Dead = new List<HeroController>();

        public static BuffState For(HeroController hero)
        {
            if (hero == null) return new BuffState();
            if (ticker == null) { States.Clear(); ticker = new GameObject("HeroBuffs").AddComponent<HeroBuffs>(); ticker.transform.SetParent(hero.transform.parent, false); }
            BuffState s;
            if (!States.TryGetValue(hero, out s)) { s = new BuffState(); States[hero] = s; }
            return s;
        }

        /// <summary>Shield-aware damage for a hero (phase 04 calls it from TakeDamage): what is left after the shield soaked its share.</summary>
        public static int Absorb(HeroController hero, int dmg)
        {
            BuffState s;
            return hero != null && States.TryGetValue(hero, out s) && s.Any ? Mathf.RoundToInt(s.Absorb(dmg)) : dmg;
        }

        /// <summary>Damage multiplier of the hero's next hit from NextHitsPct buffs (1 = none); spends a charge.</summary>
        public static float ConsumeNextHit(HeroController hero) { BuffState s; return hero != null && States.TryGetValue(hero, out s) && s.Any ? s.ConsumeNextHit() : 1f; }

        public static float AtkMul(HeroController hero) { BuffState s; return hero != null && States.TryGetValue(hero, out s) ? s.AtkMul : 1f; }
        public static float AtkSpeedMul(HeroController hero) { BuffState s; return hero != null && States.TryGetValue(hero, out s) ? s.AtkSpeedMul : 1f; }

        /// <summary>Attack-speed buff of a hero for the owner sim block: percent and remaining ms (0, 0 when none). Does not create state.</summary>
        public static void AtkSpeedOf(HeroController hero, out float pct, out float leftMs)
        {
            BuffState s;
            pct = leftMs = 0f;
            if (hero == null || !States.TryGetValue(hero, out s)) return;
            pct = s.ValueOf(BuffKind.AtkSpeedPct); leftMs = s.LeftMs(BuffKind.AtkSpeedPct);
        }

        /// <summary>Predicting client: adopts the server's attack-speed buff so replayed combo steps run at the same speed.</summary>
        public static void SetAtkSpeed(HeroController hero, float pct, float leftMs)
        {
            if (leftMs <= 0f && !States.ContainsKey(hero)) return;
            For(hero).Set(BuffKind.AtkSpeedPct, pct, leftMs);
        }

        void Update()
        {
            float dt = GameClock.Dt * 1000f;
            if (dt <= 0f) return;
            Dead.Clear();
            foreach (KeyValuePair<HeroController, BuffState> kv in States)
            {
                if (kv.Key == null) Dead.Add(kv.Key); else kv.Value.Tick(dt);
            }
            foreach (HeroController h in Dead) States.Remove(h);
        }
    }
}
