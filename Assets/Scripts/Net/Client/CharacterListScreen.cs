using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Api;

namespace ZenoniaProto
{
    /// <summary>
    /// The online character list (AccountPage.Characters), drawn with uGUI: four pedestals a page with the account's heroes (live full-body preview with the look kept in CharacterLookPrefs) and
    /// "Create Character" tablets for the free slots, the picked hero's panel with ENTER GAME and DELETE, and the gear menu (log out, play offline, direct connect in dev builds).
    /// It only reads AccountFlow and calls it (Enter, ShowDelete, Logout); the IMGUI account pages draw everything else. One hero per class: a tablet exists only while a class is free.
    /// </summary>
    public sealed partial class CharacterListScreen : MonoBehaviour
    {
        const int PageSize = 4;

        static CharacterListScreen instance;
        HeroPreviewStage preview;
        readonly List<CharacterSummary> items = new List<CharacterSummary>(); // the account's heroes, then one null per "Create Character" tablet
        string selectedId;
        int page = -1, builtVersion = -1;
        bool dirty;

        public static bool IsShown { get { return instance != null && instance.root != null && instance.root.gameObject.activeSelf; } }

        public static CharacterListScreen Create(Transform parent)
        {
            var go = new GameObject("CharacterListScreen");
            go.transform.SetParent(parent, false);
            instance = go.AddComponent<CharacterListScreen>();
            return instance;
        }

        /// <summary>Capture scripts: the picked hero, by name (false when the account has no such hero).</summary>
        public static bool TestSelect(string name)
        {
            if (instance == null) return false;
            foreach (CharacterSummary c in AccountFlow.Characters)
                if (c.name == name) { instance.selectedId = c.id; instance.page = -1; instance.dirty = true; return true; }
            return false;
        }

        void Update()
        {
            bool want = ConnectScreenGui.Visible && !ConnectScreenGui.DevPageOpen && GameSession.Hero == null && AccountFlow.Page == AccountPage.Characters;
            bool shown = IsShown;
            if (want != shown)
            {
                if (want) Open();
                else Close();
            }
            if (!want) return;
            Fit();
            if (dirty || builtVersion != AccountFlow.ListVersion) Redraw();
            RefreshState();
        }

        void Open()
        {
            if (root == null) Build();
            root.SetAsLastSibling();
            root.gameObject.SetActive(true);
            menuRt.gameObject.SetActive(false);
            preview.SetActive(true);
            page = -1; // the page of the remembered hero
            dirty = true;
        }

        void Close()
        {
            if (root == null) return;
            root.gameObject.SetActive(false);
            preview.SetActive(false);
        }

        void Fit()
        {
            float s = CharacterFlowKit.Fit(root, stage);
            var scale = new Vector3(s, s, 1f);
            titleRt.localScale = gearRt.localScale = menuRt.localScale = scale;
            titleRt.anchoredPosition = new Vector2(20f * s, -14f * s);
            gearRt.anchoredPosition = new Vector2(-15f * s, -17f * s);
            menuRt.anchoredPosition = new Vector2(-15f * s, -69f * s);
        }

        CharacterSummary Selected()
        {
            foreach (CharacterSummary c in AccountFlow.Characters) if (c.id == selectedId) return c;
            return null;
        }

        /// <summary>Rebuilds the page: which hero or tablet stands on each pedestal, the preview heroes and the info panel.</summary>
        void Redraw()
        {
            dirty = false;
            builtVersion = AccountFlow.ListVersion;
            List<CharacterSummary> owned = AccountFlow.Characters;
            items.Clear();
            items.AddRange(owned);
            int free = AccountFlow.MaxCharacters - owned.Count; // open slots, whatever the classes
            for (int n = Mathf.Min(Mathf.Max(0, free), Mathf.Max(1, PageSize - owned.Count)); n > 0; n--) items.Add(null);

            if (Selected() == null) selectedId = owned.Count > 0 ? owned[0].id : null; // a deleted or unknown hero: the first one
            int pages = Mathf.Max(1, (items.Count + PageSize - 1) / PageSize);
            if (page < 0) page = Mathf.Max(0, owned.FindIndex(c => c.id == selectedId)) / PageSize;
            page = Mathf.Clamp(page, 0, pages - 1);

            preview.Clear();
            for (int i = 0; i < PageSize; i++) ShowColumn(i, page * PageSize + i);
            ShowInfo(Selected());
            leftArrow.gameObject.SetActive(page > 0);
            rightArrow.gameObject.SetActive(page < pages - 1);
        }

