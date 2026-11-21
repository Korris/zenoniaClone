using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The ChatGPT-painted giant sword of the Warrior's EARTH BREAK (21/11/2026, user: "kết hợp 1 cái sprite cây kiếm đẹp"; sprite-rules exception:
    /// a solid object summoned by a skill may be a painted sprite, the fire / light / dissolve around it stay shader + particles). Replaces the
    /// procedural war-greatsword mask (kept for other callers). Art: Resources/Sprites/classes/warrior/fx/earth-break/greatsword(.png, -runes.png),
    /// cut by Tools/build-earth-break-greatsword.py from Assets/ArtLibrary/classes/warrior/skill-vfx/earth-break-sword/. Shader PaintedFxObject:
    /// painted body with a warm under-light at the point, pulsing rune glow, burning dissolve edge, additive silhouette copies (hot halo, flash).
    /// </summary>
    public static class WarriorPaintedSword
    {
        const string Folder = "Sprites/classes/warrior/fx/earth-break/";

        /// <summary>Width / height of the painted sprite (330 x 768 after the cut); updated from the texture when it loads.</summary>
        public static float Aspect { get; private set; } = 0.43f;

        static Material body, halo, flash;
        static bool built, failed;

        /// <summary>True when the shader and both textures are present (else Earth Break keeps the procedural mask sword).</summary>
        public static bool Ready { get { Build(); return !failed; } }

        /// <summary>Loads the art ahead of the first cast (WarriorLightLook.Load at class spawn) so the cast never hitches.</summary>
        public static void Preload() { Build(); }

        public static Material Body { get { Build(); return body; } }
        /// <summary>Additive-only copy: a hot orange glow over the silhouette, drawn ~8 % larger behind the sword (outline that reads on snow + cave).</summary>
        public static Material Halo { get { Build(); return halo; } }
        /// <summary>Additive-only copy: the white-hot compression flash over the whole sword at ground contact (40-60 ms).</summary>
        public static Material Flash { get { Build(); return flash; } }

        static void Build()
        {
            if (built) return;
            built = true;
            Shader sh = Resources.Load<Shader>("Shaders/PaintedFxObject");
            Texture2D tex = Resources.Load<Texture2D>(Folder + "greatsword"), runes = Resources.Load<Texture2D>(Folder + "greatsword-runes");
            if (sh == null || !sh.isSupported || tex == null || runes == null)
            {
                failed = true;
                Debug.LogWarning("[ZenoniaProto] Earth Break painted sword: shader or art missing, using the mask sword");
                return;
            }
            tex.wrapMode = TextureWrapMode.Clamp; runes.wrapMode = TextureWrapMode.Clamp;
            Aspect = tex.width / (float)Mathf.Max(1, tex.height);

            body = new Material(sh) { name = "WarriorPaintedSword_Body" };
            body.SetTexture("_MainTex", tex);
            body.SetTexture("_EmitTex", runes);
            body.SetColor("_Tint", Color.white);
            body.SetColor("_HeatColor", new Color(1f, 0.45f, 0.12f, 0.45f));   // the impact glow climbing the lowest third of the blade
            body.SetFloat("_HeatSpan", 0.32f);
            body.SetColor("_RuneColor", new Color(2.4f, 1.15f, 0.3f, 1f));    // passes the BloomLite threshold only on the runes
            body.SetFloat("_PulseSpeed", 2.4f);
            body.SetFloat("_PulseMin", 0.4f);
            body.SetColor("_EdgeColor", new Color(2.6f, 0.95f, 0.18f, 1f));
            body.SetColor("_SilColor", Color.black);

            halo = new Material(body) { name = "WarriorPaintedSword_Halo" };
            halo.SetColor("_Tint", new Color(1f, 1f, 1f, 0f));
            halo.SetTexture("_EmitTex", Texture2D.blackTexture);
            halo.SetColor("_SilColor", new Color(0.65f, 0.21f, 0.04f, 1f)); // round 2: 1.25 washed the sword to pale gold on snow; ChatGPT 8.8 review: -20 %

            flash = new Material(halo) { name = "WarriorPaintedSword_Flash" };
            flash.SetColor("_SilColor", new Color(0.5f, 0.36f, 0.2f, 1f)); // round 1: 2.2 HDR + hit-stop bleached the whole sword to a white column for ~110 ms
        }

        /// <summary>
        /// Lays the sword on `fx` (anchored on the impact centre): it appears at `t0`, falls `dropFrom` units over `dropMs` (PopCurve: first ground
        /// contact at 37 %), stands stuck in the ground through the hits, burns away over the last `dissolveMs` before `t1`.
        /// `height` = on-screen sword height (units); the point sinks `sink` units into the ground. Returns false when the art is missing.
        /// </summary>
        public static bool Lay(ClericLightFx fx, float t0, float t1, float height, float sink, float dropFrom, float dropMs, float contactMs, float dissolveMs, ClericLightLook.Pal fire)
        {
            if (!Ready) return false;
            float w = height * Aspect;
            Vector2 foot = new Vector2(0f, -sink);
            fx.Upright(Halo, ClericLightFx.Band.Over, true, t0, t1, w * 1.1f, w * 1.1f, height * 1.04f, height * 1.04f, 1f, foot + new Vector2(0f, -height * 0.02f), 1)
                .Drop(dropFrom, dropMs).Env(90f, 0f).DissolveOut(dissolveMs).Kind(LayerKind.Glow)                          // hot halo behind the blade
              .Upright(Body, ClericLightFx.Band.Over, true, t0, t1, w, w, height, height, 1f, foot, 2)
                .Drop(dropFrom, dropMs).Env(90f, 0f).DissolveOut(dissolveMs)                                              // the painted sword
              .Upright(Flash, ClericLightFx.Band.Over, true, contactMs, contactMs + 50f, w, w * 1.03f, height, height * 1.01f, 1f, foot, 2)
                .Env(0f, 50f);                                                                                            // compression flash at contact

            // Heat along the blade while it stands in the ground: flame tongues licking up its foot, embers rising off the edges
            for (int s = -1; s <= 1; s += 2)
                fx.Upright(WarriorLightLook.Glyph(WarriorLightLook.FlameTongue, fire), ClericLightFx.Band.Over, true, contactMs + 40f, t1 - dissolveMs * 0.4f,
                        w * 0.32f, w * 0.42f, height * 0.12f, height * 0.26f, 0.95f, foot + new Vector2(s * w * 0.18f, height * 0.02f), 3)
                    .Env(120f, 200f).Kind(LayerKind.Detail);
            for (float t = contactMs + 60f; t < t1 - 60f; t += 110f)
                fx.At(t, f =>
                {
                    int n = f.Own ? 3 : 1;
                    for (int i = 0; i < n; i++)
                    {
                        float y = Random.Range(0.05f, 0.7f) * height, x = Random.Range(-0.32f, 0.32f) * w * (1f - y / height * 0.4f);
                        f.Emit(false, f.Anchor + foot + new Vector2(x, y), new Vector2(Random.Range(-4f, 4f), Random.Range(22f, 40f)), Random.Range(1.2f, 2.4f),
                            i == 0 ? fire.Light : fire.Core, Random.Range(0.45f, 0.75f));
                    }
                });
            return true;
        }
    }
}
