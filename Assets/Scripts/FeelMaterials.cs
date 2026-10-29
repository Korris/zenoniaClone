using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Runtime materials for the feel layer. Shaders live in Resources/Shaders (so player builds include them) and are loaded
    /// by name; if one is missing we fall back to a built-in shader so nothing renders pink.
    /// </summary>
    public static class FeelMaterials
    {
        static Material flash, additive, trail;
        static MaterialPropertyBlock block;
        static int flashAmountId = -1;

        /// <summary>Alpha sprite shader with a per-renderer white flash (_FlashAmount).</summary>
        public static Material SpriteFlash { get { return Get(ref flash, "SpriteFlash", "Sprites/Default", 0f); } }

        /// <summary>Additive unlit material for sprites / particles / quads (set mainTexture on your own instance for particles).</summary>
        public static Material Additive { get { return Get(ref additive, "SpriteAdditive", "Legacy Shaders/Particles/Additive", FeelConfig.AdditiveIntensity); } }

        /// <summary>Sword ribbon material (shared; _Head / _Fade are set per renderer through a property block).</summary>
        public static Material Trail { get { return Get(ref trail, "TrailRibbon", "Legacy Shaders/Particles/Additive", FeelConfig.TrailIntensity); } }

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
