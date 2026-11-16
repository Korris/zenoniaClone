using System.Collections;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the Adventure Paper boss bars (phase 03; shoot with --ui-new boss, usually with --god):
    ///   --boss-bar-demo map       the map boss near the hero at 59.9 % HP (stand beside it first: --tp 11,6 on Frost Hollow = Frostfang King)
    ///   --boss-bar-demo wanted    a Wanted Boss (Rimeclaw Tyrant) spawned beside the hero at 57 % HP: ribbon + RAGE gauge.
    ///                             Offline it is a plain local monster: no daily state, no reward; it fights and casts like the real one
    ///   --boss-bar-demo cast      the same, shown casting its first skill on a loop (top bar + the tab above its head)
    ///   --boss-bar-demo enrage    the same at 27 % HP: really enraged (Monster.WantedEnraged)
    ///   --boss-bar-demo defeated  the same, killed --boss-defeat-at seconds after the spawn (default 2.2): DEFEATED shows for 2 s after that
    ///   --boss-hp-pct N           overrides the HP share of the demo boss
    /// The states are forced for the run only; nothing is saved.
    /// </summary>
    public partial class DebugCapture
    {
        partial void DebugUiBoss()
        {
            string demo = Val("--boss-bar-demo");
            HeroController hero = GameSession.Hero;
            if (demo == null || hero == null) return;
            string pct = Val("--boss-hp-pct");
            Monster boss;
            if (demo == "map") boss = Monster.Boss;
            else
            {
                Vector2 at = FreeSpotNear(hero.Position, hero.Position + new Vector2(46f, 14f));
                boss = Monster.Create(GameObject.Find("ZenoniaProto").transform, MonsterDef.RimeclawTyrant, at, -1);
            }
            if (boss == null) { Debug.LogWarning("[ZenoniaProto] --boss-bar-demo " + demo + ": no boss on this map"); return; }
            float share = demo == "map" ? 0.599f : demo == "enrage" ? 0.27f : 0.57f;
            boss.DebugSetHpFraction(pct != null ? int.Parse(pct) / 100f : share);
            if (demo == "cast")
            {
                WantedSkillDef[] skills = WantedSkillTable.For(boss.Type);
                if (skills != null && skills.Length > 0) Monster.DebugCastDemo = skills[0];
            }
            if (demo == "defeated")
            {
                string at = Val("--boss-defeat-at");
                StartCoroutine(DefeatAt(boss, at != null ? float.Parse(at, System.Globalization.CultureInfo.InvariantCulture) : 2.2f));
            }
        }

        static IEnumerator DefeatAt(Monster boss, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (boss != null) boss.DebugSetHpFraction(0f);
        }
    }
}
