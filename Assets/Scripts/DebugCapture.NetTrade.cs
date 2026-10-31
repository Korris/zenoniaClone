using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --net-bot trade --trade-role ari|bea|cyd|eve|ui-a|ui-b (Tools/net-trade-test.ps1): the end-to-end trade script. Needs a server with --dev-gold and --dev-items. Ari and Bea do the honest
    /// flow, by the same requests the buttons send; Cyd is the third player and Eve's partner; Eve is a modified client that sends what no button sends, then acts on an item she offered.
    /// Every expectation goes into the log as a "trade-e2e: PASS" or "FAIL" line, and the state read back is the mirror, which only the server writes. ui-a and ui-b: DebugCapture.NetTradeUi.cs.
    /// </summary>
    public partial class DebugCapture
    {
        const int DevGoldGiven = 5000, DevItemsGiven = 10;

        static TradeClientState Tr { get { return NetClient.Trade; } }
        static PlayerState Me { get { return GameSession.Hero != null ? GameSession.Hero.Rpg : null; } }

        static void T(string line) { Debug.Log("[ZenoniaProto] trade-e2e: " + line); }

        static void Check(string what, bool ok) { T((ok ? "PASS " : "FAIL ") + what); }

        IEnumerator TUntil(Func<bool> done, float timeout, string what)
        {
            float start = Time.realtimeSinceStartup;
            while (!done() && Time.realtimeSinceStartup - start < timeout) yield return null;
            T((done() ? "PASS " : "FAIL TIMEOUT ") + what + " after " + (Time.realtimeSinceStartup - start).ToString("F1") + " s");
        }

        void StartTradeBot()
        {
            string role = NetLaunchArgs.Arg(Environment.GetCommandLineArgs(), "--trade-role");
            switch (role)
            {
                case "ari": StartCoroutine(TradeAri()); break;
                case "bea": StartCoroutine(TradeBea()); break;
                case "cyd": StartCoroutine(TradeCyd()); break;
                case "eve": StartCoroutine(TradeEve()); break;
                case "ui-a": partyShots = NetMode.RendersFx; StartCoroutine(TradeUiA()); break;
                case "ui-b": partyShots = NetMode.RendersFx; StartCoroutine(TradeUiB()); break;
            }
        }

        static void Send(TradeOp op, string uid = null, int gold = 0, ushort target = 0)
        {
            uint rev = Tr.Session.HasValue ? Tr.Session.Value.Revision : 0u;
            NetClient.SendTrade(new TradeRequestMsg { Op = op, Uid = uid, Gold = gold, Target = target, Revision = rev });
        }

        static bool Has(string uid) { return Me.Inventory.Exists(i => i.uid == uid); }
        static string Bag() { return string.Join(",", Me.Inventory.Select(i => i.uid + (i.enhance > 0 ? "+" + i.enhance : "")).ToArray()); }

        static string OfferLine()
        {
            if (!Tr.Session.HasValue) return "no trade";
            TradeStateMsg s = Tr.Session.Value;
            return "rev " + s.Revision + " mine[" + s.Mine.Items.Length + " items " + s.Mine.Gold + " G lock " + s.Mine.Locked + " conf " + s.Mine.Confirmed + "] theirs[" + s.Theirs.Items.Length + " items " + s.Theirs.Gold + " G lock " + s.Theirs.Locked + " conf " + s.Theirs.Confirmed + "]";
        }

        IEnumerator TradeAri()
        {
            yield return TUntil(() => HeroNamed("Bea") != null && Me != null && Me.Inventory.Count == DevItemsGiven, 40f, "see Bea, bag has the dev items");
            ushort bea = HeroNamed("Bea").PlayerId;
            int gold0 = Me.Gold;
            T("start: gold " + gold0 + " bag " + Bag());

            // no trade with yourself, nor with nobody
            Send(TradeOp.Request, target: GameSession.Hero.PlayerId);
            Send(TradeOp.Request, target: 65000);
            yield return new WaitForSeconds(1f);
            Check("a request to yourself or nobody opened nothing", !Tr.Outgoing.HasValue);

            // the requester cancels
            Send(TradeOp.Request, target: bea);
            yield return TUntil(() => Tr.Outgoing.HasValue, 5f, "request 1 pending");
            yield return new WaitForSeconds(2f);
            Send(TradeOp.CancelRequest);
            yield return TUntil(() => !Tr.Outgoing.HasValue, 5f, "request 1 cancelled");
            yield return new WaitForSeconds(2f);

            // declined
            Send(TradeOp.Request, target: bea);
            yield return TUntil(() => Tr.Outgoing.HasValue, 5f, "request 2 pending");
            yield return TUntil(() => !Tr.Outgoing.HasValue, 8f, "request 2 declined");
            yield return new WaitForSeconds(1f);

            // left to run out
            Send(TradeOp.Request, target: bea);
            yield return TUntil(() => Tr.Outgoing.HasValue, 5f, "request 3 pending");
            float asked = Time.realtimeSinceStartup;
            Send(TradeOp.Request, target: bea); // refused: already out
            yield return TUntil(() => !Tr.Outgoing.HasValue, 34f, "request 3 ran out");
            Check("request 3 lasted about 30 s", Time.realtimeSinceStartup - asked > 28f);
            yield return new WaitForSeconds(1f);

            // accepted: the full flow
            Send(TradeOp.Request, target: bea);
            yield return TUntil(() => Tr.Session.HasValue, 8f, "request 4 accepted, window open");
            yield return new WaitForSeconds(6f); // Cyd tries to cut in here

            string sword = Me.Inventory[0].uid, armor = Me.Inventory[1].uid;
            int sent = Me.Inventory[0].enhance;
            Send(TradeOp.AddItem, sword);
            Send(TradeOp.AddItem, armor);
            Send(TradeOp.SetGold, gold: 1000);
            yield return TUntil(() => Tr.Session.HasValue && Tr.Session.Value.Mine.Items.Length == 2 && Tr.Session.Value.Mine.Gold == 1000, 5f, "my offer is in");
            yield return TUntil(() => Tr.Session.HasValue && Tr.Session.Value.Theirs.Items.Length == 1 && Tr.Session.Value.Theirs.Gold == 100, 8f, "Bea's offer shows here, " + OfferLine());
            Check("offered items are still in the bag before execution", Has(sword) && Has(armor) && Me.Gold == gold0);
            string beaItem = Tr.Session.Value.Theirs.Items[0].Uid;
            int beaEnhance = Tr.Session.Value.Theirs.Items[0].Enhance;
            Check("Bea's +9 weapon is described with its level", beaEnhance == 9 && !string.IsNullOrEmpty(Tr.Session.Value.Theirs.Items[0].Name));

            Send(TradeOp.Confirm); // refused: nobody locked
            yield return new WaitForSeconds(0.5f);
            Check("confirm before locking changed nothing", !Tr.Session.Value.Mine.Confirmed);
            Send(TradeOp.Lock);
            yield return TUntil(() => Tr.Session.Value.Mine.Locked && Tr.Session.Value.Theirs.Locked, 8f, "both locked");
            uint before = Tr.Session.Value.Revision;
            int warnings = Tr.WarningCount;
            Send(TradeOp.SetGold, gold: 900); // changes the offer: both locks go
            yield return TUntil(() => !Tr.Session.Value.Mine.Locked && !Tr.Session.Value.Theirs.Locked && Tr.Session.Value.Revision != before, 5f, "my change cleared both locks");
            Check("the change raised the warning", Tr.WarningCount > warnings);
            yield return new WaitForSeconds(1f);
            NetClient.SendTrade(new TradeRequestMsg { Op = TradeOp.Lock, Revision = before }); // the old offers
            yield return new WaitForSeconds(0.5f);
            Check("a lock naming the old revision was refused", !Tr.Session.Value.Mine.Locked);
            Send(TradeOp.Lock);
            yield return TUntil(() => Tr.Session.Value.Mine.Locked && Tr.Session.Value.Theirs.Locked, 8f, "both locked again");
            Send(TradeOp.Confirm);
            yield return TUntil(() => Tr.Session.HasValue && Tr.Session.Value.Mine.Confirmed, 5f, "I confirmed");
            yield return TUntil(() => !Tr.Session.HasValue, 8f, "trade done, window closed");
            yield return new WaitForSeconds(0.5f);

            Check("gold after trade: -900 +100", Me.Gold == gold0 - 900 + 100);
            Check("my two items left my bag", !Has(sword) && !Has(armor));
            Check("Bea's weapon arrived with +9", Me.Inventory.Exists(i => i.uid == beaItem && i.enhance == 9));
            Check("bag count -2 +1", Me.Inventory.Count == DevItemsGiven - 1);
            T("after trade: gold " + Me.Gold + " bag " + Bag() + " (sent +" + sent + ")");

            // too far: this headless client walks off with the window open
            yield return new WaitForSeconds(1f);
            Send(TradeOp.Request, target: bea);
            yield return TUntil(() => Tr.Session.HasValue, 8f, "second trade open");
            yield return new WaitForSeconds(1f);
            Send(TradeOp.AddItem, Me.Inventory[0].uid);
            yield return new WaitForSeconds(1f);
            Vector2 from = GameSession.Hero.Position;
            float t0 = Time.time;
            InputAdapter.BotHeld = k => { int d = (int)((Time.time - t0) / 2.5f) % 4; return (d == 0 && k == GameKey.Right) || (d == 1 && k == GameKey.Up) || (d == 2 && k == GameKey.Right) || (d == 3 && k == GameKey.Down); };
            yield return TUntil(() => !Tr.Session.HasValue, 25f, "walking off ended the trade");
            InputAdapter.BotHeld = null;
            T("walked " + Vector2.Distance(from, GameSession.Hero.Position).ToString("F0") + " units from where the trade began");
            Check("the walk changed nothing", Me.Inventory.Count == DevItemsGiven - 1 && Me.Gold == gold0 - 900 + 100);

            // the partner goes offline during a trade
            t0 = Time.time;
            Vector2 back = from;
            InputAdapter.BotHeld = k =>
            {
                Vector2 d = back - GameSession.Hero.Position;
                return (d.x < -6f && k == GameKey.Left) || (d.x > 6f && k == GameKey.Right) || (d.y < -6f && k == GameKey.Down) || (d.y > 6f && k == GameKey.Up);
            };
            yield return TUntil(() => Vector2.Distance(GameSession.Hero.Position, HeroNamed("Bea").Position) < 90f, 25f, "back within reach of Bea");
            InputAdapter.BotHeld = null;
            yield return new WaitForSeconds(0.5f);
            Send(TradeOp.Request, target: bea);
            yield return TUntil(() => Tr.Session.HasValue, 8f, "third trade open");
            Send(TradeOp.AddItem, Me.Inventory[0].uid);
            yield return TUntil(() => !Tr.Session.HasValue, 20f, "Bea went offline, trade over");
            Check("her going offline changed nothing", Me.Inventory.Count == DevItemsGiven - 1 && Me.Gold == gold0 - 900 + 100);
            T("ari done");
            Application.Quit();
        }

        IEnumerator TradeBea()
        {
            yield return TUntil(() => Me != null && Me.Inventory.Count == DevItemsGiven, 40f, "bag has the dev items");
            int gold0 = Me.Gold;
            yield return TUntil(() => Tr.Incoming.HasValue, 60f, "request 1 shows");
            yield return TUntil(() => !Tr.Incoming.HasValue, 8f, "Ari's cancel closed it");
            yield return TUntil(() => Tr.Incoming.HasValue, 15f, "request 2 shows");
            yield return new WaitForSeconds(1f);
            Send(TradeOp.Decline, target: Tr.Incoming.Value.Other);
            yield return TUntil(() => !Tr.Incoming.HasValue, 5f, "declined");
            yield return TUntil(() => Tr.Incoming.HasValue, 15f, "request 3 shows, not answering");
            yield return TUntil(() => !Tr.Incoming.HasValue, 34f, "request 3 ran out");
            yield return TUntil(() => Tr.Incoming.HasValue, 15f, "request 4 shows");
            yield return new WaitForSeconds(0.5f);
            Send(TradeOp.Accept, target: Tr.Incoming.Value.Other);
            yield return TUntil(() => Tr.Session.HasValue, 5f, "window open");
            yield return new WaitForSeconds(7f);

            string mine = Me.Inventory[0].uid;
            Send(TradeOp.AddItem, mine);
            Send(TradeOp.SetGold, gold: 100);
            // the server's answer to her own offer too: a Lock naming a revision older than that is rightly refused
            yield return TUntil(() => Tr.Session.Value.Theirs.Items.Length == 2 && Tr.Session.Value.Theirs.Gold == 1000 && Tr.Session.Value.Mine.Items.Length == 1 && Tr.Session.Value.Mine.Gold == 100, 8f, "both offers show here");
            string a0 = Tr.Session.Value.Theirs.Items[0].Uid, a1 = Tr.Session.Value.Theirs.Items[1].Uid;
            Send(TradeOp.Lock);
            yield return TUntil(() => Tr.Session.Value.Mine.Locked, 5f, "I locked");
            int warnings = Tr.WarningCount;
            yield return TUntil(() => Tr.Session.Value.Theirs.Gold == 900, 15f, "Ari lowered the gold");
            Check("my lock went with it, and the warning came", !Tr.Session.Value.Mine.Locked && Tr.WarningCount > warnings);
            yield return new WaitForSeconds(0.5f);
            Send(TradeOp.Lock);
            yield return TUntil(() => Tr.Session.Value.Theirs.Confirmed, 10f, "Ari confirmed");
            Send(TradeOp.Confirm);
            yield return TUntil(() => !Tr.Session.HasValue, 8f, "trade done");
            yield return new WaitForSeconds(0.5f);
            Check("gold after trade: -100 +900", Me.Gold == gold0 - 100 + 900);
            Check("Ari's items arrived, the +9 still +9", Has(a1) && Me.Inventory.Exists(i => i.uid == a0 && i.enhance == 9));
            Check("my item left", !Has(mine));
            T("after trade: gold " + Me.Gold + " bag " + Bag());

            yield return TUntil(() => Tr.Incoming.HasValue, 15f, "second request shows");
            Send(TradeOp.Accept, target: Tr.Incoming.Value.Other);
            yield return TUntil(() => !Tr.Session.HasValue && !Tr.Incoming.HasValue, 40f, "Ari walked off, trade over");
            yield return TUntil(() => Tr.Incoming.HasValue, 40f, "third request shows");
            Send(TradeOp.Accept, target: Tr.Incoming.Value.Other);
            yield return TUntil(() => Tr.Session.HasValue, 5f, "third window open");
            yield return new WaitForSeconds(2f);
            T("bea goes offline with the window open");
            Application.Quit();
        }

        /// <summary>Tries to cut into Ari's trade, then is the partner of Eve's: accepts, offers one item, locks when told by her lock, and confirms when she has.</summary>
        IEnumerator TradeCyd()
        {
            yield return TUntil(() => Me != null && HeroNamed("Ari") != null && Me.Inventory.Count == DevItemsGiven, 40f, "bag has the dev items");
            int gold0 = Me.Gold, count0 = Me.Inventory.Count;
            ushort ari = HeroNamed("Ari").PlayerId;
            // timed into Ari's flow: at 15 s Bea is sitting on request 3 for its full 30 s, at 39 s the two are in their trade
            yield return new WaitForSeconds(15f);
            Send(TradeOp.Request, target: ari);
            yield return new WaitForSeconds(1f);
            Check("a request to someone with a request open is refused", !Tr.Outgoing.HasValue);
            yield return new WaitForSeconds(23f);
            Send(TradeOp.Request, target: ari);
            yield return new WaitForSeconds(1f);
            Check("a request to someone already trading is refused", !Tr.Outgoing.HasValue);
            if (Tr.Outgoing.HasValue) Send(TradeOp.CancelRequest);
            yield return TUntil(() => Tr.Incoming.HasValue, 150f, "Eve's request shows");
            Send(TradeOp.Accept, target: Tr.Incoming.Value.Other);
            yield return TUntil(() => Tr.Session.HasValue, 5f, "window with Eve open");
            string mine = Me.Inventory[0].uid;
            Send(TradeOp.AddItem, mine);
            yield return TUntil(() => Tr.Session.Value.Theirs.Locked, 60f, "Eve locked");
            Send(TradeOp.Lock);
            yield return TUntil(() => Tr.Session.Value.Theirs.Confirmed, 20f, "Eve confirmed");
            string got = Tr.Session.Value.Theirs.Items[0].Uid;
            int gotGold = Tr.Session.Value.Theirs.Gold;
            Send(TradeOp.Confirm);
            yield return TUntil(() => !Tr.Session.HasValue, 8f, "trade with Eve done");
            yield return new WaitForSeconds(0.5f);
            Check("Cyd holds what Eve really offered, and only that", Has(got) && !Has(mine) && Me.Inventory.Count == count0 && Me.Gold == gold0 + gotGold);
            T("cyd after trade: gold " + Me.Gold + " bag " + Bag());
            yield return new WaitForSeconds(3f);
            T("cyd done");
            Application.Quit();
        }

        /// <summary>A modified client: only the server stands in her way.</summary>
        IEnumerator TradeEve()
        {
            yield return TUntil(() => Me != null && HeroNamed("Cyd") != null && Me.Inventory.Count == DevItemsGiven, 40f, "bag has the dev items");
            ushort cyd = HeroNamed("Cyd").PlayerId;
            int gold0 = Me.Gold;
            string bag0 = Bag();
            yield return new WaitForSeconds(70f); // after Cyd has finished poking at Ari
            // before a trade exists
            Send(TradeOp.Lock); Send(TradeOp.Confirm); Send(TradeOp.AddItem, Me.Inventory[0].uid); Send(TradeOp.SetGold, gold: 5);
            yield return new WaitForSeconds(0.5f);
            Check("trade ops with no trade open did nothing", !Tr.Session.HasValue);

            Send(TradeOp.Request, target: cyd);
            yield return TUntil(() => Tr.Session.HasValue, 10f, "Cyd accepted");
            yield return TUntil(() => Tr.Session.Value.Theirs.Items.Length == 1, 8f, "Cyd's offer shows");
            string theirs = Tr.Session.Value.Theirs.Items[0].Uid;
            yield return new WaitForSeconds(0.5f);
            uint rev0 = Tr.Session.Value.Revision;

            // uids that are not hers to give
            string worn = Me.GetEquipped(Slots.Weapon).uid;
            foreach (string bad in new[] { "no-such-item", "", theirs, worn, "starter-weapon", new string('x', 4000), "' or 1=1" })
                Send(TradeOp.AddItem, bad);
            yield return new WaitForSeconds(1f);
            Check("bad, foreign, worn, starter and overlong uids were all refused", Tr.Session.Value.Mine.Items.Length == 0 && Tr.Session.Value.Revision == rev0);

            // gold she does not have, in every shape
            foreach (int g in new[] { -1, int.MinValue, gold0 + 1, int.MaxValue, int.MaxValue - 1 })
                Send(TradeOp.SetGold, gold: g);
            yield return new WaitForSeconds(1f);
            Check("negative, huge and overflowing gold were all refused", Tr.Session.Value.Mine.Gold == 0 && Tr.Session.Value.Revision == rev0);

            // steps out of order
            Send(TradeOp.Confirm);
            NetClient.SendTrade(new TradeRequestMsg { Op = TradeOp.Confirm, Revision = 999999 });
            Send(TradeOp.Unlock);
            NetClient.SendTrade(new TradeRequestMsg { Op = TradeOp.Lock, Revision = 999999 });
            yield return new WaitForSeconds(1f);
            Check("confirm before lock, a made-up revision and an unlock of nothing did nothing", !Tr.Session.Value.Mine.Locked && !Tr.Session.Value.Mine.Confirmed);
            Send(TradeOp.Request, target: cyd);
            Send(TradeOp.Accept, target: cyd);
            yield return new WaitForSeconds(0.5f);
            Check("a second trade from inside a trade did nothing", Tr.Session.HasValue && Tr.Session.Value.Other == cyd);

            // a real offer, the same uid twice
            string offered = Me.Inventory[1].uid, enhanceable = Me.Inventory[0].uid;
            Send(TradeOp.AddItem, offered);
            Send(TradeOp.AddItem, offered);
            Send(TradeOp.SetGold, gold: 1234);
            yield return TUntil(() => Tr.Session.Value.Mine.Gold == 1234, 5f, "real offer in");
            Check("a uid offered twice counts once", Tr.Session.Value.Mine.Items.Length == 1 && Tr.Session.Value.Mine.Items[0].Uid == offered);

            // now act on what she offered, and on her gold
            int stones0 = Me.UpgradeStone;
            Item item = Me.Inventory.Find(i => i.uid == offered);
            NetClient.SendInvAction(InvOp.Discard, offered, 0);
            NetClient.SendInvAction(InvOp.Equip, offered, 0);
            NetClient.SendInvAction(InvOp.Enhance, enhanceable, 0);
            NetClient.SendInvAction(InvOp.Sort, "", 0);
            NetClient.SendInvAction(InvOp.Unequip, "", 0);
            NetClient.SendInvAction(InvOp.Use, "", 0);
            NetClient.SendInvAction(InvOp.PotionBuy, "apothecaryNara", 0);
            NetClient.SendInvAction(InvOp.SkillRespec, "", 0);
            yield return new WaitForSeconds(1.5f);
            Check("discard, equip, enhance, sort, unequip, use, buy and respec were all refused: bag, gold and order are untouched", Bag() == bag0 && Me.Gold == gold0 && Me.UpgradeStone == stones0 && Me.GetEquipped(Slots.Weapon).uid == worn);
            Check("the offered item is still in the bag and still offered", Has(offered) && Tr.Session.Value.Mine.Items[0].Uid == offered);

            Send(TradeOp.Lock);
            yield return TUntil(() => Tr.Session.Value.Mine.Locked && Tr.Session.Value.Theirs.Locked, 10f, "both locked");
            NetClient.SendInvAction(InvOp.Discard, offered, 0); // again, once locked
            NetClient.SendInvAction(InvOp.Equip, offered, 0);
            yield return new WaitForSeconds(1f);
            Check("locked: discard and equip still refused", Has(offered) && Bag() == bag0);
            NetClient.SendTrade(new TradeRequestMsg { Op = TradeOp.Confirm, Revision = rev0 }); // the offers of a while ago
            yield return new WaitForSeconds(0.5f);
            Check("a confirm naming old offers was refused", !Tr.Session.Value.Mine.Confirmed);

            Send(TradeOp.Confirm);
            yield return TUntil(() => !Tr.Session.HasValue, 15f, "trade done");
            yield return new WaitForSeconds(0.5f);
            Check("the server moved exactly the offer", !Has(offered) && Has(theirs) && Me.Gold == gold0 - 1234 && Me.Inventory.Count == DevItemsGiven);
            T("eve after trade: gold " + Me.Gold + " bag " + Bag() + " (" + item.name + " gone)");

            // nothing she can do now reaches the finished trade
            NetClient.SendInvAction(InvOp.Discard, offered, 0);
            Send(TradeOp.Cancel);
            yield return new WaitForSeconds(1f);
            Check("after the trade the item stays gone and normal requests work again", !Has(offered));
            T("eve done");
            Application.Quit();
        }
    }
}
