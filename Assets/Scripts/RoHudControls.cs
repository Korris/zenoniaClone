using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The RO Soft HUD layer (02-hud.png) built by TouchControls while RoHud.Active, in its own safe-area root above the old touch layer:
    ///   bottom-left   joystick (base with arrows + knob; TouchJoystick and its corner touch zone, as the other looks); none on PC
    ///   PC (RoHud.Pc) the RO3 profile: none of the bottom pieces below; RoHudHotbar (bottom-centre tray with key caps) instead
    ///   bottom-right  the coral Attack button (held = attack, pressed art while down), 4 skill buttons on the thin ring around it =
    ///                 the 8 loadout slots of the skill window as two pages of 4 (Tab or a vertical swipe on the arc turns the page),
    ///                 Auto (AutoBattle on / off, lit while on) and Run (GameKey.Dash, the dash)
    ///   right         HP / MP potion buttons with the count badge (the size they would drink, as the other potion buttons)
    ///   top           the menu row (RoHudMenu), the map / connection pills + minimap "+" (RoHudMapPanel), the WANTED card (RoHudWanted)
    /// Like every touch HUD it only feeds GameKeys through TouchControls, so the game logic is the keyboard's.
    /// </summary>
    public sealed class RoHudControls
    {
        const int PerPage = 4, Pages = SkillLoadout.Slots / PerPage;
        static readonly float[] Angles = { 162f, 90f, 18f, 234f }; // degrees around the attack button: left, top, right, lower-left (02-hud.png)
        static readonly Vector2 Attack = new Vector2(-RoHud.AttackX, RoHud.AttackY);

        readonly RectTransform root, safe;
        public readonly RoHudMenu Menu;
        readonly RoHudMapPanel map;
        readonly RoHudWanted wanted;
        readonly RoHudBossBar boss;
        readonly RoHudHotbar hotbar; // PC profile: the RO3 bottom tray instead of everything below (null on mobile)
        readonly RoHudSkillSlot[] slots = new RoHudSkillSlot[PerPage];
        readonly Text[] slotKeys = new Text[PerPage]; // PC: the digit of each ring slot (1-4, page 2: 5-8)
        readonly Image autoArt;
        readonly Text hpCount, mpCount;
        readonly Image hpIcon, mpIcon;
        int page, version = int.MinValue, shownHp = -1, shownMp = -1, shownHpSize = -1, shownMpSize = -1;
        bool shownAuto;
        HeroClassDef cls;

        public RoHudControls(RectTransform oldLayer)
        {
            PaperLayout layout = PaperLayout.CreateSafeArea(oldLayer.parent, "RoHudControls");
            root = layout.Root;
            root.SetSiblingIndex(oldLayer.GetSiblingIndex() + 1);
            safe = layout.Safe;

            if (RoHud.Pc) hotbar = new RoHudHotbar(safe); // PC moves with WASD / arrows: no joystick, no arc (RO3 profile)
            else BuildMobileCluster(safe, out autoArt, out hpCount, out mpCount, out hpIcon, out mpIcon);

            map = new RoHudMapPanel(safe);
            wanted = new RoHudWanted(safe);
            boss = new RoHudBossBar(safe);
            Menu = new RoHudMenu(safe); // last: its More column lies over the rest
            root.gameObject.SetActive(false);
        }

        /// <summary>Mobile: the joystick, the skill ring with Attack, Auto, Run and the potions (02-hud.png).</summary>
        void BuildMobileCluster(RectTransform safe, out Image autoArt, out Text hpCount, out Text mpCount, out Image hpIcon, out Image mpIcon)
        {
            BuildJoystick(safe);

            // Skill ring + swipe strip first (lowest), then the slots, then Attack on top of the ring's centre.
            RectTransform ring = RoHud.Box(safe, "SkillRing", RoHud.BottomRight, Attack.x, Attack.y, RoHud.ArcRing, RoHud.ArcRing);
            RoHud.Art(ring, "hud-skill-arc").color = new Color(1f, 1f, 1f, 0.85f);
            TouchControls.Img(UiKit.Fill(ring, "Swipe"), null, Color.clear, true).gameObject.AddComponent<SwipeDetector>().onSwipe = Cycle;
            for (int i = 0; i < PerPage; i++)
            {
                float a = Angles[i] * Mathf.Deg2Rad;
                RoHudSkillSlot s = slots[i] = new RoHudSkillSlot(safe, "Skill" + i, Attack + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * RoHud.SkillArc, RoHud.SkillSize);
                NameTags.Occlude(s.Root); // name tags / monster foot labels stay off the HUD buttons (their captions have no backing)
                var sw = s.Root.gameObject.AddComponent<SwipeDetector>();
                sw.onSwipe = Cycle;
                sw.onTap = () => { if (s.HasSkill) SkillMagazine.CastRequested(s.SkillIndex); };
                // Ground skills (ChatGPT 26/11): tap = smart cast, hold 150 ms + drag = aim, release = cast, drag back onto the button = cancel
                sw.onAimStart = p => s.HasSkill && GroundAimInput.TouchBegin(s.SkillIndex, p);
                sw.onAimDrag = GroundAimInput.TouchDrag;
                sw.onAimEnd = GroundAimInput.TouchEnd;
                sw.onAimCancel = GroundAimInput.Cancel;
                sw.onPress = () => s.Root.localScale = new Vector3(0.92f, 0.92f, 1f);
                sw.onRelease = () => s.Root.localScale = Vector3.one;
                GameObject cap = RoHud.KeyCap(s.Root, RoHud.TopLeft, 9f, -9f, (i + 1).ToString());
                if (cap != null) slotKeys[i] = cap.GetComponentInChildren<Text>();
            }
            RoHud.KeyCap(ring, new Vector2(0f, 0f), 22f, 22f, InputAdapter.KeyLabel(GameKey.SkillPage)); // Tab: next page of 4
            RectTransform attack = RoHud.Box(safe, "Attack", RoHud.BottomRight, Attack.x, Attack.y, RoHud.AttackSize, RoHud.AttackSize);
            Image attackArt = RoHud.Art(attack, "hud-attack", true);
            NameTags.Occlude(attack);
            Sprite up = RoHud.Piece("hud-attack"), down = RoHud.Piece("hud-attack-pressed");
            PaperHudPress.Hold(attack.gameObject, GameKey.Attack, d => attackArt.sprite = d && down != null ? down : up);
            Caption(attack, "Attack", RoHud.AttackSize * 0.26f, 15);
            RoHud.KeyCap(attack, new Vector2(0.5f, 0f), 0f, 2f, InputAdapter.KeyLabel(GameKey.Attack, 1));

            RectTransform auto = RoHud.Box(safe, "Auto", RoHud.BottomRight, -RoHud.AutoX, RoHud.AutoY, RoHud.AutoSize, RoHud.AutoSize);
            autoArt = RoHud.Art(auto, "hud-auto", true);
            NameTags.Occlude(auto);
            PaperHudPress.Tap(auto.gameObject, () => AutoBattle.Toggle(GameSession.Hero));
            Caption(auto, "Auto", RoHud.AutoSize * 0.14f, 14);
            RoHud.KeyCap(auto, RoHud.TopRight, -9f, -9f, InputAdapter.KeyLabel(GameKey.AutoToggle));

            RectTransform run = RoHud.Box(safe, "Run", RoHud.BottomRight, -RoHud.RunX, RoHud.RunY, RoHud.RunSize, RoHud.RunSize);
            RoHud.Art(run, "hud-run", true);
            NameTags.Occlude(run);
            PaperHudPress.Tap(run.gameObject, () => TouchControls.Press(GameKey.Dash));
            Caption(run, "Run", RoHud.RunSize * 0.14f, 14);
            RoHud.KeyCap(run, RoHud.TopRight, -11f, -11f, InputAdapter.KeyLabel(GameKey.Dash, 1));

            hpCount = Potion(safe, "HpPotion", -RoHud.PotionHpX, true, out hpIcon);
            mpCount = Potion(safe, "MpPotion", -RoHud.PotionMpX, false, out mpIcon);
        }

        void BuildJoystick(RectTransform safe)
        {
            RectTransform zone = UiKit.Fill(safe, "JoystickZone");
            zone.anchorMin = zone.anchorMax = zone.pivot = Vector2.zero;
            zone.anchoredPosition = Vector2.zero;
            zone.sizeDelta = new Vector2(RoHud.JoyX * 2f + 20f, RoHud.JoyY * 2f);
            TouchControls.Img(zone, null, Color.clear, true); // invisible, catches the touches around the base
            RectTransform b = RoHud.Box(zone, "Base", RoHud.BottomLeft, RoHud.JoyX, RoHud.JoyY, RoHud.JoySize, RoHud.JoySize);
            RoHud.Art(b, "hud-joy-base").color = new Color(1f, 1f, 1f, 0.55f); // see-through like the mockup
            NameTags.Occlude(b); // see-through: a label under it would show through
            RectTransform knob = RoHud.Box(b, "Knob", RoHud.Mid, 0f, 0f, RoHud.KnobSize, RoHud.KnobSize);
            RoHud.Art(knob, "hud-joy-knob");
            zone.gameObject.AddComponent<TouchJoystick>().Init(b, knob, RoHud.KnobTravel, (u, r, d, l) =>
            {
                TouchControls.SetHeld(GameKey.Up, u); TouchControls.SetHeld(GameKey.Right, r);
                TouchControls.SetHeld(GameKey.Down, d); TouchControls.SetHeld(GameKey.Left, l);
            });
        }

        /// <summary>White outlined caption on the lower part of a round button (the mockup's "Attack", "Auto", "Run").</summary>
        static void Caption(RectTransform button, string text, float fromBottom, int size)
        {
            RoHud.Label(RoHud.Box(button, "Caption", new Vector2(0.5f, 0f), 0f, fromBottom, button.sizeDelta.x, size + 4f), text, size, Color.white, TextAnchor.MiddleCenter, true, true);
        }

        /// <summary>Round potion button: ivory plate, the RO flask, the navy count badge on its lower right (how many of the size it would drink are left).</summary>
        Text Potion(RectTransform safe, string name, float x, bool hp, out Image icon)
        {
            RectTransform b = RoHud.Box(safe, name, RoHud.BottomRight, x, RoHud.PotionY, RoHud.PotionSize, RoHud.PotionSize);
            RoHud.Art(b, "hud-potion-plate", true);
            NameTags.Occlude(b);
            icon = RoHud.Box(b, "Icon", RoHud.Mid, 0f, 1f, RoHud.PotionSize * 0.72f, RoHud.PotionSize * 0.72f).gameObject.AddComponent<Image>();
            icon.preserveAspect = true; icon.raycastTarget = false;
            RectTransform badge;
            Text count = RoHud.Pill(b, "Count", RoHud.BottomRight, new Vector2(0.5f, 0.5f), -6f, 6f, 20f, "hud-count-badge", null, 0f, "0", 11, Color.white, out badge, 20f);
            badge.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(5, 5, 2, 2); // a round badge: smaller side padding than a pill
            PaperHudPress.Hold(b.gameObject, hp ? GameKey.HpPotion : GameKey.MpPotion);
            RoHud.KeyCap(b, RoHud.TopLeft, 9f, -9f, InputAdapter.KeyLabel(hp ? GameKey.HpPotion : GameKey.MpPotion));
            return count;
        }

        public bool Visible { get { return root.gameObject.activeSelf; } }

        /// <summary>The skill page shown (0 = slots 1-4), for capture logs.</summary>
        internal int Page { get { return page; } }
        public void SetVisible(bool on) { if (root.gameObject.activeSelf != on) root.gameObject.SetActive(on); }

        /// <summary>Next / previous page of 4 skills (Tab, a swipe), wraps.</summary>
        public void Cycle(int dir)
        {
            if (hotbar != null) return; // PC shows all 8 slots
            page = ((page + dir) % Pages + Pages) % Pages;
            version = int.MinValue; // rebind on the next tick
            for (int i = 0; i < PerPage; i++) if (slotKeys[i] != null) slotKeys[i].text = (page * PerPage + i + 1).ToString();
        }

        public void Tick(HeroController hero, float dt)
        {
            PlayerState p = hero.Rpg;
            if (RoHud.Pc && QuestLogWindow.IsOpen && InputAdapter.WindowPressed(GameKey.Quests)) QuestLogWindow.Close(); // N again closes (as the paper HUD)
            Menu.Tick(p, Visible, dt);
            if (!Visible) return;
            if (RoHud.Pc && InputAdapter.Pressed(GameKey.Quests)) QuestLogWindow.Open(); // N (the other HUDs read it in their own controls)
            map.Tick(dt);
            Menu.Fit(Mathf.Max(RoHud.MenuRight, RoHud.Margin + map.PillsWidth + 12f), safe.rect.width); // a long map name pushes the row left; 4:3 shrinks it
            wanted.Tick(true);
            boss.Tick(true);
            if (hotbar != null) { hotbar.Tick(hero); return; }
            int v = SkillMagazine.Version();
            if (hero.Class != cls || v != version)
            {
                cls = hero.Class; version = v;
                for (int i = 0; i < PerPage; i++) slots[i].Bind(p != null ? p.Loadout[page * PerPage + i] : -1);
            }
            foreach (RoHudSkillSlot s in slots) s.Live(hero);
            if (AutoBattle.On != shownAuto) { shownAuto = AutoBattle.On; autoArt.sprite = RoHud.Piece(shownAuto ? "hud-auto-on" : "hud-auto"); }
            if (p == null) return;
            ShowPotion(p, true, hpIcon, hpCount, ref shownHpSize, ref shownHp);
            ShowPotion(p, false, mpIcon, mpCount, ref shownMpSize, ref shownMp);
        }

        internal static void ShowPotion(PlayerState p, bool hp, Image icon, Text count, ref int shownSize, ref int shownCount)
        {
            int size = p.QuickPotionSize(hp), n = p.PotionCount(hp, size);
            if (size != shownSize) { shownSize = size; icon.sprite = RoHud.Piece(hp ? "hud-potion-red" : "hud-potion-blue"); } // the RO flask; the count is of the size it drinks
            if (n != shownCount) { shownCount = n; count.text = n.ToString(); icon.color = n > 0 ? Color.white : new Color(0.7f, 0.7f, 0.72f, 0.6f); }
        }
    }
}
