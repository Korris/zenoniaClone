using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Hand-made HUD art (gold-trimmed dark navy, Assets/Resources/Sprites/ui/hud-*.png): panel plate, bar track, joystick base / knob and the
    /// round button frames. Sprites are built once from the Resources textures; plates carry a 9-slice border so they stretch cleanly.
    /// </summary>
    public static class UiArt
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>Status panel plate (830x278) with ornate corners: 9-sliced, the border keeps the ornaments.</summary>
        public static Sprite Panel() { return Get("hud-panel", new Vector4(70f, 60f, 70f, 60f)); }
        /// <summary>Empty bar track with gold outline (830x93): stretches horizontally only.</summary>
        public static Sprite BarTrack() { return Get("hud-bar", new Vector4(45f, 0f, 45f, 0f)); }
        public static Sprite JoystickBase() { return Get("hud-joystick-base"); }
        public static Sprite JoystickKnob() { return Get("hud-joystick-knob"); }
        public static Sprite AttackFrame() { return Get("hud-attack-frame"); }
        public static Sprite SkillFrame() { return Get("hud-skill-frame"); }
        public static Sprite PotionFrame() { return Get("hud-potion-frame"); }

        // 9-slice borders (native pixels: left, bottom, right, top) of the win-* window pieces; anything not listed is drawn unsliced.
        static readonly Dictionary<string, Vector4> WinBorders = new Dictionary<string, Vector4>
        {
            { "panel", new Vector4(72f, 72f, 72f, 72f) }, { "dialogue", new Vector4(80f, 70f, 80f, 70f) }, { "toast", new Vector4(80f, 60f, 80f, 60f) },
            { "nameplate", new Vector4(85f, 0f, 85f, 0f) }, { "section", new Vector4(28f, 28f, 28f, 28f) }, { "slot", new Vector4(45f, 45f, 45f, 45f) },
            { "btn-gold", new Vector4(50f, 0f, 50f, 0f) }, { "btn-navy", new Vector4(50f, 0f, 50f, 0f) }, { "chat-input", new Vector4(40f, 0f, 40f, 0f) },
        };

        /// <summary>Window piece Resources/Sprites/ui/win-{name}.png (panel, dialogue, toast, nameplate, section, slot, btn-gold, btn-navy, close, tab-active ...), 9-sliced when it has a border.</summary>
        public static Sprite Win(string name)
        {
            Vector4 b;
            WinBorders.TryGetValue(name, out b);
            return Get("win-" + name, b);
        }

        /// <summary>Sprite of Resources/Sprites/ui/{name}.png (null + warning when missing).</summary>
        public static Sprite Get(string name, Vector4 border = default(Vector4))
        {
            Sprite s;
            if (Cache.TryGetValue(name, out s) && s != null) return s;
            var tex = Resources.Load<Texture2D>("Sprites/ui/" + name);
            if (tex == null) { Debug.LogWarning("[ZenoniaProto] missing HUD sprite Sprites/ui/" + name); return null; }
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            Cache[name] = s;
            return s;
        }
    }
}
