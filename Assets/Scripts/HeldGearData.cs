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
    [Serializable] public class FloatLoop { public string dir; public GripInfo[] frames; }
    /// <summary>
    /// JSON written by Tools/export-held-gear.mjs. Warrior: sword + shield. Wizard / mechanic: `weapon` (staff / gun);
    /// assassin: one dagger manifest shared by its `main` / `off` parts; mechanic / cleric also `companion` (robot / spirit tier art)
    /// + `companionFloat` (tier-1 4-facing float loop).
    /// </summary>
    [Serializable] public class HeldGearFile
    {
        public OverlayEntry[] sword; public OverlayEntry[] shield; public OverlayEntry[] weapon;
        public OverlayEntry[] companion; public FloatLoop[] companionFloat; public AnchorSet[] anchors;
    }

    /// <summary>Overlay manifests + per-frame anchors of one class, exported from the web game (Resources/Data/*.json).</summary>
    public class HeldGearData
    {
        public OverlayEntry[] Companion;
        readonly Dictionary<string, OverlayEntry[]> manifests = new Dictionary<string, OverlayEntry[]>();
        readonly Dictionary<string, GripInfo[]> floats = new Dictionary<string, GripInfo[]>();
        readonly Dictionary<string, HeldAnchor[]> anchors = new Dictionary<string, HeldAnchor[]>();

        /// <summary>Loads Resources/Data/&lt;resource&gt;.json for a class; null (logged) when missing or malformed so the baked frames still play.</summary>
        public static HeldGearData Load(HeroClassDef cls)
        {
            try
            {
                var asset = Resources.Load<TextAsset>("Data/" + cls.GearFile);
                if (asset == null) { Debug.LogError("[ZenoniaProto] Resources/Data/" + cls.GearFile + ".json missing"); return null; }
                HeldGearFile file = JsonUtility.FromJson<HeldGearFile>(asset.text);
                if (file == null || file.anchors == null) { Debug.LogError("[ZenoniaProto] " + cls.GearFile + ".json malformed"); return null; }
                var data = new HeldGearData { Companion = file.companion };
                if (cls.Id == HeroClass.Warrior)
                {
                    if (file.sword == null || file.shield == null) { Debug.LogError("[ZenoniaProto] held-gear.json has no sword / shield"); return null; }
                    data.manifests["sword"] = file.sword;
                    data.manifests["shield"] = file.shield;
                }
                else
                {
                    if (file.weapon == null) { Debug.LogError("[ZenoniaProto] " + cls.GearFile + ".json has no weapon manifest"); return null; }
                    foreach (GearPartSpec part in cls.Parts) data.manifests[part.Id] = file.weapon; // every part draws from the one weapon manifest
                }
                foreach (AnchorSet set in file.anchors) data.anchors[Key(set.part, set.anim, set.dir)] = set.frames;
                if (file.companionFloat != null) foreach (FloatLoop loop in file.companionFloat) data.floats[loop.dir] = loop.frames;
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError("[ZenoniaProto] " + cls.GearFile + ".json parse failed: " + e.Message);
                return null;
            }
        }

        static string Key(string part, string anim, string dir) { return part + "/" + anim + "/" + dir; }

        public OverlayEntry[] Manifest(string part)
        {
            OverlayEntry[] list;
            return manifests.TryGetValue(part, out list) ? list : null;
        }

        /// <summary>Number of gear tiers shared by every part of the class.</summary>
        public int TierCount
        {
            get
            {
                int n = int.MaxValue;
                foreach (var kv in manifests) n = Mathf.Min(n, kv.Value.Length);
                return n == int.MaxValue ? 1 : n;
            }
        }

        /// <summary>Tier-1 companion float loop frames for a facing ("down" / "up" / "left" / "right"), or null.</summary>
        public GripInfo[] CompanionFloat(string dir)
        {
            GripInfo[] loop;
            return floats.TryGetValue(dir, out loop) ? loop : null;
        }

        /// <summary>Anchor for a part/animation/direction/frame, or null when the data has none.</summary>
        public HeldAnchor Get(string part, string anim, string dir, int frame)
        {
            HeldAnchor[] list;
            if (!anchors.TryGetValue(Key(part, anim, dir), out list) || list == null) return null;
            return frame >= 0 && frame < list.Length ? list[frame] : null;
        }
    }
}
