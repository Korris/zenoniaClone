using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Draws one layered hero frame (HeroLayeredSet) under the renderer's lean transform. Back to front: weapon behind, offhand behind (orders 1-2),
    /// body with mask + HeroRecolor (3), hair (3, nearer z), hair accessory (Mechanic goggles, not recoloured, hidden under any hat), headgear
    /// (3, nearer still), weapon in front, offhand in front (4-5), fist over the handle (6), an `overHand` part in front (shield: 7).
    /// Body, hand, hair and headgear of the left dirs are the mirrored right rows (an anim with 8 body rows: its own left rows, AllDirs); weapons use their own per-dir grips (Dir8 order, art flipped
    /// on the left dirs by `flip`), so the sword stays in the right hand. Hair and headgear sit on the frame's head anchor (skull top x, y, width w,
    /// angle). Grips: Tools/build-hero-layered-grips.py.
    /// </summary>
    public sealed class HeroLayeredView
    {
        const float HairZ = -0.001f, AccessoryZ = -0.0015f, HeadgearZ = -0.002f;
        static readonly int[] PaintedRow = { 0, 1, 2, 3, 4, 3, 2, 1 }; // Dir8 -> painted row (5-7 mirror 3-1)
        static Material material;
        static MaterialPropertyBlock block;
        static int maskId, hairColorId, outfitColorId, hairAllId, hairRampId, hairDeepId, hairShadowId, hairLightId, hairLineId;

        readonly HeroLayeredSet set;
        readonly Transform root;
        readonly SpriteRenderer body, hand, hair, hairAcc, headgear, weaponBack, weaponFront, offBack, offFront;
        Color hairColor = Color.gray, outfitColor = Color.gray;
        int hairStyle, headgearId;
        bool colorsDirty = true;
        Texture2D shownMask;

        public HeroLayeredSet Set { get { return set; } }

        public HeroLayeredView(HeroLayeredSet layeredSet, Transform parent)
        {
            set = layeredSet;
            if (material == null)
            {
                Shader sh = Resources.Load<Shader>("Shaders/HeroRecolor");
                material = new Material(sh != null ? sh : Shader.Find("Sprites/Default")) { name = "HeroRecolor (shared)" };
                block = new MaterialPropertyBlock();
                maskId = Shader.PropertyToID("_MaskTex"); hairColorId = Shader.PropertyToID("_HairColor");
                outfitColorId = Shader.PropertyToID("_OutfitColor"); hairAllId = Shader.PropertyToID("_HairAll");
                hairRampId = Shader.PropertyToID("_HairRamp"); hairDeepId = Shader.PropertyToID("_HairDeep"); hairShadowId = Shader.PropertyToID("_HairShadow");
                hairLightId = Shader.PropertyToID("_HairLight"); hairLineId = Shader.PropertyToID("_HairLine");
            }
            root = new GameObject("L_Root").transform; // pivots on the feet: the stand-in motion of a set without walk / attack art moves it
            root.SetParent(parent, false);
            weaponBack = Child(root, "L_WeaponBack", 0f, false);
            offBack = Child(root, "L_OffhandBack", 0f, false);
            body = Child(root, "L_Body", 0f, true);
            hair = Child(root, "L_Hair", HairZ, true);
            hairAcc = Child(root, "L_HairAccessory", AccessoryZ, false);
            headgear = Child(root, "L_Headgear", HeadgearZ, false);
            weaponFront = Child(root, "L_WeaponFront", 0f, false);
            offFront = Child(root, "L_OffhandFront", 0f, false);
            hand = Child(root, "L_Hand", 0f, false);
        }

        static SpriteRenderer Child(Transform parent, string name, float z, bool recolor)
        {
            SpriteRenderer sr = ProceduralSprites.Child(parent, name, null);
            sr.transform.localPosition = new Vector3(0f, 0f, z);
            if (recolor) sr.sharedMaterial = material;
            return sr;
        }

        /// <summary>Palette colours, hairstyle and headgear (0 = none) of this hero.</summary>
        /// <param name="ramp">Hair tones deep, shadow, base, light, outline (HeroPalettes.HairRamp); null = HeroRecolor derives them from hairC.</param>
        public void SetLook(Color hairC, Color outfitC, int style, int headgearIdValue, Color32[] ramp = null)
        {
            if (hairC != hairColor || outfitC != outfitColor || ramp != hairRamp) colorsDirty = true;
            hairColor = hairC; outfitColor = outfitC; hairStyle = style; headgearId = headgearIdValue; hairRamp = ramp;
        }

        Color32[] hairRamp;

        /// <summary>The layers that can rise above the skull: hair, hair accessory, headgear (the HUD portrait fits its crop to them).</summary>
        public SpriteRenderer Hair { get { return hair; } }
        public SpriteRenderer HairAccessory { get { return hairAcc; } }
        public SpriteRenderer Headgear { get { return headgear; } }

        public SpriteRenderer[] Renderers { get { return new[] { weaponBack, offBack, body, hair, hairAcc, headgear, weaponFront, offFront, hand }; } }

        public void Hide() { foreach (SpriteRenderer r in Renderers) r.sprite = null; }

        /// <summary>Multiplies `m` into the figure's colour (body, hand, hair, headgear; weapons untouched) after Show: the wanted boss
        /// poison / freeze tints (HeroRenderer.WantedFreeze.cs). Show resets every colour each frame, so this never accumulates.</summary>
        public void MultiplyTint(Color m)
        {
            foreach (SpriteRenderer r in new[] { body, hand, hair, hairAcc, headgear })
            {
                if (r == null || r.sprite == null) continue;
                Color c = r.color;
                r.color = new Color(c.r * m.r, c.g * m.g, c.b * m.b, c.a);
            }
        }

        /// <summary>Draws painted anim `anim` (already resolved by HeroLayeredSet.Resolve), Dir8 `dir8`, frame `frame`.
        /// weaponOn / offhandOn: the hero holds something in that hand (nothing equipped = that layer hidden).
        /// shift / tiltDeg: the whole figure moved (local units) and turned around the feet (stand-in motion, HeroRenderer.Layered.cs).</summary>
        public void Show(string anim, int dir8, int frame, int orderBase, Color tint, float breathY, bool weaponOn = true, bool offhandOn = true,
                         Vector2 shift = default(Vector2), float tiltDeg = 0f)
        {
            root.localPosition = new Vector3(shift.x, shift.y, 0f);
            root.localRotation = Quaternion.Euler(0f, 0f, tiltDeg);
            dir8 = Mathf.Clamp(dir8, 0, 7);
            // An anim painted for all 8 dirs (Warrior attack, 2026-11-20: the sword hand stays the right hand) draws row dir8 unmirrored;
            // hair / headgear strips only have the 5 painted cells, so the left dirs still take the mirrored right cell on its head anchor.
            bool allDirs = set.AllDirs(anim);
            int row = allDirs ? dir8 : PaintedRow[dir8], headRow = PaintedRow[dir8];
            bool mirror = !allDirs && dir8 >= 5, headFlip = dir8 >= 5;
            frame = Mathf.Clamp(frame, 0, set.Frames(anim) - 1);
            Sprite[] cells = SpriteSheet.Row(set.BodyGrid(anim), row);
            if (cells == null || frame >= cells.Length) { Hide(); return; }
            Draw(body, cells[frame], mirror, tint, orderBase + 3);
            body.transform.localScale = new Vector3(1f, breathY, 1f);
            Texture2D mask = set.Mask(anim);
            if (mask != shownMask || colorsDirty) { ApplyBlock(body, mask, 0f); ApplyBlock(hair, null, 1f); shownMask = mask; colorsDirty = false; }
            Sprite[] handCells = SpriteSheet.Row(set.HandGrid(anim), row);
            Draw(hand, handCells != null && frame < handCells.Length ? handCells[frame] : null, mirror, tint, orderBase + 6);
            hand.transform.localScale = body.transform.localScale;

            HeroLayeredSet.HeadAnchor h = set.Head(anim, row, frame);
            HeroLayeredSet.LayerEntry hat = set.Headgear(headgearId);
            HeroLayeredSet.LayerEntry hairEntry = hat != null && hat.hidesHair ? null : set.Hair(hairStyle); // hoods hide the hair
            PlaceOnHead(hair, hairEntry, h, headRow, headFlip, mirror, tint, orderBase + 3, 0f, breathY);
            PlaceOnHead(hairAcc, hat == null ? hairEntry : null, h, headRow, headFlip, mirror, tint, orderBase + 3, 0f, breathY, true); // goggles: only bare-headed
            PlaceOnHead(headgear, hat, h, headRow, headFlip, mirror, tint, orderBase + 3, 1f, breathY);
            PlaceWeapon("weapon", weaponOn, weaponBack, weaponFront, anim, dir8, frame, tint, orderBase + 1, orderBase + 4, breathY);
            PlaceWeapon("offhand", offhandOn, offBack, offFront, anim, dir8, frame, tint, orderBase + 2, orderBase + 5, breathY);
        }

        static void Draw(SpriteRenderer r, Sprite s, bool flip, Color tint, int order)
        {
            r.sprite = s;
            r.flipX = flip;
            r.color = tint;
            r.sortingOrder = order;
        }

        void ApplyBlock(SpriteRenderer r, Texture2D mask, float hairAll)
        {
            r.GetPropertyBlock(block);
            if (mask != null) block.SetTexture(maskId, mask);
            block.SetColor(hairColorId, hairColor);
            block.SetColor(outfitColorId, outfitColor);
            block.SetFloat(hairAllId, hairAll);
            block.SetFloat(hairRampId, hairRamp != null ? 1f : 0f);
            if (hairRamp != null)
            {
                block.SetColor(hairDeepId, hairRamp[0]); block.SetColor(hairShadowId, hairRamp[1]);
                block.SetColor(hairLightId, hairRamp[3]); block.SetColor(hairLineId, hairRamp[4]); // [2] = base = hairColor
            }
            r.SetPropertyBlock(block);
        }

        /// <summary>Cell px (y down, origin top-left) -> local units around the feet.</summary>
        Vector2 Local(float x, float y, bool mirror)
        {
            if (mirror) x = set.CellW - x;
            return new Vector2((x - set.CellW * 0.5f) / set.Ppu, (set.FeetY - y) / set.Ppu);
        }

        /// <summary>row / flip: the hair cell (5 painted cells, left dirs mirrored); mirror: the head anchor is on a mirrored body row.</summary>
        void PlaceOnHead(SpriteRenderer r, HeroLayeredSet.LayerEntry e, HeroLayeredSet.HeadAnchor h, int row, bool flip, bool mirror, Color tint, int order, float dropScale, float breathY, bool accessory = false)
        {
            Sprite s = h == null ? null : accessory ? set.AccessoryCell(e, row) : set.LayerCell(e, row);
            Draw(r, s, flip, tint, order);
            if (s == null) return;
            float k = h.w / Mathf.Max(1f, e.refW);
            Vector2 p = Local(h.x, h.y + e.drop * h.w * dropScale, mirror);
            r.transform.localPosition = new Vector3(p.x, p.y * breathY, r.transform.localPosition.z);
            r.transform.localRotation = Quaternion.Euler(0f, 0f, mirror ? h.angle : -h.angle); // anchors of an unmirrored row are already in screen space
            r.transform.localScale = new Vector3(k, k, 1f);
        }

        /// <summary>Weapon / offhand on the frame's grip. breathY: the idle breath stretches the body about the feet, so the grip (the fist) rides
        /// up and down with it and the item follows, unscaled (one size per weapon); hand, hair and headgear follow the same way.</summary>
        void PlaceWeapon(string part, bool on, SpriteRenderer back, SpriteRenderer front, string anim, int dir8, int frame, Color tint, int orderBack, int orderFront, float breathY)
        {
            HeroLayeredSet.Grip g = on ? set.GripOf(part, anim, dir8, frame) : null;
            Sprite s = g != null ? set.Weapon(part) : null;
            SpriteRenderer target = g != null && g.behind ? back : front, other = target == back ? front : back;
            other.sprite = null;
            bool behind = g != null && g.behind;
            // flip: left dirs draw the mirrored art (flipX mirrors about the pivot = the grip); a shield covers the fist that holds it
            Draw(target, s, g != null && g.flip, tint, behind ? orderBack : set.OverHand(part) ? orderFront + 3 : orderFront);
            if (s == null) return;
            Vector2 p = Local(g.x, g.y, false); // grips are listed for every Dir8 dir: never mirrored
            target.transform.localPosition = new Vector3(p.x, p.y * breathY, 0f);
            target.transform.localRotation = Quaternion.Euler(0f, 0f, -g.angle); // clockwise on the y-down cell, like held-gear anchors
            target.transform.localScale = Vector3.one;
        }
    }
}
