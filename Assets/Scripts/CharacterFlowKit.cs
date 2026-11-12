#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Pieces shared by the two uGUI screens of the online character flow (CharacterListScreen, CharacterCustomizeScreen): backdrop, a 1138 x 640 stage that shrinks on narrow screens (the hero
    /// select's layout rule), the framed plate, the gem-cut buttons of Sprites/ui/character-flow with a text label, and the title / gear in the screen's corners.
    /// </summary>
    public static class CharacterFlowKit
    {
        public const float StageW = 1138f, StageH = 640f;
        public static readonly Color Gold = new Color(1f, 0.86f, 0.5f), Ink = new Color(0.25f, 0.12f, 0.02f), Soft = new Color(0.85f, 0.92f, 1f), Bad = new Color(1f, 0.6f, 0.5f);
        const float PanelSlice = 38f, PanelDensity = 3f; // the hero select plate: art pixels of the corner ornaments, and how many fit one UI unit

        public static Sprite Art(string name) { return UiArt.Get("character-flow/" + name); }

        /// <summary>Art names of the hero select (banner-wizard, hero-boxing ...): the Fighter keeps the old "boxing" file names.</summary>
        public static string ClassArtName(HeroClass id) { return id == HeroClass.Fighter ? "boxing" : HeroClassDef.Get(id).Name.ToLowerInvariant(); }

        /// <summary>Full-screen root: black (also stops every touch reaching the game below) and the castle ruins background covering any screen shape.</summary>
        public static RectTransform Backdrop(Transform owner, string name)
        {
            RectTransform root = UiKit.Fill(UiKit.Canvas(owner).transform, name);
            UiKit.Panel(root, Color.black, true);
            RectTransform bg = UiKit.Fill(root, "Background");
            Image bgImage = bg.gameObject.AddComponent<Image>();
            bgImage.sprite = UiArt.Get("hero-select/bg");
            bgImage.raycastTarget = false;
            AspectRatioFitter fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = bgImage.sprite != null ? bgImage.sprite.rect.width / bgImage.sprite.rect.height : StageW / StageH;
            return root;
        }

        /// <summary>The 1138 x 640 layout area, centred. Children use UiKit.Rect coordinates from its top-left.</summary>
        public static RectTransform Stage(RectTransform root)
        {
            var stage = new GameObject("Stage", typeof(RectTransform)).GetComponent<RectTransform>();
            stage.SetParent(root, false);
            stage.anchorMin = stage.anchorMax = stage.pivot = new Vector2(0.5f, 0.5f);
            stage.sizeDelta = new Vector2(StageW, StageH);
            return stage;
        }

        /// <summary>Shrinks the stage to a narrower screen (never grows it). Returns the scale for the corner pieces.</summary>
        public static float Fit(RectTransform root, RectTransform stage)
        {
            float s = Mathf.Min(1f, root.rect.width / StageW);
            if (s > 0f) stage.localScale = new Vector3(s, s, 1f);
            return Mathf.Max(s, 0.01f);
        }

        public static Image Pic(Transform parent, string name, Sprite art, float x, float y, float w, float h, bool center = false)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, h, center);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = art;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Dark plate with the gold corner ornaments (the hero select's info panel art).</summary>
        public static RectTransform Plate(Transform parent, string name, float x, float y, float w, float h)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, h);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = UiArt.Get("hero-select/panel-frame", new Vector4(PanelSlice, PanelSlice, PanelSlice, PanelSlice));
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = PanelDensity;
            img.raycastTarget = true; // taps on the plate must not reach a pedestal behind it
            return rt;
        }

        /// <summary>A button drawn from one of the blank gem-cut sprites (gold / red / navy) with its label as text.</summary>
        public static Button GemButton(Transform parent, string name, string art, float x, float y, float w, float h, string text, int size, Color labelColor, Action onClick)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, h);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = Art(art);
            Button b = Clickable(rt, img, onClick);
            UiKit.Label(UiKit.Fill(rt, "Label"), text, size, labelColor, TextAnchor.MiddleCenter, true);
            return b;
        }

        /// <summary>Button on any graphic: tint on hover / press / disabled, no keyboard navigation, and Space (attack) never re-presses it.</summary>
        public static Button Clickable(RectTransform rt, Graphic graphic, Action onClick)
        {
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = graphic;
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cb.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);
            b.colors = cb;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.onClick.AddListener(() =>
            {
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                onClick();
            });
            return b;
        }

        /// <summary>Invisible tap area (a hero's column, a tile).</summary>
        public static Button HitArea(Transform parent, string name, float x, float y, float w, float h, Action onClick)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, h);
            Image img = UiKit.Panel(rt, new Color(0f, 0f, 0f, 0f), true);
            Button b = Clickable(rt, img, onClick);
            b.transition = Selectable.Transition.None;
            return b;
        }

        public static Text Title(RectTransform root, string text, out RectTransform rt)
        {
            rt = UiKit.Rect(root, "Title", 0f, 0f, 520f, 62f);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            Image shade = UiKit.Panel(rt, new Color(0f, 0f, 0f, 0.55f)); // the bright stone behind the lettering is unreadable otherwise
            shade.sprite = null;
            Text t = UiKit.Label(UiKit.Fill(rt, "Text", 0f), text, 34, Gold, TextAnchor.MiddleLeft, true);
            t.rectTransform.offsetMin = new Vector2(18f, 0f);
            return t;
        }

        /// <summary>Enter / keypad Enter went down this frame (read like the hero head's H key: legacy input or the Input System).</summary>
        public static bool EnterPressed()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#elif ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
#else
            return false;
#endif
        }
    }
}
