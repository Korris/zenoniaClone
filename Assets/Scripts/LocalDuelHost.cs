using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The offline stand-in for DuelService: the same requests, the same checks (safe map, alive, no hit in the last 5 s), the same DuelNpcRun rules and payment, for the one local hero.
    /// Answers go straight to DuelClient. Offline the character file is written after a deck change or a paid result (RpgSession.SaveNow).
    /// </summary>
    public static class LocalDuelHost
    {
        static DuelNpcRun run;
        static PlayerState owner; // the character the run belongs to: another character (offline switch) drops it

        static long NowMs { get { return (long)(Time.unscaledTime * 1000f); } }

        public static void Handle(DuelRequestMsg m)
        {
            PlayerState p = PetActions.Me;
            if (p == null) return;
            if (owner != p) { run = null; owner = p; }
            switch (m.Op)
            {
                case DuelOp.SaveDeck: SaveDeck(p, m); break;
                case DuelOp.PlayNpc: Start(p, m.A); break;
                case DuelOp.Place:
                    if (run == null || run.Finished) { Refuse(DuelFail.NoMatch); break; }
                    DuelPlay r = run.Session.PlayerPlace(m.A, m.B, NowMs, m.Turn, m.Card);
                    if (!r.Ok) Refuse(r.Code == DuelPlayError.NotYourTurn ? DuelFail.NotYourTurn : r.Code == DuelPlayError.Stale ? DuelFail.StaleMove : DuelFail.BadMove);
                    Publish();
                    break;
                case DuelOp.Forfeit:
                    if (run == null || run.Finished) { Refuse(DuelFail.NoMatch); break; }
                    run.Session.Forfeit();
                    Publish();
                    break;
            }
        }

        static void SaveDeck(PlayerState p, DuelRequestMsg m)
        {
            if (run != null && !run.Finished) { Refuse(DuelFail.InDuel); return; }
            var ids = new List<string>();
            foreach (byte i in m.Cards)
            {
                CardDef c = CardTable.ByIndex(i);
                if (c == null) { Refuse(DuelFail.DeckInvalid, (byte)DeckError.Unknown); return; }
                ids.Add(c.Id);
            }
            DeckError err = DeckRules.Save(p, ids);
            if (err != DeckError.None) { Refuse(DuelFail.DeckInvalid, (byte)err); return; }
            Save();
        }

        static void Start(PlayerState p, int npc)
        {
            if (run != null && !run.Finished) { Refuse(DuelFail.InDuel); return; }
            HeroController h = GameSession.Hero;
            if (h == null || h.IsDead) { Refuse(DuelFail.Dead); return; }
            if (MapRegistry.Current.Hostile) { Refuse(DuelFail.NotSafe); return; }
            if (h.MsSinceHurt < 5000f || h.MsSinceAttack < 5000f) { Refuse(DuelFail.Combat); return; }
            if (DuelNpcTable.Get(npc) == null) { Refuse(DuelFail.NoSuchNpc); return; }
            DeckError err;
            ulong seed = (ulong)System.DateTime.UtcNow.Ticks ^ ((ulong)Random.Range(1, int.MaxValue) << 20);
            owner = p;
            run = DuelNpcRun.Start(p, npc, seed, NowMs, out err);
            if (run == null) { Refuse(DuelFail.DeckInvalid, (byte)err); return; }
            Publish();
        }

        /// <summary>Per frame from RpgSession (whether or not the Card window is open): the NPC moves, the turn clock runs, and a hero who dies or walks onto a map with monsters loses, like on the zone.</summary>
        public static void Tick()
        {
            if (run == null || run.Finished) return;
            if (owner != PetActions.Me) { run = null; owner = null; DuelClient.Reset(); return; } // a different character is playing now
            HeroController h = GameSession.Hero;
            if (h == null || h.IsDead || MapRegistry.Current.Hostile) { run.Session.Forfeit(); Publish(); return; }
            if (run.Session.Tick(NowMs)) Publish();
        }

        static void Publish()
        {
            if (run == null) return;
            if (run.Settle(DuelNpcRun.DayKeyNow()) && (run.Reward.Total > 0 || run.View(NowMs).Result == DuelResult.Win)) Save(); // a win moves the daily counter even with no gold
            DuelClient.Apply(run.View(NowMs));
        }

        static void Save() { if (RpgSession.Instance != null) RpgSession.Instance.SaveNow(); }

        static void Refuse(DuelFail why, byte extra = 0) { DuelClient.Notify(DuelClient.FailText(why, extra), false); }
    }
}
