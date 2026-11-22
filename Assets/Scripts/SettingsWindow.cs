using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Settings window behind the gear button under the minimap: background music on / off, sound effects on / off (AudioPrefs, per device), other players' pets on / off (PetViewPrefs), every monster's name / HP or only the fought one's (MonsterLabelPrefs),
    /// the 2.5D camera switches (rotate, invert drag, spring back: View25DCameraControl), graphics quality Low / Medium / High (GraphicsQuality), the world render scale Auto / 100 / 85 / 70 % and dynamic resolution (RenderScale; SettingsSegmentRow),
    /// and "Change hero", which closes the window and takes the same path the gear button used to take (HeroChangeScreen.Open: the hero select
    /// offline, the account menu online with an account, a hint on a dev connection). Same win-panel frame and gold / navy art as the quest windows.
    /// Modal (UiWindows.ModalOpen: hero frozen, touch controls hidden); Esc / X close it.
    /// </summary>
    public sealed class SettingsWindow : MonoBehaviour
    {
        const float W = 480f, H = 632f, Pad = 30f, RowH = 44f, Gap = 4f, IconSize = 34f; // 10 rows: music, sound, other pets, 3 camera switches (2.5D maps, 22/11), graphics, resolution, dynamic resolution, all monster labels (22/11); fits the 640-unit canvas
        static readonly Color TitleColor = new Color(1f, 0.93f, 0.7f), DarkText = new Color(0.25f, 0.12f, 0.02f);
        static readonly Color RowFill = new Color(0.035f, 0.065f, 0.13f, 1f) /* opaque: the Outline copies lie behind it */, RowLine = new Color(0.85f, 0.65f, 0.25f, 0.6f);
        static SettingsWindow instance;

        public static bool IsOpen { get { return instance != null && instance.open; } }

        RectTransform root;
        SettingsToggle music, sfx, otherPets, camRotate, camInvert, camSpring, dynRes, monsterLabels;
        SettingsSegmentRow graphics, resolution;
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
            instance.otherPets.Set(PetViewPrefs.ShowOthers);
            instance.camRotate.Set(View25DCameraControl.RotateOn);
            instance.camInvert.Set(View25DCameraControl.Invert);
            instance.camSpring.Set(View25DCameraControl.SpringBack);
            instance.dynRes.Set(RenderScale.Dynamic);
            instance.monsterLabels.Set(MonsterLabelPrefs.ShowAll);
            instance.graphics.Refresh();
            instance.resolution.Refresh();
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
            root = UiWinKit.Anchored(transform, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 0f, W, H);
            UiWinKit.Sliced(root, "panel", W / 639f, true); // swallows taps so they never reach the world
            Image gear = UiKit.Icon(root, "GearIcon", Pad, 18f, 40f);
            gear.sprite = UiArt.Get("hud-settings");
            gear.color = Color.white;
            UiKit.Label(root, "Title", 82f, 16f, 220f, 44f, "Settings", 22, TitleColor, TextAnchor.MiddleLeft, true);
            UiWinKit.CloseButton(root, W - 62f, 21f, 32f, Close);
            UiKit.Panel(UiKit.Rect(root, "Rule", Pad, 64f, W - Pad * 2f, 1.5f), new Color(0.85f, 0.65f, 0.25f, 0.7f));

            music = Row("Music", 76f, "Music", "settings-music", on => AudioPrefs.MusicOn = on);
            sfx = Row("Sound", 76f + RowH + Gap, "Sound effects", "settings-sound", on => AudioPrefs.SfxOn = on);
            otherPets = Row("OtherPets", 76f + (RowH + Gap) * 2f, "Other players' pets", null, on => PetViewPrefs.ShowOthers = on, PetArt.Icon(Rpg.PetConfig.All[0]));
            // 2.5D player camera (View25DCameraControl): tilt / zoom always on; rotation, drag direction and spring back are switches
            camRotate = Row("CamRotate", 76f + (RowH + Gap) * 3f, "Rotate camera", null, on => View25DCameraControl.RotateOn = on, UiArt.Get("hud-settings"));
            camInvert = Row("CamInvert", 76f + (RowH + Gap) * 4f, "Invert camera drag", null, on => View25DCameraControl.Invert = on, UiArt.Get("hud-settings"));
            camSpring = Row("CamSpring", 76f + (RowH + Gap) * 5f, "Camera springs back", null, on => View25DCameraControl.SpringBack = on, UiArt.Get("hud-settings"));
            graphics = Segment("Graphics", 6, "Graphics", new[] { "Low", "Medium", "High" }, 68f, () => (int)GraphicsQuality.Current, i => GraphicsQuality.Set((GraphicsQuality.Level)i));
            // world render scale (RenderScale): the HUD stays native; "Auto" follows the graphics level (High 100 %, Medium 85 %, Low 75 %)
            resolution = Segment("Resolution", 7, "Resolution", RenderScale.Labels, 56f, () => RenderScale.StepIndex, RenderScale.SetStep);
            dynRes = Row("DynamicRes", 76f + (RowH + Gap) * 8f, "Dynamic resolution", null, RenderScale.SetDynamic, UiArt.Get("hud-settings"));
            // off (default): a monster's HP bar + name only for the one you fight / just hit (MonsterLabelPrefs, 22/11)
            monsterLabels = Row("MonsterLabels", 76f + (RowH + Gap) * 9f, "All monster names / HP", null, on => MonsterLabelPrefs.ShowAll = on, UiArt.Get("hud-settings"));

            UiWinKit.ArtButton(root, "ChangeHero", "btn-gold", Pad, H - 26f - 46f, W - Pad * 2f, 46f, "Change hero", 20, DarkText, ChangeHero);
            root.gameObject.SetActive(false);
        }

        /// <summary>One setting: section box, round icon, caption and the on / off switch on the right.</summary>
        SettingsToggle Row(string name, float y, string caption, string icon, Action<bool> changed, Sprite art = null)
        {
            RectTransform row = RowBox(name, y, caption, icon, art, SettingsToggle.Width);
            return new SettingsToggle(row, W - Pad * 2f - 12f - SettingsToggle.Width, (RowH - SettingsToggle.Height) * 0.5f, changed);
        }

        /// <summary>Row with segment tabs (graphics level, render scale) on the right.</summary>
        SettingsSegmentRow Segment(string name, int index, string caption, string[] tabs, float tabW, Func<int> get, Action<int> set)
        {
            float w = SettingsSegmentRow.WidthOf(tabs.Length, tabW);
            RectTransform row = RowBox(name, 76f + (RowH + Gap) * index, caption, null, UiArt.Get("hud-settings"), w);
            return new SettingsSegmentRow(row, W - Pad * 2f - 12f - w, (RowH - SettingsSegmentRow.TabH) * 0.5f, name, tabs, tabW, 14, get, set);
        }

        /// <summary>Section box, round icon and caption of a row; the control (controlW wide) goes on the right.</summary>
        RectTransform RowBox(string name, float y, string caption, string icon, Sprite art, float controlW)
        {
            RectTransform row = UiKit.Rect(root, name, Pad, y, W - Pad * 2f, RowH);
            // plain dark box with a thin gold line, as in the mockup: win-section's light band reads as a stripe (rejected on the Bag screen)
            Outline line = UiKit.Panel(row, RowFill).gameObject.AddComponent<Outline>();
            line.effectColor = RowLine;
            line.effectDistance = new Vector2(1.5f, -1.5f);
            Image img = UiKit.Icon(row, "Icon", 12f, (RowH - IconSize) * 0.5f, IconSize);
            img.sprite = art ?? SettingsToggle.Art(icon);
            img.color = Color.white;
            img.enabled = img.sprite != null;
            UiKit.Label(row, "Caption", 60f, 0f, W - Pad * 2f - 60f - controlW - 18f, RowH, caption, 18, Color.white, TextAnchor.MiddleLeft, true);
            return row;
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
