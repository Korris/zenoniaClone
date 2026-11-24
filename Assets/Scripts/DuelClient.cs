using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// What the Card Duel UI talks to. Card Duel is ONLINE ONLY (player vs player on the zone server): requests go out through NetClient.SendDuel, answers come back as a DuelView (Apply) or a refusal (Notify).
    /// Offline nothing is sent and the Card Duel page shows why it is disabled (OfflineReason). The window only draws View and sends ids / cells; it never decides a flip, a score, a result or which card is taken.
    /// </summary>
    public static class DuelClient
    {
        public static DuelView View { get; private set; }
        /// <summary>Goes up with every new view: the battle screen redraws only then.</summary>
        public static int Version { get; private set; }
        static float deadlineAt, pickDeadlineAt;

        public const string OfflineReason = "Card Duel needs an online game: connect to a server to challenge other players.";

        /// <summary>True while connected to a zone server: the only place a duel can happen.</summary>
        public static bool Online { get { return NetMode.IsClient || DebugPretendOnline; } }
        /// <summary>Captures only (DebugCapture --duel-online / --duel-pvp): draw the online UI with no connection. Nothing is ever sent.</summary>
        public static bool DebugPretendOnline;

        public static bool Running { get { return View != null && !View.Finished; } }
        public static float SecondsLeft { get { return View == null || View.Finished ? 0f : Mathf.Max(0f, deadlineAt - Time.unscaledTime); } }
        /// <summary>Seconds the winner has left to pick a card (0 when not picking).</summary>
        public static float PickSecondsLeft { get { return View == null || View.WagerState != DuelView.WagerPicking ? 0f : Mathf.Max(0f, pickDeadlineAt - Time.unscaledTime); } }

        /// <summary>A placement must be sent with at least this long left on the turn clock (the server's clock is ahead by about the ping): the battle screen greys the board below it.</summary>
        public const float MinPlaceSeconds = 0.5f;

        static bool npcResultCounted; // the final view of a duel with Orin counts once for his Guidance streak (a fresh running view re-arms it)

        public static void Apply(DuelView v)
        {
            View = v; deadlineAt = Time.unscaledTime + v.MsLeft / 1000f; pickDeadlineAt = Time.unscaledTime + v.PickMsLeft / 1000f; awayAt = Time.unscaledTime - v.OppAwayMs / 1000f; Version++;
            if (v.Npc) { if (!v.Finished) npcResultCounted = false; else if (!npcResultCounted) { npcResultCounted = true; OrinGuidance.Record(PetActions.Me != null ? PetActions.Me.Cards : null, v.Result); } }
            if ((!v.Finished || v.MustPick) && !CardWindow.IsOpen) CardWindow.Open(CardWindow.Page.Duel); // a challenge was accepted from the world (or the winner must pick): the board opens by itself
        }

        /// <summary>The player closed the result (or the connection dropped): back to the deck page. A match that is still being picked / settled on the server comes back with the next view.</summary>
        public static void Reset() { View = null; Version++; ClearLobby(); }

        // ---- challenge lobby ----

        /// <summary>The player's invite as the server last sent it; Phase 0 = none.</summary>
        public static DuelLobbyMsg Lobby { get; private set; }
        public static int LobbyVersion { get; private set; }
        static float lobbyDeadlineAt;
        static float awayAt;

        public static bool InLobby { get { return Lobby.Phase != 0; } }
        public static float LobbySecondsLeft { get { return Mathf.Max(0f, lobbyDeadlineAt - Time.unscaledTime); } }
        /// <summary>Seconds the opponent has been disconnected (0 = connected); counts up between server views.</summary>
        public static float OppAwaySeconds { get { return View != null && View.OppAwayMs > 0 && !View.Finished ? Mathf.Max(0f, Time.unscaledTime - awayAt) : 0f; } }

        // ---- Ranked ----

        /// <summary>Where the player stands in the Ranked line and the season clock, as the server last said it.</summary>
        public static DuelRankedMsg Ranked { get; private set; }
        public static int RankedVersion { get; private set; }
        static float rankedAt;

        public static bool RankedQueued { get { return Ranked.Queued; } }
        /// <summary>Seconds in the line (counts on between server messages).</summary>
        public static float RankedWaitSeconds { get { return Ranked.Queued ? Ranked.WaitSec + (Time.unscaledTime - rankedAt) : 0f; } }
        /// <summary>Season number and the time left of it; 0 until the server has said.</summary>
        public static int RankedSeason { get { return Ranked.Season; } }
        public static int RankedMinutesLeft { get { return Mathf.Max(0, Ranked.MinutesLeft - Mathf.FloorToInt((Time.unscaledTime - rankedAt) / 60f)); } }

        public static void ApplyRanked(DuelRankedMsg m) { Ranked = m; rankedAt = Time.unscaledTime; RankedVersion++; }

        public static void QueueRanked() { Send(new DuelRequestMsg { Op = DuelOp.QueueRanked }); }
        public static void LeaveQueue() { Send(new DuelRequestMsg { Op = DuelOp.LeaveQueue }); }

        /// <summary>The player's rank as the screen shows it: the saved one, brought to the server's season (a character from an older season sees the halved LP it will queue with). Never null.</summary>
        public static CardRankData DisplayRank(PlayerState p)
        {
            CardRankData r = RankedRules.Copy(p.Cards.Rank);
            if (r.season > 0 && Ranked.Season > r.season) RankedRules.EnsureSeason(r, RankedRules.SeasonEndMs(Ranked.Season) - 1L);
            return r;
        }

        /// <summary>Why this character cannot queue for Ranked (empty = it can; the server still checks): offline, level. The deck is checked by the page that has the deck.</summary>
        public static string RankedProblem(PlayerState p)
        {
            if (!Online) return OfflineReason;
            return p == null ? "" : RankedRules.Text(RankedRules.CanQueue(p));
        }

        public static void ApplyLobby(DuelLobbyMsg m) { Lobby = m; lobbyDeadlineAt = Time.unscaledTime + m.MsLeft / 1000f; LobbyVersion++; CardDuelLobbyWindow.Sync(); }

        static void ClearLobby() { if (Lobby.Phase == 0) return; Lobby = default(DuelLobbyMsg); LobbyVersion++; CardDuelLobbyWindow.Sync(); }

        public static void Challenge(ushort player) { Send(new DuelRequestMsg { Op = DuelOp.Challenge, Other = player }); }
        public static void CancelInvite() { Send(new DuelRequestMsg { Op = DuelOp.CancelInvite }); }
        public static void Accept() { Send(new DuelRequestMsg { Op = DuelOp.Accept }); }
        public static void Decline() { Send(new DuelRequestMsg { Op = DuelOp.Decline }); }
        public static void LeaveLobby() { Send(new DuelRequestMsg { Op = DuelOp.LeaveLobby }); }
        /// <summary>The winner takes this card (a CardTable index) from the loser's deck. The server checks it is in that deck.</summary>
        public static void TakeCard(int cardIndex) { Send(new DuelRequestMsg { Op = DuelOp.TakeCard, A = (byte)cardIndex }); }

        public static void SaveDeck(IList<string> ids)
        {
            var cards = new byte[ids.Count];
            for (int i = 0; i < cards.Length; i++) cards[i] = (byte)CardTable.Index(ids[i]);
            Send(new DuelRequestMsg { Op = DuelOp.SaveDeck, Cards = cards });
        }

        /// <summary>Sends a placement stamped with what this client is looking at (the turn and the card in that hand slot): the server refuses it when either has moved on. False = not sent (no duel, or too little time left).</summary>
        public static bool Place(int handIndex, int cell)
        {
            DuelView v = View;
            if (v == null || v.Finished || handIndex < 0 || handIndex >= v.Hand.Count || (!v.Npc && SecondsLeft < MinPlaceSeconds)) return false; // a duel with Orin has no clock
            Send(new DuelRequestMsg { Op = DuelOp.Place, A = (byte)handIndex, B = (byte)cell, Turn = (byte)v.Turn, Card = (byte)v.Hand[handIndex] });
            return true;
        }
        public static void Forfeit() { Send(new DuelRequestMsg { Op = DuelOp.Forfeit }); }
        /// <summary>Asks Card Master Orin for a duel: tutorial = the scripted first duel (quest first-card-duel is active), else a practice duel (quest done). Works offline too (LocalDuelHost).</summary>
        public static void PlayNpc(bool tutorial) { Send(new DuelRequestMsg { Op = DuelOp.PlayNpc, A = (byte)(tutorial ? 0 : 1) }); }
        /// <summary>Drops the rest of the tutorial script (free play from the next turn).</summary>
        public static void SkipScript() { Send(new DuelRequestMsg { Op = DuelOp.SkipScript }); }

        static void Send(DuelRequestMsg m)
        {
            if (NetMode.IsClient) NetClient.SendDuel(m); else if (LocalDuelHost.Accepts(m.Op)) LocalDuelHost.Handle(m); else Notify(OfflineReason, false); // offline only the duel with Orin works
        }

        /// <summary>Why this character cannot start a duel right now (empty = it can; the server still checks): offline, level, today's limit. The deck is checked by the page that has the deck.</summary>
        public static string EntryProblem()
        {
            if (!Online) return OfflineReason;
            PlayerState p = PetActions.Me;
            if (p == null) return "";
            WagerError e = WagerRules.CanEnter(p, WagerRules.DayKeyNow());
            return e == WagerError.None ? "" : WagerRules.Text(e);
        }

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
                case DuelFail.Declined: return "Challenge declined";
                case DuelFail.Cancelled: return "Challenge cancelled";
                case DuelFail.Expired: return "The challenge ran out of time";
                case DuelFail.OpponentLeft: return "The other player left";
                case DuelFail.WagerLevel: return "Card Duel needs Lv 30";
                case DuelFail.WagerDaily: return "Card duel limit reached for today (10 a day, 2 per player)";
                case DuelFail.ApiBusy: return "Cards are busy, try again in a moment";
                case DuelFail.WagerRefused: return "The duel was refused, no card moved";
                case DuelFail.StaleMove: return "The board changed, tap again";
                case DuelFail.SelfInvite: return "You cannot challenge yourself";
                case DuelFail.TargetNotReady: return "That player cannot duel right now";
                case DuelFail.NoPick: return "That card cannot be taken";
                case DuelFail.AccountAge: return "Card duels with cards at stake need an account at least 7 days old";
                case DuelFail.SameAccount: return "You cannot duel your own other character";
                case DuelFail.RankedLevel: return RankedRules.Text(RankedError.Level);
                case DuelFail.NoStakes: return "Card Wager is switched off: this duel has no cards at stake";
                case DuelFail.NoQuest: return "Orin has no duel for you right now";
                case DuelFail.TooFar: return "Too far from Card Master Orin";
                case DuelFail.ScriptMove: return "Follow the lesson: place the highlighted card on the highlighted cell";
                default: return "";
            }
        }
    }
}
