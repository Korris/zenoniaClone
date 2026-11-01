using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Mechanic combo hit 4: the discharge at the cannon muzzle as the shot leaves (HeroFx.CastWindup, so it is the firing moment of ComboConfigs.MechanicHits by construction): a tight cone along the aim,
    /// hot core at the muzzle, rings thrown forward, a little smoke back along the barrel. No fireball, no debris: those belong to the impact. View only: no damage, nothing on the server.
    /// Everyone in view sees the discharge and the recoil; shake, flash and hit-stop are the local player's. Runs on real time so it keeps playing through its own hit-stop.
    /// </summary>
    public sealed class MechanicCannonBlastFx : MonoBehaviour
    {
        // ---- ART: everything that changes with the art is in this block ----
        // TEMPORARY: the painted cast row of the combo bolt (fx/attack-orange or attack-blue, read only) stretched along the aim, until the art of Tools/build-mechanic-cannon-blast.py is drawn.
        // New art: load fx/cannon-blast/ in ConeFrames, list its frames in ConeOrder, set ConeAlong and ConeAcross to its scale.
        static Sprite[] ConeFrames() { VfxSet set = VfxSprites.For(BoltStyle.Plasma); return set != null ? set.Rows[VfxSet.Cast] : null; }
        static readonly int[] ConeOrder = { 2, 2, 2, 3 }; // only the full cone and its tail: the row's first two frames are round, and read as a burst
        const float ConeAlong = 2f, ConeAcross = 1.05f; // the widest frame is then about 43 units long (the hero is about 20 tall), its pivot about 9 units behind the front of the barrel
        const float Fps = 16f;
        const string CorePath = "Sprites/classes/mechanic/fx/combat/muzzleFlash"; // white-blue star over the muzzle
        // Added to the muzzle (down, up, left, right), world units. The muzzle itself is HeroFx.MuzzleCannon: match the final cannon there, this is only for art whose pivot is off.
        static readonly Vector2[] MuzzleNudge = { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };

        /// <summary>Hit 4's bolt on screen, against the 1.35 every other finisher uses. Picture only: the hit box is still BoltDef W / H.</summary>
        public const float BoltScale = VfxAnim.FinisherScale * 2.2f;
        const float BoltArtUnits = 340f * VfxSprites.UnitsPerPx * BoltScale, BoltTailAtMuzzle = 0.18f, BoltTailMax = 0.55f;

        /// <summary>The bolt art's pivot is its head. Its tail is squeezed so it stays short, and comes out of the muzzle as the head travels, so it never lies over the hero.</summary>
        public static Vector3 BoltFlightScale(float travelled)
        {
            return new Vector3(BoltScale * Mathf.Min(BoltTailMax, BoltTailAtMuzzle + travelled / BoltArtUnits), BoltScale, 1f);
        }

        const float HitStopMs = 90f, Trauma = 0.72f;
        const float RecoilUnits = 2.6f, RecoilSeconds = 0.17f;
        const string Sound = "mechanic/explosion-a"; // on top of the cannon sound ClassSfx.BoltLaunched already plays
        const float SoundLocal = 1f, SoundOther = 0.55f;

        const float CoreUnits = 20f, CoreSeconds = 0.17f;
        const float RingGap = 0.035f, RingSeconds = 0.2f, RingFrom = 5f, RingTo = 40f, RingAcrossFrom = 8f, RingAcrossTo = 27f, RingThin = 0.34f;
        static readonly Color[] RingColors = { new Color(0.8f, 0.95f, 1f, 0.95f), new Color(1f, 0.8f, 0.4f, 0.9f), new Color(0.65f, 0.85f, 1f, 0.8f) };

        static Sprite core;

        Sprite[] frames;
        SpriteRenderer cone, coneGlow, coreSr;
        SpriteRenderer[] rings;
        HeroController hero;
        Vector2 kickDir;
        float age, life;

        /// <summary>Loads the art ahead of the first shot (the cone row is loaded by VfxSprites.Preload).</summary>
        public static void Preload()
        {
            if (core != null) return;
            Texture2D tex = Resources.Load<Texture2D>(CorePath);
            if (tex == null) { Debug.LogWarning("[ZenoniaProto] Missing muzzle core art " + CorePath); return; }
            core = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width, 0, SpriteMeshType.FullRect); // 1 unit across, scaled in Apply
        }

        /// <summary>False = nothing played (no art, no zone): the caller keeps its old flash.</summary>
        public static bool Play(HeroController shooter, Vector2 muzzle, float angleDeg)
        {
            if (!NetMode.RendersFx || Zone.Main == null) return false;
            Sprite[] art = ConeFrames();
            if (art == null) return false;
            Preload();
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            int facing = Mathf.Abs(dir.x) > Mathf.Abs(dir.y) ? (dir.x < 0f ? 2 : 3) : (dir.y > 0f ? 1 : 0);
            Vector2 at = muzzle + MuzzleNudge[facing];

            var go = new GameObject("MechanicCannonBlast");
            go.transform.SetParent(Zone.Main.Root, false);
            go.transform.position = new Vector3(at.x, at.y, 0f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
            if (facing == 2) go.transform.localScale = new Vector3(1f, -1f, 1f); // mirrored, not upside down
            MechanicCannonBlastFx fx = go.AddComponent<MechanicCannonBlastFx>();
            fx.frames = art;
            fx.life = ConeOrder.Length / Fps;
            fx.cone = ProceduralSprites.Child(go.transform, "Cone", null);
            fx.cone.sortingOrder = FeelConfig.FxOrder + 3; // over the hero and over the bolt leaving through it
            fx.coneGlow = ProceduralSprites.Child(fx.cone.transform, "Glow", null);
            fx.coneGlow.sharedMaterial = FeelMaterials.Additive;
            fx.coneGlow.sortingOrder = FeelConfig.FxOrder + 4;
            fx.rings = new SpriteRenderer[RingColors.Length];
            for (int i = 0; i < fx.rings.Length; i++)
            {
                fx.rings[i] = ProceduralSprites.Child(go.transform, "Ring", FxSprites.Ring());
                fx.rings[i].sortingOrder = FeelConfig.FxOrder + 5;
            }
            fx.coreSr = ProceduralSprites.Child(go.transform, "Core", core);
            fx.coreSr.sortingOrder = FeelConfig.FxOrder + 6;
            fx.hero = shooter;
            fx.kickDir = -dir;
            fx.Apply();

            UltimateFx.Dust(at - dir * 5f, 5, 2.5f, new Color(0.62f, 0.62f, 0.66f, 0.9f)); // a little smoke out of the back of the barrel

            bool local = shooter != null && shooter == LocalPlayer.Hero;
            GameSfx.Play(Sound, local ? SoundLocal : SoundOther, 0.03f);
            if (local)
            {
                CameraFollow.AddTrauma(Trauma);
                ScreenFlash.Trigger(new Color(0.9f, 0.95f, 1f, 0.22f), 0.14f);
                GameClock.HitStop(HitStopMs);
            }
            return true;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= Mathf.Max(life, RingSeconds + RingGap * RingColors.Length)) { Destroy(gameObject); return; }
            Apply();
        }

        /// <summary>The hero writes its transform every Update, so this kick is absolute and can never add up. Figure only: the sim position is untouched.</summary>
        void LateUpdate()
        {
            if (hero == null) return;
            float t = age / RecoilSeconds;
            if (t >= 1f) { hero.transform.position = new Vector3(hero.Position.x, hero.Position.y, 0f); hero = null; return; }
            Vector2 at = hero.Position + kickDir * (RecoilUnits * (1f - t) * (1f - t));
            hero.transform.position = new Vector3(at.x, at.y, 0f);
        }

        void Apply()
        {
            bool coneOn = age < life;
            cone.enabled = coneOn;
            coneGlow.enabled = coneOn && FeelConfig.FxOn;
            if (coneOn)
            {
                Sprite s = frames[ConeOrder[Mathf.Min(ConeOrder.Length - 1, (int)(age * Fps))]];
                float fade = Mathf.Clamp01((life - age) * Fps * 2f); // last half frame fades out
                cone.sprite = coneGlow.sprite = s;
                cone.transform.localScale = new Vector3(ConeAlong, ConeAcross, 1f);
                cone.color = new Color(1f, 1f, 1f, fade);
                coneGlow.color = new Color(1f, 1f, 1f, FeelConfig.PaintedGlowAlpha * fade);
            }

            float ct = age / CoreSeconds;
            coreSr.enabled = core != null && ct < 1f;
            if (coreSr.enabled)
            {
                float d = CoreUnits * (1f - 0.45f * ct);
                coreSr.transform.localScale = new Vector3(d, d, 1f);
                coreSr.color = new Color(1f, 1f, 1f, 1f - ct * ct);
            }

            for (int i = 0; i < rings.Length; i++)
            {
                float t = (age - i * RingGap) / RingSeconds;
                bool on = t >= 0f && t < 1f;
                rings[i].enabled = on;
                if (!on) continue;
                float e = 1f - (1f - t) * (1f - t);
                float across = Mathf.Lerp(RingAcrossFrom, RingAcrossTo, e);
                rings[i].transform.localPosition = new Vector3(Mathf.Lerp(RingFrom, RingTo, e), 0f, 0f);
                rings[i].transform.localScale = new Vector3(across * RingThin, across, 1f);
                Color c = RingColors[i];
                rings[i].color = new Color(c.r, c.g, c.b, c.a * (1f - t));
            }
        }
    }
}
