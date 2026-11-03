using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Enhance effect of an item icon in a UI cell, the canvas twin of HeldEnhanceFx: same bands (FeelConfig.EnhanceLooks) and same baked layers, made from the icon itself.
    /// A halo behind the icon, a light band over it, twinkling stars and rising motes. Runs on unscaled time: the game clock stops while the bag is open.
    /// Bands share one shine material, so icons of a band sweep together and the canvas keeps batching.
    /// </summary>
    public sealed class ItemEnhanceSlotFx : MonoBehaviour
    {
        const float HaloSpan = 1.7f;  // of the icon; the baked halo is twice the art, so its faint outer ring stays near the cell
        const float HaloStrength = 0.8f, StarPeak = 0.85f, MotePeak = 0.8f;
        const float StarSizeK = 0.075f, StarLife = 0.5f, StarSpinPerSec = 70f, MoteSizeK = 0.06f; // per unit of look.StarSize / of the icon
        const float MoteLifeMin = 0.8f, MoteLifeMax = 1.3f, MoteRise = 0.3f; // rise in icons per second
        const int MaxMotes = 5;       // a cell is small: +15's ten motes would hide the icon

        sealed class Spark { public Image Img; public Vector2 Pos; public float Age, Life, Wait, Spin; }

        static readonly Material[] ShineByBand = new Material[FeelConfig.EnhanceLooks.Length];
        static readonly int[] ShineStamp = new int[FeelConfig.EnhanceLooks.Length];
        static readonly Dictionary<string, Color> TierGlow = new Dictionary<string, Color>();
        static Material additive;
        static int shineId = -1;

        Image icon, halo, shine;
        readonly List<Spark> stars = new List<Spark>(), motes = new List<Spark>();
        HeldGlowBaker.Baked baked;
        Color glow;
        int band;

        /// <summary>Shows the item's effect on this icon, or clears it. Cheap for cells that never held an enhanced item: nothing is created.</summary>
        public static void Show(Image icon, Item item)
        {
            int b = item != null && icon.sprite != null ? EnhanceStats.VisualBand(item) : 0;
            ItemEnhanceSlotFx fx = icon.GetComponent<ItemEnhanceSlotFx>();
            if (b == 0 && fx == null) return;
            if (fx == null) { fx = icon.gameObject.AddComponent<ItemEnhanceSlotFx>(); fx.icon = icon; }
            fx.Apply(item, b);
        }

        void Apply(Item item, int newBand)
        {
            band = newBand;
            if (band == 0) { SetVisible(false); return; }
            baked = HeldGlowBaker.Get(icon.sprite, "#ffffff"); // the colour below replaces the bake's: it depends on the item, the bake only on the art
            if (baked.Shine == null) { band = 0; SetVisible(false); return; }
            glow = GlowOf(item);
            if (halo == null) Build();
            halo.sprite = baked.Halo;
            shine.sprite = baked.Shine;
            shine.material = ShineMaterial(band);
            // The halo sits below the icon, so it is a sibling that has to follow the icon's box (SetIcon resizes it per window).
            RectTransform from = icon.rectTransform, to = halo.rectTransform;
            to.anchorMin = from.anchorMin; to.anchorMax = from.anchorMax; to.pivot = new Vector2(0.5f, 0.5f);
            to.sizeDelta = from.sizeDelta * HaloSpan;
            to.anchoredPosition = from.anchoredPosition + new Vector2((0.5f - from.pivot.x) * from.sizeDelta.x, (0.5f - from.pivot.y) * from.sizeDelta.y);
            if (to.GetSiblingIndex() > from.GetSiblingIndex()) to.SetSiblingIndex(from.GetSiblingIndex()); // first time only: the moved halo pushes the icon up one
            SetVisible(true);
        }

        void Build()
        {
            if (additive == null) additive = new Material(Resources.Load<Shader>("Shaders/UiAdditive")) { name = "Ui_EnhanceAdditive" };
            halo = NewImage(icon.transform.parent, "EnhanceHalo", additive);
            shine = NewImage(icon.transform, "EnhanceShine", null);
            RectTransform rt = shine.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static Image NewImage(Transform parent, string name, Material material)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.material = material;
            return img;
        }

        void SetVisible(bool on)
        {
            if (halo != null) { halo.enabled = on; shine.enabled = on; }
            if (!on) { foreach (Spark s in stars) s.Img.enabled = false; foreach (Spark s in motes) s.Img.enabled = false; }
        }

        void OnDisable() { if (halo != null) halo.enabled = false; }

        void OnDestroy() { if (halo != null) Destroy(halo.gameObject); }

        void Update()
        {
            if (band == 0) return;
            if (!icon.enabled || !FeelConfig.FxOn) { SetVisible(false); return; }
            FeelConfig.EnhanceLook look = FeelConfig.EnhanceLooks[band - 1];
            float now = Time.unscaledTime, dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f), fade = icon.color.a;
            halo.enabled = look.HaloAlpha > 0f;
            shine.enabled = true;
            shine.color = new Color(glow.r, glow.g, glow.b, look.ShineAlpha * fade);
            halo.color = new Color(glow.r, glow.g, glow.b, (look.HaloAlpha + look.HaloBreath * Mathf.Sin(now * look.BreathHz * Mathf.PI * 2f)) * HaloStrength * fade);
            SweepShine(look, now);
            float size = icon.rectTransform.rect.width;
            Color tint = Color.Lerp(glow, Color.white, 0.3f);
            DrawStars(CellLooks[band - 1], size, tint, dt, fade);
            DrawMotes(CellLooks[band - 1], size, tint, dt, fade);
        }

        /// <summary>Twinkles of the item cell per band. The held weapon dropped stars and motes for its own per-band features (HeldEnhanceFx);
        /// the cell keeps its stars as they were, they read well on a still icon.</summary>
        struct CellSparks { public int Stars, Motes; public float StarGapMin, StarGapMax, StarSize; }
        static readonly CellSparks[] CellLooks =
        {
            new CellSparks(),
            new CellSparks(),
            new CellSparks { Stars = 1, StarGapMin = 1.2f, StarGapMax = 2.6f, StarSize = 3.5f },
            new CellSparks { Stars = 2, StarGapMin = 0.7f, StarGapMax = 1.6f, StarSize = 4f, Motes = 4 },
            new CellSparks { Stars = 3, StarGapMin = 0.1f, StarGapMax = 0.6f, StarSize = 4f, Motes = 10 },
        };

        void SweepShine(FeelConfig.EnhanceLook look, float now)
        {
            int i = band - 1;
            if (ShineStamp[i] == Time.frameCount) return; // once a frame for every icon of the band
            ShineStamp[i] = Time.frameCount;
            float t = Mathf.Repeat(now, look.SweepPeriod) / look.SweepSec;
            float head = t < 1f ? Mathf.LerpUnclamped(-look.SweepWidth, 1f + look.SweepWidth, t) : -2f;
            if (shineId < 0) shineId = Shader.PropertyToID("_Shine");
            ShineByBand[i].SetVector(shineId, new Vector4(head, look.SweepWidth, look.ShineLift, 0f));
        }

        void DrawStars(CellSparks look, float size, Color tint, float dt, float fade)
        {
            while (stars.Count < look.Stars) stars.Add(NewSpark("EnhanceStar", FxSprites.Star(), Random.Range(0f, look.StarGapMax)));
            for (int i = 0; i < stars.Count; i++)
            {
                Spark s = stars[i];
                if (i >= look.Stars) { s.Img.enabled = false; s.Life = 0f; continue; }
                if (s.Life <= 0f)
                {
                    s.Wait -= dt;
                    if (s.Wait > 0f) { s.Img.enabled = false; continue; }
                    s.Life = StarLife; s.Age = 0f; s.Spin = Random.Range(0f, 90f);
                    s.Pos = RandomPoint();
                }
                s.Age += dt;
                if (s.Age >= s.Life) { s.Life = 0f; s.Wait = Random.Range(look.StarGapMin, look.StarGapMax); s.Img.enabled = false; continue; }
                float env = Mathf.Sin(s.Age / s.Life * Mathf.PI), side = look.StarSize * StarSizeK * size * (0.6f + 0.4f * env);
                Place(s, s.Pos * size, side, s.Spin + s.Age * StarSpinPerSec);
                tint.a = env * StarPeak * fade;
                s.Img.color = tint;
            }
        }

        void DrawMotes(CellSparks look, float size, Color tint, float dt, float fade)
        {
            int count = Mathf.Min(look.Motes, MaxMotes);
            while (motes.Count < count) motes.Add(NewSpark("EnhanceMote", FxSprites.Dot(), Random.Range(0f, MoteLifeMax)));
            for (int i = 0; i < motes.Count; i++)
            {
                Spark s = motes[i];
                if (i >= count) { s.Img.enabled = false; s.Life = 0f; continue; }
                if (s.Life <= 0f)
                {
                    s.Wait -= dt;
                    if (s.Wait > 0f) { s.Img.enabled = false; continue; }
                    s.Life = Random.Range(MoteLifeMin, MoteLifeMax); s.Age = 0f;
                    s.Pos = RandomPoint();
                }
                s.Age += dt;
                if (s.Age >= s.Life) { s.Life = 0f; s.Wait = Random.Range(0f, 0.5f); s.Img.enabled = false; continue; }
                float k = s.Age / s.Life;
                Place(s, (s.Pos + new Vector2(0f, MoteRise * s.Age)) * size, MoteSizeK * size * (1f - 0.4f * k), 0f);
                tint.a = Mathf.Sin(k * Mathf.PI) * MotePeak * fade;
                s.Img.color = tint;
            }
        }

        /// <summary>A bright spot of the icon, in icons from its centre. Bake points are sprite local units, Ppu per art pixel.</summary>
        Vector2 RandomPoint()
        {
            Vector2[] p = baked.Points;
            if (p == null || p.Length == 0) return Vector2.zero;
            return p[Random.Range(0, p.Length)] * GearOverlays.Ppu / icon.sprite.texture.width;
        }

        Spark NewSpark(string name, Sprite sprite, float wait)
        {
            Image img = NewImage(icon.transform, name, additive);
            img.sprite = sprite;
            img.enabled = false;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            return new Spark { Img = img, Wait = wait };
        }

        static void Place(Spark s, Vector2 pos, float side, float spin)
        {
            s.Img.enabled = true;
            RectTransform rt = s.Img.rectTransform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(side, side);
            rt.localRotation = Quaternion.Euler(0f, 0f, spin);
        }

        static Material ShineMaterial(int band)
        {
            int i = band - 1;
            if (ShineByBand[i] == null) ShineByBand[i] = new Material(Resources.Load<Shader>("Shaders/UiShine")) { name = "Ui_EnhanceShine" + band };
            return ShineByBand[i];
        }

        /// <summary>The colour the item glows with on the hero: its tier's manifest colour for class gear that has a part, otherwise its rarity colour.</summary>
        static Color GlowOf(Item item)
        {
            string key = ItemInfo.IconName(item);
            Color c;
            if (TierGlow.TryGetValue(key, out c)) return c;
            bool gear = (item.slot == Slots.Weapon || item.slot == Slots.Offhand) && !string.IsNullOrEmpty(item.classId);
            string glowHex = gear ? ManifestGlow(item) : null;
            if (glowHex == null) return LootSprites.RarityColor(item); // depends on the item, so not cached under the icon
            c = LootSprites.Hex(glowHex);
            TierGlow[key] = c;
            return c;
        }

        static string ManifestGlow(Item item)
        {
            bool offhand = item.slot == Slots.Offhand;
            foreach (HeroClassDef cls in HeroClassDef.All)
            {
                if (cls.Id.ToString().ToLowerInvariant() != item.classId) continue;
                foreach (GearPartSpec part in cls.Parts)
                {
                    if (part.IsOffhand != offhand) continue;
                    HeldGearData data = HeldGearData.Load(cls);
                    OverlayEntry[] m = data != null ? data.Manifest(part.Id) : null;
                    return m != null && m.Length > 0 ? m[Mathf.Clamp(ItemInfo.ArtTier(item), 0, m.Length - 1)].glow : null;
                }
            }
            return null;
        }

        /// <summary>Forgets what was made for a scene that is gone (Bootstrap's ResetState pattern).</summary>
        public static void ResetState() { TierGlow.Clear(); additive = null; for (int i = 0; i < ShineByBand.Length; i++) { ShineByBand[i] = null; ShineStamp[i] = 0; } }
    }
}
