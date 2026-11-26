using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// TEST bone rig on the layered hero (`--hero-rig` cut-out / `--hero-rig-mesh` skinned mesh, HeroRenderer.Rig.cs): the frame body /
    /// fist are hidden and the rig (IHeroRig) draws the figure; the layered view's own hair (dye block kept), headgear, sword and shield
    /// renderers ride the rig's bones: hair / hat on the head (mesh rig: spring hair bone) at the idle-down head anchor, the sword on the
    /// sword forearm at the idle-down grip turned with the arm + the key's wrist angle, the shield on the shield forearm (overHand order).
    /// </summary>
    public sealed partial class HeroLayeredView
    {
        IHeroRig rig;

        public bool HasRig { get { return rig != null; } }

        /// <summary>Builds the rig view once (next to the layered root, same parent = the renderer's lean). make: parent -> rig view.</summary>
        public void AttachRig(System.Func<Transform, IHeroRig> make)
        {
            if (rig != null || make == null) return;
            IHeroRig r = make(root.parent);
            if (r != null && r.CellW == set.CellW && r.FeetY == set.FeetY) rig = r;
            else if (r != null) { r.Hide(); Debug.LogWarning("[ZenoniaProto] rig cell / feet differ from the layered body: rig off"); }
        }

        public void HideRig() { if (rig != null) rig.Hide(); }

        /// <summary>Draws rig anim `anim` (idle / walk / attack) at `phase` instead of a painted frame; false = no such anim (caller draws frames).</summary>
        public bool ShowRig(string anim, float phase, int orderBase, Color tint, bool weaponOn, bool offhandOn)
        {
            if (rig == null || !rig.Show(anim, phase, orderBase, tint, weaponOn)) return false;
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            body.sprite = null;
            hand.sprite = null;
            if (colorsDirty) { ApplyBlock(hair, null, 1f); colorsDirty = false; shownMask = null; } // the frame body re-applies its block on return

            HeroLayeredSet.LayerEntry hat = set.Headgear(headgearId);
            HeroLayeredSet.LayerEntry hairEntry = hat != null && hat.hidesHair ? null : set.Hair(hairStyle);
            RigOnHead(hair, hairEntry, tint, orderBase + 3, 0f, false);
            RigOnHead(hairAcc, hat == null ? hairEntry : null, tint, orderBase + 3, 0f, true);
            RigOnHead(headgear, hat, tint, orderBase + 3, 1f, false);

            // sword: idle grip carried by its bone; angle = grip angle + bone angle + wrist (all screen clockwise)
            bool swordOn = weaponOn && rig.WeaponBone >= 0 && set.Weapon("weapon") != null;
            bool behind = rig.WeaponBehind;
            SpriteRenderer w = behind ? weaponBack : weaponFront, wOther = behind ? weaponFront : weaponBack;
            wOther.sprite = null;
            Draw(w, swordOn ? set.Weapon("weapon") : null, false, tint, behind ? orderBase + 1 : orderBase + 4);
            HeroRigSet.Attach wa = rig.Weapon;
            if (swordOn) RigPlace(w, rig.Carry(rig.WeaponBone, wa.x, wa.y), wa.angle + rig.BoneAngle(rig.WeaponBone) + rig.WristAngle);

            bool shieldOn = offhandOn && rig.OffhandBone >= 0 && set.Weapon("offhand") != null;
            offBack.sprite = null;
            Draw(offFront, shieldOn ? set.Weapon("offhand") : null, false, tint, set.OverHand("offhand") ? orderBase + 8 : orderBase + 5);
            HeroRigSet.Attach oa = rig.Offhand;
            if (shieldOn) RigPlace(offFront, rig.Carry(rig.OffhandBone, oa.x, oa.y), oa.angle + rig.BoneAngle(rig.OffhandBone));
            return true;
        }

        void RigPlace(SpriteRenderer r, Vector2 cell, float angle)
        {
            Vector2 at = rig.Local(cell);
            r.transform.localPosition = new Vector3(at.x, at.y, 0f);
            r.transform.localRotation = Quaternion.Euler(0f, 0f, -angle);
            r.transform.localScale = Vector3.one;
        }

        void RigOnHead(SpriteRenderer r, HeroLayeredSet.LayerEntry e, Color tint, int order, float dropScale, bool accessory)
        {
            Sprite s = e == null || rig.HairBone < 0 ? null : accessory ? set.AccessoryCell(e, 0) : set.LayerCell(e, 0);
            Draw(r, s, false, tint, order);
            if (s == null) return;
            HeroRigSet.Attach h = rig.Hair;
            float k = h.w / Mathf.Max(1f, e.refW);
            Vector2 c = rig.Carry(rig.HairBone, h.x, h.y);
            float ang = rig.BoneAngle(rig.HairBone);
            Vector2 drop = HeroRigPose.Rotate(new Vector2(0f, e.drop * h.w * dropScale), ang);
            Vector2 at = rig.Local(c + drop);
            r.transform.localPosition = new Vector3(at.x, at.y, r.transform.localPosition.z);
            r.transform.localRotation = Quaternion.Euler(0f, 0f, -ang);
            r.transform.localScale = new Vector3(k, k, 1f);
        }
    }
}
