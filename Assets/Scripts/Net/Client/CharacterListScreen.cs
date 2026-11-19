using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Api;

namespace ZenoniaProto
{
    /// <summary>
    /// The character slot screen (RO Soft Fantasy, mockup docs/reference/character-select-ro-fantasy/mockup/01-slot-screen-chatgpt-1.png), always the first thing after
    /// logging in and, offline, after starting the game: six slot cards along the bottom (the account's heroes from server-api, or the slot saves on this device,
    /// any classes; empty = "+ Create Character"), the picked hero big on its pedestal (turns left / right), its info panel and Create Character / Delete (tap, then hold) / Enter.
    /// An empty slot (or Create Character = the first empty one) leads to the class step and the look / name step; the new hero is stored in that slot.
    /// Only CREATE leads to the class step (HeroSelectScreen create mode) and the look / name step; BACK there returns here. Offline it also opens over the running game
    /// from Settings > Change hero (HeroChangeScreen), with a BACK to the game. It only reads AccountFlow / the offline saves and calls them.
    /// </summary>
    public sealed partial class CharacterListScreen : MonoBehaviour
    {
        public enum Mode { None, Online, Offline }

        static CharacterListScreen instance;
        HeroPreviewStage preview;
        readonly List<CharacterSummary> offlineHeroes = new List<CharacterSummary>();
        readonly List<CharacterSummary> bySlot = new List<CharacterSummary>(); // row order: slot i = row i (pages of six), null = an empty slot
        Mode mode;
        string selectedId, offlineStatus = "";
        int page, builtVersion = -1, offlineVersion, dir8 = 1;
        bool dirty;

        public static bool IsShown { get { return instance != null && instance.root != null && instance.root.gameObject.activeSelf; } }
        public static Mode Shown { get { return IsShown ? instance.mode : Mode.None; } }

        public static CharacterListScreen Create(Transform parent)
        {
            var go = new GameObject("CharacterListScreen");
            go.transform.SetParent(parent, false);
            instance = go.AddComponent<CharacterListScreen>();
            return instance;
        }

        // ---- capture scripts ----
        /// <summary>The picked hero, by name (false when the list has no such hero).</summary>
        public static bool TestSelect(string name)
        {
            if (instance == null) return false;
            foreach (CharacterSummary c in instance.Heroes)
                if (c.name == name) { instance.selectedId = c.id; instance.dirty = true; return true; }
            return false;
        }
        /// <summary>The row (slot) a hero stands in, -1 when not listed.</summary>
        public static int TestSlotOf(string name) { return instance != null ? instance.bySlot.FindIndex(c => c != null && c.name == name) : -1; }
        public static List<CharacterSummary> TestHeroes { get { return instance != null ? instance.Heroes : null; } }
        public static string TestSelectedName { get { CharacterSummary c = instance != null ? instance.Selected() : null; return c != null ? c.name : null; } }
        public static void TestReload() { if (instance != null && instance.mode == Mode.Offline) { instance.ReloadOffline(); instance.dirty = true; } }
        public static bool TestDeleteConfirmOpen { get { return instance != null && instance.confirmRoot != null && instance.confirmRoot.gameObject.activeSelf; } }

        static int offlineSlot = -1; // the empty slot the offline hero being made goes into

        /// <summary>
        /// Offline: the class step of a new hero; every class is open (a device may hold several heroes of one class). start = the class to open on (BACK from the look step);
        /// slot = the empty slot tapped (-1 = keep the one picked before). The look / name step then creates the hero in that slot.
        /// </summary>
        public static void CreateOffline(HeroClass? start, int slot = -1)
        {
            if (slot >= 0) offlineSlot = slot;
            HeroSelectScreen.OpenCreate(i => false, id => CharacterCustomizeScreen.OpenOffline(id, HeroChangeScreen.IsOpen, offlineSlot), () => { }, start, false);
        }

        /// <summary>The heroes by slot: each in the slot it was made in, heroes without one (older saves / server) in the free slots in list order; null = empty.</summary>
        void PlaceBySlot()
        {
            bySlot.Clear();
            List<CharacterSummary> heroes = Heroes;
            int size = AccountFlow.MaxCharacters;
            foreach (CharacterSummary c in heroes) size = Mathf.Max(size, c.slot + 1);
            size = Mathf.Max(size, heroes.Count);
            for (int i = 0; i < size; i++) bySlot.Add(null);
            var loose = new List<CharacterSummary>();
            foreach (CharacterSummary c in heroes)
            {
                if (c.slot >= 0 && bySlot[c.slot] == null) bySlot[c.slot] = c;
                else loose.Add(c);
            }
            foreach (CharacterSummary c in loose) bySlot[bySlot.IndexOf(null)] = c;
        }

        int FirstEmptySlot()
        {
            for (int i = 0; i < Mathf.Min(bySlot.Count, AccountFlow.MaxCharacters); i++) if (bySlot[i] == null) return i;
            return -1;
        }

        /// <summary>Which list is wanted now: the account's after logging in, the device's offline (no hero yet, or Change hero over the game), or none.</summary>
        static Mode Wanted()
        {
            if (ConnectScreenGui.Visible)
                return !ConnectScreenGui.DevPageOpen && GameSession.Hero == null && AccountFlow.Page == AccountPage.Characters ? Mode.Online : Mode.None;
            if (!NetMode.IsOffline || GameSession.Instance == null || HeroSelectScreen.Creating || CharacterCustomizeScreen.OfflineOpen) return Mode.None;
            return GameSession.Hero == null || HeroChangeScreen.IsOpen ? Mode.Offline : Mode.None;
        }

