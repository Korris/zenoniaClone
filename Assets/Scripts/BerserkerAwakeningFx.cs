using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The picture of the warrior's BERSERKER AWAKENING; the buffs stay in the skill data (SkillRun on the authority), so this runs only where RendersFx.
    /// Timeline (ms): clenched fists 0-180, the roar at 180 (WarriorBerserkerLightFx: seal, wave, flame tongues, arrows; RO3 Soft Fantasy V1 shader
    /// FX, 2026-11-21, the painted fx/berserker frames are gone), then for the buff's time a clearly visible red-orange ring round the hero's hips
    /// (rule 10 exception: a maintained ATK buff = a ring round the body; ClericBuffBodyRing + BuffBodyRing in WarriorLightLook.BodyRing colours,
    /// lower and redder than the Cleric's gold ATK ring so both read when stacked), a few embers, the ring dissolving over the last 300 ms.
    /// One object per hero: a recast restarts it instead of stacking. BuffState is not replicated, so the end comes from the buff's time.
    /// </summary>
    public class BerserkerAwakeningFx : MonoBehaviour
    {
        public const string SkillId = "warrior.berserker-awakening";
        public const string PoseAnim = "berserker";
        const float RoarMs = 180f, BurstToMs = 650f, RingInMs = 250f, DissolveMs = 300f, EmberEveryMs = 600f;
        const int FistsFrame = 0, RoarFrame = 1, SettleFrame = 2;
        const float HeroW = 15f, Depth = 0.42f, RingY = 0.3f * HeroLayeredSet.BodyUnits, RingRx = 0.68f * HeroW, TurnsPerMs = 1f / 4000f;
        static readonly float[] TierWidth = { 1.25f, 1.05f, 0.85f }, TierAlpha = { 1f, 0.8f, 0.55f };
        static readonly Dictionary<HeroController, BerserkerAwakeningFx> Live = new Dictionary<HeroController, BerserkerAwakeningFx>();

        HeroController hero;
        ISkillBody body; // the zone pump finds a skill object's caster through this field: a portal transfer ends the picture with its hero
        float age, poseEnd, buffEnd, emberLeft;
        bool local, roared, posing;
        int tier = -1;
        ClericBuffBodyRing ring;
        ParticleSystem motes;

        public static void Cast(SkillContext ctx)
        {
            if (!NetMode.RendersFx) return;
            HeroController caster = ctx.Body.Hero;
            BerserkerAwakeningFx fx;
            if (!Live.TryGetValue(caster, out fx) || fx == null)
            {
                var go = new GameObject("BerserkerAwakening");
                go.transform.SetParent(caster.transform.parent, false);
                fx = go.AddComponent<BerserkerAwakeningFx>();
                Live[caster] = fx;
                fx.hero = caster;
            }
            fx.Begin(ctx);
        }

        /// <summary>Loads the light FX masks / shaders ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Preload() { WarriorLightLook.Load(); }

        void Begin(SkillContext ctx)
        {
            body = ctx.Body;
            local = body.IsLocal;
            age = 0f;
            roared = false;
            posing = true;
            emberLeft = EmberEveryMs;
            poseEnd = Mathf.Max(BurstToMs * 0.5f, ctx.Def.CastLockMs);
            buffEnd = Mathf.Max(BurstToMs, ctx.Def.Effects[0].Buff.Ms);
            WarriorBerserkerLightFx.Burst(hero, RoarMs);
            SetTier();
            Apply();
        }

        /// <summary>Crowd tier (own / party / anyone else) sets the ring's width and strength; rebuilt only when it changes.</summary>
        void SetTier()
        {
            HeroController me = LocalPlayer.Hero;
            int t = me == null || hero == me ? 0 : PartyAllies.Are(me, hero) ? 1 : 2;
            if (t == tier) return;
            tier = t;
            if (ring != null) ring.Destroy();
            Material m = WarriorLightLook.Ready ? WarriorLightLook.BodyRing : null;
            ring = m != null ? new ClericBuffBodyRing(transform, "BerserkerRing", m, RingRx, RingRx * Depth, TierWidth[t]) : null;
            if (motes == null && WarriorLightLook.Ready) motes = WantedFxParticles.Create(transform, "Embers", ClericLightLook.DotMat, FeelConfig.FxOrder - 21, 12, 0f);
        }

        void OnDestroy()
        {
            if (hero == null) return;
            if (posing) hero.ClearSkillPose();
            BerserkerAwakeningFx fx;
            if (Live.TryGetValue(hero, out fx) && fx == this) Live.Remove(hero);
        }

        void Update()
        {
            if (hero == null || hero.IsDead || age >= buffEnd + DissolveMs) { Destroy(gameObject); return; } // a fallen caster ends the picture
            float dt = GameClock.Dt * 1000f;
            age += dt;
            if (!roared && age >= RoarMs) Roar();
            if (age >= BurstToMs && age < buffEnd && (emberLeft -= dt) <= 0f)
            {
                emberLeft = EmberEveryMs * Random.Range(0.8f, 1.2f);
                if (FeelConfig.FxOn && tier < 2 && motes != null) Ember();
            }
            Apply();
        }

        void LateUpdate() { LayRing(); }

        void Roar()
        {
            roared = true;
            if (!local) return; // another player's roar never moves this player's camera
            CameraFollow.SkillShake(0.22f);
            CameraFollow.SkillPunch(0.25f);
            ScreenFlash.Trigger(new Color(1f, 0.3f, 0.1f, 0.06f), 0.16f);
        }

        /// <summary>An ember leaving the ring at one side (never in front of the face), rising a little, then fading.</summary>
        void Ember()
        {
            Color c = WarriorLightLook.Rage.Light;
            float side = Random.value < 0.5f ? -1f : 1f, a = Random.Range(0.15f, 0.75f);
            Vector2 at = hero.Position + new Vector2(side * Mathf.Cos(a) * RingRx, RingY - Mathf.Sin(a) * RingRx * Depth * 0.6f);
            WantedFxParticles.Emit(motes, at, new Vector2(side * 0.8f, 6f), Random.Range(1.2f, 1.7f), new Color(c.r, c.g, c.b, 0.85f * TierAlpha[tier]), 0.7f);
        }

        void LayRing()
        {
            if (ring == null || hero == null) return;
            SetTier();
            float on = Mathf.Clamp01((age - BurstToMs) / RingInMs), gone = Mathf.Clamp01((age - buffEnd) / DissolveMs);
            if (!FeelConfig.FxOn || on <= 0f) { ring.Hide(); return; }
            ring.Lay(hero.Position + new Vector2(0f, RingY), GameConfig.OrderBase(hero.Position.y), on * TierAlpha[tier], gone, -age * TurnsPerMs);
        }

        void Apply()
        {
            if (hero == null) return;
            if (age < poseEnd) hero.SetSkillPose(PoseAnim, age < RoarMs ? FistsFrame : age < poseEnd - 120f ? RoarFrame : SettleFrame, 0f);
            else if (posing) { hero.ClearSkillPose(); posing = false; }
        }
    }
}
