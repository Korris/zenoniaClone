using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;
using B = ZenoniaProto.InventoryRoBoxes;
using PW = ZenoniaProto.PetRoWidgets;
using W = ZenoniaProto.InventoryRoWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// PetRoWindow, the other three menu pages (the right card stays): Collection = every species, owned or not, with the collection level and how
    /// to get the selected one; Growth = Pet Food levels, stars, shared shards and the two free resets (PetGrowth); Capture = balls, the odds by rarity
    /// and the boss rules (PetRules). Every number comes from the rules tables; buying balls and throwing them stay where they are (shop, world).
    /// </summary>
    public sealed partial class PetRoWindow
    {
        readonly PW.Card[] collectionCards = new PW.Card[10];
        Text collectionTitle, collectionText, growthLevel, growthStars, growthShards, captureBalls, captureRules;
        Image growthIcon;

        Text PageTitle(RectTransform page, string text)
        {
            W.Img(page, "TitleIcon", 14f, 10f, 26f, 26f, "pet-title-paw");
            return W.Txt(page, "Title", 46f, 4f, 480f, 36f, text, 21, W.Ink);
        }

        Text Body(RectTransform parent, string name, float x, float y, float w, float h)
        {
            Text t = W.Txt(parent, name, x, y, w, h, "", 14, W.Ink, TextAnchor.UpperLeft, false);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            return t;
        }

        RectTransform Card(RectTransform page, string name, float y, float h)
        {
            Image c = W.Img(page, name, 12f, y, CentreW - 24f, h, "bag-pill-white");
            c.pixelsPerUnitMultiplier = 104f / 36f;
            return c.rectTransform;
        }

        // ---- Collection ----

        void BuildCollection(RectTransform page)
        {
            collectionTitle = PageTitle(page, "");
            for (int i = 0; i < collectionCards.Length; i++)
            {
                int k = i;
                collectionCards[i] = new PW.Card(page, "Species" + i, CardX0 + (i % 5) * CardStep, 50f + (i / 5) * CardRowStep, CardSize, () => Select(k));
            }
            collectionText = Body(Card(page, "How", 250f, 284f), "Text", 16f, 12f, CentreW - 56f, 260f);
        }

        void RefreshCollection(PlayerState p, AccountState a)
        {
            int species = a != null ? a.Species : 0, level = a != null ? a.CollectionLevel : 1;
            collectionTitle.text = "Collection (" + species + "/" + PetConfig.All.Length + ")   <size=16><color=#5F7488>Collection Lv. " + level + "</color></size>";
            for (int i = 0; i < collectionCards.Length; i++)
            {
                PetDef d = PetConfig.ByIndex(i);
                collectionCards[i].Set(d, d != null && a != null ? a.Pet(d.Id) : null, i == selected);
            }
            PetDef s = Selected ?? PetConfig.All[0];
            bool owned = a != null && a.OwnsPet(s.Id);
            string next = level - 1 < PetRules.CollectionSpecies.Length ? "Next level at " + PetRules.CollectionSpecies[level - 1] + " species." : "Top collection level.";
            collectionText.text = "<b>" + s.Name + "</b>  " + (owned ? "<color=#2E9E57>owned</color>" : "<color=#5F7488>not owned</color>") + "\n" + PetViewModel.HowToGet(s, a)
                + "\n\n<b>Collection level " + level + "</b>: a ball can catch up to " + (level >= 4 ? "UR" : level == 3 ? "SSR" : level == 2 ? "SR" : "R") + " pets. " + next;
        }

        // ---- Growth ----

        void BuildGrowth(RectTransform page)
        {
            PageTitle(page, "Growth");
            growthIcon = UiKit.Rect(page, "Icon", CentreW - 96f, 6f, 80f, 64f).gameObject.AddComponent<Image>();
            growthIcon.preserveAspect = true; growthIcon.raycastTarget = false;
            RectTransform lv = Card(page, "Level", 76f, 140f);
            growthLevel = Body(lv, "Text", 16f, 10f, CentreW - 210f, 120f);
            new B.IconButton(lv, "LevelUp", CentreW - 178f, 18f, 140f, 46f, "btn-mint", "pet-nav-growth", "Level up", 16, LevelUp);
            new B.IconButton(lv, "ResetLevel", CentreW - 178f, 74f, 140f, 46f, "btn-ivory", null, "Reset level", 15, () => SendSelected(InvOp.PetResetLevel)).Label.color = W.Ink;
            RectTransform st = Card(page, "Stars", 226f, 140f);
            growthStars = Body(st, "Text", 16f, 10f, CentreW - 210f, 120f);
            new B.IconButton(st, "StarUp", CentreW - 178f, 18f, 140f, 46f, "btn-gold", "pet-star", "Star up", 16, StarUp);
            new B.IconButton(st, "ResetStars", CentreW - 178f, 74f, 140f, 46f, "btn-ivory", null, "Reset stars", 15, () => SendSelected(InvOp.PetResetStars)).Label.color = W.Ink;
            RectTransform sh = Card(page, "Shards", 376f, 150f);
            growthShards = Body(sh, "Text", 16f, 10f, CentreW - 210f, 130f);
            new B.IconButton(sh, "Convert10", CentreW - 178f, 18f, 140f, 46f, "btn-sky", null, "Convert 10", 16, () => ConvertShards(10));
            new B.IconButton(sh, "ConvertAll", CentreW - 178f, 74f, 140f, 46f, "btn-sky", null, "Convert all", 16, () => ConvertShards(-1));
        }

        void RefreshGrowth(PlayerState p, AccountState a)
        {
            PetDef d = Selected ?? PetConfig.All[0];
            OwnedPet o = a != null ? a.Pet(d.Id) : null;
            growthIcon.sprite = ZenoniaProto.PetArt.Icon(d);
            growthIcon.color = o != null ? ZenoniaProto.PetArt.TintOf(d) : new Color(0.4f, 0.45f, 0.5f, 0.6f);
            if (o == null)
            {
                growthLevel.text = "<b>" + d.Name + "</b> is not in your collection yet.\n" + PetViewModel.HowToGet(d, a);
                growthStars.text = growthShards.text = "";
                return;
            }
            growthLevel.text = "<b>Level " + o.level + " / " + PetRules.MaxLevel + "</b>  (" + d.Name + ")\n" + (o.level >= PetRules.MaxLevel ? "Top level." : "Next level: " + PetRules.FoodForLevel(o.level) + " Pet Food (you have " + a.food + ").")
                + "\nEach level: +0.5% pet damage. A pet never outlevels your best hero. Reset: every Pet Food back (" + o.foodSpent + ").";
            int shards, gold;
            bool more = PetGrowth.NextStarCost(a, d.Id, out shards, out gold);
            growthStars.text = "<b>" + o.stars + " / " + PetRules.MaxStars + " stars</b>  passive " + PetConfig.PassiveText(d, o.stars) + "\n"
                + (more ? "Next star: " + shards + " " + d.Name + " shards (" + o.shards + ") + " + N(gold) + " G (" + N(p.Gold) + "). Always succeeds." : "Most stars.")
                + "\nReset: every shard and all gold back (" + o.shardsSpent + " shards, " + N(o.goldSpent) + " G).";
            growthShards.text = "<b>Shared shards: " + a.sharedShards + "</b>\nFrom the gacha's shard results; they turn 1 : 1 into " + d.Name + " shards.\nA duplicate pet gives " + PetRules.DuplicateShards + " shards of its kind.";
        }

        void SendSelected(InvOp op)
        {
            PetDef d;
            if (!OwnedSelected(out d)) return;
            Send(op, (byte)selected);
            Refresh();
        }

        void ConvertShards(int n)
        {
            PetDef d;
            AccountState a = PetActions.Account;
            if (!OwnedSelected(out d)) return;
            if (a.sharedShards <= 0) { Flash("No shared shards"); return; }
            PetViewModel.Convert(selected, n);
            Refresh();
        }

        // ---- Capture ----

        void BuildCapture(RectTransform page)
        {
            PageTitle(page, "Capture");
            RectTransform balls = Card(page, "Balls", 50f, 120f);
            W.Img(balls, "Ball", 18f, 10f, 48f, 48f, null).sprite = PW.Picture("Sprites/ui/paper/kit/icon-capture-ball");
            W.Img(balls, "Rare", 20f, 64f, 44f, 44f, null).sprite = PW.Picture("Sprites/ui/paper/hud/icon-capture-ball-rare");
            foreach (Image i in balls.GetComponentsInChildren<Image>()) if (i.transform != balls) { i.preserveAspect = true; i.type = Image.Type.Simple; }
            captureBalls = Body(balls, "Text", 86f, 10f, CentreW - 120f, 104f);
            captureRules = Body(Card(page, "Rules", 180f, 354f), "Text", 16f, 12f, CentreW - 56f, 330f);
        }

        void RefreshCapture(PlayerState p, AccountState a)
        {
            int bought = a != null ? a.ballsBoughtToday : 0, level = a != null ? a.CollectionLevel : 1;
            captureBalls.text = "<b>Pet Balls: " + (a != null ? a.balls : 0) + "</b>   " + PetRules.BallPrice + " G each at Dale or Nara, " + (PetRules.BallsPerDay - bought) + " of " + PetRules.BallsPerDay + " left today; " + PetViewModel.Pct(PetRules.BossBallChance) + " from map bosses."
                + "\n<b>Rare Balls: " + (a != null ? a.rareBalls : 0) + "</b>   one per " + PetRules.WantedClaimsForRareBall + " Wanted Boss claims (" + (a != null ? a.wantedForRareBall : 0) + "/" + PetRules.WantedClaimsForRareBall + "); always catches a valid target.";
            string odds = "";
            for (int r = 0; r <= (int)PetRarity.SSR; r++) odds += (r > 0 ? ",  " : "") + (PetRarity)r + " " + PetViewModel.Pct(PetRules.LiveCaptureChance((PetRarity)r, 0.5)) + " / " + PetViewModel.Pct(PetRules.LiveCaptureChance((PetRarity)r, 0.25));
            captureRules.text = "<b>How to capture</b>\n1. Fight a monster to " + PetViewModel.Pct(PetRules.CaptureHpLine) + " HP or less (hit it in the last " + PetRules.CaptureHitMemoryMs / 1000 + " s).\n2. Stand within "
                + Mathf.RoundToInt(PetRules.CaptureRangeUnits / 16f) + " tiles.\n3. Tap the ball button over the Talk prompt.\n\n<b>Odds, half HP / under a quarter</b>\n" + odds + ". UR only from a fallen boss.\n\n<b>Bosses</b>: never caught alive; after a map boss falls everyone with a share may throw within "
                + PetRules.BossCorpseMs / 1000 + " s (SSR " + PetViewModel.Pct(PetRules.BossCaptureChance(PetRarity.SSR, false)) + ", UR " + PetViewModel.Pct(PetRules.BossCaptureChance(PetRarity.UR, false)) + "), a Wanted Boss " + PetRules.WantedCorpseMs / 1000
                + " s. One boss pet a day" + (a != null && a.dayBossCaught ? " (done today)" : "") + ".\n\nCollection level " + level + ": up to " + (level >= 4 ? "UR" : level == 3 ? "SSR" : level == 2 ? "SR" : "R") + " pets.";
        }
    }
}
