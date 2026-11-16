using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// One straw nest of the Pet Camp (pet-final-mockup-pet-camp-team.png): the role ribbon with its red share pill on top ("Combat 100%"),
    /// the pet standing on the nest, its level and stars on the nest's front. An open empty nest shows "+", a closed one a padlock and the
    /// hero level that opens it; while a pet is being placed the nest is lit, says what the pet would give here and "Swap" when taken.
    /// The Combat nest is the large one. The whole nest is one tap target.
    /// </summary>
    public sealed class PetCampNest
    {
        public readonly RectTransform Root;
        /// <summary>Distance from the top of Root down to the middle of the nest: the window places that point on the camp.</summary>
        public readonly float NestY;
        readonly Image nest, pet;
        readonly Text level, note, plus, lockText;
        readonly PaperStars stars;
        readonly GameObject swapTag, lockLayer, noteTag;
        readonly string nestPiece;
        Color artTint = Color.white;
        int lit = -1;

        public PetCampNest(Transform parent, string name, float cx, float cy, bool big, string role, string share, string roleIcon, Action onClick)
        {
            float w = big ? 180f : 136f, nestH = big ? 77f : 58f, signW = big ? 124f : 108f, signH = signW * 127f / 360f, petSize = big ? 122f : 64f;
            NestY = big ? 162f : 100f;
            nestPiece = big ? PaperPiece.PetNestLarge : PaperPiece.PetNestSmall;
            float h = NestY + nestH * 0.5f;
            Root = UiKit.Rect(parent, name, cx - w * 0.5f, cy - NestY, w, h);
            Root.pivot = new Vector2(0.5f, 0.5f); // so the hover swell grows about the middle
            Root.anchoredPosition += new Vector2(w * 0.5f, -h * 0.5f);
            Image hit = UiKit.Panel(Root, Color.clear, true); // invisible: sign, pet and nest all answer the tap
            nest = PaperArt.Draw(UiKit.Rect(Root, "Nest", 0f, NestY - nestH * 0.5f, w, nestH), null, Color.white, Color.clear);
            pet = UiKit.Icon(Root, "Pet", (w - petSize) * 0.5f, NestY + nestH * 0.08f - petSize, petSize);

            RectTransform sign = UiKit.Rect(Root, "Sign", (w - signW) * 0.5f, 0f, signW, signH);
            PaperArt.Draw(sign, PaperPiece.PetRoleSign, PaperTheme.Paper, PaperTheme.PaperEdge, 6f);
            float iconSize = signH * 0.5f;
            Image icon = UiKit.Icon(sign, "Icon", signW * 0.17f, signH * 0.12f, iconSize);
            icon.sprite = PaperArt.Get(roleIcon);
            icon.enabled = icon.sprite != null;
            PaperTheme.Title(sign, "Role", signW * 0.17f + iconSize, signH * 0.08f, signW * 0.66f - iconSize, signH * 0.58f, role, big ? 14 : 12, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter)
                .horizontalOverflow = HorizontalWrapMode.Overflow;
            float pillW = big ? 52f : 44f, pillH = big ? 20f : 17f;
            RectTransform pill = UiKit.Rect(Root, "Share", (w - pillW) * 0.5f, signH * 0.7f, pillW, pillH);
            PaperArt.Draw(pill, PaperPiece.PetPercentPill, PaperTheme.BadgeRed, PaperTheme.BtnRedEdge, pillH * 0.5f);
            PaperTheme.Caption(pill, share, big ? 13 : 11, PaperTheme.TextLight);
            RectTransform tag = UiKit.Rect(Root, "Note", (w - 124f) * 0.5f, signH * 0.7f + pillH + 2f, 124f, 28f);
            PaperArt.Plate(tag, PaperTheme.Cream, PaperTheme.PaperEdge, 6f);
            note = PaperTheme.Body(tag, "Text", 4f, 1f, 116f, 26f, "", PaperTheme.BarSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
            note.lineSpacing = 0.8f;
            noteTag = tag.gameObject;

            float textY = NestY - (big ? 4f : 6f);
            level = PaperTheme.Title(Root, "Level", 0f, textY, w, 16f, "", big ? 15 : 13, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter);
            float star = big ? 14f : 11f;
            stars = new PaperStars(Root, "Stars", (w - star * 5f) * 0.5f, textY + 16f, 5, star);
            plus = PaperTheme.Title(Root, "Plus", 0f, NestY - 26f, w, 36f, "+", big ? 34 : 28, PaperTheme.Cream, PaperInk.Outlined, TextAnchor.MiddleCenter);

            RectTransform swap = UiKit.Rect(Root, "Swap", (w - 52f) * 0.5f, NestY + 6f, 52f, 20f);
            PaperArt.Draw(swap, PaperPiece.PetTagSkill, PaperTheme.BtnBlue, PaperTheme.BtnBlueEdge, 9f);
            PaperTheme.Caption(swap, "Swap", PaperTheme.BarSize + 1, PaperTheme.TextLight);
            swapTag = swap.gameObject;

            RectTransform locked = UiKit.Rect(Root, "Lock", (w - 60f) * 0.5f, NestY - 34f, 60f, 44f);
            PaperKit.Padlock(locked, big ? 26f : 22f);
            lockText = PaperTheme.Title(locked, "Text", -20f, 34f, 100f, 16f, "", PaperTheme.BodySize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter);
            lockLayer = locked.gameObject;
            UiWinKit.MakeButton(hit, onClick);
            Light(false);
        }

        /// <summary>
        /// What the nest shows. `petSprite` null = empty. `lockedText` ("Lv 20") closes the nest. `placing` = a pet is being placed: an
        /// occupied nest wears the Swap tag and `noteText` says what the carried pet would give here.
        /// </summary>
        public void Set(Sprite petSprite, Color tint, string levelText, int starCount, bool selected, string lockedText, bool placing, string noteText)
        {
            bool locked = lockedText != null, empty = petSprite == null;
            if (pet.sprite != petSprite) pet.sprite = petSprite;
            pet.enabled = !empty && !locked;
            pet.color = tint;
            bool swap = placing && !empty && !locked;
            level.text = empty || locked || swap ? "" : levelText;
            stars.Root.gameObject.SetActive(!empty && !locked && !swap);
            stars.Set(starCount);
            plus.enabled = empty && !locked;
            if (swapTag.activeSelf != swap) swapTag.SetActive(swap);
            if (lockLayer.activeSelf != locked) lockLayer.SetActive(locked);
            lockText.text = lockedText ?? "";
            bool hasNote = !locked && !string.IsNullOrEmpty(noteText);
            if (noteTag.activeSelf != hasNote) noteTag.SetActive(hasNote);
            note.text = hasNote ? noteText : "";
            artTint = locked ? new Color(0.62f, 0.62f, 0.68f, 1f) : Color.white;
            lit = -1; // redraw: the tint may have changed
            Light(selected && !locked);
        }

        /// <summary>The lit ring without changing what is shown: the selected nest, or the one a dragged card is over.</summary>
        public void Light(bool on)
        {
            if (lit == (on ? 1 : 0)) return;
            lit = on ? 1 : 0;
            bool art = PaperArt.Redraw(nest, on ? nestPiece + PaperPiece.PetSelected : nestPiece, on ? PaperTheme.Gold : PaperTheme.Wood, on ? PaperTheme.Glow : PaperTheme.WoodDark, 20f);
            if (art) nest.color = artTint; // a closed nest is greyed (the flat fallback keeps its own colours)
        }
    }
}
