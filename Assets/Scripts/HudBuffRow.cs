using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Row of small icons under the EXP bar: one per skill currently buffing the local hero, from any caster, with a dark sweep and the seconds left.
    /// A skill giving two buffs (Berserker Awakening) shows once, with the longer time. The list comes from HeroBuffs.ActiveFor, the same on every side.
    /// Two looks: the old dark 24-unit cells (8 of them), and the compact HUD's row (`paper`: thin dark slots, at most 6, "12s" under each icon).
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
        readonly bool paper;

        /// <summary>True after Refresh while a shield buff (Mana Shield, Divine Aegis, a pet's shield) is on the hero: the badge's shield layer.</summary>
        public bool Shielded { get; private set; }

        /// <summary>size 0 = the old row (24-unit dark cells, 8 slots); a size = the paper row with slots that big.</summary>
        public HudBuffRow(Transform parent, float x, float y, float size = 0f)
        {
            paper = size > 0f;
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

        public void Refresh(HeroController hero)
        {
            HeroBuffs.ActiveFor(hero, views);
            merged.Clear();
            Shielded = false;
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
                    Sprite icon = SkillIcons.Get(BuffSource.Def(v.Source));
                    s.Icon.sprite = icon;
                    s.Sweep.sprite = icon;
                    s.Root.SetActive(true);
                }
                s.Sweep.fillAmount = v.TotalMs > 0f ? 1f - Mathf.Clamp01(v.LeftMs / v.TotalMs) : 0f;
                int seconds = Mathf.CeilToInt(v.LeftMs / 1000f);
                if (seconds != s.ShownSeconds) { s.ShownSeconds = seconds; s.Seconds.text = paper ? seconds + "s" : seconds.ToString(); }
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
