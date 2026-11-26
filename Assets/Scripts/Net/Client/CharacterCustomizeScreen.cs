using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Api;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The appearance step of character creation (docs/character-appearance-layered-spec.md, step E), drawn with uGUI in the RO Soft kit.
    /// After the class is picked the player chooses hair colour (8) and outfit colour (6) from Data/hero-layered/palettes.json (a hairstyle too when
    /// the class has more than one), watching the live layered hero stand idle facing down (breathing, weapon with it). No turn arrows or motion
    /// tabs (user 19/11: only the down sprite). No hat: heroes start bare-headed.
    /// Online (AccountPage.Customize): the look goes with the create request (AccountFlow.PendingLook) and the hero is named here; server refusals
    /// show in the status line. Offline (OpenOffline + the empty slot): the hero is named here too (online rules, and no other hero on this device may have the
    /// name, case-insensitive); CREATE spawns it with the look and name in that slot (GameSession.CreateInSlot), which its save keeps from then on.
    /// A dev host only picks the look (CONFIRM, GameSession.Spawn). Back returns to the class choice on the same class.
    /// </summary>
    public sealed partial class CharacterCustomizeScreen : MonoBehaviour
    {
        const int NameMax = 16;
        const string NameHint = "3-16 letters, digits or _";
        const string OfflineNote = "Your hero keeps this look on every map, from the village to the boss arena.\nHats come later: monsters drop them and the village sells some.";

        static CharacterCustomizeScreen instance;
        static HeroClass? offlineClass;          // offline / host new hero: the class picked on the hero select; null = the online flow (or closed)
        static bool offlineOverGame;             // opened from the in-game change hero screen
        static CharacterAppearanceData offlineLook;
        static int offlineSlot = -1;             // offline: the save slot the new hero goes into
        static string offlineStatus = "";        // offline: why CREATE refused the name
        static readonly System.Random Dice = new System.Random();

        HeroPreviewStage preview;
        bool hintDirty = true;
        int styleCount = 1;
        bool fixedOutfit;                        // the class body keeps its painted outfit (RO Fantasy): the outfit colours are hidden
        CharacterAppearanceData shown;

        public static bool IsShown { get { return instance != null && instance.root != null && instance.root.gameObject.activeSelf; } }
        /// <summary>The offline appearance step is up (the hero select hides meanwhile; class keys do nothing).</summary>
        public static bool OfflineOpen { get { return offlineClass.HasValue; } }

        public static CharacterCustomizeScreen Create(Transform parent)
        {
            var go = new GameObject("CharacterCustomizeScreen");
            go.transform.SetParent(parent, false);
            instance = go.AddComponent<CharacterCustomizeScreen>();
            return instance;
        }

        /// <summary>
        /// Offline / host: the look of a new hero of this class, before it spawns. overGame = from the change hero screen over a running game.
        /// slot = the empty save slot the offline hero goes into (-1 = the first free one); a dev host has no saves and is not named here.
        /// </summary>
        public static void OpenOffline(HeroClass id, bool overGame, int slot = -1)
        {
            offlineClass = id;
            offlineOverGame = overGame;
            offlineSlot = slot;
            offlineStatus = "";
            offlineLook = CharacterAppearanceData.RandomNew(RpgSession.ClassId(id), HeroPreviewStage.HairStyleCount(id), Dice);
        }

        // ---- capture scripts ----
        public static void TestSetName(string name) { if (instance != null && instance.input != null) instance.input.text = name ?? ""; }
        public static void TestPick(int hair, int outfit) { if (instance != null) { instance.OnHair(hair); instance.OnOutfit(outfit); } }
        public static HeroRenderer TestPreviewHero { get { return instance != null && instance.preview != null ? instance.preview.ViewIn(0) : null; } }
        public static HeroPreviewStage TestPreview { get { return instance != null ? instance.preview : null; } }
        public static void TestConfirm() { if (instance != null) instance.Submit(); }
        public static CharacterAppearanceData TestLook { get { return instance != null ? instance.Look : null; } }
        public static bool TestOutfitHidden { get { return instance != null && instance.fixedOutfit; } }
        public static int TestStyleCount { get { return instance != null ? instance.styleCount : 0; } }

        public static string TestStatus { get { return instance != null && instance.status != null ? instance.status.text : null; } }
        public static string TestHint { get { return instance != null && instance.hint != null ? instance.hint.text : null; } }

        static bool IsNameChar(char c) { return HeroNameRules.IsNameChar(c); }

        bool Offline { get { return offlineClass.HasValue; } }
        /// <summary>The hero is named on this screen: online, and offline on this device (a dev host takes its name from the connect screen).</summary>
        bool Naming { get { return !Offline || NetMode.IsOffline; } }

        /// <summary>What is wrong with the typed name: the server's rules, offline also a name another hero on this device has.</summary>
        string NameProblem(string name) { return Offline ? OfflineCharacterRoster.NameProblem(name) : AccountFlow.NameProblem(name); }
        HeroClass Class { get { return Offline ? offlineClass.Value : AccountFlow.CustomizeClass; } }
        CharacterAppearanceData Look
        {
            get { return Offline ? offlineLook : AccountFlow.PendingLook; }
            set { if (Offline) offlineLook = value; else AccountFlow.PendingLook = value; }
        }

        void Update()
        {
            bool online = ConnectScreenGui.Visible && !ConnectScreenGui.DevPageOpen && GameSession.Hero == null && AccountFlow.Page == AccountPage.Customize;
            bool want = Offline || online;
            if (want != IsShown)
            {
                if (want) Open();
                else Close();
            }
            if (!want) return;
            float s = CharacterFlowKit.Fit(root, stage);
            titleRt.localScale = new Vector3(s, s, 1f);
            titleRt.anchoredPosition = new Vector2(20f * s, -14f * s);
            if (shown == null || Look == null || !shown.SameLook(Look)) Refresh();
            RefreshState();
            if (Naming && input.isFocused && CharacterFlowKit.EnterPressed()) Submit();
        }

        void Open()
        {
            if (root == null) Build();
            root.SetAsLastSibling();
            root.gameObject.SetActive(true);
            HeroClass id = Class;
            styleCount = HeroPreviewStage.HairStyleCount(id);
            fixedOutfit = HeroPreviewStage.FixedOutfit(id);
            if (Look == null) Look = CharacterAppearanceData.RandomNew(RpgSession.ClassId(id), styleCount, Dice);
            banner.sprite = UiArt.Get("hero-select-ro/banner-" + CharacterFlowKit.ClassArtName(id)) ?? UiArt.Get("hero-select/banner-" + CharacterFlowKit.ClassArtName(id)); // the class picker's RO Adventure banners (all six classes)
            titleText.text = Naming ? "CUSTOMIZE AND NAME" : "CHOOSE YOUR LOOK";
            createLabel.text = Naming ? "CREATE" : "CONFIRM";
            namePlate.SetActive(Naming);
            notePlate.SetActive(!Naming);
            note.text = HeroClassDef.Get(id).Name.ToUpperInvariant() + "\n\n" + OfflineNote;
            input.text = ""; // a fresh hero: the name is typed (or rolled) anew
            hintDirty = true;
            preview.SetActive(true);
            shown = null;
            Refresh();
        }

        void Close()
        {
            if (root == null) return;
            root.gameObject.SetActive(false);
            preview.SetActive(false);
        }

        /// <summary>Shows the current look: swatch frames and names, hairstyle row, and the hero on the pedestal. Keeps the look inside what the class has.</summary>
        void Refresh()
        {
            CharacterAppearanceData look = Look;
            int style = Mathf.Clamp(look.hairStyle, 0, styleCount - 1);
            if (style != look.hairStyle) { look = look.Clone(); look.hairStyle = style; Look = look; }
            shown = look.Clone();
            for (int i = 0; i < hairSwatches.Length; i++) PaintSwatch(hairSwatches[i], HeroPalettes.Hair(i), i == look.hairColorIndex);
            for (int i = 0; i < outfitSwatches.Length; i++) PaintSwatch(outfitSwatches[i], HeroPalettes.Outfit(i), i == look.outfitColorIndex);
            hairName.text = Capitalized(HeroPalettes.HairName(look.hairColorIndex));
            outfitName.text = Capitalized(HeroPalettes.OutfitName(look.outfitColorIndex));
            styleRow.SetActive(styleCount > 1);
            foreach (GameObject g in outfitParts) g.SetActive(!fixedOutfit);
            PlaceStyleRow(fixedOutfit);
            styleText.text = "Hairstyle " + (style + 1) + " / " + styleCount;
            preview.Set(0, Class, look, "", 1f);
            preview.SetPose(0, 0); // idle facing down
        }

        static void PaintSwatch(Swatch s, Color32 c, bool picked)
        {
            s.Frame.sprite = RoSoftSprites.Get(picked ? RoSoftSprites.Shape.SectionLocked : RoSoftSprites.Shape.Cell); // kit slot / gold-rimmed selected slot
            s.Disc.color = c;
        }

        static string Capitalized(string s) { return string.IsNullOrEmpty(s) ? "" : char.ToUpperInvariant(s[0]) + s.Substring(1); }

        /// <summary>Every frame: buttons wait for a running call or join; the status line; the hint turns into the rule that is broken.</summary>
        void RefreshState()
        {
            NetClient client = NetClient.Active;
            bool connecting = !Offline && client != null && client.State == ClientState.Connecting;
            bool busy = !Offline && (AccountFlow.Busy || connecting);
            createButton.interactable = backButton.interactable = diceButton.interactable = input.interactable = randomButton.interactable = !busy;
            status.text = Offline ? (offlineStatus == hint.text ? "" : offlineStatus) : connecting ? client.Status : AccountFlow.Status; // offline: the hint under the name already says it
            status.color = Offline ? CharacterFlowKit.Bad : AccountFlow.StatusBad && !connecting ? CharacterFlowKit.Bad : CharacterFlowKit.Soft;
            if (!hintDirty) return;
            hintDirty = false;
            string problem = input.text.Length > 0 ? NameProblem(input.text) : null;
            hint.text = problem ?? NameHint;
            hint.color = problem != null ? RoSoftTheme.BadText : RoSoftTheme.InkSoft;
        }

        void Change(System.Action<CharacterAppearanceData> edit)
        {
            CharacterAppearanceData look = (Look ?? CharacterAppearanceData.RandomNew(RpgSession.ClassId(Class), styleCount, Dice)).Clone();
            edit(look);
            Look = look;
        }

        void OnHair(int i) { Change(l => l.hairColorIndex = Mathf.Clamp(i, 0, CharacterAppearanceData.HairColorCount - 1)); }
        void OnOutfit(int i) { Change(l => l.outfitColorIndex = Mathf.Clamp(i, 0, CharacterAppearanceData.OutfitColorCount - 1)); }
        void OnStyle(int step) { Change(l => l.hairStyle = ((l.hairStyle + step) % styleCount + styleCount) % styleCount); }
        void OnRandom() { Look = CharacterAppearanceData.RandomNew(RpgSession.ClassId(Class), styleCount, Dice); }
        void OnDice()
        {
            string name = AccountFlow.RandomName();
            for (int i = 0; i < 10 && Offline && NameProblem(name) != null; i++) name = AccountFlow.RandomName(); // offline: one no hero on this device has
            input.text = name;
        }

        void OnBack()
        {
            if (Offline)
            {
                HeroClass id = offlineClass.Value;
                offlineClass = null;
                if (NetMode.IsOffline) CharacterListScreen.CreateOffline(id); // the class step of the new hero again, on the same class (a dev host's hero select comes back by itself)
                return;
            }
            if (AccountFlow.Busy) return;
            ConnectScreenGui.CreateHero(AccountFlow.Characters, AccountFlow.CustomizeClass); // Choose Class again, on the same class
        }

        void Submit()
        {
            CharacterAppearanceData look = Look;
            if (Offline)
            {
                if (GameSession.Instance == null) return;
                HeroClass id = offlineClass.Value;
                string name = input.text.Trim();
                int slot = SaveStore.IsSlot(offlineSlot) ? offlineSlot : SaveStore.FirstFreeSlot();
                if (Naming)
                {
                    string problem = NameProblem(name);
                    if (problem == null && SaveStore.Enabled && (slot < 0 || SaveStore.Load(slot) != null)) problem = "No free slot left: delete a hero first";
                    if (problem != null) { offlineStatus = problem; hintDirty = true; return; }
                }
                offlineClass = null;
                if (offlineOverGame) HeroChangeScreen.Close();
                if (Naming) GameSession.Instance.CreateInSlot(slot, id, look, name);
                else GameSession.Instance.Spawn(id, look); // dev host: no saves, no name here
                return;
            }
            if (AccountFlow.Busy) return;
            AccountFlow.Create(input.text.Trim(), AccountFlow.CustomizeClass, look);
        }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
