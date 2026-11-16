using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PetCampWindow, Capture (pet-camp-collection-growth-capture-tabs.png, panel 3; docs/pet-system-design.md 6): the two ball stations (Pet Balls:
    /// price, where, how many left to buy today, the boss drop; Rare Balls: progress of the Wanted Boss claims), how to capture in three steps, the
    /// odds of a normal ball by rarity, the rules for fallen bosses with today's limit, and what the selected species needs. Every number is PetRules;
    /// nothing is bought or thrown here (balls: Dale or Nara; the throw: the ball buttons over the Talk prompt in the world).
    /// </summary>
    public sealed partial class PetCampWindow
    {
        const float BallX = 176f, BallW = 290f, BallH = 262f, GuideX = 478f, GuideW = 650f, GuideInner = 602f;

        Text ballCount, ballText, rareCount, rareText, captureGate, captureBoss, captureSelected;
        Image captureIcon;

        void BuildCapture(RectTransform page)
        {
            RectTransform balls = BallStation(page, "PetBalls", 78f, "Pet Balls", PaperPiece.PetBall, PetArt.BallIcon(false), out ballCount, out ballText);
            RectTransform rare = BallStation(page, "RareBalls", 78f + BallH + 16f, "Rare Balls", PaperPiece.PetBallRare, PetArt.BallIcon(true), out rareCount, out rareText);
            balls.SetAsLastSibling(); rare.SetAsLastSibling();

            RectTransform inner = PaperKit.Inner(PaperKit.Frame(page, "Guide", GuideX, 60f, GuideW, 574f, PaperFrame.Paper), PaperFrame.Paper);
            PaperKit.Heading(inner, "How", -4f, -4f, 300f, "How to capture");
            string[] steps =
            {
                "Fight a monster to " + PetViewModel.Pct(PetRules.CaptureHpLine) + " HP or less. Only the one you hit in the last " + PetRules.CaptureHitMemoryMs / 1000 + " s counts.",
                "Stand within " + Mathf.RoundToInt(PetRules.CaptureRangeUnits / 16f) + " tiles of it.",
                "Tap the ball button over the Talk prompt to throw.",
            };
            for (int i = 0; i < steps.Length; i++)
            {
                float x = i * 204f;
                RectTransform num = UiKit.Rect(inner, "Step" + i, x, 26f, 26f, 26f);
                PaperArt.Plate(num, PaperTheme.BtnGold, PaperTheme.BtnGoldEdge, 13f);
                PaperTheme.Caption(num, (i + 1).ToString(), PaperTheme.SectionSize, PaperTheme.Ink, PaperInk.Plain);
                PaperTheme.Body(inner, "StepText" + i, x + 32f, 24f, 164f, 64f, steps[i], PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.UpperLeft, true);
            }
            PaperTheme.Body(inner, "More", 0f, 94f, GuideInner, 52f, "Under " + PetViewModel.Pct(PetRules.CaptureLowHp) + " HP the odds rise. A miss costs the ball; a refusal costs nothing. Caught: it vanishes with no loot or EXP and joins your collection (a species you own gives "
                + PetRules.DuplicateShards + " shards). A Rare Ball always catches a valid target.", PaperTheme.BarSize + 1, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.UpperLeft, true);

            PaperKit.SectionStrip(inner, "OddsStrip", 0f, 152f, GuideInner, 26f, "Odds with a normal ball  (half HP / under a quarter)");
            for (int i = 0; i <= (int)PetRarity.UR; i++)
            {
                float x = i * 120f;
                PetDef sample = null;
                foreach (PetDef d in PetConfig.All) if (!d.Legacy && (int)d.Rarity == i) { sample = d; break; }
                if (sample != null) new PetCampBadge(inner, "Odds" + i, x + 40f, 186f, 28f).Set(sample);
                PetRarity r = (PetRarity)i;
                PaperTheme.Body(inner, "OddsText" + i, x, 218f, 122f, 34f, r == PetRarity.UR ? "only from a\nfallen boss" : PetViewModel.Pct(PetRules.LiveCaptureChance(r, 0.5)) + " / " + PetViewModel.Pct(PetRules.LiveCaptureChance(r, 0.25)),
                    PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.UpperCenter, true);
            }
            captureGate = PaperTheme.Body(inner, "Gate", 0f, 258f, GuideInner, 22f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);

            RectTransform boss = PaperKit.Frame(inner, "Bosses", 0f, 288f, GuideInner, 116f, PaperFrame.Card, false);
            PaperKit.Mark(boss, PaperMark.AlertGold, 30f, 34f, 30f);
            captureBoss = PaperTheme.Body(boss, "Text", 56f, 12f, GuideInner - 74f, 92f, "", PaperTheme.BarSize + 1, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            RectTransform sel = PaperKit.Frame(inner, "Selected", 0f, 414f, GuideInner, 86f, PaperFrame.Card, false);
            captureIcon = UiKit.Icon(sel, "Icon", 14f, 12f, 62f);
            PaperKit.PageArrow(sel, "Prev", GuideInner - 78f, 27f, 32f, true, () => StepSpecies(-1));
            PaperKit.PageArrow(sel, "Next", GuideInner - 42f, 27f, 32f, false, () => StepSpecies(1));
            captureSelected = PaperTheme.Body(sel, "Text", 86f, 10f, GuideInner - 172f, 66f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
        }

        /// <summary>One ball station on the left: a paper with its sign, the big ball, how many the account holds and how to get more.</summary>
        static RectTransform BallStation(Transform page, string name, float y, string title, string picture, Sprite fallback, out Text count, out Text text)
        {
            RectTransform rt = PaperKit.Frame(page, name, BallX, y, BallW, BallH, PaperFrame.Pinned, true);
            Sign(rt, "Sign", BallW * 0.5f - 80f, -14f, 160f, 38f, title);
            Image ball = UiKit.Icon(rt, "Ball", BallW * 0.5f - 44f, 30f, 88f);
            Sprite art = PaperArt.Get(picture);
            ball.sprite = art != null ? art : fallback;
            ball.enabled = ball.sprite != null;
            count = PaperTheme.Title(rt, "Count", 0f, 120f, BallW, 28f, "", PaperTheme.TitleSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter);
            text = PaperTheme.Body(rt, "Text", 26f, 150f, BallW - 52f, 90f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.UpperCenter, true);
            return rt;
        }

        /// <summary>The next / previous species, owned or not (the arrows on the Selected card).</summary>
        void StepSpecies(int by)
        {
            int n = PetConfig.All.Length;
            selected = (Mathf.Clamp(selected, 0, n - 1) + by + n) % n;
            Refresh();
        }

        void RefreshCapture(PlayerState p, AccountState a)
        {
            int balls = a != null ? a.balls : 0, rare = a != null ? a.rareBalls : 0, bought = a != null ? a.ballsBoughtToday : 0, level = a != null ? a.CollectionLevel : 1;
            ballCount.text = "x " + balls;
            ballText.text = PetRules.BallPrice + " G each at Dale or Nara,\n" + (PetRules.BallsPerDay - bought) + " of " + PetRules.BallsPerDay + " left to buy today.\n" + PetViewModel.Pct(PetRules.BossBallChance) + " from every map boss.";
            rareCount.text = "x " + rare;
            rareText.text = "One per " + PetRules.WantedClaimsForRareBall + " Wanted Boss claims\n(" + (a != null ? a.wantedForRareBall : 0) + " / " + PetRules.WantedClaimsForRareBall + ").\nAlways catches a valid target.";
            captureGate.text = "Collection level " + level + ": a ball may catch up to " + (level >= 4 ? "UR" : level == 3 ? "SSR" : level == 2 ? "SR" : "R") + " pets (level 2 SR, 3 SSR, 4+ UR).";
            captureBoss.text = "Bosses are never caught alive. When a map boss falls, everyone with a real share may throw one ball within " + PetRules.BossCorpseMs / 1000 + " s (SSR "
                + PetViewModel.Pct(PetRules.BossCaptureChance(PetRarity.SSR, false)) + ", UR " + PetViewModel.Pct(PetRules.BossCaptureChance(PetRarity.UR, false)) + "); a Wanted Boss " + PetRules.WantedCorpseMs / 1000 + " s (SSR "
                + PetViewModel.Pct(PetRules.BossCaptureChance(PetRarity.SSR, true)) + ", UR " + PetViewModel.Pct(PetRules.BossCaptureChance(PetRarity.UR, true)) + "). One boss pet a day"
                + (a != null && a.dayBossCaught ? " <color=#B02A20>(done today)</color>." : ".");
            PetDef d = Shown;
            captureIcon.sprite = PetArt.Icon(d);
            captureIcon.enabled = captureIcon.sprite != null;
            captureIcon.color = PetArt.TintOf(d);
            string how = d.MonsterType == null ? "not a monster: never caught."
                : d.Rarity == PetRarity.UR ? "UR: a ball on the fallen " + d.Name + " only."
                : "a ball on a " + d.Name + " (" + d.MonsterType + ") at half HP: " + PetViewModel.Pct(PetRules.LiveCaptureChance(d.Rarity, 0.5)) + ", under a quarter: " + PetViewModel.Pct(PetRules.LiveCaptureChance(d.Rarity, 0.25))
                    + ". Needs collection level " + PetRules.CollectionLevelFor(d.Rarity) + ".";
            captureSelected.text = "<b>Selected: " + d.Name + "</b>\n" + Sentence(how);
        }
    }
}
