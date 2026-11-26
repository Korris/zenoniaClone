using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Skinned MESH rig of one painted direction (TEST 25/11, Phase 2 of plans/261125-0045-warrior-skeletal-rig-prototype, `--hero-rig-mesh`):
    /// Data/hero-layered/ro-fantasy/&lt;class&gt;/rig-mesh-down.json written by Tools/build-warrior-rig-mesh-down.py. Bones carry a rest
    /// joint + end (the bone axis that a key's `sy` scales = foreshortening along a limb); meshes are triangle grids over a part texture with
    /// up to 4 bone weights per vertex, named by bone (weights never hard-wired to one picture: a new parts set = new textures + json).
    /// Several meshes may share a slot (`slot` = base mesh name): a key's `sw` "armL=thrust" swaps the slot to mesh "armL@thrust"
    /// (sprite swap at a key pose). Anims reuse HeroRigSet's key tables (+ lb / rb arm behind the body, sw swaps). Cell / feet / Ppu = frame body.
    /// Sprites + skinning data: HeroMeshRigSprites.cs.
    /// </summary>
    public sealed partial class HeroMeshRigSet
    {
        [Serializable] public class Bone { public string name, parent; public float x, y, ex, ey; public bool spring; }
        [Serializable] public class Part { public string name, slot, sprite; public int z; public float[] v, u, bw; public int[] t, bi; }
        [Serializable] class RigFile { public int cellW = 320, cellH = 256, feetY = 244, bodyPx = 200; public string sheets; public Bone[] bones; public Part[] meshes; public HeroRigSet.Attach hair, weapon, offhand; public HeroRigSet.Anim[] anims; }

        static readonly Dictionary<string, HeroMeshRigSet> Cache = new Dictionary<string, HeroMeshRigSet>();

        public int CellW, CellH, FeetY;
        public float Ppu;
        public Bone[] Bones;
        public int[] BoneParent;
        public float[] RestAxis;          // screen degrees clockwise turning (0, 1) (cell down) onto the bone axis
        public Part[] Meshes;
        public Sprite[] MeshSprites;
        public string[] Slots;            // distinct slot names, back to front by z
        public Attach3 Hair, Weapon, Offhand;
        readonly Dictionary<string, HeroRigSet.Anim> anims = new Dictionary<string, HeroRigSet.Anim>();
        readonly Dictionary<string, int> boneIndex = new Dictionary<string, int>();

        public struct Attach3 { public HeroRigSet.Attach a; public int bone; }

        public static HeroMeshRigSet Load(string path)
        {
            HeroMeshRigSet s;
            if (Cache.TryGetValue(path, out s)) return s;
            Cache[path] = s = TryRead(path);
            return s;
        }

        public HeroRigSet.Anim GetAnim(string name) { HeroRigSet.Anim a; return name != null && anims.TryGetValue(name, out a) ? a : null; }
        public int BoneOf(string name) { int i; return name != null && boneIndex.TryGetValue(name, out i) ? i : -1; }

        /// <summary>Mesh index for `slot` under the swap string of a key ("armL=thrust,..."), else the slot's base mesh.</summary>
        public int MeshFor(string slot, string swaps)
        {
            string want = slot;
            if (!string.IsNullOrEmpty(swaps))
                foreach (string kv in swaps.Split(','))
                {
                    int eq = kv.IndexOf('=');
                    if (eq > 0 && kv.Substring(0, eq) == slot) want = slot + "@" + kv.Substring(eq + 1);
                }
            int fallback = -1;
            for (int i = 0; i < Meshes.Length; i++)
            {
                if (Meshes[i].name == want) return i;
                if (Meshes[i].name == slot) fallback = i;
            }
            return fallback;
        }

        /// <summary>Cell px (y down) -> local units around the feet (sprite space of every mesh: pivot = feet).</summary>
        public Vector2 Local(Vector2 cell) { return new Vector2((cell.x - CellW * 0.5f) / Ppu, (FeetY - cell.y) / Ppu); }

        static HeroMeshRigSet TryRead(string path)
        {
            try
            {
                TextAsset t = Resources.Load<TextAsset>(path);
                if (t == null) return null;
                RigFile f = JsonUtility.FromJson<RigFile>(t.text);
                if (f == null || f.bones == null || f.meshes == null || f.bodyPx <= 0) return null;
                var s = new HeroMeshRigSet { CellW = f.cellW, CellH = f.cellH, FeetY = f.feetY, Ppu = f.bodyPx / HeroLayeredSet.BodyUnits, Bones = f.bones, Meshes = f.meshes };
                int n = f.bones.Length;
                s.BoneParent = new int[n];
                s.RestAxis = new float[n];
                for (int i = 0; i < n; i++) s.boneIndex[f.bones[i].name] = i;
                for (int i = 0; i < n; i++)
                {
                    Bone b = f.bones[i];
                    s.BoneParent[i] = s.BoneOf(b.parent);
                    if (s.BoneParent[i] >= i) { Debug.LogWarning("[ZenoniaProto] mesh rig " + path + ": bone " + b.name + " listed before its parent"); return null; }
                    s.RestAxis[i] = Mathf.Atan2(-(b.ex - b.x), b.ey - b.y) * Mathf.Rad2Deg;
                }
                s.Hair = s.Attach(f.hair); s.Weapon = s.Attach(f.weapon); s.Offhand = s.Attach(f.offhand);
                if (f.anims != null) foreach (HeroRigSet.Anim a in f.anims) if (a.keys != null && a.keys.Length > 0) s.anims[a.name] = a;
                var slots = new List<string>();
                var meshes = new List<Part>(f.meshes);
                meshes.Sort((p, q) => p.z.CompareTo(q.z));
                foreach (Part m in meshes) if (!slots.Contains(m.slot)) slots.Add(m.slot);
                s.Slots = slots.ToArray();
                if (!s.BuildSprites(f.sheets, path)) return null;
                return s;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ZenoniaProto] mesh rig " + path + " unreadable: " + e.Message);
                return null;
            }
        }

        Attach3 Attach(HeroRigSet.Attach a) { return new Attach3 { a = a, bone = a != null ? BoneOf(a.bone) : -1 }; }
    }
}
