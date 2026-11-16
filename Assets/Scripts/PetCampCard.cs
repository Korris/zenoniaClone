using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// One pet card of the Pet Camp (the Owned Pets shelf and the Collection grid): the pet in a dark well, its stars and level, its name
    /// on the paper band below; a gold frame when selected, the role icon of its team slot in the corner. A species the account does not
    /// own is the "?" card with only its name. Drag gestures are handed to `drag` (the shelf: carry the pet to a nest).
    /// </summary>
    public sealed class PetCampCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const float W = 82f, H = 97f; // the cut card is 176 x 208 px
        const float NameWidth = 66f;         // the paper band inside the card's frame
        static readonly string[] RoleIcons = { "combat", "support", "support", "stat", "stat" };

        public RectTransform Root { get; private set; }
        /// <summary>Shelf cards: begin / move / end of a drag with the card's index. Null = not draggable.</summary>
        public Action<int, int, PointerEventData> Drag;
        public int Index;
        Image frame, icon, role;
        Text level, label;
        PaperStars stars;
        int shownFrame = -1;

        public static PetCampCard Create(Transform parent, string name, float x, float y, float scale, Action onClick)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, W, H);
            rt.localScale = new Vector3(scale, scale, 1f);
            var c = rt.gameObject.AddComponent<PetCampCard>();
            c.Root = rt;
            c.frame = PaperArt.Draw(UiKit.Fill(rt, "Frame"), null, Color.white, Color.clear);
            c.icon = UiKit.Icon(rt, "Icon", (W - 56f) * 0.5f, 5f, 56f);
            c.stars = new PaperStars(rt, "Stars", (W - 50f) * 0.5f, 50f, 5, 10f);
            c.level = PaperTheme.Body(rt, "Level", 0f, 59f, W, 14f, "", PaperTheme.BarSize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter, true);
            c.label = PaperTheme.Body(rt, "Name", 0f, 75f, W, 17f, "", PaperTheme.BarSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
            c.label.horizontalOverflow = HorizontalWrapMode.Overflow;
            c.label.rectTransform.pivot = new Vector2(0.5f, 0.5f); // scaled about its middle by FitName
            c.label.rectTransform.anchoredPosition = new Vector2(W * 0.5f, -83.5f);
            c.role = UiKit.Icon(rt, "Role", 3f, 3f, 20f);
            c.role.enabled = false;
            if (onClick != null) UiWinKit.MakeButton(c.frame, onClick);
            return c;
        }

        /// <summary>`owned` null = not owned (the "?" card). `teamSlot` -1 = not in the team.</summary>
        public void Set(PetDef d, OwnedPet owned, bool selected, int teamSlot)
        {
            int look = owned == null ? 2 : selected ? 1 : 0;
            if (look != shownFrame)
            {
                shownFrame = look;
                string piece = owned == null ? PaperPiece.PetCardUnknown : selected ? PaperPiece.PetCard + PaperPiece.PetSelected : PaperPiece.PetCard;
                PaperArt.Redraw(frame, piece, owned == null ? PaperTheme.Plate : PaperTheme.WoodDark, selected ? PaperTheme.Glow : PaperTheme.Wood, 8f);
            }
            if (owned == null && selected) frame.color = PaperTheme.TitleGold; // the "?" card has no selected art: a warm tint marks it
            else if (frame.sprite != PaperArt.Round()) frame.color = Color.white;
            icon.sprite = PetArt.Icon(d);
            icon.enabled = owned != null && icon.sprite != null;
            icon.color = PetArt.TintOf(d);
            stars.Root.gameObject.SetActive(owned != null);
            if (owned != null) stars.Set(owned.stars);
            level.text = owned != null ? "Lv." + owned.level : "";
            if (label.text != d.Name)
            {
                label.text = d.Name;
                float k = Mathf.Min(1f, NameWidth / Mathf.Max(1f, label.preferredWidth)); // "Frostfang King" is wider than the paper band at the kit's smallest size: long names shrink to fit
                label.rectTransform.localScale = new Vector3(k, k, 1f);
            }
            label.color = owned != null ? PaperTheme.Ink : PaperTheme.TextLight;
            Sprite roleSprite = teamSlot >= 0 ? PaperArt.Get(PaperPiece.PetRoleIconPrefix + RoleIcons[teamSlot]) : null;
            role.sprite = roleSprite;
            role.enabled = roleSprite != null;
        }

        public void OnBeginDrag(PointerEventData e) { if (Drag != null) Drag(Index, 0, e); }
        public void OnDrag(PointerEventData e) { if (Drag != null) Drag(Index, 1, e); }
        public void OnEndDrag(PointerEventData e) { if (Drag != null) Drag(Index, 2, e); }
    }

    /// <summary>Rarity badge of a pet (the cut N / R / SR / SSR / UR plates carry their letters); the Legacy pets get a bronze plate with the word.</summary>
    public sealed class PetCampBadge
    {
        public readonly RectTransform Root;
        readonly Image face;
        readonly Text word;
        PetDef shown;

        /// <summary>`h` high and 1.5 h wide, at a top-left based position.</summary>
        public PetCampBadge(Transform parent, string name, float x, float y, float h)
        {
            Root = UiKit.Rect(parent, name, x, y, h * 1.5f, h);
            face = PaperArt.Draw(Root, null, Color.white, Color.clear);
            face.preserveAspect = true;
            word = PaperTheme.Caption(Root, "", PaperTheme.BarSize, PaperTheme.TextLight);
        }

        public void Set(PetDef d)
        {
            if (d == shown) return;
            shown = d;
            bool art = !d.Legacy && PaperArt.Redraw(face, PaperPiece.PetRarityPrefix + d.Rarity.ToString().ToLowerInvariant(), PetArt.LabelColor(d), PaperTheme.Ink, 5f);
            if (!art) PaperArt.Redraw(face, null, PetArt.LabelColor(d) * 0.8f, PaperTheme.Ink, 5f);
            word.text = art ? "" : PetConfig.RarityLabel(d);
        }
    }
}
