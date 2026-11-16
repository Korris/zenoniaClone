using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Compact boss bar, top centre (docs/reference/ui-redesign-261116/chatgpt/hud-compact/: hud-compact-mobile-v1.png and section 7 of the
    /// parts board; about 5 % of the screen height), shown with UiScreens.UseNew(UiScreen.Boss) for the boss Hud.BarBoss() picks (the IMGUI
    /// bar of Hud.cs is then off):
    ///   map boss      small round portrait, "Frostfang King Lv. 15" as outlined text, a slim HP bar with "2,988 / 4,988 (59.9%)"
    ///   Wanted Boss   the same + a small red WANTED tag before the name and a tick on the bar where it enrages (30 %)
    ///   cast          a thin second line under the bar with the skill name + seconds, while the Wanted Boss telegraphs a skill
    ///   enraged       red rim and tint + a small ENRAGED label, under 30 % HP
    ///   defeated      grey portrait + DEFEATED for about 2 s after the boss died
    /// (states: BossBarView.States.cs). It never shows how many players are fighting (user decision). The bar keeps clear of the player
    /// status and the minimap: it narrows down to MinWidth between them and only then drops to a second row. Built on first use; texts are
    /// rebuilt only when their number changes.
    /// </summary>
    public sealed partial class BossBarView : MonoBehaviour
    {
        const float MaxWidth = 380f, MinWidth = 220f, Top = 6f, Gap = 10f;
        const float OldBlockRight = 312f, MinimapWidth = HudAnchors.MinimapMargin + HudAnchors.MinimapWidth; // RpgHud's old block; the minimap with its edge gap
        const float Face = 32f, NameH = 15f, BarH = 15f, BarY = NameH + 1f, BarLeft = Face - 7f, Height = BarY + BarH + 1f; // the bar tucks under the portrait
        const float BarPad = BarH * 0.14f, RingHole = 0.075f, TagW = 52f; // the frame's rim; the ring's window as a share of the portrait
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>Bottom edge of the bar in units from the top of the safe area, 0 while hidden: the toast area starts under it (RpgHud.ToastTop).</summary>
        public static float Bottom { get; private set; }

        RectTransform safe, root, nameRt, tagRt, tickRt;
        HudCompactBar bar;
        Image portrait, ring;
        Text nameText;
        Monster shown;
        bool hasShown, wanted;
        int shownHp = -1;
        float lastFrac, shownWidth = -1f, shownCanvas = -1f;

        public static void Create(RectTransform safeArea)
        {
            var v = UiKit.Fill(safeArea, "BossBarHost").gameObject.AddComponent<BossBarView>(); // the host stays active; the bar itself is shown / hidden
            v.safe = safeArea;
        }

        void OnDestroy() { Bottom = 0f; }

        void Build()
        {
            root = UiKit.Rect(transform, "BossBar", 0f, Top, MaxWidth, Height + CastGap + CastH);
            BuildEnrageRim(); // under the bar: it shows as an outline
            bar = new HudCompactBar(Stretch(root, "Bar", BarLeft, BarY, 0f, BarH), BarH, PaperPiece.HcBossFrame, PaperPiece.HcBossFill, PaperTheme.BarHp, BarPad, BarPad, null, TextAnchor.MiddleCenter);
            // Wanted only: a thin tick across the bar where the boss enrages.
            tickRt = UiWinKit.Anchored(bar.Track, "RageTick", new Vector2(Rpg.WantedSkillClock.EnrageHpFraction, 0.5f), new Vector2(0.5f, 0.5f), 0f, 0f, 1.5f, BarH - 3f);
            PaperArt.Plate(tickRt, PaperTheme.TitleGold, Color.clear, 1f);
            tickRt.SetSiblingIndex(1); // over the fill, under the numbers

            tagRt = UiKit.Rect(root, "Wanted", Face + 3f, 1f, TagW, NameH - 2f);
            PaperArt.Plate(tagRt, PaperTheme.BadgeRed, Color.clear, 3f);
            PaperTheme.Caption(tagRt, "WANTED", PaperTheme.BarSize, PaperTheme.TextLight);
            nameRt = Stretch(root, "Name", Face + 4f, 0f, 0f, NameH);
            nameText = PaperTheme.Title(nameRt, "Text", 0f, 0f, 10f, NameH, "", PaperTheme.BodySize, PaperTheme.TextLight, PaperInk.Outlined);
            nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            BuildEnrageLabel();
            BuildCast();
            portrait = HudCompact.Portrait(root, "Portrait", 0f, 0f, Face, PaperPiece.HcBossRing, null, RingHole, out ring);
            BuildDefeated();
        }

        /// <summary>Child anchored to the parent's top edge and stretched between `left` and `right` insets (follows the bar's width).</summary>
        static RectTransform Stretch(Transform parent, string name, float left, float top, float right, float height)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(left, -top - height); rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        void Update()
        {
            bool on = UiScreens.UseNew(UiScreen.Boss) && GameSession.Hero != null && !HeroChangeScreen.IsOpen;
            Monster boss = on ? Hud.BarBoss() : null;
            if (boss != null)
            {
                if (root == null) Build();
                if (boss != shown || !hasShown || defeatedLeft > 0f) Bind(boss);
                RefreshHp(boss);
                RefreshStates(boss);
                Layout(MaxWidth, Height + (castOn ? CastGap + CastH : 0f));
            }
            else if (hasShown) TickGone(on, Time.unscaledDeltaTime);
        }

        /// <summary>A boss takes the bar: name, portrait, and for a Wanted Boss the tag before its name and the tick on the bar.</summary>
        void Bind(Monster boss)
        {
            shown = boss; hasShown = true; wanted = boss.IsWanted;
            shownHp = -1; defeatedLeft = 0f;
            nameRt.offsetMin = new Vector2(Face + 4f + (wanted ? TagW + 4f : 0f), nameRt.offsetMin.y);
            nameText.text = boss.DisplayName + " <color=#FDC04E>Lv. " + boss.Level + "</color>";
            portrait.sprite = BossPortraits.Get(boss.Type, boss.WalkCellPx);
            portrait.enabled = portrait.sprite != null;
            tagRt.gameObject.SetActive(wanted);
            tickRt.gameObject.SetActive(wanted);
            bar.Root.gameObject.SetActive(true);
            nameRt.gameObject.SetActive(true);
            BindStates();
            root.gameObject.SetActive(true);
        }

        void RefreshHp(Monster boss)
        {
            float frac = boss.MaxHp > 0f ? Mathf.Clamp01(boss.Hp / boss.MaxHp) : 0f;
            lastFrac = frac;
            bar.Set(frac);
            int hp = Mathf.CeilToInt(boss.Hp);
            if (hp == shownHp) return;
            shownHp = hp;
            bar.Value.text = hp.ToString("#,0", Inv) + " / " + ((int)boss.MaxHp).ToString("#,0", Inv) + " (" + (frac * 100f).ToString("0.#", Inv) + "%)";
        }

        /// <summary>
        /// Width and place: centred on the screen when the gap between the player status and the minimap allows, else centred in that gap,
        /// narrowing down to MinWidth; a screen too narrow even for that gets the bar on a second row under both.
        /// </summary>
        void Layout(float maxWidth, float height)
        {
            float canvas = safe.rect.width;
            float left = (UiScreens.UseNew(UiScreen.Hud) ? PlayerStatusBadge.Right : OldBlockRight) + Gap, right = canvas - MinimapWidth - Gap;
            float y = Top, w, x;
            if (right - left >= Mathf.Min(MinWidth, maxWidth))
            {
                w = Mathf.Min(maxWidth, right - left);
                x = Mathf.Clamp((canvas - w) * 0.5f, left, right - w);
            }
            else
            {
                w = Mathf.Min(maxWidth, canvas - Gap * 2f);
                x = (canvas - w) * 0.5f;
                y = Mathf.Max(RpgHud.BlockHeight, HudAnchors.MinimapBottom) + 4f;
            }
            Bottom = y + height;
            if (Mathf.Approximately(w, shownWidth) && Mathf.Approximately(canvas, shownCanvas) && Mathf.Approximately(root.anchoredPosition.y, -y)) return;
            shownWidth = w; shownCanvas = canvas;
            root.anchoredPosition = new Vector2(x, -y);
            root.sizeDelta = new Vector2(w, root.sizeDelta.y);
        }

        void Hide()
        {
            hasShown = false; shown = null; defeatedLeft = 0f;
            Bottom = 0f;
            if (root != null) root.gameObject.SetActive(false);
        }
    }
}
