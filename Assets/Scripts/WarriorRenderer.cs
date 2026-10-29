using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Paper-doll warrior: gear-less "bare" body + shield + sword overlays placed at per-frame grip anchors
    /// (mirrors the web held-weapon-renderer). Frames with no bare sheet (death) draw the baked armed frame.
    /// Direction index == sheet row: 0 down, 1 up, 2 left, 3 right.
    /// </summary>
    public class WarriorRenderer : MonoBehaviour
    {
        public static readonly string[] DirNames = { "down", "up", "left", "right" };
        static readonly string[] AnimNames = { "idle", "walk", "attack1", "attack2", "attack3", "dash", "hurt", "death" };
        const float OverlayPpu = 100f;
        const float Unit = GameConfig.FrameUnits / GameConfig.FramePx; // world units per frame pixel
        // Web held-gear-specs: the cut sword art is enlarged around the grip and its side-view squeeze is capped.
        const float SwordDrawScale = 1.4f, SwordMinSx = 0.85f;

        readonly Dictionary<string, Sprite[][]> armed = new Dictionary<string, Sprite[][]>();
        readonly Dictionary<string, Sprite[][]> bare = new Dictionary<string, Sprite[][]>();
        readonly Dictionary<string, Sprite> overlayCache = new Dictionary<string, Sprite>();
        HeldGearData gear;
        SpriteRenderer body, sword, shield, shadow, swordGlow;
        int tier;
        bool enhanced;

        public int Tier { get { return tier; } }
        public bool Enhanced { get { return enhanced; } }
        public string TierName { get { return gear != null && tier < gear.Sword.Length ? gear.Sword[tier].name : "-"; } }

        public void Init(HeldGearData gearData)
        {
            gear = gearData;
            var pivot = new Vector2(0.5f, 0f); // feet = bottom centre of the frame
            foreach (string anim in AnimNames)
            {
                armed[anim] = SpriteSheet.Grid("Sprites/hero/" + anim, GameConfig.FramePx, GameConfig.FramePx, GameConfig.FramePpu, pivot);
                if (anim != "death") bare[anim] = SpriteSheet.Grid("Sprites/bare/" + anim, GameConfig.FramePx, GameConfig.FramePx, GameConfig.FramePpu, pivot);
            }
            shadow = ProceduralSprites.Child(transform, "Shadow", ProceduralSprites.Shadow());
            shadow.transform.localScale = new Vector3(12f, 12f, 1f); // 12 x 6 units, web radius 6 x 2.5 (slightly taller reads better)
            shadow.color = new Color(1f, 1f, 1f, 0.3f);
            shield = ProceduralSprites.Child(transform, "Shield", null);
            body = ProceduralSprites.Child(transform, "Body", null);
            sword = ProceduralSprites.Child(transform, "Sword", null);
            swordGlow = ProceduralSprites.Child(sword.transform, "SwordGlow", null); // additive copy: bright pixels of the enhanced blade bloom
            swordGlow.sharedMaterial = FeelMaterials.Additive;
        }

        public int FrameCount(string anim, int dir)
        {
            Sprite[] row;
            return armed.ContainsKey(anim) && (row = SpriteSheet.Row(armed[anim], dir)) != null ? row.Length : 0;
        }

        public void SetGear(int newTier, bool newEnhanced)
        {
            int max = gear != null ? Mathf.Min(gear.Sword.Length, gear.Shield.Length) : 1;
            tier = ((newTier % max) + max) % max;
            enhanced = newEnhanced;
        }

        /// <summary>Draws one frame. orderBase = GameConfig.OrderBase(feetY).</summary>
        public void Show(string anim, int dir, int frame, int orderBase, float alpha)
        {
            Sprite[] armedRow;
            if (!armed.ContainsKey(anim) || (armedRow = SpriteSheet.Row(armed[anim], dir)) == null || armedRow.Length == 0)
            {
                body.sprite = null; sword.sprite = null; shield.sprite = null; swordGlow.enabled = false;
                return;
            }
            frame = Mathf.Clamp(frame, 0, armedRow.Length - 1);
            Sprite bodySprite = armedRow[frame];
            HeldAnchor swordAnchor = null, shieldAnchor = null;
            Sprite[] bareRow = null;
            if (gear != null && bare.ContainsKey(anim)) bareRow = SpriteSheet.Row(bare[anim], dir);
            if (bareRow != null && frame < bareRow.Length)
            {
                swordAnchor = gear.Get("sword", anim, DirNames[dir], frame);
                shieldAnchor = gear.Get("shield", anim, DirNames[dir], frame);
                // Web: any missing / fallback anchor => the whole frame falls back to the baked armed art.
                if (swordAnchor == null || shieldAnchor == null || swordAnchor.fallback || shieldAnchor.fallback) { swordAnchor = null; shieldAnchor = null; }
                else bodySprite = bareRow[frame];
            }

            var tint = new Color(1f, 1f, 1f, alpha);
            shadow.sortingOrder = orderBase;
            shadow.color = new Color(1f, 1f, 1f, 0.3f * alpha);
            body.sprite = bodySprite;
            body.color = tint;
            body.sortingOrder = orderBase + 3;
            ApplyPart(shield, "shield", gear != null ? gear.Shield : null, shieldAnchor, 1f, 0f, orderBase + 1, orderBase + 4, tint);
            ApplyPart(sword, "sword", gear != null ? gear.Sword : null, swordAnchor, SwordDrawScale, SwordMinSx, orderBase + 2, orderBase + 5, tint);
            UpdateSwordGlow(orderBase, alpha);
        }

        /// <summary>Enhanced blade only, and only when drawn in front of the body (sorting slot +6 is unused by the other parts).</summary>
        void UpdateSwordGlow(int orderBase, float alpha)
        {
            bool on = enhanced && FeelConfig.FxOn && sword.sprite != null && sword.sortingOrder == orderBase + 5;
            swordGlow.enabled = on;
            if (!on) return;
            swordGlow.sprite = sword.sprite;
            swordGlow.sortingOrder = orderBase + 6;
            swordGlow.color = new Color(1f, 0.85f, 0.6f, FeelConfig.EnhancedGlowAlpha * alpha);
        }

        void ApplyPart(SpriteRenderer sr, string part, OverlayEntry[] manifest, HeldAnchor a, float drawScale, float minSx, int orderBehind, int orderFront, Color tint)
        {
            Sprite sprite = (a != null && !a.hidden && manifest != null) ? OverlaySprite(part, manifest) : null;
            if (sprite == null) { sr.sprite = null; return; }
            sr.sprite = sprite;
            sr.color = tint;
            sr.sortingOrder = a.behind ? orderBehind : orderFront;
            float k = a.s * Unit * drawScale * OverlayPpu;       // sprite scale: world units per overlay px / (1 / ppu)
            float sx = Mathf.Sign(a.sx == 0f ? 1f : a.sx) * Mathf.Max(Mathf.Abs(a.sx), minSx);
            Transform t = sr.transform;
            t.localPosition = new Vector3((a.x - GameConfig.FramePx * 0.5f) * Unit, (GameConfig.FramePx - a.y) * Unit, 0f);
            t.localRotation = Quaternion.Euler(0f, 0f, -a.angle); // web angle is clockwise on a y-down canvas
            t.localScale = new Vector3(k * sx, k, 1f);
        }

        Sprite OverlaySprite(string part, OverlayEntry[] manifest)
        {
            if (tier >= manifest.Length) return null;
            string key = part + "/" + tier + (enhanced ? "/e" : "/b");
            Sprite s;
            if (overlayCache.TryGetValue(key, out s)) return s;
            OverlayEntry entry = manifest[tier];
            GripInfo grip = enhanced ? entry.enhancedGrip : entry.baseGrip;
            string path = "Sprites/" + part + "/" + entry.name + (enhanced ? "-enhanced" : "-base");
            Texture2D tex = SpriteSheet.Load(path);
            if (tex != null)
            {
                // Pivot = grip pixel (PNG y runs down, Unity texture y runs up).
                var pivot = new Vector2(grip.gripX / (float)tex.width, (tex.height - grip.gripY) / (float)tex.height);
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, OverlayPpu, 0, SpriteMeshType.FullRect);
            }
            overlayCache[key] = s;
            return s;
        }
    }
}
