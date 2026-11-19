using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the RO Soft HUD follow-up (portraits, PC layout, clicks):
    ///   --look S,H,O                  the local hero's look right after spawning: hairstyle S, hair colour H, outfit colour O (palette indexes)
    ///   --look-at "3=S,H,O,HAT;6=..." changes the look (and the worn hat id, 0 = none) at those seconds: the HUD portrait must follow
    ///   --ro-click "1=Bag;2=Close"    at each second: a pointer click at the screen centre of the RO HUD control with that name, through the
    ///                                 EventSystem raycast a mouse would hit (logs the object hit), then the state after it
    ///                                 (open screens, More, Auto, skill page, party fold, chat, minimap zoom, quests, settings)
    ///   --ro-poison S                 at S seconds the local hero is poisoned for 12 s (offline: its own status; the HUD buff row shows it)
    ///   --ro-fallen S                 at S seconds the Wanted kill toast ("The Mire Hydra Queen has fallen!", the call NetEventPlayer makes)
    ///   --ro-digit "2=1;3=5"          at each second: the digit key of that loadout slot (1-8) counts as pressed for one frame (PC loadout keys)
    /// Nothing is saved.
    /// </summary>
    public partial class DebugCapture
    {
        partial void DebugRoHud()
        {
            HeroController hero = GameSession.Hero;
            string crop = Val("--portrait-crop"); // SHOULDERS,MINTOP,MAXTOP: the HUD portraits' bust crop in world units above the feet (tuning)
            if (crop != null)
            {
                string[] c = crop.Split(',');
                RoHudPortraits.SetCrop(float.Parse(c[0], CultureInfo.InvariantCulture), float.Parse(c[1], CultureInfo.InvariantCulture), float.Parse(c[2], CultureInfo.InvariantCulture));
            }
            string look = Val("--look");
            if (hero != null && look != null) ApplyLook(hero, look);
            string at = Val("--look-at"), clicks = Val("--ro-click"), digits = Val("--ro-digit");
            if (at != null) StartCoroutine(Timed(at, v => { if (GameSession.Hero != null) ApplyLook(GameSession.Hero, v); }));
            if (clicks != null) StartCoroutine(Timed(clicks, RoClick));
            if (digits != null) StartCoroutine(Timed(digits, v => StartCoroutine(Digit(int.Parse(v) - 1))));
            string poison = Val("--ro-poison"); // the local hero poisoned at that second (the icon under the HP bar with its seconds badge)
            if (poison != null) StartCoroutine(Timed(poison + "=x", v => { if (GameSession.Hero != null) GameSession.Hero.DebugPoison(12000f); }));
            string fallen = Val("--ro-fallen"); // the Wanted kill toast exactly as NetEventPlayer raises it (online only otherwise)
            if (fallen != null) StartCoroutine(Timed(fallen + "=x", v => NotificationToast.ShowStyled("The Mire Hydra Queen has fallen!", "wanted-toast-navy", "wanted-icon-swords", new Color(1f, 0.9f, 0.5f))));
        }

        /// <summary>Runs `act(value)` for each "seconds=value" step, in order, timed from now.</summary>
        static IEnumerator Timed(string script, System.Action<string> act)
        {
            float start = Time.realtimeSinceStartup;
            foreach (string step in script.Split(';'))
            {
                string[] kv = step.Split('=');
                float t = float.Parse(kv[0], CultureInfo.InvariantCulture);
                while (Time.realtimeSinceStartup - start < t) yield return null;
                act(kv[1].Trim());
            }
        }

        static void ApplyLook(HeroController hero, string v)
        {
            string[] n = v.Split(',');
            HeroRenderer view = hero.View;
            int hat = n.Length > 3 ? int.Parse(n[3]) : view.Appearance != null ? view.Appearance.equippedHeadgearId : 0;
            var a = new CharacterAppearanceData { stored = true, classId = hero.Rpg != null ? hero.Rpg.ClassId : null, hairStyle = int.Parse(n[0]), hairColorIndex = int.Parse(n[1]), outfitColorIndex = int.Parse(n[2]), equippedHeadgearId = hat };
            view.SetAppearance(a, hero.PlayerName, true, false);
            Debug.Log("[ZenoniaProto] ro-look style " + a.hairStyle + " hair " + a.hairColorIndex + " (" + HeroPalettes.HairName(a.hairColorIndex) + ") outfit " + a.outfitColorIndex + " hat " + hat + " layered " + view.IsLayered);
        }

        IEnumerator Digit(int slot)
        {
            PlayerState p = RpgSession.Player;
            int skill = p != null ? p.Loadout[slot] : -1;
            float before = GameSession.Hero != null && skill >= 0 ? GameSession.Hero.SkillCdLeftMs(skill) : -1f;
            InputAdapter.TestDigitSlot = slot;
            yield return null; yield return null;
            InputAdapter.TestDigitSlot = -1;
            yield return null; yield return null;
            float after = GameSession.Hero != null && skill >= 0 ? GameSession.Hero.SkillCdLeftMs(skill) : -1f;
            Debug.Log("[ZenoniaProto] ro-digit " + (slot + 1) + " => loadout skill " + skill + ", cooldown " + before.ToString("F0") + " -> " + after.ToString("F0") + " ms" + (after > before ? " (CAST)" : ""));
        }

        void RoClick(string name)
        {
            RectTransform target = FindRo(name);
            if (target == null) { Debug.Log("[ZenoniaProto] ro-click " + name + " => NO SUCH CONTROL (or hidden)"); return; }
            Canvas canvas = target.GetComponentInParent<Canvas>().rootCanvas;
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, target.TransformPoint(target.rect.center));
            var e = new PointerEventData(EventSystem.current) { pointerId = -1, button = PointerEventData.InputButton.Left, position = screen, pressPosition = screen };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(e, hits);
            if (hits.Count == 0) { Debug.Log("[ZenoniaProto] ro-click " + name + " => nothing under the pointer at " + screen); return; }
            GameObject hit = hits[0].gameObject;
            bool onTarget = hit.transform == target || hit.transform.IsChildOf(target) || target.IsChildOf(hit.transform);
            e.pointerPressRaycast = e.pointerCurrentRaycast = hits[0];
            GameObject down = ExecuteEvents.ExecuteHierarchy(hit, e, ExecuteEvents.pointerDownHandler);
            e.pointerPress = down; e.rawPointerPress = hit;
            ExecuteEvents.ExecuteHierarchy(hit, e, ExecuteEvents.pointerUpHandler);
            GameObject clicked = ExecuteEvents.ExecuteHierarchy(hit, e, ExecuteEvents.pointerClickHandler);
            StartCoroutine(LogAfter(name, hit.name + (onTarget ? "" : " (NOT THE TARGET)") + ", handler " + (clicked != null ? clicked.name : down != null ? down.name : "none")));
        }

        static IEnumerator LogAfter(string name, string hit)
        {
            yield return null; yield return null; yield return null; // a pressed key latches on the next frame, a window opens on the one after
            string open = "";
            foreach (UiScreen s in System.Enum.GetValues(typeof(UiScreen))) if (UiScreens.IsOpen(s)) open += s + " ";
            RoHudControls ro = TouchControls.Ro;
            Debug.Log("[ZenoniaProto] ro-click " + name + " hit " + hit + " => open: " + (open == "" ? "none " : open) + "| more " + (ro != null && ro.Menu.MoreOpen)
                + " auto " + AutoBattle.On + " page " + (ro != null ? ro.Page + 1 : 0) + " partyFolded " + RoHudParty.Collapsed + " chat " + ChatWindow.Expanded
                + " zoom " + Minimap.Zoom + " quests " + QuestLogWindow.IsOpen + " settings " + SettingsWindow.IsOpen + " modal " + UiWindows.ModalOpen);
        }

        /// <summary>The active RO HUD control named `name` (the first match under an RO root), or null.</summary>
        static RectTransform FindRo(string name)
        {
            foreach (RectTransform rt in Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None))
            {
                if (rt.name != name || !rt.gameObject.activeInHierarchy) continue;
                for (Transform t = rt; t != null; t = t.parent)
                    if (t.name.StartsWith("RoHud") || t.name.StartsWith("RoChat") || t.name == "Ro") return rt;
            }
            return null;
        }
    }
}
