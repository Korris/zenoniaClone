using System;
using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Pictures of the cosmetic hats (HeadgearCatalog). The art comes later from ChatGPT (spec Prompt 6), so everything here falls back gracefully:
    ///   - icon: Resources/Sprites/icons/headgear-&lt;key&gt;.png, else the down cell of the hat's layered sheet, else the down cell of the layered
    ///     test set's hat (an existing generated asset), else the shared necklace icon. Nothing is drawn in code.
    ///   - head layer: Data/hero-layered/headgear.json gives each hat's sheet Sprites/hero-layered/headgear/&lt;key&gt;.png and seat point;
    ///     a hat without its sheet yet draws nothing (bare head).
    /// </summary>
    public static class HeadgearArt
    {
        [Serializable] class HatFile { public HeroLayeredSet.LayerEntry[] hats; }

        const string DataPath = "Data/hero-layered/headgear", TestHat = "Sprites/hero-layered/test/headgear/1";
        const float IconWorldUnits = 12f; // as a 96 px item icon at LootSprites.IconPpu
        static Dictionary<int, HeroLayeredSet.LayerEntry> layers;
        static readonly Dictionary<int, Sprite> Icons = new Dictionary<int, Sprite>();
        static Sprite fallback;

        public static void ResetState() { layers = null; Icons.Clear(); fallback = null; }

        static Dictionary<int, HeroLayeredSet.LayerEntry> Layers()
        {
            if (layers != null) return layers;
            layers = new Dictionary<int, HeroLayeredSet.LayerEntry>();
            try
            {
                TextAsset a = Resources.Load<TextAsset>(DataPath);
                HatFile f = a != null ? JsonUtility.FromJson<HatFile>(a.text) : null;
                if (f != null && f.hats != null) foreach (HeroLayeredSet.LayerEntry e in f.hats) layers[e.id] = e;
                else Debug.LogWarning("[ZenoniaProto] " + DataPath + ".json missing");
            }
            catch (Exception e) { Debug.LogError("[ZenoniaProto] headgear.json: " + e.Message); }
            return layers;
        }

        /// <summary>The head layer entry of a hat whose sheet exists, else null (the head stays bare).</summary>
        public static HeroLayeredSet.LayerEntry Layer(int id)
        {
            HeroLayeredSet.LayerEntry e;
            return id > 0 && Layers().TryGetValue(id, out e) && e.cellW > 0 && SpriteSheet.TryLoad(e.sheet) != null ? e : null;
        }

        /// <summary>Bag / shop / quest / ground icon of a hat item (never null while any fallback exists).</summary>
        public static Sprite Icon(Item item) { return Icon(HeadgearCatalog.IdOf(item)); }

        public static Sprite Icon(int id)
        {
            Sprite s;
            if (Icons.TryGetValue(id, out s) && s != null) return s;
            HeadgearDef d = HeadgearCatalog.Get(id);
            Texture2D icon = d != null ? Resources.Load<Texture2D>("Sprites/icons/headgear-" + d.Key) : null;
            if (icon != null) s = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), new Vector2(0.5f, 0.1f), icon.width / IconWorldUnits, 0, SpriteMeshType.FullRect);
            else
            {
                HeroLayeredSet.LayerEntry e = Layer(id);
                s = e != null ? DownCell(e.sheet, e.cellW, e.cellH) : Fallback();
            }
            Icons[id] = s;
            return s;
        }

        static Sprite Fallback()
        {
            if (fallback != null) return fallback;
            fallback = DownCell(TestHat, 160, 128);
            if (fallback == null) { Texture2D t = Resources.Load<Texture2D>("Sprites/icons/helmet-0"); if (t != null) fallback = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.1f), t.width / IconWorldUnits); }
            return fallback;
        }

        /// <summary>The first (facing down) cell of a 5-cell hat strip, cropped to a centred square so square icon boxes do not stretch it.</summary>
        static Sprite DownCell(string sheet, int cellW, int cellH)
        {
            Texture2D t = SpriteSheet.TryLoad(sheet);
            if (t == null) return null;
            int side = Mathf.Min(cellW, cellH, t.height);
            var rect = new Rect((cellW - side) * 0.5f, t.height - cellH + (cellH - side) * 0.5f, side, side);
            return Sprite.Create(t, rect, new Vector2(0.5f, 0.1f), side / IconWorldUnits, 0, SpriteMeshType.FullRect);
        }
    }
}
