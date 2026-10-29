using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Sprites for loot, shared by the ground drops and the UI: the 96px item icons (Resources/Sprites/icons, built by Tools/build-item-icons.py)
    /// and small generated pixel sprites for coins, HP / MP potions, upgrade stones and the soft rarity glow.
    /// Generated sprites are 32px; `ppu` decides the world size (UI images ignore it).
    /// </summary>
    public static class LootSprites
    {
        public const float IconPpu = 8f;   // 96px icon => 12 world units on the ground
        public const float SmallPpu = 4.5f; // 32px coin / potion / stone => about 7 units
        static readonly Dictionary<string, Sprite> Icons = new Dictionary<string, Sprite>();
        static Sprite coin, hpPotion, mpPotion, stone, glow;

        public static void ResetState() { Icons.Clear(); }

        /// <summary>Icon of an equipment item (bottom-centre pivot), or null when the PNG is missing.</summary>
        public static Sprite Icon(Item item)
        {
            string name = ItemInfo.IconName(item);
            if (name == null) return null;
            Sprite s;
            if (Icons.TryGetValue(name, out s) && s != null) return s;
            var tex = Resources.Load<Texture2D>("Sprites/icons/" + name);
            s = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.1f), IconPpu, 0, SpriteMeshType.FullRect) : null;
            Icons[name] = s;
            return s;
        }

        /// <summary>Ground / UI sprite of any drop.</summary>
        public static Sprite For(LootEntry e)
        {
            switch (e.Kind)
            {
                case LootKind.Coin: return Coin();
                case LootKind.HpPotion: return Potion(true);
                case LootKind.MpPotion: return Potion(false);
                case LootKind.UpgradeStone: return Stone();
                default: return Icon(e.Item);
            }
        }

        public static Color Hex(string hex)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.white;
        }

        public static Color RarityColor(Item item) { return Hex(ItemInfo.RarityColor(item != null ? item.rarity : null)); }

        // ---- generated pixel sprites ----

        delegate Color32 Painter(float x, float y); // x, y in 0..1, y up

        static Sprite Paint(Painter p, float ppu, Vector2 pivot, int size = 32)
        {
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = p((x + 0.5f) / size, (y + 0.5f) / size);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), pivot, ppu, 0, SpriteMeshType.FullRect);
        }

        static Color32 C(float r, float g, float b, float a = 1f) { return new Color(r, g, b, a); }

        /// <summary>Gold coin with a darker rim, an embossed centre and a highlight.</summary>
        public static Sprite Coin()
        {
            if (coin != null) return coin;
            coin = Paint((x, y) =>
            {
                float dx = (x - 0.5f) / 0.42f, dy = (y - 0.5f) / 0.42f, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 1f) return C(0, 0, 0, 0);
                if (d > 0.82f) return C(0.62f, 0.4f, 0.08f);
                float light = Mathf.Clamp01(0.75f + (dy - dx) * 0.25f);
                if (d > 0.5f && d < 0.6f) light *= 0.82f; // embossed inner ring
                if (dx < -0.2f && dy > 0.2f && d < 0.72f) light = 1.15f; // highlight
                return C(1f * light, 0.8f * light, 0.22f * light);
            }, SmallPpu, new Vector2(0.5f, 0.1f));
            return coin;
        }

        /// <summary>Round flask: glass outline, cork, red (HP) or blue (MP) liquid with a shine.</summary>
        public static Sprite Potion(bool hp)
        {
            if (hp && hpPotion != null) return hpPotion;
            if (!hp && mpPotion != null) return mpPotion;
            Color liquid = hp ? new Color(0.92f, 0.16f, 0.2f) : new Color(0.2f, 0.45f, 1f);
            Sprite s = Paint((x, y) =>
            {
                float dx = (x - 0.5f) / 0.36f, dy = (y - 0.36f) / 0.34f, d = Mathf.Sqrt(dx * dx + dy * dy);
                bool neck = Mathf.Abs(x - 0.5f) < 0.11f && y >= 0.6f && y < 0.82f;
                bool cork = Mathf.Abs(x - 0.5f) < 0.13f && y >= 0.82f && y < 0.94f;
                if (cork) return C(0.6f, 0.4f, 0.22f);
                if (d > 1f && !neck) return C(0, 0, 0, 0);
                if (d > 0.84f || (neck && Mathf.Abs(x - 0.5f) > 0.07f)) return C(0.85f, 0.92f, 1f, 0.95f); // glass rim
                if (neck) return C(0.8f, 0.88f, 1f, 0.5f);
                if (dx < -0.25f && dy > 0.15f && d < 0.65f) return C(1f, 1f, 1f, 0.9f);           // shine
                float shade = Mathf.Clamp01(0.8f + dy * 0.25f);
                return new Color(liquid.r * shade, liquid.g * shade, liquid.b * shade, 1f);
            }, SmallPpu, new Vector2(0.5f, 0.06f));
            if (hp) hpPotion = s; else mpPotion = s;
            return s;
        }

        /// <summary>Cyan faceted gem (upgrade stone).</summary>
        public static Sprite Stone()
        {
            if (stone != null) return stone;
            stone = Paint((x, y) =>
            {
                float ax = Mathf.Abs(x - 0.5f), ay = Mathf.Abs(y - 0.48f);
                float d = ax / 0.34f + ay / 0.42f;
                if (d > 1f) return C(0, 0, 0, 0);
                if (d > 0.86f) return C(0.1f, 0.42f, 0.55f);
                bool top = y > 0.48f, left = x < 0.5f;
                float light = top ? (left ? 1.1f : 0.9f) : (left ? 0.75f : 0.6f);
                if (d < 0.3f) light += 0.15f;
                return C(0.45f * light, 0.95f * light, 1f * light);
            }, SmallPpu, new Vector2(0.5f, 0.06f));
            return stone;
        }

        /// <summary>Soft white radial glow (tint it with the rarity colour), 1 world unit across at scale 1.</summary>
        public static Sprite Glow()
        {
            if (glow != null) return glow;
            glow = Paint((x, y) =>
            {
                float dx = x - 0.5f, dy = y - 0.5f, d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float a = Mathf.Clamp01(1f - d);
                return C(1f, 1f, 1f, a * a);
            }, 32f, new Vector2(0.5f, 0.5f));
            return glow;
        }
    }
}
