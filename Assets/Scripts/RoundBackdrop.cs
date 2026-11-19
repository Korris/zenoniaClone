using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The opaque backing of the RO Soft HUD's round portraits (RoHudPortraits.Frame): a soft sky radial gradient like the disc behind the
    /// hero in 02-hud.png (pale cream-white just above the centre, light sky blue at the rim), the CSS
    /// `radial-gradient(circle at 50% 40%, #F4F7FC, #BBD2F1)`. Built once in code, bilinear, so it stays smooth at any size; the circle edge
    /// itself comes from the ChatGPT mask disc and hides under the ring.
    /// </summary>
    public static class RoundBackdrop
    {
        const int Side = 128;
        static readonly Color Centre = new Color32(0xF4, 0xF7, 0xFC, 0xFF), Rim = new Color32(0xBB, 0xD2, 0xF1, 0xFF);
        static Texture2D texture;

        public static Texture2D Texture
        {
            get
            {
                if (texture != null) return texture;
                texture = new Texture2D(Side, Side, TextureFormat.RGBA32, false) { name = "RoundBackdrop", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color[Side * Side];
                Vector2 c = new Vector2(0.5f, 0.6f); // the light spot a little above the centre (texture y grows upwards)
                for (int y = 0; y < Side; y++)
                    for (int x = 0; x < Side; x++)
                    {
                        Vector2 p = new Vector2((x + 0.5f) / Side, (y + 0.5f) / Side);
                        float t = Mathf.Clamp01(Vector2.Distance(p, c) / 0.6f);
                        px[y * Side + x] = Color.Lerp(Centre, Rim, t * t * (3f - 2f * t)); // smoothstep: no visible band
                    }
                texture.SetPixels(px);
                texture.Apply(false, true);
                return texture;
            }
        }
    }
}
