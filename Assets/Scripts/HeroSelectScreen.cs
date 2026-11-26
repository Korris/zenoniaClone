using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Hero select (class step of a new hero), RO Soft Adventure redesign by ChatGPT (26/11/2026, docs/reference/hero-select-ro-adventure/): six classes on pedestals over the courtyard
    /// (the real layered in-game heroes) and, below, the picked class's portrait, real stats, skills and a live in-game preview (HeroSelectPreview). A tap picks a hero, the gold button goes on.
    /// The Archer took the old Boxing (Fighter) slot: the Fighter is no longer offered (its enum value stays for old saves). Since the character select screen (CharacterListScreen, 19/11/2026) it is only the class step of a NEW hero:
    /// create mode (OpenCreate), opened from that list online and offline (also over the running game), with BACK to the list and NEXT to the look / name step; a class the
    /// `taken` test refuses cannot be picked (none today: offline a device may hold several heroes of one class). A dev host still picks its hero here directly (no saves, no list).
    /// </summary>
    public sealed partial class HeroSelectScreen : MonoBehaviour
    {
        const float EaseSeconds = 0.09f;

        sealed class Slot
        {
            public int ClassIndex;     // into HeroClassDef.All
            public string Art;
            public float HeroScale = 1f;
            public RectTransform Banner, Pedestal;
            public Image BannerImage, PlateImage, Ring;
            public Text Name;
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

        RectTransform root, stage;
        readonly List<Slot> slots = new List<Slot>();
        readonly List<SkillDef> skills = new List<SkillDef>();
        HeroSelectPreview preview;
        Image portrait, startLabel;
        Text tagline, stateText, skillNameText, nextLabel;
        Button startButton, backButton;
        readonly Image[] statFills = new Image[StatCount];
        readonly Text[] statValues = new Text[StatCount];
        readonly Image[] skillIcons = new Image[VisibleSkills];
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

        static Sprite Art(string name) { return UiArt.Get("hero-select-ro/" + name); }

        void Update()
        {
            // the create step of the character select (online / offline, maybe over the game), or a dev host's own pick; the appearance step of a new hero replaces it
            bool show = !CharacterCustomizeScreen.OfflineOpen && (Creating || (GameSession.Hero == null && !ConnectScreenGui.Visible && NetMode.Current == RunMode.Host));
            bool shown = root != null && root.gameObject.activeSelf;
            if (show != shown)
            {
                if (show) Open();
                else { root.gameObject.SetActive(false); preview.Stop(); heroes.SetActive(false); }
            }
            if (!show) return;
            FitStage();
            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / EaseSeconds);
            for (int i = 0; i < slots.Count; i++) Ease(i, i == picked, k);
            preview.Tick(Time.unscaledDeltaTime);
        }

        void Open()
        {
            if (root == null) Build();

            root.SetAsLastSibling();
            root.gameObject.SetActive(true);
            heroes.SetActive(true);
            backButton.gameObject.SetActive(Creating);
            picked = -1;
            HeroClass? back = Creating ? createStart : null; // coming back from the look step: the same class
            Pick(SlotOfClass((int)(back ?? HeroClass.Wizard))); // the mock opens on the Wizard
            for (int i = 0; i < slots.Count; i++) Ease(i, i == picked, 1f); // no easing in from the last visit
        }

        int SlotOfClass(int classIndex)
        {
            for (int i = 0; i < slots.Count; i++) if (slots[i].ClassIndex == classIndex) return i;
            return 1; // a class the picker no longer offers (the Fighter): the Wizard
        }

        /// <summary>Picked look: a slightly bigger hero and banner, the glowing ring at its feet, the selected name plate.</summary>
        void Ease(int i, bool on, float k)
        {
            Slot s = slots[i];
            s.HeroScale = Mathf.Lerp(s.HeroScale, on ? PickedScale : 1f, k);
            heroes.Set(i, (HeroClass)s.ClassIndex, PickerLook(i), "", s.HeroScale);
            float flag = Mathf.Lerp(s.Banner.localScale.x, on ? PickedBanner : 1f, k);
            s.Banner.localScale = new Vector3(flag, flag, 1f);
            float pulse = 0.72f + 0.14f * Mathf.Sin(Time.unscaledTime * Mathf.PI); // 2 s breathing light, softer than the face (ChatGPT V3)
            Color c = s.Ring.color; c.a = Mathf.Lerp(c.a, on ? pulse : 0f, k); s.Ring.color = c;
            Sprite plate = UiArt.Get("hero-select-ro/" + (on ? "name-plate-selected" : "name-plate"), on ? new Vector4(56f, 18f, 56f, 18f) : new Vector4(48f, 12f, 48f, 12f));
            if (s.PlateImage.sprite != plate) { s.PlateImage.sprite = plate; s.PlateImage.pixelsPerUnitMultiplier = (on ? 93f : 79f) / PlateH; }
        }

        /// <summary>Picks a slot: fills the panels and restarts the preview.</summary>
        public void Pick(int index)
        {
            if (index == picked) return;
            picked = index;
            Slot s = slots[index];
            portrait.sprite = Art("portrait-" + s.Art);
            portrait.enabled = portrait.sprite != null;
            infoIcon.sprite = Art("class-" + s.Art);
            HeroClassDef c = HeroClassDef.All[s.ClassIndex];
            infoName.text = c.Name;
            tagline.text = c.Tagline;
            for (int i = 0; i < StatCount; i++) ShowStat(c, i);
            skills.Clear();
            skills.AddRange(SkillTable.ForClass(RpgSession.ClassId(c.Id)));
            skills.Sort((a, b) => a.UnlockLevel != b.UnlockLevel ? a.UnlockLevel.CompareTo(b.UnlockLevel) : a.Index.CompareTo(b.Index));
            skillOffset = 0;
            ShowSkills();
            preview.Show(c, PickerLook(index));
            ShowState(c);
        }

        void ShowStat(HeroClassDef c, int stat)
        {
            // Classes differ by a few points, so the bars spread the range of the offered classes; the number beside each bar is the real value.
            float min = float.MaxValue, max = float.MinValue;
            foreach (HeroClass id in PickerClasses)
            {
                HeroClassDef d = HeroClassDef.Get(id);
                float v = StatValue(d, stat);
                min = Mathf.Min(min, v); max = Mathf.Max(max, v);
            }
            float value = StatValue(c, stat);
            statFills[stat].rectTransform.anchorMax = new Vector2(max > min ? Mathf.Lerp(0.3f, 1f, (value - min) / (max - min)) : 1f, 1f);
            statValues[stat].text = stat == StatCount - 1 ? Mathf.RoundToInt(value * 100f) + "%" : ((int)value).ToString();
        }

        void ShowSkills()
        {
            for (int i = 0; i < VisibleSkills; i++)
            {
                SkillDef d = skillOffset + i < skills.Count ? skills[skillOffset + i] : null;
                skillIcons[i].sprite = SkillIcons.Get(d);
                skillIcons[i].enabled = skillIcons[i].sprite != null;
                bool locked = d != null && d.UnlockLevel > 1; // a new hero is level 1: later skills show dimmed with a padlock
                skillIcons[i].color = locked ? new Color(0.55f, 0.53f, 0.5f) : Color.white;
                skillLocks[i].enabled = locked;
                skillIcons[i].transform.parent.parent.gameObject.SetActive(d != null); // a short last page: no empty rings
            }
            CentreSkillRow(Mathf.Clamp(skills.Count - skillOffset, 0, VisibleSkills));
            int pages = Mathf.Max(1, (skills.Count + VisibleSkills - 1) / VisibleSkills);
            ShowDots(pages, skillOffset / VisibleSkills);
            ShowSkillName(null);
        }

        /// <summary>Pages of five skills (the arrows and dots of the strip).</summary>
        void PageSkills(int delta)
        {
            int last = Mathf.Max(0, (skills.Count - 1) / VisibleSkills) * VisibleSkills;
            int next = Mathf.Clamp(skillOffset + delta * VisibleSkills, 0, last);
            if (next == skillOffset) return;
            skillOffset = next;
            ShowSkills();
        }

        /// <summary>Tapped icon (cell = its place in the row): the glowing ring on it and its name, level and MP under the row. null = the hint.</summary>
        void ShowSkillName(SkillDef d, int cell = -1)
        {
            for (int i = 0; i < VisibleSkills; i++) skillPicked[i].enabled = d != null && i == cell;
            skillInfo.text = d == null ? "<color=#776C60>Tap a skill to see it</color>"
                : "<size=15><b>" + d.Name + "</b></size>\n<size=12><color=#776C60D9>Unlocks at Lv " + d.UnlockLevel + "   MP " + d.Mp + "</color></size>";
        }

        void ShowState(HeroClassDef c)
        {
            Color ink = new Color32(0x4A, 0x2E, 0x12, 0xFF); // dark brown on the gold button
            if (Creating)
            {
                bool taken = createTaken((int)c.Id);
                nextLabel.text = "NEXT"; // it leads on to the look / name step
                stateText.text = taken ? (createOnline ? "You already have this hero" : "This class already has a saved hero") : ""; // the mockup has no line over NEXT: only a refusal shows
                startButton.interactable = !taken;
                ink.a = taken ? 0.5f : 1f;
                nextLabel.color = ink;
                startLabel.color = taken ? new Color(0.72f, 0.69f, 0.65f) : Color.white;
                return;
            }
            stateText.text = ""; // a dev host: no saves
            nextLabel.text = "START";
            nextLabel.color = ink;
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
