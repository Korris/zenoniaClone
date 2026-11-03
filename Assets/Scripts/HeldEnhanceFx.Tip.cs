using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// +15 signature of HeldEnhanceFx: a light energy blade continuing the weapon past its tip (transparent body, two bright edges, sharp point,
    /// ~4.25 world units ~ 19 screen px at 720p), breathing between 75% and 100% of its length, with a warm bright spark running along it.
    /// It changes the weapon's silhouette, which is what reads at a 90 px hero; it stays at 70% while attacking so the item keeps its identity.
    /// </summary>
    public sealed partial class HeldEnhanceFx
    {
        const float TipLength = 4.25f, TipWidth = 1.6f, TipOverlap = 0.35f, TipBreathSec = 1.5f, TipAlpha = 0.5f, TipInAction = 0.7f;
        const float TipSparkEvery = 1.5f, TipSparkSec = 0.38f, TipSparkW = 0.45f, TipSparkL = 1.2f;

        SpriteRenderer tipBlade, tipSpark;

        void DrawTip(bool show, Color edge, Color core, int order, float now, float alpha, float fit, int motion)
        {
            if (!show || baked.TipDir == Vector2.zero)
            {
                if (tipBlade != null) { tipBlade.enabled = false; tipSpark.enabled = false; }
                return;
            }
            if (tipBlade == null) { tipBlade = NewSprite(FxSprites.TipBlade()); tipSpark = NewSprite(FxSprites.Flame()); }
            Vector3 tip = art.TransformPoint(baked.Tip);
            Vector2 dir = ((Vector2)art.TransformVector(baked.TipDir)).normalized; // world: follows the swing, flips and rotation
            Quaternion rot = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f); // sprites point up (+Y)
            float breath = 0.875f + 0.125f * Mathf.Sin((now + phase) / TipBreathSec * Mathf.PI * 2f);
            float length = TipLength * fit * breath, a = alpha * (motion == 2 ? TipInAction : 1f);
            Vector3 start = tip - (Vector3)(dir * TipOverlap * fit); // tucked a hair into the blade so the two read as one

            tipBlade.enabled = true;
            Transform t = tipBlade.transform;
            t.SetPositionAndRotation(start, rot);
            t.localScale = new Vector3(TipWidth * fit, length, 1f);
            edge.a = TipAlpha * a;
            tipBlade.color = edge;
            tipBlade.sortingOrder = order;

            float k = Mathf.Repeat(now + phase, TipSparkEvery) / TipSparkSec;
            tipSpark.enabled = k < 1f;
            if (!tipSpark.enabled) return;
            Transform s = tipSpark.transform;
            s.SetPositionAndRotation(start + (Vector3)(dir * length * Mathf.Lerp(0.1f, 0.85f, k)), rot);
            s.localScale = new Vector3(TipSparkW * fit, TipSparkL * fit, 1f);
            core.a = 0.95f * Mathf.Sin(k * Mathf.PI) * a;
            tipSpark.color = core;
            tipSpark.sortingOrder = order + 1;
        }
    }
}