        void ShowColumn(int i, int index)
        {
            Column c = columns[i];
            bool exists = index < items.Count, create = exists && items[index] == null;
            CharacterSummary hero = exists ? items[index] : null;
            bool picked = hero != null && hero.id == selectedId;
            int classIndex = hero != null ? GameSession.ClassIndex(hero.classId) : -1;
            bool known = classIndex >= 0 && classIndex < HeroClassDef.All.Length;

            c.Hit.gameObject.SetActive(exists);
            c.CreateArt.gameObject.SetActive(create);
            c.Pedestal.gameObject.SetActive(hero != null && !picked);
            c.Glow.gameObject.SetActive(picked);
            c.Plate.SetActive(hero != null);
            c.Banner.gameObject.SetActive(known);
            if (hero == null) return;
            float scale = picked ? PickedScale : 1f;
            foreach (RectTransform rt in c.Scaled) rt.localScale = new Vector3(scale, scale, 1f);
            c.Name.text = hero.name.ToUpperInvariant();
            c.Level.text = "Lv " + hero.level;
            if (!known) return; // a class this client does not know: pedestal and plate only
            var id = (HeroClass)classIndex;
            c.Banner.sprite = UiArt.Get("hero-select/banner-" + CharacterFlowKit.ClassArtName(id));
            int style, dye;
            CharacterLookPrefs.Get(hero.id, out style, out dye);
            preview.Set(i, id, style, dye, scale);
        }

        void ShowInfo(CharacterSummary hero)
        {
            int classIndex = hero != null ? GameSession.ClassIndex(hero.classId) : -1;
            bool known = classIndex >= 0 && classIndex < HeroClassDef.All.Length;
            IGameMap map = hero != null && !string.IsNullOrEmpty(hero.mapId) ? MapRegistry.Get(hero.mapId) : null;
            emptyNote.gameObject.SetActive(hero == null);
            portrait.gameObject.SetActive(hero != null && known);
            infoName.gameObject.SetActive(hero != null);
            infoClass.transform.parent.gameObject.SetActive(hero != null);
            infoLevel.transform.parent.gameObject.SetActive(hero != null);
            infoPlace.transform.parent.gameObject.SetActive(map != null); // no last place in the summary (or an unknown map): the row is left out
            enterButton.gameObject.SetActive(hero != null);
            deleteButton.gameObject.SetActive(hero != null);
            if (hero == null) return;
            infoName.text = hero.name;
            infoClass.text = "Class: " + (known ? HeroClassDef.All[classIndex].Name : hero.classId);
            infoLevel.text = "Level: " + hero.level + (hero.online ? "   (online)" : "");
            if (map != null) infoPlace.text = "Last Location: " + map.Name;
            if (known)
            {
                var id = (HeroClass)classIndex;
                portrait.sprite = UiArt.Get(id == HeroClass.Fighter ? "hero-select/hero-boxing" : "hero-select/portrait-" + CharacterFlowKit.ClassArtName(id));
            }
        }

        /// <summary>Every frame: the status line and the buttons that wait for a running call or the join.</summary>
        void RefreshState()
        {
            NetClient client = NetClient.Active;
            bool connecting = client != null && client.State == ClientState.Connecting;
            bool busy = AccountFlow.Busy || connecting;
            enterButton.interactable = deleteButton.interactable = !busy;
            emptyNote.text = AccountFlow.Busy ? "Loading ..." : "No heroes yet: create your first.";
            status.text = connecting ? client.Status : AccountFlow.Status;
            status.color = AccountFlow.StatusBad && !connecting ? CharacterFlowKit.Bad : CharacterFlowKit.Soft;
        }

        void OnColumn(int i)
        {
            int index = page * PageSize + i;
            if (AccountFlow.Busy || index >= items.Count) return;
            CharacterSummary hero = items[index];
            if (hero == null) { ConnectScreenGui.CreateHero(AccountFlow.Characters); return; }
            selectedId = hero.id;
            dirty = true;
        }

        void OnEnter()
        {
            CharacterSummary hero = Selected();
            if (hero != null) AccountFlow.Enter(hero);
        }

        void OnDelete()
        {
            CharacterSummary hero = Selected();
            if (hero != null) AccountFlow.ShowDelete(hero);
        }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
