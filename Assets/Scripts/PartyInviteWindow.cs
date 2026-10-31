using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// The invite somebody sent you: who, their class, the seconds left, Accept and Decline. Open exactly while the server says the invite is, so the inviter's Cancel or the 30 s closes it at once.
    /// Not modal: the world does not stop for an invite, and the hero can still move and fight around it.
    /// </summary>
    public sealed class PartyInviteWindow : MonoBehaviour
    {
        const float W = 340f, H = 172f, IconSize = 44f, BtnW = 136f, BtnH = 44f;

        RectTransform window;
        Text line, classLine, timer;
        Image classIcon;
        double answered;

        public static void Create(Transform canvas)
        {
            UiKit.Fill(canvas, "PartyInviteWindow").gameObject.AddComponent<PartyInviteWindow>().Build(); // full-screen layer: children anchor to the real screen
        }

        void Build()
        {
            window = UiWinKit.Anchored(transform, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 10f, 96f, W, H); // 10 right: clear of the open chat on a 3:2 screen
            PartyArt.Sliced(window, "invite-frame", PartyArt.InviteFrameScale, true); // swallows taps so they never reach the world
            UiKit.Label(window, "Title", 30f, 12f, W - 60f, 30f, "Party Invite", 21, UiWinKit.Gold, TextAnchor.MiddleCenter, true);
            UiKit.Panel(UiKit.Rect(window, "Rule", 34f, 44f, W - 68f, 1.5f), new Color(0.85f, 0.65f, 0.25f, 0.7f));
            classIcon = PartyArt.Plain(UiKit.Rect(window, "Class", 24f, 54f, IconSize, IconSize), "class-warrior");
            line = UiKit.Label(window, "Line", 78f, 52f, W - 78f - 22f, 24f, "", 15, UiWinKit.Cream, TextAnchor.MiddleLeft);
            classLine = UiKit.Label(window, "ClassName", 78f, 76f, 150f, 22f, "", 14, UiWinKit.Dim, TextAnchor.MiddleLeft);
            timer = UiKit.Label(window, "Timer", W - 22f - 70f, 76f, 70f, 22f, "", 15, UiWinKit.Gold, TextAnchor.MiddleRight, true);
            PartyArt.ArtButton(window, "Accept", "btn-accept", 26f, 112f, BtnW, BtnH, "Accept", 18, new Color(0.25f, 0.12f, 0.02f), () => Answer(PartyOp.Accept));
            PartyArt.ArtButton(window, "Decline", "btn-decline", W - 26f - BtnW, 112f, BtnW, BtnH, "Decline", 18, UiWinKit.Cream, () => Answer(PartyOp.Decline));
            window.gameObject.SetActive(false);
        }

        void Answer(PartyOp op)
        {
            PartyClientState.OpenInvite? open = NetClient.Party.Incoming;
            if (!open.HasValue) return;
            answered = open.Value.ExpiresAtMs; // closes now; a second press must not answer again
            NetClient.SendParty(op, open.Value.Other);
        }

        void Update()
        {
            PartyClientState.OpenInvite? open = NetClient.Party.Incoming;
            bool show = PartyPanel.Live && open.HasValue && open.Value.ExpiresAtMs != answered;
            if (window.gameObject.activeSelf != show)
            {
                window.gameObject.SetActive(show);
                if (show) transform.SetAsLastSibling(); // above whichever window was built later
            }
            if (!show) return;
            int cls = open.Value.HeroClass;
            line.text = open.Value.Name + " invites you to a party";
            classLine.text = cls < HeroClassDef.All.Length ? HeroClassDef.All[cls].Name : "";
            classIcon.sprite = PartyArt.ClassIcon(cls);
            timer.text = Mathf.Max(0, Mathf.CeilToInt((float)((open.Value.ExpiresAtMs - NetClient.ClientMs) / 1000.0))) + " s";
        }
    }
}
