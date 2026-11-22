using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>The look a floating number gets (one baked colour atlas per kind; crit red-orange and the multi-hit Total yellow have own sets).</summary>
    public enum DamageNumberKind { Normal, Finisher, Crit, Other, Hurt, OtherHurt, Heal, PotionHp, PotionMp, Exp, Pet, Total }

    /// <summary>
    /// Motion / size table of the floating numbers, ChatGPT V3 "Comic Impact" (docs/reference/damage-numbers-chatgpt-spec.md).
    /// Pixel values are for a 720 px tall screen and scaled by Screen.height / 720.
    /// </summary>
    public struct DamageNumberMotion
    {
        public float LifeMs, FadeFromMs, RisePx, RiseDelayMs, HeightPx, Alpha;
        public float[] ScaleKeysMs, ScaleKeys; // piecewise scale curve (time ms -> scale), smoothed between keys
        public string Style;                   // atlas style name (base-<style>.png), null = crit set
        public float ShakeXPx, ShakeYPx, ShakeMs; // local shake at spawn, 3 decaying swings

        static DamageNumberMotion Make(string style, float life, float fadeFrom, float rise, float height, float[] keysMs, float[] keys)
        {
            return new DamageNumberMotion { Style = style, LifeMs = life, FadeFromMs = fadeFrom, RisePx = rise, HeightPx = height, ScaleKeysMs = keysMs, ScaleKeys = keys, Alpha = 1f };
        }

        static readonly DamageNumberMotion[] Table = BuildTable();

        static DamageNumberMotion[] BuildTable()
        {
            var t = new DamageNumberMotion[12];
            float[] pop = { 0f, 90f, 180f };
            // Normal / Finisher / Crit: Multi-Hit Barrage values (ChatGPT V3, docs/reference/warrior-skill-redesign-v2-chatgpt.md): short life and a
            // 95 ms pop so a hit every 35-65 ms does not leave a crowd of numbers all scaling at once. Sizes 30 / 42 / 52 px and ~1 s of life
            // (numbers pile up into a dense column, user 17/11 "ben ragnarok dam no ra day dac"): as big as
            // Ragnarok Midgard Senki's (user OK 2026-11-17, docs/reference/midgard-senki-damage-numbers/).
            // 20/11 same-spot stack (ChatGPT RO-style spec, DamageNumber.Stream.cs): 650 ms life, solid to 350 ms, rise 48 px ease-out cubic,
            // pop 0.8 -> 1.15 -> 1.0 in 120 ms. A hit 400 ms later spawns at the same point while this one is ~45 px up: a vertical stack, no dodge.
            // Size stays 30 px (user OK 17/11, Midgard Senki size) instead of ChatGPT's 24 px.
            t[(int)DamageNumberKind.Normal] = Make("normal", 650f, 350f, 48f, 30f, new[] { 0f, 60f, 120f }, new[] { 0.8f, 1.15f, 1f });
            t[(int)DamageNumberKind.Finisher] = Make("normal", 750f, 420f, 52f, 42f, new[] { 0f, 70f, 135f }, new[] { 0.65f, 1.3f, 1f }); // white like normal hits (user 15/11), still bigger with ring + slash
            // crit (22/11 RO spec): red-orange set on the spiky burst, a stronger pop 0.4 -> 1.75 in 65 ms, back to 1.0 by 160 ms with a +-3 px
            // shake, short read hold, then rise; 850 ms life. 46 px (~150 % of a normal hit; was 52 px gold with a softer 1.45 pop).
            t[(int)DamageNumberKind.Crit] = Make(null, 850f, 600f, 60f, 46f, new[] { 0f, 65f, 160f }, new[] { 0.4f, 1.75f, 1f });
            t[(int)DamageNumberKind.Crit].RiseDelayMs = 120f;
            t[(int)DamageNumberKind.Crit].ShakeXPx = 3f; t[(int)DamageNumberKind.Crit].ShakeYPx = 2f; t[(int)DamageNumberKind.Crit].ShakeMs = 170f;
            t[(int)DamageNumberKind.Other] = Make("other", 500f, 300f, 22f, 21f, new[] { 0f, 120f }, new[] { 0.9f, 1f });
            t[(int)DamageNumberKind.Other].Alpha = 0.85f;
            t[(int)DamageNumberKind.Hurt] = Make("hurt", 650f, 430f, 18f, 28f, pop, new[] { 0.8f, 1.18f, 1f });
            t[(int)DamageNumberKind.Hurt].ShakeXPx = 4f; t[(int)DamageNumberKind.Hurt].ShakeMs = 140f;
            t[(int)DamageNumberKind.OtherHurt] = Make("hurt", 500f, 300f, 16f, 21f, new[] { 0f, 120f }, new[] { 0.9f, 1f });
            t[(int)DamageNumberKind.OtherHurt].Alpha = 0.85f;
            float[] soft = { 0.75f, 1.1f, 1f };
            t[(int)DamageNumberKind.Heal] = Make("heal", 850f, 600f, 44f, 27f, pop, soft);
            t[(int)DamageNumberKind.PotionHp] = Make("potion-hp", 850f, 600f, 44f, 24f, pop, soft);
            t[(int)DamageNumberKind.PotionMp] = Make("potion-mp", 850f, 600f, 44f, 24f, pop, soft);
            t[(int)DamageNumberKind.Exp] = Make("exp", 650f, 450f, 24f, 20f, new[] { 0f, 120f }, new[] { 0.88f, 1f });
            t[(int)DamageNumberKind.Pet] = Make("normal", 650f, 420f, 28f, 30f, pop, new[] { 0.6f, 1.3f, 1f }); // a pet's hit: the owner's size, its own colour (teal basic / gold skill), pops harder, to the right (DamageNumber.Layout)
            // multi-hit TOTAL (22/11 RO spec, DamageNumber.Total.cs): bright yellow, thick black outline, 150 % of a normal hit, above the white
            // cluster (it follows the cluster, so almost no own rise); pops in after the last hit and bumps again when the chain grows.
            t[(int)DamageNumberKind.Total] = Make("total", 1100f, 760f, 0.01f, 45f, new[] { 0f, 80f, 170f }, new[] { 0.55f, 1.3f, 1f });
            return t;
        }

        public static DamageNumberMotion Of(DamageNumberKind k) { return Table[(int)k]; }

        /// <summary>Scale at this age; flat 1 when effects are off.</summary>
        public float Scale(float ms)
        {
            if (!FeelConfig.FxOn) return 1f;
            int n = ScaleKeys.Length;
            if (ms <= ScaleKeysMs[0]) return ScaleKeys[0];
            for (int i = 1; i < n; i++)
                if (ms < ScaleKeysMs[i])
                {
                    float u = (ms - ScaleKeysMs[i - 1]) / (ScaleKeysMs[i] - ScaleKeysMs[i - 1]);
                    if (i == 1) u = 1f - (1f - u) * (1f - u); // the first pop eases out (fast burst, soft arrival)
                    else u = u * u * (3f - 2f * u);
                    return Mathf.LerpUnclamped(ScaleKeys[i - 1], ScaleKeys[i], u);
                }
            return ScaleKeys[n - 1];
        }

        /// <summary>Upward screen offset in px (720 p): ease-out cubic after the hold (RiseDelayMs).</summary>
        public float Rise(float ms)
        {
            float u = Mathf.Clamp01((ms - RiseDelayMs) / (LifeMs - RiseDelayMs));
            float v = 1f - u;
            return RisePx * (1f - v * v * v); // ease-out cubic (ChatGPT 20/11)
        }

        /// <summary>Opacity: full until FadeFromMs, then ease-in quad to 0 at LifeMs; times the kind's max alpha.</summary>
        public float Fade(float ms)
        {
            float u = Mathf.Clamp01((ms - FadeFromMs) / (LifeMs - FadeFromMs));
            return (1f - u * u) * Alpha;
        }

        /// <summary>Local shake at spawn: 3 decaying swings over ShakeMs (x and y out of phase); 0 when effects are off.</summary>
        public Vector2 Shake(float ms)
        {
            if (ShakeMs <= 0f || ms >= ShakeMs || ms < 0f || !FeelConfig.FxOn) return Vector2.zero;
            float k = 1f - ms / ShakeMs, w = ms / ShakeMs * Mathf.PI * 6f;
            return new Vector2(ShakeXPx * k * Mathf.Sin(w), ShakeYPx * k * Mathf.Cos(w));
        }

        /// <summary>Picks the kind from the existing Spawn arguments, so callers (offline and online) stay unchanged.</summary>
        public static DamageNumberKind Classify(string text, Color c, bool big, bool crit, bool faint)
        {
            bool reddish = c.r > 0.9f && c.g < 0.7f && c.b < 0.7f;
            if (faint) return reddish ? DamageNumberKind.OtherHurt : DamageNumberKind.Other;
            if (crit) return DamageNumberKind.Crit;
            if (text.EndsWith(" EXP")) return DamageNumberKind.Exp;
            if (text.StartsWith("HP ")) return DamageNumberKind.PotionHp;
            if (text.StartsWith("MP ")) return DamageNumberKind.PotionMp;
            if (text.StartsWith("+")) return DamageNumberKind.Heal;
            if (!big && c.r < 0.6f && c.g > 0.75f && c.b > 0.75f) return DamageNumberKind.Pet; // teal: a pet's basic attack (Monster.Visuals.HitFx)
            if (!big && c.r > 0.95f && c.g > 0.8f && c.g < 0.9f && c.b > 0.4f && c.b < 0.6f) return DamageNumberKind.Pet; // pale gold: a pet's skill
            if (reddish && c.g < 0.5f) return DamageNumberKind.Hurt;
            if (big && c.b < 0.5f && c.r > 0.9f) return DamageNumberKind.Finisher; // orange finisher (crit yellow is caught above)
            return DamageNumberKind.Normal;
        }
    }
}
