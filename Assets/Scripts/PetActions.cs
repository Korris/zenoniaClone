using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// What the Pet / gacha windows and the shop ask for. Online the client only sends the InvOp (the server checks and answers with the new state); offline and on a host
    /// the local PlayerRpg runs the same rules (account changes through its AccountLink), then the windows redraw and a toast says what happened.
    /// </summary>
    public static class PetActions
    {
        /// <summary>The state the windows read: the local hero's (online: the server's mirror, with its account).</summary>
        public static PlayerState Me { get { HeroController h = GameSession.Hero; return h != null && h.Rpg != null ? h.Rpg : RpgSession.Player; } }

        public static AccountState Account { get { PlayerState p = Me; return p != null ? p.Account : null; } }

        public static void Send(InvOp op, byte slot, string uid = "")
        {
            if (NetMode.IsClient) { NetClient.SendInvAction(op, uid ?? "", slot); return; }
            PlayerRpg owner = RpgSession.Owner;
            if (owner == null) return;
            if (op == InvOp.PetCapture) { ushort id; ushort.TryParse(uid, out id); PetCaptureService.Request(GameSession.Hero, Zone.Main, id, slot == 1, Answered); return; }
            if (InvOpInfo.IsAccountOp(op)) { owner.ApplyAccountAction(op, slot, uid ?? "", Answered); return; }
            Answered(owner.ApplyInvAction(op, uid ?? "", slot));
        }

        static void Answered(ActionResult r)
        {
            if (!string.IsNullOrEmpty(r.Text)) RpgHud.Toast(r.Text, r.Ok ? new Color(0.75f, 1f, 0.6f) : new Color(1f, 0.7f, 0.5f));
            RpgSession.SyncGear();
            HeroController hero = GameSession.Hero;
            if (hero != null) NetEventBus.Appearance(hero); // host: the other players see the pet change (offline: nothing listens)
            InventoryPanel.RefreshIfOpen();
        }
    }
}
