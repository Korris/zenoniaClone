using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Enhance effect of one held item or companion, drawn around its art by the owner each frame (tuning per band: FeelConfig.EnhanceLooks):
    /// a light band sweeping over the surface (clipped to the art by the SpriteShine shader), a soft halo behind the art, twinkling stars on its
    /// bright spots and motes drifting off it. Halo and shine are children of the art and share its sorting order, the render queue puts them
    /// right before / after it; so the halo can be strong without whitening the blade. Stars and motes are placed in world space under `holderParent`.
    /// Slots are created the first time a band needs them and then reused; nothing allocates per frame.
    /// </summary>
    public sealed class HeldEnhanceFx
    {
        sealed class Spark { public SpriteRenderer Sr; public Vector2 Pos, Vel; public float Age, Life, Wait, Size, Spin; public int Point; }

        const float MaxStep = 0.05f, StarLife = 0.5f, StarSpinPerSec = 70f, StarWhite = 0.35f;
        const float MoteLifeMin = 0.9f, MoteLifeMax = 1.5f, MoteRiseMin = 5f, MoteRiseMax = 10f, MoteSide = 3f, MoteSizeMin = 1.2f, MoteSizeMax = 2.1f, MoteGapMax = 0.5f, MoteWhite = 0.15f;

        static int budgetFrame = -1, budgetUsed;

        readonly Transform art, holderParent;
        readonly SpriteRenderer halo, shine;
        readonly float share, sizeK, phase;
        readonly Spark[] stars, motes;
        Transform holder;
        OverlayEntry bakedEntry;
        HeldGlowBaker.Baked baked;
        bool shown;

        /// <summary>False when the effect must not draw (F toggle, or a process without views). Owners check it before calling Draw, then Hide.</summary>
        public static bool Enabled { get { return FeelConfig.FxOn && NetMode.RendersFx; } }

        /// <summary>share = fraction of the band's stars / motes this item gets (0 = shine and halo only); sizeK scales them for small art.</summary>
        public HeldEnhanceFx(SpriteRenderer artRenderer, Transform holderParent, float share, float sizeK)
        {
            art = artRenderer.transform;
            this.holderParent = holderParent;
            this.share = share;
            this.sizeK = sizeK;
            phase = Random.value * 10f; // heroes on screen do not sweep in lockstep
            halo = ProceduralSprites.Child(art, art.name + "Halo", null);
            halo.sharedMaterial = FeelMaterials.EnhanceHalo;
            halo.enabled = false;
            Material shineMaterial = FeelMaterials.EnhanceShine;
            if (shineMaterial != null) // no shader: halo and sparks only, never the mask drawn as a plain sprite
            {
                shine = ProceduralSprites.Child(art, art.name + "Shine", null);
                shine.sharedMaterial = shineMaterial;
                shine.enabled = false;
            }
            FeelConfig.EnhanceLook top = FeelConfig.EnhanceLooks[FeelConfig.EnhanceLooks.Length - 1];
            stars = new Spark[Mathf.CeilToInt(top.Stars * share)];
            motes = new Spark[Mathf.CeilToInt(top.Motes * share)];
        }

        /// <summary>Draws one frame, after the art's transform is final. order = the art's sorting order, topOrder = above the hero's body and gear.</summary>
        public void Draw(string folder, OverlayEntry entry, int band, int order, int topOrder, bool behind, float alpha)
        {
            if (entry != bakedEntry) { bakedEntry = entry; baked = HeldGlowBaker.Get(folder, entry); } // bakes once per art; the string key is not rebuilt per frame
            if (baked.Shine == null) { Hide(); return; }
            shown = true;
            FeelConfig.EnhanceLook look = FeelConfig.EnhanceLooks[Mathf.Clamp(band, 1, FeelConfig.EnhanceLooks.Length) - 1];
            float now = Time.time, dt = Mathf.Min(Time.deltaTime, MaxStep);
            Color glow = baked.Glow;

            if (shine != null)
            {
                shine.enabled = true;
                shine.sprite = baked.Shine;
                shine.sortingOrder = order;
                shine.color = new Color(glow.r, glow.g, glow.b, look.ShineAlpha * alpha);
                float t = Mathf.Repeat(now + phase, look.SweepPeriod) / look.SweepSec;
                float head = t < 1f ? Mathf.LerpUnclamped(-look.SweepWidth, 1f + look.SweepWidth, t) : -2f; // -2: off the art until the next sweep
                FeelMaterials.SetShine(shine, new Vector4(head, look.SweepWidth, look.ShineLift, 0f));
            }

            halo.enabled = look.HaloAlpha > 0f;
            if (halo.enabled)
            {
                halo.sprite = baked.Halo;
                halo.sortingOrder = order;
                float breath = look.HaloAlpha + look.HaloBreath * Mathf.Sin((now + phase) * look.BreathHz * Mathf.PI * 2f);
                halo.color = new Color(glow.r, glow.g, glow.b, breath * alpha);
            }

            DrawStars(look, Mathf.CeilToInt(look.Stars * share), Color.Lerp(glow, Color.white, StarWhite), topOrder, behind, dt, alpha);
            DrawMotes(Mathf.CeilToInt(look.Motes * share), Color.Lerp(glow, Color.white, MoteWhite), topOrder, dt, alpha);
        }

        void DrawStars(FeelConfig.EnhanceLook look, int count, Color tint, int order, bool behind, float dt, float alpha)
        {
            Vector2[] points = baked.Points;
            for (int i = 0; i < stars.Length; i++)
            {
                Spark s = stars[i];
                if (i >= count) { if (s != null) { s.Sr.enabled = false; s.Life = 0f; } continue; }
                if (s == null) s = stars[i] = NewSpark(FxSprites.Star(), Random.Range(0f, look.StarGapMax));
                if (s.Life <= 0f)
                {
                    s.Wait -= dt;
                    if (s.Wait > 0f || points.Length == 0) { s.Sr.enabled = false; continue; }
                    s.Life = StarLife;
                    s.Age = 0f;
                    s.Point = Random.Range(0, points.Length);
                    s.Spin = Random.Range(0f, 90f);
                }
                s.Age += dt;
                if (s.Age >= s.Life) { s.Life = 0f; s.Wait = Random.Range(look.StarGapMin, look.StarGapMax); s.Sr.enabled = false; continue; }
                s.Sr.enabled = !behind && TakeBudget(); // a star on a blade the body hides would float over the body
                if (!s.Sr.enabled) continue;
                float env = Mathf.Sin(s.Age / s.Life * Mathf.PI), size = look.StarSize * sizeK * (0.6f + 0.4f * env);
                Transform t = s.Sr.transform;
                t.position = art.TransformPoint(points[s.Point]);
                t.localScale = new Vector3(size, size, 1f);
                t.rotation = Quaternion.Euler(0f, 0f, s.Spin + s.Age * StarSpinPerSec);
                tint.a = env * alpha;
                s.Sr.color = tint;
                s.Sr.sortingOrder = order;
            }
        }

        void DrawMotes(int count, Color tint, int order, float dt, float alpha)
        {
            Vector2[] points = baked.Points;
            for (int i = 0; i < motes.Length; i++)
            {
                Spark s = motes[i];
                if (i >= count) { if (s != null) { s.Sr.enabled = false; s.Life = 0f; } continue; }
                if (s == null) s = motes[i] = NewSpark(FxSprites.Dot(), Random.Range(0f, MoteLifeMax));
                if (s.Life <= 0f)
                {
                    s.Wait -= dt;
                    if (s.Wait > 0f || points.Length == 0) { s.Sr.enabled = false; continue; }
                    s.Life = Random.Range(MoteLifeMin, MoteLifeMax);
                    s.Age = 0f;
                    s.Pos = art.TransformPoint(points[Random.Range(0, points.Length)]);
                    s.Vel = new Vector2(Random.Range(-MoteSide, MoteSide), Random.Range(MoteRiseMin, MoteRiseMax));
                    s.Size = Random.Range(MoteSizeMin, MoteSizeMax) * sizeK;
                }
                s.Age += dt;
                if (s.Age >= s.Life) { s.Life = 0f; s.Wait = Random.Range(0f, MoteGapMax); s.Sr.enabled = false; continue; }
                s.Pos += s.Vel * dt;
                s.Sr.enabled = TakeBudget();
                if (!s.Sr.enabled) continue;
                float k = s.Age / s.Life, env = Mathf.Sin(k * Mathf.PI), size = s.Size * (1f - 0.4f * k);
                Transform t = s.Sr.transform;
                t.position = new Vector3(s.Pos.x, s.Pos.y, 0f);
                t.localScale = new Vector3(size, size, 1f);
                tint.a = env * alpha;
                s.Sr.color = tint;
                s.Sr.sortingOrder = order;
            }
        }

        /// <summary>Screen-wide cap on live stars + motes: a crowd of top-tier heroes costs at most this many sprites, the later ones just skip theirs.</summary>
        static bool TakeBudget()
        {
            if (Time.frameCount != budgetFrame) { budgetFrame = Time.frameCount; budgetUsed = 0; }
            return budgetUsed++ < FeelConfig.EnhanceSparkBudget;
        }

        Spark NewSpark(Sprite sprite, float wait)
        {
            if (holder == null)
            {
                var go = new GameObject(art.name + "Sparks");
                holder = go.transform;
                holder.SetParent(holderParent, false);
            }
            SpriteRenderer sr = ProceduralSprites.Child(holder, "Spark", sprite);
            sr.sharedMaterial = FeelMaterials.Additive;
            sr.enabled = false;
            return new Spark { Sr = sr, Wait = wait };
        }

        public void Hide()
        {
            if (!shown) return;
            shown = false;
            halo.enabled = false;
            if (shine != null) shine.enabled = false;
            Clear(stars);
            Clear(motes);
        }

        static void Clear(Spark[] list)
        {
            foreach (Spark s in list) if (s != null) { s.Sr.enabled = false; s.Life = 0f; }
        }

        /// <summary>For owners that do not parent the sparks under themselves: removes the holder with its sprites.</summary>
        public void Dispose()
        {
            if (holder != null) Object.Destroy(holder.gameObject);
        }
    }
}
