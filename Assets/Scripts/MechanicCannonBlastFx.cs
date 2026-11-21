using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Mechanic combo hit 4: the discharge at the cannon muzzle as the shot leaves (HeroFx.CastTipFor, board 2026-11-08; HeroFx.CastWindup, so it is the firing moment of
    /// ComboConfigs.MechanicHits by construction). Since 2026-11-21 drawn with shaders (MechanicAttackLightFx.Cannon, RO3 Soft Fantasy V1): flare along the aim, core flash,
    /// rings thrown forward, sparks, a little smoke out of the back of the barrel; the painted mechanic-v3 cone + muzzle star are gone. No fireball, no debris: those belong to the impact.
    /// View only: no damage, nothing on the server. Everyone in view sees the discharge and the recoil; shake, flash and hit-stop are the local player's.
    /// Runs on real time so the recoil keeps playing through its own hit-stop.
    /// </summary>
    public sealed class MechanicCannonBlastFx : MonoBehaviour
    {
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

        HeroController hero;
        Vector2 kickDir;
        float age;

        /// <summary>Nothing to load since the shader FX (kept for the callers: VfxSprites.Preload).</summary>
        public static void Preload() { if (NetMode.RendersFx) MechanicLightLook.Load(); }

        /// <summary>False = nothing played (no art, no zone): the caller keeps its old flash.</summary>
        public static bool Play(HeroController shooter, Vector2 muzzle, float angleDeg)
        {
            if (!NetMode.RendersFx || Zone.Main == null || !MechanicLightLook.Active) return false;
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            bool up = dir.y > Mathf.Abs(dir.x);
            // Fired up, the blast leaves the middle of the body away from the camera: drawn behind the hero, not over his head (Sổ lỗi 2026-11-08).
            int order = up && shooter != null ? GameConfig.OrderBase(shooter.Position.y) - 1 : 0;
            MechanicAttackLightFx.Cannon(muzzle, angleDeg, MechanicLightLook.Dim(shooter), order);

            var go = new GameObject("MechanicCannonBlast");
            go.transform.SetParent(Zone.Main.Root, false);
            MechanicCannonBlastFx fx = go.AddComponent<MechanicCannonBlastFx>();
            fx.hero = shooter;
            fx.kickDir = -dir;

            bool local = shooter != null && shooter == LocalPlayer.Hero;
            GameSfx.Play(Sound, local ? SoundLocal : SoundOther, 0.03f);
            if (local)
            {
                CameraFollow.AddTrauma(Trauma);
                ScreenFlash.Trigger(new Color(1f, 0.92f, 0.72f, 0.16f), 0.14f); // warm honey, the class colour (was white-blue)
                GameClock.HitStop(HitStopMs);
            }
            return true;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= RecoilSeconds + 0.05f) Destroy(gameObject);
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
    }
}
