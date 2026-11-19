using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;
using W = ZenoniaProto.InventoryRoWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// RoGachaWindow, left column: Lumi (the keeper's existing bust, Sprites/ui/paper/gacha/lumi-bust) with her name and bubble, the angel
    /// mascot, the Guarantee panel (pulls since the last SSR+ / UR against PetRules.SsrPity / UrPity), the UR shard exchange
    /// (urShards / UrExchangeCost, Exchange sends InvOp.GachaExchange) and Buy Gacha Coins (popup: Pet Crystals -> coins).
    /// </summary>
    public sealed partial class RoGachaWindow
    {
        Text bubbleText, shardName;
        Bar ssrBar, urBar, shardBar;
        Image shardPet;
        InventoryRoBoxes.IconButton exchangeButton;

        void BuildLeft()
        {
            Image lumi = UiKit.Rect(window, "Lumi", X(52f), Y(98f), D(302f) * 424f / 451f, D(302f)).gameObject.AddComponent<Image>();
            Texture2D tex = Resources.Load<Texture2D>("Sprites/ui/paper/gacha/lumi-bust");
            if (tex != null) lumi.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            lumi.preserveAspect = true;
            lumi.raycastTarget = false;
            W.Img(window, "Mascot", X(388f), Y(312f), D(118f), D(90f), "bag-mascot");

            Image bubble = W.Img(window, "Bubble", X(318f), Y(150f), D(230f), D(150f), "wanted-hint-bubble");
            bubble.pixelsPerUnitMultiplier = 0.55f; // rounder corners, as the mockup's bubble
            bubbleText = W.Txt(bubble.transform, "Text", D(40f), D(14f), D(172f), D(122f), "", 13, W.Ink, TextAnchor.MiddleCenter, false);
            bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bubbleText.lineSpacing = 1.05f;
            var name = InventoryRoBoxes.Pill(window, "LumiName", X(328f), Y(136f), D(34f), "btn-sky", "Lumi", 14, Color.white, D(86f));
            W.Rim(name.Label);

            Image panelImg = W.Img(window, "GuaranteePanel", X(50f), Y(402f), D(466f), D(344f), "bag-section");
            panelImg.color = new Color(1f, 1f, 1f, 0.75f); // softer rim: the mockup panels are glassy
            RectTransform panel = panelImg.rectTransform;
            float px = X(50f), py = Y(402f);
            System.Func<float, float> lx = mx => X(mx) - px, ly = my => Y(my) - py; // mockup px -> panel units

            Text pity = W.Txt(panel, "PityTitle", lx(84f), ly(424f), 220f, D(34f), "Guarantee (Pity)", 18, W.Ink);
            W.Clickable(W.Img(panel, "PityInfo", lx(84f) + pity.preferredWidth + 8f, ly(426f), D(32f), D(32f), "gacha-alert", true), () => OpenPopup(Popup.Rules, 0));
            W.Img(panel, "PityBox", lx(64f), ly(464f), D(438f), D(112f), "slot").color = new Color(1f, 1f, 1f, 0.6f);
            RarityPill(panel, "SsrPill", lx(84f), ly(477f), D(36f), PetRarity.SSR, 15, D(78f));
            RarityPill(panel, "UrPill", lx(84f), ly(526f), D(36f), PetRarity.UR, 15, D(78f));
            ssrBar = new Bar(panel, "SsrBar", lx(174f), ly(479f), D(310f), D(30f), "gacha-bar-gold", 14);
            urBar = new Bar(panel, "UrBar", lx(174f), ly(528f), D(310f), D(30f), "gacha-bar-purple", 14);

            W.Img(panel, "ShardIcon", lx(78f), ly(592f), D(30f), D(30f), "gacha-ico-shard").color = W.Ink; // white glyph art, tinted
            W.Txt(panel, "ShardTitle", lx(112f), ly(590f), 260f, D(34f), "Exchange UR Pet Shards", 18, W.Ink);
            W.Img(panel, "ShardBox", lx(64f), ly(632f), D(438f), D(98f), "slot").color = new Color(1f, 1f, 1f, 0.6f);
            Image frame = W.Img(panel, "ShardFrame", lx(82f), ly(644f), D(74f), D(74f), "gacha-shard-frame");
            shardPet = UiKit.Rect(frame.transform, "Pet", D(12f), D(12f), D(50f), D(50f)).gameObject.AddComponent<Image>();
            shardPet.preserveAspect = true;
            shardPet.raycastTarget = false;
            shardName = W.Txt(panel, "ShardName", lx(170f), ly(650f), D(210f), D(28f), "", 13, W.Ink);
            shardBar = new Bar(panel, "ShardBar", lx(170f), ly(684f), D(202f), D(30f), "gacha-bar-purple", 14);
            exchangeButton = new InventoryRoBoxes.IconButton(panel, "Exchange", lx(386f), ly(658f), D(108f), D(50f), "btn-grey", null, "Exchange", 14, Exchange);

            var buy = new InventoryRoBoxes.IconButton(window, "BuyCoins", X(97f), Y(758f), D(370f), D(68f), "btn-ivory", "gacha-coin-stack", "Buy Gacha Coins", 17, () => OpenPopup(Popup.Buy, 0));
            buy.Label.color = W.Ink;
            buy.Root.GetComponent<Image>().pixelsPerUnitMultiplier = 0.72f; // pill-round like the mockup
            buy.Label.GetComponent<Shadow>().enabled = false;
            Transform icon = buy.Root.Find("Icon");
            if (icon != null) icon.GetComponent<LayoutElement>().minWidth = icon.GetComponent<LayoutElement>().preferredWidth = 34f; // the coin stack reads bigger than a line icon
            if (icon != null) icon.GetComponent<LayoutElement>().minHeight = icon.GetComponent<LayoutElement>().preferredHeight = 30f;
        }

        void Exchange()
        {
            AccountState a = Account;
            PetDef ur = Featured;
            if (a == null || ur == null) return;
            if (a.OwnsPet(ur.Id)) { Flash(ur.Name + " is already yours."); return; }
            if (a.urShards < PetRules.UrExchangeCost) { Flash("Need " + (PetRules.UrExchangeCost - a.urShards) + " more UR shards."); return; }
            PetActions.Send(InvOp.GachaExchange, 0);
        }

        void RefreshLeft(AccountState a)
        {
            ssrBar.Set(a.pitySSR, PetRules.SsrPity);
            urBar.Set(a.pityUR, PetRules.UrPity);
            PetDef ur = Featured;
            shardPet.sprite = PetArt.Icon(ur);
            shardPet.enabled = shardPet.sprite != null;
            shardName.text = ur.Name + " Shards";
            shardBar.Set(a.urShards, PetRules.UrExchangeCost, true);
            bool owned = a.OwnsPet(ur.Id), enough = a.urShards >= PetRules.UrExchangeCost;
            W.SetArt(exchangeButton.Root.GetComponent<Image>(), enough && !owned ? "btn-mint" : "btn-grey");
            exchangeButton.Set(true, owned ? "Owned" : "Exchange");
            if (resultCount == 0) bubbleText.text = "Pull the gacha to meet a pet companion! A chance at the rare <color=#B04FC9><b>UR</b></color>!";
        }
    }
}
