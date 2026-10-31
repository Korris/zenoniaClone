using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// UGUI player HUD (top-left): round portrait of the current hero, level, HP / MP bars with the numbers at the right, a thin EXP bar, the icons of the buffs on the hero
    /// (HudBuffRow); a "LEVEL UP" banner and a one-line toast for pickups / warnings. Texts are rebuilt only when their value
    /// changes. The IMGUI Hud draws the rest (skill, FPS, help).
    /// </summary>
    public class RpgHud : MonoBehaviour
    {
        /// <summary>Height of the HUD block in UI units: the IMGUI Hud starts below it.</summary>
        public const float BlockHeight = 126f;
        const float BannerSeconds = 2.4f;
        static RpgHud instance;

        RectTransform block, banner;
        Text title, bannerTitle, bannerSub, hpValue, mpValue;
        UiKit.Bar hpBar, mpBar, expBar;
        Image face;
        HudBuffRow buffRow;
        CanvasGroup bannerGroup;
        float bannerLeft;
        int lastHp = -1, lastMaxHp, lastMp = -1, lastMaxMp, lastLevel, lastExp = -1;
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
            block = UiKit.Rect(canvas, "HudBlock", 8f, 6f, 304f, BlockHeight);
            UiKit.Panel(UiKit.Rect(block, "Back", 40f, 3f, 262f, 67f), new Color(0.03f, 0.05f, 0.1f, 0.68f));

            // Portrait: gold ring, dark inner disc, the face masked to a circle.
            Image ring = UiKit.Panel(UiKit.Rect(block, "PortraitRing", 0f, 0f, 76f, 76f), new Color(0.86f, 0.67f, 0.29f));
            ring.sprite = UiSprites.Circle();
            Image disc = UiKit.Panel(UiKit.Rect(block, "PortraitBack", 3f, 3f, 70f, 70f), new Color(0.05f, 0.07f, 0.12f));
            disc.sprite = UiSprites.Circle();
            RectTransform mask = UiKit.Rect(block, "PortraitMask", 4f, 4f, 68f, 68f);
            Image maskImage = UiKit.Panel(mask, Color.white);
            maskImage.sprite = UiSprites.Circle();
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            face = UiKit.Fill(mask, "Face").gameObject.AddComponent<Image>();
            face.raycastTarget = false;

            title = UiKit.Label(block, "Title", 84f, 4f, 210f, 19f, "", 15, Color.white, TextAnchor.MiddleLeft, true);
            hpBar = new UiKit.Bar(block, "Hp", 82f, 24f, 214f, 17f, new Color(0.86f, 0.17f, 0.22f), 0);
            hpValue = BarTexts(hpBar, "HP");
            mpBar = new UiKit.Bar(block, "Mp", 82f, 42f, 214f, 17f, new Color(0.11f, 0.5f, 0.95f), 0);
            mpValue = BarTexts(mpBar, "MP");
            expBar = new UiKit.Bar(block, "Exp", 82f, 61f, 214f, 6f, new Color(0.96f, 0.64f, 0.14f), 0);
            buffRow = new HudBuffRow(block, 82f, 73f);

            banner = UiKit.Rect(canvas, "LevelUp", 0f, -150f, 420f, 90f, true);
            bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.blocksRaycasts = false;
            bannerTitle = UiKit.Label(banner, "Title", 0f, 0f, 420f, 54f, "LEVEL UP!", 40, new Color(1f, 0.88f, 0.35f), TextAnchor.MiddleCenter, true);
            bannerSub = UiKit.Label(banner, "Sub", 0f, 52f, 420f, 30f, "", 18, Color.white, TextAnchor.MiddleCenter, true);
            banner.gameObject.SetActive(false);
            NotificationToast.Create(canvas); // top-centre banner for Toast(...)
        }

        /// <summary>The bar's name at its left end; returns the text for current / max at its right end.</summary>
        static Text BarTexts(UiKit.Bar bar, string name)
        {
            UiKit.Label(UiKit.Fill(bar.Root, "Name", 1f), " " + name, 11, Color.white, TextAnchor.MiddleLeft, true);
            return UiKit.Label(UiKit.Fill(bar.Root, "Value", 1f), "", 11, Color.white, TextAnchor.MiddleRight, true);
        }

        /// <summary>The class's portrait from Resources/Sprites/ui/faces; null when the art is missing (the dark disc shows).</summary>
        static Sprite FaceOf(HeroClassDef cls)
        {
            Texture2D tex = Resources.Load<Texture2D>("Sprites/ui/faces/" + RpgSession.ClassId(cls.Id));
            return tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) : null;
        }

        /// <summary>Short message banner at the top centre (pickups, "Inventory full", potion refused ...).</summary>
        public static void Toast(string text, Color color) { NotificationToast.ShowLine(text, color); }

        /// <summary>Full banner with an icon slot, a title and a sub line (win-toast).</summary>
        public static void Toast(string title, string sub, Sprite icon) { NotificationToast.Show(title, sub, icon); }

        public static void LevelUp(int level)
        {
            if (instance == null) return;
            instance.bannerSub.text = "Level " + level + "  -  HP / MP restored, +1 skill point [T]";
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
            if (p != null) { Refresh(hero, p); DevBuffKey.Tick(hero); }
        }

        void Refresh(HeroController hero, PlayerState p)
        {
            if (p.Level != lastLevel || hero.Class.Name != lastClass)
            {
                lastLevel = p.Level; lastClass = hero.Class.Name;
                title.text = "Lv. " + p.Level;
                face.sprite = FaceOf(hero.Class);
                face.enabled = face.sprite != null;
                lastExp = -1;
            }
            int hp = Mathf.CeilToInt(p.Hp), mp = Mathf.FloorToInt(p.Mp);
            if (hp != lastHp || p.MaxHp != lastMaxHp) { lastHp = hp; lastMaxHp = p.MaxHp; hpValue.text = hp + " / " + p.MaxHp + " "; }
            if (mp != lastMp || p.MaxMp != lastMaxMp) { lastMp = mp; lastMaxMp = p.MaxMp; mpValue.text = mp + " / " + p.MaxMp + " "; }
            hpBar.Set(p.MaxHp > 0 ? p.Hp / p.MaxHp : 0f);
            mpBar.Set(p.MaxMp > 0 ? p.Mp / p.MaxMp : 0f);
            if (p.Exp != lastExp)
            {
                lastExp = p.Exp;
                bool max = p.Level >= Leveling.MaxLevel;
                expBar.Set(max ? 1f : Leveling.ProgressRatio(p.Level, p.Exp));
            }
            buffRow.Refresh(hero);
        }
    }
}
