using System.Collections;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --trade-role ui-a|ui-b (Tools/net-trade-test.ps1 -Ui): the trade UI through its real input path, with the same FakeInput as the party's: the module raycasts, presses and clicks, nothing
    /// calls a handler directly. Two visible windows; screenshots of every state go to --shot-path.
    /// </summary>
    public partial class DebugCapture
    {
        const string TradeUi = "TradeWindow/Board/";

        IEnumerator Tap(string path, bool touch = false) { yield return Press(CentreOf(path), touch, path); yield return new WaitForSeconds(0.25f); }

        IEnumerator TradeUiA()
        {
            yield return TUntil(() => HeroNamed("Bea") != null && Me != null && Me.Inventory.Count == DevItemsGiven, 40f, "see Bea, bag has the dev items");
            yield return new WaitForSeconds(4f); // Bea walks clear of the potion buttons
            int gold0 = Me.Gold;

            yield return Press(BodyOf("Bea"), false, "Bea's body");
            yield return TUntil(() => PartyPlayerMenu.OpenName == "Bea", 2f, "the box opened on Bea");
            yield return PartyShot("a-menu");
            yield return Tap("PartyPlayerMenu/Box/Trade");
            yield return TUntil(() => Tr.Outgoing.HasValue, 3f, "request pending, from the Trade button");
            yield return new WaitForSeconds(0.5f);
            yield return PartyShot("a-waiting");
            yield return Tap("TradeRequestWindow/Waiting/Cancel");
            yield return TUntil(() => !Tr.Outgoing.HasValue, 3f, "request cancelled, from the button");
            yield return new WaitForSeconds(2f);

            // Bea declines this one by finger
            yield return Press(TagOf("Bea"), true, "Bea's name tag");
            yield return TUntil(() => PartyPlayerMenu.OpenName == "Bea", 2f, "the box opened by touch");
            yield return Tap("PartyPlayerMenu/Box/Trade", true);
            yield return TUntil(() => Tr.Outgoing.HasValue, 3f, "second request pending");
            yield return TUntil(() => !Tr.Outgoing.HasValue, 10f, "declined, from the button");
            yield return new WaitForSeconds(1f);

            yield return Press(BodyOf("Bea"), false, "Bea's body");
            yield return TUntil(() => PartyPlayerMenu.OpenName == "Bea", 2f, "box");
            yield return Tap("PartyPlayerMenu/Box/Trade");
            yield return TUntil(() => Tr.Session.HasValue, 10f, "accepted, window open");
            yield return TUntil(() => TradeWindow.IsOpen, 2f, "the window is up");
            yield return new WaitForSeconds(0.5f);
            yield return PartyShot("a-empty");

            string bag0 = Me.Inventory[0].uid;
            yield return Tap(TradeUi + "Bag/Slot0");
            yield return TUntil(() => Tr.Session.Value.Mine.Items.Length == 1, 3f, "a tap on the bag offered the item");
            Check("the first offered item is the first bag item", Tr.Session.Value.Mine.Items[0].Uid == bag0);
            yield return Tap(TradeUi + "Bag/Slot0");
            yield return Tap(TradeUi + "Bag/Slot0");
            yield return TUntil(() => Tr.Session.Value.Mine.Items.Length == 3, 3f, "two more");
            yield return Tap(TradeUi + "MyOffer/Slot2");
            yield return TUntil(() => Tr.Session.Value.Mine.Items.Length == 2, 3f, "a tap on the offer took the item back");

            yield return Tap(TradeUi + "Bag/Next");
            yield return new WaitForSeconds(0.2f);
            yield return Tap(TradeUi + "Bag/Prev");

            yield return Tap(TradeUi + "MyOffer/GoldField");
            yield return PartyShot("a-pad-empty");
            yield return Tap(TradeUi + "Pad/Box/Key1");
            yield return Tap(TradeUi + "Pad/Box/Key2");
            yield return Tap(TradeUi + "Pad/Box/Key5");
            yield return Tap(TradeUi + "Pad/Box/Key5");
            yield return Tap(TradeUi + "Pad/Box/KeyDel");
            yield return Tap(TradeUi + "Pad/Box/Key0");
            yield return PartyShot("a-pad");
            yield return Tap(TradeUi + "Pad/Box/OK");
            yield return TUntil(() => Tr.Session.Value.Mine.Gold == 1250, 3f, "the keypad set 1,250");
            yield return TUntil(() => Tr.Session.Value.Theirs.Items.Length >= 3, 15f, "Bea's items show");
            yield return new WaitForSeconds(0.5f);
            yield return PartyShot("a-editing");

            // a's lock, then Bea adds one more: the lock goes and the warning shows
            yield return Tap(TradeUi + "Lock");
            yield return TUntil(() => Tr.Session.Value.Mine.Locked, 3f, "locked, from the button");
            yield return new WaitForSeconds(0.3f);
            yield return PartyShot("a-locked-mine");
            int warnings = Tr.WarningCount;
            yield return TUntil(() => Tr.WarningCount > warnings, 12f, "Bea's change raised the warning");
            yield return new WaitForSeconds(0.3f);
            yield return PartyShot("a-offer-changed");
            Check("my lock is gone", !Tr.Session.Value.Mine.Locked);
            yield return new WaitForSeconds(4f);

            yield return Tap(TradeUi + "Lock");
            yield return TUntil(() => Tr.Session.Value.Mine.Locked && Tr.Session.Value.Theirs.Locked, 12f, "both locked");
            yield return new WaitForSeconds(0.3f);
            yield return PartyShot("a-both-locked");
            yield return Tap(TradeUi + "Confirm");
            yield return TUntil(() => Tr.Session.Value.Mine.Confirmed, 3f, "confirmed, from the button");
            yield return new WaitForSeconds(0.3f);
            yield return PartyShot("a-waiting-confirm");
            int theirGold = Tr.Session.Value.Theirs.Gold;
            yield return TUntil(() => !Tr.Session.HasValue, 12f, "trade done");
            yield return new WaitForSeconds(1f);
            yield return PartyShot("a-after");
            Check("gold moved by the offers", Me.Gold == gold0 - 1250 + theirGold);
            Check("the window is down", !TradeWindow.IsOpen);

            // the X closes a trade too
            yield return new WaitForSeconds(1f);
            yield return Press(BodyOf("Bea"), false, "Bea's body");
            yield return TUntil(() => PartyPlayerMenu.OpenName == "Bea", 2f, "box");
            yield return Tap("PartyPlayerMenu/Box/Trade");
            yield return TUntil(() => Tr.Session.HasValue, 10f, "second trade open");
            yield return new WaitForSeconds(0.5f);
            yield return Tap(TradeUi + "Close");
            yield return TUntil(() => !Tr.Session.HasValue, 3f, "the X cancelled it");
            yield return new WaitForSeconds(2f);

            // and Bea's Cancel closes mine
            yield return Press(BodyOf("Bea"), false, "Bea's body");
            yield return TUntil(() => PartyPlayerMenu.OpenName == "Bea", 2f, "box");
            yield return Tap("PartyPlayerMenu/Box/Trade");
            yield return TUntil(() => Tr.Session.HasValue, 10f, "third trade open");
            yield return TUntil(() => !Tr.Session.HasValue, 12f, "Bea's Cancel closed my window");
            yield return new WaitForSeconds(3f);
            T("ui a done");
            Application.Quit();
        }

        IEnumerator TradeUiB()
        {
            Vector2 from = GameSession.Hero.Position;
            float walkUntil = Time.time + 2.5f;
            InputAdapter.BotHeld = k => k == GameKey.Right && Time.time < walkUntil && GameSession.Hero.Position.x - from.x < 70f;
            yield return TUntil(() => Me != null && Me.Inventory.Count == DevItemsGiven, 40f, "bag has the dev items");
            yield return TUntil(() => Tr.Incoming.HasValue, 60f, "first request shows");
            yield return new WaitForSeconds(0.5f);
            yield return PartyShot("b-request");
            yield return TUntil(() => !Tr.Incoming.HasValue, 8f, "Ari's Cancel closed the window");
            yield return TUntil(() => Tr.Incoming.HasValue, 15f, "second request shows");
            yield return new WaitForSeconds(0.5f);
            yield return Tap("TradeRequestWindow/Window/Decline", true);
            yield return TUntil(() => !Tr.Incoming.HasValue, 5f, "declined by finger");
            yield return TUntil(() => Tr.Incoming.HasValue, 15f, "third request shows");
            yield return new WaitForSeconds(0.5f);
            yield return Tap("TradeRequestWindow/Window/Accept");
            yield return TUntil(() => TradeWindow.IsOpen, 4f, "window open, from the button");
            yield return new WaitForSeconds(1f);

            yield return Tap(TradeUi + "Bag/Slot0", true);
            yield return Tap(TradeUi + "Bag/Slot0", true);
            yield return Tap(TradeUi + "Bag/Slot0", true);
            yield return TUntil(() => Tr.Session.Value.Mine.Items.Length == 3, 3f, "offered three by finger");
            yield return TUntil(() => Tr.Session.Value.Theirs.Gold == 1250, 30f, "Ari's gold shows");
            yield return new WaitForSeconds(0.5f);
            yield return PartyShot("b-editing");

            // inspect what she is offering
            yield return Tap(TradeUi + "TheirOffer/Slot0");
            yield return new WaitForSeconds(0.3f);
            yield return PartyShot("b-inspect");
            Check("the inspected item is Ari's +9 weapon", Tr.Session.Value.Theirs.Items[0].Enhance == 9);
            yield return Tap(TradeUi + "Inspect/Box/Close");
            yield return new WaitForSeconds(0.3f);

            // Ari locks, then this offer changes
            yield return TUntil(() => Tr.Session.Value.Theirs.Locked, 15f, "Ari locked");
            yield return new WaitForSeconds(2f);
            yield return Tap(TradeUi + "Bag/Slot0", true);
            yield return TUntil(() => !Tr.Session.Value.Theirs.Locked, 3f, "my change cleared Ari's lock");
            yield return new WaitForSeconds(0.3f);
            yield return PartyShot("b-you-changed");

            yield return Tap(TradeUi + "Lock", true);
            yield return TUntil(() => Tr.Session.Value.Mine.Locked, 3f, "locked");
            yield return TUntil(() => Tr.Session.Value.Theirs.Confirmed, 20f, "Ari confirmed");
            yield return new WaitForSeconds(0.5f);
            yield return PartyShot("b-both-locked");
            yield return Tap(TradeUi + "Confirm", true);
            yield return TUntil(() => !Tr.Session.HasValue, 5f, "trade done");
            yield return new WaitForSeconds(1f);
            yield return PartyShot("b-after");

            yield return TUntil(() => Tr.Incoming.HasValue, 20f, "second trade request shows");
            yield return Tap("TradeRequestWindow/Window/Accept");
            yield return TUntil(() => !Tr.Session.HasValue && !Tr.Incoming.HasValue, 15f, "Ari's X closed my window");
            yield return TUntil(() => Tr.Incoming.HasValue, 15f, "third trade request shows");
            yield return Tap("TradeRequestWindow/Window/Accept");
            yield return TUntil(() => TradeWindow.IsOpen, 4f, "third window open");
            yield return new WaitForSeconds(1f);
            yield return Tap(TradeUi + "Cancel");
            yield return TUntil(() => !Tr.Session.HasValue, 3f, "my Cancel closed it");
            yield return new WaitForSeconds(5f);
            T("ui b done");
            Application.Quit();
        }
    }
}
