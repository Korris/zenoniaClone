using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Draws a HeroRigSet (TEST cut-out bone rig, `--hero-rig`): one SpriteRenderer per part under its own root next to the layered
    /// view's root (inside the renderer's lean, so lean / lift / lunge / billboard / figure scale apply as to the frame art). Every part
    /// pivots on its bone joint; position / angle / own scale come from HeroRigPose each frame. Parts share the body sub-order (3) and sort by
    /// a tiny z step (list order back to front, like the layered hair); the sword-hand forearm (`hand`) takes the fist order (6) when the
    /// sword is in front, so the fist covers the handle as the frame art's hand layer does. Hair / sword / shield: HeroLayeredView.Rig.cs.
    /// </summary>
    public sealed class HeroRigView : IHeroRig
    {
        public const float PartZStep = -0.00005f; // 16 parts stay in front of nothing the layered view puts at -0.001 (hair)
        readonly HeroRigSet set;
        readonly HeroRigPose pose;
        readonly Transform root;
        readonly SpriteRenderer[] parts;

        public HeroRigSet Set { get { return set; } }
        public HeroRigPose Pose { get { return pose; } }
        public int CellW { get { return set.CellW; } }
        public int FeetY { get { return set.FeetY; } }
        public HeroRigSet.Attach Hair { get { return set.Hair; } }
        public HeroRigSet.Attach Weapon { get { return set.Weapon; } }
        public HeroRigSet.Attach Offhand { get { return set.Offhand; } }
        public int HairBone { get { return set.HairBone; } }
        public int WeaponBone { get { return set.WeaponBone; } }
        public int OffhandBone { get { return set.OffhandBone; } }
        public bool WeaponBehind { get { return pose.WeaponBehind; } }
        public float WristAngle { get { return pose.WristAngle; } }
        public Vector2 Carry(int bone, float restX, float restY) { return pose.Carry(bone, restX, restY); }
        public float BoneAngle(int bone) { return pose.Angle[bone]; }

        public HeroRigView(HeroRigSet rigSet, Transform parent)
        {
            set = rigSet;
            pose = new HeroRigPose(rigSet);
            root = new GameObject("Rig_Root").transform;
            root.SetParent(parent, false);
            parts = new SpriteRenderer[rigSet.Parts.Length];
            for (int i = 0; i < parts.Length; i++) parts[i] = ProceduralSprites.Child(root, "Rig_" + rigSet.Parts[i].name, null);
        }

        public void Hide() { foreach (SpriteRenderer r in parts) r.sprite = null; }

        /// <summary>Cell px (y down) -> local units around the feet (as HeroLayeredView.Local).</summary>
        public Vector2 Local(Vector2 cell)
        {
            return new Vector2((cell.x - set.CellW * 0.5f) / set.Ppu, (set.FeetY - cell.y) / set.Ppu);
        }

        /// <summary>Poses `anim` at `phase` and draws every part. weaponFront: the sword is drawn in front (fist order for the `hand` part).</summary>
        public bool Show(string anim, float phase, int orderBase, Color tint, bool weaponOn)
        {
            HeroRigSet.Anim a = set.GetAnim(anim);
            if (a == null) { Hide(); return false; }
            pose.Evaluate(a, phase);
            bool swordFront = weaponOn && !pose.WeaponBehind;
            for (int i = 0; i < parts.Length; i++)
            {
                SpriteRenderer r = parts[i];
                HeroRigSet.Part p = set.Parts[i];
                int b = set.PartBone[i];
                r.sprite = set.PartSprites[i];
                r.color = tint;
                r.sortingOrder = orderBase + (p.hand && swordFront ? 6 : 3);
                Vector2 at = Local(pose.Pos[b]);
                Transform t = r.transform;
                t.localPosition = new Vector3(at.x, at.y, p.z * PartZStep);
                t.localRotation = Quaternion.Euler(0f, 0f, -pose.Angle[b]); // screen clockwise -> Unity counter-clockwise
                t.localScale = new Vector3(pose.Scale[b].x, pose.Scale[b].y, 1f);
            }
            return true;
        }
    }
}
