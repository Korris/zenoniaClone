using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Hero select, laid out from the mock Assets/ArtLibrary/custom/2b14c1e0-...png (art: Sprites/ui/hero-select, built by Tools/build-hero-select-art.py): six heroes on pedestals over the
    /// castle scene and, below, the picked hero's portrait, real stats, skills and a live in-game preview (HeroSelectPreview). A tap picks a hero, the gold button starts. Boxing has art only:
    /// the class does not exist yet, so it stands dimmed and cannot be picked. Shown while there is no hero offline or as a host, and over the running game through HeroChangeScreen.
    /// The online client picks its class on ConnectScreenGui instead.
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

        RectTransform root, stage, titleRt, gearRt;
        readonly List<Slot> slots = new List<Slot>();
        readonly List<SkillDef> skills = new List<SkillDef>();
        HeroSelectPreview preview;
        Image portrait, infoName, startLabel, skillHeader, sliderKnob;
        Text tagline, stateText, skillNameText;
        Button startButton, backButton, gearButton;
        readonly Image[] statFills = new Image[StatCount];
        readonly Text[] statValues = new Text[StatCount];
        readonly Image[] skillIcons = new Image[VisibleSkills];
        readonly Image[] skillFrames = new Image[VisibleSkills];
        string[] savedLabels;
        int picked = -1, skillOffset;
        bool inGame;

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
            bool nowInGame = HeroChangeScreen.IsOpen;
            bool show = nowInGame || (GameSession.Hero == null && !ConnectScreenGui.Visible && !NetMode.IsClient);
            bool shown = root != null && root.gameObject.activeSelf;
            if (show != shown)
            {
                if (show) Open(nowInGame);
                else { root.gameObject.SetActive(false); preview.Stop(); }
            }
            if (!show) return;
            FitStage();
            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / EaseSeconds);
            for (int i = 0; i < slots.Count; i++) Ease(slots[i], i == picked, k);
            preview.Tick(Time.unscaledDeltaTime);
        }

        void Open(bool overGame)
        {
            if (root == null) Build();
            inGame = overGame;
            root.SetAsLastSibling();
            root.gameObject.SetActive(true);
            savedLabels = null; // saves may have changed since the last time
            backButton.gameObject.SetActive(inGame);
            gearButton.gameObject.SetActive(!inGame && NetMode.IsOffline);
            picked = -1;
            HeroController current = inGame ? GameSession.Hero : null;
            Pick(current != null ? SlotOfClass((int)current.Class.Id) : SlotOfClass((int)HeroClass.Wizard)); // the mock opens on the Wizard
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
            if (index == picked || slots[index].ClassIndex == NotPlayable) return; // Boxing is disabled for now
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
            HeroController current = inGame ? GameSession.Hero : null;
            bool playing = current != null && current.Class == c;
            string saved = SavedLabel((int)c.Id);
            stateText.text = playing ? "Playing now" : saved ?? "New hero";
            startLabel.sprite = Art(playing ? "label-playing" : inGame ? "label-switch" : saved != null ? "label-continue" : "label-start");
            startButton.interactable = !playing;
            startLabel.color = startButton.interactable ? Color.white : new Color(1f, 1f, 1f, 0.6f);
        }

        string SavedLabel(int i)
        {
            if (savedLabels == null)
            {
                savedLabels = new string[HeroClassDef.All.Length];
                for (int c = 0; c < savedLabels.Length; c++)
                {
                    SaveData d = SaveStore.Load(RpgSession.ClassId(HeroClassDef.All[c].Id));
                    savedLabels[c] = d != null ? "Saved hero, Lv " + d.level : null;
                }
            }
            return savedLabels[i];
        }

        void OnStart()
        {
            if (picked < 0 || GameSession.Instance == null) return;
            if (inGame) HeroChangeScreen.Close();
            GameSession.Instance.Spawn((HeroClass)slots[picked].ClassIndex);
        }
    }
}
