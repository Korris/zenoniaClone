using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the compact player status and toasts (shoot with --ui-new hud, and --hud web|mobile for the size):
    ///   --hp-pct N               hero at N % HP: under 30 = the low-HP state (also read by DebugCapture.Pets.cs, there only next to a pet flag)
    ///   --mp-pct N               hero held at N % MP (regeneration would refill it before the shot): 0 = the grey MP bar
    ///   --status-demo shield     a real Shield buff on the hero (the blue rim on the HP bar, and its icon in the buff row)
    ///   --status-demo hit        the hero loses 142 HP every 0.8 s (refilled when low): any shot shows the bright lost part and "-142"
    ///   --buffs-demo             six sample buffs in the row (display only: BuffState holds one buff per kind, so six cannot be real)
    ///   --toast-demo levelup|item|line|full   that toast again every 2.6 s: LEVEL UP, "Obtained: ..." with an icon,
    ///                            a plain warning line, title + sub line with an icon
    ///   --levelup-at S           one level-up at S seconds through HeroFx.LevelUp (what a real one calls): shoot around it, e.g.
    ///                            --levelup-at 2 --shots 2.05,2.2,2.6,3.3,3.6 for the pop, the hold and the fade of the word
    /// Nothing here is saved: capture runs never write the player's files.
    /// </summary>
    public partial class DebugCapture
    {
        partial void DebugUiHudControls(); // phase 04: entry buttons, touch controls, hotbar (DebugCapture.UiHudControls.cs)

        partial void DebugUiHud()
        {
            DebugUiHudControls();
            HeroController hero = GameSession.Hero;
            PlayerState p = hero != null ? hero.Rpg : null;
            if (p == null) return;
            string hp = Val("--hp-pct"), mp = Val("--mp-pct");
            if (hp != null) p.Hp = Mathf.Max(1f, p.MaxHp * int.Parse(hp) / 100f);
            if (mp != null) StartCoroutine(HoldMp(p, int.Parse(mp) / 100f));
            string status = Val("--status-demo");
            if (status == "shield") HeroBuffs.For(hero).Add(BuffKind.Shield, p.MaxHp * 0.3f, 60000, 0, ShieldSource());
            else if (status == "hit") StartCoroutine(HitLoop(p));
            if (Flag("--buffs-demo")) HudBuffRow.DemoViews = SampleBuffs(p.ClassId);
            string toast = Val("--toast-demo");
            if (toast != null) StartCoroutine(ToastLoop(toast, p));
            string levelUpAt = Val("--levelup-at");
            if (levelUpAt != null) StartCoroutine(LevelUpAt(hero, float.Parse(levelUpAt, System.Globalization.CultureInfo.InvariantCulture)));
        }

        static IEnumerator LevelUpAt(HeroController hero, float seconds)
        {
            float start = Time.realtimeSinceStartup; // about the clock of --shots: both start once the hero has spawned
            while (Time.realtimeSinceStartup - start < seconds) yield return null;
            if (hero != null) HeroFx.LevelUp(hero.Position, hero.Rpg.Level);
        }

        /// <summary>The first skill of any class that gives a shield, so the buff row shows a real shield icon.</summary>
        static byte ShieldSource()
        {
            foreach (string cls in RpgClasses.Ids)
                foreach (SkillDef d in SkillTable.ForClass(cls))
                    foreach (SkillEffect e in d.Effects)
                        if (e.Buff.Kind == BuffKind.Shield) return BuffSource.Pack(d.Class, d.Index);
            return BuffSource.Pack(RpgClasses.Ids[0], 0);
        }

        /// <summary>Six running buffs with the seconds of the mockup's row (12s, 5s, 3s ...), wearing the class's own skill icons.</summary>
        static List<BuffView> SampleBuffs(string classId)
        {
            float[] left = { 12000f, 5000f, 3000f, 45000f, 20000f, 8000f };
            var list = new List<BuffView>(left.Length);
            for (int i = 0; i < left.Length; i++)
                list.Add(new BuffView { Kind = BuffKind.AtkPct, Source = BuffSource.Pack(classId, i), LeftMs = left[i], TotalMs = left[i] * 1.5f });
            return list;
        }

        static IEnumerator HoldMp(PlayerState p, float share)
        {
            while (true) { p.Mp = p.MaxMp * share; yield return null; }
        }

        static IEnumerator HitLoop(PlayerState p)
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(0.8f);
                p.Hp = p.Hp - 142f > p.MaxHp * 0.45f ? p.Hp - 142f : p.MaxHp;
            }
        }

        static IEnumerator ToastLoop(string kind, PlayerState p)
        {
            while (true)
            {
                if (kind == "levelup") RpgHud.LevelUp(p.Level);
                else if (kind == "item") RpgHud.Toast("Obtained: Enhance Stone x1", "", LootSprites.Stone());
                else if (kind == "full") RpgHud.Toast("Item Obtained!", "Small HP Potion x1", LootSprites.Potion(true));
                else RpgHud.Toast("Inventory full", new Color(1f, 0.55f, 0.45f));
                yield return new WaitForSecondsRealtime(2.6f);
            }
        }
    }
}
