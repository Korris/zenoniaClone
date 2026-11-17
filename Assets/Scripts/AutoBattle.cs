using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Auto-battle V1 (docs/reference/auto-battle-midgard-senki-chatgpt.md, user OK 2026-11-17, MP kept as it is): V toggles it; the hero then fights
    /// the monsters around the spot where it was switched on (patrol radius 7 tiles), uses its magazine skills, drinks potions and picks up loot,
    /// and walks back to that spot when the area is clear. It drives the hero through the same keys a player presses (InputAdapter asks Held /
    /// Pressed), so online the server still checks every move, cast and pickup. Bosses are never hunted; a boss the player was already hitting
    /// when Auto was switched on stays the target. Moving by hand, dying or changing map switches it off.
    /// Decisions: AutoBattle.Brain.cs.
    /// </summary>
    public static partial class AutoBattle
    {
        const float BossRecentSeconds = 5f;

        public static bool On { get; private set; }
        static Vector2 origin;
        static string mapId;
        static Monster allowedBoss;

        /// <summary>Called every frame by RpgSession for the local hero: the V key, and the safety stops.</summary>
        public static void Tick(HeroController hero)
        {
            if (hero == null) { if (On) Stop(null); return; }
            if (InputAdapter.Pressed(GameKey.AutoToggle)) { if (On) Stop("Auto OFF"); else Start(hero); return; }
            if (!On) return;
            if (hero.IsDead) Stop("Auto OFF");
            else if (MapRegistry.Current == null || MapRegistry.Current.Id != mapId) Stop("Auto OFF");
            else if (InputAdapter.ManualMoveHeld()) Stop("Auto OFF (manual move)");
        }

        /// <summary>Same as pressing V (capture scripts: --skill-ui "2=auto").</summary>
        public static void Toggle(HeroController hero) { if (On) Stop("Auto OFF"); else if (hero != null) Start(hero); }

        static void Start(HeroController hero)
        {
            if (hero.IsDead || MapRegistry.Current == null) return;
            On = true;
            origin = hero.Position;
            mapId = MapRegistry.Current.Id;
            Monster last = Monster.LastHitByMe;
            allowedBoss = last != null && !last.IsDying && (last.IsBoss || last.IsWanted) && Time.time - Monster.LastHitByMeAt < BossRecentSeconds ? last : null;
            ResetBrain();
            RpgHud.Toast(allowedBoss != null ? "Auto ON (boss)" : "Auto ON", new Color(0.55f, 1f, 0.6f));
        }

        static void Stop(string toast)
        {
            On = false;
            allowedBoss = null;
            if (toast != null) RpgHud.Toast(toast, new Color(1f, 0.85f, 0.5f));
        }

        /// <summary>Movement / attack keys the Auto holds this frame (false while off).</summary>
        public static bool Held(GameKey k)
        {
            if (!On) return false;
            Think();
            switch (k)
            {
                case GameKey.Left: return move.x < 0f;
                case GameKey.Right: return move.x > 0f;
                case GameKey.Down: return move.y < 0f;
                case GameKey.Up: return move.y > 0f;
                case GameKey.Attack: return attack;
                default: return false;
            }
        }

        /// <summary>The one key the Auto taps this frame (a skill, a potion, the attack), the same for every reader of this frame.</summary>
        public static bool Pressed(GameKey k)
        {
            if (!On) return false;
            Think();
            return (k == GameKey.Attack && attack) || (tap != GameKey.AutoToggle && k == tap);
        }

        /// <summary>Small "AUTO" badge while it runs (called from RpgSession.OnGUI).</summary>
        public static void DrawBadge()
        {
            if (!On || Event.current.type != EventType.Repaint) return;
            float px = Screen.height / 720f, w = 74f * px, h = 24f * px;
            var r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.16f, w, h);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(0.55f, 1f, 0.6f, 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.5f)));
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(15f * px) };
            GUI.Label(r, "AUTO", style);
            GUI.color = Color.white;
        }
    }
}