        List<CharacterSummary> Heroes { get { return mode == Mode.Online ? AccountFlow.Characters : offlineHeroes; } }
        int ListVersion { get { return mode == Mode.Online ? AccountFlow.ListVersion : offlineVersion; } }

        void Update()
        {
            Mode want = Wanted();
            if (want != (IsShown ? mode : Mode.None))
            {
                if (want != Mode.None) Open(want);
                else Close();
            }
            if (want == Mode.None) return;
            Fit();
            if (dirty || builtVersion != ListVersion) Redraw();
            RefreshState();
        }

        void Open(Mode m)
        {
            if (root == null) Build();
            mode = m;
            root.SetAsLastSibling();
            root.gameObject.SetActive(true);
            confirmRoot.gameObject.SetActive(false);
            preview.SetActive(true);
            offlineStatus = "";
            dir8 = 1; // the 3/4 front view, standing
            if (m == Mode.Offline) ReloadOffline();
            if (m == Mode.Offline && GameSession.Hero != null) selectedId = PlayingId; // Change hero: starts on the one being played
            page = -1;
            dirty = true;
        }

        void Close()
        {
            if (root == null) return;
            root.gameObject.SetActive(false);
            preview.SetActive(false);
            mode = Mode.None;
        }

        /// <summary>Offline saves again (after opening, a delete); the hero being played is listed even without a save file (capture runs).</summary>
        void ReloadOffline()
        {
            offlineHeroes.Clear();
            offlineHeroes.AddRange(OfflineCharacterRoster.Load());
            HeroController h = GameSession.Hero;
            if (h != null && NetMode.IsOffline && mode == Mode.Offline && offlineHeroes.Find(c => c.id == PlayingId) == null)
            {
                Rpg.PlayerState p = RpgSession.Player;
                offlineHeroes.Add(new CharacterSummary { id = PlayingId, name = RpgSession.CurrentName, classId = RpgSession.ClassId(h.Class.Id), level = p != null ? p.Level : 1, mapId = MapRegistry.Current.Id, appearance = p != null ? p.Appearance : null, slot = RpgSession.CurrentSlot });
            }
            offlineVersion++;
        }

        /// <summary>List id of the offline hero being played: its slot's, or "playing" for a hero without a save (capture runs, no free slot).</summary>
        static string PlayingId { get { return RpgSession.CurrentSlot >= 0 ? OfflineCharacterRoster.IdOf(RpgSession.CurrentSlot) : "playing"; } }

        /// <summary>Offline over the game: this listed hero is the one on the map now.</summary>
        bool Playing(CharacterSummary c) { return c != null && mode == Mode.Offline && GameSession.Hero != null && c.id == PlayingId; }

        CharacterSummary Selected()
        {
            foreach (CharacterSummary c in Heroes) if (c.id == selectedId) return c;
            return null;
        }

        static bool KnownClass(CharacterSummary c, out HeroClass id)
        {
            int i = c != null ? GameSession.ClassIndex(c.classId) : -1;
            bool known = c != null && i >= 0 && i < HeroClassDef.All.Length && RpgSession.ClassId((HeroClass)i) == c.classId;
            id = known ? (HeroClass)i : HeroClass.Warrior;
            return known;
        }

        // ---- buttons ----

        void OnRow(int r)
        {
            int index = page * Rows + r;
            CharacterSummary hero = index < bySlot.Count ? bySlot[index] : null;
            if (hero == null) { CreateIn(index); return; } // an empty slot: the new hero goes into this one
            selectedId = hero.id;
            dirty = true;
        }

        void OnEnter()
        {
            CharacterSummary hero = Selected();
            if (hero == null) return;
            if (mode == Mode.Online) { AccountFlow.Enter(hero); return; }
            HeroClass id;
            int slot = OfflineCharacterRoster.SlotOf(hero.id);
            if (!KnownClass(hero, out id) || Playing(hero) || slot < 0 || GameSession.Instance == null) return;
            if (HeroChangeScreen.IsOpen) HeroChangeScreen.Close();
            if (!GameSession.Instance.EnterSlot(slot)) offlineStatus = "That save can no longer be read."; // saves the hero being played first, then loads this one's save
        }

        void OnCreate() { CreateIn(FirstEmptySlot()); }

        /// <summary>The class step of a new hero for an empty slot (online: class, look and name; offline: class and look).</summary>
        void CreateIn(int slot)
        {
            if (slot < 0 || slot >= AccountFlow.MaxCharacters || Heroes.Count >= AccountFlow.MaxCharacters) return;
            if (mode == Mode.Online) ConnectScreenGui.CreateHero(AccountFlow.Characters, null, slot);
            else CreateOffline(null, slot);
        }

        void OnTurn(int step) { dir8 = ((dir8 + step) % 8 + 8) % 8; PoseBig(); }
        void OnPage(int step) { page += step; dirty = true; }

        void OnLogout() { AccountFlow.Logout(); }
        void OnPlayOffline() { ConnectScreenGui.LeaveToOffline(GameSession.Instance); }
        void OnPlayOnline() { ConnectScreenGui.Open(); }
        void OnBackToGame() { HeroChangeScreen.Close(); }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
