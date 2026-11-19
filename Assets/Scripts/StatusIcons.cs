using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// HUD debuff icons painted by ChatGPT (Tay, chat 6aff091a, 2026-11-19; source Assets/ArtLibrary/ui/status/): the poison drop under the
    /// player's HP bar (HudBuffRow) and on each poisoned teammate of the party bar (PartyHudList). Null when the texture is missing.
    /// </summary>
    public static class StatusIcons
    {
        /// <summary>BuffView-style source id of the poison slot in HudBuffRow (BuffSource packs class &lt;&lt; 3 | slot, so 254 is never a skill).</summary>
        public const byte PoisonSource = 254;
        static Sprite poison;
        static bool loaded;

        public static Sprite Poison
        {
            get
            {
                if (loaded) return poison;
                loaded = true;
                Texture2D tex = Resources.Load<Texture2D>("Sprites/ui/status/poison");
                if (tex != null) poison = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                return poison;
            }
        }
    }
}
