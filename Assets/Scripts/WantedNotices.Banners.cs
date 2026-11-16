using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The five banners of WantedNotices (wanted-notifications.png, sections 2-5 and 7), built once, hidden until shown, all at the top centre of
    /// the toast area (NotificationToast.AreaTop: under the boss bar and the online target bar):
    ///   Login    laurel banner: face, "Today's Wanted", name + level, map, the four bounty squares
    ///   Respawn  blue banner: face, "Glacial Colossus has returned!"
    ///   Area     map banner: "A Wanted Boss lurks in this area." + metres to the boss (live while it shows)
    ///   Warning  red chained ribbon: WARNING / WANTED BOSS
    ///   Already  dark strip: "Already claimed today - no bounty."
    /// Sizes are the cut pieces at about 0.4 canvas units per art pixel; the face windows are measured on the art.
    /// </summary>
    public sealed partial class WantedNotices
    {
        static readonly Vector2 TopCentre = new Vector2(0.5f, 1f);

        readonly RectTransform[] banners = new RectTransform[Kinds];
        readonly CanvasGroup[] groups = new CanvasGroup[Kinds];
        Image loginFace, respawnFace;
        Text loginName, loginMap, respawnText, areaDistance;
        WantedRewardRow loginRewards;
        int shownMetres = -1;

        RectTransform Banner(RectTransform safe, WantedNotice kind, float w, float h, string piece, Color fill)
        {
            RectTransform rt = UiWinKit.Anchored(safe, kind + "Notice", TopCentre, TopCentre, 0f, 0f, w, h);
            if (piece != null) PaperArt.Draw(UiKit.Fill(rt, "Art"), piece, fill, PaperTheme.BtnGoldEdge, h * 0.3f);
            CanvasGroup g = rt.gameObject.AddComponent<CanvasGroup>();
            g.blocksRaycasts = false; g.interactable = false;
            banners[(int)kind] = rt; groups[(int)kind] = g;
            rt.gameObject.SetActive(false);
            return rt;
        }

        void BuildBanners(RectTransform safe)
        {
            // Login: wanted/banner-login-laurel 559 x 170, face window centre (92, 85) radius 55.
            const float LoginW = 404f, LoginH = 80f, LoginS = LoginH / 170f;
            RectTransform login = Banner(safe, WantedNotice.Login, LoginW, LoginH, null, Color.clear);
            loginFace = PaperWanted.RingPortrait(login, 559f * LoginS, LoginH, PaperPiece.WantedBannerLoginPlate, 40f * LoginS, 33f * LoginS, 104f * LoginS);
            PaperArt.Draw(UiKit.Fill(login, "Art"), PaperPiece.WantedBannerLogin, PaperTheme.Paper, PaperTheme.BtnGoldEdge, 14f);
            PaperWanted.Label(login, "Today", 98f, 10f, 96f, 17f, "Today's Wanted");
            loginName = PaperTheme.Title(login, "Name", 98f, 28f, 160f, 22f, "", PaperTheme.SectionSize, PaperTheme.Ink);
            loginName.horizontalOverflow = HorizontalWrapMode.Overflow;
            PaperWanted.Icon(login, "Pin", 98f, 50f, 15f, PaperPiece.WantedIconPin);
            loginMap = PaperTheme.Body(login, "Map", 115f, 49f, 140f, 17f, "", PaperTheme.BodySize, PaperTheme.Red, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            loginMap.horizontalOverflow = HorizontalWrapMode.Overflow;
            PaperWanted.Label(login, "Reward", 298f, 10f, 58f, 17f, "Reward");
            loginRewards = new WantedRewardRow(login, 262f, 30f, 31f, 3f);

            // Respawn: wanted/banner-respawn-blue 550 x 153, face window centre (80, 77) radius 50.
            const float RespawnW = 340f, RespawnH = 60f, RespawnS = RespawnH / 153f;
            RectTransform respawn = Banner(safe, WantedNotice.Respawn, RespawnW, RespawnH, null, Color.clear);
            respawnFace = PaperWanted.RingPortrait(respawn, 550f * RespawnS, RespawnH, PaperPiece.WantedBannerRespawnPlate, 33f * RespawnS, 30f * RespawnS, 94f * RespawnS);
            PaperArt.Draw(UiKit.Fill(respawn, "Art"), PaperPiece.WantedBannerRespawn, PaperTheme.BtnBlueEdge, PaperTheme.BtnGoldEdge, 14f);
            respawnText = PaperTheme.Title(respawn, "Text", 70f, 0f, RespawnW - 100f, RespawnH, "", PaperTheme.SectionSize, PaperTheme.TitleGold, PaperInk.Outlined, TextAnchor.MiddleCenter);
            respawnText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Area: wanted/banner-area-map-arrow 443 x 133: map scroll on the left, the arrow plate on the right (the metres go under its arrow).
            const float AreaW = 372f, AreaH = 58f;
            RectTransform area = Banner(safe, WantedNotice.Area, AreaW, AreaH, PaperPiece.WantedBannerArea, PaperTheme.Paper);
            PaperTheme.Body(area, "Text", 68f, 0f, AreaW - 68f - 58f, AreaH, "A Wanted Boss lurks in this area.", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
            areaDistance = PaperTheme.Body(area, "Metres", AreaW - 60f, AreaH - 20f, 54f, 16f, "", PaperTheme.BarSize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter, true);
            areaDistance.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Warning: wanted/ribbon-warning-chains 388 x 165, the band is x 110..280, y 68..128.
            const float WarnW = 268f, WarnH = WarnW * 165f / 388f, WarnS = WarnW / 388f;
            RectTransform warning = Banner(safe, WantedNotice.Warning, WarnW, WarnH, PaperPiece.WantedRibbonWarning, PaperTheme.Red);
            PaperTheme.Title(warning, "Warning", 0f, 66f * WarnS, WarnW, 24f, "WARNING", 19, PaperTheme.TitleGold, PaperInk.Outlined, TextAnchor.MiddleCenter);
            PaperTheme.Title(warning, "Boss", 0f, 66f * WarnS + 21f, WarnW, 16f, "WANTED BOSS", 12, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter);

            // Already: wanted/strip-already-claimed 327 x 109 (the "no" sign is drawn on its left end).
            const float AlreadyW = 310f, AlreadyH = 42f;
            RectTransform already = Banner(safe, WantedNotice.Already, AlreadyW, AlreadyH, PaperPiece.WantedStripAlready, PaperTheme.WoodDark);
            PaperTheme.Body(already, "Text", 46f, 0f, AlreadyW - 60f, AlreadyH, "Already claimed today - no bounty.", PaperTheme.BodySize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter, true);
        }

        void Show(WantedNotice kind, WantedBossDef def)
        {
            RectTransform rt = banners[(int)kind];
            if (kind == WantedNotice.Login)
            {
                PaperWanted.SetFace(loginFace, def.BossType);
                loginName.text = def.BossName + " <color=#B5651D>Lv. " + def.BossLevel + "</color>";
                loginMap.text = WantedBossTracker.MapName(def);
                loginRewards.Set(def);
            }
            else if (kind == WantedNotice.Respawn)
            {
                PaperWanted.SetFace(respawnFace, def.BossType);
                respawnText.text = def.BossName + " has returned!";
            }
            else if (kind == WantedNotice.Area) shownMetres = -1;
            rt.gameObject.SetActive(true);
        }

        void Place()
        {
            RectTransform rt = banners[current];
            float y = -NotificationToast.AreaTop;
            if (!Mathf.Approximately(rt.anchoredPosition.y, y)) rt.anchoredPosition = new Vector2(0f, y);
        }

        void Hide(WantedNotice kind) { banners[(int)kind].gameObject.SetActive(false); }

        void SetAlpha(float a) { groups[current].alpha = a; }

        /// <summary>Metres (tiles) from the hero to the boss while the area banner shows; the text is rebuilt when the number changes.</summary>
        void RefreshDistance(HeroController hero)
        {
            int metres = Mathf.RoundToInt(WantedGuide.DistanceToBoss(hero) / GameConfig.TileUnits);
            if (metres == shownMetres) return;
            shownMetres = metres;
            areaDistance.text = metres + " m";
        }
    }
}
