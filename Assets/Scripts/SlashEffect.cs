using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Painted warrior slash (Sprites/classes/warrior/fx/warrior-slash-vfx.png = ArtLibrary/vfx/warrior, already red; 4 x 3 cells: row 0 = crescent slash,
    /// row 1 = finisher ring, row 2 = impact burst). Row 0 is painted as a horizontal arc bulging down, so it turns +90 deg to face the swing.
    /// Falls back to the older recoloured warrior-slashes-chibi.png (4 x 2). Pooled.
    /// </summary>
    public class SlashEffect : MonoBehaviour
    {
        static readonly List<SlashEffect> Pool = new List<SlashEffect>();
        static Transform root;
        static Sprite[][] frames;
        static Texture2D painted; // the untinted sheet, source of the per-weapon recolours
        static readonly Dictionary<string, Sprite[][]> byWeapon = new Dictionary<string, Sprite[][]>(); // weapon glow hex -> frames (option C: white core, weapon-coloured edge)
        const float WeaponTintStrength = 1f; // whole arc in the weapon colour, no white core (user decision 2026-11-03)
        static float slashTurn; // extra rotation of the normal-slash row so its bulge points along the swing
        SpriteRenderer sr, core; // core = additive copy of the same frame (bright centre, feeds the bloom)
        bool finisherLook;
        Sprite[] row;
        Color coreGlow;
        float age, life;

        public static void Init(Transform parent)
        {
            root = parent;
            Pool.Clear();
            byWeapon.Clear();
            // Each cell becomes a 1-unit-wide sprite (scaled per use: slash 32u, arc 48u like the web FX strips).
            painted = Resources.Load<Texture2D>("Sprites/classes/warrior/fx/warrior-slash-vfx");
            if (painted != null)
            {
                frames = SpriteTint.Slice(painted, 4, 3, painted.width / 4, new Vector2(0.5f, 0.5f));
                slashTurn = 90f;
                return;
            }
            Texture2D tex = SpriteSheet.Load("Sprites/classes/warrior/fx/warrior-slashes-chibi");
            frames = tex != null ? SpriteTint.Slice(SpriteTint.Colorize(tex, ClassPalette.Red.Tint), 4, 2, tex.width / 4, new Vector2(0.5f, 0.5f)) : null;
            slashTurn = 0f;
        }

        /// <summary>Frames recoloured for a weapon glow (hex like "#59a7ff"); the painted class frames when null or unparsable. Built once per colour.</summary>
        static Sprite[][] FramesFor(string weaponGlow)
        {
            if (string.IsNullOrEmpty(weaponGlow) || painted == null) return frames;
            Sprite[][] f;
            if (byWeapon.TryGetValue(weaponGlow, out f)) return f;
            Color tint;
            f = ColorUtility.TryParseHtmlString(weaponGlow, out tint)
                ? SpriteTint.Slice(SpriteTint.ColorizeBody(painted, Saturated(tint), WeaponTintStrength, false), 4, 3, painted.width / 4, new Vector2(0.5f, 0.5f))
                : frames;
            byWeapon[weaponGlow] = f;
            return f;
        }

        /// <summary>The glow colours are pale (halo tints); on the slash, over snow and bloom, a pale blue reads as white, so the hue is pushed to full saturation.</summary>
        static Color Saturated(Color c)
        {
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            return Color.HSVToRGB(h, Mathf.Max(s, 0.9f), 1f);
        }

        /// <summary>Spawns a slash. angleDeg: 0 = right, 90 = up (counter-clockwise). weaponGlow = the worn weapon's glow colour (null = class colour only).</summary>
        public static void Spawn(float x, float y, float angleDeg, float scale, bool finisher, bool flipY, int order, string weaponGlow = null)
        {
            if (!NetMode.RendersFx) return; // headless server: no cosmetics
            if (frames == null || root == null) return;
            Sprite[][] sheet = FramesFor(weaponGlow);
            Color weapon;
            Color classGlow = ClassPalette.Red.Glow;
            Color core = !string.IsNullOrEmpty(weaponGlow) && ColorUtility.TryParseHtmlString(weaponGlow, out weapon) ? Saturated(weapon) : classGlow;
            SlashEffect fx = null;
            for (int i = 0; i < Pool.Count; i++) if (!Pool[i].gameObject.activeSelf) { fx = Pool[i]; break; }
            if (fx == null)
            {
                var go = new GameObject("Slash");
                go.transform.SetParent(root, false);
                fx = go.AddComponent<SlashEffect>();
                fx.sr = go.AddComponent<SpriteRenderer>();
                fx.core = ProceduralSprites.Child(go.transform, "Core", null);
                fx.core.sharedMaterial = FeelMaterials.Additive;
                Pool.Add(fx);
            }
            fx.row = SpriteSheet.Row(sheet, finisher ? 1 : 0); // hit 4 = the finisher ring, as originally
            fx.coreGlow = core;
            fx.age = 0f;
            fx.life = finisher ? 0.24f : 0.17f;
            float size = (finisher ? 48f : 32f) * scale;
            fx.transform.position = new Vector3(x, y, 0f);
            fx.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg + (finisher ? 0f : slashTurn));
            bool flip = flipY && (finisher || slashTurn == 0f);
            bool mirror = flipY && !finisher && slashTurn != 0f; // turned art: the swing-direction flip is a horizontal mirror
            fx.transform.localScale = new Vector3(mirror ? -size : size, flip ? -size : size, 1f);
            fx.sr.sortingOrder = order;
            fx.core.sortingOrder = order + 1;
            fx.finisherLook = finisher;
            fx.gameObject.SetActive(true);
            fx.Apply();
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= life) { gameObject.SetActive(false); return; }
            Apply();
        }

        void Apply()
        {
            if (row == null || row.Length == 0) { sr.sprite = null; return; }
            int i = Mathf.Min(row.Length - 1, Mathf.FloorToInt(age / life * row.Length));
            sr.sprite = row[i];
            float fade = Mathf.Clamp01((life - age) / (life * 0.35f));
            sr.color = new Color(1f, 1f, 1f, fade);
            core.enabled = FeelConfig.FxOn;
            core.sprite = sr.sprite;
            Color g = coreGlow;
            core.color = new Color(g.r, g.g, g.b, fade * (finisherLook ? 1f : 0.85f));
        }
    }
}
