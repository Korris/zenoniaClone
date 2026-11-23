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

        static long NowMs { get { return (long)(Time.unscaledTime * 1000f); } }

        public static void Handle(DuelRequestMsg m)
        {
            PlayerState p = PetActions.Me;
            if (p == null) return;
            switch (m.Op)
            {
                case DuelOp.SaveDeck: SaveDeck(p, m); break;
                case DuelOp.PlayNpc: Start(p, m.A); break;
                case DuelOp.Place:
                    if (run == null || run.Finished) { Refuse(DuelFail.NoMatch); break; }
                    DuelPlay r = run.Session.PlayerPlace(m.A, m.B, NowMs);
                    if (!r.Ok) Refuse(r.Error == "Not your turn" ? DuelFail.NotYourTurn : DuelFail.BadMove);
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
            if (h.MsSinceHurt < 5000f) { Refuse(DuelFail.Combat); return; }
            if (DuelNpcTable.Get(npc) == null) { Refuse(DuelFail.NoSuchNpc); return; }
            DeckError err;
            ulong seed = (ulong)System.DateTime.UtcNow.Ticks ^ ((ulong)Random.Range(1, int.MaxValue) << 20);
            run = DuelNpcRun.Start(p, npc, seed, NowMs, out err);
            if (run == null) { Refuse(DuelFail.DeckInvalid, (byte)err); return; }
            Publish();
        }

        public static void Tick()
        {
            if (run == null || run.Finished) return;
            HeroController h = GameSession.Hero;
            if (h == null || h.IsDead) run.Session.Forfeit();
            if (h == null || h.IsDead || run.Session.Tick(NowMs)) Publish();
        }

        static void Publish()
        {
            if (run == null) return;
            if (run.Settle(DuelNpcRun.DayKeyNow()) && run.Reward.Total > 0) Save();
            DuelClient.Apply(run.View(NowMs));
        }

        static void Save() { if (RpgSession.Instance != null) RpgSession.Instance.SaveNow(); }

        static void Refuse(DuelFail why, byte extra = 0) { DuelClient.Notify(DuelClient.FailText(why, extra), false); }
    }
}
