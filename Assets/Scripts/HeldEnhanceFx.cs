using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Enhance effect of one held item or companion, drawn around its art by the owner each frame (tuning per band: FeelConfig.EnhanceLooks).
    /// Every band ADDS a visible feature instead of only raising alpha (2026-11-03, docs/pending-tasks.md):
    ///   +7  thin rim just outside the silhouette (alpha-blended so it reads on snow) and a slow light sweep
    ///   +9  inner energy: the art's own energy-coloured pixels (lightning lines, frost veins, molten core) light up, steadily
    ///   +11 energy flow running grip -> tip through those pixels
    ///   +13 small diamond gems drifting beside the blade
    ///   +15 a second colour in the flow, a pulse along the rim, and one floating crest gem beside the upper blade
    /// Rim, inner, flow and halo are children of the art (same sorting order; the render queue draws the halo right before the art and the
    /// others right after it). Gems and the crest are placed in world space under `holderParent`. Rim, inner energy and crest stay at full
    /// strength while attacking, so the level still reads in combat; the decorative layers ease down (FeelConfig.EnhanceMotionK).
    /// Slots are created once and reused; nothing allocates per frame.
    /// </summary>
    public sealed partial class HeldEnhanceFx
    {
        sealed class Gem { public SpriteRenderer Sr; public float Age, Life, Wait, Side; public int Point; }

        const float MaxStep = 0.05f, GemLife = 1.1f, GemDrift = 1.6f, GemOffBlade = 2.6f, RefScale = 6.8f; // drift along the blade in world units; art scale of the Warrior sword
        const float CrestSize = 2f, CrestCore = 0.45f, CrestBob = 0.3f, CrestBobSec = 1.8f;
        const int GhostCount = 5;
        const float GhostEvery = 0.05f, GhostLife = 0.25f, GhostAlpha = 0.35f; // +15 afterimages while the weapon moves
        // Motion layers (FeelConfig.EnhanceMotionK columns).
        const int KRim = 0, KHalo = 1, KShine = 2, KGems = 3, KInner = 4, KFlow = 5;

        static int budgetFrame = -1, budgetUsed;

        readonly Transform art, holderParent;
        readonly SpriteRenderer artSr, halo, shine, rim, inner, flow, aura;
        readonly SpriteRenderer[] ghosts = new SpriteRenderer[GhostCount];
        readonly float[] ghostAge = new float[GhostCount];
        float ghostClock;
        int ghostNext;
        readonly float share, sizeK, phase;
        readonly bool enhancedArt;
        readonly Gem[] gems;
        readonly float[] motionK = { 1f, 1f, 1f, 1f, 1f, 1f }; // eased towards FeelConfig.EnhanceMotionK of the current motion
        SpriteRenderer crest, crestCore;
        Transform holder;
        OverlayEntry bakedEntry;
        Sprite bakedArt;
        HeldGlowBaker.Baked baked;
        bool shown;

        /// <summary>False when the effect must not draw (F toggle, or a process without views). Owners check it before calling Draw, then Hide.</summary>
        public static bool Enabled { get { return FeelConfig.FxOn && NetMode.RendersFx; } }

        /// <summary>share = fraction of the band's gems this item gets (0 = no gems; only share 1, the main weapon, gets the +15 crest);
        /// sizeK scales them for small art; enhancedArt = the item draws its "-enhanced" art (companions), false = its base art (held gear).</summary>
        public HeldEnhanceFx(SpriteRenderer artRenderer, Transform holderParent, float share, float sizeK, bool enhancedArt = true)
        {
            art = artRenderer.transform;
            artSr = artRenderer;
            this.holderParent = holderParent;
            this.share = share;
            this.sizeK = sizeK;
            this.enhancedArt = enhancedArt;
            phase = Random.value * 10f; // heroes on screen do not sweep in lockstep
            halo = Layer("Halo", FeelMaterials.EnhanceHalo);
            Material additiveMask = FeelMaterials.EnhanceShine; // null without the shader: those layers are skipped, never drawn as plain sprites
            if (additiveMask != null) { shine = Layer("Shine", additiveMask); inner = Layer("Inner", additiveMask); flow = Layer("Flow", additiveMask); }
            if (FeelMaterials.EnhanceRim != null) rim = Layer("Rim", FeelMaterials.EnhanceRim);
            if (FeelMaterials.EnhanceAura != null) aura = Layer("Aura", FeelMaterials.EnhanceAura);
            FeelConfig.EnhanceLook top = FeelConfig.EnhanceLooks[FeelConfig.EnhanceLooks.Length - 1];
            gems = new Gem[Mathf.CeilToInt(top.Gems * share)];
        }

        SpriteRenderer Layer(string name, Material material)
        {
            SpriteRenderer sr = ProceduralSprites.Child(art, art.name + name, null);
            sr.sharedMaterial = material;
            sr.enabled = false;
            return sr;
        }

        /// <summary>Draws one frame, after the art's transform is final. order = the art's sorting order, topOrder = above the hero's body and gear. ownArt = the frame draws art named by its anchor, so the layers come from that art; it can change with the facing.
        /// motion = 0 idle, 1 walk, 2 any action (attack, skill, hurt).</summary>
        public void Draw(string folder, OverlayEntry entry, int band, int order, int topOrder, bool behind, float alpha, Sprite ownArt = null, int motion = 0)
        {
            if (ownArt != null) { if (ownArt != bakedArt) { bakedArt = ownArt; bakedEntry = null; baked = HeldGlowBaker.Get(ownArt, entry.glow); } }
            else if (entry != bakedEntry) { bakedEntry = entry; bakedArt = null; baked = HeldGlowBaker.Get(folder, entry, enhancedArt); } // bakes once per art; the string key is not rebuilt per frame
            if (baked.Shine == null) { Hide(); return; }
            float now = Time.time, dt = Mathf.Min(Time.deltaTime, MaxStep);
            EaseMotion(Mathf.Clamp(motion, 0, 2), shown ? dt : 1f); // first frame shown: start at the target, no fade-in from idle
            shown = true;
            FeelConfig.EnhanceLook look = FeelConfig.EnhanceLooks[Mathf.Clamp(band, 1, FeelConfig.EnhanceLooks.Length) - 1];
            Color glow = baked.Glow, bright = Vivid(glow), second = look.Secondary ? Secondary(glow) : Color.Lerp(bright, Color.white, 0.35f);
            float surge = 1f + Surge(now, look.SurgeEvery, look.SurgeSec); // +11 up: a short bright surge of the energy line every few seconds

            if (shine != null) MaskLayer(shine, baked.Shine, order, glow, look.ShineAlpha * motionK[KShine] * alpha, BandHead(now, look.SweepPeriod, look.SweepSec, look.SweepWidth), look.SweepWidth, 0f);
            if (rim != null)
            {
                float pulse = look.PulseSec > 0f ? BandHead(now, look.PulsePeriod, look.PulseSec, FeelConfig.EnhancePulseWidth) : -2f;
                MaskLayer(rim, baked.Aura, order, bright, look.RimAlpha * surge * motionK[KRim] * alpha, pulse, FeelConfig.EnhancePulseWidth, 1f, look.RimWidth * (1f + 0.25f * (surge - 1f))); // steady rim, wider per band; the pulse adds where it passes
            }
            if (inner != null) MaskLayer(inner, baked.Inner, order, Color.Lerp(bright, Color.white, 0.45f), look.InnerAlpha * surge * motionK[KInner] * alpha, -2f, 0.1f, 1f);
            if (flow != null)
            {
                float head = look.FlowSec > 0f ? BandHead(now, look.FlowSec, look.FlowSec, FeelConfig.EnhanceFlowWidth) : -2f; // back to back: always one wave on the blade
                MaskLayer(flow, baked.Inner, order, second, look.FlowSec > 0f ? look.FlowAlpha * motionK[KFlow] * alpha : 0f, head, FeelConfig.EnhanceFlowWidth, 0f);
            }

            if (aura != null)
            {
                aura.enabled = look.AuraAlpha > 0f && baked.Aura != null;
                if (aura.enabled)
                {
                    aura.sprite = baked.Aura;
                    aura.sortingOrder = order;
                    Color c = bright;
                    c.a = look.AuraAlpha * Mathf.Lerp(1f, FeelConfig.EnhanceAuraInAction, motion == 2 ? 1f : 0f) * alpha;
                    aura.color = c;
                    FeelMaterials.SetAura(aura, new Vector4(look.AuraSpeed, 16f, 9f, look.AuraReach));
                }
            }
            DrawGhosts(look.Afterimage && motion > 0 && !behind, bright, order, dt, alpha);

            halo.enabled = look.HaloAlpha > 0f;
            if (halo.enabled)
            {
                halo.sprite = baked.Halo;
                halo.sortingOrder = order;
                float breath = look.HaloAlpha + look.HaloBreath * Mathf.Sin((now + phase) * look.BreathHz * Mathf.PI * 2f);
                halo.color = new Color(glow.r, glow.g, glow.b, breath * motionK[KHalo] * alpha);
            }

            float fit = Mathf.Min(1f, Mathf.Abs(art.lossyScale.y) / RefScale) * sizeK;
            DrawGems(look, Mathf.CeilToInt(look.Gems * share), Color.Lerp(bright, Color.white, 0.25f), topOrder, behind, dt, alpha * motionK[KGems], fit);
            DrawCrest(look.Crest && share >= 1f && !behind, bright, second, topOrder, now, alpha, fit);
            DrawTip(look.Tip && share >= 1f && !behind, bright, Color.Lerp(second, Color.white, 0.5f), order, now, alpha, fit, motion);
        }

        /// <summary>One SpriteShine / SpriteRim layer: steady strength `lift`, plus a band centred at `head` (off the art at -2).</summary>
        static void MaskLayer(SpriteRenderer sr, Sprite mask, int order, Color tint, float strength, float head, float width, float lift, float reach = 0f)
        {
            sr.enabled = mask != null && strength > 0.001f;
            if (!sr.enabled) return;
            sr.sprite = mask;
            sr.sortingOrder = order;
            tint.a = strength;
            sr.color = tint;
            FeelMaterials.SetShine(sr, new Vector4(head, width, lift, reach)); // w: rim reach (SpriteRim only)
        }

        /// <summary>0..EnhanceSurgeBoost bump lasting `sec` once every `every` seconds (0 = never).</summary>
        float Surge(float now, float every, float sec)
        {
            if (every <= 0f || sec <= 0f) return 0f;
            float t = Mathf.Repeat(now + phase * 0.37f, every) / sec;
            return t < 1f ? Mathf.Sin(t * Mathf.PI) * FeelConfig.EnhanceSurgeBoost : 0f;
        }

        /// <summary>Centre of a band running grip -> tip for `sec` seconds once every `period` seconds; -2 (off the art) in between.</summary>
        float BandHead(float now, float period, float sec, float width)
        {
            float t = Mathf.Repeat(now + phase, period) / sec;
            return t < 1f ? Mathf.LerpUnclamped(-width, 1f + width, t) : -2f;
        }

        /// <summary>Energy colour pushed to strong saturation and full value: the alpha-blended rim would read grey on snow otherwise.</summary>
        static Color Vivid(Color glow)
        {
            float h, s, v;
            Color.RGBToHSV(glow, out h, out s, out v);
            return Color.HSVToRGB(h, Mathf.Max(s, FeelConfig.EnhanceRimSaturation), 1f);
        }

        /// <summary>+15 accent: the hue turned a little (orange -> gold, blue -> violet), paler, so it reads as a second colour of the same weapon.</summary>
        static Color Secondary(Color glow)
        {
            float h, s, v;
            Color.RGBToHSV(glow, out h, out s, out v);
            return Color.HSVToRGB(Mathf.Repeat(h + FeelConfig.EnhanceSecondaryHueShift, 1f), 0.45f, 1f);
        }

        void EaseMotion(int motion, float dt)
        {
            float step = Mathf.Clamp01(dt / FeelConfig.EnhanceFadeSec);
            for (int i = 0; i < motionK.Length; i++) motionK[i] = Mathf.MoveTowards(motionK[i], FeelConfig.EnhanceMotionK[motion, i], step);
        }

        void DrawGems(FeelConfig.EnhanceLook look, int count, Color tint, int order, bool behind, float dt, float alpha, float fit)
        {
            Vector2[] points = baked.Points;
            for (int i = 0; i < gems.Length; i++)
            {
                Gem g = gems[i];
                if (i >= count) { if (g != null) { g.Sr.enabled = false; g.Life = 0f; } continue; }
                if (g == null) g = gems[i] = new Gem { Sr = NewSprite(FxSprites.Flame()), Wait = i * GemLife * 0.5f };
                if (g.Life <= 0f)
                {
                    g.Wait -= dt;
                    if (g.Wait > 0f || points.Length == 0) { g.Sr.enabled = false; continue; }
                    g.Life = GemLife;
                    g.Age = 0f;
                    g.Point = Random.Range(0, points.Length);
                    g.Side = Random.value < 0.5f ? -1f : 1f;
                }
                g.Age += dt;
                if (g.Age >= g.Life) { g.Life = 0f; g.Wait = Random.Range(look.GemGapMin, look.GemGapMax); g.Sr.enabled = false; continue; }
                g.Sr.enabled = !behind && alpha > 0.01f && TakeBudget(); // a gem beside a blade the body hides would float over the body
                if (!g.Sr.enabled) continue;
                float k = g.Age / g.Life, env = Mathf.Sin(k * Mathf.PI), size = look.GemSize * fit * (0.75f + 0.25f * env);
                Vector3 spot = art.TransformPoint(baked.Points[g.Point]), along = spot - art.position;
                float reach = Mathf.Max(0.001f, ((Vector2)along).magnitude);
                Vector2 dir = (Vector2)along / reach;
                float off = GemOffBlade * fit * g.Side; // sideways off the grip -> spot line, in world so a mirrored art still steps outward
                Transform t = g.Sr.transform;
                t.position = new Vector3(spot.x - dir.y * off + dir.x * GemDrift * fit * k, spot.y + dir.x * off + dir.y * GemDrift * fit * k, spot.z);
                t.localScale = new Vector3(size * 0.42f, size, 1f);                                     // a tapered tongue, as long as GemSize
                t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f); // pointing towards the tip, the way it travels
                tint.a = env * alpha * FeelConfig.EnhanceFlameAlpha;
                g.Sr.color = tint;
                g.Sr.sortingOrder = order;
            }
        }

        void DrawCrest(bool show, Color outer, Color core, int order, float now, float alpha, float fit)
        {
            if (!show) { if (crest != null) { crest.enabled = false; crestCore.enabled = false; } return; }
            if (crest == null) { crest = NewSprite(FxSprites.Gem()); crestCore = NewSprite(FxSprites.Gem()); }
            Vector3 p = art.TransformPoint(baked.Crest);
            p.y += Mathf.Sin((now + phase) / CrestBobSec * Mathf.PI * 2f) * CrestBob * fit;
            Place(crest, p, CrestSize * fit, outer, 0.95f * alpha, order);
            Place(crestCore, p, CrestSize * CrestCore * fit, Color.Lerp(core, Color.white, 0.5f), alpha, order + 1);
        }

        /// <summary>+15 afterimages: while the weapon moves, a copy of it is left behind every GhostEvery s, tinted the energy colour and fading out.</summary>
        void DrawGhosts(bool spawn, Color tint, int order, float dt, float alpha)
        {
            ghostClock += dt;
            if (spawn && ghostClock >= GhostEvery && artSr.sprite != null)
            {
                ghostClock = 0f;
                SpriteRenderer g = ghosts[ghostNext] ?? (ghosts[ghostNext] = NewSprite(null));
                g.sprite = artSr.sprite;
                g.flipX = artSr.flipX;
                g.flipY = artSr.flipY;
                Transform t = g.transform;
                t.SetPositionAndRotation(art.position, art.rotation);
                Vector3 ls = art.lossyScale, ps = holder.lossyScale;
                t.localScale = new Vector3(ls.x / ps.x, ls.y / ps.y, 1f);
                ghostAge[ghostNext] = 0f;
                ghostNext = (ghostNext + 1) % GhostCount;
            }
            for (int i = 0; i < GhostCount; i++)
            {
                SpriteRenderer g = ghosts[i];
                if (g == null) continue;
                ghostAge[i] += dt;
                float k = ghostAge[i] / GhostLife;
                g.enabled = k < 1f;
                if (!g.enabled) continue;
                Color c = Color.Lerp(tint, Color.white, 0.35f);
                c.a = GhostAlpha * (1f - k) * (1f - k) * alpha;
                g.color = c;
                g.sortingOrder = order - 1;
            }
        }

        static void Place(SpriteRenderer sr, Vector3 p, float size, Color c, float a, int order)
        {
            sr.enabled = true;
            Transform t = sr.transform;
            t.position = p;
            t.localScale = new Vector3(size, size, 1f);
            t.rotation = Quaternion.identity;
            c.a = a;
            sr.color = c;
            sr.sortingOrder = order;
        }

        /// <summary>Screen-wide cap on live gems: a crowd of top-tier heroes costs at most this many sprites, the later ones just skip theirs.</summary>
        static bool TakeBudget()
        {
            if (Time.frameCount != budgetFrame) { budgetFrame = Time.frameCount; budgetUsed = 0; }
            return budgetUsed++ < FeelConfig.EnhanceSparkBudget;
        }

        /// <summary>Alpha-blended world-space sprite under the holder (default sprite material: gems must read on snow, additive would vanish).</summary>
        SpriteRenderer NewSprite(Sprite sprite)
        {
            if (holder == null)
            {
                var go = new GameObject(art.name + "Sparks");
                holder = go.transform;
                holder.SetParent(holderParent, false);
            }
            SpriteRenderer sr = ProceduralSprites.Child(holder, "Gem", sprite);
            sr.enabled = false;
            return sr;
        }

        public void Hide()
        {
            if (!shown) return;
            shown = false;
            halo.enabled = false;
            foreach (SpriteRenderer sr in new[] { shine, rim, inner, flow, aura, crest, crestCore, tipBlade, tipSpark }) if (sr != null) sr.enabled = false;
            foreach (SpriteRenderer g in ghosts) if (g != null) g.enabled = false;
            foreach (Gem g in gems) if (g != null) { g.Sr.enabled = false; g.Life = 0f; }
        }

        /// <summary>For owners that do not parent the sparks under themselves: removes the holder with its sprites.</summary>
        public void Dispose()
        {
            if (holder != null) Object.Destroy(holder.gameObject);
        }
    }
}
