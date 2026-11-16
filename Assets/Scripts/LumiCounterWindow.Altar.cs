using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// LumiCounterWindow: the middle of the stall. The stage (Lumi with her speech bubble, the dais and the two side nests, hidden under a x10 result; the big egg, the
    /// featured banner and its ribbon while no result is shown) and, on the counter layer: the SSR+ / UR guarantee bars (pulls made of 20 / 50),
    /// the UR shard plaque (opens the exchange paper), Buy 1 / 10 coins with Pet Crystals and Pull x1 / x10.
    /// The cut plaque has no well for the shard count, so the count is written on its gold face beside the caption.
    /// </summary>
    public sealed partial class LumiCounterWindow
    {
        const float PityTag = 104f; // width the bar's label tab is stretched to, so "SSR+ Guarantee" fits on it
        const string BubbleIdle = "Summon adorable and powerful Pets! SSR and UR await!";

        GameObject showcase, props;
        Text bubbleText, plaqueCount, plaqueCaption;
        Image plaqueIcon;
        readonly RectTransform[] pityFill = new RectTransform[2];
        readonly Text[] pityValue = new Text[2];
        PaperGachaButton buy1, buy10, pull1, pull10;

        void BuildStage()
        {
            RectTransform set = UiKit.Rect(content, "Props", 0f, 0f, PaperLayout.DesignWidth, PaperLayout.DesignHeight);
            props = set.gameObject;
            Art(set, "Dais", PaperPiece.GachaDais, 259f, 296f, 354f, 133f); // 531 px wide art, scaled to the stall's stone ring
            Art(set, "NestPurple", PaperPiece.GachaNestPurple, 204f, 244f, 88f, 98f);
            Art(set, "NestGold", PaperPiece.GachaNestGold, 580f, 252f, 88f, 98f);
            RectTransform show = UiKit.Rect(content, "Showcase", 0f, 0f, PaperLayout.DesignWidth, PaperLayout.DesignHeight);
            showcase = show.gameObject;
            RectTransform banner = Art(show, "FeaturedBanner", PaperPiece.GachaBanner, 487f, 58f, 170f, 180f);
            PetDef ur = Featured;
            Image pet = UiKit.Icon(banner, "Pet", 42f, 24f, 86f); // on the cloth, above its notch
            pet.sprite = PetArt.Icon(ur);
            pet.color = PetArt.TintOf(ur);
            pet.enabled = pet.sprite != null;
            Art(show, "Egg", PaperPiece.GachaEgg, 364f, 166f, 143f, 160f);
            RectTransform ribbon = Art(show, "FeaturedRibbon", PaperPiece.GachaRibbonFeatured, 316f, 72f, 184f, 86f);
            Text feat = PaperTheme.Title(ribbon, "Text", 22f, 22f, 140f, 40f, "Featured <size=17>" + ur.Rarity + "</size>\n" + ur.Name, PaperTheme.BodySize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter);
            feat.lineSpacing = 0.8f;
            feat.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 9f); // the band of the ribbon rises to the right
            Art(content, "Lumi", PaperPiece.GachaLumi, -20f, 353f, 270f, 287f); // a bust cut flat at the hip: it stands on the bottom edge, the Buy buttons in front
            RectTransform bubble = Art(content, "Bubble", PaperPiece.GachaBubble, 30f, 226f, 150f, 130f);
            PaperTheme.Title(bubble, "Name", 0f, 12f, 150f, 20f, "Lumi", PaperTheme.SectionSize, PaperTheme.BtnGoldEdge, PaperInk.Plain, TextAnchor.MiddleCenter);
            bubbleText = PaperTheme.Body(bubble, "Text", 16f, 30f, 118f, 62f, BubbleIdle, PaperTheme.BarSize + 1, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
        }

        void BuildAltar()
        {
            BuildPity(0, 380f, "<color=#FFD45A>SSR+</color> Guarantee", PaperTheme.Gold);
            BuildPity(1, 412f, "<color=#D9A6FF>UR</color> Guarantee", PaperTheme.Purple);

            const float pw = 440f, ph = 58f, s = ph / 119f; // canvas units per native pixel of the plaque
            RectTransform plaque = UiKit.Rect(counter, "ShardExchange", 232f, 448f, pw, ph);
            Image face = PaperArt.Draw(plaque, PaperPiece.GachaPlaque, PaperTheme.Wood, PaperTheme.WoodDark, 10f);
            UiWinKit.MakeButton(face, () => OpenDialog(Dialog.Exchange, 0));
            plaqueIcon = UiKit.Icon(plaque, "Pet", 40f * s, 20f * s, 72f * s); // the solid portrait cell of the art
            float gx = 200f * s, gw = pw - gx - 36f * s;                       // its gold button face
            Image shard = UiKit.Icon(plaque, "Shard", gx + 8f, 17f, 22f);
            shard.sprite = PaperArt.Get(PaperPiece.IconShard);
            shard.enabled = shard.sprite != null;
            plaqueCount = PaperTheme.Title(plaque, "Count", gx + 32f, 10f, 96f, 36f, "", PaperTheme.SectionSize, PaperTheme.Ink);
            plaqueCaption = PaperTheme.Title(plaque, "Caption", gx + 122f, 10f, gw - 126f, 36f, "", PaperTheme.SectionSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter);

            Color gold = PaperTheme.BtnGold, goldEdge = PaperTheme.BtnGoldEdge;
            buy1 = new PaperGachaButton(counter, "Buy1", 20f, 546f, 152f, 62f, PaperPiece.GachaBtnBuy, gold, goldEdge, PaperTheme.Ink, "Buy 1 Coin", () => Buy(1), PaperPiece.IconPetCrystal);
            buy10 = new PaperGachaButton(counter, "Buy10", 178f, 546f, 152f, 62f, PaperPiece.GachaBtnBuy, gold, goldEdge, PaperTheme.Ink, "Buy 10 Coins", () => Buy(10), PaperPiece.IconPetCrystal);
            buy1.SetSub(Amount(PetRules.CrystalsPerPull));
            buy10.SetSub(Amount(PetRules.CrystalsPerPull * 10));
            pull1 = new PaperGachaButton(counter, "Pull1", 350f, 538f, 170f, 72f, PaperPiece.GachaBtnPullBlue, PaperTheme.BtnBlue, PaperTheme.BtnBlueEdge, PaperTheme.TextLight, "Pull x1", () => Pull(1), PaperPiece.IconGachaCoin);
            pull10 = new PaperGachaButton(counter, "Pull10", 528f, 538f, 170f, 72f, PaperPiece.GachaBtnPullPurple, PaperTheme.Purple, PaperTheme.BtnPurpleEdge, PaperTheme.TextLight, "Pull x10", () => Pull(PetRules.MaxPullsAtOnce), PaperPiece.IconGachaCoin);
            pull1.SetSub("x1");
            pull10.SetSub("x" + PetRules.MaxPullsAtOnce);
        }

        /// <summary>One guarantee bar: label tab, track with a fill in `color`, "made / needed" and the round help button (the rules paper).</summary>
        void BuildPity(int i, float y, string label, Color color)
        {
            const float x = 248f, w = 356f, h = 30f;
            RectTransform rt = UiKit.Rect(counter, "Pity" + i, x, y, w, h);
            Image frame = PaperArt.Draw(rt, PaperPiece.GachaPityBar, PaperTheme.Plate, PaperTheme.PlateEdge, h * 0.5f);
            bool art = frame.sprite != PaperArt.Round();
            if (art) frame.pixelsPerUnitMultiplier = 170f / PityTag; // the tab is the left border of the 9-slice (170 px)
            Text tag = PaperTheme.Title(rt, "Tag", 4f, 0f, PityTag - 12f, h, label, PaperTheme.BarSize + 1, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter);
            tag.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform track = UiKit.Rect(rt, "Track", PityTag + 2f, h * 0.2f, w - PityTag - 14f, h * 0.6f);
            pityFill[i] = UiKit.Fill(track, "Fill");
            PaperArt.Draw(pityFill[i], PaperPiece.BarFill, color, Color.clear, h * 0.3f).color = color;
            pityValue[i] = PaperTheme.Body(rt, "Value", PityTag, 0f, w - PityTag - 22f, h, "", PaperTheme.BodySize, PaperTheme.TextLight, PaperInk.World, TextAnchor.MiddleRight, true);
            RectTransform help = UiKit.Rect(counter, "Help" + i, x + w + 4f, y + 2f, 26f, 26f);
            Image q = PaperArt.Draw(help, PaperPiece.GachaHelp, PaperTheme.Gold, PaperTheme.BtnGoldEdge, 13f);
            if (q.sprite == PaperArt.Round()) PaperTheme.Caption(help, "?", PaperTheme.SectionSize, PaperTheme.Ink, PaperInk.Plain);
            UiWinKit.MakeButton(q, () => OpenDialog(Dialog.Rules, 0));
        }

        void RefreshAltar(AccountState a, bool result)
        {
            showcase.SetActive(!result);
            props.SetActive(resultCount <= 1); // the ten cards of a x10 result take the place of the dais and the nests
            if (!result) bubbleText.text = BubbleIdle;
            SetPity(0, a.pitySSR, PetRules.SsrPity);
            SetPity(1, a.pityUR, PetRules.UrPity);
            PetDef ur = Featured;
            plaqueIcon.sprite = PetArt.Icon(ur);
            plaqueIcon.color = PetArt.TintOf(ur);
            plaqueIcon.enabled = plaqueIcon.sprite != null;
            bool owned = a.OwnsPet(ur.Id), enough = a.urShards >= PetRules.UrExchangeCost;
            plaqueCount.text = (enough ? "" : "<color=#B3261E>") + a.urShards + (enough ? "" : "</color>") + " / " + PetRules.UrExchangeCost;
            plaqueCaption.text = owned ? ur.Name + " owned" : "Shards → " + ur.Name;
            buy1.SetLit(a.crystals >= PetRules.CrystalsPerPull);
            buy10.SetLit(a.crystals >= PetRules.CrystalsPerPull * 10);
            pull1.SetLit(a.tickets >= 1 && pendingCount == 0);
            pull10.SetLit(a.tickets >= PetRules.MaxPullsAtOnce && pendingCount == 0);
        }

        void SetPity(int i, int made, int needed)
        {
            pityFill[i].anchorMax = new Vector2(Mathf.Clamp01((float)made / needed), 1f);
            pityFill[i].gameObject.SetActive(made > 0);
            pityValue[i].text = made + " / " + needed;
        }
    }
}
