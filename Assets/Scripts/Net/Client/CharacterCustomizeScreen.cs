using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Api;

namespace ZenoniaProto
{
    /// <summary>
    /// Customize and Name (AccountPage.Customize), drawn with uGUI: pick the hairstyle and hair colour of the new hero (shown live on a full-body preview), type or roll a name, then CREATE.
    /// The picks live in AccountFlow.PendingStyle / PendingDye, which AccountFlow.Create stores with the new character's id. A class without layered hair has no colour row; a class without
    /// head art has no hairstyles at all (a note says so). Server refusals (name_taken, class_taken) show in the status line and the typed name stays. Everything else is AccountFlow.
    /// </summary>
    public sealed partial class CharacterCustomizeScreen : MonoBehaviour
    {
        const int NameMax = 16;
        const string NameHint = "3-16 letters, digits or _";

        static CharacterCustomizeScreen instance;
        HeroPreviewStage preview;
        HairPreviewSprites.Look look; // null = the class has no head art
        bool hintDirty = true;
        int shownStyle = int.MinValue, shownDye = int.MinValue;

        public static bool IsShown { get { return instance != null && instance.root != null && instance.root.gameObject.activeSelf; } }

        public static CharacterCustomizeScreen Create(Transform parent)
        {
            var go = new GameObject("CharacterCustomizeScreen");
            go.transform.SetParent(parent, false);
            instance = go.AddComponent<CharacterCustomizeScreen>();
            return instance;
        }

        /// <summary>Capture scripts: the name typed in the field.</summary>
        public static void TestSetName(string name) { if (instance != null && instance.input != null) instance.input.text = name ?? ""; }

        static bool IsNameChar(char c) { return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_'; }

        void Update()
        {
            bool want = ConnectScreenGui.Visible && !ConnectScreenGui.DevPageOpen && GameSession.Hero == null && AccountFlow.Page == AccountPage.Customize;
            bool shown = IsShown;
            if (want != shown)
            {
                if (want) Open();
                else Close();
            }
            if (!want) return;
            float s = CharacterFlowKit.Fit(root, stage);
            titleRt.localScale = new Vector3(s, s, 1f);
            titleRt.anchoredPosition = new Vector2(20f * s, -14f * s);
            if (AccountFlow.PendingStyle != shownStyle || AccountFlow.PendingDye != shownDye) Refresh();
            RefreshState();
            if (input.isFocused && CharacterFlowKit.EnterPressed()) Submit();
        }

        void Open()
        {
            if (root == null) Build();
            root.SetAsLastSibling();
            root.gameObject.SetActive(true);
            HeroClass id = AccountFlow.CustomizeClass;
            look = HairPreviewSprites.For(id);
            if (look != null) FitTiles();
            banner.sprite = UiArt.Get("hero-select/banner-" + CharacterFlowKit.ClassArtName(id));
            input.text = ""; // a fresh hero: the name is typed (or rolled) anew
            hintDirty = true;
            preview.SetActive(true);
            shownStyle = shownDye = int.MinValue;
            Refresh();
        }

        void Close()
        {
            if (root == null) return;
            root.gameObject.SetActive(false);
            preview.SetActive(false);
        }

        /// <summary>Crops every tile around the head: the cell (a whole 192 frame) is scaled so the head's bounds fill the tile and shifted so their centre sits in its middle.</summary>
        void FitTiles()
        {
            float iw = TileW - 2f * TileInset, ih = TileH - 2f * TileInset;
            Rect h = look.Head;
            float size = GameConfig.FramePx * 0.92f * Mathf.Min(iw / (h.width * GameConfig.FramePx), ih / (h.height * GameConfig.FramePx));
            var pos = new Vector2(iw * 0.5f - (h.x + h.width * 0.5f) * size, -(ih * 0.5f - (1f - (h.y + h.height * 0.5f)) * size));
            foreach (Tile t in tiles)
            {
                t.Cell.sizeDelta = new Vector2(size, size);
                t.Cell.anchoredPosition = pos;
            }
        }

        /// <summary>Shows the current picks: tile art and frames, swatches, notes, and the hero on the pedestal. Also keeps the picks inside what the class has.</summary>
        void Refresh()
        {
            HeroClass id = AccountFlow.CustomizeClass;
            int style = 0, dye = CharacterLookPrefs.DefaultDye;
            if (look != null)
            {
                style = Mathf.Clamp(AccountFlow.PendingStyle, 0, look.StyleCount - 1);
                dye = look.Dyeable ? Mathf.Clamp(AccountFlow.PendingDye, -1, look.Presets.Length - 1) : CharacterLookPrefs.DefaultDye;
            }
            AccountFlow.PendingStyle = shownStyle = style;
            AccountFlow.PendingDye = shownDye = dye;

            for (int i = 0; i < tiles.Length; i++)
            {
                bool on = look != null && i < look.StyleCount;
                tiles[i].Frame.gameObject.SetActive(on);
                if (!on) continue;
                tiles[i].Frame.sprite = CharacterFlowKit.Art(i == style ? "slot-selected" : "slot");
                Sprite[] layers = HairPreviewSprites.Layers(look, i, dye);
                for (int l = 0; l < tiles[i].Layers.Length; l++)
                {
                    Sprite sp = l < layers.Length ? layers[l] : null;
                    tiles[i].Layers[l].sprite = sp;
                    tiles[i].Layers[l].enabled = sp != null;
                }
            }
            int count = look != null && look.Dyeable ? look.Presets.Length + 1 : 0;
            for (int i = 0; i < swatches.Length; i++)
            {
                bool on = i < count;
                swatches[i].Disc.gameObject.SetActive(on);
                swatches[i].Ring.gameObject.SetActive(on && i - 1 == dye);
                if (on) swatches[i].Disc.color = HairPreviewSprites.SwatchColor(look, style, i - 1);
            }
            styleNote.gameObject.SetActive(look == null);
            colorNote.gameObject.SetActive(count == 0);
            preview.Set(0, id, style, dye, 1f);
        }

        /// <summary>Every frame: buttons wait for a running call or join; the status line; the hint turns into the rule that is broken.</summary>
        void RefreshState()
        {
            NetClient client = NetClient.Active;
            bool connecting = client != null && client.State == ClientState.Connecting;
            bool busy = AccountFlow.Busy || connecting;
            createButton.interactable = backButton.interactable = diceButton.interactable = input.interactable = !busy;
            status.text = connecting ? client.Status : AccountFlow.Status;
            status.color = AccountFlow.StatusBad && !connecting ? CharacterFlowKit.Bad : CharacterFlowKit.Soft;
            if (!hintDirty) return;
            hintDirty = false;
            string problem = input.text.Length > 0 ? AccountFlow.NameProblem(input.text) : null;
            hint.text = problem ?? NameHint;
            hint.color = problem != null ? CharacterFlowKit.Bad : CharacterFlowKit.Soft;
        }

        void OnStyle(int style) { AccountFlow.PendingStyle = style; }
        void OnDye(int dye) { AccountFlow.PendingDye = dye; }
        void OnDice() { input.text = AccountFlow.RandomName(); }

        void OnBack()
        {
            if (AccountFlow.Busy) return;
            ConnectScreenGui.CreateHero(AccountFlow.Characters, AccountFlow.CustomizeClass); // Choose Class again, on the same class
        }

        void Submit()
        {
            if (AccountFlow.Busy) return;
            AccountFlow.Create(input.text.Trim(), AccountFlow.CustomizeClass, AccountFlow.PendingStyle, AccountFlow.PendingDye);
        }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
