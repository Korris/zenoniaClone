using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The picture of the assassin's SHADOW CLONE; the summon and its strikes stay in the skill data and SkillMinion (authority), so this runs only where RendersFx.
    /// Timeline (ms): the hero holds two daggers crossed at his chest while shadow surges around him 0-400; at 400 SkillMinion spawns and a dark copy of the hero rises beside him out of
    /// the same surge, then follows the minion for its whole life: it walks, turns to its foe and swings the hero's own melee sheets with a purple slash at each of the minion's strikes;
    /// at the end it dissolves into purple smoke. The copy is the hero's armed sheets tinted dark, so it matches his moves in every direction. One picture per cast; the minion cap
    /// (SkillCaps) bounds them, and a capped-out minion dissolves its copy. Only the cast flash touches the camera, only for the local caster.
    /// </summary>
    public class ShadowCloneFx : MonoBehaviour
    {
        public const string SkillId = "assassin.shadow-clone";
        const string Folder = "Sprites/fx/shadowclone/", SurgeAnim = NightfallExecutionUltimate.PoseAnim;
        const float CastToMs = 400f, CrouchToMs = 90f, SurgeLenMs = 520f, RiseMs = 260f, SwingMs = 320f, WalkCycleMs = 480f, IdleFrameMs = 200f, DissolveMs = 560f, SlashLenMs = 240f;
        const float SurgeUnitsPerPx = 0.085f, DissolveUnitsPerPx = 0.05f, SlashUnitsPerPx = 0.085f, SurgeAnchorY = 430f, SlashOut = 16f, SlashLift = 8f, MoveEps = 0.02f, FarUnits = 300f;
        const int CrouchFrame = 0, CrossFrame = 4;
        static readonly string[] SwingAnims = { "melee1", "melee2", "melee3" };
        static readonly Color Violet = new Color(0.68f, 0.28f, 1f), BodyTint = new Color(0.3f, 0.17f, 0.46f, 0.96f), RimTint = new Color(0.42f, 0.14f, 0.7f, 0.34f);
        static readonly Dictionary<HeroController, ShadowCloneFx> Latest = new Dictionary<HeroController, ShadowCloneFx>();
        static AssassinBoardFx.Strip surgeArt;
        static Sprite[] slashArt, dissolveArt;
        static Sprite[][] idleSheet, walkSheet;
        static readonly Sprite[][][] SwingSheets = new Sprite[SwingAnims.Length][][];

        HeroController hero;
        ISkillBody body; // the zone pump finds a skill object's caster through this field: a portal transfer ends the picture with its hero
        SkillMinion minion;
        float age, cloneAge, swingLeft, walkClock, slashAge = SlashLenMs, dissolveAge;
        bool local, posing, bound, dissolving;
        int swingIndex, cloneFacing;
        Vector2 clonePos, lastPos, slashDir, surgePos;
        AssassinBoardFx.Pair surge;
        SpriteRenderer figure, figureRim, slash, slashGlow, dissolve, dissolveGlow, groundGlow;

        public static void Cast(SkillContext ctx)
        {
            if (!NetMode.RendersFx) return;
            HeroController caster = ctx.Body.Hero;
            ShadowCloneFx old;
            if (Latest.TryGetValue(caster, out old) && old != null) old.posing = false; // a recast takes the pose; the older copy lives on under the minion cap
            var go = new GameObject("ShadowClone");
            go.transform.SetParent(caster.transform.parent, false);
            ShadowCloneFx fx = go.AddComponent<ShadowCloneFx>();
            Latest[caster] = fx;
            fx.Begin(ctx);
        }

        /// <summary>SkillMinion: the summon of this cast fired; the picture starts drawing its copy there.</summary>
        public static ShadowCloneFx Bind(SkillContext ctx, SkillMinion m)
        {
            ShadowCloneFx fx;
            if (!Latest.TryGetValue(ctx.Body.Hero, out fx) || fx == null || fx.bound) return null;
            fx.minion = m;
            fx.bound = true;
            fx.clonePos = fx.lastPos = m.Pos;
            fx.surgePos = m.Pos;
            fx.cloneAge = 0f;
            fx.cloneFacing = ctx.Body.Facing;
            UltimateFx.Embers(m.Pos, 12, 9f, 40f, new Color(Violet.r, Violet.g, Violet.b, 1f));
            ShockRing.Spawn(m.Pos, 16f, 0.32f, new Color(Violet.r, Violet.g, Violet.b, 0.7f), UltimateFx.GroundFxOrder + 6);
            return fx;
        }

        /// <summary>Loads and slices the painted frames and the hero sheets of the copy ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Preload()
        {
            if (!NetMode.RendersFx) return;
            if (surgeArt == null) surgeArt = AssassinBoardFx.LoadCut(Folder, "rise", SurgeAnchorY, SurgeUnitsPerPx);
            if (slashArt == null) slashArt = AssassinBoardFx.LoadWhole(Folder, "slash", SlashUnitsPerPx, 0.733f, 0.5f);
            if (dissolveArt == null) dissolveArt = AssassinBoardFx.LoadWhole(Folder, "dissolve", DissolveUnitsPerPx, 0.5f, 0.074f);
            if (idleSheet == null) idleSheet = Sheet("idle");
            if (walkSheet == null) walkSheet = Sheet("walk");
            for (int i = 0; i < SwingAnims.Length; i++) if (SwingSheets[i] == null) SwingSheets[i] = Sheet(SwingAnims[i]);
        }

        static Sprite[][] Sheet(string anim) { return SpriteSheet.Grid("Sprites/" + HeroClassDef.Assassin.SheetFolder + "/" + anim, GameConfig.FramePx, GameConfig.FramePx, GameConfig.FramePpu, new Vector2(0.5f, 0f)); }

        void Begin(SkillContext ctx)
        {
            hero = ctx.Body.Hero;
            body = ctx.Body;
            local = body.IsLocal;
            posing = true;
            surgePos = hero.Position;
            Preload();
            surge = new AssassinBoardFx.Pair(transform, "Surge");
            figure = ProceduralSprites.Child(transform, "Figure", null);
            figureRim = ProceduralSprites.Child(figure.transform, "Rim", null);
            figureRim.sharedMaterial = FeelMaterials.Additive;
            figure.enabled = figureRim.enabled = false;
            slash = UltimateFx.Layer(transform, "Slash", out slashGlow);
            dissolve = UltimateFx.Layer(transform, "Dissolve", out dissolveGlow);
            slash.enabled = slashGlow.enabled = dissolve.enabled = dissolveGlow.enabled = false;
            groundGlow = ProceduralSprites.Child(transform, "GroundGlow", FxSprites.Dot());
            groundGlow.sortingOrder = UltimateFx.GroundFxOrder + 1;
            UltimateFx.Dust(hero.Position, 5, 10f, new Color(0.25f, 0.12f, 0.4f));
            if (local) CameraFollow.AddTrauma(0.1f);
            Apply();
        }

        void OnDestroy()
        {
            if (hero == null) return;
            if (posing) hero.ClearSkillPose();
            ShadowCloneFx fx;
            if (Latest.TryGetValue(hero, out fx) && fx == this) Latest.Remove(hero);
        }

        /// <summary>SkillMinion: one strike. `foe` is the enemy in its reach by this side's view, null when it swings at nothing (then no swing is drawn).</summary>
        public void OnStrike(Monster foe)
        {
            if (foe == null || dissolving) return;
            Vector2 to = foe.Center - clonePos;
            slashDir = to.sqrMagnitude > 0.01f ? to.normalized : Vector2.right;
            cloneFacing = AssassinBoardFx.FacingOf(slashDir);
            swingIndex = (swingIndex + 1) % SwingAnims.Length;
            swingLeft = SwingMs;
            slashAge = 0f;
            UltimateFx.Embers(foe.Center, 6, 6f, 40f, new Color(Violet.r, Violet.g, Violet.b, 1f));
        }

        /// <summary>SkillMinion's time is over (or it was capped out): the copy turns to smoke.</summary>
        public void Finish()
        {
            if (dissolving) return;
            dissolving = true;
            dissolveAge = 0f;
            minion = null;
            UltimateFx.Dust(clonePos, 6, 10f, new Color(0.3f, 0.14f, 0.46f));
            UltimateFx.Embers(clonePos + new Vector2(0f, 8f), 10, 8f, 34f, new Color(Violet.r, Violet.g, Violet.b, 1f));
        }

        void Update()
        {
            if (hero == null) { Destroy(gameObject); return; }
            if (hero.IsDead && !bound) { Destroy(gameObject); return; } // a fallen caster ends the picture; a live copy goes with its minion, which ends on the same death
            float dt = GameClock.Dt * 1000f;
            age += dt;
            if (bound)
            {
                cloneAge += dt;
                if (dissolving)
                {
                    dissolveAge += dt;
                    if (dissolveAge >= DissolveMs) { Destroy(gameObject); return; }
                }
                else if (minion == null)
                {
                    if ((clonePos - hero.Position).sqrMagnitude > FarUnits * FarUnits) { Destroy(gameObject); return; } // left behind on another map: nothing to dissolve there
                    Finish();
                }
                else
                {
                    lastPos = clonePos;
                    clonePos = minion.Pos;
                }
                if (swingLeft > 0f) swingLeft -= dt;
                slashAge += dt;
            }
            else if (age > CastToMs + 400f) { Destroy(gameObject); return; } // the summon never came (the run ended with its caster)
            Apply();
        }

        void ApplyPose()
        {
            if (!posing) return;
            if (age >= CastToMs) { hero.ClearSkillPose(); posing = false; return; }
            hero.SetSkillPose(SurgeAnim, age < CrouchToMs ? CrouchFrame : CrossFrame, 0f);
        }

        void ApplyFigure()
        {
            if (!bound) { figure.enabled = figureRim.enabled = false; return; }
            Vector2 moved = clonePos - lastPos;
            float dtMs = Mathf.Max(0.001f, GameClock.Dt * 1000f);
            bool walking = moved.sqrMagnitude > MoveEps * MoveEps;
            if (walking) { walkClock += dtMs; if (swingLeft <= 0f) cloneFacing = AssassinBoardFx.FacingOf(moved); }
            Sprite[][] sheet;
            int frame = 0;
            if (swingLeft > 0f) { sheet = SwingSheets[swingIndex]; frame = Mathf.FloorToInt((1f - swingLeft / SwingMs) * AssassinBoardFx.Frames); }
            else if (walking && walkSheet != null) { sheet = walkSheet; frame = Mathf.FloorToInt(walkClock / WalkCycleMs * walkSheet[0].Length); }
            else { sheet = idleSheet; frame = sheet != null ? Mathf.FloorToInt(cloneAge / IdleFrameMs) : 0; }
            bool on = sheet != null;
            figure.enabled = on;
            figureRim.enabled = on && FeelConfig.FxOn;
            if (!on) return;
            Sprite[] row = sheet[cloneFacing];
            figure.sprite = figureRim.sprite = row[swingLeft > 0f ? Mathf.Clamp(frame, 0, row.Length - 1) : frame % row.Length];
            float rise = Mathf.Clamp01(cloneAge / RiseMs), gone = dissolving ? Mathf.Clamp01(1f - dissolveAge / (DissolveMs * 0.6f)) : 1f;
            figure.transform.position = clonePos;
            figure.transform.localScale = new Vector3(1f, Mathf.Lerp(0.25f, 1f, rise), 1f); // stands up out of its own shadow
            float a = rise * gone;
            figure.color = new Color(BodyTint.r, BodyTint.g, BodyTint.b, BodyTint.a * a);
            figureRim.color = new Color(RimTint.r, RimTint.g, RimTint.b, RimTint.a * a);
            int order = GameConfig.OrderBase(clonePos.y);
            figure.sortingOrder = order + 3; figureRim.sortingOrder = order + 3;
        }

        void ApplySlash()
        {
            float t = slashAge / SlashLenMs;
            bool on = slashArt != null && bound && !dissolving && t < 1f;
            slash.enabled = on;
            slashGlow.enabled = on && FeelConfig.FxOn;
            if (!on) return;
            slash.sprite = slashGlow.sprite = slashArt[AssassinBoardFx.ByProgress(t)];
            slash.transform.position = clonePos + new Vector2(0f, SlashLift) + slashDir * SlashOut;
            slash.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(slashDir.y, slashDir.x) * Mathf.Rad2Deg);
            slash.color = new Color(1f, 1f, 1f, Mathf.Min(1f, (1f - t) * 4f));
            slashGlow.color = new Color(Violet.r, Violet.g, Violet.b, 0.45f * slash.color.a);
            slash.sortingOrder = slashGlow.sortingOrder = FeelConfig.FxOrder - 3;
        }

        void ApplySurge()
        {
            // The first surge wraps the hero until the summon; the same surge then wraps the copy as it rises.
            float t = bound ? cloneAge / SurgeLenMs : Mathf.Min(0.75f, age / SurgeLenMs);
            bool on = surgeArt != null && t < 1f;
            if (!on) { surge.Hide(); }
            else
            {
                Vector2 at = bound ? surgePos : hero.Position;
                float alpha = bound ? Mathf.Min(1f, (1f - t) * 3f) : Mathf.Clamp01(age / 60f);
                surge.Show(surgeArt, AssassinBoardFx.ByProgress(t), alpha * 0.95f, 0.3f, Violet, at, 1f, 0.8f);
            }
            float g = !bound ? 0.4f * Mathf.Clamp01(age / 120f) : 0.4f * Mathf.Clamp01(1f - cloneAge / 500f);
            groundGlow.transform.position = bound ? surgePos : hero.Position;
            groundGlow.transform.localScale = new Vector3(52f, 32f, 1f);
            groundGlow.color = new Color(0.2f, 0.05f, 0.34f, g);
        }

        void ApplyDissolve()
        {
            bool on = dissolving && dissolveArt != null;
            dissolve.enabled = on;
            dissolveGlow.enabled = on && FeelConfig.FxOn;
            if (!on) return;
            float t = dissolveAge / DissolveMs;
            dissolve.sprite = dissolveGlow.sprite = dissolveArt[AssassinBoardFx.ByProgress(t)];
            dissolve.transform.position = clonePos;
            dissolve.color = new Color(1f, 1f, 1f, Mathf.Min(1f, (1f - t) * 3f));
            dissolveGlow.color = new Color(Violet.r, Violet.g, Violet.b, 0.25f * dissolve.color.a);
            int order = GameConfig.OrderBase(clonePos.y) + 4;
            dissolve.sortingOrder = dissolveGlow.sortingOrder = order;
        }

        void Apply()
        {
            if (hero == null) return;
            ApplyPose();
            ApplySurge();
            ApplyFigure();
            ApplySlash();
            ApplyDissolve();
        }
    }
}
