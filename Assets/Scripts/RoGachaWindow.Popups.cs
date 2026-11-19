using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;
using W = ZenoniaProto.InventoryRoWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// RoGachaWindow popups on a veil (a tap on the veil or the X closes them):
    ///   Buy    Buy Gacha Coins with Pet Crystals (PetRules.CrystalsPerPull each; InvOp.GachaBuyCoins), from the button, the coin "+" and a
    ///          pull without the coins (then it says how many are missing and closes itself once they are bought). Crystals come from play only.
    ///   Rules  the guarantees, duplicates, stars and UR shards (PetGachaText.Rules: the server's own numbers), from "?" and the pity "!".
    /// </summary>
    public sealed partial class RoGachaWindow
    {
        enum Popup { None, Buy, Rules }

        Popup popup;
        int popupNeed; // Buy: the coins of the refused pull (0 = opened by hand)
        GameObject veil, buyRoot, rulesRoot;
        Text buyInfo, buyNeed;
        InventoryRoBoxes.IconButton buy1, buy10, buyMissing;
        int buyMissingCoins;

        void BuildPopups()
        {
            RectTransform v = UiKit.Rect(window, "PopupVeil", 6f, 6f, WinW - 12f, WinH - 12f);
            Image vi = UiKit.Panel(v, new Color(0.2f, 0.24f, 0.36f, 0.4f), true);
            vi.sprite = W.Art("bag-section"); vi.type = Image.Type.Sliced;
            W.Clickable(vi, ClosePopup);
            veil = v.gameObject;

            RectTransform b = PopupCard("Buy", 400f, 290f, "Buy Gacha Coins");
            buyRoot = b.gameObject;
            buyInfo = W.Txt(b, "Info", 24f, 70f, 352f, 50f, "", 15, W.Ink, TextAnchor.MiddleCenter, false);
            buyInfo.horizontalOverflow = HorizontalWrapMode.Wrap;
            buyNeed = W.Txt(b, "Need", 24f, 120f, 352f, 26f, "", 15, W.Bad, TextAnchor.MiddleCenter);
            buy1 = new InventoryRoBoxes.IconButton(b, "Buy1", 24f, 156f, 170f, 50f, "btn-sky", "gacha-coin", "", 14, () => Buy(1));
            buy10 = new InventoryRoBoxes.IconButton(b, "Buy10", 206f, 156f, 170f, 50f, "btn-sky", "gacha-coin", "", 14, () => Buy(PetRules.MaxPullsAtOnce));
            buyMissing = new InventoryRoBoxes.IconButton(b, "BuyMissing", 24f, 216f, 352f, 50f, "btn-mint", "gacha-coin", "", 15, () => Buy(buyMissingCoins));

            RectTransform r = PopupCard("Rules", 600f, 360f, "Gacha Rules");
            rulesRoot = r.gameObject;
            Text rules = W.Txt(r, "Text", 28f, 64f, 544f, 280f, RatesLine() + "\n\n" + PetGachaText.Rules(), 13, W.Ink, TextAnchor.UpperLeft, false);
            rules.horizontalOverflow = HorizontalWrapMode.Wrap;
            rules.lineSpacing = 1.1f;
            veil.SetActive(false); buyRoot.SetActive(false); rulesRoot.SetActive(false);
        }

        /// <summary>A white card centred in the window with its title and a close X.</summary>
        RectTransform PopupCard(string name, float w, float h, string title)
        {
            RectTransform c = W.Img(window, name, (WinW - w) * 0.5f, (WinH - h) * 0.5f, w, h, "bag-window", true).rectTransform;
            W.Txt(c, "Title", 24f, 12f, w - 90f, 44f, title, 22, W.Ink);
            W.Clickable(W.Img(c, "Close", w - 54f, 10f, 42f, 42f, "bag-close", true), ClosePopup);
            return c;
        }

        static string RatesLine()
        {
            var b = new System.Text.StringBuilder("<b>Rates per pull</b>  ");
            for (int i = 0; i < PetGacha.Rates.Length; i++) b.Append((PetRarity)i).Append(' ').Append(PetGachaText.Pct(PetGacha.Rates[i])).Append(",  ");
            b.Append("shared shards ").Append(PetGachaText.Pct(PetRules.GachaShardRate)).Append(". Pets of one rarity share its rate evenly.");
            return b.ToString();
        }

        void OpenPopup(Popup p, int need) { popup = p; popupNeed = need; Refresh(); }

        void ClosePopup() { popup = Popup.None; popupNeed = 0; Refresh(); }

        void Buy(int coins) { if (coins > 0) PetActions.Send(InvOp.GachaBuyCoins, (byte)coins); }

        void RefreshPopups(AccountState a)
        {
            if (popup == Popup.Buy && popupNeed > 0 && a.tickets >= popupNeed) popup = Popup.None; // bought: the pull can be asked again
            veil.SetActive(popup != Popup.None);
            buyRoot.SetActive(popup == Popup.Buy);
            rulesRoot.SetActive(popup == Popup.Rules);
            veil.transform.SetAsLastSibling();
            if (popup == Popup.Buy) buyRoot.transform.SetAsLastSibling();
            if (popup == Popup.Rules) rulesRoot.transform.SetAsLastSibling();
            if (popup != Popup.Buy) return;
            int price = PetRules.CrystalsPerPull;
            buyInfo.text = "1 Gacha Coin = " + price + " Pet Crystals. You have " + Amount(a.crystals) + " crystals and " + Amount(a.tickets) + " coins.\n<size=12>" + PetGachaText.CrystalSource + "</size>";
            int missing = Mathf.Max(0, popupNeed - a.tickets);
            buyNeed.text = missing > 0 ? "Pull x" + popupNeed + " needs " + missing + " more coin" + (missing == 1 ? "" : "s") + "." : "";
            buy1.Set(true, "Buy 1  (" + price + ")", a.crystals < price);
            buy10.Set(true, "Buy " + PetRules.MaxPullsAtOnce + "  (" + Amount(price * PetRules.MaxPullsAtOnce) + ")", a.crystals < price * PetRules.MaxPullsAtOnce);
            buyMissingCoins = missing;
            float h = missing > 1 ? 290f : 232f;
            var rt = (RectTransform)buyRoot.transform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -(WinH - h) * 0.5f);
            buyMissing.Set(missing > 1, "Buy the missing " + missing + "  (" + Amount(missing * price) + ")", a.crystals < missing * price);
        }
    }
}
