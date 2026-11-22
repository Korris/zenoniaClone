using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Keeps the RO Soft monster foot labels (Monster.RoFootLabel.cs) readable once the frame's monsters have placed them (LateUpdate):
    ///   - crowd: nearest-to-camera monster first (lowest feet y); a name tab that would overlap a tab already kept hides its tab + text,
    ///     so does a tab under the HP bar of a nearer monster (that bar draws over it); its own HP bar stays (Ragnarok keeps every bar; a pile of five names on top of each other read as soup, 20/11 capture);
    ///   - HUD: a label touching the RO HUD's top row (menu captions "Pet", "Quest" have no backing, the label showed through) or an
    ///     RO panel registered with NameTags.Occlude hides completely, the same rule as the IMGUI name tags.
    /// </summary>
    public class MonsterFootLabelCrowd : MonoBehaviour
    {
        static MonsterFootLabelCrowd instance;
        static readonly List<Monster> labelled = new List<Monster>();
        static readonly List<Rect> kept = new List<Rect>(); // tabs shown so far, and the bars of every nearer label

        /// <summary>Called by a monster that shows its RO label this frame; creates the runner on first use.</summary>
        public static void Register(Monster m)
        {
            if (instance == null)
            {
                var go = new GameObject("MonsterFootLabelCrowd");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<MonsterFootLabelCrowd>();
            }
            labelled.Add(m);
        }

        static readonly System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        static readonly bool timed = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--fps-log") >= 0;
        static int timedFrames, timedLabels;
        static float timedNext;

        void LateUpdate()
        {
            if (!timed) { Pass(); return; }
            int n = labelled.Count;
            watch.Start(); Pass(); watch.Stop();
            timedFrames++; timedLabels += n;
            if (Time.unscaledTime < timedNext) return;
            timedNext = Time.unscaledTime + 5f;
            Debug.Log("[ZenoniaProto] LabelCrowd " + (watch.Elapsed.TotalMilliseconds / Mathf.Max(1, timedFrames)).ToString("F3") + " ms/frame, " + (timedLabels / Mathf.Max(1, timedFrames)) + " labels");
            watch.Reset(); timedFrames = timedLabels = 0;
        }

        void Pass()
        {
            Camera cam = Camera.main;
            if (cam == null || labelled.Count == 0) { labelled.Clear(); return; }
            labelled.RemoveAll(m => m == null);
            labelled.Sort((a, b) => a.Feet.y.CompareTo(b.Feet.y));
            kept.Clear();
            foreach (Monster m in labelled)
            {
                Rect tab, bar, all;
                if (!m.RoFootLabelScreenRects(cam, out tab, out bar, out all)) continue;
                if (NameTags.CoversHud(all)) { m.HideRoFootLabel(true); continue; }
                bool overlaps = false;
                Rect t = new Rect(tab.x + 2f, tab.y + 1f, tab.width - 4f, tab.height - 2f); // touching edges is fine, real overlap is not
                foreach (Rect k in kept) if (k.Overlaps(t)) { overlaps = true; break; }
                if (overlaps) m.HideRoFootLabel(false);
                else kept.Add(tab);
                kept.Add(bar);
            }
            labelled.Clear();
        }
    }
}
