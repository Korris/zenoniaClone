using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Round HUD portrait drawn from the hero's own head: the chosen hairstyle and hair colour on the class face (user 2026-11-17: the profile
    /// picture must look like the look picked at character creation). Uses the Customize screen's stacked layers (HairPreviewSprites: hair-under,
    /// hair-back, face, hair, dyed exactly as in the world) and its head crop, inside the masked portrait plate of HudCompact.Portrait.
    /// A class without head art keeps its painted face (RpgHud.FaceOf). Rebuilds only when the look changes, so Show can run every frame.
    /// </summary>
    public sealed class HeroPortraitLayers
    {
        const float Zoom = 1.0f; // the head's bounds fill the disc (the ring hides the edge)

        readonly Image face;       // the portrait's own image: the painted fallback face
        readonly RectTransform cell;
        readonly Image[] layers = new Image[4];
        string shownKey;
        Vector2 shownBox;

        public HeroPortraitLayers(Image face)
        {
            this.face = face;
            cell = UiKit.Rect(face.rectTransform, "HeadCell", 0f, 0f, 1f, 1f);
            for (int l = 0; l < layers.Length; l++)
            {
                layers[l] = UiKit.Fill(cell, "Layer" + l).gameObject.AddComponent<Image>();
                layers[l].raycastTarget = false;
                layers[l].enabled = false;
            }
        }

        /// <summary>The hero's current look (null or no renderer = nothing changes).</summary>
        public void Show(HeroController hero)
        {
            if (hero == null) return;
            HeroRenderer v = hero.View;
            if (v == null) { ShowPainted(hero.Class); return; }
            Show(hero.Class, v.HairStyle, v.HairDye, v.HairColor);
        }

        /// <summary>A look by its parts: style index, dye preset (-1 = the style's own colour), custom colour (wins over the preset).</summary>
        public void Show(HeroClassDef cls, int style, int dye, Color32? custom)
        {
            if (cls == null) return;
            HairPreviewSprites.Look look = HairPreviewSprites.For(cls.Id);
            if (look == null) { ShowPainted(cls); return; }
            Vector2 box = face.rectTransform.rect.size;
            string key = cls.Id + "|" + style + "|" + dye + "|" + (custom.HasValue ? custom.Value.ToString() : "-");
            if (key == shownKey && box == shownBox) return;
            shownKey = key; shownBox = box;
            Sprite[] s = HairPreviewSprites.Layers(look, style, dye, custom);
            for (int l = 0; l < layers.Length; l++)
            {
                Sprite sp = l < s.Length ? s[l] : null;
                layers[l].sprite = sp;
                layers[l].enabled = sp != null;
            }
            face.sprite = null;
            face.enabled = false;
            Fit(look.Head, box);
        }

        /// <summary>The class's painted face (no head art, or a teammate whose look this client does not know).</summary>
        public void ShowPainted(HeroClassDef cls)
        {
            if (cls == null) return;
            string key = "painted|" + cls.Id;
            if (key == shownKey) return;
            shownKey = key;
            foreach (Image l in layers) l.enabled = false;
            face.sprite = RpgHud.FaceOf(cls);
            face.enabled = face.sprite != null;
        }

        /// <summary>Tint of the whole portrait (grey for a dead teammate).</summary>
        public void SetTint(Color c)
        {
            if (face.color != c) face.color = c;
            foreach (Image l in layers) if (l.color != c) l.color = c;
        }

        /// <summary>The Customize tile crop: the 192 cell scaled so the head's bounds fill the box, centred on the head.</summary>
        void Fit(Rect head, Vector2 box)
        {
            if (box.x <= 0f || box.y <= 0f) { shownKey = null; return; } // not laid out yet: try again next frame
            float px = GameConfig.FramePx;
            float size = px * Zoom * Mathf.Min(box.x / (head.width * px), box.y / (head.height * px));
            cell.sizeDelta = new Vector2(size, size);
            cell.anchoredPosition = new Vector2(box.x * 0.5f - (head.x + head.width * 0.5f) * size, -(box.y * 0.5f - (1f - (head.y + head.height * 0.5f)) * size));
        }
    }
}
