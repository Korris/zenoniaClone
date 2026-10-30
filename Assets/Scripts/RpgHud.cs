using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// UGUI player HUD (top-left): level + class, gold, HP / MP / EXP bars, potion + upgrade stone counters; a "LEVEL UP" banner and a
    /// one-line toast for pickups / warnings. Texts are rebuilt only when their value changes. The IMGUI Hud draws the rest (skill, FPS, help).
    /// </summary>
    public class RpgHud : MonoBehaviour
    {
        /// <summary>Height of the HUD block in UI units: the IMGUI Hud starts below it.</summary>
        public const float BlockHeight = 112f;
        const float BannerSeconds = 2.4f, ToastSeconds = 2.6f;
        static RpgHud instance;

        RectTransform block, banner;
        Text title, gold, hpPotText, mpPotText, stoneText, toastText, bannerTitle, bannerSub;
        UiKit.Bar hpBar, mpBar, expBar;
        Image toastBack;
        CanvasGroup bannerGroup;
        float bannerLeft, toastLeft;
        int lastHp = -1, lastMaxHp, lastMp = -1, lastMaxMp, lastLevel, lastExp = -1, lastGold = -1, lastHpPot = -1, lastMpPot = -1, lastStones = -1;
        string lastClass;

        public static RpgHud Create(Transform parent)
        {
            var go = new GameObject("RpgHud");
            go.transform.SetParent(parent, false);
            instance = go.AddComponent<RpgHud>();
            instance.Build(UiKit.Canvas(parent).transform);
            return instance;
        }

        void OnDestroy() { if (instance == this) instance = null; }

        void Build(Transform canvas)
        {
            // Gold-bordered navy panel (9-sliced procedural frame); content sits inside the ~6 unit border.
            block = UiKit.Rect(canvas, "HudBlock", 8f, 6f, 252f, BlockHeight);
            Image frame = UiKit.Panel(block, Color.white);
            frame.sprite = UiSprites.Panel(); frame.type = Image.Type.Sliced; frame.pixelsPerUnitMultiplier = 1.4f;
            title = UiKit.Label(block, "Title", 11f, 5f, 150f, 20f, "", 15, Color.white, TextAnchor.MiddleLeft, true);
            gold = UiKit.Label(block, "Gold", 150f, 5f, 92f, 20f, "", 15, new Color(1f, 0.82f, 0.2f), TextAnchor.MiddleRight, true);
            hpBar = new UiKit.Bar(block, "Hp", 9f, 28f, 234f, 17f, new Color(0.13f, 0.78f, 0.16f), 12);
            mpBar = new UiKit.Bar(block, "Mp", 9f, 47f, 234f, 17f, new Color(0.1f, 0.42f, 0.98f), 12);
            expBar = new UiKit.Bar(block, "Exp", 9f, 66f, 234f, 17f, new Color(0.58f, 0.2f, 0.92f), 11);
            UiKit.Icon(block, "HpIcon", 12f, 87f, 20f).sprite = LootSprites.Potion(true);
            UiKit.Icon(block, "MpIcon", 90f, 87f, 20f).sprite = LootSprites.Potion(false);
            UiKit.Icon(block, "StoneIcon", 168f, 87f, 20f).sprite = LootSprites.Stone();
            hpPotText = UiKit.Label(block, "HpCount", 36f, 86f, 50f, 22f, "", 13, Color.white, TextAnchor.MiddleLeft, true);
            mpPotText = UiKit.Label(block, "MpCount", 114f, 86f, 50f, 22f, "", 13, Color.white, TextAnchor.MiddleLeft, true);
            stoneText = UiKit.Label(block, "StoneCount", 192f, 86f, 50f, 22f, "", 13, Color.white, TextAnchor.MiddleLeft, true);

            banner = UiKit.Rect(canvas, "LevelUp", 0f, -150f, 420f, 90f, true);
            bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.blocksRaycasts = false;
            bannerTitle = UiKit.Label(banner, "Title", 0f, 0f, 420f, 54f, "LEVEL UP!", 40, new Color(1f, 0.88f, 0.35f), TextAnchor.MiddleCenter, true);
            bannerSub = UiKit.Label(banner, "Sub", 0f, 52f, 420f, 30f, "", 18, Color.white, TextAnchor.MiddleCenter, true);
            banner.gameObject.SetActive(false);

            RectTransform toast = UiKit.Rect(canvas, "Toast", 0f, 165f, 420f, 28f, true); // clear of the touch buttons along the bottom edge
            toastBack = UiKit.Panel(toast, new Color(0.03f, 0.06f, 0.1f, 0.75f));
            toastText = UiKit.Label(UiKit.Fill(toast, "Text", 2f), "", 14, Color.white, TextAnchor.MiddleCenter, true);
            toast.gameObject.SetActive(false);
        }

        /// <summary>Short message line at the bottom centre (pickups, "Inventory full", potion refused ...).</summary>
        public static void Toast(string text, Color color)
        {
            if (instance == null || string.IsNullOrEmpty(text)) return;
            instance.toastText.text = text;
            instance.toastText.color = color;
            instance.toastLeft = ToastSeconds;
            instance.toastBack.transform.gameObject.SetActive(true);
        }

        public static void LevelUp(int level)
        {
            if (instance == null) return;
            instance.bannerSub.text = "Level " + level + "  -  HP / MP restored, stats up";
            instance.bannerLeft = BannerSeconds;
            instance.banner.gameObject.SetActive(true);
        }

        void Update()
        {
            HeroController hero = GameSession.Hero;
            PlayerState p = hero != null ? hero.Rpg : null;
            block.gameObject.SetActive(p != null);
            float dt = Time.unscaledDeltaTime;
            if (bannerLeft > 0f)
            {
                bannerLeft -= dt;
                float age = BannerSeconds - bannerLeft;
                float pop = age < 0.18f ? Mathf.Lerp(0.6f, 1.12f, age / 0.18f) : Mathf.Lerp(1.12f, 1f, Mathf.Clamp01((age - 0.18f) / 0.2f));
                banner.localScale = new Vector3(pop, pop, 1f);
                bannerGroup.alpha = Mathf.Clamp01(bannerLeft / 0.6f);
                if (bannerLeft <= 0f) banner.gameObject.SetActive(false);
            }
            if (toastLeft > 0f)
            {
                toastLeft -= dt;
                float a = Mathf.Clamp01(toastLeft / 0.5f);
                Color c = toastText.color; c.a = a; toastText.color = c;
                toastBack.color = new Color(0.03f, 0.06f, 0.1f, 0.75f * a);
                if (toastLeft <= 0f) toastBack.transform.gameObject.SetActive(false);
            }
            if (p != null) Refresh(hero, p);
        }

        void Refresh(HeroController hero, PlayerState p)
        {
            if (p.Level != lastLevel || hero.Class.Name != lastClass)
            {
                lastLevel = p.Level; lastClass = hero.Class.Name;
                title.text = "Lv " + p.Level + "  " + hero.Class.Name;
                lastExp = -1;
            }
            int hp = Mathf.CeilToInt(p.Hp), mp = Mathf.FloorToInt(p.Mp);
            if (hp != lastHp || p.MaxHp != lastMaxHp) { lastHp = hp; lastMaxHp = p.MaxHp; hpBar.Caption.text = "HP " + hp + " / " + p.MaxHp; }
            if (mp != lastMp || p.MaxMp != lastMaxMp) { lastMp = mp; lastMaxMp = p.MaxMp; mpBar.Caption.text = "MP " + mp + " / " + p.MaxMp; }
            float hpFrac = p.MaxHp > 0 ? p.Hp / p.MaxHp : 0f;
            hpBar.Set(hpFrac);
            hpBar.FillImage.color = hpFrac > 0.3f ? new Color(0.2f, 0.78f, 0.35f) : new Color(0.9f, 0.25f, 0.25f);
            mpBar.Set(p.MaxMp > 0 ? p.Mp / p.MaxMp : 0f);
            if (p.Exp != lastExp)
            {
                lastExp = p.Exp;
                bool max = p.Level >= Leveling.MaxLevel;
                expBar.Caption.text = max ? "MAX LEVEL" : "EXP " + p.Exp + " / " + Leveling.ExpToNext(p.Level);
                expBar.Set(max ? 1f : Leveling.ProgressRatio(p.Level, p.Exp));
            }
            if (p.Gold != lastGold) { lastGold = p.Gold; gold.text = p.Gold + " G"; }
            if (p.HpPotions != lastHpPot || p.MpPotions != lastMpPot || p.UpgradeStone != lastStones)
            {
                lastHpPot = p.HpPotions; lastMpPot = p.MpPotions; lastStones = p.UpgradeStone;
                hpPotText.text = "x" + p.HpPotions;
                mpPotText.text = "x" + p.MpPotions;
                stoneText.text = "x" + p.UpgradeStone;
            }
        }
    }
}
