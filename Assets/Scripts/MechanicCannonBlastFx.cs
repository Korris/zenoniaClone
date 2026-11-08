using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Mechanic combo hit 4: the discharge at the cannon muzzle as the shot leaves (HeroFx.CastTipFor, board 2026-11-08; the shell leaves the muzzle too) (HeroFx.CastWindup, so it is the firing moment of ComboConfigs.MechanicHits by construction): a tight cone along the aim,
    /// hot core at the muzzle, rings thrown forward, a little smoke back along the barrel. No fireball, no debris: those belong to the impact. View only: no damage, nothing on the server.
    /// Everyone in view sees the discharge and the recoil; shake, flash and hit-stop are the local player's. Runs on real time so it keeps playing through its own hit-stop.
    /// </summary>
    public sealed class MechanicCannonBlastFx : MonoBehaviour
    {
        // ---- ART: everything that changes with the art is in this block ----
        // The painted muzzle-flash (cast) row of the combo set (mechanic-v2 since 2026-11-08, Tools/build-mechanic-attack-fx-v2.py: a star burst, pivot at the
        // muzzle end of its hot core) stretched along the aim. Other art: load it in ConeFrames, list its frames in ConeOrder, set ConeAlong / ConeAcross.
        static Sprite[] ConeFrames() { VfxSet set = VfxSprites.For(BoltStyle.Plasma); return set != null ? set.Rows[VfxSet.Cast] : null; }
        static readonly int[] ConeOrder = { 1, 2, 2, 3 }; // v2 row: burst, biggest burst with sparks (held), fading sparks
        const float ConeAlong = 2.0f, ConeAcross = 1.25f; // the widest v2 frame (271 x 156 px) is then about 31 units long, 11 across (the hero is about 20 tall)
        // The cone starts at the cannon muzzle (HeroFx.CastTipFor, since the 2026-11-08 board, where the shell also starts: RangedAimRules.MechanicMuzzle): it opens from ConeStart of its size to full in ConeGrowSeconds, so it blooms out of the hero instead of covering him.
        const float ConeStart = 0.45f, ConeGrowSeconds = 0.12f;
        const float Fps = 16f;
        const string CorePath = "Sprites/classes/mechanic/fx/combat/muzzleFlash-yellow"; // yellow-white star over the muzzle (class colour; Tools/build-mechanic-attack-fx-v3.py)
        const string CoreFallbackPath = "Sprites/classes/mechanic/fx/combat/muzzleFlash"; // the older white-blue star, when the yellow copy is missing
        // Added to the start point (down, up, left, right), world units. The point itself is the cannon muzzle (HeroFx.CastTipFor), this is only for art whose pivot is off.
        static readonly Vector2[] MuzzleNudge = { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };

        /// <summary>Hit 4's bolt on screen, against the 1.35 every other finisher uses. Picture only: the hit box is still BoltDef W / H.</summary>
        public const float BoltScale = VfxAnim.FinisherScale * 2.2f;
        const float BoltArtUnits = 249f * VfxSprites.UnitsPerPx * BoltScale, BoltTailAtMuzzle = 0.25f, BoltTailMax = 0.75f; // v2 bullet art 249 px long (was 340)
        // The shell leaves the cannon muzzle (RangedAimRules.MechanicBoltOrigin, board 2026-11-08; the middle of the body before): it starts at BoltStartSize of its size and is full size BoltGrowUnits out (about the hero's height).
        const float BoltStartSize = 0.4f, BoltGrowUnits = 18f;

        /// <summary>
        /// The bolt art's pivot is its head. The shell starts small at the body and reaches full size BoltGrowUnits out, so the ~3x art never covers the hero;
        /// its tail is squeezed so it stays short, and comes out of the start point as the head travels, so it never lies back over him.
        /// </summary>
        public static Vector3 BoltFlightScale(float travelled)
        {
            float size = BoltScale * Mathf.Lerp(BoltStartSize, 1f, Mathf.Clamp01(travelled / BoltGrowUnits));
            return new Vector3(size * Mathf.Min(BoltTailMax, BoltTailAtMuzzle + travelled / BoltArtUnits), size, 1f);
        }

        const float HitStopMs = 90f, Trauma = 0.72f;
        const float RecoilUnits = 2.6f, RecoilSeconds = 0.17f;
        const string Sound = "mechanic/explosion-a"; // on top of the cannon sound ClassSfx.BoltLaunched already plays
        const float SoundLocal = 1f, SoundOther = 0.55f;

        const float CoreUnits = 12f, CoreSeconds = 0.17f; // over the torso since the blast left the cannon muzzle (was 20): a flash, not a disc hiding the hero
        const float RingGap = 0.035f, RingSeconds = 0.2f, RingFrom = 5f, RingTo = 40f, RingAcrossFrom = 8f, RingAcrossTo = 27f, RingThin = 0.34f;
        static readonly Color[] RingColors = { new Color(1f, 0.97f, 0.75f, 0.95f), new Color(1f, 0.8f, 0.25f, 0.9f), new Color(1f, 0.9f, 0.45f, 0.8f) }; // yellow-white, amber, yellow: Mechanic class colour (was white-blue / orange / blue)

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
            Texture2D tex = Resources.Load<Texture2D>(CorePath) ?? Resources.Load<Texture2D>(CoreFallbackPath);
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
            if (facing == 1 && shooter != null)
            {
                // Fired up, the blast leaves the middle of the body away from the camera: draw it behind the hero, not over his head.
                int order = GameConfig.OrderBase(shooter.Position.y) - 1;
                fx.cone.sortingOrder = fx.coneGlow.sortingOrder = fx.coreSr.sortingOrder = order;
                foreach (SpriteRenderer r in fx.rings) r.sortingOrder = order;
            }
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
                float grow = Mathf.Lerp(ConeStart, 1f, Mathf.Clamp01(age / ConeGrowSeconds));
                cone.transform.localScale = new Vector3(ConeAlong * grow, ConeAcross * grow, 1f);
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
