using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// A TEST bone rig drawn instead of the painted frames facing the camera (HeroRenderer.Rig.cs): the cut-out rig (HeroRigView,
    /// `--hero-rig`) or the skinned mesh rig (HeroMeshRigView, `--hero-rig-mesh`). HeroLayeredView.Rig.cs hangs the layered hair /
    /// headgear / sword / shield on it through these members (cell px, y down; angles screen degrees clockwise, 0 = rest).
    /// </summary>
    public interface IHeroRig
    {
        int CellW { get; }
        int FeetY { get; }
        HeroRigSet.Attach Hair { get; }
        HeroRigSet.Attach Weapon { get; }
        HeroRigSet.Attach Offhand { get; }
        int HairBone { get; }
        int WeaponBone { get; }
        int OffhandBone { get; }
        bool WeaponBehind { get; }
        float WristAngle { get; }

        /// <summary>Poses `anim` at `phase` and draws it; false = no such anim (frames are drawn).</summary>
        bool Show(string anim, float phase, int orderBase, Color tint, bool weaponOn);
        void Hide();
        /// <summary>Cell px -> local units around the feet.</summary>
        Vector2 Local(Vector2 cell);
        /// <summary>A rest-pose cell point carried by bone b to its posed place (cell px).</summary>
        Vector2 Carry(int bone, float restX, float restY);
        /// <summary>Turn of bone b from its rest direction (screen degrees clockwise).</summary>
        float BoneAngle(int bone);
    }
}
