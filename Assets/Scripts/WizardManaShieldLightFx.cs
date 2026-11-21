using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Mana Shield, RO3 Soft Fantasy V1 (2026-11-21; icon: a water-blue honeycomb bubble with gold sparkles). SkillTable: self shield (250 % of ATK
    /// as HP), 8000 ms, applied at 0 ms. Mana azure + gold accent (rule 8), never over the face (old VFX rule 3).
    ///  - Cast (0-800 ms, rule 4: peak ~120-320 ms): hexagon seal 1.4 H drawing itself in under the feet, the honeycomb bubble 1.5 H popping in BEHIND
    ///    the wizard (only its rim reads round him), 4 gold sparkles at its sides, a soft flash behind the chest, 8 motes;
    ///  - Maintained (user 21/11: maintained buff = a ring round the body, clearly visible; exception to rule 10): two azure rings with ice-white
    ///    running segments round the body, low (30 % H) and chest (68 % H, above the Cleric DEF ring's 56 %), turning opposite ways, far half behind
    ///    the hero, near half in front (ClericBuffBodyRing, shader BuffBodyRing). A hit taken (HP drop, or the shield soaking on the authority)
    ///    lights both rings for 150 ms; the shield breaking early bursts the bubble (pop out + shards + flash); running out dissolves the rings.
    /// Shield time: the real buff on the authority and for the local client hero (HeroBuffs.ActiveFor, matched by source); another player's
    /// shield on a client plays the full 8 s (BuffState is not replicated for them). Lives under its ClericLightFx (map change clears it).
    /// </summary>
    public sealed class WizardManaShieldLightFx : MonoBehaviour
    {
        const float CastMs = 800f, FadeInMs = 250f, DissolveMs = 300f, FlashMs = 150f, BreakSlackMs = 120f;
        const float HeroH = HeroLayeredSet.BodyUnits, HeroW = 15f, Depth = 0.42f;
        const float LowY = 0.3f * HeroH, LowRx = 0.62f * HeroW, HighY = 0.68f * HeroH, HighRx = 0.55f * HeroW;
        static readonly float[] TierWidth = { 1.25f, 1.05f, 0.85f }, TierAlpha = { 1f, 0.8f, 0.55f };
        static readonly List<BuffView> Views = new List<BuffView>(6);
        static readonly Dictionary<HeroController, WizardManaShieldLightFx> Live = new Dictionary<HeroController, WizardManaShieldLightFx>();
        static Material ringMat;
        static bool ringTried;

        HeroController hero;
        ClericLightFx fx;
        ClericBuffBodyRing low, high;
        byte source;
        float durMs, age, on, gone, lastLeft = -1f, lastAmount = -1f, lastHp, hitAt = -1e6f;
        int tier;
        bool ended;

        public static void Cast(SkillContext ctx)
        {
            SkillEffect buff = WizardLightSkills.Effect(ctx.Def, EffectKind.Buff);
            HeroController h = ctx.Body.Hero;
            if (buff == null || h == null) return;
            WizardManaShieldLightFx old;
            if (Live.TryGetValue(h, out old) && old != null) Destroy(old.gameObject); // a recast refreshes the shield: one set of rings
            float dim = ClericLightFx.DimFor(h, h);
            var fx = ClericLightFx.Create("ManaShield", h.Position, h, buff.Buff.Ms + 600f, dim);
            CastBurst(fx);
            var s = fx.gameObject.AddComponent<WizardManaShieldLightFx>();
            s.hero = h; s.fx = fx; s.durMs = buff.Buff.Ms; s.lastHp = h.Hp;
            Live[h] = s;
            s.source = BuffSource.Pack(ctx.Def.Class, ctx.Def.Index);
            HeroController me = LocalPlayer.Hero;
            s.tier = me == null || h == me ? 0 : PartyAllies.Are(me, h) ? 1 : 2;
            Material m = RingMat();
            if (m != null)
            {
                float w = TierWidth[s.tier];
                s.low = new ClericBuffBodyRing(fx.transform, "ManaLow", m, LowRx, LowRx * Depth, w);
                s.high = new ClericBuffBodyRing(fx.transform, "ManaHigh", m, HighRx, HighRx * Depth, w);
            }
        }

        static void CastBurst(ClericLightFx fx)
        {
            ClericLightLook.Pal p = ElementLightLook.Mana, gold = ElementLightLook.Gold;
            Vector2 chest = new Vector2(0f, 10f);
            fx.Ground(ElementLightLook.M(ClericLightLook.Hexagon, p, ClericLightLook.Role.Line), 0f, CastMs, 20f, 28f, 0.9f, ClericLightFx.Squash, 18f, 5).Reveal(200f).Env(30f, 320f).Phases(0f, 320f, 1f, 0.5f)
              .Ground(ElementLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Fill), 0f, 700f, 14f, 28f, 1f).Kind(LayerKind.Glow)
              .Upright(ElementLightLook.M(ElementLightLook.HexCells, p, ClericLightLook.Role.Line), ClericLightFx.Band.Behind, false, 60f, CastMs, 22f, 31f, 22f, 31f, 0.8f, chest, -1).Pop(200f).Env(40f, 320f)
              .Upright(ElementLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Behind, false, 0f, 220f, 6f, 15f, 6f, 15f, 0.7f, chest, 1);
            for (int i = 0; i < 4; i++)                                   // gold sparkles at the bubble's sides (the icon's 4 stars), never on the face
            {
                float a = i * Mathf.PI * 0.5f, t0 = 120f + 40f * i;
                Vector2 off = chest + new Vector2(Mathf.Cos(a) * 16f, Mathf.Sin(a) * 15f);
                fx.Upright(ElementLightLook.M(ClericLightLook.Star, gold, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, t0, t0 + 380f, 3f, 6.5f, 3f, 6.5f, 0.95f, off, 2).Pop(150f).Kind(i % 2 == 0 ? LayerKind.Main : LayerKind.Detail);
            }
            Color mote = ElementLightLook.A(p.Light, 0.9f);
            fx.At(80f, f => ClericBlessingLightFx.SideSparks(f, 8, mote, 0.55f));
        }

        static Material RingMat()
        {
            if (ringTried) return ringMat;
            ringTried = true;
            Shader sh = Resources.Load<Shader>("Shaders/BuffBodyRing");
            if (sh == null || !sh.isSupported) { Debug.LogWarning("[ZenoniaProto] Mana Shield: BuffBodyRing shader missing, no body rings"); return null; }
            ringMat = new Material(sh) { name = "Wizard_ManaShieldRing" };
            ringMat.SetColor("_BodyColor", Hex(0x7DBDF2, 0.78f));
            ringMat.SetColor("_RimColor", Hex(0x2F5F9C, 0.88f));
            ringMat.SetColor("_HiColor", Hex(0xE6F4FF, 0.95f));      // ice-white segments (the Cleric DEF ring runs gold ones)
            ringMat.SetColor("_CoreColor", new Color(0.12f, 0.3f, 0.5f, 1f));
            ringMat.SetFloat("_Segs", 3f);
            ringMat.SetFloat("_SegLen", 0.3f);
            return ringMat;
        }

        void OnDestroy()
        {
            WizardManaShieldLightFx s;
            if ((object)hero != null && Live.TryGetValue(hero, out s) && s == this) Live.Remove(hero);
        }

        static Color Hex(int rgb, float a) { return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a); }

        /// <summary>ms of shield left: the real buff where this process knows it, else the cast's full time.</summary>
        float LeftMs()
        {
            if (hero == null || hero.IsDead) return 0f;
            if (NetMode.IsClient && hero != LocalPlayer.Hero) return Mathf.Max(0f, durMs - age);
            HeroBuffs.ActiveFor(hero, Views);
            for (int i = 0; i < Views.Count; i++) if (Views[i].Source == source && Views[i].Kind == BuffKind.Shield && Views[i].LeftMs > 0f) return Views[i].LeftMs;
            return age < 200f ? durMs - age : 0f; // the buff lands on the same frame; a client's SelfState list may lag a little
        }

        void Update()
        {
            if (hero == null) { Destroy(gameObject); return; }
            float dt = GameClock.Dt * 1000f;
            age += dt;
            float left = LeftMs();
            if (left > 0f)
            {
                on = age < CastMs - 250f ? 0f : Mathf.MoveTowards(on, 1f, dt / FadeInMs); // the rings fade in as the cast burst fades out
                gone = 1f - Mathf.Clamp01(left / DissolveMs);
            }
            else
            {
                if (!ended && lastLeft > BreakSlackMs && !hero.IsDead) Break();
                ended = true;
                gone = Mathf.MoveTowards(gone, 1f, dt / DissolveMs);
                if (gone >= 0.999f) { Destroy(gameObject); return; }
            }
            lastLeft = left;
            bool soaked = false;
            if (!NetMode.IsClient)
            {
                float amount = left > 0f ? HeroBuffs.For(hero).ShieldLeft : 0f;
                soaked = lastAmount > 0f && amount > 0f && amount < lastAmount - 0.5f;
                lastAmount = amount;
            }
            float hp = hero.Hp;
            if ((hp < lastHp - 0.5f || soaked) && age - hitAt > FlashMs) hitAt = age;
            lastHp = hp;
        }

        /// <summary>The shield broke early: the bubble bursts out, shards + a flash (300 ms).</summary>
        void Break()
        {
            ClericLightLook.Pal p = ElementLightLook.Mana;
            Vector2 chest = new Vector2(0f, 10f);
            float t = fx.AgeMs;
            fx.Upright(ElementLightLook.M(ElementLightLook.HexCells, p, ClericLightLook.Role.Line), ClericLightFx.Band.Behind, false, t, t + 260f, 28f, 40f, 28f, 40f, 0.85f, chest, -1)
              .Upright(ElementLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Behind, false, t, t + 160f, 8f, 18f, 8f, 18f, 0.75f, chest, 1);
            Color shard = p.Light;
            Vector2 at = hero.Position + chest;
            fx.At(t, f => f.Ring(true, at, 10, 42f, 1.6f, 2.6f, shard, 0.35f));
        }

        void LateUpdate()
        {
            if (hero == null || low == null) return;
            Vector2 feet = hero.Position;
            int order = GameConfig.OrderBase(feet.y);
            float since = age - hitAt, flash = since < FlashMs ? 1f - since / FlashMs : 0f, k = on * TierAlpha[tier];
            low.Lay(feet + new Vector2(0f, LowY), order, Mathf.Lerp(k, on, flash), gone, -age / 5000f, flash);
            high.Lay(feet + new Vector2(0f, HighY), order, Mathf.Lerp(k, on, flash), gone, age / 7000f + 0.17f, flash);
        }
    }
}
