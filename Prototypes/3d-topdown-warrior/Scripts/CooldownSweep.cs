using UnityEngine;

/// <summary>
/// Radial cooldown veil for one skill slot (ChatGPT V2: #08121C, the dark part shrinks clockwise from 12 o'clock). OnGUI cannot fill
/// radially, so this keeps a small texture per slot and repaints it only when the remaining share changes by 1 %: the rounded-square
/// shape (corner radius as the slot art) and each pixel's clock angle are worked out once, so a repaint just writes a cached Color32 array.
/// </summary>
public class CooldownSweep
{
    const int S = 96;
    public readonly Texture2D Tex;
    readonly Color32[] px = new Color32[S * S];
    readonly float[] angle = new float[S * S];   // 0..1 clockwise from the top
    readonly byte[] shape = new byte[S * S];     // rounded-square alpha
    int shown = -1;

    public CooldownSweep()
    {
        Tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        const float corner = 0.17f;
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float u = (x + 0.5f) / S - 0.5f, v = (y + 0.5f) / S - 0.5f;
            float a = Mathf.Atan2(u, v) / (2f * Mathf.PI); if (a < 0f) a += 1f;   // texture y is up: 0 at the top, growing clockwise
            angle[y * S + x] = a;
            float qx = Mathf.Max(0f, Mathf.Abs(u) - (0.5f - corner)), qy = Mathf.Max(0f, Mathf.Abs(v) - (0.5f - corner));
            float d = Mathf.Sqrt(qx * qx + qy * qy) - corner;                    // signed distance to the rounded square
            shape[y * S + x] = (byte)(255 * Mathf.Clamp01(-d * S / 1.5f));
        }
        Set(0f);
    }

    /// <summary>`left01` = remaining share of the cooldown (1 = just cast): that share stays dark, ending at 12 o'clock.</summary>
    public void Set(float left01)
    {
        int step = Mathf.Clamp(Mathf.CeilToInt(left01 * 100f), 0, 100);
        if (step == shown) return;
        shown = step;
        float edge = 1f - step / 100f;   // the cleared part grows clockwise from the top
        var c = new Color32(0x08, 0x12, 0x1C, 0);
        for (int i = 0; i < px.Length; i++) { c.a = angle[i] >= edge ? shape[i] : (byte)0; px[i] = c; }
        Tex.SetPixels32(px);
        Tex.Apply(false);
    }
}
