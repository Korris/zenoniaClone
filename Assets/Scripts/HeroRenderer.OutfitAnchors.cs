using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    public partial class HeroRenderer
    {
        // Clothing can change the idle pose. Place equipment at the painted hand
        // rather than deforming the sleeve to meet an unrelated class-wide grip.
        readonly Dictionary<string, HeldAnchor[]> outfitAnchors = new Dictionary<string, HeldAnchor[]>();

        void LoadOutfitAnchors(string folder)
        {
            outfitAnchors.Clear();
            if (cls == null || string.IsNullOrEmpty(cls.BareFolder) || !cls.BareFolder.EndsWith("/bare")) return;
            if (folder == null) folder = cls.BareFolder.Substring(0, cls.BareFolder.Length - "/bare".Length);
            var asset = Resources.Load<TextAsset>("Sprites/" + folder + "/held-anchors");
            if (asset == null) return;
            try
            {
                HeldGearFile file = JsonUtility.FromJson<HeldGearFile>(asset.text);
                if (file == null || file.anchors == null) return;
                foreach (AnchorSet set in file.anchors)
                    if (set != null && set.frames != null)
                        outfitAnchors[set.part + "/" + set.anim + "/" + set.dir] = set.frames;
            }
            catch (Exception e) { Debug.LogError("[ZenoniaProto] outfit held-anchors: " + e.Message); }
        }

        HeldAnchor OutfitAnchor(string part, string anim, string dir, int frame)
        {
            HeldAnchor[] frames;
            return outfitAnchors.TryGetValue(part + "/" + anim + "/" + dir, out frames)
                && frame >= 0 && frame < frames.Length ? frames[frame] : null;
        }
    }
}
