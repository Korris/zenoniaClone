using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// "Gem Removal" (mockup 05): items with sockets on the left, the chosen item with its gems in the middle (tap one to pick it), the removal cost under it and the red
    /// "Trade Locked" note, and on the right the picked gem's info and option plus the Remove Gem button. The gem returns to the collection LOCKED (never tradable again).
    /// The server checks gold and room for the gem before it takes anything (InvOp.GemRemove: ItemUid = item uid, Slot = GemSockets.Code of the gem).
    /// </summary>
    public sealed class GemRemovePage : GemPage
    {
        const float ListW = 292f, MidX = 300f, MidW = 372f, RightX = 680f, RightW = 271f;

        GemGearList gear;
        GemItemStage stage;
        GemInfoPanel info;
        U.PieceButton removeButton;
        Text costText, reason;
        Image lockIcon;
        int socket;

        public override void Build(RectTransform parent)
        {
            Root = UiKit.Rect(parent, "RemovePage", 0f, 0f, PageW, PageH);
            U.Panel(Root, "ListBox", 0f, 0f, ListW, PageH, true);
            gear = new GemGearList(Root, 10f, 12f, ListW - 20f, item => { socket = 0; Refresh(); });
            stage = new GemItemStage(Root, MidX, 0f, MidW, i => { socket = i; Refresh(); });
            U.Panel(Root, "CostBox", MidX, 308f, MidW, 96f, true);
            U.Heading(Root, "CostHeading", MidX + 16f, 314f, 200f, "Removal Cost");
            GemUiArt.Img(Root, "GoldIcon", MidX + 40f, 354f, 34f, 34f, GemUiArt.Icon("icon-gold"));
            costText = U.Txt(Root, "Cost", MidX + 82f, 350f, MidW - 100f, 42f, "", 24, U.Ink);
            Image note = GemUiArt.Img(Root, "LockedNote", MidX, 414f, MidW, 90f, GemUiArt.Frame("panel-detail"));
            lockIcon = GemUiArt.Img(Root, "LockIcon", MidX + 20f, 436f, 40f, 44f, GemUiArt.Icon("icon-lock"));
            U.Txt(Root, "LockedText", MidX + 72f, 422f, MidW - 88f, 74f, "The removed gem stays <color=#B33A3A><b>Trade Locked</b></color>: it cannot be traded, listed or sent.", U.TextNormal, U.Ink, TextAnchor.MiddleLeft);
            info = new GemInfoPanel(Root, RightX, 0f, RightW, "Selected Gem");
            reason = U.Txt(Root, "Reason", RightX, PageH - 88f, RightW, 32f, "", U.TextSmall, U.Bad, TextAnchor.MiddleCenter);
            removeButton = new U.PieceButton(Root, "RemoveButton", RightX + (RightW - 220f) * 0.5f, PageH - 52f, 220f, U.ButtonH, GemUiArt.Frame("btn-gold"), "Remove Gem", OnRemove);
        }

        public override void DebugPressMain() { OnRemove(); }

        void OnRemove()
        {
            Item item = gear.Selected;
            string id; GemKind kind;
            if (item == null || item.sockets == null || socket >= item.sockets.Count || !GemSockets.TryParse(item.sockets[socket], out id, out kind)) return;
            string uid = item.uid; byte code = GemSockets.Code(id, kind);
            CardDef card = CardTable.Get(id);
            if (card != null && card.Rarity >= CardRarity.SR) // an expensive step (3,000 G and more): ask first, like Sell does
            {
                Confirm("Remove " + card.Name + " " + kind + " Gem from " + item.name + "?\nIt costs " + CardFees.Remove(card).ToString("N0") + " G. The gem returns to you, Trade Locked for good.", "Remove", () => Send(InvOp.GemRemove, code, uid));
                return;
            }
            Send(InvOp.GemRemove, code, uid); // the gem itself, not its socket index: a repeated press finds no gem
        }

        public override void Refresh()
        {
            PlayerState p = Me;
            if (Root == null || p == null) return;
            gear.Refresh(p);
            Item item = gear.Selected;
            int filled = GemUiModel.Filled(item);
            if (socket >= filled) socket = 0;
            stage.Select(socket);
            stage.Draw(item);
            string id = null; GemKind kind = GemKind.Power;
            bool have = item != null && filled > 0 && GemSockets.TryParse(item.sockets[socket], out id, out kind);
            CardDef card = have ? CardTable.Get(id) : null;
            info.Set(card, kind, true, have ? "Trade Locked: this gem can never be traded." : null);
            int fee = CardFees.Remove(card);
            costText.text = have ? fee.ToString("N0") + " G / per gem" : "-";
            string why = !CardOps.GemsEnabled ? "Soul Gems are not available" : item == null ? "Pick an item" : !have ? "This item holds no gem" : p.Gold < fee ? "Need " + fee.ToString("N0") + " G" : !p.Cards.CanAddGem(id, kind, true) ? "No room to keep this gem" : null;
            reason.text = why ?? "";
            removeButton.Set("Remove Gem", why == null && !Pending);
        }
    }
}
