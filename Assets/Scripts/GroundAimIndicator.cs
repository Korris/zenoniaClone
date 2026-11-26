using System.Text.RegularExpressions;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The ground preview of a Ground skill while GroundAimInput aims ("Magic Target Ring V1", designed and painted by ChatGPT 26/11/2026:
    /// docs/reference/skill-targeting/, cut by Tools/cut-skill-target-indicator-sheet.py into Sprites/fx/skill-target + Data/skill-target-indicator.json).
    /// Pieces lie flat on the walking plane (z = 0) so the 2.5D / 3D camera foreshortens them like the ground; each ring is scaled so its painted band
    /// (bandPx) sits exactly on the real radius: cast range round the hero, the skill's AoE round the chosen centre (Area / Placement / Leap ring by
    /// GroundTargetRules.IndicatorOf), the faint max reach of a rain's drops, the reticle, the dashed path to a leap's landing or to a clamped centre.
    /// Past the range the range ring turns gold and the reticle coral; a wall in a leap / turret way shows the red ring. Fade in 80 ms, V2 numbers below,
    /// a cast shrinks it to 92 % in 90 ms, a cancel fades it in 80 ms (ChatGPT timing table). Drawn only on this client.
    /// </summary>
    public sealed class GroundAimIndicator : MonoBehaviour
    {
        const string Dir = "Sprites/fx/skill-target/", Meta = "Data/skill-target-indicator";
        // Indicator V2 numbers (ChatGPT in-game review 26/11, docs/reference/skill-targeting/skill-targeting-ingame-review-1-chatgpt.txt, 8.5/10):
        // reticle 75 % of V1 (0.8 -> 0.6 tile), range ring quiet (0.24 for the first 0.4 s, then 0.16), max blast reach 0.16, AoE ring breathes 0.62-0.78 / 1.4 s,
        // round 2 (9.1/10): reticle 0.48 tile; the parts of the ring hidden by trees / cliffs show through at 0.32 while aiming (x-ray copy over the props);
        // no spin (the band marks the radius), out of range: range ring #F2B85B 0.65 + coral reticle #FF7466 blinking 0.70-0.95 / 0.5 s, dark rim #234F55 0.35.
        const float Z = -0.08f, ReticleTiles = 0.48f, FadeIn = 0.08f, ConfirmOut = 0.09f, CancelOut = 0.08f;
        static readonly Color RangeTeal = Hex(0x73C8C0), BlastTeal = Hex(0x87CFC4), EdgeGold = Hex(0xF2B85B), ReticleCream = Hex(0xFFF0B8), ReticleCoral = Hex(0xFF7466),
            Rim = Hex(0x234F55);

        static Color Hex(int rgb) { return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f); }

        sealed class Piece { public SpriteRenderer R; public float Band; public float W; }
        Piece range, outer, area, placement, leap, invalid, reticle, path, rim, xray;
        bool loaded;
        Vector2 lastPoint, lastFeet;
        SkillDef lastDef;

        void Awake() { Load(); }

        void Load()
        {
            TextAsset meta = Resources.Load<TextAsset>(Meta);
            string json = meta != null ? meta.text : "";
            int o = UltimateFx.GroundFxOrder + 40;
            range = Make("range-ring", json, o);
            outer = Make("range-ring", json, o + 1);
            path = Make("leap-path", json, o + 1);
            rim = Make("area-ring", json, o + 1); // takes the main ring's sprite each frame, drawn dark and 1.5 % wider under it
            area = Make("area-ring", json, o + 2);
            placement = Make("placement-ring", json, o + 2);
            leap = Make("leap-ring", json, o + 2);
            invalid = Make("invalid-ring", json, o + 3);
            reticle = Make("reticle", json, o + 4);
            xray = Make("area-ring", json, 32000); // over every y-sorted prop: the ring hidden by a tree or a cliff still reads (ChatGPT round 2)
            loaded = area.R.sprite != null;
            if (!loaded) Debug.LogWarning("[ZenoniaProto] skill target indicator art missing (" + Dir + "): run Tools/cut-skill-target-indicator-sheet.py");
        }

        Piece Make(string name, string json, int order)
        {
            var go = new GameObject("Aim_" + name);
            go.transform.SetParent(transform, false);
            var p = new Piece { R = go.AddComponent<SpriteRenderer>() };
            Texture2D tex = Resources.Load<Texture2D>(Dir + name);
            if (tex != null) { tex.wrapMode = TextureWrapMode.Clamp; p.R.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect); }
            p.R.sortingOrder = order;
            p.R.enabled = false;
            p.Band = Number(json, name, "bandPx", tex != null ? tex.width * 0.45f : 100f);
            p.W = Number(json, name, "w", tex != null ? tex.width : 100f);
            return p;
        }

        /// <summary>`key` of piece `name` in the cutter's JSON (a tiny reader: the file is ours and flat).</summary>
        static float Number(string json, string name, string key, float fallback)
        {
            Match m = Regex.Match(json, "\"" + Regex.Escape(name) + "\"\\s*:\\s*\\{[^}]*\"" + key + "\"\\s*:\\s*([0-9.]+)");
            float v;
            return m.Success && float.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) ? v : fallback;
        }

        void LateUpdate()
        {
            HeroController hero = LocalPlayer.Hero;
            float now = Time.unscaledTime, alpha, scale = 1f;
            if (GroundAimInput.Aiming && hero != null)
            {
                lastDef = GroundAimInput.AimDef; lastPoint = GroundAimInput.Point; lastFeet = hero.Position;
                alpha = Mathf.Clamp01((now - GroundAimInput.StartedAt) / FadeIn);
            }
            else
            {
                float t = now - GroundAimInput.EndedAt, len = GroundAimInput.EndedConfirmed ? ConfirmOut : CancelOut;
                if (!loaded || lastDef == null || t >= len) { HideAll(); return; }
                alpha = 1f - t / len;
                if (GroundAimInput.EndedConfirmed) scale = Mathf.Lerp(1f, 0.92f, t / len);
                if (hero != null) lastFeet = hero.Position;
            }
            if (!loaded || lastDef == null) { HideAll(); return; }
            Draw(alpha, scale, now);
        }

        void Draw(float alpha, float scale, float now)
        {
            float u = GameConfig.TileUnits, wave = 0.5f + 0.5f * Mathf.Sin(now * Mathf.PI * 2f / 1.4f);
            GroundIndicator kind = GroundTargetRules.IndicatorOf(lastDef);
            bool bad = GroundAimInput.Aiming ? GroundAimInput.Invalid : false, edge = GroundAimInput.Aiming && GroundAimInput.AtEdge;
            float aimAge = now - GroundAimInput.StartedAt;
            Color rangeTint = edge ? EdgeGold : RangeTeal; // the range ring and the path turn gold past the range; the AoE ring keeps its painted colours
            float rangeA = edge ? 0.65f : aimAge < 0.4f ? 0.24f : 0.16f;
            Ring(range, lastFeet, lastDef.CastRange * u, 0f, new Color(rangeTint.r, rangeTint.g, rangeTint.b, rangeA * alpha));
            float r = Mathf.Max(0.4f, GroundTargetRules.AreaRadiusTiles(lastDef)) * u * scale, ro = GroundTargetRules.OuterRadiusTiles(lastDef) * u * scale;
            Ring(outer, lastPoint, ro, 0f, new Color(BlastTeal.r, BlastTeal.g, BlastTeal.b, ro > r + 1f ? 0.16f * alpha : 0f));
            Piece main = bad ? invalid : kind == GroundIndicator.Placement ? placement : kind == GroundIndicator.Leap ? leap : area;
            foreach (Piece p in new[] { area, placement, leap, invalid }) if (p != main) p.R.enabled = false;
            float peak = kind == GroundIndicator.Placement ? 0.78f : kind == GroundIndicator.Leap ? 0.76f : 0.78f;
            float ringA = Mathf.Lerp(peak - 0.16f, peak, wave) * alpha;
            rim.R.sprite = main.R.sprite; rim.Band = main.Band;
            Ring(rim, lastPoint, r * 1.015f, 0f, new Color(Rim.r, Rim.g, Rim.b, 0.35f * ringA));
            Ring(main, lastPoint, r, 0f, new Color(1f, 1f, 1f, ringA));
            xray.R.sprite = main.R.sprite; xray.Band = main.Band;
            Ring(xray, lastPoint, r, 0f, new Color(1f, 1f, 1f, GroundAimInput.Aiming ? 0.32f * alpha : 0f));
            float rw = ReticleTiles * u * scale;
            Color rc = edge ? ReticleCoral : ReticleCream;
            float reticleA = edge ? Mathf.Lerp(0.70f, 0.95f, 0.5f + 0.5f * Mathf.Sin(now * Mathf.PI * 2f / 0.5f)) : 0.85f;
            Ring(reticle, lastPoint, rw * 0.5f * reticle.Band / Mathf.Max(1f, reticle.R.sprite != null ? reticle.R.sprite.rect.width * 0.5f : 1f), 0f, new Color(rc.r, rc.g, rc.b, (bad ? 0f : reticleA) * alpha));
            // dashed path: always for a leap (where the hero will land), else only when the centre was pulled back onto the range edge
            Vector2 d = lastPoint - lastFeet;
            bool showPath = (kind == GroundIndicator.Leap || edge) && d.magnitude > r * 0.5f && path.R.sprite != null;
            path.R.enabled = showPath;
            if (showPath)
            {
                Vector2 from = lastFeet + d.normalized * 6f, to = lastPoint - d.normalized * Mathf.Min(r * 0.6f, d.magnitude * 0.4f);
                float len = Mathf.Max(4f, (to - from).magnitude), k = len / Mathf.Max(1f, path.W);
                Vector2 mid = (from + to) * 0.5f;
                path.R.transform.position = new Vector3(mid.x, mid.y, Z);
                path.R.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                path.R.transform.localScale = new Vector3(k, Mathf.Clamp(k, 0.12f, 0.2f), 1f);
                path.R.color = new Color(rangeTint.r, rangeTint.g, rangeTint.b, (kind == GroundIndicator.Leap ? 0.9f : 0.55f) * alpha);
            }
        }

        /// <summary>Ring piece centred on `at` whose painted band lands on `radius` world units.</summary>
        static void Ring(Piece p, Vector2 at, float radius, float spin, Color c)
        {
            bool on = p.R.sprite != null && c.a > 0.004f && radius > 0.5f;
            p.R.enabled = on;
            if (!on) return;
            float k = radius / Mathf.Max(1f, p.Band);
            p.R.transform.position = new Vector3(at.x, at.y, Z);
            p.R.transform.rotation = Quaternion.Euler(0f, 0f, spin);
            p.R.transform.localScale = new Vector3(k, k, 1f);
            p.R.color = c;
        }

        void HideAll()
        {
            foreach (Piece p in new[] { range, outer, area, placement, leap, invalid, reticle, path, rim, xray }) if (p != null) p.R.enabled = false;
        }
    }
}
