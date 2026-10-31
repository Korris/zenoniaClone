using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Shared kit of the board-built assassin timelines (Blade Dance, Venom Daggers, Shadow Clone, Death Mark): loading the 6-frame painted strips, the back / front cut that keeps a hero
    /// visible inside a ring, and one Preload call for class spawn so the first cast never hitches.
    /// </summary>
    public static class AssassinBoardFx
    {
        public const int Frames = 6;

        /// <summary>One frame strip cut in two at a ground line: Back above it, Front below.</summary>
        public sealed class Strip { public Sprite[] Back = new Sprite[Frames], Front = new Sprite[Frames]; }

        /// <summary>A strip's four renderers: the back half draws behind the hero and the front half in front of his feet, so the hero is never covered.</summary>
        public sealed class Pair
        {
            public SpriteRenderer Back, BackGlow, Front, FrontGlow;

            public Pair(Transform parent, string name)
            {
                Back = UltimateFx.Layer(parent, name + "Back", out BackGlow);
                Front = UltimateFx.Layer(parent, name + "Front", out FrontGlow);
            }

            public void Show(Strip art, int frame, float alpha, float glowAlpha, Color glow, Vector2 at, float scale = 1f, float frontAlpha = 1f)
            {
                bool on = art != null && alpha > 0.01f;
                Back.enabled = Front.enabled = on;
                BackGlow.enabled = FrontGlow.enabled = on && FeelConfig.FxOn && glowAlpha > 0.01f;
                if (!on) return;
                Back.sprite = BackGlow.sprite = art.Back[frame];
                Front.sprite = FrontGlow.sprite = art.Front[frame];
                Back.color = new Color(1f, 1f, 1f, alpha);
                Front.color = new Color(1f, 1f, 1f, alpha * frontAlpha);
                BackGlow.color = FrontGlow.color = new Color(glow.r, glow.g, glow.b, glowAlpha * alpha);
                Back.transform.position = Front.transform.position = at;
                Back.transform.localScale = Front.transform.localScale = Vector3.one * scale;
                int behind = GameConfig.OrderBase(at.y) - 2;
                Back.sortingOrder = behind; BackGlow.sortingOrder = behind + 1;
                Front.sortingOrder = FeelConfig.FxOrder - 3; FrontGlow.sortingOrder = FeelConfig.FxOrder - 2;
            }

            public void Hide() { Back.enabled = BackGlow.enabled = Front.enabled = FrontGlow.enabled = false; }
        }

        public static Strip LoadCut(string folder, string name, float anchorY, float unitsPerPx)
        {
            var s = new Strip();
            for (int i = 0; i < Frames; i++)
            {
                Texture2D tex = SpriteSheet.Load(folder + name + "-" + (i + 1));
                if (tex == null) return null;
                float below = tex.height - anchorY, ppu = 1f / unitsPerPx;
                s.Back[i] = Sprite.Create(tex, new Rect(0f, below, tex.width, anchorY), new Vector2(0.5f, 0f), ppu, 0, SpriteMeshType.FullRect);
                s.Front[i] = Sprite.Create(tex, new Rect(0f, 0f, tex.width, below), new Vector2(0.5f, 1f), ppu, 0, SpriteMeshType.FullRect);
            }
            return s;
        }

        public static Sprite[] LoadWhole(string folder, string name, float unitsPerPx, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            var s = new Sprite[Frames];
            for (int i = 0; i < Frames; i++)
            {
                Texture2D tex = SpriteSheet.Load(folder + name + "-" + (i + 1));
                if (tex == null) return null;
                s[i] = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(pivotX, pivotY), 1f / unitsPerPx, 0, SpriteMeshType.FullRect);
            }
            return s;
        }

        public static int ByProgress(float t) { return Mathf.Clamp(Mathf.FloorToInt(t * Frames), 0, Frames - 1); }

        /// <summary>1 inside [from, to], easing over `ms` at both ends.</summary>
        public static float Fade(float age, float from, float to, float ms) { return Mathf.Clamp01(Mathf.Min((age - from) / ms, (to - age) / ms)); }

        /// <summary>The sheet row (0 down, 1 up, 2 left, 3 right) that looks along `dir`.</summary>
        public static int FacingOf(Vector2 dir) { return Mathf.Abs(dir.x) >= Mathf.Abs(dir.y) ? (dir.x < 0f ? 2 : 3) : (dir.y > 0f ? 1 : 0); }

        /// <summary>Loads and slices the painted frames of the four timelines ahead of the first cast.</summary>
        public static void Preload()
        {
            BladeDanceFx.Preload();
        }
    }
}
