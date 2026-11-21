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
    /// the navy capsule `hotbar-tray`, `hotbar-attack(-pressed)`, `hotbar-auto(-on)`, `hotbar-run`, `hotbar-divider`. Thin-ring fix 21/11
    /// (docs/reference/ro3-pc-hud/hotbar-thick-ring-fix-chatgpt.md), sheet H (Tools/ui-ro-soft-sheets/ro-hud-h-pc-hotbar-thin.json): the
    /// skill slots (RoHudSkillSlot.Hotbar.cs), `hotbar-potion-ring`, the navy key badge `hotbar-key-corner` on each slot's bottom-right
    /// corner. Sizes in RoHud.Hot* (mockup px x 0.889).
    /// All 8 loadout slots show at once (no Tab page). Placement: RoHud.PcBottomRow (centred, kept right of the chat group, scaled
    /// down on 4:3). The tray raycasts, so a click on it never attacks.
    /// </summary>
    public sealed class RoHudHotbar
    {
        const float Mid = RoHud.HotH * 0.5f;
        // key badge, ChatGPT fix 21/11 (docs/reference/ro3-pc-hud/hotbar-thick-ring-fix-chatgpt.md): 15x13 px, radius 4, font 9-10 semibold
        // white on navy 90 % (`hotbar-key-corner`, 9-slice border 10 art px = 4 units at multiplier 1.25). Skills 1-8 and Q / E: on the
        // slot's bottom-right corner, centre KeyIn from the corner (it pokes 2-3 px past the ring, stays inside the tray); Space under
        // Attack, V / Shift on the top edge, KeyDrop inside.
        const float KeyH = 11.6f, KeyMinW = 13.3f, KeyIn = 6f, KeyDrop = 3f, KeyPpuMul = 1.25f, KeyAlpha = 0.9f;
        const int KeyFont = 9;
        const float PotionIcon = 0.82f; // flask = 80-85 % of the slot
        static readonly Color PotionCore = new Color(0.96f, 0.94f, 0.89f, 1f);

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
            RoHud.Art(tray, "hotbar-tray", true); // raycast: a click on the bar is not an attack (body 78 % see-through, rim opaque: cut with body_alpha)
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
                PaperHudPress.Tap(sl.Root.gameObject, () => { if (sl.HasSkill) { sl.PressedAt = Time.unscaledTime; SkillMagazine.CastRequested(sl.SkillIndex); } });
                Key(sl.Root, (i + 1).ToString(), KeySpot.Corner);
            }
            x -= RoHud.HotGap + s * 0.5f; // right edge of slot 8
            Divider(x + RoHud.HotGapActions * 0.5f);

            x += RoHud.HotGapActions + RoHud.HotAttack * 0.5f;
            RectTransform attack = RoHud.Box(tray, "Attack", RoHud.BottomLeft, x, Mid, RoHud.HotAttack, RoHud.HotAttack);
            Image attackArt = RoHud.Art(attack, "hotbar-attack", true);
            Sprite up = RoHud.Piece("hotbar-attack"), down = RoHud.Piece("hotbar-attack-pressed");
            PaperHudPress.Hold(attack.gameObject, GameKey.Attack, d => attackArt.sprite = d && down != null ? down : up);
            Key(attack, InputAdapter.KeyLabel(GameKey.Attack, 1), KeySpot.Bottom); // "Space" on the lower edge, as the mockup

            x += RoHud.HotAttack * 0.5f + RoHud.HotSmallGap + RoHud.HotSmall * 0.5f;
            RectTransform auto = RoHud.Box(tray, "Auto", RoHud.BottomLeft, x, Mid, RoHud.HotSmall, RoHud.HotSmall);
            autoArt = RoHud.Art(auto, "hotbar-auto", true);
            PaperHudPress.Tap(auto.gameObject, () => AutoBattle.Toggle(GameSession.Hero));
            Key(auto, InputAdapter.KeyLabel(GameKey.AutoToggle), KeySpot.Top);

            x += RoHud.HotSmall + RoHud.HotSmallGap;
            RectTransform run = RoHud.Box(tray, "Run", RoHud.BottomLeft, x, Mid, RoHud.HotSmall, RoHud.HotSmall);
            RoHud.Art(run, "hotbar-run", true);
            PaperHudPress.Tap(run.gameObject, () => TouchControls.Press(GameKey.Dash));
            Key(run, InputAdapter.KeyLabel(GameKey.Dash, 1), KeySpot.Top);
        }

        /// <summary>Thin cream group divider (mockup 2x22 px), centred at `x` of the tray.</summary>
        void Divider(float x)
        {
            RoHud.Art(RoHud.Box(tray, "Divider", RoHud.BottomLeft, x, Mid, 3f, 20f), "hotbar-divider");
        }

        enum KeySpot { Corner, Top, Bottom }

        /// <summary>
        /// Navy key badge (`hotbar-key-corner`, a RoHud.Pill box: padding 0 / 3, fitted to the text, at least KeyMinW) on `button`'s
        /// bottom-right corner, or centred on its top / bottom edge KeyDrop inside. Nothing for an unbound key.
        /// </summary>
        static void Key(RectTransform button, string key, KeySpot spot)
        {
            if (string.IsNullOrEmpty(key)) return;
            RectTransform rt;
            Vector2 anchor = spot == KeySpot.Corner ? RoHud.BottomRight : new Vector2(0.5f, spot == KeySpot.Top ? 1f : 0f);
            float x = spot == KeySpot.Corner ? -KeyIn : 0f, y = spot == KeySpot.Corner ? KeyIn : spot == KeySpot.Top ? -KeyDrop : KeyDrop;
            RoHud.Pill(button, "Key", anchor, RoHud.Mid, x, y, KeyH, "hotbar-key-corner", null, 0f, key, KeyFont, Color.white, out rt, KeyMinW);
            rt.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(3, 3, 0, 0);
            Image art = rt.GetComponent<Image>();
            art.pixelsPerUnitMultiplier = KeyPpuMul; // border 10 art px -> radius 4 units
            art.color = new Color(1f, 1f, 1f, KeyAlpha);
        }

        /// <summary>
        /// Round potion slot in the tray: an ivory core (`hotbar-potion-ring-mask`, the ring's hole) under the RO flask (82 % of the slot)
        /// and the thin cream `hotbar-potion-ring`; the navy count badge (the size it would drink) bottom-left, Q / E bottom-right.
        /// </summary>
        Text Potion(string name, float x, bool hp, out Image icon)
        {
            float s = RoHud.HotSlot;
            RectTransform b = RoHud.Box(tray, name, RoHud.BottomLeft, x, Mid, s, s);
            Image core = RoHud.Art(b, "hotbar-potion-ring-mask", true);
            core.color = PotionCore;
            icon = RoHud.Box(b, "Icon", RoHud.Mid, 0f, 0f, s * PotionIcon, s * PotionIcon).gameObject.AddComponent<Image>();
            icon.preserveAspect = true; icon.raycastTarget = false;
            RoHud.Art(UiKit.Fill(b, "Ring"), "hotbar-potion-ring");
            RectTransform badge;
            Text count = RoHud.Pill(b, "Count", RoHud.BottomLeft, new Vector2(0.5f, 0.5f), 4f, 5f, 14f, "hud-count-badge", null, 0f, "0", 9, Color.white, out badge, 14f);
            badge.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(3, 3, 1, 1);
            PaperHudPress.Hold(b.gameObject, hp ? GameKey.HpPotion : GameKey.MpPotion);
            Key(b, InputAdapter.KeyLabel(hp ? GameKey.HpPotion : GameKey.MpPotion), KeySpot.Corner);
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
            for (int i = 0; i < slots.Length; i++)
            {
                float at = InputAdapter.RoDigitPressedAt[i];
                if (at > slots[i].PressedAt) slots[i].PressedAt = at; // digit key: same gold flash as a click
                slots[i].Live(hero);
            }
            if (AutoBattle.On != shownAuto) { shownAuto = AutoBattle.On; autoArt.sprite = RoHud.Piece(shownAuto ? "hotbar-auto-on" : "hotbar-auto"); }
            if (p == null) return;
            RoHudControls.ShowPotion(p, true, hpIcon, hpCount, ref shownHpSize, ref shownHp);
            RoHudControls.ShowPotion(p, false, mpIcon, mpCount, ref shownMpSize, ref shownMp);
        }
    }
}
