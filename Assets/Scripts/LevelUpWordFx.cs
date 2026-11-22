using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The glowing "LEVEL UP" word over the local hero's head (22/11, replaces the painted wordmark / UGUI banner: "không xài hình nữa").
    /// Real text (TextMesh, Crimson Text Bold, the serif closest to the RO Adventure titles) drawn by the LevelUpText shader in four layers:
    /// a soft additive gold glow (8 copies, wide), a warm-brown rim (8 copies, thin, readable on snow), the fill with an ivory -> gold gradient
    /// and a light sweep, plus a soft light plate behind the word. Pops in (60 -> 118 -> 100 %, 0.28 s), rises slowly, holds, fades
    /// (total 2.1 s). Lives in the world like the skill FX: billboard on the 2.5D / 3D maps (ClericLightFx.PicturePoint), flat on the 2D view.
    /// One at a time (a second level-up restarts it).
    /// </summary>
    public sealed class LevelUpWordFx : MonoBehaviour
    {
        const string Word = "LEVEL UP";
        const float Em = 11f;           // text em height, world units (caps ~7.5 units = ~37 px at 1080p, default zoom)
        const float BaseY = 40f, RiseY = 7f; // over the head (name tag ~30)
        const float PopMs = 280f, HoldMs = 1500f, FadeMs = 450f, TotalMs = HoldMs + FadeMs;
        const float RimW = 0.38f, GlowW = 1.1f;
        const int FontPx = 64;
        static readonly Color Rim = new Color(0.33f, 0.2f, 0.07f, 0.9f);
        static readonly Color Glow = new Color(1f, 0.82f, 0.4f, 0.32f);

        static LevelUpWordFx live;
        static Material fillMat, rimMat, glowMat;
        static Font font;

        HeroController hero;
        float age;
        TextMesh fill;
        readonly MeshRenderer[] copies = new MeshRenderer[16];
        readonly Vector2[] copyOff = new Vector2[16];
        MeshRenderer fillMr, plate;

        public static void Show(HeroController hero)
        {
            if (hero == null || !NetMode.RendersFx || !FeelConfig.FxOn || !Build()) return;
            if (live != null) Destroy(live.gameObject);
            var go = new GameObject("LevelUpWord");
            if (Zone.Main != null) go.transform.SetParent(Zone.Main.Root, false);
            live = go.AddComponent<LevelUpWordFx>();
            live.hero = hero;
            live.Make();
        }

        static bool Build()
        {
            if (fillMat != null) return true;
            Shader s = Resources.Load<Shader>("Shaders/LevelUpText");
            font = Resources.Load<Font>("Fonts/CrimsonText/CrimsonText-Bold");
            if (font == null) font = RoSoftTheme.Bold;
            if (s == null || font == null) return false;
            fillMat = new Material(s) { name = "LevelUpFill" };
            fillMat.SetFloat("_Y0", -Em * 0.32f); fillMat.SetFloat("_Y1", Em * 0.32f);
            fillMat.SetColor("_Top", new Color(1f, 0.98f, 0.88f)); fillMat.SetColor("_Bottom", new Color(0.93f, 0.68f, 0.24f));
            fillMat.SetFloat("_Intensity", 1.08f);
            rimMat = new Material(s) { name = "LevelUpRim" };
            rimMat.SetFloat("_Grad", 0f);
            glowMat = new Material(s) { name = "LevelUpGlow" };
            glowMat.SetFloat("_Grad", 0f);
            glowMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            glowMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            glowMat.SetFloat("_Intensity", 1.4f);
            return true;
        }

        void Make()
        {
            for (int i = 0; i < 16; i++)
            {
                bool glow = i < 8;
                Vector2 d = RoTextOutline.Dirs[i % 8];
                copyOff[i] = d * (glow ? GlowW : RimW);
                copies[i] = Text(glow ? "Glow" : "Rim", glow ? Glow : Rim, glow ? glowMat : rimMat, glow ? 1 : 2).GetComponent<MeshRenderer>();
            }
            fill = Text("Fill", Color.white, fillMat, 3);
            fillMr = fill.GetComponent<MeshRenderer>();
            // soft light plate behind the word (shader quad, additive, no image)
            if (ClericLightLook.Drawable)
            {
                plate = LightSkillAssets.Quad(transform, "Plate", ClericLightLook.M(ClericLightLook.Core, ClericLightLook.Ascend, ClericLightLook.Role.Flash), false, Order);
                plate.transform.localScale = new Vector3(Em * 5.2f, Em * 1.6f, 1f);
            }
            Tick();
        }

        static int Order { get { return FeelConfig.FxOrder - 6; } } // over the skill FX layers (FxOrder - 20 ...), under the IMGUI numbers

        TextMesh Text(string name, Color c, Material m, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            TextMesh t = go.AddComponent<TextMesh>();
            t.font = font;
            t.fontSize = FontPx;
            t.characterSize = Em * 10f / FontPx;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            t.richText = false;
            t.color = c;
            t.text = Word;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = m;
            mr.sortingOrder = Order + order;
            return t;
        }

        void Update()
        {
            age += GameClock.Dt * 1000f;
            if (hero == null || age >= TotalMs) { Destroy(gameObject); return; }
            Tick();
        }

        void LateUpdate() { if (hero != null) Place(); } // after the hero moved this frame

        void Tick()
        {
            // the font atlas can be rebuilt by any text: keep the materials on the current texture
            Texture atlas = font.material != null ? font.material.mainTexture : null;
            if (atlas != null && fillMat.mainTexture != atlas) { fillMat.mainTexture = atlas; rimMat.mainTexture = atlas; glowMat.mainTexture = atlas; }
            float u = age / PopMs;
            float scale = u < 0.6f ? Mathf.Lerp(0.6f, 1.18f, u / 0.6f) : Mathf.Lerp(1.18f, 1f, Mathf.Clamp01((u - 0.6f) / 0.4f));
            float alpha = Mathf.Clamp01(age / (PopMs * 0.5f)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - HoldMs) / FadeMs)));
            transform.localScale = new Vector3(scale, scale, 1f);
            fillMat.SetFloat("_Alpha", alpha);
            rimMat.SetFloat("_Alpha", alpha);
            float glowPulse = age < PopMs * 1.6f ? 1.6f - 0.6f * Mathf.Clamp01(age / (PopMs * 1.6f)) : 1f; // brighter glow at the pop
            glowMat.SetFloat("_Alpha", alpha * glowPulse);
            fillMat.SetFloat("_Shine", Mathf.Lerp(-Em * 3.4f, Em * 3.4f, Mathf.Clamp01((age - PopMs * 0.6f) / 700f))); // one sweep after the pop
            if (plate != null) LightSkillAssets.Set(plate, 0.42f * alpha * (age < PopMs * 2f ? 1.4f : 1f), -1f);
            Place();
        }

        void Place()
        {
            float rise = RiseY * Mathf.Clamp01(age / TotalMs);
            Vector2 at = hero.Position;
            bool deep = ClericLightFx.Deep;
            Vector2 off = new Vector2(0f, BaseY + rise);
            transform.position = deep ? ClericLightFx.PicturePoint(at, off) : new Vector3(at.x + off.x, at.y + off.y, 0f);
            transform.rotation = deep ? View25D.Tilt : Quaternion.identity;
            for (int i = 0; i < copies.Length; i++) copies[i].transform.localPosition = new Vector3(copyOff[i].x, copyOff[i].y, 0.01f);
        }

        void OnDestroy() { if (live == this) live = null; }
    }
}
