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

        public static void Apply(DuelView v) { View = v; deadlineAt = Time.unscaledTime + v.MsLeft / 1000f; Version++; }

        /// <summary>The player closed the result (or the connection dropped): back to the deck page.</summary>
        public static void Reset() { View = null; Version++; }

        public static void SaveDeck(IList<string> ids)
        {
            var cards = new byte[ids.Count];
            for (int i = 0; i < cards.Length; i++) cards[i] = (byte)CardTable.Index(ids[i]);
            Send(new DuelRequestMsg { Op = DuelOp.SaveDeck, Cards = cards });
        }

        public static void PlayNpc(int npc) { Send(new DuelRequestMsg { Op = DuelOp.PlayNpc, A = (byte)npc }); }
        public static void Place(int handIndex, int cell) { Send(new DuelRequestMsg { Op = DuelOp.Place, A = (byte)handIndex, B = (byte)cell }); }
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
                default: return "";
            }
        }
    }
}
