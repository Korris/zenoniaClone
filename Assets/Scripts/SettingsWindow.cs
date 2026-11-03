using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Settings window behind the gear button under the minimap: background music on / off, sound effects on / off (AudioPrefs, per device),
    /// and "Change hero", which closes the window and takes the same path the gear button used to take (HeroChangeScreen.Open: the hero select
    /// offline, the account menu online with an account, a hint on a dev connection). Same win-panel frame and gold / navy art as the quest windows.
    /// Modal (UiWindows.ModalOpen: hero frozen, touch controls hidden); Esc / X close it.
    /// </summary>
    public sealed class SettingsWindow : MonoBehaviour
    {
        const float W = 400f, H = 300f, Pad = 30f, RowH = 62f;
        static readonly Color TitleColor = new Color(1f, 0.93f, 0.7f), DarkText = new Color(0.25f, 0.12f, 0.02f);
        static readonly Color RowFill = new Color(0.035f, 0.065f, 0.13f, 1f) /* opaque: the Outline copies lie behind it */, RowLine = new Color(0.85f, 0.65f, 0.25f, 0.6f);
        static SettingsWindow instance;

        public static bool IsOpen { get { return instance != null && instance.open; } }

        RectTransform root;
        SettingsToggle music, sfx;
        bool open, changeHeroNextFrame;

        public static void Create(Transform canvas)
        {
            instance = UiKit.Fill(canvas, "SettingsWindow").gameObject.AddComponent<SettingsWindow>(); // full-screen layer: children anchor to the real screen
            instance.Build();
        }

        void OnDestroy() { if (instance == this) instance = null; }

        /// <summary>The gear button: opens the window (nothing while the hero change screen or the account menu is already up).</summary>
        public static void Open()
        {
            if (instance == null || GameSession.Hero == null || HeroChangeScreen.IsOpen || AccountScreenGui.MenuOpen) return;
            instance.music.Set(AudioPrefs.MusicOn);
            instance.sfx.Set(AudioPrefs.SfxOn);
            instance.open = true;
            instance.root.gameObject.SetActive(true);
        }

        /// <summary>Capture runs (DebugCapture --settings-show): draws the switches in these states without changing or saving the player's sound settings.</summary>
        public static void TestShow(bool musicOn, bool sfxOn)
        {
            if (instance == null) return;
            instance.music.Set(musicOn);
            instance.sfx.Set(sfxOn);
        }

        public static void Close()
        {
            if (instance == null) return;
            instance.open = false;
            instance.root.gameObject.SetActive(false);
        }

        void Build()
        {
            root = UiWinKit.Anchored(transform, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 8f, W, H);
            UiWinKit.Sliced(root, "panel", W / 639f, true); // swallows taps so they never reach the world
            Image gear = UiKit.Icon(root, "GearIcon", Pad, 18f, 40f);
            gear.sprite = UiArt.Get("hud-settings");
            gear.color = Color.white;
            UiKit.Label(root, "Title", 82f, 16f, 220f, 44f, "Settings", 22, TitleColor, TextAnchor.MiddleLeft, true);
            UiWinKit.CloseButton(root, W - 62f, 21f, 32f, Close);
            UiKit.Panel(UiKit.Rect(root, "Rule", Pad, 64f, W - Pad * 2f, 1.5f), new Color(0.85f, 0.65f, 0.25f, 0.7f));

            music = Row("Music", 76f, "Music", "settings-music", on => AudioPrefs.MusicOn = on);
            sfx = Row("Sound", 76f + RowH + 8f, "Sound effects", "settings-sound", on => AudioPrefs.SfxOn = on);

            UiWinKit.ArtButton(root, "ChangeHero", "btn-gold", Pad, H - 26f - 46f, W - Pad * 2f, 46f, "Change hero", 20, DarkText, ChangeHero);
            root.gameObject.SetActive(false);
        }

        /// <summary>One setting: section box, round icon, caption and the on / off switch on the right.</summary>
        SettingsToggle Row(string name, float y, string caption, string icon, Action<bool> changed)
        {
            RectTransform row = UiKit.Rect(root, name, Pad, y, W - Pad * 2f, RowH);
            // plain dark box with a thin gold line, as in the mockup: win-section's light band reads as a stripe (rejected on the Bag screen)
            Outline line = UiKit.Panel(row, RowFill).gameObject.AddComponent<Outline>();
            line.effectColor = RowLine;
            line.effectDistance = new Vector2(1.5f, -1.5f);
            Image img = UiKit.Icon(row, "Icon", 12f, (RowH - 42f) * 0.5f, 42f);
            img.sprite = SettingsToggle.Art(icon);
            img.color = Color.white;
            img.enabled = img.sprite != null;
            UiKit.Label(row, "Caption", 66f, 0f, W - Pad * 2f - 66f - SettingsToggle.Width - 18f, RowH, caption, 18, Color.white, TextAnchor.MiddleLeft, true);
            return new SettingsToggle(row, W - Pad * 2f - 12f - SettingsToggle.Width, (RowH - SettingsToggle.Height) * 0.5f, changed);
        }

        /// <summary>Closes first, then opens the hero change path one frame later: WindowsHost (earlier in the frame) has released the modal pause by then, so it cannot undo the pause HeroChangeScreen sets.</summary>
        void ChangeHero()
        {
            Close();
            changeHeroNextFrame = true;
        }

        void Update()
        {
            if (changeHeroNextFrame) { changeHeroNextFrame = false; HeroChangeScreen.Open(); }
            if (open && (GameSession.Hero == null || InventoryPanel.IsOpen)) Close();
        }
    }
}
