using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Buff state per hero (BuffState) plus the tiny ticker that expires them. Lives as a child of the world root, so a rebuilt world starts clean.
    /// Phase 04 hooks: HeroController.AtkStat x AtkMul, attack speed x AtkSpeedMul, TakeDamage -> Absorb (see plan phase 04).
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
