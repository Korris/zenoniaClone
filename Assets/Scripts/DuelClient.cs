using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// What the Card Duel UI talks to, online or offline. Requests go to the zone server (NetClient.SendDuel) or to the local host (LocalDuelHost, same DuelNpcRun rules); answers come back as a DuelView
    /// (Apply) or a refusal (Notify). The window only draws View and sends ids / cells; it never decides a flip, a score or a result.
    /// </summary>
    public static class DuelClient
    {
        public static DuelView View { get; private set; }
        /// <summary>Goes up with every new view: the battle screen redraws only then.</summary>
        public static int Version { get; private set; }
        static float deadlineAt;

        public static bool Running { get { return View != null && !View.Finished; } }
        public static float SecondsLeft { get { return View == null || View.Finished ? 0f : Mathf.Max(0f, deadlineAt - Time.unscaledTime); } }

        /// <summary>A placement must be sent with at least this long left on the turn clock (the server's clock is ahead by about the ping): the battle screen greys the board below it.</summary>
        public const float MinPlaceSeconds = 0.5f;

        public static void Apply(DuelView v)
        {
            View = v; deadlineAt = Time.unscaledTime + v.MsLeft / 1000f; awayAt = Time.unscaledTime - v.OppAwayMs / 1000f; Version++;
            if (!v.Finished && !CardWindow.IsOpen) CardWindow.Open(CardWindow.Page.Duel); // a challenge was accepted from the world: the board opens by itself
        }

        /// <summary>The player closed the result (or the connection dropped): back to the deck page.</summary>
        public static void Reset() { View = null; Version++; ClearLobby(); }

        // ---- challenge lobby (PvP) ----

        /// <summary>The player's invite / wager panel as the server last sent it; Phase 0 = none.</summary>
        public static DuelLobbyMsg Lobby { get; private set; }
        public static int LobbyVersion { get; private set; }
        static float lobbyDeadlineAt;
        static float awayAt;

        public static bool InLobby { get { return Lobby.Phase != 0; } }
        public static float LobbySecondsLeft { get { return Mathf.Max(0f, lobbyDeadlineAt - Time.unscaledTime); } }
        /// <summary>Seconds the opponent has been disconnected (0 = connected); counts up between server views.</summary>
        public static float OppAwaySeconds { get { return View != null && View.OppAwayMs > 0 && !View.Finished ? Mathf.Max(0f, Time.unscaledTime - awayAt) : 0f; } }

        public static void ApplyLobby(DuelLobbyMsg m) { Lobby = m; lobbyDeadlineAt = Time.unscaledTime + m.MsLeft / 1000f; LobbyVersion++; CardDuelLobbyWindow.Sync(); }

        static void ClearLobby() { if (Lobby.Phase == 0) return; Lobby = default(DuelLobbyMsg); LobbyVersion++; CardDuelLobbyWindow.Sync(); }

        public static void Challenge(ushort player, bool wager) { Send(new DuelRequestMsg { Op = DuelOp.Challenge, Other = player, A = (byte)(wager ? 1 : 0) }); }
        public static void CancelInvite() { Send(new DuelRequestMsg { Op = DuelOp.CancelInvite }); }
        public static void Accept() { Send(new DuelRequestMsg { Op = DuelOp.Accept }); }
        public static void Decline() { Send(new DuelRequestMsg { Op = DuelOp.Decline }); }
        public static void WagerPick(int cardIndex) { Send(new DuelRequestMsg { Op = DuelOp.WagerPick, A = (byte)cardIndex }); }
        public static void WagerLock() { Send(new DuelRequestMsg { Op = DuelOp.WagerLock }); }
        public static void WagerConfirm() { Send(new DuelRequestMsg { Op = DuelOp.WagerConfirm }); }
        public static void LeaveLobby() { Send(new DuelRequestMsg { Op = DuelOp.LeaveLobby }); }

        public static void SaveDeck(IList<string> ids)
        {
            var cards = new byte[ids.Count];
            for (int i = 0; i < cards.Length; i++) cards[i] = (byte)CardTable.Index(ids[i]);
            Send(new DuelRequestMsg { Op = DuelOp.SaveDeck, Cards = cards });
        }

        public static void PlayNpc(int npc) { Send(new DuelRequestMsg { Op = DuelOp.PlayNpc, A = (byte)npc }); }
        /// <summary>Sends a placement stamped with what this client is looking at (the turn and the card in that hand slot): the server refuses it when either has moved on. False = not sent (no duel, or too little time left).</summary>
        public static bool Place(int handIndex, int cell)
        {
            DuelView v = View;
            if (v == null || v.Finished || handIndex < 0 || handIndex >= v.Hand.Count || SecondsLeft < MinPlaceSeconds) return false;
            Send(new DuelRequestMsg { Op = DuelOp.Place, A = (byte)handIndex, B = (byte)cell, Turn = (byte)v.Turn, Card = (byte)v.Hand[handIndex] });
            return true;
        }
        public static void Forfeit() { Send(new DuelRequestMsg { Op = DuelOp.Forfeit }); }

        static void Send(DuelRequestMsg m)
        {
            if (NetMode.IsClient) NetClient.SendDuel(m); else LocalDuelHost.Handle(m);
        }

        /// <summary>Per frame while the window is open: the offline host runs the NPC and the turn clock (online the server does).</summary>
        public static void Tick() { if (!NetMode.IsClient) LocalDuelHost.Tick(); }

        public static void OnNotice(DuelNoticeMsg m)
        {
            if (m.Kind == DuelNoticeKind.Refused) Notify(FailText((DuelFail)m.Detail, m.Extra), false);
        }

        public static void Notify(string text, bool good)
        {
            if (!string.IsNullOrEmpty(text)) RpgHud.Toast(text, good ? new Color(0.75f, 1f, 0.6f) : new Color(1f, 0.7f, 0.5f));
        }

        public static string FailText(DuelFail f, byte extra)
        {
            switch (f)
            {
                case DuelFail.Busy: return "You are busy";
                case DuelFail.NotSafe: return "Card duels need a safe map (a town)";
                case DuelFail.Combat: return "Not while in combat: wait 5 seconds";
                case DuelFail.Dead: return "You cannot duel while down";
                case DuelFail.Trading: return "Finish your trade first";
                case DuelFail.DeckInvalid: return DeckRules.Text((DeckError)extra);
                case DuelFail.NoSuchNpc: return "No such opponent";
                case DuelFail.NoMatch: return "No duel is running";
                case DuelFail.NotYourTurn: return "Not your turn";
                case DuelFail.BadMove: return "That move is not allowed";
                case DuelFail.Disabled: return "Card Duel is switched off";
                case DuelFail.InDuel: return "Finish the duel first";
                case DuelFail.NoTarget: return "That player is not here";
                case DuelFail.TargetBusy: return "That player is busy";
                case DuelFail.InviteCooldown: return "Wait 30 seconds before challenging them again";
                case DuelFail.NoInvite: return "That challenge is gone";
                case DuelFail.NoLobby: return "No challenge is open";
                case DuelFail.NoPick: return "Pick a card first";
                case DuelFail.NotLocked: return "Both players must lock a card first";
                case DuelFail.CardMismatch: return "Both cards must have the same rarity";
                case DuelFail.Declined: return "Challenge declined";
                case DuelFail.Cancelled: return "Challenge cancelled";
                case DuelFail.Expired: return "The challenge ran out of time";
                case DuelFail.OpponentLeft: return "The other player left";
                case DuelFail.WagerOff: return "Card Wager is switched off";
                case DuelFail.WagerLevel: return "Card Wager needs Lv 30";
                case DuelFail.WagerCard: return "That card cannot be wagered";
                case DuelFail.WagerDaily: return "Wager limit reached for today";
                case DuelFail.ApiBusy: return "Cards are busy, try again in a moment";
                case DuelFail.WagerRefused: return "The wager was refused, nothing moved";
                case DuelFail.StaleMove: return "The board changed, tap again";
                case DuelFail.SelfInvite: return "You cannot challenge yourself";
                case DuelFail.TargetNotReady: return "That player cannot duel right now";
                default: return "";
            }
        }
    }
}
