using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// "Change hero" screen over the running game, opened by the Settings button: reuses ClassSelectGui (in-game mode with a Back button),
    /// picking another class respawns like F1-F5 (GameSession.Spawn saves the current hero first). Esc closes. Offline only: online the class
    /// is fixed for the session. The simulation is frozen while it is open (same pause as the inventory).
    /// </summary>
    public sealed class HeroChangeScreen : MonoBehaviour
    {
        static HeroChangeScreen instance;

        /// <summary>True while the screen is up (touch controls hide, the game is paused).</summary>
        public static bool IsOpen { get { return instance != null && instance.open; } }

        bool open;

        public static void Create(Transform parent)
        {
            var go = new GameObject("HeroChangeScreen");
            go.transform.SetParent(parent, false);
            instance = go.AddComponent<HeroChangeScreen>();
        }

        /// <summary>Opens the screen; online it only shows a hint because the hero cannot change there.</summary>
        public static void Open()
        {
            if (instance == null || GameSession.Hero == null || IsOpen) return;
            if (!NetMode.IsOffline) { RpgHud.Toast("Hero cannot be changed while online", new Color(1f, 0.85f, 0.5f)); return; }
            instance.open = true;
            GameClock.Paused = NetMode.UsesLocalTimeBending;
        }

        public static void Close()
        {
            if (instance == null || !instance.open) return;
            instance.open = false;
            GameClock.Paused = false;
        }

        void OnDestroy()
        {
            if (instance != this) return;
            Close();
            instance = null;
        }

        void Update()
        {
            if (!open) return;
            if (GameSession.Hero == null || InventoryPanel.IsOpen || InputAdapter.Pressed(GameKey.Close)) Close();
        }

        void OnGUI()
        {
            if (!open) return;
            int picked = ClassSelectGui.Draw(true);
            if (picked == -1) return;
            Close();
            if (picked >= 0 && GameSession.Instance != null && (int)GameSession.Hero.Class.Id != picked)
                GameSession.Instance.Spawn((HeroClass)picked);
        }
    }
}
