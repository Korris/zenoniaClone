using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using B = ZenoniaProto.InventoryRoBoxes;
using W = ZenoniaProto.InventoryRoWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// Pieces of the RO Soft Fantasy Pet window (PetRoWindow, mockup docs/reference/ui-style-ro-soft-fantasy/05-pet.png): the ChatGPT pet-* art
    /// (Tools/ui-ro-soft-sheets/ro6-pet-pieces.json, ro7-pet-icons.json), the paper kit icons it borrows (coin, food, ball), rarity pills, star rows,
    /// pet pictures, the left menu button and the pet list card. Layout is the bag's CSS-like boxes (InventoryRoBoxes): padding / gap constants here.
    /// </summary>
    public static class PetRoWidgets
    {
        // nav button: padding 14 left, gap 10, icon 30, text 18
        public const float NavH = 56f, NavPadL = 14f, NavGap = 10f, NavIcon = 32f;
        public const int NavText = 18;

        static readonly Dictionary<string, Sprite> Loaded = new Dictionary<string, Sprite>();

        /// <summary>A whole picture from Resources (paper kit icons, pet art), cached; null when missing.</summary>
        public static Sprite Picture(string path)
        {
            Sprite s;
            if (Loaded.TryGetValue(path, out s) && s != null) return s;
            var tex = Resources.Load<Texture2D>(path);
            s = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect) : null;
            Loaded[path] = s;
            return s;
        }

        /// <summary>The big picture of a pet (Sprites/pets/&lt;ArtId&gt;/art.png), else its icon.</summary>
        public static Sprite PetArt(PetDef d) { if (d == null) return null; Sprite s = Picture("Sprites/pets/" + d.ArtId + "/art"); return s != null ? s : ZenoniaProto.PetArt.Icon(d); }

        public static string RarityPiece(PetDef d)
        {
            if (d == null || d.Legacy) return "pet-pill-cream";
            switch (d.Rarity) { case PetRarity.R: return "pet-rarity-r"; case PetRarity.SR: return "pet-rarity-sr"; case PetRarity.SSR: return "pet-rarity-ssr"; case PetRarity.UR: return "pet-rarity-ur"; default: return "pet-rarity-n"; }
        }

        /// <summary>Light tint of a list card by rarity (the mockup's gold / lavender / blue / mint card backs).</summary>
        public static Color CardTint(PetDef d)
        {
            if (d == null || d.Legacy) return RoSoftTheme.Hex("#F3E3CF");
            switch (d.Rarity) { case PetRarity.R: return RoSoftTheme.Hex("#D8E8FA"); case PetRarity.SR: return RoSoftTheme.Hex("#E6DAF8"); case PetRarity.SSR: return RoSoftTheme.Hex("#F8E7B8"); case PetRarity.UR: return RoSoftTheme.Hex("#F8DDF0"); default: return RoSoftTheme.Hex("#DDF1E4"); }
        }

        /// <summary>A rarity pill (N .. UR, Legacy) sized to its label.</summary>
        public static B.PillBox Rarity(Transform parent, string name, float x, float y, float h, int size, PetDef d, float minW = 0f)
        {
            B.PillBox p = B.Pill(parent, name, x, y, h, RarityPiece(d), PetConfig.RarityLabel(d), size, Color.white, minW);
            W.Rim(p.Label);
            return p;
        }

        public static void SetRarity(B.PillBox p, PetDef d)
        {
            W.SetArt(p.Skin, RarityPiece(d));
            Sprite s = p.Skin.sprite;
            if (s != null) p.Skin.pixelsPerUnitMultiplier = s.rect.height / (2f * p.Root.sizeDelta.y);
            p.Label.text = PetConfig.RarityLabel(d);
            p.Label.color = d != null && d.Legacy ? W.Ink : Color.white;
            Outline rim = p.Label.GetComponent<Outline>();
            if (rim != null) rim.enabled = d == null || !d.Legacy; // ink on the cream Legacy pill needs no rim
            p.Root.gameObject.SetActive(d != null);
        }

        /// <summary>Five stars in a row (gold up to `stars`, grey after), `size` each, `gap` apart; (x, y) = top-left, or the row's top-centre when centred.</summary>
        public sealed class Stars
        {
            readonly Image[] cells = new Image[PetRules.MaxStars];
            public readonly RectTransform Root;

            public Stars(Transform parent, string name, float x, float y, float size, float gap, bool centred)
            {
                float w = PetRules.MaxStars * size + (PetRules.MaxStars - 1) * gap;
                Root = UiKit.Rect(parent, name, centred ? x - w * 0.5f : x, y, w, size);
                for (int i = 0; i < cells.Length; i++) cells[i] = W.Img(Root, "Star" + i, i * (size + gap), 0f, size, size, "pet-star");
            }

            /// <summary>`max` hides the grey stars after `stars` (the small list cards show only what is earned).</summary>
            public void Set(int stars, bool showEmpty = true)
            {
                for (int i = 0; i < cells.Length; i++)
                {
                    bool on = i < stars;
                    cells[i].gameObject.SetActive(on || showEmpty);
                    W.SetArt(cells[i], on ? "pet-star" : "pet-star-empty");
                }
            }
        }

        /// <summary>Left menu button: icon + text in one row, mint when on (bag-pill-mint / bag-pill-white skins).</summary>
        public sealed class NavButton
        {
            readonly Image skin, icon;
            readonly Text label;

            public NavButton(Transform parent, string name, float x, float y, float w, string iconPiece, string text, UnityAction onClick)
            {
                RectTransform root = UiKit.Rect(parent, name, x, y, w, NavH);
                skin = root.gameObject.AddComponent<Image>();
                var row = root.gameObject.AddComponent<HorizontalLayoutGroup>();
                row.padding = new RectOffset((int)NavPadL, 8, 0, 2);
                row.spacing = NavGap;
                row.childAlignment = TextAnchor.MiddleLeft;
                row.childControlWidth = row.childControlHeight = true;
                row.childForceExpandWidth = row.childForceExpandHeight = false;
                icon = W.Img(root, "Icon", 0f, 0f, NavIcon, NavIcon, iconPiece);
                Fixed(icon.gameObject, NavIcon, NavIcon);
                label = W.Txt(root, "Label", 0f, 0f, w - NavPadL - NavIcon - NavGap - 8f, NavH, text, NavText, W.Ink);
                Fixed(label.gameObject, w - NavPadL - NavIcon - NavGap - 8f, NavH - 4f);
                W.Clickable(skin, onClick);
                Set(false);
            }

            public void Set(bool on)
            {
                W.SetArt(skin, on ? "bag-pill-mint" : "bag-pill-white");
                skin.pixelsPerUnitMultiplier = 2f; // ~12 px corners like the bag tabs
                icon.color = on ? Color.white : W.IconSlate;
                label.color = on ? Color.white : W.Ink;
                if (on) { if (label.GetComponent<Shadow>() == null) label.gameObject.AddComponent<Shadow>().effectColor = new Color(0.1f, 0.3f, 0.3f, 0.35f); }
                else { Shadow s = label.GetComponent<Shadow>(); if (s != null) Object.Destroy(s); }
            }
        }

        /// <summary>One pet in the list: rarity-tinted cell, picture, rarity pill top-left, earned stars and Lv along the bottom, gold glow when selected.</summary>
        public sealed class Card
        {
            public readonly W.Cell Cell;
            readonly B.PillBox rarity;
            readonly Stars stars;
            readonly Text lv;

            public Card(Transform parent, string name, float x, float y, float size, UnityAction onClick)
            {
                Cell = new W.Cell(parent, name, x, y, size, onClick);
                rarity = Rarity(Cell.Root, "Rarity", 3f, 3f, 17f, 11, null, 26f);
                stars = new Stars(Cell.Root, "Stars", 5f, size - 16f, 9f, 0f, false);
                lv = W.Txt(Cell.Root, "Lv", 0f, size - 22f, size - 5f, 18f, "", 12, Color.white, TextAnchor.MiddleRight);
                W.Rim(lv);
            }

            /// <summary>`owned` false = a species not caught yet (Collection): faded picture, no level.</summary>
            public void Set(PetDef d, OwnedPet o, bool selected)
            {
                Cell.Root.gameObject.SetActive(d != null);
                if (d == null) return;
                bool owned = o != null;
                Cell.SetState(selected, false, CardTint(d));
                Cell.Back.color = selected ? new Color(1f, 1f, 1f, 0f) : CardTint(d);
                Cell.SetIcon(ZenoniaProto.PetArt.Icon(d), owned ? ZenoniaProto.PetArt.TintOf(d) : new Color(0.35f, 0.4f, 0.48f, 0.55f), 0.8f);
                SetRarity(rarity, d);
                stars.Root.gameObject.SetActive(owned);
                if (owned) stars.Set(o.stars, false);
                lv.text = owned ? "Lv." + o.level : "?";
            }
        }

        static void Fixed(GameObject go, float w, float h)
        {
            LayoutElement e = go.AddComponent<LayoutElement>();
            e.minWidth = e.preferredWidth = w;
            e.minHeight = e.preferredHeight = h;
        }
    }
}
