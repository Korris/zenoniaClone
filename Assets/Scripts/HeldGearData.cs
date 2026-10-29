using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    [Serializable] public class GripInfo { public int w; public int h; public int gripX; public int gripY; }
    [Serializable] public class OverlayEntry { public string name; public string glow; public GripInfo baseGrip; public GripInfo enhancedGrip; }
    /// <summary>Per-frame grip anchor in the 192px frame (x,y px from top-left; angle deg clockwise; s = frame px per art px).</summary>
    [Serializable] public class HeldAnchor { public float x; public float y; public float angle; public float s = 1f; public float sx = 1f; public bool behind; public bool hidden; public bool fallback; }
    [Serializable] public class AnchorSet { public string part; public string anim; public string dir; public HeldAnchor[] frames; }
    [Serializable] public class HeldGearFile { public OverlayEntry[] sword; public OverlayEntry[] shield; public AnchorSet[] anchors; }

    /// <summary>Warrior sword/shield overlay manifests + per-frame anchors, exported from the web game (Resources/Data/held-gear.json).</summary>
    public class HeldGearData
    {
        public OverlayEntry[] Sword;
        public OverlayEntry[] Shield;
        readonly Dictionary<string, HeldAnchor[]> anchors = new Dictionary<string, HeldAnchor[]>();

        public static HeldGearData Load()
        {
            try
            {
                var asset = Resources.Load<TextAsset>("Data/held-gear");
                if (asset == null) { Debug.LogError("[ZenoniaProto] Resources/Data/held-gear.json missing"); return null; }
                HeldGearFile file = JsonUtility.FromJson<HeldGearFile>(asset.text);
                if (file == null || file.sword == null || file.shield == null || file.anchors == null)
                {
                    Debug.LogError("[ZenoniaProto] held-gear.json malformed");
                    return null;
                }
                var data = new HeldGearData { Sword = file.sword, Shield = file.shield };
                foreach (AnchorSet set in file.anchors) data.anchors[Key(set.part, set.anim, set.dir)] = set.frames;
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError("[ZenoniaProto] held-gear.json parse failed: " + e.Message);
                return null;
            }
        }

        static string Key(string part, string anim, string dir) { return part + "/" + anim + "/" + dir; }

        /// <summary>Anchor for a part/animation/direction/frame, or null when the data has none.</summary>
        public HeldAnchor Get(string part, string anim, string dir, int frame)
        {
            HeldAnchor[] list;
            if (!anchors.TryGetValue(Key(part, anim, dir), out list) || list == null) return null;
            return frame >= 0 && frame < list.Length ? list[frame] : null;
        }
    }
}
