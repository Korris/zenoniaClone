using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Hero select, laid out from the mock Assets/ArtLibrary/custom/2b14c1e0-...png (art: Sprites/ui/hero-select, built by Tools/build-hero-select-art.py): six heroes on pedestals over the
    /// castle scene and, below, the picked hero's portrait, real stats, skills and a live in-game preview (HeroSelectPreview). A tap picks a hero, the gold button starts. The "boxing" art is the Fighter
    /// (playable since 2026-11-04; NotPlayable stays for a future art-only slot). Since the character select screen (CharacterListScreen, 19/11/2026) it is only the class step of a NEW hero:
    /// create mode (OpenCreate), opened from that list online and offline (also over the running game), with BACK to the list and NEXT to the look / name step; a class the
    /// `taken` test refuses cannot be picked (none today: offline a device may hold several heroes of one class). A dev host still picks its hero here directly (no saves, no list).
    /// </summary>
    public sealed partial class HeroSelectScreen : MonoBehaviour
    {
        const int NotPlayable = -1;
        const float EaseSeconds = 0.09f;
        static readonly Color Dimmed = new Color(0.82f, 0.82f, 0.86f, 1f); // the mock's other heroes stay nearly full colour
        static readonly Color Disabled = new Color(0.42f, 0.42f, 0.46f, 1f);
        static readonly Color Gold = new Color(1f, 0.86f, 0.5f);

        sealed class Slot
        {
            public int ClassIndex;     // into HeroClassDef.All, or NotPlayable (never picked)
            public string Art;
            public Color Color, NamePicked;
            public float Scale;
            public RectTransform Group, Hero, Banner, Pedestal;
            public GameObject Underline;
            public Image HeroImage, BannerImage, PedestalImage, NameImage, Glow;
        }

        static HeroSelectScreen instance;
        static System.Func<int, bool> createTaken; // class index -> this class cannot be made (no caller refuses one today). Non null while the create mode is up.
        static System.Action<HeroClass> createPicked;
        static System.Action createBack;
        static HeroClass? createStart; // class the create mode opens on (coming back from the look step); null = the Wizard
        static bool createOnline;

        /// <summary>The class of a new character is being chosen (from the character select screen, online or offline).</summary>
        public static bool Creating { get { return createTaken != null; } }

        /// <summary>Opens the create mode; exactly one of the two callbacks runs when it closes. Nothing happens without a screen (headless bots create by class directly).</summary>
        public static void OpenCreate(System.Func<int, bool> taken, System.Action<HeroClass> picked, System.Action back, HeroClass? start, bool online)
        {
            if (instance == null) return;
            createTaken = taken; createPicked = picked; createBack = back; createStart = start; createOnline = online;
        }

        static void CloseCreate() { createTaken = null; createPicked = null; createBack = null; createStart = null; }

        /// <summary>Capture scripts: what a tap on a pedestal and on the gold button do.</summary>
        public static void TestPickClass(int classIndex) { if (instance != null) instance.Pick(instance.SlotOfClass(classIndex)); }
        public static void TestPressStart() { if (instance != null) instance.OnStart(); }

        void OnBack()
        {
            if (!Creating) return;
            System.Action back = createBack;
            CloseCreate();
            back();
        }

        RectTransform root, stage, titleRt, gearRt;
        readonly List<Slot> slots = new List<Slot>();
        readonly List<SkillDef> skills = new List<SkillDef>();
        HeroSelectPreview preview;
        Image portrait, infoName, startLabel, skillHeader, sliderKnob;
        Text tagline, stateText, skillNameText, nextLabel;
        Button startButton, backButton, gearButton;
        readonly Image[] statFills = new Image[StatCount];
        readonly Text[] statValues = new Text[StatCount];
        readonly Image[] skillIcons = new Image[VisibleSkills];
        readonly Image[] skillFrames = new Image[VisibleSkills];
        int picked = -1, skillOffset;

        const int StatCount = 5, VisibleSkills = 5;
        const float BaseCrit = 0.08f; // HeroController.CritChance

        public static HeroSelectScreen Create(Transform parent)
        {
            var go = new GameObject("HeroSelectScreen");
            go.transform.SetParent(parent, false);
            instance = go.AddComponent<HeroSelectScreen>();
            return instance;
        }

        /// <summary>True while the screen is up (the capture script waits for it).</summary>
        public static bool IsShown { get { return instance != null && instance.root != null && instance.root.gameObject.activeSelf; } }

        static float StatValue(HeroClassDef c, int stat)
        {
            switch (stat)
            {
                case 0: return c.Atk;
                case 1: return c.Def;
                case 2: return c.MaxHp;
                case 3: return c.MaxMp;
                default: return BaseCrit + c.CritBonus;
            }
        }

        static Sprite Art(string name) { return UiArt.Get("hero-select/" + name); }

        void Update()
        {
            // the create step of the character select (online / offline, maybe over the game), or a dev host's own pick; the appearance step of a new hero replaces it
            bool show = !CharacterCustomizeScreen.OfflineOpen && (Creating || (GameSession.Hero == null && !ConnectScreenGui.Visible && NetMode.Current == RunMode.Host));
            bool shown = root != null && root.gameObject.activeSelf;
            if (show != shown)
            {
                if (show) Open();
                else { root.gameObject.SetActive(false); preview.Stop(); }
            }
            if (!show) return;
            FitStage();
            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / EaseSeconds);
            for (int i = 0; i < slots.Count; i++) Ease(slots[i], i == picked, k);
            preview.Tick(Time.unscaledDeltaTime);
        }

        void Open()
        {
            if (root == null) Build();

            root.SetAsLastSibling();
            root.gameObject.SetActive(true);
            backButton.gameObject.SetActive(Creating);
            gearButton.gameObject.SetActive(false); // online / offline is chosen on the character select screen now
            picked = -1;
            HeroClass? back = Creating ? createStart : null; // coming back from the look step: the same class
            Pick(SlotOfClass((int)(back ?? HeroClass.Wizard))); // the mock opens on the Wizard
            for (int i = 0; i < slots.Count; i++) Ease(slots[i], i == picked, 1f); // no easing in from the last visit
        }

        int SlotOfClass(int classIndex)
        {
            for (int i = 0; i < slots.Count; i++) if (slots[i].ClassIndex == classIndex) return i;
            return 0;
        }

        static void Ease(Slot s, bool on, float k)
        {
            float scale = Mathf.Lerp(s.Hero.localScale.x, (on ? PickedScale : 1f) * s.Scale, k);
            s.Hero.localScale = new Vector3(scale, scale, 1f);
            float big = Mathf.Lerp(s.Pedestal.localScale.x, on ? PickedPedestal : 1f, k);
            s.Pedestal.localScale = new Vector3(big, big, 1f);
            float flag = Mathf.Lerp(s.Banner.localScale.x, on ? PickedBanner : 1f, k);
            s.Banner.localScale = new Vector3(flag, flag, 1f);
            Color rest = s.ClassIndex == NotPlayable ? Disabled : Dimmed;
            s.HeroImage.color = Color.Lerp(s.HeroImage.color, on ? Color.white : rest, k);
            s.BannerImage.color = Color.Lerp(s.BannerImage.color, s.ClassIndex == NotPlayable ? rest : Color.white, k); // in the mock every banner is lit
            s.PedestalImage.color = Color.Lerp(s.PedestalImage.color, on ? Color.white : rest, k);
            s.NameImage.color = Color.Lerp(s.NameImage.color, on ? s.NamePicked : s.ClassIndex == NotPlayable ? rest : Gold, k);
            s.Underline.SetActive(on);
            Color g = s.Glow.color; g.a = Mathf.Lerp(g.a, on ? 0.85f : 0f, k); s.Glow.color = g;
        }

        /// <summary>Picks a slot: fills the panels and restarts the preview.</summary>
        public void Pick(int index)
        {
            if (index == picked || slots[index].ClassIndex == NotPlayable) return; // an art-only slot cannot be picked
            picked = index;
            Slot s = slots[index];
            s.Hero.parent.SetAsLastSibling(); // the big picked hero stands in front of its neighbours
            portrait.sprite = Art("portrait-" + s.Art);
            infoName.sprite = Art("name-" + s.Art);
            infoName.color = s.Color;
            HeroClassDef c = HeroClassDef.All[s.ClassIndex];
            tagline.text = c.Tagline;
            for (int i = 0; i < StatCount; i++) ShowStat(c, i, s.Color);
            skills.Clear();
            skills.AddRange(SkillTable.ForClass(RpgSession.ClassId(c.Id)));
            skills.Sort((a, b) => a.UnlockLevel != b.UnlockLevel ? a.UnlockLevel.CompareTo(b.UnlockLevel) : a.Index.CompareTo(b.Index));
            skillOffset = 0;
            ShowSkills();
            preview.Show(c);
            ShowState(c);
        }

        void ShowStat(HeroClassDef c, int stat, Color col)
        {
            // Classes differ by a few points, so the bars spread the class range; the number beside each bar is the real value.
            float min = float.MaxValue, max = float.MinValue;
            foreach (HeroClassDef d in HeroClassDef.All)
            {
                float v = StatValue(d, stat);
                min = Mathf.Min(min, v); max = Mathf.Max(max, v);
            }
            float value = StatValue(c, stat);
            statFills[stat].rectTransform.anchorMax = new Vector2(max > min ? Mathf.Lerp(0.3f, 1f, (value - min) / (max - min)) : 1f, 1f);
            statFills[stat].color = col;
            statValues[stat].text = stat == StatCount - 1 ? Mathf.RoundToInt(value * 100f) + "%" : ((int)value).ToString();
        }

        void ShowSkills()
        {
            for (int i = 0; i < VisibleSkills; i++)
            {
                SkillDef d = skillOffset + i < skills.Count ? skills[skillOffset + i] : null;
                skillIcons[i].sprite = SkillIcons.Get(d);
                skillIcons[i].enabled = skillIcons[i].sprite != null;
            }
            float room = Mathf.Max(0, skills.Count - VisibleSkills);
            sliderKnob.rectTransform.anchorMin = sliderKnob.rectTransform.anchorMax = new Vector2(room > 0f ? skillOffset / room : 0f, 0.5f);
            ShowSkillName(null);
        }

        void PageSkills(int delta)
        {
            int next = Mathf.Clamp(skillOffset + delta, 0, Mathf.Max(0, skills.Count - VisibleSkills));
            if (next == skillOffset) return;
            skillOffset = next;
            ShowSkills();
        }

        /// <summary>Tapped icon: its name replaces the panel's header. null = the plain header.</summary>
        void ShowSkillName(SkillDef d)
        {
            skillHeader.enabled = d == null;
            skillNameText.text = d == null ? "" : d.Name + "  <color=#b8c4d8>Lv " + d.UnlockLevel + "   MP " + d.Mp + "</color>";
        }

        void ShowState(HeroClassDef c)
        {
            startLabel.enabled = !Creating; // the create step says NEXT in text (it leads on to the look / name step), not START
            nextLabel.gameObject.SetActive(Creating);
            if (Creating)
            {
                bool taken = createTaken((int)c.Id);
                stateText.text = taken ? (createOnline ? "You already have this hero" : "This class already has a saved hero") : "New hero";
                startButton.interactable = !taken;
                nextLabel.color = taken ? new Color(0.25f, 0.12f, 0.02f, 0.5f) : new Color(0.25f, 0.12f, 0.02f); // dark brown on the gold button, as the art labels
                return;
            }
            stateText.text = "New hero"; // a dev host: no saves
            startLabel.sprite = Art("label-start");
            startButton.interactable = true;
            startLabel.color = Color.white;
        }

        void OnStart()
        {
            if (picked < 0 || GameSession.Instance == null) return;
            if (Creating)
            {
                if (createTaken((int)slots[picked].ClassIndex)) return;
                System.Action<HeroClass> done = createPicked;
                CloseCreate();
                done((HeroClass)slots[picked].ClassIndex);
                return;
            }
            CharacterCustomizeScreen.OpenOffline((HeroClass)slots[picked].ClassIndex, false); // a dev host's hero: its look first (the appearance step spawns it)
        }
    }
}
