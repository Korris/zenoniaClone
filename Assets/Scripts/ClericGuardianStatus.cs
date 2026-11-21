using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Maintained sign of Guardian Spirit's shield on one hero (part of ClericBuffStatusFx), RO3 rule 10 / spec 4.6: a small ivory halo over the
    /// head at ~25 % while the shield lasts; it flashes to full for 160 ms when the wearer is hit (HP drop, or on the authority the shield soaking
    /// a hit); when the shield breaks early (gone with more than 300 ms left) the light wings flash once behind the shoulders (300 ms).
    /// Strangers' heroes only show the halo (the wings flash is for the own hero and party).
    /// </summary>
    public sealed class ClericGuardianStatus
    {
        const float FlashMs = 160f, BreakMs = 300f, BreakSlackMs = 300f;
        readonly MeshRenderer halo, wings;
        float flashAt = -1e6f, breakAt = -1e6f, lastLeft, lastAmount = -1f;

        /// <summary>True for one frame when the authority saw the shield amount drop (a hit soaked without any HP loss).</summary>
        public bool Soaked { get; private set; }

        public ClericGuardianStatus(Transform parent)
        {
            ClericLightLook.Pal ivory = ClericLightLook.Spirit;
            halo = LightSkillAssets.Quad(parent, "GuardHalo", ClericLightLook.M(ClericLightLook.Band, ivory, ClericLightLook.Role.Glyph), false, 0);
            wings = LightSkillAssets.Quad(parent, "GuardWings", ClericLightLook.M(ClericLightLook.Wings, ivory, ClericLightLook.Role.Glyph), false, 0);
            halo.enabled = wings.enabled = false;
        }

        /// <summary>Per frame: shield time left (ms, 0 = none) of `h`; spots an early break and (authority) a soaked hit.</summary>
        public void Track(HeroController h, float leftMs, float age)
        {
            Soaked = false;
            if (lastLeft > BreakSlackMs && leftMs <= 0f && h != null && !h.IsDead) breakAt = age; // a fallen hero's shield just ends
            lastLeft = leftMs;
            if (NetMode.IsClient || h == null) return;
            float amount = leftMs > 0f ? HeroBuffs.For(h).ShieldLeft : 0f;
            Soaked = lastAmount > 0f && amount > 0f && amount < lastAmount - 0.5f;
            lastAmount = amount;
        }

        public void Flash(float age) { flashAt = age; }

        /// <summary>`k` = visible 0..1 (fade + crowd dim), `gone` = dissolve 0..1.</summary>
        public void Lay(Vector2 feet, int order, float k, float gone, float age, float dim)
        {
            float flash = Mathf.Clamp01(1f - (age - flashAt) / FlashMs);
            float a = k * (1f - gone) * Mathf.Lerp(ClericBuffStatusFx.Rest, 1f, flash);
            bool on = FeelConfig.FxOn && a > 0.01f;
            halo.enabled = on;
            if (on)
            {
                halo.transform.position = new Vector3(feet.x, feet.y + ClericGuardianLightFx.HaloY, 0f);
                halo.transform.localScale = new Vector3(10f, 4f, 1f);
                halo.sortingOrder = FeelConfig.FxOrder - 22;
                LightSkillAssets.Set(halo, a);
            }
            float b = Mathf.Clamp01(1f - (age - breakAt) / BreakMs) * dim;
            bool wingsOn = FeelConfig.FxOn && b > 0.01f && dim >= 0.5f;
            wings.enabled = wingsOn;
            if (!wingsOn) return;
            wings.transform.position = new Vector3(feet.x, feet.y + 16f, 0f);
            wings.transform.localScale = new Vector3(34f, 17f, 1f);
            wings.sortingOrder = order - 4;
            LightSkillAssets.Set(wings, 0.85f * b);
        }
    }
}
