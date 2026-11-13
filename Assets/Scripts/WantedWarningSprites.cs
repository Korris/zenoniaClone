using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Ground-telegraph primitives built from the shared warning-*.png pieces (white art, alpha = brightness; tinted in code here, like every
    /// other "no painted FX yet" fallback in this codebase -- docs/wanted-boss-skills-design.md "Cảnh báo"). Each piece loads at ppu = its own
    /// pixel width, so at scale 1 its WORLD WIDTH is exactly 1 unit and its world height is `aspect` (= texture height / width) units; every
    /// method below scales from that so a desired size in world units never depends on the source PNG's resolution.
    ///  - warning-circle: centred, symmetric -- no rotation.
    ///  - warning-cone: apex at the top-middle, opens toward image-down ("south"). Rotated to the shape's facing; stretched on X relative to its
    ///    own ~90 deg drawn angle (sprite-rules.md: "quạt 90° trong hình phải xoay/giãn cho góc 120°/100°").
    ///  - warning-line: anchored at its bottom-middle, points "up". Rotated to the facing.
    ///  - warning-safe: a small centred badge with no rotation, placed at the middle of a safe lane.
    /// </summary>
    public static class WantedWarningSprites
    {
        const float ConeBaseAngleDeg = 90f;

        public static SpriteRenderer Circle(Transform parent, float radiusUnits, Color tint)
        {
            float aspect;
            SpriteRenderer sr = Child(parent, "Circle", "Sprites/fx/wanted/warning-circle", new Vector2(0.5f, 0.5f), tint, out aspect);
            if (sr != null) sr.transform.localScale = new Vector3(radiusUnits * 2f, radiusUnits * 2f / aspect, 1f);
            return sr;
        }

        public static SpriteRenderer Cone(Transform parent, float radiusUnits, float angleDeg, float dirDeg, Color tint)
        {
            float aspect;
            SpriteRenderer sr = Child(parent, "Cone", "Sprites/fx/wanted/warning-cone", new Vector2(0.5f, 1f), tint, out aspect);
            if (sr == null) return null;
            float height = radiusUnits / aspect;
            sr.transform.localScale = new Vector3(height * (angleDeg / ConeBaseAngleDeg), height, 1f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, dirDeg + 90f); // the art points "down" (world -Y) unrotated
            return sr;
        }

        public static SpriteRenderer Line(Transform parent, float lengthUnits, float widthUnits, float dirDeg, Color tint)
        {
            float aspect;
            SpriteRenderer sr = Child(parent, "Line", "Sprites/fx/wanted/warning-line", new Vector2(0.5f, 0f), tint, out aspect);
            if (sr == null) return null;
            sr.transform.localScale = new Vector3(widthUnits, lengthUnits / aspect, 1f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, dirDeg - 90f); // the art points "up" (world +Y) unrotated
            return sr;
        }

        public static SpriteRenderer Badge(Transform parent, float dirDeg, float distanceUnits, Color tint)
        {
            float aspect;
            SpriteRenderer sr = Child(parent, "SafeBadge", "Sprites/fx/wanted/warning-safe", new Vector2(0.5f, 0.5f), tint, out aspect);
            if (sr == null) return null;
            const float Size = 4f;
            sr.transform.localScale = new Vector3(Size, Size / aspect, 1f);
            float rad = dirDeg * Mathf.Deg2Rad;
            sr.transform.localPosition = new Vector3(Mathf.Cos(rad) * distanceUnits, Mathf.Sin(rad) * distanceUnits, 0f);
            return sr;
        }

        /// <summary>The solid fill under a telegraph outline (the outline pieces are thin glowing rims; the area itself must read as danger).
        /// A sector of `angleDeg` (360 = disc) with its centre at the pivot, pointing "down" like the cone piece; world radius 1 at scale 1.
        /// WantedSkillTelegraphFx grows it from the centre as the strike nears.</summary>
        public static SpriteRenderer Fill(Transform parent, float angleDeg, float dirDeg, Color tint)
        {
            SpriteRenderer sr = ProceduralSprites.Child(parent, "Fill", Sector(angleDeg));
            sr.color = tint;
            sr.sortingOrder = UltimateFx.GroundFxOrder;
            if (angleDeg < 360f) sr.transform.localRotation = Quaternion.Euler(0f, 0f, dirDeg + 90f);
            return sr;
        }

        /// <summary>The solid fill of a line telegraph: a white unit square anchored at its bottom-middle, pointing "up" (scaled by the caller).</summary>
        public static SpriteRenderer FillLine(Transform parent, float dirDeg, Color tint)
        {
            SpriteRenderer sr = ProceduralSprites.Child(parent, "Fill", LineFill ?? (LineFill = Bake(16, (dx, dy) => true, new Vector2(0.5f, 0f), 16)));
            sr.color = tint;
            sr.sortingOrder = UltimateFx.GroundFxOrder;
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, dirDeg - 90f);
            return sr;
        }

        static readonly System.Collections.Generic.Dictionary<int, Sprite> Sectors = new System.Collections.Generic.Dictionary<int, Sprite>();
        static Sprite LineFill;

        static Sprite Sector(float angleDeg)
        {
            int key = Mathf.RoundToInt(angleDeg);
            Sprite s;
            if (Sectors.TryGetValue(key, out s) && s != null) return s;
            float half = key * 0.5f;
            s = Bake(128, (dx, dy) => dx * dx + dy * dy <= 1f && (key >= 360 || Mathf.Abs(Mathf.DeltaAngle(-90f, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg)) <= half),
                     new Vector2(0.5f, 0.5f), 64);
            Sectors[key] = s;
            return s;
        }

        /// <summary>A white mask texture of `size` px where inside(dx, dy) holds, dx / dy in [-1, 1] from the centre; `ppu` px per world unit.</summary>
        static Sprite Bake(int size, System.Func<float, float, bool> inside, Vector2 pivot, float ppu)
        {
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (inside((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f)) px[y * size + x] = new Color32(255, 255, 255, 255);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), pivot, ppu, 0, SpriteMeshType.FullRect);
        }

        static SpriteRenderer Child(Transform parent, string name, string path, Vector2 pivot, Color tint, out float aspect)
        {
            Texture2D tex = SpriteSheet.TryLoad(path);
            aspect = tex != null ? (float)tex.height / tex.width : 1f;
            if (tex == null) return null;
            Sprite sprite = SpriteSheet.Whole(path, tex.width, pivot);
            SpriteRenderer sr = ProceduralSprites.Child(parent, name, sprite);
            sr.color = tint;
            sr.sortingOrder = UltimateFx.GroundFxOrder + 1;
            return sr;
        }
    }
}
