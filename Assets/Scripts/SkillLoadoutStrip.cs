using System;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The magazine as shown in the skill tree window: 3 pages x 3 slots (page 1 slot 1 = the fixed Dash, the other 8 = SkillLoadout). In assign mode the
    /// 8 slots pulse and a tap places the selected skill there; outside it, tapping a filled slot selects that skill in the tree.
    /// </summary>
    public sealed class SkillLoadoutStrip
    {
        const float Cell = 52f, Gap = 5f, GroupGap = 20f;
        public const float Width = 3 * (3 * Cell + 2 * Gap) + 2 * GroupGap;
        static readonly Color Dim = new Color(0.55f, 0.55f, 0.6f, 0.9f);

        readonly Image[] icons = new Image[SkillMagazine.MaxPages * SkillMagazine.SlotCount], glows = new Image[SkillMagazine.MaxPages * SkillMagazine.SlotCount];
        bool assignMode;

        /// <summary>onTap(flat slot 0-8): 0 = Dash. Cells are laid out left to right, a gap between pages.</summary>
        public static SkillLoadoutStrip Build(Transform parent, float x, float y, Action<int> onTap)
        {
            var s = new SkillLoadoutStrip();
            UiKit.Label(parent, "MagazineTitle", x, y - 26f, 300f, 22f, "Skill magazine  (swipe pages in game)", 14, UiWinKit.Gold, TextAnchor.MiddleLeft, true);
            for (int i = 0; i < s.icons.Length; i++)
            {
                int page = i / 3, col = i % 3, flat = i;
                float cx = x + page * (3 * Cell + 2 * Gap + GroupGap) + col * (Cell + Gap);
                RectTransform cell = UiKit.Rect(parent, "Slot" + i, cx, y, Cell, Cell);
                Image bg = UiWinKit.Sliced(cell, "slot", Cell / 160f, true);
                s.glows[i] = UiKit.Fill(cell, "Glow", -3f).gameObject.AddComponent<Image>();
                s.glows[i].sprite = UiSprites.Ring(); s.glows[i].color = UiWinKit.Gold; s.glows[i].raycastTarget = false; s.glows[i].enabled = false;
                s.icons[i] = UiKit.Fill(cell, "Icon", 6f).gameObject.AddComponent<Image>();
                s.icons[i].preserveAspect = true; s.icons[i].raycastTarget = false;
                UiKit.Label(cell, "Key", 3f, 1f, 20f, 14f, flat == 0 ? "D" : flat.ToString(), 11, new Color(1f, 1f, 1f, 0.8f), TextAnchor.UpperLeft, true);
                UiWinKit.MakeButton(bg, () => onTap(flat));
                if (col == 0) UiKit.Label(parent, "Page" + page, cx, y + Cell + 2f, 3 * Cell + 2 * Gap, 16f, "Page " + (page + 1), 11, UiWinKit.Dim, TextAnchor.MiddleCenter);
            }
            return s;
        }

        public void Show(PlayerState p, HeroClassDef cls, bool assign)
        {
            assignMode = assign;
            for (int i = 0; i < icons.Length; i++)
            {
                Sprite sprite = null;
                if (i == 0) sprite = TouchControls.SkillSprite(cls, cls.DashIcon);
                else if (p.Loadout[i - 1] >= 0) sprite = SkillIcons.Get(SkillTable.Get(p.ClassId, p.Loadout[i - 1]));
                icons[i].sprite = sprite; icons[i].enabled = sprite != null;
                icons[i].color = i == 0 ? Dim : Color.white;
                glows[i].enabled = assign && i > 0;
            }
        }

        public void Pulse(float t)
        {
            if (!assignMode) return;
            var c = new Color(1f, 0.82f, 0.36f, 0.35f + 0.55f * (0.5f + 0.5f * Mathf.Sin(t * 4f)));
            for (int i = 1; i < glows.Length; i++) glows[i].color = c;
        }
    }
}
