using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The Mechanic's Deploy Turret on the ground: stays where it was placed, shoots the nearest monster in range, gone when its time ends (numbers: TurretConfig). No collider, so nothing walks into it.
    /// On the authority (offline, server, host) it has a SkillContext and decides targets, damage and lifetime, one per caster. It holds a zone NetId, so a client gets it through the normal
    /// Spawn / Despawn of its area of interest, also when it arrives later, and each shot as a TurretShot event; a client's copy only draws. A server (ZoneBehaviourPump) ends it when the caster leaves the zone.
    /// </summary>
    public sealed class DeployedTurret : MonoBehaviour
    {
        const string ArtPath = "Sprites/classes/mechanic/skills/deploy-turret/turret";
        const float DropHeight = 9f, KickUnits = 1.1f, KickMs = 110f, ShadowWidth = 15f, ShotFps = 16f;
        static readonly List<TurretCandidate> Candidates = new List<TurretCandidate>(32);
        static readonly List<Monster> CandidateMonsters = new List<Monster>(32);
        static Sprite art;

        public ushort NetId;
        SkillContext ctx; // null on a client's copy; ZoneBehaviourPump finds the caster through this field
        SkillEffect fx;
        TurretClock clock;
        Zone home;
        Vector2 feet;
        ushort owner;
        bool flip, gone;
        float viewLeftMs, viewAgeMs, kickLeftMs, fadeLeftMs;
        SpriteRenderer body, shadow;
        Monster hitTarget;   // authority: the shot in the air
        float hitInMs;
        Vector2 hitDir;
        SkillFxAnim shotAnim; // picture of the shot in the air
        Monster shotTarget;
        Vector2 shotFrom, shotAim;
        float shotAgeMs, shotMs;
        int shotsFired;
        float bornRealtime;

        public Vector2 Feet { get { return feet; } }
        public ushort Owner { get { return owner; } }
        public bool FacesRight { get { return flip; } }
        public int RemainingMs { get { return clock != null ? clock.RemainingMs : Mathf.Max(0, Mathf.CeilToInt(viewLeftMs)); } }
        /// <summary>Test tooling: shots drawn in this process.</summary>
        public static int ShotsDrawn;

        /// <summary>Art ahead of the first cast.</summary>
        public static void Preload() { if (NetMode.RendersFx) Art(); }

        static Sprite Art()
        {
            if (art != null) return art;
            Texture2D tex = Resources.Load<Texture2D>(ArtPath);
            if (tex == null) { Debug.LogError("[ZenoniaProto] missing " + ArtPath); return null; }
            tex.wrapMode = TextureWrapMode.Clamp;
            var pivot = new Vector2(TurretConfig.ArtGripXPx / tex.width, 1f - TurretConfig.ArtGripYPx / tex.height);
            art = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, tex.height / TurretConfig.DrawHeightUnits, 0, SpriteMeshType.FullRect);
            return art;
        }

        /// <summary>Skill effect, on the authority: places the turret at the caster's feet; the caster's older turret goes. A client's replay of the cast does nothing here, the server's Spawn brings the turret.</summary>
        public static void Place(SkillContext ctx, SkillEffect e)
        {
            Zone zone = Zone.Main;
            if (!ctx.Authority || zone == null || ctx.Body.Hero == null) return;
            DeployedTurret t = Create(ctx.Body.Hero.transform.parent, zone, ctx.Body.Hero.NetId, ctx.Feet, ctx.Dir.x > 0f, e.DurationMs);
            t.ctx = ctx; t.fx = e;
            t.clock = new TurretClock(e.DurationMs, e.IntervalMs, TurretConfig.FirstShotMs);
            t.NetId = zone.Ids.Alloc(t);
            t.bornRealtime = Time.realtimeSinceStartup;
            if (NetMode.IsHeadless) Debug.Log("[ZenoniaProto] turret " + t.NetId + " placed by hero " + t.owner + " at " + t.feet + ", rank " + ctx.Rank);
        }

        /// <summary>Client: the server's Spawn. `leftMs` near the whole life means it was placed just now, so it drops in; otherwise it is simply there.</summary>
        public static void SpawnProxy(Transform root, ushort id, Vector2 at, ushort ownerHero, bool facesRight, int leftMs)
        {
            Zone zone = Zone.Main;
            if (zone == null) return;
            DeployedTurret t = Create(root, zone, ownerHero, at, facesRight, leftMs);
            t.NetId = id;
            zone.Ids.Register(id, t);
        }

        static DeployedTurret Create(Transform parent, Zone zone, ushort ownerHero, Vector2 at, bool facesRight, int leftMs)
        {
            var go = new GameObject("DeployedTurret");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            DeployedTurret t = go.AddComponent<DeployedTurret>();
            t.home = zone; t.owner = ownerHero; t.feet = at; t.flip = facesRight; t.viewLeftMs = leftMs;
            t.viewAgeMs = Mathf.Max(0f, TurretConfig.DurationMs - leftMs);
            DeployedTurret replaced = zone.Turrets.Place(ownerHero, t);
            if (replaced != null) replaced.End(true);
            if (NetMode.RendersFx) t.BuildView();
            return t;
        }

        void BuildView()
        {
            int order = GameConfig.OrderBase(feet.y + 1.5f) - 2; // under a hero still standing on the spot (his quantized feet can be a hair higher than the server's), y-sorted against everyone else
            shadow = ProceduralSprites.Child(transform, "Shadow", ProceduralSprites.Shadow());
            shadow.transform.localScale = new Vector3(ShadowWidth, ShadowWidth, 1f);
            shadow.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            shadow.sortingOrder = order - 1;
            shadow.color = new Color(1f, 1f, 1f, 0.3f);
            body = ProceduralSprites.Child(transform, "Body", Art());
            body.sortingOrder = order;
            Draw(0f);
            if (viewAgeMs < TurretConfig.DeployMs) ShockRing.Spawn(feet, 8f, 0.3f, new Color(0.8f, 0.75f, 0.65f, 0.7f), order - 1, 0.7f, 0.3f, true); // set down on the ground
        }

        void Update()
        {
            float dtMs = GameClock.Dt * 1000f;
            if (dtMs <= 0f) return;
            if (gone) { fadeLeftMs -= dtMs; if (body != null) Draw(dtMs); if (fadeLeftMs <= 0f) Destroy(gameObject); return; }
            if (ctx != null && !Think(dtMs)) return;
            viewLeftMs -= dtMs;
            if (body != null) Draw(dtMs);
        }

        /// <summary>Authority: lifetime, the shot in the air, the next shot. False when the turret just ended.</summary>
        bool Think(float dtMs)
        {
            if (ctx.Body.Hero == null) { End(true); return false; } // caster gone (offline: another hero loaded)
            clock.Tick(dtMs);
            if (hitTarget != null) { hitInMs -= dtMs; if (hitInMs <= 0f) LandHit(); }
            if (clock.Expired) { LandHit(); End(true); return false; }
            if (!clock.ShotDue) return true;
            Monster foe = PickTarget();
            if (foe == null) { clock.NoTarget(); return true; }
            clock.Fired();
            shotsFired++;
            Fire(foe);
            return true;
        }

        Monster PickTarget()
        {
            Candidates.Clear(); CandidateMonsters.Clear();
            IReadOnlyList<Monster> all = Monster.All;
            for (int i = 0; i < all.Count; i++)
            {
                Monster m = all[i];
                Candidates.Add(new TurretCandidate(m.Center.x, m.Center.y, m.IsDying));
                CandidateMonsters.Add(m);
            }
            int pick = TurretTargeting.Nearest(feet.x, feet.y, fx.Radius * ctx.Units, Candidates);
            return pick >= 0 ? CandidateMonsters[pick] : null;
        }

        /// <summary>The shot leaves now and hits on arrival: the event and the MonsterHit carry the server's times, so damage numbers land with the picture on every client too.</summary>
        void Fire(Monster foe)
        {
            LandHit(); // only with an interval shorter than a flight
            Vector2 aim = foe.Center;
            flip = aim.x > feet.x;
            Vector2 from = Muzzle();
            float dist = Vector2.Distance(from, aim);
            hitTarget = foe; hitInMs = dist / TurretConfig.ShotSpeedUnitsPerSec * 1000f; hitDir = dist > 0.01f ? (aim - from) / dist : Vector2.down;
            NetEventBus.TurretShot(this, foe, aim);
            if (ctx.View) PlayShot(foe, aim);
        }

        void LandHit()
        {
            Monster foe = hitTarget;
            hitTarget = null;
            if (foe == null || foe.IsDying) return;
            SkillDamage.Single(ctx.Dmg, fx, foe, feet, hitDir);
        }

        Vector2 Muzzle()
        {
            float h = TurretConfig.DrawHeightUnits;
            return feet + new Vector2((flip ? -TurretConfig.MuzzleX : TurretConfig.MuzzleX) * h, TurretConfig.MuzzleY * h);
        }

        /// <summary>The picture of a shot, on every side that draws: turn to the target, recoil, muzzle blast and the mechanic's own bolt art and shot sound. `foe` may be null when the client does not know that monster.</summary>
        public void PlayShot(Monster foe, Vector2 aim)
        {
            if (gone || !NetMode.RendersFx) return;
            ShotsDrawn++;
            FinishShotArt(true);
            flip = aim.x > feet.x;
            kickLeftMs = KickMs;
            Vector2 from = Muzzle(), to = aim - from;
            float angle = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
            bool near = NearListener();
            if (near) ClassSfx.BoltLaunched(HeroClass.Mechanic, false);
            VfxSet set = VfxSprites.For(BoltStyle.Plasma);
            if (set == null) return;
            VfxAnim.Cast(set, from, angle, false);
            shotFrom = from; shotAim = aim; shotTarget = foe; shotAgeMs = 0f;
            shotMs = Mathf.Max(30f, to.magnitude / TurretConfig.ShotSpeedUnitsPerSec * 1000f);
            shotAnim = SkillFxAnim.Play(set.Rows[VfxSet.Proj], ShotFps, from, angle, 1f, FeelConfig.FxOrder - 10, true, shotMs / 1000f + 0.1f);
        }

        void DrawShot(float dtMs)
        {
            if (shotAnim == null && shotMs <= 0f) return;
            shotAgeMs += dtMs;
            if (shotTarget != null && !shotTarget.IsDying) shotAim = shotTarget.Center; // the monster walks on: the shot meets it, as the damage does
            if (shotAgeMs >= shotMs) { FinishShotArt(true); return; }
            Vector2 at = Vector2.Lerp(shotFrom, shotAim, shotAgeMs / shotMs), to = shotAim - at;
            if (shotAnim != null) shotAnim.MoveTo(at, Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg);
        }

        void FinishShotArt(bool impact)
        {
            bool flying = shotMs > 0f;
            shotMs = 0f;
            if (shotAnim != null) { shotAnim.Stop(); shotAnim = null; }
            if (!flying || !impact) return;
            VfxSet set = VfxSprites.For(BoltStyle.Plasma);
            if (set != null) VfxAnim.Impact(set, shotAim, false);
            if (NearListener()) ClassSfx.BoltImpact(HeroClass.Mechanic, false);
        }

        bool NearListener()
        {
            HeroController me = LocalPlayer.Hero;
            return me == null || (me.Position - feet).sqrMagnitude < 200f * 200f; // as SkillSfx: other players' skills are heard only near by
        }

        void Draw(float dtMs)
        {
            DrawShot(dtMs);
            viewAgeMs += dtMs;
            kickLeftMs = Mathf.Max(0f, kickLeftMs - dtMs);
            float drop = viewAgeMs < TurretConfig.DeployMs ? Mathf.Pow(1f - viewAgeMs / TurretConfig.DeployMs, 2f) : 0f;
            float kick = (kickLeftMs / KickMs) * KickUnits * (flip ? -1f : 1f);
            float alpha = gone ? Mathf.Clamp01(fadeLeftMs / TurretConfig.FadeMs) : drop > 0f ? 1f - 0.6f * drop : 1f;
            float left = RemainingMs;
            if (!gone && left < TurretConfig.BlinkLastMs && Mathf.Repeat(left, 300f) < 90f) alpha *= 0.55f; // about to run out
            body.flipX = flip; // the art points left; pivot on the base's middle, so it turns on the spot
            body.transform.localPosition = new Vector3(kick, drop * DropHeight, 0f);
            body.color = new Color(1f, 1f, 1f, alpha);
            shadow.color = new Color(1f, 1f, 1f, 0.3f * alpha);
        }

        /// <summary>Time over, replaced, or the server's Despawn. The id and the slot go at once; with `fade` the picture fades out on the spot, otherwise it just goes (left the area of interest).</summary>
        public void End(bool fade)
        {
            if (gone) return;
            gone = true;
            if (NetMode.IsHeadless && clock != null) Debug.Log("[ZenoniaProto] turret " + NetId + " ended: " + shotsFired + " shots, " + clock.RemainingMs + " ms of life left, " + (Time.realtimeSinceStartup - bornRealtime).ToString("F2") + " s of real time");
            Detach();
            FinishShotArt(false);
            if (fade && body != null)
            {
                fadeLeftMs = TurretConfig.FadeMs;
                ShockRing.Spawn(feet, 9f, 0.25f, new Color(0.45f, 0.85f, 1f, 0.8f), FeelConfig.FxOrder - 8, 0.7f, 0.25f, true);
                return;
            }
            Destroy(gameObject);
        }

        /// <summary>Client, the whole id space goes at once (arriving on another map, leaving the online game): no picture, and this id must release nothing later.</summary>
        public static void ClearZone(Zone zone)
        {
            for (int i = zone.Turrets.Count - 1; i >= 0; i--)
            {
                DeployedTurret t = zone.Turrets[i];
                if (t == null) continue;
                t.gone = true;
                t.FinishShotArt(false);
                t.NetId = 0;
                zone.Turrets.Remove(t);
                Destroy(t.gameObject);
            }
        }

        void Detach()
        {
            if (home == null) return;
            home.Turrets.Remove(this);
            home.Ids.Release(NetId, home.TimeMs);
            NetId = 0;
        }

        void OnDestroy()
        {
            if (shotAnim != null) shotAnim.Stop();
            Detach();
        }
    }
}
