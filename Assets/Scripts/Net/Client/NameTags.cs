#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Names above players (online client and host), IMGUI over the world: your own hero's name in bright green (sprite-rules "Quy tắc nhận diện
    /// nhân vật online" 1, user rule 19/11/2026: finds you in a crowd), strangers in white, party members none.
    /// Crowd rule: names are placed nearest-to-your-hero first, a name that would overlap one already placed is skipped, and at most MaxNames show,
    /// so a pack of players never turns into a wall of text. A name never covers the ring under your own hero, and a name shown last frame
    /// keeps a head start (ShownBonus) so two players running past each other do not make the names flicker.
    /// Holding Alt shows every name (still nearest first, overlaps allowed). Heroes hidden behind a live Wanted Boss's body get no name (Monster.HidesHeroAt).
    /// Look (user 20/11 "tên người chơi xài font xấu ... ko đẹp như tên quái vật"): Nunito Bold with the thin dark navy outline of the monster
    /// foot labels (RoTextOutline), sized from the camera so a name is as big as a monster label (Monster.RoTextH), never under MinNamePt when zoomed out.
    /// </summary>
    public partial class NameTags : MonoBehaviour
    {
        const int MaxNames = 8;
        const float ShownBonus = 40f; // world units: a name on screen last frame sorts as if this much nearer
        const float MinNamePt = 11f;  // canvas points (640 tall): the smallest name size, whatever the zoom
        GUIStyle style;
        float outlinePx = 1f, plateScale = 1f; // this frame: outline width in pixels; own plate (emblem, HP bar) scale vs its 13-point design
        readonly List<HeroController> order = new List<HeroController>();
        readonly List<Rect> placed = new List<Rect>();
        readonly HashSet<HeroController> shown = new HashSet<HeroController>(), shownNow = new HashSet<HeroController>();
        readonly Dictionary<HeroController, float> sortKey = new Dictionary<HeroController, float>();

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || UiWindows.ModalOpen || HeroChangeScreen.IsOpen || WantedRewardPanel.AnyOpen || RoWantedBoard.IsOpen) return; // the bounty panel / RO board too // hero select / appearance step over the game: no tags on top. Offline too (user 19/11/2026): your own green name, nobody else's. IMGUI draws over the UGUI canvas: tags would sit on any open window (trade items, shop tabs)
            Camera cam = Camera.main;
            Zone zone = Zone.Main;
            if (cam == null || zone == null) return;
            // Nunito Bold is already bold (no fake bold); no rich text: a player could put <color> / <size> tags in a name.
            if (style == null) style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, font = RoSoftTheme.Bold, fontStyle = RoSoftTheme.Bold != null ? FontStyle.Normal : FontStyle.Bold, richText = false, wordWrap = false };

            HeroController me = LocalPlayer.Hero;
            Vector2 from = me != null ? me.Position : (Vector2)cam.transform.position;
            float ppu = PixelsPerUnit(cam, from);
            // As big as a monster label at this zoom, but never under 11 canvas points (zoomed out, the default view): a name must stay readable.
            style.fontSize = Mathf.RoundToInt(Mathf.Max(Monster.RoTextH * ppu, Screen.height / 640f * MinNamePt, 10f));
            outlinePx = Mathf.Max(1f, RoTextOutline.Width * ppu);
            plateScale = style.fontSize / (Screen.height / 640f * 13f);
            Monster boss = zone.Wanted != null && !zone.Wanted.IsDying ? zone.Wanted : null;
            order.Clear();
            for (int i = 0; i < zone.Heroes.All.Count; i++)
            {
                HeroController h = zone.Heroes.All[i];
                if (h == null || h.IsDead || string.IsNullOrEmpty(h.PlayerName) || h == me || NetMode.IsOffline || NetClient.Party.Contains(h.PlayerId)) continue;
                if (boss != null && boss.HidesHeroAt(h.Position)) continue; // behind the Wanted Boss's body: no name piled on the boss (Alt still skips them)
                order.Add(h);
                sortKey[h] = Vector2.Distance(h.Position, from) - (shown.Contains(h) ? ShownBonus : 0f);
            }
            order.Sort((a, b) => sortKey[a].CompareTo(sortKey[b]));
            sortKey.Clear();

            bool all = ShowAllHeld();
            placed.Clear();
            shownNow.Clear();
            Rect ring = me != null ? HeroRingRect(cam, me.Position) : Rect.zero;
            if (RoHud.Active) DrawVillagerPills(cam); // RO Soft HUD: villager name pills (NameTags.RoSoft.cs)
            if (me != null && !me.IsDead && OwnName(me) != "") DrawOwnName(cam, me); // first, so the other names keep clear of it
            for (int i = 0; i < order.Count; i++)
            {
                HeroController h = order[i];
                Vector3 sp = cam.WorldToScreenPoint(View25D.Above(h.Position.x, h.Position.y, 30f));
                if (sp.z < 0f || sp.x < -80f || sp.x > Screen.width + 80f) continue;
                var label = new GUIContent(h.PlayerName);
                Vector2 size = style.CalcSize(label);
                var rect = new Rect(0f, 0f, size.x + 4f + outlinePx * 2f, size.y);
                rect.center = new Vector2(sp.x, Screen.height - sp.y);
                if (Occluded(rect)) continue; // under an RO panel / banner
                if (!all)
                {
                    if (placed.Count >= MaxNames) break;
                    if (Overlaps(rect) || rect.Overlaps(ring)) continue;
                }
                placed.Add(rect);
                shownNow.Add(h);
                RoTextOutline.GuiLabel(rect, label, style, Color.white, outlinePx);
            }
            shown.Clear();
            shown.UnionWith(shownNow);
        }

        static readonly Color OwnGreen = new Color(0.42f, 1f, 0.42f, 1f);

        /// <summary>Your own name, bright green, always drawn (the crowd rule never skips it), at the height the other names use.</summary>
        /// <summary>The character name; offline the save's name (the class name for heroes from before names).</summary>
        static string OwnName(HeroController me)
        {
            if (!string.IsNullOrEmpty(me.PlayerName)) return me.PlayerName;
            if (NetMode.IsOffline && !string.IsNullOrEmpty(RpgSession.CurrentName)) return RpgSession.CurrentName;
            return NetMode.IsOffline && me.Class != null ? me.Class.Name : "";
        }

        void DrawOwnName(Camera cam, HeroController me)
        {
            Vector3 sp = cam.WorldToScreenPoint(View25D.Above(me.Position.x, me.Position.y, 30f));
            if (sp.z < 0f) return;
            var name = new GUIContent(OwnName(me));
            Vector2 size = style.CalcSize(name);
            var rect = new Rect(0f, 0f, size.x + 4f + outlinePx * 2f, size.y);
            rect.center = new Vector2(sp.x, Screen.height - sp.y);
            if (Occluded(rect)) return; // under the chat panel / a banner
            placed.Add(rect);
            RoTextOutline.GuiLabel(rect, name, style, OwnGreen, outlinePx);
            if (RoHud.Active) DrawOwnPlate(rect, me, plateScale); // emblem + small HP bar (NameTags.RoSoft.cs)
        }

        /// <summary>Screen pixels per world unit at the head height of `feet` (follows the wheel zoom, the screen size and the 2.5D view).</summary>
        static float PixelsPerUnit(Camera cam, Vector2 feet)
        {
            Vector3 a = cam.WorldToScreenPoint(View25D.Above(feet.x, feet.y, 30f)), b = cam.WorldToScreenPoint(View25D.Above(feet.x, feet.y, 40f));
            float ppu = Mathf.Abs(b.y - a.y) / 10f;
            return ppu > 0.01f ? ppu : Screen.height / 216f;
        }

        /// <summary>Screen rect (GUI space) of the ring under your own hero (PlayerIndicatorRing, Width x Width/2 units around the feet).</summary>
        static Rect HeroRingRect(Camera cam, Vector2 feet)
        {
            float hw = PlayerIndicatorRing.Width * 0.5f, hh = PlayerIndicatorRing.Width * 0.25f;
            Vector3 a = cam.WorldToScreenPoint(new Vector3(feet.x - hw, feet.y - hh, 0f));
            Vector3 b = cam.WorldToScreenPoint(new Vector3(feet.x + hw, feet.y + hh, 0f));
            return Rect.MinMaxRect(a.x, Screen.height - b.y, b.x, Screen.height - a.y);
        }

        bool Overlaps(Rect r)
        {
            for (int i = 0; i < placed.Count; i++) if (placed[i].Overlaps(r)) return true;
            return false;
        }

        /// <summary>PC: either Alt key held.</summary>
        static bool ShowAllHeld()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
#elif ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb[Key.LeftAlt].isPressed || kb[Key.RightAlt].isPressed);
#else
            return false;
#endif
        }
    }
}
