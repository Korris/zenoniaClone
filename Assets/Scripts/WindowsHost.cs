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
        /// <summary>The full-screen layers of the menus built above that wear the light RO Soft Fantasy skin (HUD-side views keep theirs).</summary>
        static readonly string[] MenuLayers =
        {
            "NpcDialogueBox", "QuestOfferWindow", "QuestLogWindow", "PotionShopWindow", "GearShopWindow", "PartyPanel", "PartyPlayerMenu",
            "PartyInviteWindow", "TradeRequestWindow", "TradeWindow", "SettingsWindow", "PetWindow", "PetGachaWindow",
        };

        GameObject talkPrompt;
        Text talkLabel;
        string talkText;
        Transform root;
        bool pausedByUs;

        public static void Create(Transform parent)
        {
            UiWindows.ResetState();
            ChatHub.ResetState();
            var go = new GameObject("WindowsHost");
            go.transform.SetParent(parent, false);
            var host = go.AddComponent<WindowsHost>();
            host.root = parent;
            UiScreens.Root = parent; // the screen switch builds the old bag / skill tree under the game root
            host.Build(parent, UiKit.Canvas(parent).transform);
        }

        void Build(Transform root, Transform canvas)
        {
            NpcDialogueBox.Create(canvas);
            QuestOfferWindow.Create(canvas);
            QuestLogWindow.Create(canvas);
            PotionShopWindow.Create(canvas);
            GearShopWindow.Create(canvas);
            ChatWindow.Create(canvas);
            PartyPanel.Create(canvas);
            PartyHudList.Create(canvas); // new-HUD party list (teammates only); its title opens PartyPanel as the full board
            PartyPlayerMenu.Create(canvas);
            PartyInviteWindow.Create(canvas);
            TradeRequestWindow.Create(canvas);
            TradeWindow.Create(canvas);
            SettingsWindow.Create(canvas);
            PetWindow.Create(canvas);
            PetGachaWindow.Create(canvas);
            CaptureMarker.Create(canvas);
            // RO Soft Fantasy (default look, UiStyle): light menus over the classic builders; nothing happens with --ui-style classic
            foreach (string layer in MenuLayers) RoSoftSkin.Light(canvas.Find(layer));
            RectTransform t = UiWinKit.Anchored(canvas, "TalkPrompt", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 0f, 76f, 150f, 42f);
            UiWinKit.ArtButton(t, "Talk", "btn-gold", 0f, 0f, 150f, 42f, Application.isMobilePlatform ? "Talk" : "Talk  [E]", 19, new Color(0.25f, 0.12f, 0.02f), Interact);
            RoSoftSkin.Light(t); // mint Talk button in the RO Soft Fantasy look
            talkPrompt = t.gameObject;
            talkLabel = talkPrompt.GetComponentInChildren<Text>(true);
            talkPrompt.SetActive(false);
        }

        void OnDestroy() { SetPaused(false); }

        /// <summary>The Talk button / E: the nearest villager, else the chest or warp crystal in reach.</summary>
        static void Interact()
        {
            if (TownNpc.InRange) TownNpc.Talk();
            else DungeonInteract.Use();
        }

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
                if (modal) { NpcDialogueBox.Close(); QuestOfferWindow.Close(); QuestLogWindow.Close(); SkillTreeWindow.Close(); PotionShopWindow.Close(); GearShopWindow.Close(); SettingsWindow.Close(); PetWindow.Close(); PetGachaWindow.Close(); UiWindows.CloseRegistered(); }
                UiWindows.InteractPrompt = false;
                talkPrompt.SetActive(false);
                SetPaused(false);
                return;
            }
            bool menuOpen = InventoryPanel.IsOpen || HeroChangeScreen.IsOpen;
            bool npc = TownNpc.InRange, dungeon = !npc && DungeonInteract.InRange; // a villager first; else a dungeon chest / warp crystal (Rootburrow V5)
            bool inRange = (npc || dungeon) && !menuOpen;
            string text = dungeon ? DungeonInteract.Prompt : Application.isMobilePlatform ? "Talk" : "Talk  [E]";
            if (talkLabel != null && text != talkText) { talkText = text; talkLabel.text = text; }
            UiWindows.InteractPrompt = inRange;
            talkPrompt.SetActive(inRange && !modal && !ChatWindow.RecentlyTyping);
            SetPaused(modal);

            if (InputAdapter.Pressed(GameKey.SkillTree))
            {
                if (HeroChangeScreen.IsOpen || TradeWindow.IsOpen) return;
                if (!UiScreens.IsOpen(UiScreen.Skill)) UiScreens.Close(UiScreen.Bag);
                UiScreens.Toggle(UiScreen.Skill);
                return;
            }
            if (InputAdapter.Pressed(GameKey.PetWindow) && !menuOpen && !TradeWindow.IsOpen && !ChatWindow.RecentlyTyping) { UiScreens.Close(UiScreen.Gacha); UiScreens.Toggle(UiScreen.Pet); return; }
            if (InputAdapter.Pressed(GameKey.PetGacha) && !menuOpen && !TradeWindow.IsOpen && !ChatWindow.RecentlyTyping) { UiScreens.Close(UiScreen.Pet); UiScreens.Toggle(UiScreen.Gacha); return; }
            bool interact = InputAdapter.Pressed(GameKey.Interact), enter = InputAdapter.Pressed(GameKey.Chat);
            if (InputAdapter.Pressed(GameKey.Close) && modal) { NpcDialogueBox.Close(); QuestOfferWindow.Close(); QuestLogWindow.Close(); SkillTreeWindow.Close(); PotionShopWindow.Close(); GearShopWindow.Close(); SettingsWindow.Close(); PetWindow.Close(); PetGachaWindow.Close(); UiWindows.CloseRegistered(); TradeWindow.CloseKey(); return; }
            if (SkillTreeWindow.IsOpen || QuestLogWindow.IsOpen || PotionShopWindow.IsOpen || GearShopWindow.IsOpen || TradeWindow.IsOpen || SettingsWindow.IsOpen || PetWindow.IsOpen || PetGachaWindow.IsOpen || UiWindows.RegisteredOpen) return; // E / Enter must not talk or focus the chat behind the tree
            if (!interact && !enter) return;
            if (QuestOfferWindow.IsOpen) QuestOfferWindow.Accept();
            else if (NpcDialogueBox.IsOpen) NpcDialogueBox.Advance();
            else if (inRange && !ChatWindow.RecentlyTyping) Interact();
            else if (enter && !menuOpen) ChatWindow.FocusFromKey();
        }
    }
}
