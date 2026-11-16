using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Root of a full-screen "place" screen (bag = armory, skills = training hall ...) and of safe-area HUD anchors.
    /// The UiKit canvas is 640 units high and 853 (4:3) to 1529 (21:9) units wide, so a screen is built from three layers:
    ///   Root        the whole screen
    ///   Background  one scene picture drawn "cover": it fills the screen and is cropped at the sides / top, never stretched; swallows clicks
    ///   Content     a 16:9 design frame of 1138 x 640 units, centred in the device safe area and scaled down as a whole when the
    ///               screen is narrower than 16:9 (4:3 = 0.75x); on wider screens it keeps its size and the background shows beside it.
    /// Views lay their widgets out once, in Content, in design units (top-left based). HUD pieces use Safe instead: a full-screen
    /// rect inset to the safe area, to anchor on its corners.
    /// </summary>
    public sealed class PaperLayout : MonoBehaviour
    {
        public const float DesignWidth = 1138f, DesignHeight = 640f;

        public RectTransform Root { get; private set; }
        /// <summary>Full-screen rect inset to Screen.safeArea (notches, rounded corners).</summary>
        public RectTransform Safe { get; private set; }
        /// <summary>The 16:9 design frame (null for a HUD root made with CreateSafeArea).</summary>
        public RectTransform Content { get; private set; }

        Image background;
        Texture2D backgroundTexture;
        Sprite backgroundSprite;
        AspectRatioFitter cover;
        Rect shownSafe;
        Vector2 shownSize;

        /// <summary>A place screen: `fallback` is the flat colour shown until SetBackground loads a scene (or when its art is missing).</summary>
        public static PaperLayout Create(Transform canvas, string name, Color fallback)
        {
            PaperLayout l = Make(canvas, name);
            RectTransform bg = UiKit.Rect(l.Root, "Background", 0f, 0f, 100f, 100f, true);
            l.background = UiKit.Panel(bg, fallback, true); // covers the screen, so it is also the click blocker
            l.cover = bg.gameObject.AddComponent<AspectRatioFitter>();
            l.cover.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            l.cover.aspectRatio = DesignWidth / DesignHeight;
            l.Safe = UiKit.Fill(l.Root, "Safe");
            l.Content = UiKit.Rect(l.Safe, "Content", 0f, 0f, DesignWidth, DesignHeight, true);
            l.Fit();
            return l;
        }

        /// <summary>HUD root: only the safe-area rect, no background, clicks pass through.</summary>
        public static PaperLayout CreateSafeArea(Transform canvas, string name)
        {
            PaperLayout l = Make(canvas, name);
            l.Safe = UiKit.Fill(l.Root, "Safe");
            l.Fit();
            return l;
        }

        static PaperLayout Make(Transform canvas, string name)
        {
            RectTransform root = UiKit.Fill(canvas, name);
            var l = root.gameObject.AddComponent<PaperLayout>();
            l.Root = root;
            return l;
        }

        /// <summary>Loads the scene picture Resources/Sprites/ui/paper/{piece} (call when the screen opens). False = not delivered, the flat colour stays.</summary>
        public bool SetBackground(string piece)
        {
            ReleaseBackground();
            backgroundTexture = PaperArt.LoadBackground(piece);
            if (backgroundTexture == null) return false;
            backgroundSprite = Sprite.Create(backgroundTexture, new Rect(0, 0, backgroundTexture.width, backgroundTexture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            background.sprite = backgroundSprite;
            background.color = Color.white;
            cover.aspectRatio = (float)backgroundTexture.width / backgroundTexture.height;
            return true;
        }

        /// <summary>Frees the scene picture (call when the screen closes: one big texture per screen must not stay in memory).</summary>
        public void ReleaseBackground()
        {
            if (backgroundSprite == null) return;
            background.sprite = null;
            Destroy(backgroundSprite);
            PaperArt.Unload(backgroundTexture);
            backgroundSprite = null; backgroundTexture = null;
        }

        void OnDestroy() { ReleaseBackground(); }

        void Update()
        {
            if (Screen.safeArea != shownSafe || Root.rect.size != shownSize) Fit(); // rotation, window resize, a notch appearing
        }

        void Fit()
        {
            shownSafe = Screen.safeArea;
            shownSize = Root.rect.size;
            float sw = Mathf.Max(1, Screen.width), sh = Mathf.Max(1, Screen.height);
            Safe.anchorMin = new Vector2(shownSafe.xMin / sw, shownSafe.yMin / sh);
            Safe.anchorMax = new Vector2(shownSafe.xMax / sw, shownSafe.yMax / sh);
            Safe.offsetMin = Safe.offsetMax = Vector2.zero;
            if (Content == null) return;
            // Canvas units of the safe area (Root spans the whole canvas), then one uniform scale so the 16:9 frame fits inside.
            float w = shownSize.x * shownSafe.width / sw, h = shownSize.y * shownSafe.height / sh;
            float scale = w > 0f && h > 0f ? Mathf.Min(w / DesignWidth, h / DesignHeight) : 1f;
            Content.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
