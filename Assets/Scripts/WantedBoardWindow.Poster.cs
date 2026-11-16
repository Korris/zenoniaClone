using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The poster on the left of the Wanted board, in its three states (wanted-board-poster-states-*.png):
    ///   alive        colour picture, "ALIVE - go hunt!", the bounty, Show on Map + the NOT CLAIMED YET stamp
    ///   respawning   dimmed picture, "Respawns in 18:24" + "Come back when it returns", the rest as alive
    ///   claimed      CLAIMED stamp over the picture, "Bounty claimed - see you tomorrow.", the bounty ticked off, "No bounty for a second kill today."
    /// Show on Map turns the guide on (WantedGuide: arrow + distance + the skull on the minimap; the game has no world map) and closes the board;
    /// while the guide is on the button stops it.
    /// </summary>
    public sealed partial class WantedBoardWindow
    {
        const float PosterX = 212f, PosterY = 86f, PosterW = 360f, PosterH = 470f;
        static readonly Color Dimmed = new Color(0.5f, 0.55f, 0.66f); // the respawning picture: the art is tinted, there is no grey copy of it

        RectTransform poster, statusRt;
        Image picture, statusFace, statusIcon;
        Text nameText, levelText, mapText, statusText, comeBackText, noSecondText;
        WantedRewardRow rewards;
        PaperButton guideButton;
        GameObject stampOpen, stampClaimed, tickClaimed;

        void BuildPoster()
        {
            poster = UiKit.Rect(content, "Poster", PosterX, PosterY, PosterW, PosterH);
            PaperArt.Draw(poster, PaperPiece.WantedPoster, PaperTheme.Paper, PaperTheme.PaperEdge, 10f);
            RectTransform ribbon = UiKit.Rect(poster, "Daily", 52f, -10f, 256f, 36f);
            PaperArt.Draw(ribbon, PaperPiece.WantedRibbonDaily, PaperTheme.Red, PaperTheme.BtnRedEdge, 8f);
            Text daily = PaperTheme.Caption(ribbon, "DAILY WANTED BOSS", PaperTheme.BodySize, PaperTheme.TitleGold);
            daily.rectTransform.offsetMin = new Vector2(0f, 5f); // the band is the upper part of the ribbon
            PaperTheme.Title(poster, "Wanted", 0f, 28f, PosterW, 40f, "WANTED", 34, PaperWanted.TitleRed, PaperInk.Plain, TextAnchor.MiddleCenter);

            // The boss picture in its torn frame: the plate (white window of the frame) masks the first walk frame of the boss's own sheet.
            const float FrameX = 50f, FrameY = 70f, FrameW = 260f, FrameH = 150f, Pic = 196f;
            RectTransform plate = UiKit.Rect(poster, "PicturePlate", FrameX, FrameY, FrameW, FrameH);
            Image back = PaperArt.Draw(plate, PaperPiece.WantedPortraitPlate, PaperWanted.PortraitBack, Color.clear, 8f);
            back.color = PaperWanted.PortraitBack;
            plate.gameObject.AddComponent<Mask>();
            picture = UiKit.Icon(plate, "Picture", (FrameW - Pic) * 0.5f, (FrameH - Pic) * 0.5f - 4f, Pic);
            PaperWanted.Piece(poster, "PictureFrame", FrameX, FrameY, FrameW, FrameH, PaperPiece.WantedPortraitFrame, Color.clear);
            stampClaimed = PaperWanted.Piece(poster, "StampClaimed", 96f, 96f, 168f, 100f, PaperPiece.WantedStampClaimed, PaperTheme.LabelGreen).gameObject;

            nameText = PaperTheme.Title(poster, "Name", 20f, 222f, PosterW - 40f, 32f, "", 25, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter);
            nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform level = UiKit.Rect(poster, "Level", 62f, 257f, 82f, 26f);
            PaperArt.Draw(level, PaperPiece.WantedPlaqueDark, PaperTheme.WoodDark, PaperTheme.BtnGoldEdge, 8f);
            levelText = PaperTheme.Caption(level, "", PaperTheme.SectionSize, PaperTheme.TitleGold);
            PaperWanted.Icon(poster, "Pin", 158f, 259f, 20f, PaperPiece.WantedIconPin);
            mapText = PaperTheme.Title(poster, "Map", 180f, 257f, 160f, 26f, "", PaperTheme.SectionSize, PaperTheme.Red);
            mapText.horizontalOverflow = HorizontalWrapMode.Overflow;

            statusRt = UiKit.Rect(poster, "Status", 44f, 290f, 272f, 36f);
            statusFace = PaperArt.Draw(statusRt, PaperPiece.WantedPlaqueDark, PaperTheme.WoodDark, PaperTheme.BtnGoldEdge, 12f);
            statusIcon = PaperWanted.Icon(statusRt, "Icon", 22f, 5f, 26f, null);
            tickClaimed = PaperKit.Mark(statusRt, PaperMark.Check, 30f, 18f, 22f);
            statusText = PaperTheme.Title(statusRt, "Text", 48f, 0f, 204f, 34f, "", 17, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter);
            statusText.horizontalOverflow = HorizontalWrapMode.Overflow;
            comeBackText = PaperTheme.Body(poster, "ComeBack", 30f, 325f, PosterW - 60f, 26f, "Come back when it returns -\nthe bounty is still yours today.", PaperTheme.BarSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);

            PaperWanted.Label(poster, "RewardLabel", 44f, 351f, 70f, 18f, "REWARD");
            PaperTheme.Body(poster, "Once", 122f, 350f, 200f, 20f, "(Once per character per day)", PaperTheme.BarSize, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            rewards = new WantedRewardRow(poster, 71f, 371f, 44f, 14f);
            noSecondText = PaperTheme.Body(poster, "NoSecond", 20f, 417f, PosterW - 40f, 14f, "No bounty for a second kill today.", PaperTheme.BarSize, PaperTheme.Red, PaperInk.Plain, TextAnchor.MiddleCenter, true);

            guideButton = new PaperButton(poster, "ShowOnMap", 42f, 428f, 156f, 34f, "Show on Map", PaperTone.Blue, ToggleGuide);
            stampOpen = PaperWanted.Piece(poster, "StampOpen", 218f, 414f, 96f, 58f, PaperPiece.WantedStampOpen, PaperTheme.Red).gameObject;
        }

        void ToggleGuide()
        {
            WantedGuide.On = !WantedGuide.On;
            if (WantedGuide.On) Close(); // the arrow and the minimap skull are on the HUD behind the board
        }

        void RefreshPoster(WantedBossDef d, WantedPhase phase)
        {
            bool claimed = phase == WantedPhase.Claimed, respawning = phase == WantedPhase.Respawning;
            picture.sprite = PaperWanted.BossCell(d.BossType);
            picture.enabled = picture.sprite != null;
            picture.color = respawning ? Dimmed : Color.white;
            stampClaimed.SetActive(claimed);
            nameText.text = d.BossName;
            levelText.text = "Lv. " + d.BossLevel;
            mapText.text = WantedBossTracker.MapName(d);

            PaperArt.Redraw(statusFace, respawning ? PaperPiece.WantedPlaqueSteel : PaperPiece.WantedPlaqueDark, respawning ? PaperTheme.Steel : PaperTheme.WoodDark, PaperTheme.BtnGoldEdge, 12f);
            statusIcon.sprite = PaperArt.Get(respawning ? PaperPiece.WantedIconRespawn : PaperPiece.WantedIconAlive);
            statusIcon.enabled = !claimed && statusIcon.sprite != null;
            tickClaimed.SetActive(claimed);
            statusText.color = respawning ? PaperTheme.TextLight : PaperWanted.AliveGreen;
            statusText.fontSize = claimed ? PaperTheme.BodySize : 17;
            if (claimed) statusText.text = "Bounty claimed - see you tomorrow.";
            else if (!respawning) statusText.text = "ALIVE - go hunt!"; // respawning: RefreshClocks writes the countdown
            comeBackText.gameObject.SetActive(respawning);

            rewards.Set(d);
            rewards.SetClaimed(claimed);
            noSecondText.gameObject.SetActive(claimed);
            stampOpen.SetActive(!claimed);
            guideButton.Label.text = WantedGuide.On ? "Stop Guide" : "Show on Map";
            // Claimed: the stamp is gone, so the button moves to the middle, under the "no second bounty" line.
            guideButton.Root.anchoredPosition = claimed ? new Vector2((PosterW - 156f) * 0.5f, -433f) : new Vector2(42f, -428f);
        }
    }
}
