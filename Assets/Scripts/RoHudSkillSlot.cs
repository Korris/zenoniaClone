using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// One skill button of the RO Soft HUD's arc (02-hud.png): the skill icon masked round inside the ivory ring. States, read from the
    /// hero's own cooldowns (server-confirmed online) and MP like the other skill views:
    ///   ready      full-colour icon
    ///   cooldown   dark radial sweep + "6.6s"
    ///   no MP      icon tinted blue-grey, a small red "MP" pill
    ///   empty      the ring alone, dimmed (nothing assigned to this loadout slot; assign in the skill window)
    /// Tap = cast (SkillMagazine.CastRequested, the same path as every other skill button). Texts change only when their value does.
    /// </summary>
    public sealed partial class RoHudSkillSlot
    {
        static readonly Color CooldownTint = new Color(0.55f, 0.55f, 0.58f, 1f), NoMpTint = new Color(0.55f, 0.62f, 0.8f, 0.85f);
        static readonly Color SweepColor = new Color(0.05f, 0.08f, 0.14f, 0.6f), EmptyRing = new Color(1f, 1f, 1f, 0.55f);

        public readonly RectTransform Root;
        Image icon, sweep, ring, backing, placeholder;
        Text cdText;
        GameObject noMp;
        readonly bool hotbar;
        SkillDef def;
        int mpCost, shownTenths = -1, shownLook = -1;

        /// <param name="corner">Anchor of `centre` in `parent` (the arc: bottom-right; the PC hotbar: its tray's bottom-left).</param>
        /// <param name="hotbar">PC hotbar slot (RoHudHotbar): thin ring look of RoHudSkillSlot.Hotbar.cs.</param>
        public RoHudSkillSlot(Transform parent, string name, Vector2 centre, float size, Vector2? corner = null, bool hotbar = false)
        {
            this.hotbar = hotbar;
            Root = RoHud.Box(parent, name, corner ?? RoHud.BottomRight, centre.x, centre.y, size, size);
            TouchControls.Img(Root, null, Color.clear, true); // the touch target
            if (hotbar) { BuildHotbar(size); return; }
            icon = RoHud.Masked(Root, "hud-skill-mask", size * 0.1f);
            backing = icon.transform.parent.GetComponent<Image>();
            backing.color = RoSoftTheme.SkyMist; // backing under icons with transparent corners
            icon.transform.parent.GetComponent<Mask>().showMaskGraphic = true;
            sweep = UiKit.Fill(icon.transform.parent, "Sweep").gameObject.AddComponent<Image>();
            sweep.sprite = RoHud.Piece("hud-skill-mask");
            sweep.type = Image.Type.Filled; sweep.fillMethod = Image.FillMethod.Radial360; sweep.fillOrigin = (int)Image.Origin360.Top; sweep.fillClockwise = false;
            sweep.color = SweepColor; sweep.raycastTarget = false; sweep.enabled = false;
            ring = RoHud.Art(UiKit.Fill(Root, "Ring"), "hud-skill-ring");
            placeholder = RoHud.Art(RoHud.Box(Root, "Empty", RoHud.Mid, 0f, 0f, size * 0.36f, size * 0.36f), "hud-icon-plus");
            placeholder.color = new Color(1f, 1f, 1f, 0.4f); // a free loadout slot: assign a skill in the skill window
            cdText = RoHud.Label(UiKit.Fill(Root, "Cd"), "", Mathf.RoundToInt(size * 0.26f), Color.white, TextAnchor.MiddleCenter, true, true);
            RectTransform pill;
            RoHud.Pill(Root, "NoMp", new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), 0f, 2f, 16f, "hud-wanted-tag", null, 0f, "MP", 10, Color.white, out pill);
            noMp = pill.gameObject;
            noMp.SetActive(false);
        }

        /// <summary>Shows tree skill `index` (or nothing: -1 / not learned).</summary>
        public void Bind(int index)
        {
            if (hotbar) { BindHotbar(index); return; }
            PlayerState p = RpgSession.Player;
            def = p != null && index >= 0 && p.Skills[index] > 0 ? SkillTable.Get(p.ClassId, index) : null;
            shownTenths = -1; shownLook = -1;
            cdText.text = "";
            sweep.enabled = false;
            noMp.SetActive(false);
            icon.sprite = def != null ? SkillIcons.Get(def) : null;
            icon.enabled = icon.sprite != null;
            icon.color = Color.white;
            ring.color = def != null ? Color.white : EmptyRing;
            if (placeholder.sprite != null) placeholder.enabled = def == null;
            backing.color = def != null ? RoSoftTheme.SkyMist : new Color(1f, 1f, 1f, 0.18f); // an empty slot is a faint glass disc
            if (def != null) mpCost = def.MpAt(p.Skills[def.Index]);
        }

        public bool HasSkill { get { return def != null; } }
        public int SkillIndex { get { return def != null ? def.Index : -1; } }

        public void Live(HeroController hero)
        {
            if (hotbar) { LiveHotbar(hero); return; }
            if (def == null) return;
            float left = hero.SkillCdLeftMs(def.Index);
            bool enoughMp = hero.Mp >= mpCost, cooling = left > 0f;
            int look = (cooling ? 1 : 0) + (enoughMp ? 0 : 2);
            if (look != shownLook)
            {
                shownLook = look;
                icon.color = cooling ? CooldownTint : enoughMp ? Color.white : NoMpTint;
                sweep.enabled = cooling;
                noMp.SetActive(!enoughMp);
            }
            if (cooling) sweep.fillAmount = Mathf.Clamp01(left / Mathf.Max(1f, def.CooldownMs));
            int tenths = cooling ? Mathf.CeilToInt(left / 100f) : 0;
            if (tenths == shownTenths) return;
            shownTenths = tenths;
            cdText.text = tenths > 0 ? PaperSkillSlot.Seconds(tenths) : "";
        }
    }
}
