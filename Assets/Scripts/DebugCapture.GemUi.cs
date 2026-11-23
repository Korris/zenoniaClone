using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the Card window (CardWindow, mockups docs/reference/card-gem-ui/01..05):
    ///   --gem-seed          100,000 gold, 12 cards of every rarity, a few gems, two bag items and a gem already socketed in the weapon (offline test data)
    ///   --open-gems my|album|convert|socket|remove   opens the Card window on that page (--card-pick <cardId> chooses a card in My Cards / Album)
    ///   --gem-press convert|socket|remove   presses that page's main button once (the real InvOp path)
    /// </summary>
    public partial class DebugCapture
    {
        // --card-press claim|sell|lock (Card window ops through the real InvOp path), --card-confirm x (shows the "Sure?" box)
        static void DebugGemWindow()
        {
            string[] args = Environment.GetCommandLineArgs();
            PlayerState p = RpgSession.Player;
            if (p != null && Array.IndexOf(args, "--gem-seed") >= 0) SeedGems(p);
            string open = Arg(args, "--open-gems");
            if (open == null) return;
            CardWindow.Page page = open == "socket" ? CardWindow.Page.Socket : open == "remove" ? CardWindow.Page.Remove : open == "convert" ? CardWindow.Page.Convert : open == "album" ? CardWindow.Page.Album : CardWindow.Page.MyCards;
            string pick = Arg(args, "--card-pick");
            CardWindow.Open(page);
            if (pick != null) CardWindow.Show(page, pick);
            Canvas.ForceUpdateCanvases();
            string press = Arg(args, "--gem-press") ?? Arg(args, "--card-press");
            if (press == "convert") PetActions.Send(Net.InvOp.GemConvert, 0, CardTable.All[0].Id);
            else if (press == "socket") PetActions.Send(Net.InvOp.GemSocket, GemSockets.Code(CardTable.All[1].Id, GemKind.Power), p.Inventory.Count > 0 ? p.Inventory[0].uid : "");
            else if (press == "claim") PetActions.Send(Net.InvOp.AlbumClaim, 0);
            else if (press == "sell") PetActions.Send(Net.InvOp.CardSell, 1, "meadowSlime");
            else if (press == "lock") PetActions.Send(Net.InvOp.CardLock, 1, "meadowSlime");
            else if (Arg(args, "--card-confirm") != null) CardConfirmBox.Ask("Sell 1 x Meadow Slime [N] to the shop for 50 G?\nLocked and in-deck copies stay.", "Sell", () => { });
            else if (press == "remove" && p != null) PetActions.Send(Net.InvOp.GemRemove, GemSockets.Code("thornBoar", GemKind.Guard), p.GetEquipped(Slots.Weapon).uid);
        }

        static void SeedGems(PlayerState p)
        {
            p.Gold = 100000;
            string[] ids = { "meadowSlime", "hornRabbit", "meadowMantis", "thornBoar", "forestWolf", "brambleTreant", "frostSlime", "iceGoblin", "crystalGolem", "duneScarab", "sandRaider", "frostfangKing" };
            int n = 12;
            foreach (string id in ids) { p.Cards.Add(id, n); n = Mathf.Max(1, n - 2); }
            p.Cards.SetLocked("hornRabbit", 1);
            foreach (string id in ids) { p.Cards.AddGem(id, GemKind.Power, false, 3); p.Cards.AddGem(id, GemKind.Guard, id == "thornBoar", 2); }
            for (int s = 0; s < 3; s++)
            {
                string slot = s == 0 ? Slots.Armor : s == 1 ? Slots.Helmet : Slots.Weapon;
                Item it = ItemFactory.Generate(RpgRandom.Default, 1, slot, "rare", p.ClassId);
                it.uid = "gem-seed-" + s;
                p.AddToInventory(it);
            }
            Item weapon = p.GetEquipped(Slots.Weapon);
            if (weapon != null) { CardOps.Socket(p, weapon.uid, "thornBoar", GemKind.Guard); }
            p.RecomputeStats();
        }
    }
}
