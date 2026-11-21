using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PC hotbar look of a skill slot (RoHudHotbar), ChatGPT fix "viền tròn dày quá" (docs/reference/ro3-pc-hud/hotbar-thick-ring-fix-chatgpt.md):
    /// a thin steel-blue ring (`hotbar-ring-thin`, sheet H Tools/ui-ro-soft-sheets/ro-hud-h-pc-hotbar-thin.json) over the icon masked by
    /// its hole (`hotbar-ring-thin-mask`, same canvas: no inset), so the icon fills ~91 % of the slot. States:
    ///   ready        full-colour icon, steel-blue ring
    ///   pressed      gold ring `hotbar-ring-selected` for PressFlash s after a digit key / click (the "selected" ring of the spec)
    ///   upgradable   5 px gold dot `hotbar-dot` top-right (a free skill point can raise it now)
    ///   cooldown     navy-black radial wipe at 43 % + white seconds with a 1 px outline; the icon itself stays bright
    ///   short of MP  icon at 80 % + navy disc 18 % + the crossed MP drop `hotbar-mp-crossed` bottom-left
    ///   not learned  icon at 72 % brightness, a little grey, small lock `hotbar-lock` bottom-left (never the cooldown wash)
    ///   empty        `hotbar-slot-empty-thin`: pale #D8E2E8 core, faint 4-point star
    /// </summary>
    public sealed partial class RoHudSkillSlot
    {
        static readonly Color HotSweep = new Color(0.04f, 0.07f, 0.12f, 0.43f), HotNoMp = new Color(0.2f, 0.34f, 0.42f, 0.18f);
        static readonly Color HotNoMpIcon = new Color(0.8f, 0.8f, 0.8f, 1f), HotLockedIcon = new Color(0.72f, 0.72f, 0.75f, 1f);
        const float PressFlash = 0.18f, GlyphH = 9f, DotSize = 4.5f, CornerIn = 6f; // CornerIn: glyph centre from the slot corner (on the ring at 45 deg)

        /// <summary>Time (unscaled) of the last digit key / click on this slot: shows the gold ring briefly.</summary>
        public float PressedAt = -1f;
        Image lockGlyph, mpGlyph, dot;
        bool locked, shownPressed, shownDot;

        void BuildHotbar(float size)
        {
            icon = RoHud.Masked(Root, "hotbar-ring-thin-mask", 0f);
            backing = icon.transform.parent.GetComponent<Image>();
            backing.color = RoSoftTheme.SkyMist; // backing under icons with transparent corners
            icon.transform.parent.GetComponent<Mask>().showMaskGraphic = true;
            sweep = UiKit.Fill(icon.transform.parent, "Sweep").gameObject.AddComponent<Image>();
            sweep.sprite = RoHud.Piece("hotbar-ring-thin-mask");
            sweep.type = Image.Type.Filled; sweep.fillMethod = Image.FillMethod.Radial360; sweep.fillOrigin = (int)Image.Origin360.Top; sweep.fillClockwise = false;
            sweep.color = HotSweep; sweep.raycastTarget = false; sweep.enabled = false;
            Image tint = UiKit.Fill(icon.transform.parent, "NoMp").gameObject.AddComponent<Image>();
            tint.sprite = RoHud.Piece("hotbar-ring-thin-mask"); tint.color = HotNoMp; tint.raycastTarget = false;
            noMp = tint.gameObject;
            noMp.SetActive(false);
            ring = RoHud.Art(UiKit.Fill(Root, "Ring"), "hotbar-ring-thin");
            lockGlyph = Glyph("Lock", "hotbar-lock", CornerIn, CornerIn, GlyphH);
            mpGlyph = Glyph("NoMpDrop", "hotbar-mp-crossed", CornerIn, CornerIn, GlyphH);
            dot = Glyph("Upgrade", "hotbar-dot", size - CornerIn, size - CornerIn, DotSize);
            cdText = RoHud.Label(UiKit.Fill(Root, "Cd"), "", 12, Color.white, TextAnchor.MiddleCenter, true, true); // 13-14 px at 1280x720
            Outline o = cdText.GetComponent<Outline>();
            if (o != null) { o.effectColor = new Color(0.05f, 0.08f, 0.14f, 0.9f); o.effectDistance = new Vector2(0.9f, -0.9f); }
        }

        /// <summary>Small painted glyph centred at (x, y) from the slot's bottom-left, `h` units high, hidden at first.</summary>
        Image Glyph(string name, string piece, float x, float y, float h)
        {
            Image g = RoHud.Art(RoHud.Box(Root, name, RoHud.BottomLeft, x, y, h, h), piece);
            g.preserveAspect = true;
            g.enabled = false;
            return g;
        }

        void BindHotbar(int index)
        {
            PlayerState p = RpgSession.Player;
            SkillDef d = p != null && index >= 0 ? SkillTable.Get(p.ClassId, index) : null;
            locked = d != null && p.Skills[index] <= 0; // the loadout keeps learned skills only, so this shows if that ever changes
            def = locked ? null : d;
            shownTenths = -1; shownLook = -1; shownPressed = false; shownDot = false;
            cdText.text = "";
            sweep.enabled = false;
            noMp.SetActive(false);
            mpGlyph.enabled = false; dot.enabled = false;
            lockGlyph.enabled = locked;
            icon.sprite = d != null ? SkillIcons.Get(d) : null;
            icon.enabled = icon.sprite != null;
            icon.color = locked ? HotLockedIcon : Color.white;
            ring.sprite = RoHud.Piece(d != null ? "hotbar-ring-thin" : "hotbar-slot-empty-thin");
            backing.color = d != null ? RoSoftTheme.SkyMist : Color.clear; // the empty disc paints its own core
            if (def != null) mpCost = SkillCooldowns.DevFreeMp ? 0 : def.MpAt(p.Skills[def.Index]); // same cost as the cast (HeroController.Skill)
        }

        void LiveHotbar(HeroController hero)
        {
            if (def == null) return;
            bool pressed = Time.unscaledTime - PressedAt < PressFlash;
            if (pressed != shownPressed) { shownPressed = pressed; ring.sprite = RoHud.Piece(pressed ? "hotbar-ring-selected" : "hotbar-ring-thin"); }
            PlayerState p = hero.Rpg;
            bool up = p != null && p.Skills.CanLearn(def, p.Level) == LearnResult.Ok;
            if (up != shownDot) { shownDot = up; dot.enabled = up; }
            float left = hero.SkillCdLeftMs(def.Index);
            bool enoughMp = hero.Mp >= mpCost, cooling = left > 0f;
            int look = (cooling ? 1 : 0) + (enoughMp ? 0 : 2);
            if (look != shownLook)
            {
                shownLook = look;
                icon.color = enoughMp ? Color.white : HotNoMpIcon; // cooldown darkens through the wipe only
                sweep.enabled = cooling;
                noMp.SetActive(!enoughMp);
                mpGlyph.enabled = !enoughMp;
            }
            if (cooling) sweep.fillAmount = Mathf.Clamp01(left / Mathf.Max(1f, def.CooldownMs));
            int tenths = cooling ? Mathf.CeilToInt(left / 100f) : 0;
            if (tenths == shownTenths) return;
            shownTenths = tenths;
            cdText.text = tenths > 0 ? PaperSkillSlot.Seconds(tenths) : "";
        }
    }
}
