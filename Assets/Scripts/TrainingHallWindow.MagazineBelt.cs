using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// TrainingHallWindow: the Skill Magazine belt under the shelf. All nine slots at once, flat index 0-8 as in the old strip: 0 = the
    /// fixed Dash, 1-8 = SkillLoadout slots 0-7, in three groups of three = the three HUD pages ("Page 1" - "Page 3" on the belt's
    /// lower rim) instead of the mockup's "Page 1 / 3" arrows. A tap on a filled slot selects that skill on the shelf. In assign mode
    /// (the scroll's Assign button, or a statue being dragged) the eight slots glow, a banner says what is being placed and an arrow
    /// marks the slot the keys would pick (Left / Right move it, J / Space places; first the first empty slot).
    /// </summary>
    public sealed partial class TrainingHallWindow
    {
        const int BeltSlots = SkillMagazine.MaxPages * SkillMagazine.SlotCount; // 9
        const float BeltX = 196f, BeltY = 492f, BeltW = 876f, BeltH = 134f, SlotGap = 8f, PageGap = 26f, SlotY = BeltY + 31f;
        const float PageW = SkillMagazine.SlotCount * PaperSkillBeltSlot.Size + (SkillMagazine.SlotCount - 1) * SlotGap;
        const float SlotsX = BeltX + (BeltW - SkillMagazine.MaxPages * PageW - (SkillMagazine.MaxPages - 1) * PageGap) * 0.5f;

        readonly PaperSkillBeltSlot[] beltSlots = new PaperSkillBeltSlot[BeltSlots];
        RectTransform belt, hintArrow;
        GameObject hint;
        Image hintIcon;
        Text hintText;
        Sprite dashSprite;
        string dashClass;
        int targetSlot = 1; // flat slot the assign arrow is over (1-8)

        static float SlotX(int flat)
        {
            int page = flat / SkillMagazine.SlotCount, col = flat % SkillMagazine.SlotCount;
            return SlotsX + page * (PageW + PageGap) + col * (PaperSkillBeltSlot.Size + SlotGap);
        }

        void BuildBelt()
        {
            belt = UiKit.Rect(content, "MagazineBelt", BeltX, BeltY, BeltW, BeltH);
            PaperArt.Draw(belt, PaperPiece.HallBelt, PaperTheme.Paper, PaperTheme.Wood, 14f, true);
            RectTransform book = UiKit.Rect(content, "MagazineBook", 36f, 476f, 152f, 161f);
            if (PaperArt.Has(PaperPiece.HallBook)) PaperSkillParts.Pic(book, "Art", 0f, 0f, 152f, 161f, PaperPiece.HallBook);
            else PaperArt.Plate(book, PaperTheme.Wood, PaperTheme.BtnGoldEdge, 10f);
            Text title = PaperTheme.Title(book, "Title", 44f, 82f, 100f, 40f, "SKILL\nMAGAZINE", PaperTheme.BodySize, PaperTheme.TitleGold, PaperInk.Outlined, TextAnchor.MiddleCenter);
            title.lineSpacing = 0.8f;
            for (int page = 0; page < SkillMagazine.MaxPages; page++)
            {
                float x = SlotX(page * SkillMagazine.SlotCount);
                PaperTheme.Body(content, "Page" + page, x, BeltY + BeltH - 29f, PageW, 16f, "Page " + (page + 1), PaperTheme.BarSize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter, true);
                if (page > 0) PaperArt.Plate(UiKit.Rect(content, "PageRule" + page, x - PageGap * 0.5f - 1f, SlotY + 4f, 2f, PaperSkillBeltSlot.Size - 8f), new Color(PaperTheme.Wood.r, PaperTheme.Wood.g, PaperTheme.Wood.b, 0.55f), Color.clear, 1f);
            }
            for (int i = 0; i < BeltSlots; i++)
            {
                int flat = i;
                beltSlots[i] = new PaperSkillBeltSlot(content, "Slot" + i, SlotX(i), SlotY, i == 0 ? "Dash" : i.ToString(), () => OnSlot(flat));
                if (i > 0) beltSlots[i].Root.gameObject.AddComponent<TrainingHallDrag>().Init(this, false, i);
            }
            RectTransform banner = UiKit.Rect(content, "AssignHint", SlotsX, BeltY - 38f, 330f, 34f);
            PaperArt.Draw(banner, PaperPiece.HallHint, PaperTheme.BtnGold, PaperTheme.BtnGoldEdge, 8f);
            hintIcon = UiKit.Icon(banner, "Icon", 8f, 4f, 26f);
            hintText = PaperTheme.Body(banner, "Text", 40f, 0f, 262f, 32f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            hintText.horizontalOverflow = HorizontalWrapMode.Overflow;
            hint = banner.gameObject;
            hintArrow = PaperSkillParts.Pic(content, "AssignArrow", 0f, 0f, 22f, 25f, PaperPiece.HallArrow).rectTransform;
        }

        void RefreshBelt(PlayerState p, HeroController hero, SkillDef shown)
        {
            bool picking = assignMode || dragSkill >= 0;
            if (dashClass != p.ClassId) { dashClass = p.ClassId; dashSprite = TouchControls.SkillSprite(hero.Class, hero.Class.DashIcon); } // SkillSprite builds a new sprite each call
            for (int i = 0; i < BeltSlots; i++)
            {
                Sprite sprite = i == 0 ? dashSprite : p.Loadout[i - 1] >= 0 ? SkillIcons.Get(SkillTable.Get(p.ClassId, p.Loadout[i - 1])) : null;
                beltSlots[i].Set(sprite, i == 0, picking && i > 0);
            }
            hint.SetActive(assignMode);
            hintArrow.gameObject.SetActive(assignMode);
            if (!assignMode) return;
            hintIcon.sprite = SkillIcons.Get(shown);
            hintIcon.enabled = hintIcon.sprite != null;
            hintText.text = "Tap a slot to assign " + shown.Name;
            float w = Mathf.Clamp(hintText.preferredWidth + 78f, 200f, 420f); // the banner ends in an arrow head
            ((RectTransform)hint.transform).sizeDelta = new Vector2(w, 34f);
        }

        /// <summary>The slot the assign arrow starts over: the first empty one, else slot 1.</summary>
        static int FirstFreeSlot(PlayerState p)
        {
            for (int i = 0; i < SkillLoadout.Slots; i++) if (p.Loadout[i] < 0) return i + 1;
            return 1;
        }

        void PulseBelt(float t)
        {
            for (int i = 1; i < BeltSlots; i++) beltSlots[i].Pulse(t);
            if (!assignMode) return;
            float bob = 3f * Mathf.Sin(t * 5f);
            hintArrow.anchoredPosition = new Vector2(SlotX(targetSlot) + (PaperSkillBeltSlot.Size - 22f) * 0.5f, -(SlotY - 40f + bob));
        }

        /// <summary>Flat slot under a screen point (-1 = none): where a dragged statue or slot is dropped.</summary>
        int SlotUnder(Vector2 screen)
        {
            for (int i = 0; i < BeltSlots; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint(beltSlots[i].Root, screen, null)) return i;
            return -1;
        }
    }
}
