using System.Collections;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Part of --appearance-test (DebugCapture.Appearance.cs): the look step's live hero. It only stands idle facing down (the turn arrows and the
    /// Idle / Walk / Attack tabs are gone), its texture has one texel per screen pixel (the old fixed 16 px/unit texture was stretched and blurred),
    /// and the weapon, offhand, fist and hair ride the code breath with the body: their local y over the body's y-scale stays constant while the
    /// body scale changes. Shots at the top and the bottom of the breath, twice, for the frame strip in the report. BreathCheck is shared with
    /// --record-layered-breath (DebugCapture.LayeredBreath.cs).
    /// </summary>
    public partial class DebugCapture
    {
        static readonly string[] RemovedPreviewButtons = { "TurnLeft", "TurnRight", "Motion0", "Motion1", "Motion2" };
        static readonly string[] BreathLayers = { "L_WeaponFront", "L_WeaponBack", "L_OffhandFront", "L_OffhandBack", "L_Hand", "L_Hair" };

        IEnumerator PreviewIdleDownChecks(string cls)
        {
            yield return WaitSeconds(0.3f);
            bool gone = true;
            foreach (string b in RemovedPreviewButtons) gone &= GameObject.Find(b) == null;
            A((gone ? "PASS" : "FAIL") + " " + cls + ": no turn arrows, no idle / walk / attack tabs");
            HeroRenderer v = CharacterCustomizeScreen.TestPreviewHero;
            A((v != null && v.Dir8 == 0 ? "PASS" : "FAIL") + " " + cls + ": the preview faces down (Dir8 " + (v != null ? v.Dir8 : -1) + ")");
            HeroPreviewStage stage = CharacterCustomizeScreen.TestPreview;
            float screenH = stage != null ? stage.ViewScreenHeight : 0f;
            int texH = stage != null ? stage.TextureSize.y : 0;
            A((texH > 0 && Mathf.Abs(texH - screenH) <= 1f ? "PASS" : "FAIL") + " " + cls + ": preview texture " + (stage != null ? stage.TextureSize.ToString() : "-") + " for a view " + screenH.ToString("0.0") + " px tall on screen");
        }

        /// <summary>Breath top / bottom shots of the preview hero, with the layer checks.</summary>
        IEnumerator BreathShots(string cls, string shot)
        {
            yield return BreathCheck(new[] { CharacterCustomizeScreen.TestPreviewHero }, cls, shot, A);
        }

        /// <summary>
        /// Heroes breathing in step (same breath phase): waits for the top / bottom of the first one's breath twice, shoots each, and logs PASS / FAIL
        /// per hero: the body breathes, every shown held layer keeps (local y / body y-scale) constant, the main weapon moves between top and bottom.
        /// </summary>
        IEnumerator BreathCheck(HeroRenderer[] views, string label, string shot, System.Action<string> log)
        {
            Transform lead = views.Length > 0 && views[0] != null ? Deep(views[0].transform, "L_Body") : null;
            if (lead == null) { log("FAIL " + label + ": no layered body"); yield break; }
            int n = views.Length, m = BreathLayers.Length;
            var lo = new float[n]; var hi = new float[n];
            var rLo = new float[n, m]; var rHi = new float[n, m];
            var wTop = new float[n]; var wBottom = new float[n];
            for (int h = 0; h < n; h++) { lo[h] = float.MaxValue; hi[h] = float.MinValue; for (int i = 0; i < m; i++) { rLo[h, i] = float.MaxValue; rHi[h, i] = float.MinValue; } }
            for (int k = 0; k < 4; k++)
            {
                bool top = k % 2 == 0;
                float until = Time.realtimeSinceStartup + 2f;
                while (Time.realtimeSinceStartup < until && (top ? lead.localScale.y < 1.0073f : lead.localScale.y > 0.9927f)) yield return null;
                for (int h = 0; h < n; h++)
                {
                    Transform body = Deep(views[h].transform, "L_Body");
                    if (body == null) continue;
                    float sy = body.localScale.y;
                    lo[h] = Mathf.Min(lo[h], sy); hi[h] = Mathf.Max(hi[h], sy);
                    for (int i = 0; i < m; i++)
                    {
                        Transform t = Deep(views[h].transform, BreathLayers[i]);
                        SpriteRenderer r = t != null ? t.GetComponent<SpriteRenderer>() : null;
                        if (r == null || r.sprite == null) continue;
                        float y = BreathLayers[i] == "L_Hand" ? t.localScale.y : t.localPosition.y; // the fist layer is scaled with the body, the others are moved
                        rLo[h, i] = Mathf.Min(rLo[h, i], y / sy); rHi[h, i] = Mathf.Max(rHi[h, i], y / sy);
                        if (i < 2) { if (top) wTop[h] = t.localPosition.y; else wBottom[h] = t.localPosition.y; } // the main weapon, in front of or behind the body
                    }
                }
                yield return Shot(shot + "-" + k + (top ? "-top" : "-bottom"));
            }
            for (int h = 0; h < n; h++)
            {
                string who = label + (n > 1 ? " Dir8 " + views[h].Dir8 : "");
                bool breathes = hi[h] - lo[h] > 0.012f, follows = true;
                string off = "";
                for (int i = 0; i < m; i++)
                {
                    if (rLo[h, i] == float.MaxValue) continue;
                    float spread = rHi[h, i] - rLo[h, i];
                    if (Mathf.Abs(spread) >= 1e-4f) { follows = false; off += " " + BreathLayers[i] + " " + spread.ToString("0.00000"); }
                }
                bool moved = Mathf.Abs(wTop[h] - wBottom[h]) > 1e-3f;
                log((breathes && follows && moved ? "PASS" : "FAIL") + " " + who + ": body y-scale " + lo[h].ToString("0.0000") + ".." + hi[h].ToString("0.0000")
                    + ", weapon y " + wTop[h].ToString("0.0000") + " / " + wBottom[h].ToString("0.0000") + (follows ? ", fist / hair / weapon / offhand ride the breath" : ", off the breath:" + off));
            }
        }

        static Transform Deep(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform f = Deep(t.GetChild(i), name);
                if (f != null) return f;
            }
            return null;
        }
    }
}
