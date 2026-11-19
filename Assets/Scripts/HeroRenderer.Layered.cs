using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Layered hero path (docs/character-appearance-layered-spec.md, step B): body + mask recoloured by HeroRecolor.shader, hair, headgear,
    /// weapon / offhand on per-frame grips, fist (HeroLayeredView), 8 view dirs like the RO Soft path. Step D: the default style for every class
    /// with real art (Data/hero-layered/&lt;class&gt;/class.json, Tools/build-hero-layered-game-assets.py), local hero and others alike; the class's
    /// weapon / offhand on per-frame grips (Tools/build-hero-layered-grips.py), each hidden only when that hand has nothing equipped.
    /// `--hero-style classic|rosoft` turns it off; `--layered-test` draws every non-local
    /// hero with the synthetic set (Tools/build-hero-recolor-mask.py test-sheets). Appearance (CharacterAppearanceData) picks the palette colours; heroes other than the local one
    /// get a small crowd variation from their seed: walk phase and breath offset, figure scale 0.96-1.04.
    /// </summary>
    public partial class HeroRenderer
    {
        public const string LayeredTestSet = "Data/hero-layered/test/class";
        const float CrowdScaleMin = 0.96f, CrowdScaleRange = 0.08f;

        static bool? layeredTest;
        /// <summary>`--layered-test` on the command line (or set by a capture before heroes spawn).</summary>
        public static bool LayeredTestEnabled
        {
            get { if (layeredTest == null) layeredTest = Array.IndexOf(Environment.GetCommandLineArgs(), "--layered-test") >= 0; return layeredTest.Value; }
            set { layeredTest = value; }
        }

        HeroLayeredView layered;
        float crowdScale = 1f, crowdPhase;

        public const string RoFantasyRoot = "Data/hero-layered/ro-fantasy/";
        static readonly System.Collections.Generic.Dictionary<string, bool> roFantasyHas = new System.Collections.Generic.Dictionary<string, bool>();

        /// <summary>
        /// Class json of a class's layered set, or null when the layered style is off. Default: the class's own RO Fantasy body
        /// (Data/hero-layered/ro-fantasy/&lt;class&gt;/class, Tools/build-ro-fantasy-hero-game-assets.py: body per class, fixed outfit, 3 dyeable
        /// hairstyles) when it exists, else the common gender body (Tools/build-hero-layered-game-assets.py). `--hero-style layered-common` = the common body.
        /// </summary>
        public static string LayeredSetFor(HeroClassDef def)
        {
            if (def == null || !LayeredStyleEnabled) return null;
            string cls = def.Name.ToLowerInvariant();
            if (RoFantasyStyleEnabled)
            {
                bool has;
                if (!roFantasyHas.TryGetValue(cls, out has)) roFantasyHas[cls] = has = Resources.Load<TextAsset>(RoFantasyRoot + cls + "/class") != null;
                if (has) return RoFantasyRoot + cls + "/class";
            }
            return "Data/hero-layered/" + cls + "/class";
        }

        /// <summary>The local player's resolved look (last SetAppearance with local = true), for views of the same hero (bag portrait).</summary>
        public static CharacterAppearanceData LocalLook { get; private set; }

        /// <summary>True when this hero draws a layered set.</summary>
        public bool IsLayered { get { return layered != null; } }

        /// <summary>The look in use, colours resolved (never null after SetAppearance).</summary>
        public CharacterAppearanceData Appearance { get; private set; }

        /// <summary>Draws this hero from a layered set from now on; false (nothing changes) when the set is missing.</summary>
        public bool UseLayered(string classJson)
        {
            HeroLayeredSet set = classJson != null ? HeroLayeredSet.Load(classJson) : null;
            if (set == null || lean == null) return false;
            if (layered == null) layered = new HeroLayeredView(set, lean);
            ApplyLookToLayers();
            return true;
        }

        /// <summary>
        /// The character's look: palette colours (missing ones picked from `seed`, CharacterAppearanceData.SeedFor), hairstyle, headgear.
        /// classicHair: also dye the paper doll's layered hair with a CHOSEN palette colour (other players; the local hero keeps its device dye).
        /// local: the player's own hero, never varied.
        /// </summary>
        public void SetAppearance(CharacterAppearanceData raw, string seed, bool local, bool classicHair)
        {
            if (raw == null) raw = CharacterAppearanceData.Unset(cls != null ? cls.Name : null);
            Appearance = raw.Resolved(seed);
            if (local) LocalLook = Appearance;
            if (classicHair && raw.HasHairColor) HairColor = HeroPalettes.Hair(raw.hairColorIndex);
            crowdScale = local ? 1f : CrowdScaleMin + CrowdScaleRange * CharacterAppearanceData.Unit(seed, 1);
            crowdPhase = local ? 0f : CharacterAppearanceData.Unit(seed, 2);
            if (!local) breathPhase = crowdPhase * BreathPeriod;
            ApplyLookToLayers();
        }

        /// <summary>The worn hat changed (equip / unequip, ArmorLooks.FromWorn): only the headgear id of the look in use changes.</summary>
        public void SetHeadgear(int id)
        {
            if (Appearance == null || Appearance.equippedHeadgearId == id) return;
            Appearance.equippedHeadgearId = id;
            ApplyLookToLayers();
        }

        void ApplyLookToLayers()
        {
            if (layered == null || Appearance == null) return;
            layered.SetLook(HeroPalettes.Hair(Appearance.hairColorIndex), HeroPalettes.Outfit(Appearance.outfitColorIndex), Appearance.hairStyle, Appearance.equippedHeadgearId);
        }

        /// <summary>Figure scale with the crowd variation (layered heroes only: the other paths keep their exact size).</summary>
        float FigureK { get { return figureScale * (layered != null ? crowdScale : 1f); } }

        int LayeredFrameCount(string anim)
        {
            if (anim == null) return 0;
            if (anim == "death") return DeathFadeSteps;
            if (StandIn(anim, LayeredAnim(anim))) return StandInFrames;
            return layered.Set.Frames(LayeredAnim(anim));
        }

        // Stand-in motion (RO Fantasy class bodies before their walk / attack art exists): the idle frame hops and tilts while walking, leans
        // back then lunges toward the facing on attacks. Per frame, cell px / degrees.
        const int StandInFrames = 4;
        static readonly float[] WalkHopPx = { 0f, 3f, 0f, 3f }, WalkTiltDeg = { 2.5f, 0f, -2.5f, 0f };
        static readonly float[] AttackLungePx = { -3f, 5f, 7f, 2f }, AttackLeanDeg = { -3f, 6f, 8f, 2f };
        static readonly float[] FaceX = { 0f, 0.7f, 1f, 0.7f, 0f, -0.7f, -1f, -0.7f }, FaceY = { -1f, -0.7f, 0f, 0.7f, 1f, 0.7f, 0f, -0.7f };

        /// <summary>A moving game anim drawn with the idle frame because the set has no art for it yet.</summary>
        static bool StandIn(string anim, string painted)
        {
            return painted == "idle" && anim != null && anim != "idle" && anim != "hurt" && anim != "death";
        }

        /// <summary>(shift in local units, tilt in degrees) of the stand-in motion at frame f.</summary>
        void StandInPose(string anim, int dir8, int f, out Vector2 shift, out float tilt)
        {
            float ppu = layered.Set.Ppu;
            f = ((f % StandInFrames) + StandInFrames) % StandInFrames;
            if (anim == "walk" || anim == "dash")
            {
                shift = new Vector2(0f, WalkHopPx[f] / ppu);
                tilt = WalkTiltDeg[f];
                return;
            }
            int d = Mathf.Clamp(dir8, 0, 7);
            shift = new Vector2(FaceX[d], FaceY[d] * 0.4f) * (AttackLungePx[f] / ppu);
            tilt = -FaceX[d] * AttackLeanDeg[f]; // lean toward the side it strikes (clockwise = right)
        }

        /// <summary>
        /// Painted anim of a game anim: idle (idle, hurt, death), walk (walk, dash), combo step N -> attack1/2/3 (step 4 cycles back to attack1),
        /// any other action (skill / cast pose, spin) -> attack2 (the thrust / point), else what the set resolves (test set: attack1 only).
        /// </summary>
        string LayeredAnim(string anim)
        {
            HeroLayeredSet set = layered.Set;
            if (anim == null || anim == "idle" || anim == "hurt" || anim == "death" || anim == "walk" || anim == "dash") return set.Resolve(anim);
            if (set.HasAnim("attack") && !set.HasAnim("attack1")) return "attack"; // RO Fantasy bodies: one lock-target attack for every combo step / skill pose
            int step = cls.ComboAnims != null ? Array.IndexOf(cls.ComboAnims, anim) : -1;
            string want = step >= 0 ? "attack" + (step % 3 + 1) : set.HasAnim(anim) ? anim : "attack2";
            return set.HasAnim(want) ? want : set.Resolve(anim);
        }

        /// <summary>The main hand holds something: a non-offhand part with an item (SetEquipmentGear / SetGear), or no paper-doll parts at all.</summary>
        bool LayeredWeaponOn { get { return parts.Count == 0 || parts.Exists(p => !p.Spec.IsOffhand && !p.Empty); } }

        /// <summary>A skill pose's frame is counted on its classic sheet: spread it over the painted attack's frames.</summary>
        int PoseFrame(string anim, string painted, int frame, int n)
        {
            if (painted == anim || (cls.ComboAnims != null && Array.IndexOf(cls.ComboAnims, anim) >= 0)) return frame;
            Sprite[][] g;
            int orig = armed.TryGetValue(anim, out g) && g != null && g.Length > 0 && g[0] != null ? g[0].Length : 0;
            return orig > n ? frame * n / orig : frame;
        }

        void ShowLayered(string anim, int dir, int frame, int orderBase, float alpha)
        {
            body.sprite = null;
            handOver.sprite = null;
            if (headLayer != null) headLayer.Hide();
            foreach (Part p in parts) { p.Sr.sprite = null; if (p.Fx != null) p.Fx.Hide(); }
            motion = anim == "idle" ? 0 : anim == "walk" ? 1 : 2;
            string painted = LayeredAnim(anim);
            int n = layered.Set.Frames(painted);
            float a = alpha;
            Vector2 shift = Vector2.zero;
            float tilt = 0f;
            if (StandIn(anim, painted))
            {
                int f = anim == "walk" || anim == "dash" ? frame + Mathf.FloorToInt(crowdPhase * StandInFrames) : PoseFrame(anim, "attack-stand-in", frame, StandInFrames);
                StandInPose(anim, SoftRow(dir), f, out shift, out tilt);
                frame = 0;
            }
            if (anim == "death") { a *= 1f - Mathf.Clamp01((frame + 1f) / DeathFadeSteps); frame = 0; }
            else if (painted.StartsWith("attack", StringComparison.Ordinal)) frame = PoseFrame(anim, painted, frame, n);
            else if (painted == "walk" && n > 1) frame = (frame + Mathf.FloorToInt(crowdPhase * n)) % n; // crowd: not every walker steps in time
            Color c = anim == "hurt" ? HurtTint : Color.white;
            shadow.sortingOrder = orderBase;
            shadow.color = new Color(1f, 1f, 1f, 0.3f * a);
            float sy = painted == "idle" && anim != "death" ? 1f + BreathAmp * Mathf.Sin((Time.time + breathPhase) * (2f * Mathf.PI / BreathPeriod)) : 1f;
            layered.Show(painted, SoftRow(dir), frame, orderBase, new Color(c.r, c.g, c.b, a), sy, LayeredWeaponOn, !offhandEmpty, shift, tilt);
        }
    }
}
