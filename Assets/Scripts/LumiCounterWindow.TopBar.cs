using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// LumiCounterWindow: the top bar. Title sign, Gacha Coins (its "+" buys one coin, like Buy 1 Coin), Pet Crystals (earned in play only:
    /// no "+"), what a coin costs, Pet Camp (opens the Pet screen) and Close.
    /// </summary>
    public sealed partial class LumiCounterWindow
    {
        PaperChip coinChip, crystalChip;

        void BuildTopBar()
        {
            PaperKit.TitleBoard(content, 2f, -3f, 262f, 81f, "PET GACHA", "Summon");
            coinChip = new PaperChip(content, "Coins", 372f, 10f, 130f, 28f, PaperArt.Get(PaperPiece.IconGachaCoin), "", () => Buy(1));
            crystalChip = new PaperChip(content, "Crystals", 514f, 10f, 126f, 28f, PaperArt.Get(PaperPiece.IconPetCrystal), "");
            RectTransform rate = UiKit.Rect(content, "CoinPrice", 652f, 10f, 190f, 28f);
            PaperArt.Draw(rate, PaperPiece.Chip, PaperTheme.Plate, PaperTheme.PlateEdge, 14f);
            PaperTheme.Body(rate, "Text", 0f, 0f, 190f, 28f, PetRules.CrystalsPerPull + " Crystals = 1 Coin", PaperTheme.BodySize, PaperTheme.TextLight, PaperInk.World, TextAnchor.MiddleCenter, true);
            new PaperButton(content, "PetCamp", 950f, 5f, 130f, 40f, "Pet Camp", PaperTone.Blue, () => { Close(); UiScreens.Open(UiScreen.Pet); });
            PaperKit.CloseButton(content, 1090f, 4f, 42f, Close);
        }

        void RefreshTopBar(AccountState a)
        {
            coinChip.SetText(Amount(a.tickets));
            crystalChip.SetText(Amount(a.crystals));
        }
    }
}
