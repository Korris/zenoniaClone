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

        /// <summary>--damage-numbers-demo ro: the 22/11 RO spec cases through the real own-hit path (DamageNumber.SpawnOwnHit) on three
        /// fake monsters left / centre / right of the hero: a single white hit, a 5-part multi-hit (white cluster + yellow TOTAL), a crit.</summary>
        static bool RoDemo()
        {
            int i = Array.IndexOf(Environment.GetCommandLineArgs(), "--damage-numbers-demo");
            string[] a = Environment.GetCommandLineArgs();
            return i >= 0 && i + 1 < a.Length && a[i + 1] == "ro";
        }

        IEnumerator DamageNumbersRoDemo()
        {
            object left = new object(), mid = new object(), right = new object();
            float d, iv;
            while (true)
            {
                HeroController hero = GameSession.Hero;
                if (hero != null)
                {
                    Vector2 f = hero.Position + new Vector2(0f, 30f);
                    DamageNumber.DebugForceParts = 1;
                    DamageNumber.SpawnOwnHit(f.x - 55f, f.y, 128, false, false, false, false, left, out d, out iv);   // single hit: one white number
                    DamageNumber.DebugForceParts = 5;
                    DamageNumber.SpawnOwnHit(f.x, f.y, 1240, false, false, false, false, mid, out d, out iv);         // multi-hit: 5 white + TOTAL 1240
                    DamageNumber.DebugForceParts = 1;
                    DamageNumber.SpawnOwnHit(f.x + 55f, f.y, 2873, true, false, false, false, right, out d, out iv);  // crit: red-orange on the burst
                    DamageNumber.DebugForceParts = 0;
                }
                yield return new WaitForSeconds(2f);
            }
        }

        IEnumerator DamageNumbersDemo()
        {
            if (RoDemo()) { yield return DamageNumbersRoDemo(); yield break; }
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
