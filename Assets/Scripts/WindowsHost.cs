using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Builds the in-game windows (dialogue, quest offer, chat, the Talk prompt) on the shared UGUI canvas and runs their keyboard rules:
    /// E / Enter = talk to a nearby NPC, advance the dialogue or accept the quest; Enter otherwise focuses the chat; Esc closes the open window.
    /// While a modal window is open the offline sim is paused (like the inventory) and InputAdapter blocks the hero's keys (UiWindows).
    /// </summary>
    [DefaultExecutionOrder(-850)] // after TouchControls latched its buttons, before the hero reads input
    public sealed class WindowsHost : MonoBehaviour
    {
        GameObject talkPrompt;
        Transform root;
        bool pausedByUs;

        public static void Create(Transform parent)
        {
            UiWindows.ResetState();
            ChatHub.ResetState();
            QuestTracker.Init();
            var go = new GameObject("WindowsHost");
            go.transform.SetParent(parent, false);
            var host = go.AddComponent<WindowsHost>();
            host.root = parent;
            host.Build(parent, UiKit.Canvas(parent).transform);
        }

        void Build(Transform root, Transform canvas)
        {
            NpcDialogueBox.Create(canvas);
            QuestOfferWindow.Create(canvas);
            ChatWindow.Create(canvas);
            RectTransform t = UiWinKit.Anchored(canvas, "TalkPrompt", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 0f, 76f, 150f, 42f);
            UiWinKit.ArtButton(t, "Talk", "btn-gold", 0f, 0f, 150f, 42f, Application.isMobilePlatform ? "Talk" : "Talk  [E]", 19, new Color(0.25f, 0.12f, 0.02f), TownNpc.Talk);
            talkPrompt = t.gameObject;
            talkPrompt.SetActive(false);
        }

        void OnDestroy() { SetPaused(false); }

        void SetPaused(bool on)
        {
            if (on == pausedByUs) return;
            pausedByUs = on;
            GameClock.Paused = on && NetMode.UsesLocalTimeBending;
        }

        void Update()
        {
            HeroController hero = GameSession.Hero;
            bool modal = UiWindows.ModalOpen;
            if (hero == null)
            {
                if (modal) { NpcDialogueBox.Close(); QuestOfferWindow.Close(); SkillTreeWindow.Close(); }
                UiWindows.InteractPrompt = false;
                talkPrompt.SetActive(false);
                SetPaused(false);
                return;
            }
            bool menuOpen = InventoryPanel.IsOpen || HeroChangeScreen.IsOpen;
            bool inRange = TownNpc.InRange && !menuOpen;
            UiWindows.InteractPrompt = inRange;
            talkPrompt.SetActive(inRange && !modal && !ChatWindow.RecentlyTyping);
            SetPaused(modal);

            if (InputAdapter.Pressed(GameKey.SkillTree))
            {
                if (HeroChangeScreen.IsOpen) return;
                if (!SkillTreeWindow.IsOpen) InventoryPanel.Close();
                SkillTreeWindow.Toggle(root);
                return;
            }
            bool interact = InputAdapter.Pressed(GameKey.Interact), enter = InputAdapter.Pressed(GameKey.Chat);
            if (InputAdapter.Pressed(GameKey.Close) && modal) { NpcDialogueBox.Close(); QuestOfferWindow.Close(); SkillTreeWindow.Close(); return; }
            if (SkillTreeWindow.IsOpen) return; // E / Enter must not talk or focus the chat behind the tree
            if (!interact && !enter) return;
            if (QuestOfferWindow.IsOpen) QuestOfferWindow.Accept();
            else if (NpcDialogueBox.IsOpen) NpcDialogueBox.Advance();
            else if (inRange && !ChatWindow.RecentlyTyping) TownNpc.Talk();
            else if (enter && !menuOpen) ChatWindow.FocusFromKey();
        }
    }
}
