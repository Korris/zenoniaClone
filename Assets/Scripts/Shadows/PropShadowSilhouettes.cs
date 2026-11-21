using UnityEngine;
using UnityEngine.Rendering;

namespace ZenoniaProto
{
    /// <summary>
    /// Directional cast shadows of a RO3 2.5D map (FakeSun.Ro3; ChatGPT RO3 comparison section 5, "tầng 2"): every standing prop (tree, house, rock,
    /// log) throws its own sprite silhouette across the ground along FakeSun.Direction, as long as FakeSun.LengthFactor x the sprite height
    /// (sun 45 deg, shortened to 0.6). A pixel h units up the billboard lands h x length down-sun of its column's foot, so the trunk draws a thin
    /// line out from the root and the crown a leafy patch with its gaps, all in one direction. Baked once per map on the GPU into a 1 texel / unit
    /// R8 texture: silhouettes (BlendOp Max) -> gaussian blur (soft penumbra) -> max with the contact ellipses of StaticPropShadows.
    /// The result replaces the prop-shadow cache in GroundShadowRenderer and is read back once into <see cref="Field"/>, which units sample under
    /// their feet to take the canopy shade (View25DLighting). Cost: ~150 draws and 2 blur blits at map load, nothing per frame.
    /// </summary>
    public static class PropShadowSilhouettes
    {
        const int PassSilhouette = 0, PassBlur = 1, PassMax = 2;
        static readonly int SrcId = Shader.PropertyToID("_ShadowSrc"), AlphaId = Shader.PropertyToID("_ShadowAlpha"), BlurId = Shader.PropertyToID("_BlurDir");
        static Material mat;
        static bool tried;
        static Mesh quad;
        static byte[] field;
        static int fieldW, fieldH;

        /// <summary>Bakes the map's cast + contact shadows into a new render texture (MapW x MapH texels), or null when the shader is missing.</summary>
        public static RenderTexture Bake(IGameMap map, Texture2D contact)
        {
            field = null;
            if (!Ready()) return null;
            int w = Mathf.Clamp(Mathf.CeilToInt(GameConfig.MapW), 1, 2048), h = Mathf.Clamp(Mathf.CeilToInt(GameConfig.MapH), 1, 2048);
            RenderTextureFormat f = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8) ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;
            var rt = new RenderTexture(w, h, 0, f) { name = "PropCastShadows_" + map.Id, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            rt.Create();
            RenderTexture tmp = RenderTexture.GetTemporary(w, h, 0, f);
            var cb = new CommandBuffer { name = "PropCastShadowsBake" };
            cb.SetRenderTarget(tmp);
            cb.ClearRenderTarget(false, true, Color.clear);
            cb.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.Ortho(0f, GameConfig.MapW, 0f, GameConfig.MapH, -100f, 100f));
            float len = FakeSun.LengthFactor;
            Vector2 sun = FakeSun.Direction;
            int drawn = 0;
            foreach (MapProp p in map.Props)
            {
                PropDef d;
                if (!VillagePropDefs.TryGet(p.Type, out d)) continue;
                float alpha = CastAlpha(StaticPropShadows.Classify(p.Type, d));
                if (alpha <= 0f) continue;
                Texture2D tex = SpriteSheet.TryLoad("Sprites/village/props/" + p.Type);
                if (tex == null) continue;
                float sw = tex.width / 4f, sh = tex.height / 4f; // MapScenery: 4 px per world unit, pivot at the bottom centre
                float bx = (p.Tx + d.FootW * 0.5f) * GameConfig.TileUnits, by = GameConfig.MapH - (p.Ty + 1) * GameConfig.TileUnits;
                // quad (u, v) in 0..1 -> ground: x = bx + (u - 0.5) sw + sun.x len v sh, y = by + sun.y len v sh
                var m = Matrix4x4.identity;
                m.m00 = sw; m.m01 = sun.x * len * sh; m.m03 = bx - 0.5f * sw;
                m.m10 = 0f; m.m11 = sun.y * len * sh; m.m13 = by;
                cb.SetGlobalTexture(SrcId, tex);
                cb.SetGlobalFloat(AlphaId, alpha);
                cb.DrawMesh(Quad(), m, mat, 0, PassSilhouette);
                drawn++;
            }
            // soft penumbra: blur tmp -> rt -> tmp, then the cast result into rt and the contact ellipses over it (max)
            cb.SetGlobalVector(BlurId, new Vector4(1.5f, 0f, 0f, 0f));
            cb.Blit(tmp, rt, mat, PassBlur);
            cb.SetGlobalVector(BlurId, new Vector4(0f, 1.5f, 0f, 0f));
            cb.Blit(rt, tmp, mat, PassBlur);
            cb.SetRenderTarget(rt);
            cb.ClearRenderTarget(false, true, Color.clear);
            cb.Blit(tmp, rt, mat, PassMax);
            if (contact != null) cb.Blit(contact, rt, mat, PassMax);
            Graphics.ExecuteCommandBuffer(cb);
            cb.Release();
            RenderTexture.ReleaseTemporary(tmp);
            ReadBack(rt);
            Debug.Log("[ZenoniaProto] Prop cast shadows " + map.Id + ": " + drawn + " silhouettes");
            return rt;
        }

