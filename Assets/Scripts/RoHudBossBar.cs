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
    /// </summary>
    public sealed class RoHudBossBar
    {
        const float W = 300f, BarH = 18f, Top = 90f, PillH = 22f, Face = 58f, CastH = 16f, HintW = 210f, HintH = 64f;
        static readonly Color EnragedFace = new Color(1f, 0.7f, 0.66f);

        /// <summary>Bottom of the bar (and its hint) in units below the safe area's top, 0 while hidden: the RO Wanted notices start under it.</summary>
        public static float Bottom { get; private set; }

        readonly RectTransform root, safe, hint;
        readonly RoHud.Bar bar, cast;
        readonly Text name, value, castText, hintText;
        readonly Image face;
        readonly GameObject portrait, enraged;
        int shownHp = -1, castTenths = -1;
        Monster shownBoss;
        WantedSkillDef castSkill;
        bool enragedOn;

        public RoHudBossBar(RectTransform safe)
        {
            this.safe = safe;
            root = RoHud.Box(safe, "RoHudBossBar", new Vector2(0.5f, 1f), 0f, -(Top + (PillH + BarH) * 0.5f), W, PillH + BarH + 2f);
            RectTransform pill;
            name = RoHud.Pill(root, "Name", RoHud.TopLeft, RoHud.TopLeft, 6f, 0f, PillH, "hud-lv-badge", "hud-icon-skull", 15f, "", 12, Color.white, out pill); // left: ENRAGED takes the row's right end
            bar = new RoHud.Bar(root, "Hp", 0f, PillH + 2f, W, BarH, "hud-bar-track", "hud-bar-red");
            value = RoHud.Label(UiKit.Fill(bar.Root, "Value"), "", 12, Color.white, TextAnchor.MiddleCenter, true, true);

            face = RoWantedWidgets.Portrait(root, "Portrait", -Face - 4f, (PillH + BarH + 2f - Face) * 0.5f + 4f, Face, true);
            portrait = face.transform.parent.parent.gameObject;
            RectTransform tag = UiKit.Rect(root, "Enraged", W - 92f, -2f, 92f, 24f);
            RoWantedWidgets.Plate(tag, "Art", 0f, 0f, 92f, 24f, "wanted-enraged", 40f);
            RectTransform tagText = UiKit.Fill(tag, "Text");
            tagText.offsetMin = new Vector2(26f, 2f); tagText.offsetMax = new Vector2(-8f, -2f); // clear of the flame
            RoHud.Label(tagText, "ENRAGED", 11, Color.white, TextAnchor.MiddleCenter, true, true);
            enraged = tag.gameObject;

            cast = new RoHud.Bar(root, "Cast", 18f, PillH + BarH + 5f, W - 36f, CastH, "wanted-cast-track", "wanted-cast-fill");
            castText = RoHud.Label(UiKit.Fill(cast.Root, "Text"), "", 11, Color.white, TextAnchor.MiddleCenter, true, true);
            hint = UiKit.Rect(root, "Hint", W + 8f, PillH - 6f, HintW, HintH);
            RoWantedWidgets.Plate(hint, "Art", 0f, 0f, HintW, HintH, "wanted-hint-bubble", 80f);
            RectTransform ht = UiKit.Fill(hint, "Text");
            ht.offsetMin = new Vector2(24f, 9f); ht.offsetMax = new Vector2(-12f, -9f); // the pointer on the left + the rim
            hintText = RoHud.Label(ht, "", 12, RoHud.Ink, TextAnchor.MiddleLeft, true);
            hintText.verticalOverflow = VerticalWrapMode.Truncate; hintText.lineSpacing = 0.95f;
            hintText.horizontalOverflow = HorizontalWrapMode.Wrap;
            hintText.resizeTextForBestFit = true; hintText.resizeTextMinSize = 9; hintText.resizeTextMaxSize = 12;
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
                face.color = Color.white; enraged.SetActive(false);
            }
            States(boss);
            int hp = Mathf.CeilToInt(boss.Hp);
            if (hp == shownHp) return;
            shownHp = hp;
            value.text = hp + " / " + (int)boss.MaxHp;
            bar.Set(boss.MaxHp > 0f ? boss.Hp / boss.MaxHp : 0f);
        }

        /// <summary>Enraged tag + tinted face, the cast bar and the hint bubble; Bottom follows what shows.</summary>
        void States(Monster boss)
        {
            bool rage = boss.IsWanted && boss.WantedEnraged;
            if (rage != enragedOn) { enragedOn = rage; enraged.SetActive(rage); face.color = rage ? EnragedFace : Color.white; }
            WantedSkillDef skill = null;
            float leftMs = 0f;
            bool casting = boss.IsWanted && boss.WantedCast(out skill, out leftMs);
            if (cast.Root.gameObject.activeSelf != casting) { cast.Root.gameObject.SetActive(casting); castTenths = -1; if (casting) cast.Relayout(); }
            bool showHint = casting && !string.IsNullOrEmpty(skill.Hint);
            if (hint.gameObject.activeSelf != showHint) hint.gameObject.SetActive(showHint);
            float bottom = Top + PillH + BarH + 2f;
            if (casting)
            {
                bottom += CastH + 5f;
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
                // Right of the bar when the screen has room (clear of the minimap column), else under the cast bar.
                bool right = safe.rect.width * 0.5f - W * 0.5f - 8f - HintW > RoHud.MinimapRight + RoHud.MinimapSize + 8f;
                hint.anchoredPosition = right ? new Vector2(W + 8f, -(PillH - 6f)) : new Vector2((W - HintW) * 0.5f, -(bottom - Top + 4f));
                if (!right) bottom += HintH + 4f;
            }
            Bottom = bottom;
        }
    }
}
