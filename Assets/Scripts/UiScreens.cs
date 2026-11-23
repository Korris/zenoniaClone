using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>The screens of the Adventure Paper rebuild. Bag, Skill, Pet, Gacha and Wanted (the board) open as windows; Chat, Hud and Boss only have a look to pick.</summary>
    public enum UiScreen { Bag, Skill, Pet, Gacha, Chat, Hud, Boss, Wanted, Card }

    /// <summary>
    /// One switch per screen between the old window and its Adventure Paper rebuild, and the only place that opens / closes those windows:
    /// keys, HUD buttons, tabs, NPCs and capture flags all call Open / Close / Toggle here, never a window class directly.
    ///   - Every switch is OFF (old screen) until the user approves the rebuilt one; then its phase turns the default on in `useNew` below.
    ///   - A rebuilt screen registers itself once (Register, e.g. from a [RuntimeInitializeOnLoadMethod]) with how to open / close it.
    ///     With the switch on and a view registered the new view opens; otherwise the old window does, so nothing breaks half-way.
    ///   - Hud, Boss and Chat have nothing to open: their builders just read UseNew(...).
    ///   - Capture runs flip switches with --ui new|old and --ui-new bag,skill (DebugCapture.UiRedesign.cs) for A/B screenshots.
    /// Rebuilt windows are modal like the old ones: this class reports them to UiWindows (hero frozen, touch controls hidden, Esc closes).
    /// </summary>
    public static class UiScreens
    {
        sealed class View { public Action Open, Close; public Func<bool> IsOpen; }

        static readonly int Count = Enum.GetValues(typeof(UiScreen)).Length;
        static readonly bool[] useNew = new bool[Count]; // defaults: all old
        static readonly View[] views = new View[Count];
        // Villagers whose talk opens a screen: Lumi, the pet keeper, opens the gacha. A rebuilt screen adds its own with BindNpc.
        static readonly Dictionary<string, UiScreen> npcScreens = new Dictionary<string, UiScreen> { { PetGachaWindow.KeeperNpcId, UiScreen.Gacha } };

        /// <summary>The game root (parent of the lazily built old windows); set by WindowsHost.Create on every scene start.</summary>
        public static Transform Root;

        static UiScreens() { UiWindows.RegisterModal(AnyNewOpen, CloseAllNew); }

        public static bool UseNew(UiScreen s) { return useNew[(int)s]; }
        public static void SetNew(UiScreen s, bool on) { useNew[(int)s] = on; }

        /// <summary>A rebuilt window announces itself. `open` builds the view on first use (the canvas may be new after a map change).</summary>
        public static void Register(UiScreen s, Action open, Action close, Func<bool> isOpen)
        {
            views[(int)s] = new View { Open = open, Close = close, IsOpen = isOpen };
        }

        /// <summary>Talking to this villager opens screen `s` (TownNpc.Talk asks OpenForNpc).</summary>
        public static void BindNpc(string npcId, UiScreen s) { npcScreens[npcId] = s; }

        static View NewView(UiScreen s) { return useNew[(int)s] ? views[(int)s] : null; }

        public static void Open(UiScreen s)
        {
            View v = NewView(s);
            if (v != null) { v.Open(); return; }
            switch (s)
            {
                case UiScreen.Bag: InventoryPanel.Open(Root); break;
                case UiScreen.Skill: SkillTreeWindow.Open(Root); break;
                case UiScreen.Pet: if (PetRoWindow.Wanted) PetRoWindow.Open(); else PetWindow.Open(); break; // RO Soft (default) or the classic window
                case UiScreen.Gacha: PetGachaWindow.Open(); break;
                case UiScreen.Card: CardWindow.Open(); break; // the Monster Card window (My Cards, Album, Soul Gems)
                case UiScreen.Wanted: if (RoHud.Active) WantedBoardWindow.Open(); break; // RO Soft HUD: its compact board (RoWantedBoard) without the paper switch
            }
        }

        /// <summary>Closes the screen whichever look is showing.</summary>
        public static void Close(UiScreen s)
        {
            View v = views[(int)s];
            if (v != null && v.IsOpen()) v.Close();
            switch (s)
            {
                case UiScreen.Bag: InventoryPanel.Close(); break;
                case UiScreen.Skill: SkillTreeWindow.Close(); break;
                case UiScreen.Pet: PetWindow.Close(); PetRoWindow.Close(); break;
                case UiScreen.Gacha: PetGachaWindow.Close(); break;
                case UiScreen.Card: CardWindow.Close(); break;
                case UiScreen.Wanted: RoWantedBoard.Close(); break;
            }
        }

        public static bool IsOpen(UiScreen s)
        {
            View v = views[(int)s];
            if (v != null && v.IsOpen()) return true;
            switch (s)
            {
                case UiScreen.Bag: return InventoryPanel.IsOpen;
                case UiScreen.Skill: return SkillTreeWindow.IsOpen;
                case UiScreen.Pet: return PetWindow.IsOpen || PetRoWindow.IsOpen;
                case UiScreen.Gacha: return PetGachaWindow.IsOpen;
                case UiScreen.Card: return CardWindow.IsOpen;
                default: return false;
            }
        }

        public static void Toggle(UiScreen s) { if (IsOpen(s)) Close(s); else Open(s); }

        /// <summary>
        /// The screen a villager opens, if any. False = not a screen NPC, or its screen has no window to show yet
        /// (the old Wanted board is a dialogue, WantedBossBoard.TryOpen: the caller goes on to it).
        /// </summary>
        public static bool OpenForNpc(string npcId)
        {
            UiScreen s;
            if (!npcScreens.TryGetValue(npcId, out s)) return false;
            if (NewView(s) == null && s != UiScreen.Gacha) return false;
            Open(s);
            return true;
        }

        static bool AnyNewOpen()
        {
            for (int i = 0; i < views.Length; i++) if (views[i] != null && views[i].IsOpen()) return true;
            return false;
        }

        static void CloseAllNew()
        {
            for (int i = 0; i < views.Length; i++) if (views[i] != null && views[i].IsOpen()) views[i].Close();
        }

        /// <summary>--ui new|old (every screen) then --ui-new bag,skill (those on); either may be null.</summary>
        public static void ApplyArgs(string ui, string uiNew)
        {
            if (ui != null) for (int i = 0; i < Count; i++) useNew[i] = ui == "new";
            if (uiNew == null) return;
            foreach (string name in uiNew.Split(','))
            {
                UiScreen s;
                if (Enum.TryParse(name.Trim(), true, out s) && Enum.IsDefined(typeof(UiScreen), s)) useNew[(int)s] = true;
                else Debug.LogWarning("[ZenoniaProto] --ui-new: unknown screen '" + name + "' (bag, skill, pet, gacha, chat, hud, boss, wanted)");
            }
        }
    }
}
