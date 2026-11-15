using System;
using System.Collections;
using UnityEngine;

namespace ZenoniaProto
{
    public partial class DebugCapture
    {
        /// <summary>
        /// --damage-numbers-demo (with --class): every 1.6 s spawns one of each floating-number kind around the hero, with the
        /// same arguments as the real callers (Monster.Visuals HitFx, HeroFx.Hurt / Heal / Potion, RpgSession EXP), so captures
        /// (--shots) show normal, finisher, crit, other-player, hero hurt, heal, potion and EXP numbers side by side.
        /// </summary>
        static bool DamageNumbersDemoRequested()
        {
            return Array.IndexOf(Environment.GetCommandLineArgs(), "--damage-numbers-demo") >= 0;
        }

        IEnumerator DamageNumbersDemo()
        {
            int wave = 0;
            while (true)
            {
                HeroController hero = GameSession.Hero;
                if (hero != null)
                {
                    Vector2 f = hero.Position;
                    int k = wave * 37;
                    DamageNumber.Spawn(f.x - 30f, f.y + 26f, (128 + k).ToString(), Color.white, false);                                 // normal hit
                    DamageNumber.Spawn(f.x + 34f, f.y + 34f, (1456 + k) + "!", new Color(1f, 0.85f, 0.2f), true, true);                 // crit
                    DamageNumber.Spawn(f.x - 24f, f.y + 10f, (311 + k).ToString(), new Color(1f, 0.69f, 0.23f), true);                   // combo finisher
                    DamageNumber.Spawn(f.x + 26f, f.y + 8f, (77 + k).ToString(), Color.white, false, false, true);                      // another player's hit
                    DamageNumber.Spawn(f.x, f.y + 26f, (42 + wave).ToString(), new Color(1f, 0.35f, 0.35f), false);                      // hero hurt
                    yield return new WaitForSeconds(0.25f);
                    f = hero.Position;
                    DamageNumber.Spawn(f.x - 22f, f.y + 44f, "+" + (250 + k), new Color(0.55f, 1f, 0.55f), true);                       // heal skill
                    DamageNumber.Spawn(f.x + 30f, f.y + 54f, "HP +30%", new Color(0.5f, 1f, 0.55f), false);                              // potion
                    DamageNumber.Spawn(f.x + 4f, f.y - 4f, "+" + (35 + wave) + " EXP", new Color(0.75f, 0.6f, 1f), false);              // exp
                }
                wave++;
                yield return new WaitForSeconds(1.35f);
            }
        }
    }
}
