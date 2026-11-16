using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the compact HUD controls (shoot with --ui-new hud and --hud web|mobile):
    ///   --more-open              the strip of the "..." icon open (Gacha / Settings)
    ///   --badges-demo            every badge: a new-item dot on Bag, a quest ready to hand in ("!" on Quests); the count on Skill is the
    ///                            character's real free skill points (--level 60 has plenty)
    ///   --skill-cd-demo          slot 2 really cast again and again (its cooldown shows; give it targets with --dummies 3), MP held just
    ///                            under the cost of slot 3 (the "not enough MP" look)
    ///   --skill-slots-demo lock|empty   lock: only the first skill learned, the other magazine slots show the padlock;
    ///                            empty: a second skill learned but taken out of the magazine, so the free slots show "+"
    ///   --skill-page N           magazine page N (1-3)
    ///   --potion-demo empty|cd   no HP potions left / the cooldown look on the HP potion (display only: the game has no potion cooldown)
    ///   --exp-pct N              EXP at N % of the level (PC bar: 95 and up = gold); the purple MAX bar is --level 90
    ///   --hud-tap "1=Bag;2.5=More;3=Quests"   taps that control at that second through the EventSystem handlers a finger would reach
    ///                            (Bag, Skill, Pet, Quests, More, Gacha, Settings, Outside, Dash, Page on PC) and logs what is open afterwards
    ///   --hud-hold Attack        keeps that control pressed (pressed art): Attack, Bag, Skill, Pet ...
    /// Nothing here is saved: capture runs never write the player's files.
    /// </summary>
    public partial class DebugCapture
    {
        partial void DebugUiHudControls()
        {
            HeroController hero = GameSession.Hero;
            PlayerState p = hero != null ? hero.Rpg : null;
            if (p == null) return;
            string exp = Val("--exp-pct"), potion = Val("--potion-demo");
            if (exp != null) p.Exp = (int)(Leveling.ExpToNext(p.Level) * (long)int.Parse(exp) / 100);
            if (potion == "empty") { p.HpPotions = 0; p.HpPotionsMedium = 0; p.HpPotionsLarge = 0; }
            string slots = Val("--skill-slots-demo");
            if (slots != null) SlotsDemo(p, slots == "empty");
            if (Flag("--badges-demo")) BadgesDemo(p);
            if (Flag("--skill-cd-demo")) StartCoroutine(SkillCdDemo(hero, p));
            StartCoroutine(HudControlsWhenBuilt(potion));
        }

        /// <summary>Only skill 1 learned (the rest of the magazine = padlocks); `empty`: skill 2 learned too but taken out of the magazine ("+").</summary>
        static void SlotsDemo(PlayerState p, bool empty)
        {
            p.Skills.Reset();
            p.LearnSkill(0);
            if (!empty) return;
            for (int n = 0; n < 4; n++) p.LearnSkill(0); // ranks the tree may ask for before the next skill
            for (int i = 1; i < SkillDef.PerClass; i++) // the first other skill the tree lets this character learn
                if (p.LearnSkill(i) == LearnResult.Ok) { p.Loadout.Unassign(p.Loadout.SlotOf(i)); return; }
            Debug.LogWarning("[ZenoniaProto] --skill-slots-demo empty: no second skill could be learned");
        }

        static void BadgesDemo(PlayerState p)
        {
            NewItemFlags.Raise();
            QuestDef q = QuestCatalog.All[0];
            p.Quests.Accept(q);
            for (int i = 0; i < q.Count; i++) p.Quests.CountKill(q.MonsterType);
        }

        /// <summary>The paper controls are built on the first frame the switch is seen on: the flags that touch them wait for that.</summary>
        IEnumerator HudControlsWhenBuilt(string potion)
        {
            while (TouchControls.Paper == null) yield return null;
            PaperHudControls hud = TouchControls.Paper;
            string page = Val("--skill-page"), hold = Val("--hud-hold"), taps = Val("--hud-tap");
            if (page != null) for (int i = 1; i < int.Parse(page); i++) { TouchControls.CycleMagazine(1); yield return new WaitForSecondsRealtime(0.3f); }
            if (potion == "cd") hud.Potion(true).ShowCooldown(2.5f);
            if (Flag("--more-open")) hud.Entry.OpenMore();
            if (hold != null) { yield return null; Send(hud, hold, false); }
            if (taps == null) yield break;
            float start = Time.realtimeSinceStartup;
            foreach (string step in taps.Split(';'))
            {
                string[] kv = step.Split('=');
                float at = float.Parse(kv[0], CultureInfo.InvariantCulture);
                while (Time.realtimeSinceStartup - start < at) yield return null;
                bool hit = Send(hud, kv[1].Trim(), true);
                yield return null; yield return null; yield return null; // a pressed key is latched on the next frame, the window opens on the one after
                string open = "";
                foreach (UiScreen s in System.Enum.GetValues(typeof(UiScreen))) if (UiScreens.IsOpen(s)) open += s + " ";
                Debug.Log("[ZenoniaProto] hud-tap " + kv[1].Trim() + (hit ? "" : " (NO SUCH CONTROL)") + " at " + at + " => open: " + (open == "" ? "none " : open)
                    + "| quests " + QuestLogWindow.IsOpen + " settings " + SettingsWindow.IsOpen + " more " + hud.Entry.MoreOpen + " modal " + UiWindows.ModalOpen);
            }
        }

        /// <summary>Pointer down (+ up and click when `release`) on the control named `name`, through its own event handlers.</summary>
        static bool Send(PaperHudControls hud, string name, bool release)
        {
            Transform t = hud.Find(name);
            if (t == null || !t.gameObject.activeInHierarchy) return false;
            var e = new PointerEventData(EventSystem.current) { pointerId = -1, button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(t.gameObject, e, ExecuteEvents.pointerDownHandler);
            if (!release) return true;
            ExecuteEvents.Execute(t.gameObject, e, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(t.gameObject, e, ExecuteEvents.pointerClickHandler);
            return true;
        }

        static IEnumerator SkillCdDemo(HeroController hero, PlayerState p)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            while (hero != null)
            {
                int page = SkillMagazine.CurrentPage(hero.Class);
                SkillSlot second = SkillMagazine.Slot(hero.Class, page, 1), third = SkillMagazine.Slot(hero.Class, page, 2);
                SkillDef costly = SkillMagazine.Def(third);
                float low = costly != null ? Mathf.Max(0f, costly.MpAt(p.Skills[costly.Index]) - 1f) : 0f;
                if (second.IsSkill && hero.SkillCdLeftMs(second.SkillIndex) <= 0f) { p.Mp = p.MaxMp; Debug.Log("[ZenoniaProto] skill-cd-demo cast skill " + second.SkillIndex + " => " + hero.TryCastSkill(second.SkillIndex)); }
                for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime) { p.Mp = Mathf.Min(p.Mp, low); yield return null; }
            }
        }
    }
}
