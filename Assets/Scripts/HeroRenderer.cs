using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Paper-doll hero for every class: gear-less "bare" body + overlays (sword + shield / staff / gun) placed at per-frame grip anchors
    /// (mirrors the web held-weapon-renderer). Frames with no bare sheet or a fallback anchor draw the baked armed frame instead.
    /// Direction index: 0 down, 1 up, 2 left, 3 right (sheet row; classes list the anims whose left row is a mirrored right row).
    /// Each part shows its own equipped item (SetEquipmentGear: tier from the item level, enhanced art from +7, brighter glow per +7/+9/+11/+13 band);
    /// SetGear is the Q / E / R debug look that applies one tier to every part.
    /// </summary>
    public class HeroRenderer : MonoBehaviour
    {
        public static readonly string[] DirNames = { "down", "up", "left", "right" };
        static readonly string[] AnimNames = { "idle", "walk", "attack1", "attack2", "attack3", "dash", "hurt", "death" };

        sealed class Part { public GearPartSpec Spec; public SpriteRenderer Sr, Glow; public int Tier, Band; public bool Enhanced, Empty; }

        readonly Dictionary<string, Sprite[][]> armed = new Dictionary<string, Sprite[][]>();
        readonly Dictionary<string, Sprite[][]> bare = new Dictionary<string, Sprite[][]>();
        readonly List<Part> parts = new List<Part>(2);
        readonly HeldAnchor[] anchorBuffer = new HeldAnchor[4];
        HeroClassDef cls;
        HeldGearData gear;
        SpriteRenderer body, shadow;
        int tier;
        bool enhanced, debugLook;
        int companionTier;
        bool companionEnhanced, companionEmpty;

        public HeroClassDef Class { get { return cls; } }
        public HeldGearData Gear { get { return gear; } }
        public int Tier { get { return tier; } }
        public bool Enhanced { get { return enhanced; } }
        /// <summary>True after a Q / E / R debug override, until the equipment is synced again.</summary>
        public bool DebugLook { get { return debugLook; } }
        /// <summary>Companion art (mechanic robot / cleric spirit) follows the offhand item; Empty = no offhand equipped.</summary>
        public int CompanionTier { get { return companionTier; } }
        public bool CompanionEnhanced { get { return companionEnhanced; } }
        public bool CompanionEmpty { get { return companionEmpty; } }
        public string TierName
        {
            get
            {
                OverlayEntry[] m = gear != null ? gear.Manifest(cls.Parts[0].Id) : null;
                return m != null && tier < m.Length ? m[tier].name : "-";
            }
        }

        public void Init(HeroClassDef classDef, HeldGearData gearData)
        {
            cls = classDef;
            gear = gearData; // null => baked armed frames only (still playable)
            var pivot = new Vector2(0.5f, 0f); // feet = bottom centre of the frame
            var names = new List<string>(AnimNames);
            if (cls.ComboAnims != null) foreach (string a in cls.ComboAnims) if (!names.Contains(a)) names.Add(a);
            foreach (string anim in names)
            {
                armed[anim] = SpriteSheet.Grid("Sprites/" + cls.SheetFolder + "/" + anim, GameConfig.FramePx, GameConfig.FramePx, GameConfig.FramePpu, pivot);
                if (System.Array.IndexOf(cls.BareAnims, anim) >= 0 || anim.StartsWith("melee"))
                    bare[anim] = SpriteSheet.Grid("Sprites/" + cls.BareFolder + "/" + anim, GameConfig.FramePx, GameConfig.FramePx, GameConfig.FramePpu, pivot);
            }
            shadow = ProceduralSprites.Child(transform, "Shadow", ProceduralSprites.Shadow());
            shadow.transform.localScale = new Vector3(12f, 12f, 1f); // 12 x 6 units, web radius 6 x 2.5 (slightly taller reads better)
            shadow.color = new Color(1f, 1f, 1f, 0.3f);
            // Draw order within an entity (stride 8): shadow 0, behind parts 1-2, body 3, front parts 4-5, glow 6.
            var parts0 = new List<Part>();
            foreach (GearPartSpec spec in cls.Parts)
            {
                var part = new Part { Spec = spec, Sr = ProceduralSprites.Child(transform, spec.Id, null) };
                if (spec.Glow)
                {
                    part.Glow = ProceduralSprites.Child(part.Sr.transform, spec.Id + "Glow", null); // additive copy: bright pixels of the enhanced art bloom
                    part.Glow.sharedMaterial = FeelMaterials.Additive;
                }
                parts0.Add(part);
            }
            body = ProceduralSprites.Child(transform, "Body", null);
            parts.AddRange(parts0);
        }

        /// <summary>Sheet row for a direction + animation; mirror = draw the right row flipped.</summary>
        int RowFor(string anim, int dir, out bool mirror)
        {
            mirror = dir == 2 && cls.MirrorLeft.Contains(anim);
            return mirror ? 3 : dir;
        }

        public int FrameCount(string anim, int dir)
        {
            bool mirror;
            Sprite[] row;
            return armed.ContainsKey(anim) && (row = SpriteSheet.Row(armed[anim], RowFor(anim, dir, out mirror))) != null ? row.Length : 0;
        }

        public void SetGear(int newTier, bool newEnhanced)
        {
            int max = gear != null ? gear.TierCount : 1;
            tier = ((newTier % max) + max) % max;
            enhanced = newEnhanced;
            debugLook = true;
            foreach (Part p in parts) { p.Tier = tier; p.Enhanced = enhanced; p.Band = enhanced ? 1 : 0; p.Empty = false; }
            companionTier = tier; companionEnhanced = enhanced; companionEmpty = false;
        }

        /// <summary>Paper doll from the equipment: every part draws its own slot's item (null = nothing in that hand).</summary>
        public void SetEquipmentGear(Item weapon, Item offhand)
        {
            debugLook = false;
            foreach (Part p in parts)
            {
                Item item = p.Spec.IsOffhand ? offhand : weapon;
                p.Empty = item == null;
                p.Tier = ClampTier(p.Spec.Id, ItemInfo.ArtTier(item));
                p.Band = EnhanceStats.VisualBand(item);
                p.Enhanced = p.Band > 0;
            }
            Part main = parts.Find(p => !p.Spec.IsOffhand) ?? (parts.Count > 0 ? parts[0] : null);
            tier = main != null ? main.Tier : 0;
            enhanced = main != null && main.Enhanced;
            companionEmpty = offhand == null;
            companionTier = ItemInfo.ArtTier(offhand);
            companionEnhanced = EnhanceStats.VisualBand(offhand) > 0;
        }

        int ClampTier(string part, int t)
        {
            OverlayEntry[] m = gear != null ? gear.Manifest(part) : null;
            return m != null && m.Length > 0 ? Mathf.Clamp(t, 0, m.Length - 1) : 0;
        }

        /// <summary>Draws one frame. orderBase = GameConfig.OrderBase(feetY).</summary>
        public void Show(string anim, int dir, int frame, int orderBase, float alpha)
        {
            bool mirror;
            int row = RowFor(anim, dir, out mirror);
            Sprite[] armedRow;
            if (!armed.ContainsKey(anim) || (armedRow = SpriteSheet.Row(armed[anim], row)) == null || armedRow.Length == 0)
            {
                body.sprite = null;
                foreach (Part p in parts) { p.Sr.sprite = null; if (p.Glow != null) p.Glow.enabled = false; }
                return;
            }
            frame = Mathf.Clamp(frame, 0, armedRow.Length - 1);
            Sprite bodySprite = armedRow[frame];
            bool useGear = false;
            Sprite[] bareRow = gear != null && bare.ContainsKey(anim) ? SpriteSheet.Row(bare[anim], row) : null;
            if (bareRow != null && frame < bareRow.Length)
            {
                useGear = true;
                for (int i = 0; i < parts.Count; i++)
                {
                    HeldAnchor a = gear.Get(parts[i].Spec.Id, anim, DirNames[dir], frame);
                    // Web: any missing / fallback anchor => the whole frame falls back to the baked armed art.
                    if (a == null || a.fallback) { useGear = false; break; }
                    anchorBuffer[i] = a;
                }
                if (useGear) bodySprite = bareRow[frame];
            }

            var tint = new Color(1f, 1f, 1f, alpha);
            shadow.sortingOrder = orderBase;
            shadow.color = new Color(1f, 1f, 1f, 0.3f * alpha);
            body.sprite = bodySprite;
            body.flipX = mirror;
            body.color = tint;
            body.sortingOrder = orderBase + 3;
            for (int i = 0; i < parts.Count; i++)
                ApplyPart(parts[i], useGear ? anchorBuffer[i] : null, orderBase + 1 + i, orderBase + 4 + i, orderBase, tint, alpha);
        }

        void ApplyPart(Part p, HeldAnchor a, int orderBehind, int orderFront, int orderBase, Color tint, float alpha)
        {
            OverlayEntry[] manifest = gear != null ? gear.Manifest(p.Spec.Id) : null;
            Sprite sprite = (a != null && !a.hidden && !p.Empty && manifest != null && p.Tier < manifest.Length) ? GearOverlays.Get(p.Spec.Folder, manifest[p.Tier], p.Enhanced) : null;
            p.Sr.sprite = sprite;
            if (sprite == null) { if (p.Glow != null) p.Glow.enabled = false; return; }
            p.Sr.color = tint;
            p.Sr.sortingOrder = a.behind ? orderBehind : orderFront;
            float k = a.s * GearOverlays.FrameUnit * p.Spec.DrawScale * GearOverlays.Ppu; // sprite scale: world units per overlay px / (1 / ppu)
            float sx = Mathf.Sign(a.sx == 0f ? 1f : a.sx) * Mathf.Max(Mathf.Abs(a.sx), p.Spec.MinSx);
            Transform t = p.Sr.transform;
            t.localPosition = new Vector3((a.x - GameConfig.FramePx * 0.5f) * GearOverlays.FrameUnit, (GameConfig.FramePx - a.y) * GearOverlays.FrameUnit, 0f);
            t.localRotation = Quaternion.Euler(0f, 0f, -a.angle); // web angle is clockwise on a y-down canvas
            t.localScale = new Vector3(k * sx, k, 1f);
            if (p.Glow == null) return;
            // Enhanced art only, and only when drawn in front of the body (sorting slot +6 is unused by the other parts).
            bool glow = p.Enhanced && FeelConfig.FxOn && !a.behind;
            p.Glow.enabled = glow;
            if (!glow) return;
            p.Glow.sprite = sprite;
            p.Glow.sortingOrder = orderBase + 6;
            float bandAlpha = Mathf.Min(1f, FeelConfig.EnhancedGlowAlpha + 0.15f * (Mathf.Max(1, p.Band) - 1)); // +7 / +9 / +11 / +13 glow brighter
            p.Glow.color = new Color(1f, 0.85f, 0.6f, bandAlpha * alpha);
        }
    }
}
