using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Draws one layered hero frame (HeroLayeredSet) under the renderer's lean transform. Back to front: weapon behind, offhand behind (orders 1-2),
    /// body with mask + HeroRecolor (3), hair (3, nearer z), headgear (3, nearer still), weapon in front, offhand in front (4-5), fist over the
    /// handle (6). Body, hand, hair and headgear of the left dirs are the mirrored right rows; weapons use their own per-dir grips (Dir8 order),
    /// so the sword stays in the right hand. Hair and headgear sit on the frame's head anchor (skull top x, y, width w, angle).
    /// </summary>
    public sealed class HeroLayeredView
    {
        const float HairZ = -0.001f, HeadgearZ = -0.002f;
        static readonly int[] PaintedRow = { 0, 1, 2, 3, 4, 3, 2, 1 }; // Dir8 -> painted row (5-7 mirror 3-1)
        static Material material;
        static MaterialPropertyBlock block;
        static int maskId, hairColorId, outfitColorId, hairAllId;

        readonly HeroLayeredSet set;
        readonly SpriteRenderer body, hand, hair, headgear, weaponBack, weaponFront, offBack, offFront;
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
            }
            weaponBack = Child(parent, "L_WeaponBack", 0f, false);
            offBack = Child(parent, "L_OffhandBack", 0f, false);
            body = Child(parent, "L_Body", 0f, true);
            hair = Child(parent, "L_Hair", HairZ, true);
            headgear = Child(parent, "L_Headgear", HeadgearZ, false);
            weaponFront = Child(parent, "L_WeaponFront", 0f, false);
            offFront = Child(parent, "L_OffhandFront", 0f, false);
            hand = Child(parent, "L_Hand", 0f, false);
        }

        static SpriteRenderer Child(Transform parent, string name, float z, bool recolor)
        {
            SpriteRenderer sr = ProceduralSprites.Child(parent, name, null);
            sr.transform.localPosition = new Vector3(0f, 0f, z);
            if (recolor) sr.sharedMaterial = material;
            return sr;
        }

        /// <summary>Palette colours, hairstyle and headgear (0 = none) of this hero.</summary>
        public void SetLook(Color hairC, Color outfitC, int style, int headgearIdValue)
        {
            if (hairC != hairColor || outfitC != outfitColor) colorsDirty = true;
            hairColor = hairC; outfitColor = outfitC; hairStyle = style; headgearId = headgearIdValue;
        }

        public SpriteRenderer[] Renderers { get { return new[] { weaponBack, offBack, body, hair, headgear, weaponFront, offFront, hand }; } }

        public void Hide() { foreach (SpriteRenderer r in Renderers) r.sprite = null; }

        /// <summary>Draws painted anim `anim` (already resolved by HeroLayeredSet.Resolve), Dir8 `dir8`, frame `frame`.</summary>
        public void Show(string anim, int dir8, int frame, int orderBase, Color tint, float breathY)
        {
            int row = PaintedRow[Mathf.Clamp(dir8, 0, 7)];
            bool mirror = dir8 >= 5;
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
            PlaceOnHead(hair, hat != null && hat.hidesHair ? null : set.Hair(hairStyle), h, row, mirror, tint, orderBase + 3, 0f, breathY); // hoods hide the hair
            PlaceOnHead(headgear, hat, h, row, mirror, tint, orderBase + 3, 1f, breathY);
            PlaceWeapon("weapon", weaponBack, weaponFront, anim, dir8, frame, tint, orderBase + 1, orderBase + 4);
            PlaceWeapon("offhand", offBack, offFront, anim, dir8, frame, tint, orderBase + 2, orderBase + 5);
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
            r.SetPropertyBlock(block);
        }

        /// <summary>Cell px (y down, origin top-left) -> local units around the feet.</summary>
        Vector2 Local(float x, float y, bool mirror)
        {
            if (mirror) x = set.CellW - x;
            return new Vector2((x - set.CellW * 0.5f) / set.Ppu, (set.FeetY - y) / set.Ppu);
        }

        void PlaceOnHead(SpriteRenderer r, HeroLayeredSet.LayerEntry e, HeroLayeredSet.HeadAnchor h, int row, bool mirror, Color tint, int order, float dropScale, float breathY)
        {
            Sprite s = h != null ? set.LayerCell(e, row) : null;
            Draw(r, s, mirror, tint, order);
            if (s == null) return;
            float k = h.w / Mathf.Max(1f, e.refW);
            Vector2 p = Local(h.x, h.y + e.drop * h.w * dropScale, mirror);
            r.transform.localPosition = new Vector3(p.x, p.y * breathY, r.transform.localPosition.z);
            r.transform.localRotation = Quaternion.Euler(0f, 0f, mirror ? h.angle : -h.angle);
            r.transform.localScale = new Vector3(k, k, 1f);
        }

        void PlaceWeapon(string part, SpriteRenderer back, SpriteRenderer front, string anim, int dir8, int frame, Color tint, int orderBack, int orderFront)
        {
            HeroLayeredSet.Grip g = set.GripOf(part, anim, dir8, frame);
            Sprite s = g != null ? set.Weapon(part) : null;
            SpriteRenderer on = g != null && g.behind ? back : front, off = on == back ? front : back;
            off.sprite = null;
            Draw(on, s, false, tint, g != null && g.behind ? orderBack : orderFront);
            if (s == null) return;
            Vector2 p = Local(g.x, g.y, false); // grips are listed for every Dir8 dir: never mirrored
            on.transform.localPosition = new Vector3(p.x, p.y, 0f);
            on.transform.localRotation = Quaternion.Euler(0f, 0f, -g.angle); // clockwise on the y-down cell, like held-gear anchors
            on.transform.localScale = Vector3.one;
        }
    }
}
