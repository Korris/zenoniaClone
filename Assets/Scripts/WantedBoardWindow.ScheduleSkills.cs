using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Right column of the Wanted board: "Rotation Schedule (3 Bosses)" = yesterday, today (gold frame), tomorrow with their bounty
    /// (WantedBossTracker.Schedule), and "Boss Skills &amp; How to Dodge" = the four skills of today's boss (WantedSkillTable): name, kind,
    /// the two-sentence hint (WantedSkillDef.Hint) and a diagram picked from the skill's shape. The enrage skill is marked "(Below 30% HP)".
    /// </summary>
    public sealed partial class WantedBoardWindow
    {
        const float ColumnX = 580f, ColumnW = 470f, CardW = 146f, CardH = 148f, RowH = 47f;
        const int SkillRows = 4;
        static readonly string[] DayNames = { "Yesterday", "Today", "Tomorrow" };
        static readonly string[] DayTags = { "grey", "red", "purple" };

        sealed class DayCard
        {
            public Image Picture, Coin, Stone, HpPotion, MpPotion;
            public Text Name, Level, Map, Gold, Stones;
            public WantedBossDef Shown;
        }

        sealed class SkillRow
        {
            public GameObject Root;
            public Image Ring, Diagram;
            public Text Name, Kind, Hint;
        }

        readonly DayCard[] days = new DayCard[3];
        readonly SkillRow[] skillRows = new SkillRow[SkillRows];
        WantedBossDef shownSkillsOf;

        /// <summary>Cream tag heading a block of the right column.</summary>
        void SectionTag(string name, float y, float w, string text)
        {
            RectTransform tag = UiKit.Rect(content, name, ColumnX + 8f, y, w, 30f);
            PaperArt.Draw(tag, PaperPiece.WantedSectionTag, PaperTheme.Paper, PaperTheme.BtnGoldEdge, 8f);
            PaperTheme.Caption(tag, text, PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain);
        }

        void BuildSchedule()
        {
            RectTransform panel = PaperKit.Frame(content, "Schedule", ColumnX, 100f, ColumnW, 168f, PaperFrame.Card, false);
            for (int i = 0; i < 3; i++)
            {
                var d = days[i] = new DayCard();
                RectTransform card = UiKit.Rect(panel, DayNames[i], 10f + i * (CardW + 6f), 14f, CardW, CardH);
                PaperArt.Draw(card, PaperPiece.WantedScheduleCard, PaperTheme.Cream, PaperTheme.PaperEdge, 8f);
                if (i == 1) PaperArt.Draw(UiKit.Fill(card, "TodayFrame", -3f), PaperPiece.WantedScheduleToday, Color.clear, PaperTheme.Gold, 8f);
                d.Picture = UiKit.Icon(card, "Picture", (CardW - 62f) * 0.5f, 14f, 62f);
                d.Name = PaperTheme.Body(card, "Name", 0f, 76f, CardW, 16f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
                d.Level = PaperTheme.Body(card, "Level", 0f, 91f, CardW, 14f, "", PaperTheme.BarSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
                d.Map = PaperTheme.Body(card, "Map", 0f, 104f, CardW, 14f, "", PaperTheme.BarSize, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.MiddleCenter, true);
                d.Name.horizontalOverflow = d.Map.horizontalOverflow = HorizontalWrapMode.Overflow;
                // Bounty row: coin + gold, stone + count, the two potions. Today's card has none on the mockup: its bounty is on the poster.
                if (i != 1)
                {
                    d.Coin = UiKit.Icon(card, "Coin", 12f, 124f, 15f);
                    d.Gold = PaperTheme.Body(card, "Gold", 29f, 123f, 46f, 17f, "", PaperTheme.BarSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
                    d.Stone = UiKit.Icon(card, "Stone", 70f, 124f, 15f);
                    d.Stones = PaperTheme.Body(card, "Stones", 87f, 123f, 22f, 17f, "", PaperTheme.BarSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
                    d.HpPotion = UiKit.Icon(card, "HpPotion", 106f, 123f, 16f);
                    d.MpPotion = UiKit.Icon(card, "MpPotion", 122f, 123f, 16f);
                    d.Gold.horizontalOverflow = HorizontalWrapMode.Overflow;
                }
                RectTransform tag = UiKit.Rect(card, "Day", (CardW - 76f) * 0.5f, -12f, 76f, 34f);
                PaperArt.Draw(tag, PaperPiece.WantedDayTagPrefix + DayTags[i], i == 1 ? PaperTheme.Red : i == 0 ? PaperTheme.Steel : PaperTheme.Purple, Color.clear, 6f);
                Text day = PaperTheme.Caption(tag, DayNames[i], PaperTheme.BarSize + 1, PaperTheme.TextLight);
                day.rectTransform.offsetMin = new Vector2(0f, 8f); // the band is the upper part, the tails hang below
            }
            SectionTag("ScheduleTag", 74f, 226f, "Rotation Schedule (3 Bosses)");
        }

        void RefreshSchedule(WantedBossDef[] schedule)
        {
            for (int i = 0; i < 3; i++)
            {
                DayCard c = days[i];
                WantedBossDef d = schedule[i];
                if (c.Shown == d) continue;
                c.Shown = d;
                c.Picture.sprite = PaperWanted.BossCell(d.BossType);
                c.Picture.enabled = c.Picture.sprite != null;
                c.Name.text = d.BossName;
                c.Level.text = "Lv. " + d.BossLevel;
                c.Map.text = WantedBossTracker.MapName(d);
                if (c.Gold == null) continue;
                c.Coin.sprite = LootSprites.Coin(); c.Gold.text = d.Gold.ToString("#,0") + " G";
                c.Stone.sprite = LootSprites.Stone(); c.Stones.text = "x" + d.Stones;
                c.HpPotion.sprite = LootSprites.Potion(true, d.PotionSize);
                c.MpPotion.sprite = LootSprites.Potion(false, d.PotionSize);
            }
        }

        void BuildSkills()
        {
            RectTransform panel = PaperKit.Frame(content, "Skills", ColumnX, 298f, ColumnW, 200f, PaperFrame.Card, false);
            for (int i = 0; i < SkillRows; i++)
            {
                var r = skillRows[i] = new SkillRow();
                RectTransform row = UiKit.Rect(panel, "Skill" + i, 10f, 7f + i * RowH, ColumnW - 20f, RowH);
                r.Root = row.gameObject;
                if (i > 0) PaperArt.Plate(UiKit.Rect(row, "Line", 44f, 0f, ColumnW - 72f, 1f), PaperTheme.PaperEdge, Color.clear, 1f);
                // No icons are drawn for the boss skills yet: the medal shows its plain plate (blue, red for the enrage skill).
                RectTransform plate = UiKit.Rect(row, "Plate", 2f, 3f, 40f, 41f);
                r.Ring = PaperArt.Draw(plate, PaperPiece.WantedSkillPlate, PaperTheme.Navy, Color.clear, 20f);
                PaperWanted.Piece(row, "Ring", 2f, 3f, 40f, 41f, PaperPiece.WantedSkillRing, Color.clear);
                r.Name = PaperTheme.Body(row, "Name", 50f, 6f, 132f, 18f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
                r.Kind = PaperTheme.Body(row, "Kind", 50f, 23f, 126f, 16f, "", PaperTheme.BarSize, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.MiddleLeft, true);
                r.Kind.horizontalOverflow = HorizontalWrapMode.Overflow;
                r.Name.resizeTextForBestFit = true; r.Name.resizeTextMinSize = PaperTheme.BarSize; r.Name.resizeTextMaxSize = PaperTheme.BodySize; // "Frozen Ground Stomp" stays in its column
                r.Hint = PaperTheme.Body(row, "Hint", 188f, 4f, 172f, 40f, "", PaperTheme.BarSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
                r.Diagram = UiKit.Icon(row, "Diagram", 362f, 4f, 84f);
                r.Diagram.rectTransform.sizeDelta = new Vector2(84f, 40f); // the dodge pictures are 200 x 96
            }
            SectionTag("SkillsTag", 272f, 236f, "Boss Skills & How to Dodge");
        }

        void RefreshSkills(WantedBossDef boss)
        {
            if (boss == shownSkillsOf) return;
            shownSkillsOf = boss;
            WantedSkillDef[] skills = WantedSkillTable.For(boss.BossType);
            for (int i = 0; i < SkillRows; i++)
            {
                SkillRow r = skillRows[i];
                bool has = skills != null && i < skills.Length;
                r.Root.SetActive(has);
                if (!has) continue;
                WantedSkillDef s = skills[i];
                r.Name.text = s.Name;
                r.Kind.text = s.Enrage ? "(Below " + Mathf.RoundToInt(WantedSkillClock.EnrageHpFraction * 100f) + "% HP)" : KindOf(s);
                r.Kind.color = s.Enrage ? PaperTheme.Red : PaperTheme.InkSoft;
                r.Ring.color = s.Enrage ? PaperTheme.BtnRedEdge : PaperTheme.Navy;
                string hint = s.Hint ?? "";
                int cut = hint.IndexOf(". "); // "What it is. What to do." on two lines
                r.Hint.text = cut > 0 ? hint.Substring(0, cut + 1) + "\n" + hint.Substring(cut + 2) : hint;
                r.Diagram.sprite = PaperArt.Get(PaperPiece.WantedDodgePrefix + DiagramOf(s));
                r.Diagram.enabled = r.Diagram.sprite != null;
            }
        }

        static string KindOf(WantedSkillDef s)
        {
            if (s.Shape == WantedAreaShape.Line) return "(Line Attack)";
            if (s.Shape == WantedAreaShape.Cone) return "(Fan Attack)";
            if (s.OnPlayers) return s.Count > 1 ? "(Multi-Circle)" : "(Target Circle)";
            return "(Circle Attack)";
        }

        /// <summary>
        /// Sheet W1 has four pictures: line, fan, circles on players, circle with a safe lane. A plain circle around the boss ("back away") has none
        /// of its own and borrows the circles picture.
        /// </summary>
        static string DiagramOf(WantedSkillDef s)
        {
            if (s.Shape == WantedAreaShape.Line) return "line";
            if (s.Shape == WantedAreaShape.Cone) return "fan";
            return s.SafeArcCount > 0 ? "safe-lane" : "multi-circle";
        }
    }
}
