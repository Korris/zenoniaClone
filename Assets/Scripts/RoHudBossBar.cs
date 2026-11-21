using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Boss HP bar of the RO Soft HUD: top centre under the menu row (Hud.BarBoss: the Wanted Boss or the map boss near the hero), laid out as
    /// the Wanted mockup's panel 5 (docs/reference/ro-soft-hud/wanted-notices-mockup-chatgpt-1.png):
    ///   portrait   the boss's face in the crowned gold ring on the left (Wanted Boss only; a map boss has no portrait art)
    ///   name row   navy pill with the skull + name; ENRAGED (coral flame tag) at its right end while Monster.WantedEnraged
    ///   HP         the ChatGPT track with the red fill and "hp / max"
    ///   cast       a purple bar under the HP bar, "Frozen Ground Stomp 1.6s", from Monster.WantedCast (the telegraph's own strike clock)
    ///   hint       the skill's dodge hint (WantedSkillDef.Hint) in an ivory speech bubble right of the bar while it casts, under it on a
    ///              narrow screen
    /// Hud.DrawBossBar (IMGUI) stays quiet while RoHud.Active.
    /// Compact sizes and placement from ChatGPT (user 20/11: the status panel covered the bar on 4:3 / 1280x853,
    /// docs/reference/hud-compact-player-panel/): portrait 52, then the 242-wide column (name pill 22, HP 18, cast 18). The group sits
    /// under the menu row, centred on the screen but clamped into the free band between the status plate and the minimap column
    /// (8 units clear of both); the hint goes right of it when the band has room for both, else under the cast bar.
    /// </summary>
    public sealed class RoHudBossBar
    {
        const float Face = 52f, ColX = Face + 6f, W = 242f, GroupW = ColX + W, PillH = 22f, BarY = PillH + 4f, BarH = 18f;
        const float CastY = BarY + BarH + 5f, CastH = 18f, Top = 92f, Gap = 8f;
        const float HintW = 200f, HintH = 64f, HintGap = 10f, HintUnderH = 54f, TagW = 100f, TagH = 26f;
        const float HintRightW = GroupW + HintGap + HintW, HintArtH = 80f, NameIcon = 15f;
        static readonly Color EnragedFace = new Color(1f, 0.7f, 0.66f);

        /// <summary>Bottom of the bar (and its hint) in units below the safe area's top, 0 while hidden: the RO Wanted notices start under it.</summary>
        public static float Bottom { get; private set; }

        readonly RectTransform root, safe, hint;
        readonly RoHud.Bar bar, cast;
        readonly Text name, value, castText, hintText;
        readonly Image face, hintArt;
        readonly GameObject portrait, enraged;
        int shownHp = -1, castTenths = -1;
        Monster shownBoss;
        WantedSkillDef castSkill;
        bool enragedOn;

        public RoHudBossBar(RectTransform safe)
        {
            this.safe = safe;
            // Root = the whole group (portrait + column), top-left anchored; Place() moves it every frame.
            root = UiKit.Rect(safe, "RoHudBossBar", 0f, Top, GroupW, Face);
            RectTransform pill;
            name = RoHud.Pill(root, "Name", RoHud.TopLeft, RoHud.TopLeft, ColX, 0f, PillH, "hud-lv-badge", "hud-icon-skull", NameIcon, "", 14, Color.white, out pill);
            pill.GetComponent<Image>().pixelsPerUnitMultiplier = 44f / PillH; // left: ENRAGED takes the row's right end
            bar = new RoHud.Bar(root, "Hp", ColX, BarY, W, BarH, "hud-bar-track", "hud-bar-red");
            value = RoHud.Label(UiKit.Fill(bar.Root, "Value"), "", 12, Color.white, TextAnchor.MiddleCenter, true, true);

            face = RoWantedWidgets.Portrait(root, "Portrait", 0f, 0f, Face, true);
            portrait = face.transform.parent.parent.gameObject;
            RectTransform tag = UiKit.Rect(root, "Enraged", GroupW - TagW, -2f, TagW, TagH);
            RoWantedWidgets.Plate(tag, "Art", 0f, 0f, TagW, TagH, "wanted-enraged", 40f);
            RectTransform tagText = UiKit.Fill(tag, "Text");
            tagText.offsetMin = new Vector2(28f, 3f); tagText.offsetMax = new Vector2(-8f, -3f); // clear of the flame
            RoHud.Label(tagText, "ENRAGED", 12, Color.white, TextAnchor.MiddleCenter, true, true);
            enraged = tag.gameObject;

            cast = new RoHud.Bar(root, "Cast", ColX, CastY, W, CastH, "wanted-cast-track", "wanted-cast-fill");
            castText = RoHud.Label(UiKit.Fill(cast.Root, "Text"), "", 12, Color.white, TextAnchor.MiddleCenter, true, true);
            hint = UiKit.Rect(root, "Hint", GroupW + HintGap, 0f, HintW, HintH);
            hintArt = RoWantedWidgets.Plate(hint, "Art", 0f, 0f, HintW, HintH, "wanted-hint-bubble", HintArtH);
            RectTransform ht = UiKit.Fill(hint, "Text");
            ht.offsetMin = new Vector2(22f, 7f); ht.offsetMax = new Vector2(-10f, -7f); // the pointer on the left + the rim
            hintText = RoHud.Label(ht, "", 12, RoHud.Ink, TextAnchor.MiddleLeft, true);
            hintText.verticalOverflow = VerticalWrapMode.Truncate; hintText.lineSpacing = 0.95f;
            hintText.horizontalOverflow = HorizontalWrapMode.Wrap;
            hintText.resizeTextForBestFit = true; hintText.resizeTextMinSize = 10; hintText.resizeTextMaxSize = 12;
            enraged.SetActive(false); cast.Root.gameObject.SetActive(false); hint.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
            NameTags.Occlude(root); NameTags.Occlude(hint);
        }

        public void Tick(bool visible)
        {
            Monster boss = visible ? Hud.BarBoss() : null;
            bool on = boss != null;
            if (root.gameObject.activeSelf != on) { root.gameObject.SetActive(on); if (on) { bar.Relayout(); cast.Relayout(); } }
            if (!on) { shownBoss = null; Bottom = 0f; return; }
            if (boss != shownBoss)
            {
                shownBoss = boss; name.text = boss.DisplayName; shownHp = -1; enragedOn = false; castSkill = null;
                portrait.SetActive(boss.IsWanted);
                if (boss.IsWanted) { face.sprite = BossPortraits.Get(boss.Type, boss.WalkCellPx); face.enabled = face.sprite != null; }
                face.color = Color.white; enraged.SetActive(false); FitName();
            }
            States(boss);
            int hp = Mathf.CeilToInt(boss.Hp);
            if (hp == shownHp) return;
            shownHp = hp;
            value.text = hp + " / " + (int)boss.MaxHp;
            bar.Set(boss.MaxHp > 0f ? boss.Hp / boss.MaxHp : 0f);
        }

        /// <summary>
        /// The name pill grows with the name (ContentSizeFitter); while ENRAGED holds the row's right end, the name shrinks (14 down to
        /// 11) so the pill ends 4 units before the tag instead of running under its flame.
        /// </summary>
        void FitName()
        {
            float room = (enragedOn ? W - TagW - 4f : W) - RoHud.PillPadX * 2f - NameIcon - RoHud.PillGap;
            int size = 14;
            name.fontSize = size;
            while (size > 11 && name.preferredWidth > room) name.fontSize = --size;
        }

        /// <summary>Enraged tag + tinted face, the cast bar and the hint bubble; Bottom follows what shows.</summary>
        void States(Monster boss)
        {
            bool rage = boss.UsesSkills && boss.WantedEnraged;
            if (rage != enragedOn) { enragedOn = rage; enraged.SetActive(rage); face.color = rage ? EnragedFace : Color.white; FitName(); }
            WantedSkillDef skill = null;
            float leftMs = 0f;
            bool casting = boss.UsesSkills && boss.WantedCast(out skill, out leftMs);
            if (cast.Root.gameObject.activeSelf != casting) { cast.Root.gameObject.SetActive(casting); castTenths = -1; if (casting) cast.Relayout(); }
            bool showHint = casting && !string.IsNullOrEmpty(skill.Hint);
            if (hint.gameObject.activeSelf != showHint) hint.gameObject.SetActive(showHint);
            // Free band between the status plate and the minimap column (safe-area units from its left edge).
            float safeW = safe.rect.width;
            float bandL = RoHud.StatusRight + Gap, bandR = safeW - (RoHud.MinimapRight + RoHud.MinimapSize) - Gap;
            bool right = showHint && bandR - bandL >= HintRightW;
            float groupX = Mathf.Clamp(safeW * 0.5f - GroupW * 0.5f, bandL, Mathf.Max(bandL, bandR - (right ? HintRightW : GroupW)));
            if (Mathf.Abs(root.anchoredPosition.x - groupX) > 0.5f) root.anchoredPosition = new Vector2(groupX, -Top);
            float bottom = Top + Face;
            if (casting)
            {
                bottom = Top + CastY + CastH;
                cast.Set(1f - Mathf.Clamp01(leftMs / Mathf.Max(1, skill.TelegraphMs)));
                int tenths = Mathf.CeilToInt(leftMs / 100f);
                if (tenths != castTenths || skill != castSkill)
                {
                    castTenths = tenths;
                    if (skill != castSkill) { castSkill = skill; hintText.text = skill.Hint ?? ""; }
                    castText.text = skill.Name + "  " + (tenths / 10) + "." + (tenths % 10) + "s";
                }
            }
            if (showHint)
            {
                // Right of the group when the band has room for both, else under the cast bar in the column's width.
                float w = right ? HintW : W, h = right ? HintH : HintUnderH, y = right ? 0f : CastY + CastH + 6f;
                hint.anchoredPosition = new Vector2(right ? GroupW + HintGap : ColX, -y);
                if (Mathf.Abs(hint.sizeDelta.x - w) > 0.5f)
                {
                    hint.sizeDelta = hintArt.rectTransform.sizeDelta = new Vector2(w, h);
                    hintArt.pixelsPerUnitMultiplier = HintArtH / h;
                }
                if (!right) bottom = Top + y + h;
            }
            Bottom = bottom;
        }
    }
}
