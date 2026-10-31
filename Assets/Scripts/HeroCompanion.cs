using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Shared offhand companion (mechanic robot / cleric spirit; web companion-follow.js + companion-renderer.js, scale + art folders from HeroClassDef): hovers on the shoulder the hero's back is turned to,
    /// eases toward that spot, bobs, and is y-sorted by its own ground point. Tier 1 unenhanced plays the 4-facing float loop;
    /// every other tier shows the static tier art (base / enhanced) with a slightly bigger code bob. Tier / enhanced follow the equipped offhand;
    /// with no offhand equipped the companion is hidden. Enhanced art (always static) gets the offhand's band of HeldEnhanceFx, like a held item.
    /// </summary>
    public class HeroCompanion : MonoBehaviour
    {
        // Web COMPANION_OFFSETS (y flipped: world y points up). Index == facing: down, up, left, right.
        static readonly Vector2[] Offsets = { new Vector2(11f, 3f), new Vector2(-11f, -3f), new Vector2(10f, 0f), new Vector2(-10f, 0f) };
        const float Hover = 11f, FollowMs = 160f, SnapDistance = 90f, MaxStepMs = 100f, BobPeriodMs = 1500f, FloatFrameMs = 140f;
        const float FxShare = 0.5f, FxSize = 0.7f; // smaller on screen than a held weapon: half the stars / motes, 70% of their size

        HeroController hero;
        SpriteRenderer sr;
        HeldEnhanceFx fx;
        OverlayEntry artEntry;
        bool artEnhanced;
        Sprite art;
        Vector2 pos;
        float ageMs;

        public static HeroCompanion Create(Transform parent, HeroController owner)
        {
            var go = new GameObject("Companion");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<HeroCompanion>();
            r.hero = owner;
            r.sr = go.AddComponent<SpriteRenderer>();
            r.pos = owner.Position + Offsets[owner.Facing];
            return r;
        }

        void LateUpdate()
        {
            if (hero == null) { Destroy(gameObject); return; }
            sr.enabled = !hero.IsDead;
            if (hero.IsDead) { if (fx != null) fx.Hide(); return; }
            Vector2 target = hero.Position + Offsets[hero.Facing];
            float dtMs = Mathf.Min(MaxStepMs, GameClock.Dt * 1000f);
            ageMs += dtMs;
            if (Vector2.Distance(target, pos) > SnapDistance) pos = target;
            else pos += (target - pos) * (1f - Mathf.Exp(-dtMs / FollowMs));
            Draw();
        }

        void OnDestroy() { if (fx != null) fx.Dispose(); } // the sparks live beside the companion: its own transform is scaled

        void Draw()
        {
            HeroRenderer view = hero.View;
            HeldGearData gear = view.Gear;
            if (gear == null || gear.Companion == null || gear.Companion.Length == 0 || view.CompanionEmpty) { sr.sprite = null; if (fx != null) fx.Hide(); return; }
            int tier = Mathf.Clamp(view.CompanionTier, 0, gear.Companion.Length - 1);
            bool animated = tier == 0 && !view.CompanionEnhanced;
            string dir = HeroRenderer.DirNames[hero.Facing];
            GripInfo[] loop = animated ? gear.CompanionFloat(dir) : null;
            Sprite sprite;
            if (loop != null && loop.Length > 0)
            {
                int i = Mathf.FloorToInt(ageMs / FloatFrameMs) % loop.Length;
                sprite = GearOverlays.Pivoted("Sprites/" + view.Class.CompanionFloatFolder + "/" + dir + "-" + i, loop[i]);
            }
            else
            {
                animated = false;
                if (gear.Companion[tier] != artEntry || view.CompanionEnhanced != artEnhanced) // static art: no resource path built per frame
                {
                    artEntry = gear.Companion[tier];
                    artEnhanced = view.CompanionEnhanced;
                    art = GearOverlays.Get(view.Class.CompanionFolder, artEntry, artEnhanced);
                }
                sprite = art;
            }
            sr.sprite = sprite;
            float bob = Mathf.Sin(ageMs / BobPeriodMs * Mathf.PI * 2f) * (animated ? 0.6f : 1.3f);
            transform.position = new Vector3(pos.x, pos.y + Hover + bob, 0f);
            float k = view.Class.CompanionScale * GearOverlays.FrameUnit * GearOverlays.Ppu;
            transform.localScale = new Vector3(k, k, 1f);
            sr.color = new Color(1f, 1f, 1f, 0.96f);
            sr.sortingOrder = GameConfig.OrderBase(pos.y) + 3;
            if (sprite == null || !view.CompanionEnhanced || !HeldEnhanceFx.Enabled) { if (fx != null) fx.Hide(); return; }
            if (fx == null) fx = new HeldEnhanceFx(sr, transform.parent, FxShare, FxSize);
            fx.Draw(view.Class.CompanionFolder, gear.Companion[tier], view.CompanionBand, sr.sortingOrder, sr.sortingOrder + 1, false, sr.color.a);
        }
    }
}