        /// <summary>Cast alpha by rule (ChatGPT: cast 20-32 %); flat decor, fences, bridges cast nothing beyond their contact.</summary>
        static float CastAlpha(StaticPropShadows.Shape s)
        {
            switch (s)
            {
                case StaticPropShadows.Shape.Tree: return FakeSun.TreeCrownAlpha;
                case StaticPropShadows.Shape.House: return FakeSun.HouseCastAlpha;
                case StaticPropShadows.Shape.Rock: return FakeSun.RockCastAlpha;
                default: return 0f;
            }
        }

        /// <summary>Static shadow strength (0..1, cast + contact, no units) at a world point of the map baked last; 0 outside or before a bake.</summary>
        public static float Field(Vector2 world)
        {
            if (field == null) return 0f;
            int x = Mathf.FloorToInt(world.x), y = Mathf.FloorToInt(world.y);
            if (x < 0 || y < 0 || x >= fieldW || y >= fieldH) return 0f;
            return field[y * fieldW + x] / 255f;
        }

        /// <summary>The bake lost its contents (device reset, mobile pause): the caller rebakes.</summary>
        public static bool Lost(RenderTexture rt) { return rt != null && !rt.IsCreated(); }

        /// <summary>One synchronous read of the baked texture into a byte field for CPU sampling (map load only; ~1 MB on a 72x48 map).</summary>
        static void ReadBack(RenderTexture rt)
        {
            RenderTexture copy = RenderTexture.GetTemporary(rt.width, rt.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(rt, copy);
            RenderTexture prev = RenderTexture.active;
            var t = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = copy;
                t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
                Color32[] px = t.GetPixels32();
                field = new byte[px.Length];
                for (int i = 0; i < px.Length; i++) field[i] = px[i].r;
                fieldW = rt.width; fieldH = rt.height;
                DumpIfAsked(t);
            }
            catch (System.Exception e) { Debug.LogWarning("[ZenoniaProto] Prop shadow read-back failed, units take no canopy shade: " + e.Message); field = null; }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(copy);
                Object.Destroy(t);
            }
        }

        /// <summary>`--dump-prop-shadows <file.png>` (test builds): writes the baked map (white = shadow, north up) to check direction and length.</summary>
        static void DumpIfAsked(Texture2D t)
        {
            if (ReleaseBuild.IsRelease) return;
            string[] a = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(a, "--dump-prop-shadows");
            if (i < 0 || i + 1 >= a.Length) return;
            Color32[] px = t.GetPixels32();
            for (int k = 0; k < px.Length; k++) { byte v = (byte)Mathf.Min(255, px[k].r * 3); px[k] = new Color32(v, v, v, 255); }
            t.SetPixels32(px);
            System.IO.File.WriteAllBytes(a[i + 1], t.EncodeToPNG());
            Debug.Log("[ZenoniaProto] Prop shadow bake written to " + a[i + 1]);
        }

        static bool Ready()
        {
            if (!tried)
            {
                tried = true;
                Shader s = Resources.Load<Shader>("Shaders/PropShadowBake");
                if (s != null && s.isSupported) mat = new Material(s) { name = "PropShadowBake", hideFlags = HideFlags.DontSave };
                else Debug.LogWarning("[ZenoniaProto] PropShadowBake shader missing: RO3 maps fall back to the ellipse prop shadows");
            }
            return mat != null;
        }

        static Mesh Quad()
        {
            if (quad != null) return quad;
            quad = new Mesh { name = "PropShadowQuad", hideFlags = HideFlags.DontSave };
            quad.vertices = new[] { new Vector3(0f, 0f, 0f), new Vector3(0f, 1f, 0f), new Vector3(1f, 1f, 0f), new Vector3(1f, 0f, 0f) };
            quad.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            quad.RecalculateBounds();
            return quad;
        }
    }
}
