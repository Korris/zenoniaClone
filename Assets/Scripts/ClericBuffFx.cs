using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The picture of the cleric's two ally buffs, gold Spirit Blessing and blue Divine Aegis. The buffs stay in the skill data (SkillRun on the authority), so this runs only where RendersFx.
    /// The caster raises a hand over a rune circle with a column of light (0-620 ms) while streams of light fly to each ally in range; each ally then gets a burst and a thin looping aura that
    /// follows them for the buff's time. Targets come from the same SkillSupport.Collect the buff uses: on a client that is the replayed cast against the heroes it knows about.
    /// Frames are cut at the ring's centre line: the upper half draws behind the hero, the ground arc in front, with a faint veil over the body so the hero always stays visible.
    /// One aura per hero and colour: a recast restarts it instead of stacking, and both colours layer. BuffState is not replicated, so an aura's end comes from the buff's time.
    /// </summary>
    public sealed class ClericBuffFx : MonoBehaviour
    {
        public const string BlessingId = "cleric.spirit-blessing", AegisId = "cleric.divine-aegis", PoseAnim = "clericcast";
        const string Folder = "Sprites/fx/clericbuff/";
        const int Frames = 6, PoseLowered = 0, PoseRaised = 1, PoseReach = 2, MaxStreams = 8;
        const float CastMs = 620f, StreamFromMs = 180f, StreamToMs = 460f, RaiseFromMs = 110f, ReachFromMs = 430f, CastUnitsPerPx = 0.09f, RingLift = 1f;
        const float AnchorX = 256f, AnchorY = 520f, StreamLength = 13f, StreamWidth = 3.2f, StreamArc = 7f, StreamHeight = 10f;

        /// <summary>One frame strip cut in two at the ring's centre line: Back above it, Front below.</summary>
        sealed class Strip { public Sprite[] Back = new Sprite[Frames], Front = new Sprite[Frames]; }

        /// <summary>The art and colours of one buff.</summary>
        sealed class Look
        {
            public string Name; public Color Tint, Glow;
            public float VeilAlpha;
            public Strip Cast, Apply, Loop;
        }

        static readonly Look Gold = new Look { Name = "g", Tint = new Color(1f, 0.82f, 0.36f), Glow = new Color(1f, 0.7f, 0.2f), VeilAlpha = 0.1f };
        static readonly Look Blue = new Look { Name = "b", Tint = new Color(0.62f, 0.86f, 1f), Glow = new Color(0.25f, 0.6f, 1f), VeilAlpha = 0.2f };
        static readonly Dictionary<HeroController, ClericBuffFx> Live = new Dictionary<HeroController, ClericBuffFx>();

        public static bool Handles(string skillId) { return skillId == BlessingId || skillId == AegisId; }

        static Strip Load(string name, float unitsPerPx)
        {
            var s = new Strip();
            for (int i = 0; i < Frames; i++)
            {
                Texture2D tex = SpriteSheet.Load(Folder + name + "-" + (i + 1));
                if (tex == null) return null;
                float below = tex.height - AnchorY, ppu = 1f / unitsPerPx, px = AnchorX / tex.width;
                s.Back[i] = Sprite.Create(tex, new Rect(0f, below, tex.width, AnchorY), new Vector2(px, 0f), ppu, 0, SpriteMeshType.FullRect);
                s.Front[i] = Sprite.Create(tex, new Rect(0f, 0f, tex.width, below), new Vector2(px, 1f), ppu, 0, SpriteMeshType.FullRect);
            }
            return s;
        }

        static void LoadLook(Look l)
        {
            if (l.Cast == null) l.Cast = Load(l.Name + "cast", CastUnitsPerPx);
            if (l.Apply == null) l.Apply = Load(l.Name + "apply", Aura.ApplyUnitsPerPx);
            if (l.Loop == null) l.Loop = Load(l.Name + "loop", Aura.LoopUnitsPerPx);
        }

        /// <summary>Loads and slices the painted frames ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Preload()
        {
            if (!NetMode.RendersFx) return;
            LoadLook(Gold);
            LoadLook(Blue);
        }

        /// <summary>Starts the picture of a resolved cast. Copies the target list, because SkillSupport.Collect reuses its own.</summary>
        public static void Cast(SkillContext ctx)
        {
            if (!NetMode.RendersFx || ctx.Def.Effects.Length == 0) return;
            SkillEffect buff = ctx.Def.Effects[0];
            Look look = ctx.Def.Id == AegisId ? Blue : Gold;
            HeroController caster = ctx.Body.Hero;
            var targets = new List<HeroController>(SkillSupport.Collect(ctx, buff));
            ClericBuffFx fx;
            if (!Live.TryGetValue(caster, out fx) || fx == null)
            {
                var go = new GameObject("ClericBuff");
                go.transform.SetParent(caster.transform.parent, false);
                fx = go.AddComponent<ClericBuffFx>();
                Live[caster] = fx;
                fx.Build(caster);
            }
            fx.Begin(ctx, look, targets);
            foreach (HeroController t in targets) Aura.Wear(t, look, buff.Buff.Ms, t == caster ? StreamFromMs : StreamToMs);
        }

        /// <summary>Test key (DevBuffKey): `hero` gets both buffs and their auras as if a cleric stood next to them, so a class that cannot cast them can be looked at alone.</summary>
        public static void DevGrantBoth(HeroController hero)
        {
            Preload();
            foreach (SkillDef def in SkillTable.ForClass(RpgSession.ClassId(HeroClass.Cleric)))
            {
                if (!Handles(def.Id)) continue;
                SkillEffect buff = def.Effects[0];
                HeroBuffs.For(hero).Add(buff.Buff.Kind, def.BuffAt(buff, 1), buff.Buff.Ms, buff.Count, BuffSource.Pack(def.Class, def.Index));
                Aura.Wear(hero, def.Id == AegisId ? Blue : Gold, buff.Buff.Ms, 0f);
            }
        }

        HeroController hero;
        ISkillBody body; // the zone pump finds a skill object's caster through this field: a portal transfer ends the picture with its hero
        Look look;
        float age;
        bool posing, raised;
        SpriteRenderer back, backGlow, front, frontGlow;
        readonly SpriteRenderer[] streams = new SpriteRenderer[MaxStreams];
        readonly List<HeroController> streamTargets = new List<HeroController>(MaxStreams);

        void Build(HeroController owner)
        {
            hero = owner;
            Preload();
            back = UltimateFx.Layer(transform, "Back", out backGlow);
            front = UltimateFx.Layer(transform, "Front", out frontGlow);
        }

        void Begin(SkillContext ctx, Look l, List<HeroController> targets)
        {
            body = ctx.Body;
            look = l;
            age = 0f;
            posing = true;
            raised = false;
            streamTargets.Clear();
            foreach (HeroController t in targets) if (t != hero && streamTargets.Count < MaxStreams) streamTargets.Add(t);
            for (int i = 0; i < streamTargets.Count; i++)
            {
                if (streams[i] != null) continue;
                streams[i] = ProceduralSprites.Child(transform, "Stream", FxSprites.Dot());
                streams[i].sharedMaterial = FeelMaterials.Additive;
            }
            Apply();
        }

        void OnDestroy()
        {
            if (hero == null) return;
            if (posing) hero.ClearSkillPose();
            ClericBuffFx fx;
            if (Live.TryGetValue(hero, out fx) && fx == this) Live.Remove(hero);
        }

        void Update()
        {
            if (hero == null || hero.IsDead || age >= CastMs) { Destroy(gameObject); return; }
            age += GameClock.Dt * 1000f;
            if (!raised && age >= RaiseFromMs) { raised = true; ShockRing.Spawn(hero.Position + new Vector2(0f, RingLift), 22f, 0.32f, new Color(look.Tint.r, look.Tint.g, look.Tint.b, 0.6f), UltimateFx.GroundFxOrder + 6); }
            Apply();
        }

        void Apply()
        {
            if (hero == null) return;
            if (age < CastMs) hero.SetSkillPose(PoseAnim, age < RaiseFromMs ? PoseLowered : age < ReachFromMs ? PoseRaised : PoseReach, 0f);
            else if (posing) { hero.ClearSkillPose(); posing = false; }
            int order = GameConfig.OrderBase(hero.Position.y);
            Show(back, backGlow, front, frontGlow, look.Cast, Mathf.Clamp(Mathf.FloorToInt(age / CastMs * Frames), 0, Frames - 1), hero.Position, order, look, 0.95f, 0.8f, 0.3f);
            float t = Mathf.InverseLerp(StreamFromMs, StreamToMs, age);
            Vector2 from = hero.Position + new Vector2(0f, StreamHeight);
            for (int i = 0; i < streamTargets.Count; i++)
            {
                SpriteRenderer s = streams[i];
                HeroController target = streamTargets[i];
                bool on = FeelConfig.FxOn && target != null && age >= StreamFromMs && age < StreamToMs;
                s.enabled = on;
                if (!on) continue;
                Vector2 to = target.Position + new Vector2(0f, StreamHeight), dir = to - from;
                Vector2 side = new Vector2(-dir.y, dir.x).normalized * (i % 2 == 0 ? 1f : -1f);
                s.transform.position = from + dir * t + side * (Mathf.Sin(t * Mathf.PI) * StreamArc);
                s.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                s.transform.localScale = new Vector3(StreamLength, StreamWidth, 1f);
                s.color = new Color(look.Glow.r, look.Glow.g, look.Glow.b, 0.95f);
                s.sortingOrder = FeelConfig.FxOrder;
            }
            for (int i = streamTargets.Count; i < MaxStreams; i++) if (streams[i] != null) streams[i].enabled = false;
        }

        static void Show(SpriteRenderer back, SpriteRenderer backGlow, SpriteRenderer front, SpriteRenderer frontGlow, Strip art, int frame, Vector2 feet, int order, Look look, float backAlpha, float frontAlpha, float glowAlpha)
        {
            bool on = art != null && backAlpha > 0.01f;
            back.enabled = front.enabled = on;
            bool glow = on && FeelConfig.FxOn && glowAlpha > 0.01f;
            backGlow.enabled = frontGlow.enabled = glow;
            if (!on) return;
            back.sprite = backGlow.sprite = art.Back[frame];
            front.sprite = frontGlow.sprite = art.Front[frame];
            back.color = new Color(look.Tint.r, look.Tint.g, look.Tint.b, backAlpha);
            front.color = new Color(look.Tint.r, look.Tint.g, look.Tint.b, frontAlpha);
            backGlow.color = frontGlow.color = new Color(look.Glow.r, look.Glow.g, look.Glow.b, glowAlpha);
            Vector2 at = feet + new Vector2(0f, RingLift);
            back.transform.position = front.transform.position = at;
            back.sortingOrder = order - 2; backGlow.sortingOrder = order - 1;
            front.sortingOrder = order + 7; frontGlow.sortingOrder = order + 7;
        }

        /// <summary>What one hero wears while a buff lasts: a short burst, then a thin loop, fading out as the buff's time ends.</summary>
        sealed class Aura : MonoBehaviour
        {
            public const float ApplyUnitsPerPx = 0.064f, LoopUnitsPerPx = 0.072f;
            const float ApplyMs = 450f, LoopFrameMs = 130f, FadeMs = 300f;
            static readonly Dictionary<HeroController, Aura>[] Live = { new Dictionary<HeroController, Aura>(), new Dictionary<HeroController, Aura>() };
            static readonly int[] LoopOrder = { 0, 1, 2, 3, 4, 5, 4, 3, 2, 1 };

            HeroController hero;
            ISkillBody body; // see ClericBuffFx.body
            Look look;
            float age, delay, total;
            SpriteRenderer back, backGlow, front, frontGlow, veil, veilGlow;

            static int Slot(Look l) { return l == Blue ? 1 : 0; }

            public static void Wear(HeroController target, Look look, float buffMs, float delayMs)
            {
                if (target == null) return;
                Aura a;
                Dictionary<HeroController, Aura> live = Live[Slot(look)];
                if (!live.TryGetValue(target, out a) || a == null)
                {
                    var go = new GameObject("ClericBuffAura");
                    go.transform.SetParent(target.transform.parent, false);
                    a = go.AddComponent<Aura>();
                    live[target] = a;
                    a.hero = target;
                    a.look = look;
                    a.body = new HeroSkillBody(target);
                    a.back = UltimateFx.Layer(go.transform, "Back", out a.backGlow);
                    a.front = UltimateFx.Layer(go.transform, "Front", out a.frontGlow);
                    a.veil = UltimateFx.Layer(go.transform, "Veil", out a.veilGlow);
                    a.veilGlow.enabled = false;
                }
                a.age = 0f;
                a.delay = delayMs;
                a.total = buffMs;
                a.Apply();
            }

            void OnDestroy()
            {
                if (hero == null) return;
                Aura a;
                if (Live[Slot(look)].TryGetValue(hero, out a) && a == this) Live[Slot(look)].Remove(hero);
            }

            void Update()
            {
                if (hero == null || hero.IsDead || age >= total) { Destroy(gameObject); return; } // a fallen hero loses the picture with the buff
                age += GameClock.Dt * 1000f;
                Apply();
            }

            void Apply()
            {
                if (hero == null) return;
                float t = age - delay;
                if (t < 0f) { back.enabled = front.enabled = veil.enabled = backGlow.enabled = frontGlow.enabled = false; return; }
                int order = GameConfig.OrderBase(hero.Position.y);
                float k = Mathf.Clamp01((total - age) / FadeMs);
                bool burst = t < ApplyMs;
                Strip art = burst ? look.Apply : look.Loop;
                int frame = burst ? Mathf.Clamp(Mathf.FloorToInt(t / ApplyMs * Frames), 0, Frames - 1) : LoopOrder[Mathf.FloorToInt(t / LoopFrameMs) % LoopOrder.Length];
                Show(back, backGlow, front, frontGlow, art, frame, hero.Position, order, look, (burst ? 0.95f : 0.75f) * k, (burst ? 0.85f : 0.55f) * k, (burst ? 0.3f : 0.1f) * k);
                veil.enabled = art != null && k > 0.01f;
                if (!veil.enabled) return;
                veil.sprite = art.Back[frame];
                veil.color = new Color(look.Tint.r, look.Tint.g, look.Tint.b, look.VeilAlpha * k);
                veil.transform.position = hero.Position + new Vector2(0f, RingLift);
                veil.sortingOrder = order + 7;
            }
        }
    }
}
