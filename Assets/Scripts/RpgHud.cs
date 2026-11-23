using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// UGUI player HUD (top-left): round portrait of the current hero, level, HP / MP bars with the numbers at the right, a thin EXP bar, the icons of the buffs on the hero
    /// (HudBuffRow) and a one-line toast for pickups / warnings. Texts are rebuilt only when their value
    /// changes. The IMGUI Hud draws the rest (skill, FPS, help). LEVEL UP is no longer a HUD banner / painted wordmark (22/11): HeroFx.LevelUp
    /// plays it in the world (LevelUpLightFx + the glowing word LevelUpWordFx).
    /// With UiScreens.UseNew(UiScreen.Hud) the block is replaced by the compact status (PlayerStatusBadge); the switch is read every frame, so capture flags and a later settings toggle both work. The
    /// new HUD lives in one safe-area root ("PaperHud") created here, which also holds the boss bar (BossBarView, its own switch UiScreen.Boss).
    /// </summary>
    public class RpgHud : MonoBehaviour
    {
        const float OldBlockHeight = 126f;
        /// <summary>Height of the HUD block in UI units: the IMGUI debug Hud and the mobile chat panel start below it.</summary>
        public static float BlockHeight { get { return UiScreens.UseNew(UiScreen.Hud) ? PlayerStatusBadge.Bottom : RoHud.Active ? RoHudStatus.Bottom + 4f : OldBlockHeight; } }
        /// <summary>Top-centre notice area of the new HUD, units below the top of the safe area: under the boss bar when one shows (the PC
        /// toast pill and the Wanted banners start here).</summary>
        public static float ToastTop { get { return Mathf.Max(46f, BossBarView.Bottom + 6f); } }
        static RpgHud instance;

        RectTransform block;
        PaperLayout paper;
        PlayerStatusBadge badge;
        RoHudStatus roStatus; // RO Soft HUD (RoHud.Active): status + party instead of the classic block
        RoHudParty roParty;
        Text title, hpValue, mpValue;
        UiKit.Bar hpBar, mpBar, expBar;
        Image face;
        HudBuffRow buffRow;
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
            block = UiKit.Rect(canvas, "HudBlock", 8f, 6f, 304f, OldBlockHeight);
            Image back = UiKit.Panel(UiKit.Rect(block, "Back", 40f, 3f, 262f, 67f), new Color(0.03f, 0.05f, 0.1f, 0.68f));

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
            if (UiStyle.Ro) // RO Soft Fantasy fight HUD: blue-black rounded plate at ~90 %, light ring, white text (never a translucent white panel)
            {
                back.sprite = RoSoftSprites.Get(RoSoftSprites.Shape.Night); back.type = Image.Type.Sliced; back.color = Color.white;
                ring.color = RoSoftTheme.MintMist;
                disc.color = RoSoftTheme.Night;
                RoSoftSkin.Dark(block); // Nunito on the status texts; colours stay white
            }
            paper = PaperLayout.CreateSafeArea(canvas, "PaperHud"); // here in the sibling order: under the touch layer and every window
            BossBarView.Create(paper.Safe);

            NotificationToast.Create(canvas); // top-centre banner for Toast(...)
            WantedHud.Create(canvas);          // phase 11: Wanted card, notices and guide (only shown with the HUD switch on, online)
            MapNameBanner.Create(canvas);      // the area title each time the hero enters a map
        }

        /// <summary>The bar's name at its left end; returns the text for current / max at its right end.</summary>
        static Text BarTexts(UiKit.Bar bar, string name)
        {
            UiKit.Label(UiKit.Fill(bar.Root, "Name", 1f), " " + name, 11, Color.white, TextAnchor.MiddleLeft, true);
            return UiKit.Label(UiKit.Fill(bar.Root, "Value", 1f), "", 11, Color.white, TextAnchor.MiddleRight, true);
        }

        /// <summary>The class's portrait from Resources/Sprites/ui/faces; null when the art is missing (the dark disc shows).</summary>
        internal static Sprite FaceOf(HeroClassDef cls)
        {
            Texture2D tex = Resources.Load<Texture2D>("Sprites/ui/faces/" + RpgSession.ClassId(cls.Id));
            return tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) : null;
        }

        /// <summary>Short message banner at the top centre (pickups, "Inventory full", potion refused ...).</summary>
        public static void Toast(string text, Color color) { NotificationToast.ShowLine(text, color); }

        /// <summary>Full banner with an icon slot, a title and a sub line (win-toast).</summary>
        public static void Toast(string title, string sub, Sprite icon) { NotificationToast.Show(title, sub, icon); }

        void Update()
        {
            HeroController hero = GameSession.Hero;
            PlayerState p = hero != null ? hero.Rpg : null;
            bool useNew = UiScreens.UseNew(UiScreen.Hud);
            bool ro = RoHud.Active;
            block.gameObject.SetActive(p != null && !useNew && !ro);
            if (ro && p != null && roStatus == null) { roStatus = new RoHudStatus(paper.Safe); roParty = new RoHudParty(paper.Safe, 4f + RoHudStatus.Bottom + 4f); }
            if (roStatus != null) { roStatus.SetActive(ro && p != null); roParty.Tick(ro && p != null && !UiWindows.ModalOpen && !HeroChangeScreen.IsOpen && !Hud.CaptureClean); }
            if (useNew && p != null && badge == null) badge = new PlayerStatusBadge(paper.Safe);
            if (badge != null) badge.SetActive(useNew && p != null);
            float dt = Time.unscaledDeltaTime;
            if (p == null) return;
            if (useNew) badge.Refresh(hero, p, dt); else if (ro) roStatus.Refresh(hero, p); else Refresh(hero, p);
            DevBuffKey.Tick(hero);
            GemWindow.DevKeyTick(); // F8 test key until the Card window (phase 03) opens the Soul Gem screens
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
