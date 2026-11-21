using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PC profile of the RO Soft HUD (RoHud.Pc), RO3 style (docs/reference/willowbrook-25d/willowbrook-25d-vs-ro3-feel-chatgpt.md
    /// section 11: "chuyển hotbar về dưới giữa màn hình ... ưu tiên không gian chiến đấu"). Replaces the mobile cluster (joystick,
    /// Attack disc, skill arc, Auto, Run, potions) with one low tray at the bottom centre, like the RO3 CBT skill bar:
    ///   [HP Q][MP E] | [1][2][3][4][5][6][7][8] | (Attack Space) [Auto V][Run Shift]
    /// Designed by ChatGPT for the RO Soft kit (user 21/11: "kêu chatgpt nó design cho hợp style ro fantasy"): mockup
    /// docs/reference/ro3-pc-hud/pc-hotbar-mockup-chatgpt.png, pieces of sheet G (Tools/ui-ro-soft-sheets/ro-hud-g-pc-hotbar.json):
    /// the navy capsule `hotbar-tray`, round `hotbar-slot-ring` / `-empty` / `-mask`, `hotbar-potion-plate`, `hotbar-attack(-pressed)`,
    /// `hotbar-auto(-on)`, `hotbar-run`, `hotbar-divider`, the navy key badge `hotbar-key`. Sizes in RoHud.Hot* (mockup px x 0.889).
    /// All 8 loadout slots show at once (no Tab page). Placement: RoHud.PcBottomRow (centred, kept right of the chat group, scaled
    /// down on 4:3). The tray raycasts, so a click on it never attacks.
    /// </summary>
    public sealed class RoHudHotbar
    {
        const float Mid = RoHud.HotH * 0.5f;
        // key badge (mockup: 18x12 px, 9 px semibold, centred on the slot's top edge): 12 units high, font 8, padding 1 / 5,
        // its centre KeyDrop under the slot top so it stays inside the tray rim (docs/ui-layout-rules.md Sổ lỗi 21/11)
        const float KeyH = 12f, KeyDrop = 3f, KeyArtH = 28f;
        const int KeyFont = 8;

        readonly RectTransform tray, safe;
        readonly RoHudSkillSlot[] slots = new RoHudSkillSlot[SkillLoadout.Slots];
        readonly Image autoArt, hpIcon, mpIcon;
        readonly Text hpCount, mpCount;
        int version = int.MinValue, shownHp = -1, shownMp = -1, shownHpSize = -1, shownMpSize = -1;
        bool shownAuto;
        float shownW = -1f;
        HeroClassDef cls;

        public RoHudHotbar(RectTransform safe)
        {
            this.safe = safe;
            tray = RoHud.Box(safe, "RoHudHotbar", RoHud.BottomLeft, 0f, 0f, RoHud.HotW, RoHud.HotH);
            tray.pivot = Vector2.zero;
            tray.anchoredPosition = new Vector2(RoHud.Margin, RoHud.HotBottom);
            RoHud.Art(tray, "hotbar-tray", true); // raycast: a click on the bar is not an attack
            NameTags.Occlude(tray);

            float s = RoHud.HotSlot, x = RoHud.HotPadX + s * 0.5f;
            hpCount = Potion("HpPotion", x, true, out hpIcon);
            x += s + RoHud.HotPotionGap;
            mpCount = Potion("MpPotion", x, false, out mpIcon);
            x += s * 0.5f;
            Divider(x + RoHud.HotGapPotions * 0.5f);

            x += RoHud.HotGapPotions + s * 0.5f;
            for (int i = 0; i < slots.Length; i++, x += s + RoHud.HotGap)
            {
                RoHudSkillSlot sl = slots[i] = new RoHudSkillSlot(tray, "Skill" + i, new Vector2(x, Mid), s, RoHud.BottomLeft, true);
                PaperHudPress.Tap(sl.Root.gameObject, () => { if (sl.HasSkill) SkillMagazine.CastRequested(sl.SkillIndex); });
                Key(sl.Root, (i + 1).ToString());
            }
            x -= RoHud.HotGap + s * 0.5f; // right edge of slot 8
            Divider(x + RoHud.HotGapActions * 0.5f);

            x += RoHud.HotGapActions + RoHud.HotAttack * 0.5f;
            RectTransform attack = RoHud.Box(tray, "Attack", RoHud.BottomLeft, x, Mid, RoHud.HotAttack, RoHud.HotAttack);
            Image attackArt = RoHud.Art(attack, "hotbar-attack", true);
            Sprite up = RoHud.Piece("hotbar-attack"), down = RoHud.Piece("hotbar-attack-pressed");
            PaperHudPress.Hold(attack.gameObject, GameKey.Attack, d => attackArt.sprite = d && down != null ? down : up);
            Key(attack, InputAdapter.KeyLabel(GameKey.Attack, 1), false); // "Space" on the lower edge, as the mockup

            x += RoHud.HotAttack * 0.5f + RoHud.HotSmallGap + RoHud.HotSmall * 0.5f;
            RectTransform auto = RoHud.Box(tray, "Auto", RoHud.BottomLeft, x, Mid, RoHud.HotSmall, RoHud.HotSmall);
            autoArt = RoHud.Art(auto, "hotbar-auto", true);
            PaperHudPress.Tap(auto.gameObject, () => AutoBattle.Toggle(GameSession.Hero));
            Key(auto, InputAdapter.KeyLabel(GameKey.AutoToggle));

            x += RoHud.HotSmall + RoHud.HotSmallGap;
            RectTransform run = RoHud.Box(tray, "Run", RoHud.BottomLeft, x, Mid, RoHud.HotSmall, RoHud.HotSmall);
            RoHud.Art(run, "hotbar-run", true);
            PaperHudPress.Tap(run.gameObject, () => TouchControls.Press(GameKey.Dash));
            Key(run, InputAdapter.KeyLabel(GameKey.Dash, 1));
        }

        /// <summary>Thin cream group divider (mockup 2x22 px), centred at `x` of the tray.</summary>
        void Divider(float x)
        {
            RoHud.Art(RoHud.Box(tray, "Divider", RoHud.BottomLeft, x, Mid, 3f, 20f), "hotbar-divider");
        }

        /// <summary>
        /// Navy key badge (`hotbar-key`, a RoHud.Pill box: padding 1 / 5, fitted to the text, round ends at this height) centred on the
        /// top edge of `button` (or its bottom edge), KeyDrop inside. Nothing for an unbound key.
        /// </summary>
        static void Key(RectTransform button, string key, bool top = true)
        {
            if (string.IsNullOrEmpty(key)) return;
            RectTransform rt;
            RoHud.Pill(button, "Key", new Vector2(0.5f, top ? 1f : 0f), RoHud.Mid, 0f, top ? -KeyDrop : KeyDrop, KeyH, "hotbar-key", null, 0f, key, KeyFont, Color.white, out rt, 16f);
            rt.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(5, 5, 1, 1);
            rt.GetComponent<Image>().pixelsPerUnitMultiplier = KeyArtH / (2f * KeyH); // border = half the height: round ends
        }

        /// <summary>Round potion slot in the tray: the ivory `hotbar-potion-plate`, RO flask, the navy count badge (the size it would drink), Q / E.</summary>
        Text Potion(string name, float x, bool hp, out Image icon)
        {
            float s = RoHud.HotSlot;
            RectTransform b = RoHud.Box(tray, name, RoHud.BottomLeft, x, Mid, s, s);
            RoHud.Art(b, "hotbar-potion-plate", true);
            icon = RoHud.Box(b, "Icon", RoHud.Mid, 0f, 0f, s * 0.7f, s * 0.7f).gameObject.AddComponent<Image>();
            icon.preserveAspect = true; icon.raycastTarget = false;
            RectTransform badge;
            Text count = RoHud.Pill(b, "Count", RoHud.BottomRight, new Vector2(0.5f, 0.5f), -4f, 5f, 14f, "hud-count-badge", null, 0f, "0", 9, Color.white, out badge, 14f);
            badge.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(3, 3, 1, 1);
            PaperHudPress.Hold(b.gameObject, hp ? GameKey.HpPotion : GameKey.MpPotion);
            Key(b, InputAdapter.KeyLabel(hp ? GameKey.HpPotion : GameKey.MpPotion));
            return count;
        }

        public void Tick(HeroController hero)
        {
            float w = safe.rect.width;
            if (Mathf.Abs(w - shownW) > 0.5f) // window resize: same rule as the chat group (RoHudChatBar), so they never overlap
            {
                shownW = w;
                float barW, scale;
                tray.anchoredPosition = new Vector2(RoHud.PcBottomRow(w, RoHud.ChatBubbleSize + 6f, out barW, out scale), RoHud.HotBottom);
                tray.localScale = new Vector3(scale, scale, 1f);
            }
            PlayerState p = hero.Rpg;
            int v = SkillMagazine.Version();
            if (hero.Class != cls || v != version)
            {
                cls = hero.Class; version = v;
                for (int i = 0; i < slots.Length; i++) slots[i].Bind(p != null ? p.Loadout[i] : -1);
            }
            foreach (RoHudSkillSlot s in slots) s.Live(hero);
            if (AutoBattle.On != shownAuto) { shownAuto = AutoBattle.On; autoArt.sprite = RoHud.Piece(shownAuto ? "hotbar-auto-on" : "hotbar-auto"); }
            if (p == null) return;
            RoHudControls.ShowPotion(p, true, hpIcon, hpCount, ref shownHpSize, ref shownHp);
            RoHudControls.ShowPotion(p, false, mpIcon, mpCount, ref shownMpSize, ref shownMp);
        }
    }
}
