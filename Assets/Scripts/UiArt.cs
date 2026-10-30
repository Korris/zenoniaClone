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
