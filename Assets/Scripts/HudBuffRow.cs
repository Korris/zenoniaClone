using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Row of small icons under the EXP bar: one per skill currently buffing the local hero, from any caster, with a dark sweep and the seconds left.
    /// A skill giving two buffs (Berserker Awakening) shows once, with the longer time. The list comes from HeroBuffs.ActiveFor, the same on every side.
    /// Three looks: the old dark 24-unit cells (8 of them), the compact HUD's row (`paper`: thin dark slots, at most 6, "12s" under each icon), and
    /// the RO Soft HUD's row (`ro`: ivory cells, the seconds in a small navy badge on the cell's lower-right corner, never over the icon; RoBadge).
    /// </summary>
    public sealed class HudBuffRow
    {
        const float OldSize = 24f, Gap = 3f, PaperGap = 5f, SecondsH = 12f;
        const int OldSlots = 8;
        /// <summary>Slots of the compact row (hud-compact-parts-board-states-v2.png: "Buff Icons (6)").</summary>
        public const int PaperSlots = 6;

        /// <summary>Capture runs only (--buffs-demo): shown instead of the hero's real buffs, because BuffState holds one buff per kind (5 kinds).</summary>
        public static List<BuffView> DemoViews;

        sealed class Slot { public GameObject Root; public Image Icon, Sweep; public Text Seconds; public byte Source = BuffSource.None; public int ShownSeconds = -1; }

        readonly Slot[] slots;
        readonly List<BuffView> views = new List<BuffView>(OldSlots);
        readonly List<BuffView> merged = new List<BuffView>(OldSlots);
        readonly bool paper, ro;
        float poisonTotalMs;

        /// <summary>True after Refresh while a shield buff (Mana Shield, Divine Aegis, a pet's shield) is on the hero: the badge's shield layer.</summary>
        public bool Shielded { get; private set; }

        /// <summary>size 0 = the old row (24-unit dark cells, 8 slots); a size = the paper row with slots that big.</summary>
        public HudBuffRow(Transform parent, float x, float y, float size = 0f, bool roSoft = false)
        {
            ro = roSoft;
            paper = size > 0f && !ro;
            if (ro) { slots = new Slot[PaperSlots]; for (int i = 0; i < slots.Length; i++) slots[i] = RoSlot(parent, i, x + i * (size + RoGap), y, size); return; }
            if (!paper) size = OldSize;
            slots = new Slot[paper ? PaperSlots : OldSlots];
            float inset = paper ? 2.5f : 1f, gap = paper ? PaperGap : Gap;
            for (int i = 0; i < slots.Length; i++)
            {
                RectTransform rt = UiKit.Rect(parent, "Buff" + i, x + i * (size + gap), y, size, size);
                bool framed = paper && PaperArt.Has(PaperPiece.HcBuffSlot); // the cut slot is a hollow frame: it goes over the icon
                if (framed) PaperArt.Plate(UiKit.Fill(rt, "Back", 1.5f), HudCompact.Dark, Color.clear, 3f);
                else if (paper) HudCompact.Capsule(rt, null, HudCompact.Dark, HudCompact.Outline, 4f);
                else UiKit.Panel(rt, new Color(0.02f, 0.03f, 0.06f, 0.8f));
                var s = new Slot { Root = rt.gameObject };
                s.Icon = UiKit.Fill(rt, "Icon", inset).gameObject.AddComponent<Image>();
                s.Icon.raycastTarget = false;
                s.Sweep = UiKit.Fill(rt, "Sweep", inset).gameObject.AddComponent<Image>();
                s.Sweep.raycastTarget = false;
                s.Sweep.color = new Color(0f, 0f, 0f, 0.6f);
                s.Sweep.type = Image.Type.Filled;
                s.Sweep.fillMethod = Image.FillMethod.Radial360;
                s.Sweep.fillOrigin = (int)Image.Origin360.Top;
                s.Sweep.fillClockwise = true;
                if (framed) PaperArt.Draw(UiKit.Fill(rt, "Frame"), PaperPiece.HcBuffSlot, Color.clear, Color.clear, 4f);
                if (paper)
                {
                    // "12s" under the icon, as on the mockup.
                    s.Seconds = PaperTheme.Body(rt, "Seconds", -PaperGap * 0.5f, size, size + PaperGap, SecondsH, "", PaperTheme.BarSize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter, true);
                    s.Seconds.horizontalOverflow = HorizontalWrapMode.Overflow;
                }
                else s.Seconds = UiKit.Label(UiKit.Fill(rt, "Seconds"), "", 11, Color.white, TextAnchor.LowerRight, true);
                s.Root.SetActive(false);
                slots[i] = s;
            }
        }

        const float RoGap = 8f; // room for the corner badge between two cells

        /// <summary>RO Soft cell: ivory slot, the icon inset 2, the dark sweep, the seconds badge (RoBadge) on the lower-right corner.</summary>
        static Slot RoSlot(Transform parent, int i, float x, float y, float size)
        {
            RectTransform rt = UiKit.Rect(parent, "Buff" + i, x, y, size, size);
            RoHud.Art(rt, "slot");
            var s = new Slot { Root = rt.gameObject };
            s.Icon = UiKit.Fill(rt, "Icon", 2.5f).gameObject.AddComponent<Image>();
            s.Icon.raycastTarget = false; s.Icon.preserveAspect = true;
            s.Sweep = UiKit.Fill(rt, "Sweep", 2.5f).gameObject.AddComponent<Image>();
            s.Sweep.raycastTarget = false;
            s.Sweep.color = new Color(0f, 0f, 0f, 0.45f);
            s.Sweep.type = Image.Type.Filled; s.Sweep.fillMethod = Image.FillMethod.Radial360;
            s.Sweep.fillOrigin = (int)Image.Origin360.Top; s.Sweep.fillClockwise = true;
            s.Seconds = RoBadge(rt);
            s.Root.SetActive(false);
            return s;
        }

        /// <summary>
        /// The RO seconds badge: a navy pill (hud-lv-badge, padding 1 / 4, 10-unit Nunito) centred on the lower-right corner of `cell`, so
        /// the number reads on its own and never covers the icon (ui-layout-rules: badges in a corner). Shared with PoisonStatusIcon.
        /// </summary>
        public static Text RoBadge(RectTransform cell)
        {
            RectTransform b;
            Text t = RoHud.Pill(cell, "Seconds", RoHud.BottomRight, new Vector2(0.5f, 0.5f), -1f, 2f, 15f, "hud-lv-badge", null, 0f, "", 10, Color.white, out b, 16f); // the 9-slice navy pill: it grows with "12s"
            b.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(4, 4, 1, 1);
            b.GetComponent<Image>().pixelsPerUnitMultiplier = 44f / 15f; // art height onto the pill height: round ends
            return t;
        }

        public void Refresh(HeroController hero)
        {
            HeroBuffs.ActiveFor(hero, views);
            merged.Clear();
            Shielded = false;
            // Wanted boss poison first (2026-11-19: "icon trúng độc ở thanh máu"): the ChatGPT drop icon with its seconds; total = the longest
            // time seen since it started, so a refresh (new hit) refills the sweep.
            float poisonMs = hero.PoisonMsLeft;
            if (poisonMs > 0f && StatusIcons.Poison != null)
            {
                if (poisonMs > poisonTotalMs) poisonTotalMs = poisonMs;
                merged.Add(new BuffView { Source = StatusIcons.PoisonSource, LeftMs = poisonMs, TotalMs = poisonTotalMs });
            }
            else poisonTotalMs = 0f;
            foreach (BuffView v in views) { Merge(v); if (v.Kind == BuffKind.Shield) Shielded = true; }
            if (DemoViews != null) { merged.Clear(); foreach (BuffView v in DemoViews) Merge(v); }
            for (int i = 0; i < slots.Length; i++)
            {
                Slot s = slots[i];
                if (i >= merged.Count) { if (s.Root.activeSelf) s.Root.SetActive(false); continue; }
                BuffView v = merged[i];
                if (s.Source != v.Source || !s.Root.activeSelf)
                {
                    s.Source = v.Source;
                    Sprite icon = v.Source == StatusIcons.PoisonSource ? StatusIcons.Poison : SkillIcons.Get(BuffSource.Def(v.Source));
                    s.Icon.sprite = icon;
                    s.Sweep.sprite = icon;
                    s.Root.SetActive(true);
                }
                s.Sweep.fillAmount = v.TotalMs > 0f ? 1f - Mathf.Clamp01(v.LeftMs / v.TotalMs) : 0f;
                int seconds = Mathf.CeilToInt(v.LeftMs / 1000f);
                if (seconds != s.ShownSeconds) { s.ShownSeconds = seconds; s.Seconds.text = paper || ro ? seconds + "s" : seconds.ToString(); }
            }
        }

        void Merge(BuffView v)
        {
            if (v.Source == BuffSource.None) return; // only a predicting client's own attack-speed copy has no skill behind it
            for (int i = 0; i < merged.Count; i++)
            {
                if (merged[i].Source != v.Source) continue;
                if (v.LeftMs > merged[i].LeftMs) merged[i] = v;
                return;
            }
            if (merged.Count < slots.Length) merged.Add(v);
        }
    }
}
