using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// The area title shown each time the local hero enters a map (Zenonia style): the map's Name in big gold letters between two thin gold rules,
    /// with "Safe Zone" / "Danger Zone" under it, high above the centre of the screen. It watches MapRegistry.Current instead of hooking every
    /// transfer path (offline portals, online Welcome / zone transfer, the class select reset): the map has to stay the same for SettleSeconds with a
    /// hero on it, so the placeholder map an online client loads before the server's Welcome never flashes its name. Unscaled time, no raycasts.
    /// </summary>
    public sealed class MapNameBanner : MonoBehaviour
    {
        const float SettleSeconds = 0.5f, FadeIn = 0.45f, Hold = 2.4f, FadeOut = 0.8f, RiseUnits = 10f;
        const float Width = 520f, Height = 78f, CentreAbove = 200f;   // reference units (960 x 640 canvas), clear of the boss bar and the level-up banner
        static readonly Color Safe = new Color(0.62f, 0.95f, 0.66f), Danger = new Color(1f, 0.5f, 0.42f);

        RectTransform root;
        CanvasGroup group;
        Text title, sub;
        string seenId, shownId;
        float seenAt, age = -1f;

        public static void Create(Transform canvas)
        {
            var banner = UiKit.Rect(canvas, "MapNameBanner", 0f, -CentreAbove, Width, Height, true).gameObject.AddComponent<MapNameBanner>();
            banner.Build();
        }

        void Build()
        {
            root = (RectTransform)transform;
            group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false; group.interactable = false; group.alpha = 0f;
            title = UiKit.Label(root, "Title", 0f, 0f, Width, 46f, "", 34, UiWinKit.Gold, TextAnchor.MiddleCenter, true);
            var outline = title.gameObject.AddComponent<Outline>();               // dark rim: the gold must read on snow and on sand alike
            outline.effectColor = new Color(0.12f, 0.07f, 0.02f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
            Rule(-Width * 0.5f + 20f); Rule(Width * 0.5f - 20f - RuleW);
            sub = UiKit.Label(root, "Sub", 0f, 50f, Width, 24f, "", 15, Safe, TextAnchor.MiddleCenter, true);
        }

        const float RuleW = 90f, RuleY = Height * 0.5f - 23f;   // the title row's middle (title = top 46 units of the banner)

        /// <summary>A thin gold line beside the title (x = its left edge from the banner's centre).</summary>
        void Rule(float x)
        {
            var rt = UiKit.Rect(root, "Rule", 0f, 0f, RuleW, 2f, true);
            rt.anchoredPosition = new Vector2(x + RuleW * 0.5f, RuleY);
            UiKit.Panel(rt, new Color(UiWinKit.Gold.r, UiWinKit.Gold.g, UiWinKit.Gold.b, 0.85f));
        }

        void Update()
        {
            IGameMap map = MapRegistry.Current;
            bool inWorld = map != null && GameSession.Hero != null && !HeroChangeScreen.IsOpen;
            if (!inWorld) { seenId = null; Hide(); return; }
            if (map.Id != seenId) { seenId = map.Id; seenAt = Time.unscaledTime; }
            if (map.Id != shownId && Time.unscaledTime - seenAt >= SettleSeconds) Begin(map);
            Animate();
        }

        void Begin(IGameMap map)
        {
            shownId = map.Id;
            title.text = map.Name;
            sub.text = map.Hostile ? "Danger Zone" : "Safe Zone";
            sub.color = map.Hostile ? Danger : Safe;
            float w = Mathf.Min(Width - 2f * RuleW, title.preferredWidth + 24f);   // the rules hug the title, whatever its length
            Transform a = root.GetChild(1), b = root.GetChild(2);
            ((RectTransform)a).anchoredPosition = new Vector2(-w * 0.5f - RuleW * 0.5f - 8f, RuleY);
            ((RectTransform)b).anchoredPosition = new Vector2(w * 0.5f + RuleW * 0.5f + 8f, RuleY);
            age = 0f;
        }

        void Animate()
        {
            if (age < 0f) return;
            age += Time.unscaledDeltaTime;
            float a = age < FadeIn ? age / FadeIn : age < FadeIn + Hold ? 1f : 1f - (age - FadeIn - Hold) / FadeOut;
            if (a <= 0f && age > FadeIn) { Hide(); return; }
            group.alpha = Mathf.Clamp01(a);
            float rise = (1f - Mathf.Clamp01(age / FadeIn)) * RiseUnits;            // eases up into place while fading in
            root.anchoredPosition = new Vector2(0f, CentreAbove - rise);
        }

        void Hide() { age = -1f; if (group != null) group.alpha = 0f; }
    }
}
