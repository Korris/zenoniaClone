using System;
using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// A monster's ranged shot (Monster.Ranged.cs; Cliff Harpy feather): flies straight, ends on the first solid tile, the first living hero it
    /// touches, or after MaxTravel. The authority deals the monster's normal attack damage on touch (HeroController.TakeDamage, which also
    /// sends the HeroHit event); on a client the same flight is only a picture started by the MonsterShot event. Flight rules: MonsterRangedRules.
    /// </summary>
    public class MonsterProjectile : MonoBehaviour
    {
        const float VisualLift = 6f;      // drawn at chest height above the ground line it flies on
        const string ArtFolder = "Sprites/fx/monster-shots/";
        static readonly List<MonsterProjectile> Live = new List<MonsterProjectile>();
        static Sprite quill;

        MonsterRangedProfile profile;
        Zone homeZone;
        float x, y, dirX, dirY, travelled, atk;
        Func<float, float, bool> hitAt;
        HeroController touched;
        SpriteRenderer body, shadow;

        public static void Fire(Transform parent, Zone zone, MonsterRangedProfile profile, Vector2 from, float angleDeg, float atk)
        {
            var go = new GameObject("MonsterShot");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<MonsterProjectile>();
            float rad = angleDeg * Mathf.Deg2Rad;
            p.profile = profile; p.homeZone = zone; p.atk = atk;
            p.x = from.x; p.y = from.y; p.dirX = Mathf.Cos(rad); p.dirY = Mathf.Sin(rad);
            p.hitAt = p.TouchesHero;
            Live.Add(p);
            if (!NetMode.RendersFx) return; // headless server: a moving hit box only
            Color32 c = new Color32((byte)(profile.ColorRgb >> 16), (byte)(profile.ColorRgb >> 8), (byte)profile.ColorRgb, 255);
            bool painted = SpriteSheet.TryLoad(ArtFolder + profile.Sprite) != null;
            p.shadow = ProceduralSprites.Child(go.transform, "Shadow", ProceduralSprites.Shadow());
            p.shadow.transform.localScale = new Vector3(6f, 6f, 1f);
            p.shadow.color = new Color(1f, 1f, 1f, 0.3f);
            p.body = ProceduralSprites.Child(go.transform, "Body", painted ? SpriteSheet.Whole(ArtFolder + profile.Sprite, GameConfig.FramePpu, new Vector2(0.5f, 0.5f)) : Quill());
            p.body.color = painted ? Color.white : (Color)c; // painted art carries its own colours
            p.body.transform.localPosition = new Vector3(0f, VisualLift, 0f);
            p.body.transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg); // drawn flying to screen-right
            p.Place();
        }

        /// <summary>Map change (offline transfer, client arriving on another map): every shot in flight belongs to the old map.</summary>
        public static void ClearAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--) if (Live[i] != null) Destroy(Live[i].gameObject);
            Live.Clear();
        }

        void OnDestroy() { Live.Remove(this); }

        void Update()
        {
            Zone.Enter(homeZone);
            float dt = GameClock.Dt;
            if (dt <= 0f) return; // hit-stop
            touched = null;
            MonsterShotEnd end = MonsterRangedRules.Advance(ref x, ref y, dirX, dirY, profile.ProjectileSpeed * dt, ref travelled, profile.MaxTravel, Monster.SolidAt, hitAt);
            if (end == MonsterShotEnd.Flying) { Place(); return; }
            if (end == MonsterShotEnd.Hit && NetMode.IsAuthority && touched != null) touched.TakeDamage(CombatMath.Damage(atk, 1f, touched.DefStat)); // a client's shot is a picture: the server decides the hit
            Destroy(gameObject);
        }

        /// <summary>First living hero of this zone whose body box the shot overlaps at (px, py).</summary>
        bool TouchesHero(float px, float py)
        {
            if (homeZone == null) return false;
            IReadOnlyList<HeroController> heroes = homeZone.Heroes.All;
            for (int i = 0; i < heroes.Count; i++)
            {
                HeroController h = heroes[i];
                if (h == null || h.IsDead) continue;
                Rect b = h.Box;
                if (!MonsterRangedRules.Touches(px, py, profile.HitSize, b.x, b.y, b.width, b.height)) continue;
                touched = h;
                return true;
            }
            return false;
        }

        void Place()
        {
            transform.position = new Vector3(x, y, 0f);
            if (body == null) return;
            int order = GameConfig.OrderBase(y);
            body.sortingOrder = order + 4;
            shadow.sortingOrder = order;
        }

        /// <summary>Placeholder art until a painted feather exists: a white quill, 10 x 3.5 units, pointing right, with a dark rim so it reads on
        /// bright grass and on snow; the profile's colour tints it.</summary>
        static Sprite Quill()
        {
            if (quill != null) return quill;
            const int w = 20, h = 7;
            var px = new Color32[w * h];
            for (int ix = 0; ix < w; ix++)
            {
                float t = (ix + 0.5f) / w;
                float half = 3.4f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.7f)); // blunt tail, long pointed tip
                for (int iy = 0; iy < h; iy++)
                {
                    float d = Mathf.Abs(iy - (h - 1) * 0.5f);
                    if (d > half) continue;
                    bool rim = d > half - 1f || ix == 0;
                    byte v = (byte)(iy == h / 2 ? 190 : rim ? 95 : 255); // shaft, rim, vane
                    px[iy * w + ix] = new Color32(v, v, v, 255);
                }
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, true);
            quill = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 2f, 0, SpriteMeshType.FullRect);
            return quill;
        }
    }
}
