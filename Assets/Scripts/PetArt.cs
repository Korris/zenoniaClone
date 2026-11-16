using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Pictures and colours of the Pet Companion UI: the icon (Sprites/icons/pet-&lt;ArtId&gt;.png, tinted like the pet: six pets borrow Spirit's / Imp's picture until theirs is drawn),
    /// rarity colours N grey / R green / SR blue / SSR purple / UR gold, and the star text.
    /// </summary>
    public static class PetArt
    {
        static readonly Dictionary<string, Sprite> Icons = new Dictionary<string, Sprite>();

        public static Sprite Icon(PetDef d)
        {
            if (d == null) return null;
            Sprite s;
            if (Icons.TryGetValue(d.ArtId, out s)) return s;
            var tex = Resources.Load<Texture2D>("Sprites/icons/pet-" + d.ArtId);
            if (tex == null) tex = Resources.Load<Texture2D>("Sprites/pets/" + d.ArtId + "/art");
            s = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect) : null;
            Icons[d.ArtId] = s;
            return s;
        }

        /// <summary>The Pet Ball / Rare Ball picture (Sprites/icons/pet-ball, pet-ball-rare), null when missing.</summary>
        public static Sprite BallIcon(bool rare) { return Flat(rare ? "pet-ball-rare" : "pet-ball"); }
        /// <summary>The shared-shard picture of a gacha shard result.</summary>
        public static Sprite ShardIcon() { return Flat("pet-shard"); }

        static Sprite Flat(string name)
        {
            Sprite s;
            if (Icons.TryGetValue("#" + name, out s)) return s;
            var tex = Resources.Load<Texture2D>("Sprites/icons/" + name);
            s = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect) : null;
            Icons["#" + name] = s;
            return s;
        }

        public static Color TintOf(PetDef d)
        {
            if (d == null) return Color.white;
            uint c = d.Tint;
            return new Color(((c >> 16) & 255) / 255f, ((c >> 8) & 255) / 255f, (c & 255) / 255f, 1f);
        }

        public static Color RarityColor(PetRarity r)
        {
            switch (r)
            {
                case PetRarity.R: return new Color(0.45f, 0.9f, 0.45f);
                case PetRarity.SR: return new Color(0.4f, 0.7f, 1f);
                case PetRarity.SSR: return new Color(0.78f, 0.5f, 1f);
                case PetRarity.UR: return new Color(1f, 0.82f, 0.3f);
                default: return new Color(0.78f, 0.8f, 0.84f);
            }
        }

        public static string RarityHex(PetRarity r) { return "#" + ColorUtility.ToHtmlStringRGB(RarityColor(r)); }

        /// <summary>Colour of a pet's label: its rarity, or a warm bronze for the Legacy pets outside the gacha.</summary>
        public static Color LabelColor(PetDef d) { return d != null && d.Legacy ? new Color(0.9f, 0.68f, 0.45f) : RarityColor(d != null ? d.Rarity : PetRarity.N); }

        /// <summary>"&lt;color=#..&gt;SR&lt;/color&gt;" / Legacy, for UI texts.</summary>
        public static string Label(PetDef d) { return "<color=#" + ColorUtility.ToHtmlStringRGB(LabelColor(d)) + ">" + PetConfig.RarityLabel(d) + "</color>"; }

        /// <summary>"★★☆" style with plain characters every font has: "3*" reads better at icon size than glyphs a fallback font may not have.</summary>
        public static string Stars(int stars) { return stars <= 0 ? "" : stars + "*"; }

        /// <summary>The colour of a skill's FX by element.</summary>
        public static Color SkillColor(PetSkillKind k)
        {
            switch (k)
            {
                case PetSkillKind.Fire: return new Color(1f, 0.55f, 0.2f);
                case PetSkillKind.Ice: return new Color(0.55f, 0.85f, 1f);
                case PetSkillKind.Poison: return new Color(0.5f, 0.95f, 0.35f);
                case PetSkillKind.Lightning: return new Color(1f, 0.95f, 0.4f);
                case PetSkillKind.Heal: return new Color(0.55f, 1f, 0.6f);
                default: return new Color(0.6f, 0.9f, 1f); // Shield
            }
        }
    }
}
