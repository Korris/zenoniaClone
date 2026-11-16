using System;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>How a skill statue of the Training Hall stands on its shelf.</summary>
    public enum SkillStatueLook
    {
        Learned,   // gold statue, green tick
        Learnable, // silver statue, gold "!": one skill point teaches it
        Unlearned, // silver statue, nothing to tick: open, but no skill point to spend
        Locked,    // stone statue, padlock: level too low or a prerequisite missing
    }

    /// <summary>Small shared builders of the Training Hall widgets.</summary>
    public static class PaperSkillParts
    {
        /// <summary>The dark well behind a skill icon (statue ring, belt slot, scroll medallion).</summary>
        public static readonly Color Well = new Color(0.17f, 0.1f, 0.07f, 1f);

        /// <summary>A cut picture at a top-left based position, kept in its drawn proportions; hidden while its art is missing.</summary>
        public static Image Pic(Transform parent, string name, float x, float y, float w, float h, string piece)
        {
            Image img = UiKit.Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            Sprite s = PaperArt.Get(piece);
            img.sprite = s;
            img.enabled = s != null;
            return img;
        }
    }

    /// <summary>
    /// One skill statue of the Training Hall (skill-final-mockup-training-hall.png): the skill icon in the ring of a small pedestal, the
    /// name on its body and the rank "3/10" on a dark pill at the foot. Gold = learned, silver = open, stone = locked; a mark on the ring
    /// says which (tick, "!", padlock); gold brackets = selected; purple MAX ribbon and laurels at rank 10. A level lock shows "Lv. 40"
    /// on the pill instead of the rank. `Need` is the small pill over a statue the selected skill asks a rank of ("Rank 3").
    /// The three cut statues share one 256 x 320 canvas, so everything is placed in fractions of that canvas.
    /// </summary>
    public sealed class PaperSkillStatue
    {
        public const float W = 100f, H = 125f;
        const float K = W / 256f; // canvas units per art pixel
        static readonly Color LockedIcon = new Color(0.4f, 0.4f, 0.45f, 1f), OpenIcon = new Color(0.72f, 0.72f, 0.78f, 1f);
        static readonly string[] Bodies = { PaperPiece.HallStatueGold, PaperPiece.HallStatueSilver, PaperPiece.HallStatueSilver, PaperPiece.HallStatueStone };
        static readonly Color[] BodyFills = { PaperTheme.Gold, PaperTheme.PaperEdge, PaperTheme.PaperEdge, PaperTheme.Steel };

        public readonly RectTransform Root;
        public readonly Image Icon;
        readonly Image body;
        readonly Text name, rank, need, max;
        readonly GameObject brackets, laurel, ribbon, needPill;
        readonly GameObject[] marks = new GameObject[3]; // tick, "!", padlock
        int shownLook = -1;

        /// <summary>Middle of the ring, from the statue's top-left: where a floating sign or a drag starts.</summary>
        public static readonly Vector2 RingCentre = new Vector2(127.5f * K, 94f * K);

        /// <summary>(cx, bottom) = where the statue stands: the middle of its foot, in the parent's top-left based units.</summary>
        public PaperSkillStatue(Transform parent, string objectName, float cx, float bottom, Action onClick)
        {
            Root = UiKit.Rect(parent, objectName, cx - W * 0.5f, bottom - H, W, H);
            laurel = PaperSkillParts.Pic(Root, "Laurel", -8f, 16f, W + 16f, 66f, PaperPiece.HallLaurel).gameObject;
            RectTransform plate = UiKit.Fill(Root, "Plate"); // the ring's window: masks the icon
            PaperArt.Draw(plate, PaperPiece.HallStatuePlate, PaperSkillParts.Well, Color.clear, 20f).color = PaperSkillParts.Well;
            plate.gameObject.AddComponent<Mask>();
            Icon = UiKit.Icon(plate, "Icon", RingCentre.x - 34f, RingCentre.y - 34f, 68f);
            body = PaperArt.Draw(UiKit.Fill(Root, "Body"), null, Color.white, Color.clear);
            name = PaperTheme.Body(Root, "Name", 16f, 71f, W - 32f, 30f, "", PaperTheme.BarSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
            name.lineSpacing = 0.8f; // as wide as the statue's body: a two-word name takes two lines ("Blade / Cyclone")
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            RectTransform pill = UiKit.Rect(Root, "Rank", (W - 46f) * 0.5f, 101f, 46f, 17f);
            PaperArt.Draw(pill, PaperPiece.HallRankPill, PaperTheme.Plate, PaperTheme.PlateEdge, 8f);
            rank = PaperTheme.Caption(pill, "", PaperTheme.BarSize, PaperTheme.TextLight, PaperInk.Plain);
            RectTransform b = UiKit.Fill(Root, "Selected", -7f);
            PaperArt.Draw(b, PaperPiece.HallBrackets, Color.clear, PaperTheme.Glow, 10f);
            brackets = b.gameObject;
            marks[0] = PaperKit.Mark(Root, PaperMark.Check, 80f, 13f, 22f);
            marks[1] = PaperKit.Mark(Root, PaperMark.AlertGold, 80f, 13f, 22f);
            marks[2] = PaperKit.Mark(Root, PaperMark.Lock, 80f, 13f, 22f);
            RectTransform rib = UiKit.Rect(Root, "Max", (W - 62f) * 0.5f, -12f, 62f, 27f);
            PaperArt.Draw(rib, PaperPiece.HallMaxRibbon, PaperTheme.Purple, PaperTheme.BtnPurpleEdge, 6f);
            max = PaperTheme.Caption(rib, "MAX", PaperTheme.BarSize + 1, PaperTheme.TextLight);
            max.rectTransform.offsetMin = new Vector2(0f, 8f); // the band is the upper part of the ribbon, its tails hang below
            ribbon = rib.gameObject;
            RectTransform np = UiKit.Rect(Root, "Need", -8f, 50f, 54f, 17f); // on the lower left of the ring: clear of the mark and of a MAX ribbon
            PaperArt.Draw(np, PaperPiece.HallRankPill, PaperTheme.Plate, PaperTheme.Gold, 8f);
            need = PaperTheme.Caption(np, "", PaperTheme.BarSize, PaperTheme.TextLight, PaperInk.Plain);
            needPill = np.gameObject;
            Image hit = UiKit.Fill(Root, "Hit").gameObject.AddComponent<Image>(); // a child, so a drag on the statue (handler on Root) cancels the tap
            hit.color = Color.clear;
            UiWinKit.MakeButton(hit, onClick);
            needPill.SetActive(false);
        }

        /// <summary>`levelLock` = the level the hero still has to reach (0 = not locked by level).</summary>
        public void Set(SkillDef d, int rankNow, SkillStatueLook look, int levelLock, bool selected, Sprite sprite)
        {
            if ((int)look != shownLook)
            {
                shownLook = (int)look;
                PaperArt.Redraw(body, Bodies[shownLook], BodyFills[shownLook], PaperTheme.WoodDark, 14f);
                marks[1].SetActive(look == SkillStatueLook.Learnable);
                marks[2].SetActive(look == SkillStatueLook.Locked);
            }
            bool isMax = rankNow >= SkillDef.MaxRank, tick = look == SkillStatueLook.Learned && !isMax; // the MAX ribbon replaces the tick
            if (marks[0].activeSelf != tick) marks[0].SetActive(tick);
            if (Icon.sprite != sprite) { Icon.sprite = sprite; Icon.enabled = sprite != null; }
            Icon.color = rankNow > 0 ? Color.white : look == SkillStatueLook.Locked ? LockedIcon : OpenIcon;
            name.text = d.Name;
            rank.text = levelLock > 0 ? "Lv. " + levelLock : rankNow + "/" + SkillDef.MaxRank;
            rank.color = levelLock > 0 ? TrainingHallWindow.BadOnDark : isMax ? PaperTheme.TitleGold : rankNow > 0 ? PaperTheme.TextLight : PaperTheme.DisabledText;
            if (brackets.activeSelf != selected) brackets.SetActive(selected);
            if (ribbon.activeSelf != isMax) { ribbon.SetActive(isMax); laurel.SetActive(isMax); }
        }

        /// <summary>The rank the selected skill asks of this one ("Rank 3", green when reached); null hides the pill.</summary>
        public void Need(string text, bool met)
        {
            if (needPill.activeSelf != (text != null)) needPill.SetActive(text != null);
            if (text == null) return;
            need.text = text;
            need.color = met ? TrainingHallWindow.GoodOnDark : TrainingHallWindow.BadOnDark;
        }

        /// <summary>The gold "!" of a learnable statue breathes (time in seconds), like the old tree's gold pulse.</summary>
        public void Pulse(float t)
        {
            if (!marks[1].activeSelf) return;
            float s = 1f + 0.12f * Mathf.Sin(t * 4f);
            marks[1].transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
