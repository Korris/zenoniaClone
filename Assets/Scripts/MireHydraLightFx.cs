using System;
using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Shader / particle FX of ONE locked Mire Hydra Queen skill shape (2026-11-19, user: "hiệu ứng chuyển qua làm vfx theo kiểu ice storm hay
    /// portal, không dùng sprite nữa"), replacing both the warning sprites and the painted impact sheet of that boss (WantedSkillTelegraphFx keeps
    /// the danger checks, cast bar and freeze prediction). Everything is driven by t = ServerNowMs - AtMs, the same server instant the damage uses
    /// (WantedSkillLightTiming, tested), never a local timer:
    ///  - t &lt; 0: the WantedWarningDisc shader circle / sector, exactly the skill radius, fills from the centre and reaches the rim at t = 0;
    ///    over the last ChargeMs, poison bubbles rise inside the zone;
    ///  - t &gt;= 0: the skill's impact (MireHydraLightFx.Impacts.cs): venom streams, geysers, shockwave, falling drops, ground light, bubble bursts.
    /// Ground pieces sit on the ground band (under every hero and the boss); upright pieces are y-sorted like props so heroes in front stay on top.
    /// `--boss-fx legacy` (or Enabled = false) brings back the painted sprite FX. ClearAll on a map change (WantedSkillTelegraphFx.ClearAll).
    /// </summary>
    public sealed partial class MireHydraLightFx : MonoBehaviour
    {
        static readonly List<MireHydraLightFx> Live = new List<MireHydraLightFx>(16);

        /// <summary>Shader FX on (default) or the painted sprite FX (`--boss-fx legacy`).</summary>
        public static bool Enabled = ReadFlag();

        static bool ReadFlag()
        {
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "--boss-fx");
            return !(i >= 0 && i + 1 < a.Length && a[i + 1].Equals("legacy", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>True when boss `bossId`'s skills draw with this class instead of the painted sprites.</summary>
        public static bool ActiveFor(string bossId)
        {
            return Enabled && bossId == WantedSkillLightTiming.LightBoss && NetMode.RendersFx && FeelConfig.FxOn && MireHydraFxLook.Ready;
        }

        WantedSkillDef skill;
        Vector2 origin;
        float dirDeg, radius;
        uint atMs;
        MeshRenderer warn;
        ParticleSystem bubbles;
        MaterialPropertyBlock block;
        float emitDebt;

        public static void Spawn(WantedSkillDef skill, Vector2 origin, float dirDeg, uint atMs)
        {
            if (Zone.Main == null) return;
            var go = new GameObject("MireHydraLightFx_" + skill.Id);
            go.transform.SetParent(Zone.Main.Root, false);
            go.transform.position = new Vector3(origin.x, origin.y, 0f);
            var fx = go.AddComponent<MireHydraLightFx>();
            fx.skill = skill; fx.origin = origin; fx.dirDeg = dirDeg; fx.atMs = atMs;
            fx.radius = skill.RadiusTiles * WantedSkillArea.TileUnits;
            fx.BuildWarning();
            Live.Add(fx);
        }

        public static void ClearAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--) if (Live[i] != null) Destroy(Live[i].gameObject);
            Live.Clear();
        }

        void BuildWarning()
        {
            block = new MaterialPropertyBlock();
            warn = LightSkillAssets.Quad(transform, "Warning", MireHydraFxLook.Warning, false, UltimateFx.GroundFxOrder + 1);
            warn.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);    // round: the map is seen from above (Sổ lỗi 2026-11-12, flat ellipses)
            bool cone = skill.Shape == WantedAreaShape.Cone;
            if (cone) warn.transform.localRotation = Quaternion.Euler(0f, 0f, dirDeg); // sector centred on local +X
            warn.GetPropertyBlock(block);
            block.SetFloat("_HalfAngle", cone ? skill.AngleDeg * 0.5f : 180f);
            block.SetFloat("_RimWidth", Mathf.Clamp(1.2f / radius, 0.004f, 0.2f)); // about 1.2 world units wide whatever the radius
            warn.SetPropertyBlock(block);
            bubbles = WantedFxParticles.Create(transform, "Bubbles", MireHydraFxLook.Bubble, UltimateFx.GroundFxOrder + 6, 160, 0f);
        }

        void Update()
        {
            float t = (float)(WantedSkillTelegraphFx.ClockMs - atMs);   // ms relative to the strike instant
            TickWarning(t);
            if (t < 0f) TickCharge(-t);
            TickImpact(t);
            if (t > WantedSkillLightTiming.ImpactEndMs(skill.Id) + 400f) { Live.Remove(this); Destroy(gameObject); }  // + the last bubbles' life
        }

        void TickWarning(float t)
        {
            if (warn == null) return;
            float fade = t <= 0f ? 1f : 1f - Mathf.Clamp01(t / 220f);                  // the warning hands over to the impact in 0.22 s
            if (fade <= 0f) { Destroy(warn.gameObject); warn = null; return; }
            warn.GetPropertyBlock(block);
            block.SetFloat("_Progress", WantedSkillLightTiming.WarningFill(skill, Mathf.Max(0f, -t)));
            block.SetFloat("_Fade", fade);
            warn.SetPropertyBlock(block);
        }

        /// <summary>Bubbles rising inside the zone over the last ChargeMs: more and more of them as the strike nears.</summary>
        void TickCharge(float msToStrike)
        {
            float charge = WantedSkillLightTiming.Charge(msToStrike);
            if (charge <= 0f) return;
            float rate = (skill.Shape == WantedAreaShape.Cone ? 70f : 40f) * Mathf.Clamp(radius / 32f, 0.6f, 2f) * charge; // per second
            emitDebt += rate * GameClock.Dt;
            for (; emitDebt >= 1f; emitDebt -= 1f)
            {
                Vector2 p = RandomPointInShape(0.9f);
                WantedFxParticles.Emit(bubbles, p, new Vector2(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(6f, 12f)),
                    UnityEngine.Random.Range(1.6f, 3.2f), WantedFxParticles.VenomColor(), UnityEngine.Random.Range(0.5f, 0.8f));
            }
        }

        /// <summary>A random world point inside the skill's own shape (circle or cone), at most `k` x the radius out.</summary>
        Vector2 RandomPointInShape(float k)
        {
            float r = radius * k * Mathf.Sqrt(UnityEngine.Random.value);
            float half = skill.Shape == WantedAreaShape.Cone ? skill.AngleDeg * 0.5f : 180f;
            float a = (dirDeg + UnityEngine.Random.Range(-half, half)) * Mathf.Deg2Rad;
            return origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }

        void OnDestroy() { Live.Remove(this); }
    }
}
