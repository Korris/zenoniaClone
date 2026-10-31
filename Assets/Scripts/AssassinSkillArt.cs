using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// What Shadow Strike, Shadow Step and Phantom Rush share: dispatch, preload, frame strips, and a painted layer that points along the aim.
    /// The art is painted aiming right in the game's tilted view: left mirrors it, up and down turn it a quarter and press it flat by the view tilt so the picture stays on the ground plane.
    /// </summary>
    public static class AssassinSkillArt
    {
        public const int Frames = 6;
        const float ViewPress = 0.28f;

        static readonly Dictionary<HeroController, object> PoseOwner = new Dictionary<HeroController, object>();

        /// <summary>The newest cast drives the figure: a recast while an older picture is still fading takes the pose over, and the older one no longer touches it.</summary>
        public static void ClaimPose(HeroController hero, object owner) { PoseOwner[hero] = owner; }

        public static bool OwnsPose(HeroController hero, object owner) { object o; return PoseOwner.TryGetValue(hero, out o) && o == owner; }

        /// <summary>Clears the figure's pose unless a newer cast owns it now.</summary>
        public static void ReleasePose(HeroController hero, object owner)
        {
            if (!OwnsPose(hero, owner)) return;
            PoseOwner.Remove(hero);
            hero.ClearSkillPose();
        }

        public static bool Handles(string id) { return id == ShadowStrikeFx.SkillId || id == ShadowStepFx.SkillId || id == PhantomRushFx.SkillId; }

        public static void Cast(SkillContext ctx)
        {
            if (ctx.Def.Id == ShadowStrikeFx.SkillId) ShadowStrikeFx.Cast(ctx);
            else if (ctx.Def.Id == ShadowStepFx.SkillId) ShadowStepFx.Cast(ctx);
            else if (ctx.Def.Id == PhantomRushFx.SkillId) PhantomRushFx.Cast(ctx);
        }

        /// <summary>Loads and slices the painted frames ahead of the first cast (hero spawn) so the cast itself never hitches.</summary>
        public static void Preload()
        {
            if (!NetMode.RendersFx) return;
            ShadowStrikeFx.Preload();
            ShadowStepFx.Preload();
            PhantomRushFx.Preload();
        }

        /// <summary>Six frames of one stage; the pivot is given in painted pixels from the frame's top left.</summary>
        public static Sprite[] Load(string folder, string name, float unitsPerPx, float pivotPxX, float pivotPxY)
        {
            var s = new Sprite[Frames];
            for (int i = 0; i < Frames; i++)
            {
                Texture2D tex = SpriteSheet.Load(folder + name + "-" + (i + 1));
                if (tex == null) return null;
                s[i] = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(pivotPxX / tex.width, 1f - pivotPxY / tex.height), 1f / unitsPerPx, 0, SpriteMeshType.FullRect);
            }
            return s;
        }

        /// <summary>One strip cut in two at `cutPxY` from the top: Back above the cut, Front below; both pivot on the cut's point at `pivotPxX`.</summary>
        public static CutStrip LoadCut(string folder, string name, float unitsPerPx, float pivotPxX, float cutPxY)
        {
            var s = new CutStrip();
            for (int i = 0; i < Frames; i++)
            {
                Texture2D tex = SpriteSheet.Load(folder + name + "-" + (i + 1));
                if (tex == null) return null;
                float below = tex.height - cutPxY, ppu = 1f / unitsPerPx, px = pivotPxX / tex.width;
                s.Back[i] = Sprite.Create(tex, new Rect(0f, below, tex.width, cutPxY), new Vector2(px, 0f), ppu, 0, SpriteMeshType.FullRect);
                s.Front[i] = Sprite.Create(tex, new Rect(0f, 0f, tex.width, below), new Vector2(px, 1f), ppu, 0, SpriteMeshType.FullRect);
            }
            return s;
        }

        /// <summary>1 inside [from, to], easing over `ms` at both ends.</summary>
        public static float Fade(float age, float from, float to, float ms) { return Mathf.Clamp01(Mathf.Min((age - from) / ms, (to - age) / ms)); }

        /// <summary>Frame of a stage at progress t (0..1).</summary>
        public static int ByProgress(float t) { return Mathf.Clamp(Mathf.FloorToInt(t * Frames), 0, Frames - 1); }

        /// <summary>Frame of a stage whose peak `peakFrame` must arrive at progress `peakAt`: quick through the build, slower through the fall.</summary>
        public static int ToPeak(float t, int peakFrame, float peakAt)
        {
            if (t < peakAt) return Mathf.Clamp(Mathf.FloorToInt(t / peakAt * peakFrame), 0, peakFrame);
            return Mathf.Clamp(peakFrame + Mathf.FloorToInt((t - peakAt) / (1f - peakAt) * (Frames - peakFrame)), peakFrame, Frames - 1);
        }

        /// <summary>The tilted view presses anything that points up or down the screen.</summary>
        public static float Press(Vector2 dir) { return 1f - ViewPress * Mathf.Abs(dir.y); }

        public static Color Solid(Color c) { return new Color(c.r, c.g, c.b, 1f); }
    }

    public sealed class CutStrip { public Sprite[] Back = new Sprite[AssassinSkillArt.Frames], Front = new Sprite[AssassinSkillArt.Frames]; }

    /// <summary>A painted sprite and its additive copy on a parent that carries the view press, so a turned sprite still lies on the ground plane.</summary>
    public sealed class AimedLayer
    {
        readonly Transform tilt;
        readonly SpriteRenderer sprite, glow;

        public AimedLayer(Transform parent, string name)
        {
            tilt = new GameObject(name).transform;
            tilt.SetParent(parent, false);
            sprite = UltimateFx.Layer(tilt, name + "Art", out glow);
            Hide();
        }

        public void Hide() { sprite.enabled = glow.enabled = false; }

        /// <summary>Draws `art` with its pivot at `at`, pointing along `dir`; `stretch` scales along the aim only.</summary>
        public void Show(Sprite art, Vector2 at, Vector2 dir, float alpha, Color glowColor, float glowAlpha, int order, float stretch = 1f, float width = 1f, int glowOrder = 0)
        {
            bool on = art != null && alpha > 0.01f;
            sprite.enabled = on;
            glow.enabled = on && FeelConfig.FxOn;
            if (!on) return;
            bool mirror = dir.x < -0.01f;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - (mirror ? 180f : 0f);
            tilt.position = at;
            tilt.localScale = new Vector3(1f, AssassinSkillArt.Press(dir), 1f);
            sprite.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            sprite.transform.localScale = new Vector3(mirror ? -stretch : stretch, width, 1f);
            sprite.sprite = glow.sprite = art;
            sprite.color = new Color(1f, 1f, 1f, alpha);
            glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowAlpha * alpha);
            sprite.sortingOrder = order;
            glow.sortingOrder = glowOrder != 0 ? glowOrder : order + 1;
        }
    }

    /// <summary>Upper half behind the hero, lower half in front.</summary>
    public sealed class CutLayer
    {
        readonly SpriteRenderer back, backGlow, front, frontGlow;

        public CutLayer(Transform parent, string name)
        {
            back = UltimateFx.Layer(parent, name + "Back", out backGlow);
            front = UltimateFx.Layer(parent, name + "Front", out frontGlow);
            Hide();
        }

        public void Hide() { back.enabled = backGlow.enabled = front.enabled = frontGlow.enabled = false; }

        public void Show(CutStrip art, int frame, Vector2 at, float alpha, Color glowColor, float glowAlpha, int backOrder)
        {
            bool on = art != null && alpha > 0.01f;
            back.enabled = front.enabled = on;
            backGlow.enabled = frontGlow.enabled = on && FeelConfig.FxOn;
            if (!on) return;
            back.sprite = backGlow.sprite = art.Back[frame];
            front.sprite = frontGlow.sprite = art.Front[frame];
            back.color = front.color = new Color(1f, 1f, 1f, alpha);
            backGlow.color = frontGlow.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowAlpha * alpha);
            back.transform.position = front.transform.position = at;
            back.sortingOrder = backOrder; backGlow.sortingOrder = backOrder + 1;
            front.sortingOrder = FeelConfig.FxOrder - 3; frontGlow.sortingOrder = FeelConfig.FxOrder - 2;
        }
    }
}
