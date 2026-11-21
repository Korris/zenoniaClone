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
    /// All 8 loadout slots show at once (no Tab page). Every piece is kit art painted by ChatGPT: the navy tray `hud-plate` (see-through),
    /// square slots `slot-empty` / `slot`, the round `hud-potion-plate`, `hud-attack`, `hud-auto`, `hud-run`. Placement: RoHud.PcBottomRow
    /// (centred, kept right of the chat group, scaled down on 4:3). The tray raycasts, so a click on it never attacks.
    /// </summary>
    public sealed class RoHudHotbar
    {
        const float Round = RoHud.HotSlot, Mid = RoHud.HotH * 0.5f, TrayAlpha = 0.62f;
        const int CapFont = 9;
        const float CapX = 7f, CapY = 4f; // key cap centre from the slot corner: the 16-high cap stays inside the tray (no poking over its rim)

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
            RoHud.Art(tray, "hud-plate", true).color = new Color(1f, 1f, 1f, TrayAlpha); // raycast: a click on the bar is not an attack
            NameTags.Occlude(tray);

            float x = RoHud.HotPadX + RoHud.HotSlot * 0.5f;
            hpCount = Potion("HpPotion", x, true, out hpIcon);
            mpCount = Potion("MpPotion", x + RoHud.HotPitch, false, out mpIcon);

            x += RoHud.HotPitch + RoHud.HotSlot + RoHud.HotGroupGap;
            for (int i = 0; i < slots.Length; i++)
            {
                RoHudSkillSlot s = slots[i] = new RoHudSkillSlot(tray, "Skill" + i, new Vector2(x + i * RoHud.HotPitch, Mid), RoHud.HotSlot, RoHud.BottomLeft, true);
                PaperHudPress.Tap(s.Root.gameObject, () => { if (s.HasSkill) SkillMagazine.CastRequested(s.SkillIndex); });
                RoHud.KeyCap(s.Root, RoHud.TopLeft, CapX, -CapY, (i + 1).ToString(), CapFont);
            }

            x += (slots.Length - 1) * RoHud.HotPitch + RoHud.HotSlot * 0.5f + RoHud.HotGroupGap + RoHud.HotAttack * 0.5f;
            RectTransform attack = RoHud.Box(tray, "Attack", RoHud.BottomLeft, x, Mid, RoHud.HotAttack, RoHud.HotAttack);
            Image attackArt = RoHud.Art(attack, "hud-attack", true);
            Sprite up = RoHud.Piece("hud-attack"), down = RoHud.Piece("hud-attack-pressed");
            PaperHudPress.Hold(attack.gameObject, GameKey.Attack, d => attackArt.sprite = d && down != null ? down : up);
            RoHud.KeyCap(attack, new Vector2(0.5f, 0f), 0f, 5f, InputAdapter.KeyLabel(GameKey.Attack, 1), CapFont);

            x += RoHud.HotAttack * 0.5f + 6f + Round * 0.5f;
            RectTransform auto = RoHud.Box(tray, "Auto", RoHud.BottomLeft, x, Mid, Round, Round);
            autoArt = RoHud.Art(auto, "hud-auto", true);
            PaperHudPress.Tap(auto.gameObject, () => AutoBattle.Toggle(GameSession.Hero));
            RoHud.KeyCap(auto, RoHud.TopRight, -CapX, -CapY, InputAdapter.KeyLabel(GameKey.AutoToggle), CapFont);

            RectTransform run = RoHud.Box(tray, "Run", RoHud.BottomLeft, x + RoHud.HotPitch, Mid, Round, Round);
            RoHud.Art(run, "hud-run", true);
            PaperHudPress.Tap(run.gameObject, () => TouchControls.Press(GameKey.Dash));
            RoHud.KeyCap(run, RoHud.TopRight, -CapX, -CapY, InputAdapter.KeyLabel(GameKey.Dash, 1), CapFont);
        }

        /// <summary>Round potion button in the tray: ivory plate, RO flask, the navy count badge (the size it would drink), Q / E cap.</summary>
        Text Potion(string name, float x, bool hp, out Image icon)
        {
            RectTransform b = RoHud.Box(tray, name, RoHud.BottomLeft, x, Mid, Round, Round);
            RoHud.Art(b, "hud-potion-plate", true);
            icon = RoHud.Box(b, "Icon", RoHud.Mid, 0f, 1f, Round * 0.72f, Round * 0.72f).gameObject.AddComponent<Image>();
            icon.preserveAspect = true; icon.raycastTarget = false;
            RectTransform badge;
            Text count = RoHud.Pill(b, "Count", RoHud.BottomRight, new Vector2(0.5f, 0.5f), -3f, 4f, 16f, "hud-count-badge", null, 0f, "0", 10, Color.white, out badge, 16f);
            badge.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(4, 4, 1, 1);
            PaperHudPress.Hold(b.gameObject, hp ? GameKey.HpPotion : GameKey.MpPotion);
            RoHud.KeyCap(b, RoHud.TopLeft, CapX, -CapY, InputAdapter.KeyLabel(hp ? GameKey.HpPotion : GameKey.MpPotion), CapFont);
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
            if (AutoBattle.On != shownAuto) { shownAuto = AutoBattle.On; autoArt.sprite = RoHud.Piece(shownAuto ? "hud-auto-on" : "hud-auto"); }
            if (p == null) return;
            RoHudControls.ShowPotion(p, true, hpIcon, hpCount, ref shownHpSize, ref shownHp);
            RoHudControls.ShowPotion(p, false, mpIcon, mpCount, ref shownMpSize, ref shownMp);
        }
    }
}
