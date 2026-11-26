using UnityEngine;
using UnityEngine.U2D.Animation;

namespace ZenoniaProto
{
    /// <summary>
    /// Draws a HeroMeshRigSet (TEST skinned mesh rig, `--hero-rig-mesh`) with Unity 2D Animation: one bone Transform per json bone (flat
    /// under the rig root, TRS = posed joint, rest axis + turn, own sx / sy: no scale inheritance), one SpriteRenderer + SpriteSkin per slot
    /// (cape, body, armR, armL) on the runtime skinned sprites (HeroMeshRigSprites.cs). Sprite swap at key poses = the slot renderer's sprite
    /// changes to another mesh of the same slot (SpriteSkin rebinds by itself). Draw order: one sub-order per slot (SubOrder); an arm flagged
    /// behind (key lb / rb) goes to the cape's order; the sword arm takes the fist order (6) when the sword is in front.
    /// If SpriteSkin is not Ready on these sprites the same data is skinned on the CPU (HeroMeshRigCpuSkin). Log line says which.
    /// </summary>
    public sealed class HeroMeshRigView : IHeroRig
    {
        const float ZStep = -0.00005f;
        readonly HeroMeshRigSet set;
        readonly HeroMeshRigPose pose;
        readonly Transform[] bones;
        readonly SpriteRenderer[] slots;
        readonly int[] shown;
        readonly HeroMeshRigCpuSkin cpu;
        float lastTime = -1f;

        public bool UsesSpriteSkin { get { return cpu == null; } }
        public int CellW { get { return set.CellW; } }
        public int FeetY { get { return set.FeetY; } }
        public HeroRigSet.Attach Hair { get { return set.Hair.a; } }
        public HeroRigSet.Attach Weapon { get { return set.Weapon.a; } }
        public HeroRigSet.Attach Offhand { get { return set.Offhand.a; } }
        public int HairBone { get { return set.Hair.bone; } }
        public int WeaponBone { get { return set.Weapon.bone; } }
        public int OffhandBone { get { return set.Offhand.bone; } }
        public bool WeaponBehind { get { return pose.WeaponBehind; } }
        public float WristAngle { get { return pose.WristAngle; } }

        public HeroMeshRigView(HeroMeshRigSet rigSet, Transform parent)
        {
            set = rigSet;
            pose = new HeroMeshRigPose(rigSet);
            Transform root = new GameObject("MeshRig_Root").transform;
            root.SetParent(parent, false);
            bones = new Transform[set.Bones.Length];
            for (int b = 0; b < bones.Length; b++)
            {
                bones[b] = new GameObject("Bone_" + set.Bones[b].name).transform;
                bones[b].SetParent(root, false);
                Matrix4x4 m = set.RestMatrix(b);
                bones[b].localPosition = m.GetColumn(3);
                bones[b].localRotation = Quaternion.Euler(0f, 0f, -set.RestAxis[b]);
            }
            slots = new SpriteRenderer[set.Slots.Length];
            shown = new int[slots.Length];
            string why = null;
            for (int s = 0; s < slots.Length; s++)
            {
                shown[s] = set.MeshFor(set.Slots[s], null);
                SpriteRenderer r = ProceduralSprites.Child(root, "MeshRig_" + set.Slots[s], set.MeshSprites[shown[s]]);
                r.spriteSortPoint = SpriteSortPoint.Pivot;
                slots[s] = r;
                if (why != null) continue;
                try
                {
                    SpriteSkin skin = r.gameObject.AddComponent<SpriteSkin>();
                    skin.alwaysUpdate = true;
                    skin.SetRootBone(bones[0]);
                    SpriteSkinState st = skin.SetBoneTransforms(bones);
                    if (st != SpriteSkinState.Ready) why = "SpriteSkin state " + st;
                }
                catch (System.Exception e) { why = "SpriteSkin failed: " + e.Message; }
            }
            if (why != null)
            {
                foreach (SpriteRenderer r in slots) { SpriteSkin k = r.GetComponent<SpriteSkin>(); if (k != null) Object.Destroy(k); r.sprite = null; }
                cpu = new HeroMeshRigCpuSkin(set, root);
            }
            Debug.Log("[ZenoniaProto] mesh rig on: " + set.Meshes.Length + " meshes, " + bones.Length + " bones, " + (cpu == null ? "SpriteSkin" : "CPU skinning (" + why + ")"));
            Hide(); // shown by the first Show (facing camera, idle / walk / attack)
        }

        public Vector2 Local(Vector2 cell) { return set.Local(cell); }
        public Vector2 Carry(int bone, float restX, float restY) { return pose.Affine(bone, new Vector2(restX, restY)); }
        public float BoneAngle(int bone) { return pose.Turn[bone]; }

        public void Hide()
        {
            foreach (SpriteRenderer r in slots) r.sprite = null;
            if (cpu != null) cpu.Hide();
            lastTime = -1f;
        }

        /// <summary>Sub-order inside the hero's stride (GameConfig.OrderStride 8): sword behind 1, cape 2, body + hair 3, arms 4 (in front of
        /// the hair when raised), shield 5, sword arm 6 when the sword is in front (its fist covers the handle). Separate orders, not z steps:
        /// in the 2.5D camera a walking hero swapped cape and body that only differed by a z step (round 1: legs vanished behind the cape).</summary>
        static int SubOrder(string slot, bool behind, bool swordFront)
        {
            if (slot == "cape" || behind) return 2;
            if (slot == "body") return 3;
            return slot == "armL" && swordFront ? 6 : 4;
        }

        public bool Show(string anim, float phase, int orderBase, Color tint, bool weaponOn)
        {
            HeroRigSet.Anim a = set.GetAnim(anim);
            if (a == null) { Hide(); return false; }
            float now = Time.time, dt = lastTime < 0f ? 0f : now - lastTime;
            lastTime = now;
            pose.Evaluate(a, phase, dt);
            for (int b = 0; b < bones.Length; b++)
            {
                Vector2 at = set.Local(pose.Pos[b]);
                bones[b].localPosition = new Vector3(at.x, at.y, 0f);
                bones[b].localRotation = Quaternion.Euler(0f, 0f, -(set.RestAxis[b] + pose.Turn[b]));
                bones[b].localScale = new Vector3(pose.Scale[b].x, pose.Scale[b].y, 1f);
            }
            bool swordFront = weaponOn && !pose.WeaponBehind;
            for (int s = 0; s < slots.Length; s++)
            {
                string slot = set.Slots[s];
                bool behind = (slot == "armL" && pose.LeftBehind) || (slot == "armR" && pose.RightBehind);
                float zi = behind ? 0.5f : s;                       // slots are listed back to front: cape 0, body 1, armR 2, armL 3
                int order = orderBase + SubOrder(slot, behind, swordFront);
                int mi = set.MeshFor(slot, pose.Swaps);
                if (cpu != null) { cpu.Draw(s, mi, bones, order, zi * ZStep, tint); continue; }
                SpriteRenderer r = slots[s];
                if (r.sprite != set.MeshSprites[mi]) r.sprite = set.MeshSprites[mi];
                shown[s] = mi;
                r.color = tint;
                r.sortingOrder = order;
                r.transform.localPosition = new Vector3(0f, 0f, zi * ZStep);
            }
            return true;
        }
    }
}
