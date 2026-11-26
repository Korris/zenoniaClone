using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Cut-out bone rig of one painted direction (TEST, 25/11, plans/261125-0045-warrior-skeletal-rig-prototype): Data/hero-layered/ro-fantasy/&lt;class&gt;/rig-&lt;dir&gt;.json
    /// written by Tools/build-warrior-rig-down.py. Bones carry a rest joint in cell px (same cell / feet / Ppu as the frame body, so the
    /// figure lines up with the frame art); every part sprite pivots on its bone's joint. Anims are keyframe tables over a 0..1 phase
    /// (bone angle in screen degrees clockwise, offset in cell px y-down, own scale; per-anim bone lag = follow-through), evaluated every frame (HeroRigPose), so the motion is
    /// continuous instead of stepping 75 ms frames. Hair / weapon / offhand attach points are the frame art's idle-down anchors, carried by a bone.
    /// </summary>
    public sealed class HeroRigSet
    {
        [Serializable] public class Bone { public string name, parent; public float x, y; }
        [Serializable] public class BoneLag { public string n; public float v; }
        [Serializable] public class Part { public string name, bone, sprite; public float left, top; public int z; public bool hand; }
        [Serializable] public class Attach { public string bone; public float x, y, w, angle; }
        [Serializable] public class BoneKey { public string n; public float a, x, y, sx = 1f, sy = 1f; }
        [Serializable] public class Key { public float t; public int e; public bool wb; public float wa; public bool lb, rb; public string sw; public BoneKey[] b; } // lb / rb / sw: mesh rig only (arm behind body, sprite swaps)
        [Serializable] public class Anim { public string name; public float ms = 1000f; public bool loop = true; public Key[] keys; public BoneLag[] lags; }
        [Serializable] class RigFile { public int cellW = 320, cellH = 256, feetY = 244, bodyPx = 200; public string sheets; public Bone[] bones; public Part[] parts; public Attach hair, weapon, offhand; public Anim[] anims; }

        static readonly Dictionary<string, HeroRigSet> Cache = new Dictionary<string, HeroRigSet>();

        public int CellW, CellH, FeetY;
        public float Ppu;
        public Bone[] Bones;
        public Part[] Parts;
        public Sprite[] PartSprites;
        public int[] PartBone, BoneParent;
        public Attach Hair, Weapon, Offhand;
        public int HairBone, WeaponBone, OffhandBone;
        readonly Dictionary<string, Anim> anims = new Dictionary<string, Anim>();
        readonly Dictionary<string, int> boneIndex = new Dictionary<string, int>();

        /// <summary>The rig at a resource path (no extension), or null when missing / broken (the frame art is drawn then).</summary>
        public static HeroRigSet Load(string path)
        {
            HeroRigSet s;
            if (Cache.TryGetValue(path, out s)) return s;
            Cache[path] = s = TryRead(path);
            return s;
        }

        public Anim GetAnim(string name) { Anim a; return name != null && anims.TryGetValue(name, out a) ? a : null; }
        public int BoneOf(string name) { int i; return name != null && boneIndex.TryGetValue(name, out i) ? i : -1; }

        static HeroRigSet TryRead(string path)
        {
            try
            {
                TextAsset t = Resources.Load<TextAsset>(path);
                if (t == null) return null;
                RigFile f = JsonUtility.FromJson<RigFile>(t.text);
                if (f == null || f.bones == null || f.parts == null || f.bodyPx <= 0) return null;
                var s = new HeroRigSet { CellW = f.cellW, CellH = f.cellH, FeetY = f.feetY, Ppu = f.bodyPx / HeroLayeredSet.BodyUnits, Bones = f.bones, Parts = f.parts };
                for (int i = 0; i < f.bones.Length; i++) s.boneIndex[f.bones[i].name] = i;
                s.BoneParent = new int[f.bones.Length];
                for (int i = 0; i < f.bones.Length; i++)
                {
                    s.BoneParent[i] = s.BoneOf(f.bones[i].parent);
                    if (s.BoneParent[i] >= i) { Debug.LogWarning("[ZenoniaProto] rig " + path + ": bone " + f.bones[i].name + " listed before its parent"); return null; }
                }
                s.PartSprites = new Sprite[f.parts.Length];
                s.PartBone = new int[f.parts.Length];
                for (int i = 0; i < f.parts.Length; i++)
                {
                    Part p = f.parts[i];
                    int b = s.BoneOf(p.bone);
                    if (b < 0) { Debug.LogWarning("[ZenoniaProto] rig " + path + ": part " + p.name + " has no bone"); return null; }
                    s.PartBone[i] = b;
                    string sheet = f.sheets + p.sprite;
                    Texture2D tex = SpriteSheet.TryLoad(sheet);
                    if (tex == null) { Debug.LogWarning("[ZenoniaProto] rig " + path + ": missing " + sheet); return null; }
                    // pivot = the bone joint inside the part image (part image top-left sits at left, top of the cell at rest)
                    Vector2 pivot = new Vector2((f.bones[b].x - p.left) / tex.width, 1f - (f.bones[b].y - p.top) / tex.height);
                    s.PartSprites[i] = SpriteSheet.Whole(sheet, s.Ppu, pivot);
                }
                s.Hair = f.hair; s.Weapon = f.weapon; s.Offhand = f.offhand;
                s.HairBone = f.hair != null ? s.BoneOf(f.hair.bone) : -1;
                s.WeaponBone = f.weapon != null ? s.BoneOf(f.weapon.bone) : -1;
                s.OffhandBone = f.offhand != null ? s.BoneOf(f.offhand.bone) : -1;
                if (f.anims != null) foreach (Anim a in f.anims) if (a.keys != null && a.keys.Length > 0) s.anims[a.name] = a;
                return s;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ZenoniaProto] rig " + path + " unreadable: " + e.Message);
                return null;
            }
        }
    }
}
