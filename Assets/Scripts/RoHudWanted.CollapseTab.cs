using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Folding of the RO HUD WANTED card (user 20/11: the round arrow "ấn hide đi ko đc", it was art only and a tap on it opened the
    /// board). The arrow is its own button: it slides the card (and the respawn pill under it) off to the right in FoldSeconds and slides
    /// in a small tab at the screen edge: the red WANTED tag, the boss face, the arrow turned to point left. A tap anywhere on the tab
    /// unfolds the card. The fold is remembered across sessions (PlayerPrefs FoldKey). Nothing else moves: the card sits alone in the
    /// top-right column, the potions and skill ring are anchored to the bottom-right corner.
    /// </summary>
    public sealed partial class RoHudWanted
    {
        static string FoldKey { get { return RoHud.Pc ? "ro-hud-wanted-folded-pc" : "ro-hud-wanted-folded"; } } // the PC profile keeps its own fold (folded until opened)
        const float TabW = 96f, TabH = 58f, TabFace = 36f, ArrowSize = 26f, ArrowHit = 40f, FoldSeconds = 0.2f;

        /// <summary>The card is folded to its tab (capture logs; there is one Wanted card).</summary>
        internal static bool Collapsed;

        CanvasGroup bodyGroup, tabGroup;
        RectTransform tab;
        Image tabFace;
        float fold = -1f; // 0 = card shown, 1 = tab shown; -1 = not placed yet

        /// <summary>The round arrow on the card's right: a 40-unit transparent hit box (the art is 26) that folds the card.</summary>
        void BuildFoldArrow()
        {
            RectTransform hit = RoHud.Box(body, "WantedArrow", new Vector2(1f, 0.5f), -20f, -16f, ArrowHit, ArrowHit);
            Image hitBox = hit.gameObject.AddComponent<Image>();
            hitBox.color = new Color(1f, 1f, 1f, 0f); // invisible, still takes the raycast: the tap never reaches the card under it
            RoHud.Art(RoHud.Box(hit, "Art", RoHud.Mid, 0f, 0f, ArrowSize, ArrowSize), "hud-btn-arrow-big");
            PaperHudPress.Tap(hit.gameObject, () => SetCollapsed(true));
        }

        /// <summary>The folded tab, right-aligned with the card's top-right corner.</summary>
        void BuildTab()
        {
            tab = RoHud.Box(root, "WantedTab", RoHud.TopRight, 0f, 0f, TabW, TabH);
            tab.pivot = Vector2.one;
            tab.anchoredPosition = Vector2.zero;
            tabGroup = tab.gameObject.AddComponent<CanvasGroup>();
            Image back = RoHud.Art(tab, "hud-wanted-card", true);
            back.pixelsPerUnitMultiplier = 1.6f; // corner radius 36 / 1.6 = 22 units: fits the 58-high tab
            PaperHudPress.Tap(tab.gameObject, () => SetCollapsed(false));
            RectTransform tag;
            RoHud.Pill(tab, "Tag", RoHud.TopLeft, new Vector2(0f, 0.5f), 6f, 0f, 22f, "hud-wanted-tag", null, 0f, "WANTED", 12, Color.white, out tag);
            tabFace = FaceFrame(tab, 9f, (TabH - TabFace) * 0.5f + 5f, TabFace);
            RectTransform arrow = RoHud.Box(tab, "Arrow", new Vector2(1f, 0.5f), -22f, 5f, ArrowSize, ArrowSize);
            RoHud.Art(arrow, "hud-btn-arrow-big");
            arrow.localScale = new Vector3(-1f, 1f, 1f); // mirrored: points left, "pull the card back out"
            NameTags.Occlude(tab);
            Collapsed = LoadFolded();
        }

        /// <summary>The boss face in the gold frame with its crown, `size` square, top-left at (x, y) of `parent`. Returns the face image.</summary>
        static Image FaceFrame(RectTransform parent, float x, float y, float size)
        {
            RectTransform frame = RoHud.TopLeftBox(parent, "Face", x, y, size, size);
            RectTransform m = UiKit.Fill(frame, "Mask", size / 16f);
            Image mask = RoHud.Art(m, "hud-wanted-frame-mask");
            mask.preserveAspect = false;
            mask.color = PaperWanted.PortraitBack;
            m.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            Image img = UiKit.Fill(m, "Boss").gameObject.AddComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            RoHud.Art(UiKit.Fill(frame, "Frame"), "hud-wanted-frame");
            float crown = size * 0.28f;
            RoHud.Art(RoHud.TopLeftBox(frame, "Crown", -crown * 0.28f, -crown * 0.39f, crown, crown), "hud-icon-crown-small");
            return img;
        }

        void SetCollapsed(bool on)
        {
            Collapsed = on;
            try { PlayerPrefs.SetInt(FoldKey, on ? 1 : 0); PlayerPrefs.Save(); }
            catch (System.Exception e) { Debug.LogWarning("[ZenoniaProto] wanted fold not saved: " + e.Message); }
        }

        static bool LoadFolded()
        {
            int byDefault = RoHud.Pc ? 1 : 0; // PC profile: folded to its small tab until opened (RO3 review 21/11: "thu gọn thành thẻ nhỏ")
            try { return PlayerPrefs.GetInt(FoldKey, byDefault) == 1; }
            catch (System.Exception) { return byDefault == 1; }
        }

        /// <summary>Per shown frame: eases the slide toward the fold state; only the side being shown takes taps.</summary>
        void AnimateFold()
        {
            float target = Collapsed ? 1f : 0f;
            if (fold < 0f) fold = target; // first frame: snap to the saved state, no slide
            if (fold == target && body.gameObject.activeSelf != Collapsed && tab.gameObject.activeSelf == Collapsed) return;
            fold = Mathf.MoveTowards(fold, target, Time.unscaledDeltaTime / FoldSeconds);
            if (!body.gameObject.activeSelf && fold < 1f) body.gameObject.SetActive(true);
            if (!tab.gameObject.activeSelf && fold > 0f) tab.gameObject.SetActive(true);
            float e = fold * fold * (3f - 2f * fold); // smoothstep
            body.anchoredPosition = new Vector2(e * (W + RoHud.Margin + 24f), 0f); // fully off the right edge
            tab.anchoredPosition = new Vector2((1f - e) * (TabW + RoHud.Margin + 12f), 0f);
            bodyGroup.alpha = 1f - e;
            tabGroup.alpha = e;
            bodyGroup.blocksRaycasts = !Collapsed;
            tabGroup.blocksRaycasts = Collapsed;
            if (fold >= 1f) body.gameObject.SetActive(false);
            if (fold <= 0f) tab.gameObject.SetActive(false);
        }
    }
}
