using System;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// One skill node of the tree window: icon in a win-slot frame (tinted by tier), rank badge "3/10", name, and the state styling:
    /// locked = dim icon + padlock + "Lv N", learnable = pulsing gold outline, selected = white ring.
    /// </summary>
    public sealed class SkillTreeNodeView
    {
        public const float Size = 96f;
        static readonly Color[] TierTint = { Color.white, new Color(0.72f, 0.85f, 1f), new Color(1f, 0.8f, 0.5f) };
        static readonly Color Locked = new Color(0.4f, 0.4f, 0.45f, 1f), BadgeBg = new Color(0.03f, 0.06f, 0.12f, 0.92f);

        public RectTransform Root;
        Image slot, icon, glow, ring, lockImg;
        Text rank, name, level;
        bool learnable;

        /// <summary>Centre of the node in the parent's top-left based units.</summary>
        public Vector2 Center { get { return new Vector2(Root.anchoredPosition.x + Size * 0.5f, -Root.anchoredPosition.y + Size * 0.5f); } }

        /// <summary>(x, y) = top-left of the node inside `parent` (top-left based canvas units).</summary>
        public static SkillTreeNodeView Build(Transform parent, int index, float x, float y, Action<int> onClick)
        {
            var n = new SkillTreeNodeView();
            n.Root = UiKit.Rect(parent, "Node" + index, x, y, Size, Size);
            n.slot = UiWinKit.Sliced(n.Root, "slot", Size / 160f, true);
            n.glow = Overlay(n.Root, "Glow", -4f, UiSprites.Ring(), UiWinKit.Gold);
            n.icon = Overlay(n.Root, "Icon", 9f, null, Color.white);
            n.icon.preserveAspect = true;
            n.lockImg = UiKit.Icon(n.Root, "Lock", Size * 0.5f - 14f, Size * 0.5f - 24f, 28f);
            n.lockImg.sprite = LockGlyph.Get(); n.lockImg.color = new Color(0.9f, 0.82f, 0.6f, 0.95f);
            n.level = UiKit.Label(n.Root, "Lv", 0f, Size * 0.5f + 2f, Size, 20f, "", 14, new Color(1f, 0.85f, 0.55f), TextAnchor.MiddleCenter, true);
            n.ring = Overlay(n.Root, "Selected", -3f, UiSprites.Ring(), Color.white);
            RectTransform badge = UiKit.Rect(n.Root, "Badge", Size - 46f, Size - 22f, 44f, 20f);
            UiKit.Panel(badge, BadgeBg);
            n.rank = UiKit.Label(UiKit.Fill(badge, "Text"), "", 13, Color.white, TextAnchor.MiddleCenter, true);
            n.name = UiKit.Label(n.Root, "Name", -22f, Size + 3f, Size + 44f, 20f, "", 14, UiWinKit.Cream, TextAnchor.MiddleCenter, true);
            UiWinKit.MakeButton(n.slot, () => onClick(index));
            return n;
        }

        static Image Overlay(RectTransform parent, string name, float inset, Sprite sprite, Color c)
        {
            Image img = UiKit.Fill(parent, name, inset).gameObject.AddComponent<Image>();
            img.sprite = sprite; img.color = c; img.raycastTarget = false;
            return img;
        }

        public void Set(SkillDef d, int rankNow, bool locked, bool levelLocked, bool canLearn, bool selected, Sprite sprite)
        {
            learnable = canLearn;
            slot.color = TierTint[(int)d.Tier];
            icon.sprite = sprite; icon.enabled = sprite != null;
            icon.color = rankNow > 0 ? Color.white : locked ? Locked : new Color(0.72f, 0.72f, 0.78f, 1f);
            lockImg.enabled = locked;
            level.text = levelLocked ? "Lv " + d.UnlockLevel : ""; // a prerequisite lock shows only the padlock
            rank.text = rankNow + "/" + SkillDef.MaxRank;
            rank.color = rankNow >= SkillDef.MaxRank ? UiWinKit.Gold : rankNow > 0 ? Color.white : UiWinKit.Dim;
            name.text = d.Name;
            name.color = locked ? UiWinKit.Dim : UiWinKit.Cream;
            ring.enabled = selected;
            glow.enabled = canLearn;
        }

        /// <summary>Gold pulse of a learnable node (time in seconds).</summary>
        public void Pulse(float t)
        {
            if (learnable) glow.color = new Color(1f, 0.82f, 0.36f, 0.35f + 0.55f * (0.5f + 0.5f * Mathf.Sin(t * 4f)));
        }
    }
}
