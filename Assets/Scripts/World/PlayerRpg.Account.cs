using System;
using System.Collections.Generic;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PlayerRpg: the account side (pets, gacha, growth, balls; docs/pet-system-design.md). Every change goes through AccountLink.Run, so the account is stored before the hero
    /// sees it; the hero's gold part (price, star cost, refunds, compensation) is applied in the same callback. Authority only: an online client sends InvOps instead.
    /// </summary>
    public sealed partial class PlayerRpg
    {
        public AccountLink Account { get; private set; }
        /// <summary>An account change worth telling the player (text, good news). Offline a toast, on the server a NetEventBus toast.</summary>
        public event Action<string, bool> AccountNews;
        /// <summary>The account changed (stored): the server pushes the new state, offline the windows redraw.</summary>
        public event Action AccountChanged;
        /// <summary>The last pulls, for the gacha window's result panel (authority side; the client reads the history instead).</summary>
        public readonly List<PullResult> LastPulls = new List<PullResult>();

        static long NowMs { get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); } }

        /// <summary>Binds the account and runs what a join owes: old pets migrated, starter coins, level unlocks / milestones.</summary>
        public void AttachAccount(AccountLink link)
        {
            Account = link;
            State.AttachAccount(link.Live);
            AccountSync.Plan plan = default(AccountSync.Plan);
            link.Run("join", a => { plan = AccountSync.OnJoin(a, State, NowMs); return plan.Changed ? ActionResult.Success(plan.Text) : ActionResult.Fail(""); }, r =>
            {
                if (!r.Ok) return;
                if (plan.RemoveLegacy) State.RemoveLegacyPets();
                if (plan.WearLegacy != null && State.PetId == "" && State.Level >= PetRules.SlotUnlockLevel[0]) State.PetId = plan.WearLegacy;
                Stored(plan.GoldDelta, r);
            });
        }

        void AccountLevelReached()
        {
            if (Account == null) return;
            AccountSync.Plan plan = default(AccountSync.Plan);
            Account.Run("level", a => { plan = AccountSync.OnLevel(a, State.Level); return plan.Changed ? ActionResult.Success(plan.Text) : ActionResult.Fail(""); }, r => { if (r.Ok) Stored(0, r); });
        }

        /// <summary>This hero's Wanted Boss claim was paid: the account's first of the day adds Pet Crystals, food, a step toward a rare ball.</summary>
        public void AccountWantedClaimed() { if (Account != null) Account.Run("wanted", a => AccountRewards.OnWantedClaim(a, NowMs), r => { if (r.Ok) Stored(0, r); }); }

        /// <summary>This hero had a valid share of a map boss kill (KillRewards): food, the 5th of the week Crystals, and a ball when `ball` was rolled.</summary>
        public void AccountMapBossKilled(bool ball)
        {
            if (Account == null) return;
            Account.Run("boss", a => { ActionResult r = AccountRewards.OnMapBossKill(a, NowMs); if (ball) { AccountRewards.AddBall(a); r = ActionResult.Success(r.Text + ", +1 Pet Ball"); } return r; }, r => { if (r.Ok) Stored(0, r); });
        }

        /// <summary>A normal monster dropped a bag of Pet Food (the zone rolled the 8 %).</summary>
        public void AccountFoodDrop() { if (Account != null) Account.Run("food", a => AccountRewards.AddFood(a, 1), r => { if (r.Ok) Stored(0, r); }); }

        /// <summary>Pet buy / growth / gacha / balls. `done` gets the result after the account is stored (or refused). Seller and reach are the caller's check.</summary>
        public void ApplyAccountAction(InvOp op, byte slot, string text, Action<ActionResult> done)
        {
            if (Account == null) { done(ActionResult.Fail("Not available")); return; }
            int gold = 0;
            PetDef def = PetConfig.ByIndex(slot);
            string id = def != null ? def.Id : "";
            int gold0 = State.Gold, heroTop = State.Level, n;
            Func<AccountState, ActionResult> compute;
            switch (op)
            {
                case InvOp.PetBuy: compute = a => Note(AccountPets.Buy(a, def, gold0), ref gold); break;
                case InvOp.PetStarUp: compute = a => Note(PetGrowth.StarUp(a, id, gold0), ref gold); break;
                case InvOp.PetLevelUp: compute = a => Note(PetGrowth.LevelUp(a, id, heroTop), ref gold); break;
                case InvOp.PetResetLevel: compute = a => Note(PetGrowth.ResetLevel(a, id), ref gold); break;
                case InvOp.PetResetStars: compute = a => Note(PetGrowth.ResetStars(a, id), ref gold); break;
                case InvOp.PetConvertShards: int.TryParse(text, out n); compute = a => Note(PetGrowth.Convert(a, id, n), ref gold); break;
                case InvOp.GachaBuyCoins: compute = a => PetGacha.BuyCoins(a, slot); break;
                case InvOp.GachaPull: compute = a => { LastPulls.Clear(); return PetGacha.Pull(a, slot, NetMode.IsServerSide ? (Rand)RpgRandom.Secure : RpgRandom.Default, NowMs, LastPulls); }; break;
                case InvOp.GachaExchange: compute = PetGacha.ExchangeUr; break;
                case InvOp.BallBuy: compute = a => Note(PetCapture.BuyBalls(a, slot, gold0, NowMs), ref gold); break;
                default: done(ActionResult.Fail("Unknown action")); return;
            }
            Account.Run(op.ToString(), compute, r => { if (r.Ok) Stored(gold, r, false); done(r); });
        }

        /// <summary>A ball thrown (PetCaptureService checked the target): spends the ball and, on a catch, adds the pet; the outcome comes back once stored.</summary>
        public void ApplyCapture(CaptureTarget target, bool rare, Action<CaptureOutcome> done)
        {
            CaptureOutcome outcome = CaptureOutcome.Refused;
            double roll = NetMode.IsServerSide ? RpgRandom.Secure() : RpgRandom.Default();
            Account.Run("capture", a => { outcome = PetCapture.Throw(a, target, rare, roll, NowMs); return outcome == CaptureOutcome.Refused ? ActionResult.Fail("") : ActionResult.Success(""); },
                r => { if (r.Ok) Stored(0, r, false); else outcome = CaptureOutcome.Refused; done(outcome); });
        }

        static ActionResult Note(AccountPets.Outcome o, ref int gold) { gold = o.GoldDelta; return o.Result; }

        /// <summary>The account is stored: the hero's gold follows, stats follow the stars, listeners hear about it.</summary>
        void Stored(int goldDelta, ActionResult r, bool news = true)
        {
            if (goldDelta != 0) State.Gold = Math.Max(0, State.Gold + goldDelta);
            State.AttachAccount(Account.Live); // a pet lost elsewhere, new stars: recompute
            if (news && !string.IsNullOrEmpty(r.Text) && AccountNews != null) AccountNews(r.Text, true);
            if (AccountChanged != null) AccountChanged();
            Touch();
        }
    }
}
