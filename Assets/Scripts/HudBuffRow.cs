using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Row of small icons under the EXP bar: one per skill currently buffing the local hero, from any caster, with a dark sweep and the seconds left.
    /// A skill giving two buffs (Berserker Awakening) shows once, with the longer time. The list comes from HeroBuffs.ActiveFor, the same on every side.
    /// </summary>
    public sealed class HudBuffRow
    {
        const float Size = 24f, Gap = 3f;
        const int Slots = 8;

        sealed class Slot { public GameObject Root; public Image Icon, Sweep; public Text Seconds; public byte Source = BuffSource.None; public int ShownSeconds = -1; }

        readonly Slot[] slots = new Slot[Slots];
        readonly List<BuffView> views = new List<BuffView>(Slots);
        readonly List<BuffView> merged = new List<BuffView>(Slots);

        public HudBuffRow(Transform parent, float x, float y)
        {
            for (int i = 0; i < Slots; i++)
            {
                RectTransform rt = UiKit.Rect(parent, "Buff" + i, x + i * (Size + Gap), y, Size, Size);
                UiKit.Panel(rt, new Color(0.02f, 0.03f, 0.06f, 0.8f));
                var s = new Slot { Root = rt.gameObject };
                s.Icon = UiKit.Fill(rt, "Icon", 1f).gameObject.AddComponent<Image>();
                s.Icon.raycastTarget = false;
                s.Sweep = UiKit.Fill(rt, "Sweep", 1f).gameObject.AddComponent<Image>();
                s.Sweep.raycastTarget = false;
                s.Sweep.color = new Color(0f, 0f, 0f, 0.6f);
                s.Sweep.type = Image.Type.Filled;
                s.Sweep.fillMethod = Image.FillMethod.Radial360;
                s.Sweep.fillOrigin = (int)Image.Origin360.Top;
                s.Sweep.fillClockwise = true;
                s.Seconds = UiKit.Label(UiKit.Fill(rt, "Seconds"), "", 11, Color.white, TextAnchor.LowerRight, true);
                s.Root.SetActive(false);
                slots[i] = s;
            }
        }

        public void Refresh(HeroController hero)
        {
            HeroBuffs.ActiveFor(hero, views);
            merged.Clear();
            foreach (BuffView v in views) Merge(v);
            for (int i = 0; i < Slots; i++)
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
                if (seconds != s.ShownSeconds) { s.ShownSeconds = seconds; s.Seconds.text = seconds.ToString(); }
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
            if (merged.Count < Slots) merged.Add(v);
        }
    }
}
