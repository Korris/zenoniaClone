using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Api;

namespace ZenoniaProto
{
    /// <summary>
    /// The character select screen (RO Soft kit, light), first thing after logging in and, offline, after starting the game: the account's heroes (online, server-api)
    /// or the saves on this device (offline, one per class) as cards on the left, empty cards "+ Create character" up to the six slots, the picked hero big on its
    /// pedestal in the middle (turn left / right, idle / walk / attack), its info on the right with ENTER GAME, CREATE NEW and DELETE (tap, then hold to confirm).
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
        HeroPreviewStage.Motion motion = HeroPreviewStage.Motion.Idle;
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
        /// Offline: the class step of a new hero, classes that already have a save greyed out. start = the class to open on (BACK from the look step);
        /// slot = the empty slot tapped (-1 = keep the one picked before). Picking the class remembers the slot for that class's save.
        /// </summary>
        public static void CreateOffline(HeroClass? start, int slot = -1)
        {
            if (slot >= 0) offlineSlot = slot;
            var taken = new HashSet<int>();
            foreach (CharacterSummary c in OfflineCharacterRoster.Load()) taken.Add(GameSession.ClassIndex(c.classId));
            if (GameSession.Hero != null) taken.Add((int)GameSession.Hero.Class.Id); // the hero being played (a capture run has no save for it)
            HeroSelectScreen.OpenCreate(i => taken.Contains(i), id =>
            {
                OfflineCharacterRoster.SetSlot(RpgSession.ClassId(id), offlineSlot);
                CharacterCustomizeScreen.OpenOffline(id, HeroChangeScreen.IsOpen);
            }, () => { }, start, false);
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
            motion = HeroPreviewStage.Motion.Idle;
            if (m == Mode.Offline) ReloadOffline();
            if (m == Mode.Offline && GameSession.Hero != null) selectedId = RpgSession.ClassId(GameSession.Hero.Class.Id); // Change hero: starts on the one being played
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
            if (h != null && NetMode.IsOffline && Playing(RpgSession.ClassId(h.Class.Id)) && offlineHeroes.Find(c => c.classId == RpgSession.ClassId(h.Class.Id)) == null)
            {
                Rpg.PlayerState p = RpgSession.Player;
                offlineHeroes.Add(new CharacterSummary { id = RpgSession.ClassId(h.Class.Id), name = h.Class.Name, classId = RpgSession.ClassId(h.Class.Id), level = p != null ? p.Level : 1, mapId = MapRegistry.Current.Id, appearance = p != null ? p.Appearance : null, slot = OfflineCharacterRoster.SlotOf(RpgSession.ClassId(h.Class.Id)) });
            }
            offlineVersion++;
        }

        /// <summary>Offline over the game: this class is the hero on the map now.</summary>
        bool Playing(string classId) { return mode == Mode.Offline && GameSession.Hero != null && RpgSession.ClassId(GameSession.Hero.Class.Id) == classId; }

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

        static string ClassName(CharacterSummary c) { HeroClass id; return KnownClass(c, out id) ? HeroClassDef.Get(id).Name : c.classId; }

        /// <summary>Which hero or empty card is on each row, the big hero and the info card.</summary>
        void Redraw()
        {
            dirty = false;
            builtVersion = ListVersion;
            PlaceBySlot();
            if (Selected() == null) { CharacterSummary first = bySlot.Find(c => c != null); selectedId = first != null ? first.id : null; } // deleted or unknown: the first one
            int pages = (bySlot.Count + Rows - 1) / Rows;
            if (page < 0) page = Mathf.Max(0, bySlot.FindIndex(c => c != null && c.id == selectedId)) / Rows;
            page = Mathf.Clamp(page, 0, pages - 1);
            pageUp.gameObject.SetActive(pages > 1);
            pageDown.gameObject.SetActive(pages > 1);
            preview.Clear();
            for (int r = 0; r < Rows; r++) ShowRow(r, page * Rows + r);
            ShowBig(Selected());
        }

        void ShowRow(int r, int index)
        {
            Row row = rows[r];
            CharacterSummary hero = index < bySlot.Count ? bySlot[index] : null;
            bool empty = hero == null && index < AccountFlow.MaxCharacters;
            row.Root.SetActive(hero != null || empty);
            row.Hero.SetActive(hero != null);
            row.Empty.SetActive(empty);
            if (hero == null) { row.Face.sprite = RoSoftSprites.Named("slot-empty"); return; }
            bool picked = hero.id == selectedId;
            row.Face.sprite = RoSoftSprites.Get(picked ? RoSoftSprites.Shape.SectionLocked : RoSoftSprites.Shape.Cell);
            row.Name.text = hero.name;
            row.Info.text = "Lv " + hero.level + "  " + ClassName(hero);
            row.Badge.text = Playing(hero.classId) ? "PLAYING" : hero.online ? "ONLINE" : "";
            HeroClass id;
            row.Mini.enabled = KnownClass(hero, out id);
            if (!row.Mini.enabled) return;
            preview.Set(r, id, Rpg.CharacterAppearanceData.Sanitize(hero.appearance, hero.classId), Rpg.CharacterAppearanceData.SeedFor(hero.name, 0), 1f);
            preview.SetPose(r, 0, HeroPreviewStage.Motion.Idle);
        }

