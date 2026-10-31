using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// --party-role ui-leader|ui-ally|joiner|joiner-away (Tools/net-party-test.ps1 -Ui): the party UI driven through its real input path. A fake BaseInput on the EventSystem's module
    /// presses the mouse or a finger at a screen point, so the module does its own raycast, press and click, and PartyPlayerMenu its own tap detection. Nothing calls a button handler
    /// directly. Screenshots go to --shot-path.
    /// </summary>
    public partial class DebugCapture
    {
        /// <summary>Stands in for Input: one scripted press at a time, with the position it says.</summary>
        sealed class FakeInput : BaseInput
        {
            public Vector2 Pos;
            public bool UseTouch;
            public int DownFrame = -1, UpFrame = -1;

            int F { get { return Time.frameCount; } }
            bool Active { get { return DownFrame >= 0 && F >= DownFrame && F <= UpFrame; } }

            public override Vector2 mousePosition { get { return Pos; } }
            public override bool mousePresent { get { return true; } }
            public override bool GetMouseButtonDown(int b) { return !UseTouch && b == 0 && F == DownFrame; }
            public override bool GetMouseButtonUp(int b) { return !UseTouch && b == 0 && F == UpFrame; }
            public override bool GetMouseButton(int b) { return !UseTouch && b == 0 && Active && F < UpFrame; }
            public override int touchCount { get { return UseTouch && Active ? 1 : 0; } }
            public override Touch GetTouch(int i)
            {
                return new Touch { fingerId = 3, type = TouchType.Direct, position = Pos, rawPosition = Pos, phase = F == DownFrame ? TouchPhase.Began : F == UpFrame ? TouchPhase.Ended : TouchPhase.Stationary, tapCount = 1 };
            }
        }

        FakeInput fakeInput;

        /// <summary>One press and release at a screen point, as the player's mouse or finger would.</summary>
        IEnumerator Press(Vector2 screen, bool touch, string what)
        {
            if (fakeInput == null)
            {
                fakeInput = EventSystem.current.gameObject.AddComponent<FakeInput>();
                EventSystem.current.currentInputModule.inputOverride = fakeInput;
            }
            EventSystem.current.SendMessage("OnApplicationFocus", true); // an unfocused window ignores pointers, and this script must not take the focus from whoever is at the machine
            fakeInput.UseTouch = touch;
            fakeInput.Pos = screen;
            yield return null; // the module sees where the pointer is first
            fakeInput.DownFrame = Time.frameCount + 1;
            fakeInput.UpFrame = fakeInput.DownFrame + 3;
            while (Time.frameCount <= fakeInput.UpFrame + 1) yield return null;
            E2e((touch ? "touch " : "click ") + what + " at " + screen);
        }

        static Vector2 CentreOf(string path)
        {
            GameObject go = GameObject.Find(path);
            if (go == null) { E2e("MISSING on screen: " + path); return new Vector2(-100f, -100f); }
            var rt = (RectTransform)go.transform;
            return RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
        }

        static Vector2 BodyOf(string name)
        {
            HeroController h = HeroNamed(name);
            return Camera.main.WorldToScreenPoint(new Vector3(h.Position.x, h.Position.y + 12f, 0f));
        }

        static Vector2 TagOf(string name)
        {
            HeroController h = HeroNamed(name);
            return Camera.main.WorldToScreenPoint(new Vector3(h.Position.x, h.Position.y + 30f, 0f));
        }

        void StartPartyUiBot(string role)
        {
            partyShots = NetMode.RendersFx;
            if (role == "ui-leader") StartCoroutine(UiLeader());
            else if (role == "ui-ally") StartCoroutine(UiAlly());
            else StartCoroutine(Joiner(role == "joiner-away"));
        }

        IEnumerator UiLeader()
        {
            PartyClientState party = NetClient.Party;
            yield return Until(() => HeroNamed("Allie") != null && HeroNamed("Luna") != null && HeroNamed("Kirito-the-Long1") != null && HeroNamed("Sora") != null, 40f, "see everybody");
            yield return new WaitForSeconds(4f); // Allie walks clear of the potion buttons

            // a real tap on a body opens the box; its real Invite button sends the invite
            yield return Press(BodyOf("Allie"), false, "Allie's body");
            yield return Until(() => PartyPlayerMenu.OpenName == "Allie", 2f, "the box opened on Allie, by mouse");
            yield return PartyShot("ui-leader-menu");
            yield return Press(CentreOf("PartyPlayerMenu/Box/Invite"), false, "Invite");
            yield return Until(() => party.Outgoing.HasValue, 3f, "invite pending, from the button");
            yield return new WaitForSeconds(0.5f);
            yield return PartyShot("ui-leader-pending");
            yield return new WaitForSeconds(2.5f);
            yield return Press(CentreOf("PartyPanel/Panel/Pending/Cancel"), false, "Cancel");
            yield return Until(() => !party.Outgoing.HasValue, 3f, "invite cancelled, from the button");
            yield return new WaitForSeconds(2f);

            // a tap on empty ground opens nothing
            yield return Press(new Vector2(Screen.width * 0.62f, Screen.height * 0.3f), true, "empty ground");
            yield return new WaitForSeconds(0.2f);
            E2e("box after a tap on empty ground: '" + PartyPlayerMenu.OpenName + "'");

            // a finger on the name tag takes the same way
            yield return Press(TagOf("Allie"), true, "Allie's name tag");
            yield return Until(() => PartyPlayerMenu.OpenName != null, 2f, "the box opened by touch on the tag");
            E2e("the tag's box is on: " + PartyPlayerMenu.OpenName);
            yield return Press(CentreOf("PartyPlayerMenu/Box/Invite"), true, "Invite");
            yield return Until(() => party.InParty, 8f, "Allie accepted with the Accept button");
            yield return new WaitForSeconds(1f);
            yield return PartyShot("ui-leader-2members");

            foreach (string joiner in new[] { "Luna", "Kirito-the-Long1", "Sora" })
            {
                int before = party.Members.Count;
                NetClient.SendParty(PartyOp.Invite, HeroNamed(joiner).PlayerId);
                yield return Until(() => party.Members.Count > before, 8f, joiner + " joined");
                yield return new WaitForSeconds(1f);
            }
            yield return PartyShot("ui-leader-5members");

            yield return Until(() => party.Members.Count == 5 && party.Members[4].MapId != party.Members[0].MapId, 60f, "Sora shows away");
            yield return new WaitForSeconds(1f);
            yield return PartyShot("ui-leader-away");

            yield return Press(CentreOf("PartyPanel/Panel/Member1/Kick"), false, "Kick on Allie's row");
            yield return Until(() => party.Members.Count == 4, 3f, "Allie kicked, from the button");
            yield return new WaitForSeconds(1f);
            NetClient.SendParty(PartyOp.Leave);
            yield return Until(() => !party.InParty, 3f, "left through a request, so the invite below comes from a free player");
            yield return new WaitForSeconds(1f);
            NetClient.SendParty(PartyOp.Invite, HeroNamed("Allie").PlayerId);
            yield return Until(() => !party.Outgoing.HasValue, 12f, "Allie declined with the Decline button");
            yield return new WaitForSeconds(1f);
            NetClient.SendParty(PartyOp.Invite, HeroNamed("Allie").PlayerId);
            yield return Until(() => party.InParty, 12f, "Allie accepted once more");
            yield return new WaitForSeconds(1f);
            yield return Press(CentreOf("PartyPanel/Panel/Leave"), false, "Leave");
            yield return Until(() => !party.InParty, 3f, "left, from the button");
            E2e("ui leader done");
            Application.Quit();
        }

        IEnumerator UiAlly()
        {
            PartyClientState party = NetClient.Party;
            // heroes begin around the entrance, some under the potion buttons, where a tap rightly goes to the button: walk clear of them
            Vector2 from = GameSession.Hero.Position;
            float walkUntil = Time.time + 2.5f;
            InputAdapter.BotHeld = k => k == GameKey.Right && Time.time < walkUntil && GameSession.Hero.Position.x - from.x < 70f;
            yield return Until(() => party.Incoming.HasValue, 60f, "first invite shows");
            yield return new WaitForSeconds(1f);
            yield return PartyShot("ui-ally-invite");
            yield return Until(() => !party.Incoming.HasValue, 8f, "the leader's Cancel closed the window");
            E2e("window on screen after the cancel: " + (GameObject.Find("PartyInviteWindow/Window") != null));
            yield return Until(() => party.Incoming.HasValue, 15f, "second invite shows");
            yield return new WaitForSeconds(0.5f);
            yield return Press(CentreOf("PartyInviteWindow/Window/Accept"), false, "Accept");
            yield return Until(() => party.InParty, 5f, "in a party, from the button");
            yield return new WaitForSeconds(1f);
            yield return PartyShot("ui-ally-2members");
            yield return Until(() => !party.InParty, 90f, "kicked");
            yield return Until(() => party.Incoming.HasValue, 15f, "third invite shows");
            yield return new WaitForSeconds(0.5f);
            yield return Press(CentreOf("PartyInviteWindow/Window/Decline"), true, "Decline");
            yield return Until(() => !party.Incoming.HasValue, 5f, "declined, from the button");
            yield return Until(() => party.Incoming.HasValue, 15f, "fourth invite shows");
            yield return new WaitForSeconds(0.5f);
            yield return Press(CentreOf("PartyInviteWindow/Window/Accept"), true, "Accept");
            yield return Until(() => party.InParty, 5f, "in a party again");
            yield return new WaitForSeconds(5f);
            E2e("ui ally done");
            Application.Quit();
        }

        /// <summary>Says yes to everything; the away one then walks through the near portal once the party is full.</summary>
        IEnumerator Joiner(bool away)
        {
            PartyClientState party = NetClient.Party;
            yield return Until(() => party.Incoming.HasValue, 90f, "invite shows");
            if (!party.Incoming.HasValue) yield break;
            NetClient.SendParty(PartyOp.Accept, party.Incoming.Value.Other);
            if (!away) yield break;
            yield return Until(() => party.Members.Count == 5, 30f, "party full");
            yield return new WaitForSeconds(3f);
            EnablePortalBot();
        }
    }
}
