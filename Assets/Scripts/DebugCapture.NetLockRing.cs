using System;
using System.Collections;
using System.Globalization;
using UnityEngine;

namespace ZenoniaProto
{
    public partial class DebugCapture
    {
        /// <summary>
        /// Persist role "lock-ring" (Tools/wanted-boss-crowd-playtest.ps1 -Role lock-ring): the window's hero holds Attack, so the lock-target slash
        /// walks it to the Wanted Boss and locks it (gold ring, soft collision). A shot every 0.5 s for 40 s into --wanted-shots, and a log line with
        /// the boss's feet and the hero's ground distance to them (the boss must not drift / shove; the hero must not stand in its foot core).
        /// </summary>
        IEnumerator LockRingShots()
        {
            string dir = NetLaunchArgs.Arg(Environment.GetCommandLineArgs(), "--wanted-shots") ?? "shots";
            System.IO.Directory.CreateDirectory(dir);
            ChatWindow.SetExpandedState(false);
            float start = Time.realtimeSinceStartup;
            while (Monster.Wanted == null && Time.realtimeSinceStartup - start < 20f) yield return null;
            P((Monster.Wanted != null ? "PASS" : "FAIL") + " lock-ring: wanted boss in sight");
            InputAdapter.BotPressed = key => key == GameKey.Attack;
            for (int k = 0; k < 80; k++)
            {
                yield return new WaitForSeconds(0.5f);
                Monster boss = Monster.Wanted;
                HeroController hero = GameSession.Hero;
                if (boss != null && hero != null)
                {
                    Vector2 o = hero.SimPosition - boss.Feet;
                    P("lock-ring t" + k + " boss " + boss.Feet.ToString("F1") + " ground " + Rpg.BodyCollisionRules.GroundDist(o.x, o.y).ToString("F1", CultureInfo.InvariantCulture)
                        + " contact " + boss.HeroContact.ToString("F1", CultureInfo.InvariantCulture) + " ring " + (TargetMarker.Target() == boss));
                }
                yield return Shot(dir, "lock-" + k.ToString("00"));
            }
            P("lock-ring done");
            while (true) yield return new WaitForSeconds(2f);
        }
    }
}
