using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// LumiCounterWindow: the three papers over the stall (pet-gacha-result-states mockup, states 3 and 4), one at a time behind a dark veil.
    ///   NoCoins   a pull was asked without the coins: what is missing, and Buy for all of it (grey + "Need N more crystals" when the
    ///             crystals do not reach) or for as many coins as the crystals pay; it closes by itself once the coins are there.
    ///   Exchange  120 UR shards for Frostfang King: Exchange, or grey "Need N more" / "Already owned"; closes once the pet is owned.
    ///   Rules     the rules text of the old Rates tab (guarantees, duplicates, stars, UR shards, Legacy pets), from the "?" buttons and the Rates flag.
    /// Buying and exchanging send what the old window sends (PetActions.Send); a tap on the veil or Cancel closes the paper.
    /// </summary>
    public sealed partial class LumiCounterWindow
    {
        enum Dialog { None, NoCoins, Exchange, Rules }

        Dialog dialog;
        int dialogNeed; // NoCoins: the coins the refused pull costs
        GameObject veil, noCoins, exchange, rules;
        Text needText, haveText, shortText, spendText, getText;
        PaperButton buyAll, buySome, exchangeButton;
        PaperBar exchangeBar;
        int buyAllCoins, buySomeCoins;

        void OpenDialog(Dialog d, int need) { dialog = d; dialogNeed = need; }
        void CloseDialog() { dialog = Dialog.None; }

        void BuildDialogs()
        {
            RectTransform v = UiKit.Rect(content, "Veil", -600f, -300f, PaperLayout.DesignWidth + 1200f, PaperLayout.DesignHeight + 600f);
            UiWinKit.MakeButton(UiKit.Panel(v, new Color(0.05f, 0.03f, 0.08f, 0.6f), true), CloseDialog).transition = Selectable.Transition.None;
            veil = v.gameObject;

            RectTransform n = Paper("NoCoins", 369f, 150f, 400f, 300f);
            noCoins = n.gameObject;
            Icon(n, PaperPiece.IconGachaCoin, 30f, 22f, 44f);
            PaperTheme.Title(n, "Title", 84f, 22f, 290f, 44f, "Not enough Gacha Coins", 19, PaperTheme.Ink);
            needText = PaperTheme.Title(n, "Need", 30f, 70f, 340f, 26f, "", PaperTheme.SectionSize + 1, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter);
            Icon(n, PaperPiece.IconPetCrystal, 38f, 102f, 24f);
            haveText = PaperTheme.Body(n, "Have", 68f, 100f, 310f, 28f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            buyAll = new PaperButton(n, "BuyAll", 30f, 138f, 226f, 40f, "", PaperTone.Blue, () => Buy(buyAllCoins));
            shortText = PaperTheme.Body(n, "Short", 262f, 138f, 112f, 40f, "", PaperTheme.BarSize + 1, PaperTheme.Red, PaperInk.Plain, TextAnchor.MiddleCenter, true);
            buySome = new PaperButton(n, "BuySome", 30f, 188f, 226f, 40f, "", PaperTone.Blue, () => Buy(buySomeCoins));
            new PaperGachaButton(n, "Cancel", 266f, 188f, 104f, 40f, PaperPiece.GachaBtnSlate, PaperTheme.Slate, PaperTheme.PlateEdge, PaperTheme.TextLight, "Cancel", CloseDialog);
            PaperKit.Mark(n, PaperMark.AlertGold, 42f, 254f, 20f);
            PaperTheme.Body(n, "Source", 58f, 240f, 320f, 28f, PetGachaText.CrystalSource, PaperTheme.BarSize + 1, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.MiddleLeft, true);

            PetDef ur = Featured;
            RectTransform e = Paper("Exchange", 399f, 150f, 340f, 312f);
            exchange = e.gameObject;
            PetIcon(e, 34f, 28f, 92f, ur);
            PaperTheme.Title(e, "Name", 134f, 30f, 186f, 30f, ur.Name, 18, PaperTheme.Ink);
            ShowRarity(UiKit.Icon(e, "Rarity", 150f, 58f, 62f), ur);
            spendText = PaperTheme.Body(e, "Spend", 20f, 124f, 300f, 24f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
            exchangeBar = new PaperBar(e, "Bar", 34f, 150f, 272f, 28f, PaperTheme.BarExp);
            getText = PaperTheme.Body(PaperKit.Frame(e, "Get", 34f, 186f, 272f, 36f, PaperFrame.Card, false), "Text", 0f, 0f, 272f, 36f, "You will get:  <color=#B3261E>" + ur.Name + "</color>", PaperTheme.BodySize + 1, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
            exchangeButton = new PaperButton(e, "Exchange", 34f, 238f, 156f, 44f, "Exchange", PaperTone.Purple, () => PetActions.Send(InvOp.GachaExchange, 0));
            new PaperGachaButton(e, "Cancel", 200f, 238f, 106f, 44f, PaperPiece.GachaBtnSlate, PaperTheme.Slate, PaperTheme.PlateEdge, PaperTheme.TextLight, "Cancel", CloseDialog);

            RectTransform r = Paper("Rules", 219f, 130f, 700f, 340f);
            rules = r.gameObject;
            PaperKit.Heading(r, "Title", 26f, 22f, 400f, "Rates and rules");
            Text body = PaperTheme.Body(r, "Text", 32f, 54f, 636f, 260f, PetGachaText.Headline + "\nShard result: " + PetGachaText.SharedShardNote + ".\n\n" + PetGachaText.Rules() + "\n\n" + PetGachaText.CrystalSource,
                PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.UpperLeft);
            body.lineSpacing = 1.05f;
            PaperKit.CloseButton(r, 700f - 44f, 8f, 34f, CloseDialog);
        }

        RectTransform Paper(string name, float x, float y, float w, float h)
        {
            RectTransform rt = UiKit.Rect(content, name, x, y, w, h);
            PaperArt.Draw(rt, PaperPiece.GachaPanel, PaperTheme.Paper, PaperTheme.Wood, 10f, true);
            return rt;
        }

        static void Icon(Transform parent, string piece, float x, float y, float size)
        {
            Image img = UiKit.Icon(parent, "Icon", x, y, size);
            img.sprite = PaperArt.Get(piece);
            img.enabled = img.sprite != null;
        }

        void RefreshDialogs(AccountState a)
        {
            PetDef ur = Featured;
            if (dialog == Dialog.NoCoins && a.tickets >= dialogNeed) dialog = Dialog.None;   // bought: the pull can be asked again
            if (dialog == Dialog.Exchange && exchange.activeSelf && a.OwnsPet(ur.Id) && exchangeButton.Click.interactable) dialog = Dialog.None; // exchanged while the paper was up
            veil.SetActive(dialog != Dialog.None);
            noCoins.SetActive(dialog == Dialog.NoCoins);
            exchange.SetActive(dialog == Dialog.Exchange);
            rules.SetActive(dialog == Dialog.Rules);
            if (dialog == Dialog.NoCoins)
            {
                int price = PetRules.CrystalsPerPull, missing = dialogNeed - a.tickets, afford = Mathf.Min(a.crystals / price, missing);
                needText.text = "Need <color=#B3261E>" + dialogNeed + "</color>  -  have <color=#B3261E>" + a.tickets + "</color>";
                haveText.text = price + " Pet Crystals = 1 coin  (you have " + Amount(a.crystals) + ")";
                buyAllCoins = missing;
                buyAll.Label.text = "Buy " + Coins(missing) + "  -  " + Amount(missing * price) + " crystals";
                buyAll.SetEnabled(afford >= missing);
                shortText.text = afford >= missing ? "" : "Need " + Amount(missing * price - a.crystals) + "\nmore crystals";
                buySomeCoins = afford;
                buySome.Root.gameObject.SetActive(afford > 0 && afford < missing);
                buySome.Label.text = "Buy " + Coins(afford) + "  -  " + Amount(afford * price);
            }
            else if (dialog == Dialog.Exchange)
            {
                int cost = PetRules.UrExchangeCost;
                bool owned = a.OwnsPet(ur.Id), enough = a.urShards >= cost;
                spendText.text = "Spend " + cost + " UR shards  (you have " + Amount(a.urShards) + ")";
                exchangeBar.Set((float)a.urShards / cost);
                exchangeBar.SetText(a.urShards + " / " + cost);
                getText.text = owned ? ur.Name + " is already yours" : "You will get:  <color=#B3261E>" + ur.Name + "</color>";
                exchangeButton.Label.text = owned ? "Already owned" : enough ? "Exchange" : "Need " + (cost - a.urShards) + " more";
                exchangeButton.SetEnabled(enough && !owned);
            }
        }

        static string Coins(int n) { return n + (n == 1 ? " coin" : " coins"); }
    }
}