        void ShowBig(CharacterSummary hero)
        {
            HeroClass id;
            bool known = KnownClass(hero, out id);
            bigHero.enabled = known;
            banner.gameObject.SetActive(known);
            pedestal.gameObject.SetActive(hero != null);
            foreach (GameObject g in heroControls) g.SetActive(known);
            infoHero.SetActive(hero != null);
            infoEmpty.SetActive(hero == null);
            if (hero == null) return;
            infoName.text = hero.name;
            infoClass.text = ClassName(hero);
            infoLevel.text = "Level " + hero.level;
            IGameMap map = !string.IsNullOrEmpty(hero.mapId) ? MapRegistry.Get(hero.mapId) : null;
            infoPlace.text = map != null ? map.Name : "Oakhaven Village";
            infoState.text = Playing(hero.classId) ? "Playing now" : hero.online ? "Still in the world: saving ..." : mode == Mode.Online ? "Account hero" : "Saved on this device";
            if (!known) return;
            banner.sprite = UiArt.Get("hero-select/banner-" + CharacterFlowKit.ClassArtName(id));
            preview.Set(BigSlot, id, Rpg.CharacterAppearanceData.Sanitize(hero.appearance, hero.classId), Rpg.CharacterAppearanceData.SeedFor(hero.name, 0), 1f);
            PoseBig();
        }

        void PoseBig()
        {
            preview.SetPose(BigSlot, dir8, motion);
            for (int i = 0; i < tabs.Length; i++)
            {
                bool on = (int)motion == i;
                tabs[i].sprite = RoSoftSprites.Get(on ? RoSoftSprites.Shape.TabOn : RoSoftSprites.Shape.TabOff);
                tabLabels[i].color = on ? Color.white : RoSoftTheme.Ink;
            }
        }

        /// <summary>Every frame: buttons that wait for a running call or the join, the corner buttons of the mode, the counts and the status line.</summary>
        void RefreshState()
        {
            NetClient client = NetClient.Active;
            bool online = mode == Mode.Online;
            bool connecting = online && client != null && client.State == ClientState.Connecting;
            bool busy = online && (AccountFlow.Busy || connecting);
            CharacterSummary hero = Selected();
            bool playing = hero != null && Playing(hero.classId);
            bool full = Heroes.Count >= AccountFlow.MaxCharacters;
            enterButton.interactable = hero != null && !busy && !playing;
            enterLabel.text = playing ? "PLAYING" : "ENTER GAME";
            createButton.interactable = !busy && !full;
            deleteButton.interactable = hero != null && !busy && !playing;
            foreach (Row r in rows) r.Button.interactable = !busy;
            countText.text = "HEROES  " + Heroes.Count + " / " + AccountFlow.MaxCharacters;
            ownerText.text = online ? "Account: " + AccountFlow.Username : "Saved on this device";
            string text = online ? (connecting ? client.Status : AccountFlow.Status) : offlineStatus;
            status.text = text;
            statusPlate.SetActive(!string.IsNullOrEmpty(text));
            status.color = online && AccountFlow.StatusBad && !connecting ? RoSoftTheme.BadText : RoSoftTheme.Ink;
            emptyNote.text = online && AccountFlow.Busy && Heroes.Count == 0 ? "Loading ..." : "No heroes yet.\nTap  + Create character  to make your first one.";
            bool overGame = !online && GameSession.Hero != null;
            ShowCorner(logoutButton, online);
            ShowCorner(offlineButton, online);
            ShowCorner(directButton, online && ConnectScreenGui.DevPageAvailable);
            ShowCorner(onlineButton, !online && !overGame);
            ShowCorner(backButton, overGame);
            LayoutCorner();
            RefreshConfirm(busy);
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
            if (!KnownClass(hero, out id) || Playing(hero.classId) || GameSession.Instance == null) return;
            if (HeroChangeScreen.IsOpen) HeroChangeScreen.Close();
            GameSession.Instance.Spawn(id); // saves the hero being played first, then loads this one's save
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
        void OnMotion(HeroPreviewStage.Motion m) { motion = m; PoseBig(); }
        void OnPage(int step) { page += step; dirty = true; }

        void OnLogout() { AccountFlow.Logout(); }
        void OnPlayOffline() { ConnectScreenGui.LeaveToOffline(GameSession.Instance); }
        void OnPlayOnline() { ConnectScreenGui.Open(); }
        void OnBackToGame() { HeroChangeScreen.Close(); }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
