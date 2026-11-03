using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// On / off switch of the Settings window: one tap flips it. Drawn with the switch art of Resources/Sprites/ui/settings/ when it is there
    /// (settings-toggle-on / -off, with the state written on it), else with the window kit's gold (on) / navy (off) buttons and an ON / OFF caption.
    /// </summary>
    public sealed class SettingsToggle
    {
        public const float Width = 112f, Height = 38f; // the switch art is 281 x 96 (aspect 2.93), the mockup draws it about 112 x 38
        static readonly Color OnText = new Color(0.25f, 0.12f, 0.02f);

        readonly Image img;
        readonly Text caption;
        readonly Action<bool> changed;
        readonly Sprite onArt, offArt;
        bool on;

        public SettingsToggle(Transform parent, float x, float y, Action<bool> changed)
        {
            this.changed = changed;
            RectTransform rt = UiKit.Rect(parent, "Toggle", x, y, Width, Height);
            onArt = Art("settings-toggle-on");
            offArt = Art("settings-toggle-off");
            bool switchArt = onArt != null && offArt != null;
            if (!switchArt) { onArt = UiArt.Win("btn-gold"); offArt = UiArt.Win("btn-navy"); }
            img = rt.gameObject.AddComponent<Image>();
            img.sprite = onArt;
            if (switchArt) img.preserveAspect = true;
            else if (onArt != null) { img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = onArt.rect.height / Height; }
            img.color = Color.white; // a tint would multiply into the art (the dark Start button in docs/sprite-weapon-rules.md)
            if (!switchArt) caption = UiKit.Label(UiKit.Fill(rt, "State"), "", 17, OnText, TextAnchor.MiddleCenter, true);
            UiWinKit.MakeButton(img, Flip);
        }

        /// <summary>Settings art Resources/Sprites/ui/settings/{name}.png, or null when it is not there (no warning: the fallback is fine).</summary>
        public static Sprite Art(string name)
        {
            var tex = Resources.Load<Texture2D>("Sprites/ui/settings/" + name);
            return tex == null ? null : Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Shows a state without reporting it (the window reads AudioPrefs on open).</summary>
        public void Set(bool value)
        {
            on = value;
            img.sprite = on ? onArt : offArt;
            if (img.type == Image.Type.Sliced && img.sprite != null) img.pixelsPerUnitMultiplier = img.sprite.rect.height / Height;
            if (caption != null) { caption.text = on ? "ON" : "OFF"; caption.color = on ? OnText : UiWinKit.Cream; }
        }

        void Flip()
        {
            Set(!on);
            changed(on);
        }
    }
}
