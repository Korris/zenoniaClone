using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Runtime materials for the feel layer. Shaders live in Resources/Shaders (so player builds include them) and are loaded
    /// by name; if one is missing we fall back to a built-in shader so nothing renders pink.
    /// </summary>
    public static class FeelMaterials
    {
        // Same sorting order as the art they belong to: the render queue then draws the halo right before it and the shine right after it.
        const int HaloQueue = 2999, ShineQueue = 3001;

        static Material flash, additive, trail, enhanceHalo, enhanceShine;
        static MaterialPropertyBlock block;
        static int flashAmountId = -1, shineId = -1;
        static bool shineMissing;

        /// <summary>Alpha sprite shader with a per-renderer white flash (_FlashAmount).</summary>
        public static Material SpriteFlash { get { return Get(ref flash, "SpriteFlash", "Sprites/Default", 0f); } }

        /// <summary>Additive unlit material for sprites / particles / quads (set mainTexture on your own instance for particles).</summary>
        public static Material Additive { get { return Get(ref additive, "SpriteAdditive", "Legacy Shaders/Particles/Additive", FeelConfig.AdditiveIntensity); } }

        /// <summary>Sword ribbon material (shared; _Head / _Fade are set per renderer through a property block).</summary>
        public static Material Trail { get { return Get(ref trail, "TrailRibbon", "Legacy Shaders/Particles/Additive", FeelConfig.TrailIntensity); } }

        /// <summary>Additive halo of an enhanced item, drawn behind the art so the painted blade covers it.</summary>
        public static Material EnhanceHalo
        {
            get
            {
                if (enhanceHalo != null) return enhanceHalo;
                enhanceHalo = new Material(Additive) { name = "Feel_EnhanceHalo", renderQueue = HaloQueue };
                if (enhanceHalo.HasProperty("_Intensity")) enhanceHalo.SetFloat("_Intensity", FeelConfig.EnhanceHaloIntensity);
                return enhanceHalo;
            }
        }

        /// <summary>Light band clipped to an enhanced item (shader in Resources, so builds include it); null when missing or unsupported, as no fallback can clip to the art.</summary>
        public static Material EnhanceShine
        {
            get
            {
                if (enhanceShine != null || shineMissing) return enhanceShine;
                Shader s = Resources.Load<Shader>("Shaders/SpriteShine");
                if (s == null || !s.isSupported)
                {
                    Debug.LogWarning("ZenoniaProto: shader 'SpriteShine' missing or unsupported, enhanced items draw without the light band.");
                    shineMissing = true;
                    return null;
                }
                enhanceShine = new Material(s) { name = "Feel_SpriteShine", renderQueue = ShineQueue };
                return enhanceShine;
            }
        }

        /// <summary>Sets the light band (x centre, y half width, z lift) on one shine renderer without allocating.</summary>
        public static void SetShine(Renderer r, Vector4 shine)
        {
            if (block == null) block = new MaterialPropertyBlock();
            if (shineId < 0) shineId = Shader.PropertyToID("_Shine");
            r.GetPropertyBlock(block);
            block.SetVector(shineId, shine);
            r.SetPropertyBlock(block);
        }

        /// <summary>Separate additive material instance (own texture), e.g. for the spark particle system.</summary>
        public static Material NewAdditive(Texture tex)
        {
            var m = new Material(Additive) { name = "FeelAdditive" };
            m.mainTexture = tex;
            return m;
        }

        /// <summary>Sets the white-flash amount (0..1) on one renderer without allocating or breaking batching of others.</summary>
        public static void SetFlash(Renderer r, float amount)
        {
            if (block == null) block = new MaterialPropertyBlock();
            if (flashAmountId < 0) flashAmountId = Shader.PropertyToID("_FlashAmount");
            r.GetPropertyBlock(block);
            block.SetFloat(flashAmountId, Mathf.Min(amount, FeelConfig.FlashMaxAmount));
            r.SetPropertyBlock(block);
        }

        static Material Get(ref Material slot, string shaderName, string fallbackShader, float intensity)
        {
            if (slot != null) return slot;
            Shader s = Resources.Load<Shader>("Shaders/" + shaderName);
            bool usable = s != null && s.isSupported;
            if (!usable)
            {
                Debug.LogWarning("ZenoniaProto: shader '" + shaderName + "' missing or unsupported, using " + fallbackShader);
                s = Shader.Find(fallbackShader);
            }
            slot = new Material(s) { name = "Feel_" + shaderName };
            if (usable && intensity > 0f && slot.HasProperty("_Intensity")) slot.SetFloat("_Intensity", intensity);
            return slot;
        }
    }
}
