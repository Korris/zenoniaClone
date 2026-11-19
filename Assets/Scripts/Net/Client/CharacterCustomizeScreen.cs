using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Api;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The appearance step of character creation (docs/character-appearance-layered-spec.md, step E), drawn with uGUI in the RO Soft kit.
    /// After the class is picked the player chooses hair colour (8) and outfit colour (6) from Data/hero-layered/palettes.json (a hairstyle too when
    /// the class has more than one), watching the live layered hero turn through 8 directions and idle / walk / attack. No hat: heroes start bare-headed.
    /// Online (AccountPage.Customize): the look goes with the create request (AccountFlow.PendingLook) and the hero is named here; server refusals
    /// show in the status line. Offline / host (OpenOffline, a class without a save): Confirm spawns the new hero with the look (GameSession.Spawn),
    /// which its save keeps from then on. Back returns to the class choice on the same class.
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
        static readonly System.Random Dice = new System.Random();

        HeroPreviewStage preview;
        bool hintDirty = true;
        int dir8 = 1, styleCount = 1;            // starts on the 3/4 front view (down-right)
        bool fixedOutfit;                        // the class body keeps its painted outfit (RO Fantasy): the outfit colours are hidden
        HeroPreviewStage.Motion motion = HeroPreviewStage.Motion.Idle;
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

        /// <summary>Offline / host: the look of a new hero of this class, before it spawns. overGame = from the change hero screen over a running game.</summary>
        public static void OpenOffline(HeroClass id, bool overGame)
        {
            offlineClass = id;
            offlineOverGame = overGame;
            offlineLook = CharacterAppearanceData.RandomNew(RpgSession.ClassId(id), HeroPreviewStage.HairStyleCount(id), Dice);
        }

        // ---- capture scripts ----
        public static void TestSetName(string name) { if (instance != null && instance.input != null) instance.input.text = name ?? ""; }
        public static void TestPick(int hair, int outfit) { if (instance != null) { instance.OnHair(hair); instance.OnOutfit(outfit); } }
        public static void TestPose(int dir8, HeroPreviewStage.Motion m) { if (instance != null) { instance.dir8 = dir8; instance.motion = m; instance.shown = null; } }
        public static void TestConfirm() { if (instance != null) instance.Submit(); }
        public static CharacterAppearanceData TestLook { get { return instance != null ? instance.Look : null; } }
        public static bool TestOutfitHidden { get { return instance != null && instance.fixedOutfit; } }
        public static int TestStyleCount { get { return instance != null ? instance.styleCount : 0; } }

        static bool IsNameChar(char c) { return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_'; }

        bool Offline { get { return offlineClass.HasValue; } }
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
            if (!Offline && input.isFocused && CharacterFlowKit.EnterPressed()) Submit();
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
            banner.sprite = UiArt.Get("hero-select/banner-" + CharacterFlowKit.ClassArtName(id));
            titleText.text = Offline ? "CHOOSE YOUR LOOK" : "CUSTOMIZE AND NAME";
            createLabel.text = Offline ? "CONFIRM" : "CREATE";
            namePlate.SetActive(!Offline);
            notePlate.SetActive(Offline);
            note.text = HeroClassDef.Get(id).Name.ToUpperInvariant() + "\n\n" + OfflineNote;
            input.text = ""; // a fresh hero: the name is typed (or rolled) anew
            hintDirty = true;
            dir8 = 1;
            motion = HeroPreviewStage.Motion.Idle;
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

        /// <summary>Shows the current look: swatch frames and names, hairstyle row, motion tabs, and the hero on the pedestal. Keeps the look inside what the class has.</summary>
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
            for (int i = 0; i < tabs.Length; i++)
            {
                bool on = (int)motion == i;
                tabs[i].sprite = RoSoftSprites.Get(on ? RoSoftSprites.Shape.TabOn : RoSoftSprites.Shape.TabOff);
                tabLabels[i].color = on ? Color.white : RoSoftTheme.Ink;
            }
            preview.Set(0, Class, look, "", 1f);
            preview.SetPose(0, dir8, motion);
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
            status.text = Offline ? "" : connecting ? client.Status : AccountFlow.Status;
            status.color = !Offline && AccountFlow.StatusBad && !connecting ? CharacterFlowKit.Bad : CharacterFlowKit.Soft;
            if (!hintDirty) return;
            hintDirty = false;
            string problem = input.text.Length > 0 ? AccountFlow.NameProblem(input.text) : null;
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
        void OnTurn(int step) { dir8 = ((dir8 + step) % 8 + 8) % 8; shown = null; }
        void OnMotion(HeroPreviewStage.Motion m) { motion = m; shown = null; }
        void OnDice() { input.text = AccountFlow.RandomName(); }

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
                offlineClass = null;
                if (offlineOverGame) HeroChangeScreen.Close();
                GameSession.Instance.Spawn(id, look);
                return;
            }
            if (AccountFlow.Busy) return;
            AccountFlow.Create(input.text.Trim(), AccountFlow.CustomizeClass, look);
        }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
