using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Top-left status of the RO Soft HUD (02-hud.png): the hero's round portrait (the live layered hero, RoHudPortraits) in the ivory ring with the star, the navy "Lv. N" pill,
    /// and on the ivory plate HP / MP bars with "cur / max" and the EXP bar with its percentage. Under the plate: StatusIcons, the row of
    /// buff / status icons (HudBuffRow; other status kinds such as poison add their icons to this anchor). Texts change only when their
    /// numbers change. Built and ticked by RpgHud while RoHud.Active.
    /// </summary>
    public sealed class RoHudStatus
    {
        const float RowX = 92f, LabelW = 34f, BarX = RowX + LabelW, BarW = 182f, BarH = 18f, RowTop = 36f, RowPitch = 23f;
        const float PlateTop = 16f, LvX = 90f, LvY = 6f, LvW = 72f, LvH = 24f;
        /// <summary>Top of the status icon row, units below the top of the safe area (RoHudParty starts under it).</summary>
        public const float IconsTop = PlateTop + RoHud.StatusPlateH + 3f, IconSize = 18f;
        public const float Bottom = IconsTop + IconSize;

        public readonly RectTransform Root;
        /// <summary>Anchor of the status icons under the HP bar (buffs now; poison and other states append here).</summary>
        public readonly RectTransform StatusIcons;
        readonly Image face;
        readonly HeroPortraitLayers portrait; // painted fallback (a class without layered art)
        readonly RawImage live;               // the real layered hero (RoHudPortraits slot 0)
        readonly Text level, hpText, mpText, expText;
        readonly RoHud.Bar hp, mp, exp;
        readonly HudBuffRow buffs;
        int lastLevel = -1, lastHp = -1, lastMaxHp = -1, lastMp = -1, lastMaxMp = -1, lastExp = -1, lastTenths = -1;
        HeroController lastHero;
        string lastLook;
        bool laidOut;

        public RoHudStatus(RectTransform safe)
        {
            Root = UiKit.Rect(safe, "RoHudStatus", RoHud.Margin, 4f, RoHud.StatusPlateX + RoHud.StatusPlateW, Bottom);
            RectTransform plate = RoHud.TopLeftBox(Root, "Plate", RoHud.StatusPlateX, PlateTop, RoHud.StatusPlateW, RoHud.StatusPlateH);
            RoHud.Art(plate, "hud-status-plate");

            // Portrait: white mask plate (a soft sky backing), the hero's face layers, the ring with its star on top.
            RectTransform ring = RoHud.TopLeftBox(Root, "Portrait", 0f, 0f, RoHud.PortraitSize, RoHud.PortraitSize);
            RectTransform m = UiKit.Fill(ring, "Mask", 6f);
            Image mask = RoHud.Art(m, "hud-portrait-mask");
            mask.preserveAspect = false;
            mask.color = RoSoftTheme.SkyMist;
            m.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            face = UiKit.Fill(m, "Face").gameObject.AddComponent<Image>();
            face.raycastTarget = false;
            face.rectTransform.localScale = new Vector3(0.88f, 0.88f, 1f); // a bust like 02-hud.png, not a face crop
            face.rectTransform.anchoredPosition = new Vector2(0f, -5f);
            portrait = new HeroPortraitLayers(face);
            live = RoHudPortraits.Make(m, 0);
            RoHud.Art(UiKit.Fill(ring, "Ring"), "hud-portrait-ring");

            RectTransform lv;
            level = RoHud.Pill(Root, "Level", RoHud.TopLeft, RoHud.TopLeft, LvX, -LvY, LvH, "hud-lv-badge", null, 0f, "Lv. 1", 14, RoHud.TextOnNavy, out lv, LvW);

            hp = Row("Hp", 0, "HP", "hud-bar-red", out hpText);
            mp = Row("Mp", 1, "MP", "hud-bar-blue", out mpText);
            exp = Row("Exp", 2, "EXP", "hud-bar-teal", out expText);

            StatusIcons = RoHud.TopLeftBox(Root, "StatusIcons", RowX, IconsTop, BarW + LabelW, IconSize);
            buffs = new HudBuffRow(StatusIcons, 0f, 0f, IconSize);
        }

        RoHud.Bar Row(string name, int i, string caption, string fill, out Text value)
        {
            float y = RowTop + i * RowPitch;
            RoHud.Label(RoHud.TopLeftBox(Root, name + "Label", RowX, y - 1f, LabelW, BarH + 2f), caption, 14, RoHud.Ink, TextAnchor.MiddleLeft);
            var bar = new RoHud.Bar(Root, name, BarX, y, BarW, BarH, "hud-bar-track", fill);
            value = RoHud.Label(UiKit.Fill(bar.Root, "Value"), "", 13, Color.white, TextAnchor.MiddleCenter, true, true);
            return bar;
        }

        public void SetActive(bool on)
        {
            if (Root.gameObject.activeSelf != on) Root.gameObject.SetActive(on);
            if (!on) RoHudPortraits.Idle();
        }

        public void Refresh(HeroController hero, PlayerState p)
        {
            if (!laidOut) { laidOut = true; hp.Relayout(); mp.Relayout(); exp.Relayout(); }
            bool real = RoHudPortraits.Show(live, 0, hero); // follows hairstyle / colours / hat changes by itself
            if (face.gameObject.activeSelf == real) face.gameObject.SetActive(!real);
            string look = hero.Class.Name + hero.PlayerName; // a class switch or a new look
            if (!real && (hero != lastHero || look != lastLook)) { lastHero = hero; lastLook = look; portrait.Show(hero); }
            if (p.Level != lastLevel) { lastLevel = p.Level; level.text = "Lv. " + p.Level; lastExp = -1; }
            int h = Mathf.CeilToInt(p.Hp), m = Mathf.FloorToInt(p.Mp);
            if (h != lastHp || p.MaxHp != lastMaxHp) { lastHp = h; lastMaxHp = p.MaxHp; hpText.text = h + " / " + p.MaxHp; }
            if (m != lastMp || p.MaxMp != lastMaxMp) { lastMp = m; lastMaxMp = p.MaxMp; mpText.text = m + " / " + p.MaxMp; }
            hp.Set(p.MaxHp > 0 ? p.Hp / p.MaxHp : 0f);
            mp.Set(p.MaxMp > 0 ? p.Mp / p.MaxMp : 0f);
            if (p.Exp != lastExp)
            {
                lastExp = p.Exp;
                float r = p.Level >= Leveling.MaxLevel ? 1f : Leveling.ProgressRatio(p.Level, p.Exp);
                exp.Set(r);
                int tenths = Mathf.FloorToInt(r * 1000f);
                if (tenths != lastTenths) { lastTenths = tenths; expText.text = p.Level >= Leveling.MaxLevel ? "MAX" : (tenths / 10) + "." + (tenths % 10) + "%"; }
            }
            buffs.Refresh(hero);
        }
    }
}
