using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// "RO Soft" hero path: one baked body sprite per frame (weapon + hair painted in), 8 view directions, sheets
    /// Sprites/classes/&lt;class&gt;/body/soft/{idle,walk,attack}.png laid out by Data/classes/&lt;class&gt;/soft-sheet.json
    /// (rows = Dir8 order: down, down-right, right, up-right, up, up-left, left, down-left; feet on row feetY).
    /// On for a class when its idle + walk sheets and json exist; `--hero-style classic` forces the paper doll everywhere.
    /// No weapon / head / hand layers here: shadow, lean, lift, figure scale, dash trail, sort order and freeze tint stay.
    /// Sim, hit boxes and network keep the 4-dir facing; Dir8 is view only (HeroController.Dir8.cs sets it).
    /// </summary>
    public partial class HeroRenderer
    {
        [Serializable] class SoftFrames { public int idle = 1, walk = 4, attack = 4; }
        [Serializable] class SoftSheetSpec { public int cellW = 320, cellH = 256, feetY = 244, bodyPx = 200; public SoftFrames frames; }

        /// <summary>World height of the hero body on screen (the paper doll's figure height), so both styles stand the same size.</summary>
        const float SoftBodyUnits = 20.5f;
        const float BreathAmp = 0.0075f, BreathPeriod = 1.6f; // +-0.75 % y-scale around the feet: 1.5 % peak to peak
        const int DeathFadeSteps = 8;                          // death = idle fading out over 8 virtual frames (120 ms each in HeroController)
        static readonly Color HurtTint = new Color(1f, 0.72f, 0.72f);

        static bool? softStyle, layeredStyle;

        /// <summary>
        /// `--hero-style classic` (paper doll everywhere), `rosoft` (Warrior's baked RO Soft sheets, the rest paper doll) or `layered` (default:
        /// every class with layered art, HeroRenderer.Layered.cs; a class without it falls back to RO Soft / paper doll).
        /// </summary>
        static string StyleArg()
        {
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "--hero-style");
            return i >= 0 && i + 1 < a.Length ? a[i + 1].ToLowerInvariant() : "layered";
        }

        /// <summary>False with `--hero-style classic` (or when a test turns it off before Init).</summary>
        public static bool SoftStyleEnabled
        {
            get { if (softStyle == null) softStyle = StyleArg() != "classic"; return softStyle.Value; }
            set { softStyle = value; }
        }

        /// <summary>True unless `--hero-style classic` / `rosoft` (or a test turns it off before heroes spawn).</summary>
        public static bool LayeredStyleEnabled
        {
            get { if (layeredStyle == null) { string s = StyleArg(); layeredStyle = s != "classic" && s != "rosoft"; } return layeredStyle.Value; }
            set { layeredStyle = value; }
        }

        Sprite[][] softIdle, softWalk, softAttack; // softAttack null until attack.png is painted
        bool soft;
        float breathPhase;

        /// <summary>True when this hero draws 8 view directions: the RO Soft sheets or a layered set.</summary>
        public bool IsSoft { get { return soft || layered != null; } } // the layered path (HeroRenderer.Layered.cs) uses the same 8 view dirs

        /// <summary>View-only 8-way facing (sheet row, Dir8 order); -1 = draw the cardinal of the facing passed to Show.</summary>
        public int Dir8 { get; set; } = -1;

        void InitSoft()
        {
            soft = false;
            if (!SoftStyleEnabled || string.IsNullOrEmpty(cls.SheetFolder)) return;
            int cut = cls.SheetFolder.IndexOf("/body/", StringComparison.Ordinal);
            if (cut < 0) return;
            string folder = cls.SheetFolder.Substring(0, cut); // classes/<class>
            TextAsset json = Resources.Load<TextAsset>("Data/" + folder + "/soft-sheet");
            if (json == null) return;
            SoftSheetSpec spec;
            try { spec = JsonUtility.FromJson<SoftSheetSpec>(json.text); }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] bad soft-sheet.json for " + folder + ": " + e.Message); return; }
            if (spec == null || spec.cellW <= 0 || spec.cellH <= 0 || spec.bodyPx <= 0) return;
            string sheets = "Sprites/" + folder + "/body/soft/";
            if (SpriteSheet.TryLoad(sheets + "idle") == null || SpriteSheet.TryLoad(sheets + "walk") == null) return;
            float ppu = spec.bodyPx / SoftBodyUnits;
            var pivot = new Vector2(0.5f, (spec.cellH - spec.feetY) / (float)spec.cellH);
            softIdle = SpriteSheet.Grid(sheets + "idle", spec.cellW, spec.cellH, ppu, pivot);
            softWalk = SpriteSheet.Grid(sheets + "walk", spec.cellW, spec.cellH, ppu, pivot);
            softAttack = SpriteSheet.TryLoad(sheets + "attack") != null ? SpriteSheet.Grid(sheets + "attack", spec.cellW, spec.cellH, ppu, pivot) : null;
            soft = softIdle != null && softWalk != null && softIdle.Length >= 8 && softWalk.Length >= 8;
            breathPhase = UnityEngine.Random.value * BreathPeriod; // heroes standing together do not breathe in step
        }

        /// <summary>Which soft sheet an anim draws: 0 idle (idle, hurt, death), 1 walk (walk, dash), 2 attack (every action pose).</summary>
        static int SoftKind(string anim)
        {
            if (anim == "idle" || anim == "hurt" || anim == "death") return 0;
            return anim == "walk" || anim == "dash" ? 1 : 2;
        }

        int SoftFrameCount(string anim)
        {
            if (anim == null) return 0;
            if (anim == "death") return DeathFadeSteps;
            int kind = SoftKind(anim);
            Sprite[][] g = kind == 0 ? softIdle : kind == 1 ? softWalk : softAttack;
            if (g == null) return 1; // attack not painted yet: one fallback pose
            return g[0].Length;
        }

        /// <summary>Sheet row to draw: the view Dir8 while it agrees with the facing (one of its axes), else that facing's cardinal row.</summary>
        int SoftRow(int dir)
        {
            int d = Dir8;
            return d >= 0 && Dir8Util.Matches(d, dir) ? d : Dir8Util.FromCardinal(dir);
        }

        void ShowSoft(string anim, int dir, int frame, int orderBase, float alpha)
        {
            handOver.sprite = null;
            if (headLayer != null) headLayer.Hide();
            foreach (Part p in parts) { p.Sr.sprite = null; if (p.Fx != null) p.Fx.Hide(); }
            motion = anim == "idle" ? 0 : anim == "walk" ? 1 : 2;
            int row = SoftRow(dir);
            int kind = SoftKind(anim);
            Sprite[] cells;
            if (kind == 2 && softAttack != null) cells = SpriteSheet.Row(softAttack, row);
            else if (kind == 2) { cells = SpriteSheet.Row(softWalk, row); frame = 2; } // fallback pose until attack.png exists: a walk stride
            else cells = SpriteSheet.Row(kind == 1 ? softWalk : softIdle, row);
            if (cells == null || cells.Length == 0) { body.sprite = null; return; }
            float a = alpha;
            if (anim == "death") { a *= 1f - Mathf.Clamp01((frame + 1f) / DeathFadeSteps); frame = 0; }
            Sprite s = cells[Mathf.Clamp(frame, 0, cells.Length - 1)];
            Color c = anim == "hurt" ? HurtTint : Color.white;
            shadow.sortingOrder = orderBase;
            shadow.color = new Color(1f, 1f, 1f, 0.3f * a);
            body.sprite = s;
            body.flipX = false;
            body.color = new Color(c.r, c.g, c.b, a);
            body.sortingOrder = orderBase + 3;
            // Idle: one painted frame + a gentle breath in code (sprite-rules: idle loops should not be AI-drawn frame sets).
            float sy = kind == 0 && anim != "death" ? 1f + BreathAmp * Mathf.Sin((Time.time + breathPhase) * (2f * Mathf.PI / BreathPeriod)) : 1f;
            body.transform.localScale = new Vector3(1f, sy, 1f);
        }
    }
}
