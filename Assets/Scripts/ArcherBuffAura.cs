using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// A ring round the Archer's body while one of his buffs runs (user 21/11: a maintained buff shows as a ring round the body, clearly visible,
    /// not an icon over the head; exception to RO3 rule 10). Far half behind the hero, near half in front (ClericBuffBodyRing, shader BuffBodyRing),
    /// fading in after the cast burst and dissolving when the buff ends. One per (hero, look): a recast refreshes it.
    ///  - Eagle Eye: leaf ring with gold running segments, low (32 % H);  - Hunter's Focus: gold ring with lime segments, chest (64 % H);
    ///  - Wind Step (next hits +40 %): a pale wind ring at the knees (14 % H) spinning fast, gone when the bonus hits are spent.
    /// Buff time: the real buff on the authority and for the local client hero (HeroBuffs.ActiveFor, matched by source + kind); another player's
    /// buff on a client plays the cast's full time (BuffState is not replicated for them).
    /// </summary>
    public sealed class ArcherBuffAura : MonoBehaviour
    {
        public enum Look { EagleEye, HuntersFocus, WindStep }
        const float FadeInMs = 250f, DissolveMs = 320f, HeroW = 15f, Depth = 0.42f;
        static readonly float[] TierWidth = { 1.2f, 1f, 0.8f }, TierAlpha = { 1f, 0.8f, 0.5f };
        static readonly List<BuffView> Views = new List<BuffView>(6);
        static readonly Dictionary<long, ArcherBuffAura> Live = new Dictionary<long, ArcherBuffAura>();
        static readonly Material[] Mats = new Material[3];
        static bool shaderTried;
        static Shader shader;

        HeroController hero;
        ClericBuffBodyRing ring;
        Look look;
        BuffKind kind;
        byte source;
        float durMs, age, startMs, on, gone, y, turnMs;
        int tier;
        bool ended;
        long key;

        /// <summary>Starts (or refreshes) the ring of `look` on `hero` for buff `kind` from skill `def`, `durMs` long, fading in from `startMs`.</summary>
        public static void Begin(HeroController hero, SkillDef def, Look look, BuffKind kind, float durMs, float startMs)
        {
            if (hero == null) return;
            Material m = Mat(look);
            if (m == null) return;
            long k = ((long)hero.GetInstanceID() << 4) | (long)look;
            ArcherBuffAura old;
            if (Live.TryGetValue(k, out old) && old != null) Destroy(old.gameObject);
            var go = new GameObject("ArcherBuffAura_" + look);
            Transform root = Zone.Main != null ? Zone.Main.Root : null;
            if (root != null) go.transform.SetParent(root, false);
            var a = go.AddComponent<ArcherBuffAura>();
            a.hero = hero; a.look = look; a.kind = kind; a.durMs = durMs; a.startMs = startMs; a.key = k;
            a.source = BuffSource.Pack(def.Class, def.Index);
            HeroController me = LocalPlayer.Hero;
            a.tier = me == null || hero == me ? 0 : PartyAllies.Are(me, hero) ? 1 : 2;
            float h = HeroLayeredSet.BodyUnits, rx;
            switch (look)
            {
                case Look.EagleEye: a.y = 0.32f * h; rx = 0.62f * HeroW; a.turnMs = 5200f; break;
                case Look.HuntersFocus: a.y = 0.64f * h; rx = 0.56f * HeroW; a.turnMs = -6400f; break;
                default: a.y = 0.14f * h; rx = 0.7f * HeroW; a.turnMs = 1400f; break;
            }
            a.ring = new ClericBuffBodyRing(go.transform, "ArcherAura" + look, m, rx, rx * Depth, TierWidth[a.tier]);
            Live[k] = a;
        }

        /// <summary>Removes every aura (map change, ProxySpawner.ClearForTransfer through ClericLightFx.ClearAll's caller).</summary>
        public static void ClearAll()
        {
            foreach (ArcherBuffAura a in Live.Values) if (a != null) Destroy(a.gameObject);
            Live.Clear();
        }

        static Material Mat(Look look)
        {
            if (!shaderTried) { shaderTried = true; shader = Resources.Load<Shader>("Shaders/BuffBodyRing"); if (shader == null || !shader.isSupported) { shader = null; Debug.LogWarning("[ZenoniaProto] Archer aura: BuffBodyRing shader missing"); } }
            if (shader == null) return null;
            int i = (int)look;
            if (Mats[i] != null) return Mats[i];
            var m = new Material(shader) { name = "Archer_Aura_" + look };
            ClericLightLook.Pal body = look == Look.HuntersFocus ? ArcherLightLook.Gold : look == Look.WindStep ? ArcherLightLook.Wind : ArcherLightLook.Leaf;
            Color hi = look == Look.HuntersFocus ? ArcherLightLook.Leaf.Light : look == Look.WindStep ? Color.white : ArcherLightLook.Gold.Light;
            m.SetColor("_BodyColor", ArcherLightLook.A(body.Main, look == Look.WindStep ? 0.62f : 0.78f));
            m.SetColor("_RimColor", ArcherLightLook.A(body.Rim, 0.88f));
            m.SetColor("_HiColor", ArcherLightLook.A(hi, 0.95f));
            m.SetColor("_CoreColor", body.Glow);
            m.SetFloat("_Segs", look == Look.WindStep ? 4f : 3f);
            m.SetFloat("_SegLen", look == Look.WindStep ? 0.22f : 0.3f);
            return Mats[i] = m;
        }

        /// <summary>ms of buff left: the real buff where this process knows it, else the cast's full time.</summary>
        float LeftMs()
        {
            if (hero == null || hero.IsDead) return 0f;
            if (NetMode.IsClient && hero != LocalPlayer.Hero) return Mathf.Max(0f, durMs - age);
            HeroBuffs.ActiveFor(hero, Views);
            for (int i = 0; i < Views.Count; i++) if (Views[i].Kind == kind && Views[i].Source == source && Views[i].LeftMs > 0f) return Views[i].LeftMs;
            return age < 300f ? durMs - age : 0f; // the buff lands on the same frame; a client's SelfState list may lag a little
        }

        void Update()
        {
            if (hero == null) { Destroy(gameObject); return; }
            float dt = GameClock.Dt * 1000f;
            age += dt;
            float left = LeftMs();
            if (left > 0f && !ended)
            {
                on = age < startMs ? 0f : Mathf.MoveTowards(on, 1f, dt / FadeInMs);
                gone = 1f - Mathf.Clamp01(left / DissolveMs);
            }
            else
            {
                ended = true;
                gone = Mathf.MoveTowards(gone, 1f, dt / DissolveMs);
                if (gone >= 0.999f) { Destroy(gameObject); return; }
            }
        }

        void LateUpdate()
        {
            if (hero == null || ring == null) return;
            Vector2 feet = hero.Position;
            ring.Lay(feet + new Vector2(0f, y), GameConfig.OrderBase(feet.y), on * TierAlpha[tier], gone, age / turnMs, 0f);
        }

        void OnDestroy()
        {
            ArcherBuffAura a;
            if (Live.TryGetValue(key, out a) && a == this) Live.Remove(key);
        }
    }
}
