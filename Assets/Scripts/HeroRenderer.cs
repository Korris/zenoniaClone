using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Paper-doll hero for every class: gear-less "bare" body + overlays (sword + shield / staff / gun) placed at per-frame grip anchors
    /// (mirrors the web held-weapon-renderer). Frames with no bare sheet or a fallback anchor draw the baked armed frame instead.
    /// Direction index: 0 down, 1 up, 2 left, 3 right (sheet row; classes list the anims whose left row is a mirrored right row).
    /// Each part shows its own equipped item (SetEquipmentGear: tier from the item level, enhanced art from +7, HeldEnhanceFx escalating per +7/+9/+11/+13/+15 band);
    /// SetGear is the Q / E / R debug look that applies one tier to every part.
    /// </summary>
    public class HeroRenderer : MonoBehaviour
    {
        public static readonly string[] DirNames = { "down", "up", "left", "right" };
        static readonly string[] AnimNames = { "idle", "walk", "attack1", "attack2", "attack3", "dash", "hurt", "death" };

        sealed class Part
        {
            public GearPartSpec Spec; public SpriteRenderer Sr; public HeldEnhanceFx Fx; public int Tier, Band; public bool Enhanced, Empty;
            public OverlayEntry ArtEntry; public bool ArtEnhanced; public Sprite Art; // resolved art, so the resource path is not rebuilt per frame
        }

        readonly Dictionary<string, Sprite[][]> armed = new Dictionary<string, Sprite[][]>();
        readonly Dictionary<string, Sprite[][]> bare = new Dictionary<string, Sprite[][]>();
        readonly List<Part> parts = new List<Part>(2);
        readonly HeldAnchor[] anchorBuffer = new HeldAnchor[4];
        HeroClassDef cls;
        HeldGearData gear;
        SpriteRenderer body, shadow;
        Transform lean;
        const float LeanDegrees = 6f, LeanSideDip = 0.02f, LeanFrontDip = 0.05f, LeanInSeconds = 0.06f, LeanOutSeconds = 0.09f;
        float leanAngle, leanDip;
        int tier;
        bool enhanced, debugLook;
        int companionTier, companionBand;
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
        public int CompanionBand { get { return companionBand; } }
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
            if (cls.ExtraAnims != null) names.AddRange(cls.ExtraAnims);
            foreach (string anim in names)
            {
                armed[anim] = SpriteSheet.Grid("Sprites/" + cls.SheetFolder + "/" + anim, GameConfig.FramePx, GameConfig.FramePx, GameConfig.FramePpu, pivot);
                if (System.Array.IndexOf(cls.BareAnims, anim) >= 0 || anim.StartsWith("melee") || (cls.ExtraAnims != null && System.Array.IndexOf(cls.ExtraAnims, anim) >= 0))
                    bare[anim] = SpriteSheet.Grid("Sprites/" + cls.BareFolder + "/" + anim, GameConfig.FramePx, GameConfig.FramePx, GameConfig.FramePpu, pivot);
            }
            shadow = ProceduralSprites.Child(transform, "Shadow", ProceduralSprites.Shadow());
            shadow.transform.localScale = new Vector3(12f, 12f, 1f); // 12 x 6 units, web radius 6 x 2.5 (slightly taller reads better)
            shadow.color = new Color(1f, 1f, 1f, 0.3f);
            // Draw order within an entity (stride 8): shadow 0, behind parts 1-2, body 3, front parts 4-5, enhance stars + motes 6 (halo and shine share their part's order).
            var parts0 = new List<Part>();
            lean = new GameObject("Lean").transform; // pivots on the feet; the shadow stays out of it
            lean.SetParent(transform, false);
            foreach (GearPartSpec spec in cls.Parts)
                parts0.Add(new Part { Spec = spec, Sr = ProceduralSprites.Child(lean, spec.Id, null) });
            body = ProceduralSprites.Child(lean, "Body", null);
            parts.AddRange(parts0);
        }

        /// <summary>Raises the whole figure (body + gear) off the ground, or steps it sideways; the shadow stays down.</summary>
        public void Lift(float units, float sideways = 0f) { lean.localPosition = new Vector3(sideways, units, 0f); }

        /// <summary>Eases the whole figure (body + gear) into its walking lean, or back upright: tips toward the side it walks to, dips when walking to or from the camera.</summary>
        public void Lean(bool walking, int dir, float dt)
        {
            float wantAngle = !walking ? 0f : dir == 2 ? LeanDegrees : dir == 3 ? -LeanDegrees : 0f;
            float wantDip = !walking ? 0f : dir == 2 || dir == 3 ? LeanSideDip : LeanFrontDip;
            float k = 1f - Mathf.Exp(-dt / (walking ? LeanInSeconds : LeanOutSeconds));
            leanAngle += (wantAngle - leanAngle) * k;
            leanDip += (wantDip - leanDip) * k;
            lean.localRotation = Quaternion.Euler(0f, 0f, leanAngle);
            lean.localScale = new Vector3(1f, 1f - leanDip, 1f);
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
            companionTier = tier; companionEnhanced = enhanced; companionBand = enhanced ? 1 : 0; companionEmpty = false;
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
            companionBand = EnhanceStats.VisualBand(offhand);
            companionEnhanced = companionBand > 0;
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
                foreach (Part p in parts) { p.Sr.sprite = null; if (p.Fx != null) p.Fx.Hide(); }
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
            OverlayEntry entry = (a != null && !a.hidden && !p.Empty && manifest != null && p.Tier < manifest.Length) ? manifest[p.Tier] : null;
            if (entry != null && (entry != p.ArtEntry || p.Enhanced != p.ArtEnhanced))
            {
                p.ArtEntry = entry;
                p.ArtEnhanced = p.Enhanced;
                p.Art = GearOverlays.Get(p.Spec.Folder, entry, p.Enhanced);
            }
            Sprite sprite = entry != null ? p.Art : null;
            p.Sr.sprite = sprite;
            if (sprite == null) { if (p.Fx != null) p.Fx.Hide(); return; }
            p.Sr.color = tint;
            p.Sr.sortingOrder = a.behind ? orderBehind : orderFront;
            float k = a.s * GearOverlays.FrameUnit * p.Spec.DrawScale * GearOverlays.Ppu; // sprite scale: world units per overlay px / (1 / ppu)
            float sx = Mathf.Sign(a.sx == 0f ? 1f : a.sx) * Mathf.Max(Mathf.Abs(a.sx), p.Spec.MinSx);
            Transform t = p.Sr.transform;
            t.localPosition = new Vector3((a.x - GameConfig.FramePx * 0.5f) * GearOverlays.FrameUnit, (GameConfig.FramePx - a.y) * GearOverlays.FrameUnit, 0f);
            t.localRotation = Quaternion.Euler(0f, 0f, -a.angle); // web angle is clockwise on a y-down canvas
            t.localScale = new Vector3(k * sx, k, 1f);
            // Enhanced (+7 and up): the blade keeps its painted colours, HeldEnhanceFx lights it per band in the tier colour.
            if (!p.Enhanced || !HeldEnhanceFx.Enabled) { if (p.Fx != null) p.Fx.Hide(); return; }
            if (p.Fx == null) p.Fx = new HeldEnhanceFx(p.Sr, transform, !p.Spec.Glow ? 0f : p.Spec.IsOffhand ? 0.5f : 1f, 1f);
            p.Fx.Draw(p.Spec.Folder, entry, p.Band, p.Sr.sortingOrder, orderBase + 6, a.behind, alpha);
        }
    }
}
