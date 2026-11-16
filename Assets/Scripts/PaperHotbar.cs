using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PC hotbar of the compact HUD (hud-compact-pc-v1.png), one slim row bottom centre: the HP / MP potions, attack, the three skill
    /// slots of the current magazine page, Dash, then the page label ("Tab 2/3") with its three dots; the name of its key on top of
    /// each slot, read from the live bindings (InputAdapter.KeyLabel); the thin EXP line right under the row (PaperHotbar.Exp.cs).
    /// Every slot is also clickable. Same input as the old web hotbar (TouchControls.WebHotbar.cs): keys, clicks, the Warrior's farm
    /// hold, Tab / a click on the page label for the next page.
    /// On a window too narrow to clear the chat lines the bar moves right, and shrinks when even that is not enough.
    /// </summary>
    public sealed partial class PaperHotbar
    {
        const float Slot = 44f, Near = 5f, Wide = 11f, PageW = 54f, Pad = 7f; // 44 units = 6.9 % of the screen height
        public const float Width = Pad + Slot * 7f + Near * 4f + Wide * 2f + Near + PageW + Pad; // 422
        const float RowBottom = 17f, SlotY = RowBottom + Slot * 0.5f, KeyY = RowBottom + Slot + 2f, Height = KeyY + 8f;
        const float Lift = 12f; // the whole bar off the screen's bottom edge: it sat too close to it (user, 16/11/2026)
        const float ChatRight = PaperChatFeedView.WebWidth + 12f, EdgeGap = 6f; // the chat lines end there on the PC layout
        static readonly Vector2 Origin = Vector2.zero, BottomCentre = new Vector2(0.5f, 0f);

        readonly RectTransform root, safe;
        readonly PaperPotionButton hp, mp;
        readonly PaperSkillSlot[] slots = new PaperSkillSlot[SkillMagazine.SlotCount];
        readonly Image[] dots = new Image[SkillMagazine.MaxPages];
        readonly Image dashIcon;
        readonly string pageKey;
        Text pageLabel;
        HeroClassDef cls, dashClass;
        int version, page = -1;
        float shownCanvasWidth = -1f;

        public PaperHotbar(RectTransform layer)
        {
            safe = layer;
            root = UiWinKit.Anchored(layer, "PaperHotbar", BottomCentre, BottomCentre, 0f, Lift, Width, Height);
            RectTransform band = UiWinKit.Anchored(root, "Band", BottomCentre, BottomCentre, 0f, RowBottom - 4f, Width, Slot + 8f);
            PaperArt.Plate(band, new Color(0.04f, 0.05f, 0.08f, 0.34f), Color.clear, 10f, true); // raycast: a click on the bar never attacks

            float x = Pad + Slot * 0.5f;
            hp = new PaperPotionButton(root, "HpPotion", Origin, x, SlotY, Slot, true, true);
            Key(x, InputAdapter.KeyLabel(GameKey.HpPotion));
            x += Slot + Near;
            mp = new PaperPotionButton(root, "MpPotion", Origin, x, SlotY, Slot, false, true);
            Key(x, InputAdapter.KeyLabel(GameKey.MpPotion));
            x += Slot + Wide;
            PaperHudCompact.Plate(TouchControls.Place(root, "AttackSlot", Origin, x, SlotY, Slot), PaperPiece.CHotbarSlot, 8f);
            PaperTouchPad.BuildAttack(root, Origin, new Vector2(x, SlotY), Slot, Slot - 8f);
            Key(x, "LMB");
            x += Slot + Wide;
            for (int i = 0; i < slots.Length; i++, x += Slot + Near)
            {
                AddSkill(i, x);
                Key(x, InputAdapter.KeyLabel(GameKey.SlotKey1 + i));
            }
            bool dashArt = PaperArt.Has(PaperPiece.CDash); // the cut button is round: it sits in a slot like the attack button
            if (dashArt) PaperHudCompact.Plate(TouchControls.Place(root, "DashSlot", Origin, x, SlotY, Slot), PaperPiece.CHotbarSlot, 8f);
            dashIcon = PaperTouchPad.BuildDash(root, Origin, new Vector2(x, SlotY), dashArt ? Slot - 8f : Slot, 8f);
            Key(x, InputAdapter.KeyLabel(GameKey.Dash, 1)); // "Shift" (K, the first key of the row, dashes too)
            pageKey = InputAdapter.KeyLabel(GameKey.SkillPage);
            BuildPage(x + Slot * 0.5f + Near + PageW * 0.5f);
            BuildExp();
        }

        void Key(float x, string label) { PaperHudCompact.KeyLabel(root, Origin, x, KeyY, label); }

        /// <summary>"Tab 2/3" over the three page dots; a click shows the next page.</summary>
        void BuildPage(float x)
        {
            RectTransform cell = TouchControls.Place(root, "Page", Origin, x, SlotY, PageW);
            cell.sizeDelta = new Vector2(PageW, Slot);
            TouchControls.Img(cell, null, Color.clear, true);
            pageLabel = PaperTheme.Title(cell, "Label", 0f, 5f, PageW, 16f, "", 12, PaperTheme.TextLight, PaperInk.World, TextAnchor.MiddleCenter);
            pageLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            for (int i = 0; i < dots.Length; i++)
            {
                RectTransform dot = TouchControls.Place(cell, "PageDot" + i, new Vector2(0.5f, 0.5f), (i - (dots.Length - 1) * 0.5f) * 13f, -9f, 9f);
                dots[i] = PaperArt.Draw(dot, PaperPiece.CPageDotOff, PaperHudCompact.Dark, PaperHudCompact.Rim, 4.5f);
            }
            PaperHudPress.Tap(cell.gameObject, () => CyclePage(1));
        }

        void AddSkill(int i, float x)
        {
            PaperSkillSlot s = slots[i] = new PaperSkillSlot(root, i, Origin, new Vector2(x, SlotY), Slot, true);
            var press = s.root.gameObject.AddComponent<SwipeDetector>();
            bool holdingFarm = false;
            press.onTap = () =>
            {
                if (s.entry.Locked) return;
                if (s.entry.IsSkill) SkillMagazine.CastRequested(s.entry.SkillIndex);
                else TouchControls.Press(s.entry.Key);
            };
            press.onHold = () =>
            {
                if (cls == null || cls.Id != HeroClass.Warrior || !s.entry.IsSkill || s.entry.SkillIndex != 0) return false;
                TouchControls.SetHeld(GameKey.Skill1, true);
                holdingFarm = true;
                return true;
            };
            press.onRelease = () => { if (holdingFarm) TouchControls.SetHeld(GameKey.Skill1, false); holdingFarm = false; };
        }

        public PaperPotionButton Potion(bool hpPotion) { return hpPotion ? hp : mp; }

        public void CyclePage(int dir)
        {
            HeroController hero = GameSession.Hero;
            if (hero != null) SkillMagazine.Cycle(hero.Class, dir);
        }

        public void Tick(HeroController hero, float dt)
        {
            HeroClassDef c = hero.Class;
            int v = SkillMagazine.Version(), now = SkillMagazine.CurrentPage(c);
            if (c != cls || v != version || now != page)
            {
                cls = c; version = v; page = now;
                for (int i = 0; i < slots.Length; i++) { slots[i].Bind(c, page, i); slots[i].Pose(0f, 1f, 1f); }
                pageLabel.text = pageKey + " " + (page + 1) + "/" + SkillMagazine.PageCount(c);
                PaperSkillArc.ShowPage(dots, page);
            }
            foreach (PaperSkillSlot s in slots) s.Live(hero);
            PaperTouchPad.ShowDash(dashIcon, c, ref dashClass);
            Fit();
            PlayerState p = hero.Rpg;
            if (p == null) return;
            hp.Tick(p, dt);
            mp.Tick(p, dt);
            TickExp(p);
        }

        /// <summary>Keeps clear of the chat lines: shifts right until the left end passes them, and scales down when the window is narrower than chat + bar.</summary>
        void Fit()
        {
            float w = safe.rect.width;
            if (Mathf.Approximately(w, shownCanvasWidth)) return;
            shownCanvasWidth = w;
            float room = w - ChatRight - EdgeGap, scale = Mathf.Min(1f, room / Width);
            float shift = Mathf.Max(0f, ChatRight + Width * scale * 0.5f - w * 0.5f);
            root.localScale = new Vector3(scale, scale, 1f);
            root.anchoredPosition = new Vector2(shift, Lift);
        }
    }
}
