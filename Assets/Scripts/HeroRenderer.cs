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
    public partial class HeroRenderer : MonoBehaviour
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
        readonly Dictionary<string, Sprite[][]> hand = new Dictionary<string, Sprite[][]>();
        readonly List<Part> parts = new List<Part>(2);
        readonly HeldAnchor[] anchorBuffer = new HeldAnchor[4];
        HeroClassDef cls;
        HeldGearData gear;
        Sprite[] bigIdleDown;
        HeroDashTrail dashTrail;
        SpriteRenderer body, shadow, handOver;
        Transform lean;
        const float LeanDegrees = 6f, LeanSideDip = 0.02f, LeanFrontDip = 0.05f, LeanInSeconds = 0.06f, LeanOutSeconds = 0.09f;
        float leanAngle, leanDip;
        int tier, motion; // motion: 0 idle, 1 walk, 2 any action, for the enhance effect (HeldEnhanceFx.Draw)
        bool enhanced, debugLook;
        int companionTier, companionBand;
        // Outfit (ArmorLooks): the worn armour's tier picks a whole body: body/<outfit>/bare + hand sheets; rows not painted yet are the Lv1 rows.
        int outfit;
        readonly Dictionary<string, Sprite[][]> outfitBare = new Dictionary<string, Sprite[][]>();
        readonly Dictionary<string, Sprite[][]> outfitHand = new Dictionary<string, Sprite[][]>();
        bool companionEnhanced, companionEmpty;
        HeroHeadLayer headLayer; // null = this class has no separate head art (the whole body is one sprite)

        public HeroClassDef Class { get { return cls; } }
        public HeldGearData Gear { get { return gear; } }
        /// <summary>Glow colour (hex) of the worn main weapon's art tier, for the slash tint; null when bare-handed or without a manifest.</summary>
        public string WeaponGlow
        {
            get
            {
                if (gear == null) return null;
                foreach (Part p in parts)
                {
                    if (p.Spec.IsOffhand || p.Empty) continue;
                    OverlayEntry[] manifest = gear.Manifest(p.Spec.Id);
                    return manifest != null && p.Tier < manifest.Length ? manifest[p.Tier].glow : null;
                }
                return null;
            }
        }
        public int Tier { get { return tier; } }
        public bool Enhanced { get { return enhanced; } }
        /// <summary>True after a Q / E / R debug override, until the equipment is synced again.</summary>
        public bool DebugLook { get { return debugLook; } }
        /// <summary>Companion art (mechanic robot / cleric spirit) follows the offhand item; Empty = no offhand equipped.</summary>
        public int CompanionTier { get { return companionTier; } }
        public bool CompanionEnhanced { get { return companionEnhanced; } }
        public int CompanionBand { get { return companionBand; } }
        public bool CompanionEmpty { get { return companionEmpty; } }
        /// <summary>Art id of the pet flying with this hero (HeroPet); null = none.</summary>
        public string PetId { get; private set; }
        public void SetPet(string petId) { PetId = petId; }
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
            if (cls.BigIdleDown != null)
            {
                // 288 x 240 cells; the 192 cell's bottom, where the feet are measured from, sits 48 rows up.
                Sprite[][] big = SpriteSheet.Grid("Sprites/" + cls.BigIdleDown, 288, 240, GameConfig.FramePpu, new Vector2(0.5f, 48f / 240f));
                bigIdleDown = big != null ? big[0] : null;
            }
            shadow = ProceduralSprites.Child(transform, "Shadow", ProceduralSprites.Shadow());
            shadow.transform.localScale = new Vector3(12f, 12f, 1f); // 12 x 6 units, web radius 6 x 2.5 (slightly taller reads better)
            shadow.color = new Color(1f, 1f, 1f, 0.3f);
            // Draw order within an entity (stride 8): shadow 0, behind parts 1-2, body 3, front parts 4-5, the fist over the handle and enhance stars + motes 6 (halo and shine share their part's order).
            var parts0 = new List<Part>();
            lean = new GameObject("Lean").transform; // pivots on the feet; the shadow stays out of it
            lean.SetParent(transform, false);
            foreach (GearPartSpec spec in cls.Parts)
                parts0.Add(new Part { Spec = spec, Sr = ProceduralSprites.Child(lean, spec.Id, null) });
            body = ProceduralSprites.Child(lean, "Body", null);
            handOver = ProceduralSprites.Child(lean, "HandOver", null);
            headLayer = HeroHeadLayer.Create(cls, lean);
            if (cls.HandFolder != null)
                foreach (string anim in cls.HandAnims)
                    hand[anim] = SpriteSheet.Grid("Sprites/" + cls.HandFolder + "/" + anim, GameConfig.FramePx, GameConfig.FramePx, GameConfig.FramePpu, pivot);
            parts.AddRange(parts0);
            LoadOutfitAnchors(null);
            var figure = new List<SpriteRenderer> { body, handOver };
            foreach (Part p in parts) figure.Add(p.Sr);
            dashTrail = gameObject.AddComponent<HeroDashTrail>();
            dashTrail.Init(figure.ToArray());
        }

        /// <summary>Hairstyle index (head layer prototype: Lv1 body, idle facing down, every class with heads/ art); wraps; 0 = the painted head. Ignored by classes without head art.</summary>
        public int HairStyle
        {
            get { return headLayer != null ? headLayer.Style : 0; }
            set { if (headLayer != null) headLayer.Style = value; }
        }

        /// <summary>Hair dye cycle index for layered heads (-1 = the style's own colour); local look only.</summary>
        public int HairDye
        {
            get { return headLayer != null ? headLayer.HairDye : -1; }
            set { if (headLayer != null) headLayer.HairDye = value; }
        }

        /// <summary>Custom hair colour for layered hair (any colour; null = the dye preset / the style's own colour); local look only.</summary>
        public Color32? HairColor
        {
            get { return headLayer != null ? headLayer.HairColor : null; }
            set { if (headLayer != null) headLayer.HairColor = value; }
        }

        /// <summary>Outfit of the worn armour's art tier (-1 = none worn = the Lv1 body): see ArmorLooks.OutfitFolder.</summary>
        public void SetOutfit(int armorTier)
        {
            int want = ArmorLooks.OutfitIndex(cls, armorTier);
            if (want == outfit) return;
            outfit = want;
            outfitBare.Clear();
            outfitHand.Clear();
            string folder = ArmorLooks.OutfitFolder(cls, outfit);
            LoadOutfitAnchors(folder);
            if (headLayer != null) headLayer.SetBodyFolder(folder); // the hairstyle head sits on the outfit's headless sheets (Lv1 rows keep the Lv1 headless sheets)
            if (folder == null) return;
            var pivot = new Vector2(0.5f, 0f);
            foreach (string anim in bare.Keys)
            {
                Sprite[][] g = SpriteSheet.Grid("Sprites/" + folder + "/bare/" + anim, GameConfig.FramePx, GameConfig.FramePx, GameConfig.FramePpu, pivot);
                if (g != null) outfitBare[anim] = g;
            }
            foreach (string anim in hand.Keys)
            {
                Sprite[][] g = SpriteSheet.Grid("Sprites/" + folder + "/hand/" + anim, GameConfig.FramePx, GameConfig.FramePx, GameConfig.FramePpu, pivot);
                if (g != null) outfitHand[anim] = g;
            }
        }

        /// <summary>Leaves and fades the dash afterimages; call after the figure is drawn.</summary>
        public void TickDashTrail(bool dashing, float dtMs) { dashTrail.Tick(dashing, dtMs); }

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
            lean.localScale = new Vector3(figureScale, figureScale * (1f - leanDip), 1f);
        }

        float figureScale = 1f;

        /// <summary>Draws the figure (body + gear + enhance effect) this much larger around the feet; the shadow and the hero's footprint stay. Test switch: DevStartConfig.HeroScale.</summary>
        public float FigureScale
        {
            get { return figureScale; }
            set { figureScale = value; if (lean != null) lean.localScale = new Vector3(value, value * (1f - leanDip), 1f); }
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
            SetOutfit(tier); // E / Q debug look: the outfit of the same art tier too
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
            motion = anim == "idle" ? 0 : anim == "walk" ? 1 : 2;
            Sprite[] armedRow;
            if (!armed.ContainsKey(anim) || (armedRow = SpriteSheet.Row(armed[anim], row)) == null || armedRow.Length == 0)
            {
                body.sprite = null;
                handOver.sprite = null;
                if (headLayer != null) headLayer.Hide();
                foreach (Part p in parts) { p.Sr.sprite = null; if (p.Fx != null) p.Fx.Hide(); }
                return;
            }
            frame = Mathf.Clamp(frame, 0, armedRow.Length - 1);
            Sprite bodySprite = armedRow[frame];
            bool useGear = false;
            Sprite[][] bareSheet;
            bool lv1Body = !outfitBare.TryGetValue(anim, out bareSheet); // outfit rows draw the outfit sheet; the head layer uses its headless sheet
            if (lv1Body) bare.TryGetValue(anim, out bareSheet);
            Sprite[] bareRow = gear != null && bareSheet != null ? SpriteSheet.Row(bareSheet, row) : null;
            if (bareRow != null && frame < bareRow.Length)
            {
                useGear = true;
                for (int i = 0; i < parts.Count; i++)
                {
                    HeldAnchor a = OutfitAnchor(parts[i].Spec.Id, anim, DirNames[dir], frame)
                        ?? gear.Get(parts[i].Spec.Id, anim, DirNames[dir], frame);
                    // An optional part (the wizard's spellbook) without a usable anchor is just not drawn this frame.
                    if (parts[i].Spec.Optional && (a == null || a.fallback || a.hidden)) { anchorBuffer[i] = null; continue; }
                    // Web: any missing / fallback anchor => the whole frame falls back to the baked armed art.
                    if (a == null || a.fallback) { useGear = false; break; }
                    anchorBuffer[i] = a;
                }
                if (useGear) bodySprite = bareRow[frame];
            }

            if (!useGear && bigIdleDown != null && dir == 0 && anim == "idle" && frame < bigIdleDown.Length) bodySprite = bigIdleDown[frame]; // art wider than the 192 cell

            var tint = new Color(1f, 1f, 1f, alpha);
            shadow.sortingOrder = orderBase;
            shadow.color = new Color(1f, 1f, 1f, 0.3f * alpha);
            // Head layer: the Lv1 bare body of a frame with head art draws headless, the chosen hairstyle's head on top (same order, nearer z).
            Sprite headless = useGear && headLayer != null ? headLayer.Apply(anim, DirNames[dir], row, frame, mirror, orderBase + 3, tint, !lv1Body) : null;
            if (headless != null) bodySprite = headless;
            else if (headLayer != null) headLayer.Hide();
            body.sprite = bodySprite;
            body.flipX = mirror;
            body.color = tint;
            body.sortingOrder = orderBase + 3;
            Sprite[][] handSheet;
            if (!outfitHand.TryGetValue(anim, out handSheet)) hand.TryGetValue(anim, out handSheet);
            Sprite[] handRow = useGear && handSheet != null ? SpriteSheet.Row(handSheet, row) : null;
            handOver.sprite = handRow != null && frame < handRow.Length ? handRow[frame] : null;
            handOver.flipX = mirror;
            handOver.color = tint;
            handOver.sortingOrder = orderBase + 6;
            for (int i = 0; i < parts.Count; i++)
                ApplyPart(parts[i], useGear ? anchorBuffer[i] : null, frame, orderBase + 1 + Mathf.Min(i, 1), orderBase + 4 + Mathf.Min(i, 1), orderBase, tint, alpha); // two slots each side of the body: a third part shares the second, the two hands do not cross
        }

        void ApplyPart(Part p, HeldAnchor a, int frame, int orderBehind, int orderFront, int orderBase, Color tint, float alpha)
        {
            OverlayEntry[] manifest = gear != null ? gear.Manifest(p.Spec.Id) : null;
            OverlayEntry entry = (a != null && !a.hidden && !p.Empty && manifest != null && p.Tier < manifest.Length) ? manifest[p.Tier] : null;
            bool ownArt = entry != null && !string.IsNullOrEmpty(a.art);
            Sprite sprite = null;
            if (ownArt) sprite = GearOverlays.CenteredTier(p.Spec.Folder, a.art, entry.name); // art named by the anchor (the mechanic's cannon and pistol views): no <tier>-base path is built for it, that file does not exist
            else if (entry != null)
            {
                if (entry != p.ArtEntry || p.Enhanced != p.ArtEnhanced)
                {
                    p.ArtEntry = entry;
                    p.ArtEnhanced = p.Enhanced;
                    p.Art = GearOverlays.Get(p.Spec.Folder, entry, p.Enhanced && !FeelConfig.EnhanceUsesBaseArt);
                }
                sprite = p.Art;
            }
            p.Sr.sprite = sprite;
            if (sprite == null) { if (p.Fx != null) p.Fx.Hide(); return; }
            p.Sr.color = tint;
            p.Sr.sortingOrder = a.behind ? orderBehind : orderFront;
            float k = a.s * GearOverlays.FrameUnit * p.Spec.DrawScale * GearOverlays.Ppu; // sprite scale: world units per overlay px / (1 / ppu)
            float sx = Mathf.Sign(a.sx == 0f ? 1f : a.sx) * Mathf.Max(Mathf.Abs(a.sx), p.Spec.MinSx);
            Transform t = p.Sr.transform;
            float bob = motion == 0 && !ownArt && entry.bobY != null && frame >= 0 && frame < entry.bobY.Length ? entry.bobY[frame] : 0f; // per-tier per-frame lift of the idle breathing (wizard book follows the tier's fist); attack frames have their own anchors
            t.localPosition = new Vector3((a.x - GameConfig.FramePx * 0.5f) * GearOverlays.FrameUnit, (GameConfig.FramePx - a.y - bob - a.artY) * GearOverlays.FrameUnit, 0f);
            t.localRotation = Quaternion.Euler(0f, 0f, -a.angle); // web angle is clockwise on a y-down canvas
            t.localScale = new Vector3(k * sx, k, 1f);
            // Enhanced (+7 and up): the blade keeps its painted colours, HeldEnhanceFx lights it per band in the tier colour.
            if (!p.Enhanced || !HeldEnhanceFx.Enabled) { if (p.Fx != null) p.Fx.Hide(); return; }
            if (p.Fx == null) p.Fx = new HeldEnhanceFx(p.Sr, transform, !p.Spec.Glow ? 0f : p.Spec.IsOffhand ? 0.5f : 1f, 1f, !FeelConfig.EnhanceUsesBaseArt);
            p.Fx.Draw(p.Spec.Folder, entry, p.Band, p.Sr.sortingOrder, orderBase + 6, a.behind, alpha, ownArt ? sprite : null, motion);
        }
    }
}
