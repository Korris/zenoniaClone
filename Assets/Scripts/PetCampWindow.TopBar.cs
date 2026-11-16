using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PetCampWindow: the top bar (title sign, the account's wallet: gold, Gacha Coins, Pet Crystals, Pet Food, Pet Balls, Rare Balls; Inventory, Close)
    /// and the plank menu on the left (Team, Collection, Growth, Capture and the purple Gacha plank that opens the gacha screen).
    /// Nothing is bought on this screen (balls are sold by Dale and Nara), so the mockup's "+" buttons are left out.
    /// </summary>
    public sealed partial class PetCampWindow
    {
        const float MenuX = 10f, MenuY = 108f, MenuW = 152f, MenuH = 46f, MenuStep = 54f;
        static readonly string[] WalletIcons = { PaperPiece.IconGold, PaperPiece.IconGachaCoin, PaperPiece.IconPetCrystal, PaperPiece.IconPetFood, PaperPiece.IconCaptureBall, PaperPiece.IconCaptureBallRare };
        static readonly string[] TabNames = { "Team", "Collection", "Growth", "Capture" };

        readonly PaperChip[] wallet = new PaperChip[WalletIcons.Length];
        readonly PaperTab[] menu = new PaperTab[TabNames.Length];

        void BuildTopBar()
        {
            PaperKit.TitleBoard(content, 2f, -3f, 244f, 75f, "PET CAMP");
            for (int i = 0; i < wallet.Length; i++)
                wallet[i] = new PaperChip(content, "Wallet" + i, 262f + i * 116f, 12f, 106f, 26f, PaperArt.Get(WalletIcons[i]), "");
            new PaperButton(content, "Inventory", 962f, 5f, 120f, 40f, "Inventory", PaperTone.Blue, () => { Close(); UiScreens.Open(UiScreen.Bag); });
            PaperKit.CloseButton(content, 1090f, 4f, 42f, Close);

            for (int i = 0; i < menu.Length; i++)
            {
                Tab t = (Tab)i;
                menu[i] = new PaperTab(content, "Menu" + TabNames[i], MenuX, MenuY + i * MenuStep, MenuW, MenuH, TabNames[i], PaperTabStyle.Plank, () => SetTab(t));
            }
            RectTransform gacha = UiKit.Rect(content, "MenuGacha", MenuX, MenuY + menu.Length * MenuStep + 6f, MenuW, MenuH + 4f);
            Image face = PaperArt.Draw(gacha, PaperPiece.PetPlankGacha, PaperTheme.Purple, PaperTheme.BtnPurpleEdge);
            PaperTheme.Caption(gacha, "Gacha", PaperTheme.SectionSize, PaperTheme.TextLight);
            UiWinKit.MakeButton(face, () => { Close(); UiScreens.Open(UiScreen.Gacha); }); // like the old window's Gacha button
        }

        void RefreshTopBar(PlayerState p, AccountState a)
        {
            wallet[0].SetText(Amount(p.Gold));
            wallet[1].SetText(Amount(a != null ? a.tickets : 0));
            wallet[2].SetText(Amount(a != null ? a.crystals : 0));
            wallet[3].SetText(Amount(a != null ? a.food : 0));
            wallet[4].SetText(Amount(a != null ? a.balls : 0));
            wallet[5].SetText(Amount(a != null ? a.rareBalls : 0));
            for (int i = 0; i < menu.Length; i++) menu[i].SetActive(i == (int)tab);
        }

        /// <summary>"123,456", then "12.3M" so a chip never overflows.</summary>
        static string Amount(int n)
        {
            if (n < 1000000) return n.ToString("N0", CultureInfo.InvariantCulture);
            return (n / 1000000f).ToString("0.#", CultureInfo.InvariantCulture) + "M";
        }
    }
}
