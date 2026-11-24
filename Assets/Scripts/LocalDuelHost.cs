using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The offline stand-in for DuelService.Npc: ONLY the duel with Card Master Orin (tutorial / practice; PvP and Ranked stay online-only). The same checks (safe map, alive, no hit in the last 5 s, Orin in reach,
    /// the quest in the right state) and the same DuelNpcSession as the zone, for the one local hero; answers go straight to DuelClient. When the tutorial is WON the quest moves to hand-in
    /// and the character file is written (RpgSession.SaveNow); a loss, draw or give-up leaves it Active (play again). A hero who dies, walks onto a hostile map or is swapped for another character drops the duel without moving the quest.
    /// </summary>
    public static class LocalDuelHost
    {
        static DuelNpcSession run;
        static PlayerState owner; // the character the duel belongs to: another character (offline switch) drops it

        static long NowMs { get { return (long)(Time.unscaledTime * 1000f); } }

        /// <summary>A duel with Orin is running here (DuelClient routes Place / Forfeit / SkipScript to this host instead of the server).</summary>
        public static bool Running { get { return run != null && !run.Finished; } }

        /// <summary>Requests offline play can answer: starting the Orin duel, or driving the one that is running.</summary>
        public static bool Accepts(DuelOp op) { return op == DuelOp.PlayNpc || (Running && (op == DuelOp.Place || op == DuelOp.Forfeit || op == DuelOp.SkipScript)); }

        public static void Handle(DuelRequestMsg m)
        {
            PlayerState p = PetActions.Me;
            if (p == null) return;
            if (owner != p) { run = null; owner = p; }
            switch (m.Op)
            {
                case DuelOp.PlayNpc: Start(p, m.A); break;
                case DuelOp.Place:
                    if (!Running) { Refuse(DuelFail.NoMatch); break; }
                    DuelPlay r = run.PlayerPlace(m.A, m.B, NowMs, m.Turn, m.Card);
                    if (!r.Ok) Refuse(DuelService.PlayFailure(r)); // the zone's mapping
                    Publish();
                    break;
                case DuelOp.SkipScript:
                    if (!Running || !run.SkipScript(NowMs)) { Refuse(Running ? DuelFail.BadMove : DuelFail.NoMatch); break; }
                    Publish();
                    break;
                case DuelOp.Forfeit:
                    if (!Running) { Refuse(DuelFail.NoMatch); break; }
                    run.Forfeit(); // giving up counts like a loss: the quest stays Active
                    Publish();
                    break;
            }
        }

        static void Start(PlayerState p, int mode)
        {
            if (!DuelService.Enabled) { Refuse(DuelFail.Disabled); return; }
            if (mode > 1) { Refuse(DuelFail.BadMove); return; }
            if (Running) { Refuse(DuelFail.InDuel); return; }
            HeroController h = GameSession.Hero;
            if (h == null || h.IsDead) { Refuse(DuelFail.Dead); return; }
            if (MapRegistry.Current.Hostile) { Refuse(DuelFail.NotSafe); return; }
            if (h.MsSinceHurt < DuelService.CombatBlockMs || h.MsSinceAttack < DuelService.CombatBlockMs) { Refuse(DuelFail.Combat); return; }
            if (!OrinInReach(h)) { Refuse(DuelFail.TooFar); return; }
            QuestState quest = p.Quests.StateOf(CardDuelQuest.First);
            if (mode == 0 ? quest != QuestState.Active : quest != QuestState.Done) { Refuse(DuelFail.NoQuest); return; }
            ulong seed = (ulong)System.DateTime.UtcNow.Ticks ^ ((ulong)Random.Range(1, int.MaxValue) << 20);
            run = mode == 0 ? DuelNpcSession.Tutorial(NowMs, seed) : DuelNpcSession.Practice(seed, DuelNpcSession.PracticeDeck(p.Cards), NowMs);
            owner = p;
            DuelClient.Apply(run.View(NowMs));
        }

        static bool OrinInReach(HeroController h)
        {
            foreach (TownNpc n in TownNpc.Villagers)
                if (n.Def.Id == CardDuelQuest.GiverId && Vector2.Distance(h.Position, n.Feet) <= QuestCatalog.ReachUnits) return true;
            return false;
        }

        /// <summary>Per frame from RpgSession (whether or not the Card window is open): Orin's reply when due, and the end of a duel for a hero who dies, walks onto a map with monsters or idles for 10 minutes (a loss, the quest does NOT move).</summary>
        public static void Tick()
        {
            if (!Running) return;
            if (owner != PetActions.Me) { run = null; owner = null; DuelClient.Reset(); return; } // a different character is playing now
            HeroController h = GameSession.Hero;
            if (h == null || h.IsDead || MapRegistry.Current.Hostile || run.Idle(NowMs)) { run.Forfeit(); DuelClient.Apply(run.View(NowMs)); run = null; return; }
            if (run.Tick(NowMs)) Publish();
        }

        static void Publish()
        {
            if (run == null) return;
            DuelView v = run.View(NowMs);
            // the quest moves BEFORE the final view is shown (as on the zone): the result banner reads the quest state
            if (run.Mode == DuelNpcMode.Tutorial && run.TakeResult() == DuelResult.Win)
            {
                if (RpgSession.Owner != null) RpgSession.Owner.QuestCardDuel(); // QuestChanged -> the quest tracker + hand-in banner
                if (RpgSession.Instance != null) RpgSession.Instance.SaveNow();
            }
            DuelClient.Apply(v);
        }

        static void Refuse(DuelFail why, byte extra = 0) { DuelClient.Notify(DuelClient.FailText(why, extra), false); }
    }
}
