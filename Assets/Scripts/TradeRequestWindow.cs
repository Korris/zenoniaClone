using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// The two small windows around a trade request: the one somebody sent you (who, their class, the seconds left, Accept and Decline) and the one you sent (who, the seconds left, Cancel).
    /// Open exactly while the server says the request is, and not modal: the world does not stop for a request.
    /// </summary>
    public sealed class TradeRequestWindow : MonoBehaviour
    {
        const float W = 340f, H = 172f, WaitH = 120f, IconSize = 44f, ClockSize = 22f, BtnW = 136f, BtnH = 44f;

        RectTransform window, waiting;
        Text line, classLine, timer, waitLine, waitTimer;
        Image classIcon;
        double answered, cancelled;

        public static void Create(Transform canvas)
        {
            UiKit.Fill(canvas, "TradeRequestWindow").gameObject.AddComponent<TradeRequestWindow>().Build(); // full-screen layer: children anchor to the real screen
        }

        void Build()
        {
            window = UiWinKit.Anchored(transform, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 10f, 96f, W, H); // same place as the party invite: clear of the open chat on a 3:2 screen
            PartyArt.Sliced(window, "invite-frame", PartyArt.InviteFrameScale, true); // swallows taps so they never reach the world
            UiKit.Label(window, "Title", 30f, 12f, W - 60f, 30f, "Trade Request", 21, UiWinKit.Gold, TextAnchor.MiddleCenter, true);
            UiKit.Panel(UiKit.Rect(window, "Rule", 34f, 44f, W - 68f, 1.5f), new Color(0.85f, 0.65f, 0.25f, 0.7f));
            classIcon = PartyArt.Plain(UiKit.Rect(window, "Class", 24f, 54f, IconSize, IconSize), "class-warrior");
            line = UiKit.Label(window, "Line", 78f, 52f, W - 78f - 22f, 24f, "", 15, UiWinKit.Cream, TextAnchor.MiddleLeft);
            classLine = UiKit.Label(window, "ClassName", 78f, 76f, 150f, 22f, "", 14, UiWinKit.Dim, TextAnchor.MiddleLeft);
            TradeArt.Plain(UiKit.Rect(window, "Clock", W - 22f - 44f - 4f - ClockSize, 76f, ClockSize, ClockSize), "clock");
            timer = UiKit.Label(window, "Timer", W - 22f - 44f, 76f, 44f, 22f, "", 15, UiWinKit.Gold, TextAnchor.MiddleRight, true);
            PartyArt.ArtButton(window, "Accept", "btn-accept", 26f, 112f, BtnW, BtnH, "Accept", 18, new Color(0.25f, 0.12f, 0.02f), () => Answer(TradeOp.Accept));
            PartyArt.ArtButton(window, "Decline", "btn-decline", W - 26f - BtnW, 112f, BtnW, BtnH, "Decline", 18, UiWinKit.Cream, () => Answer(TradeOp.Decline));
            window.gameObject.SetActive(false);

            waiting = UiWinKit.Anchored(transform, "Waiting", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 10f, 96f, W, WaitH);
            PartyArt.Sliced(waiting, "invite-frame", PartyArt.InviteFrameScale, true);
            waitLine = UiKit.Label(waiting, "Line", 28f, 14f, W - 28f - 96f, 40f, "", 15, UiWinKit.Cream, TextAnchor.MiddleLeft);
            TradeArt.Plain(UiKit.Rect(waiting, "Clock", W - 26f - 44f - 4f - ClockSize, 23f, ClockSize, ClockSize), "clock");
            waitTimer = UiKit.Label(waiting, "Timer", W - 26f - 44f, 23f, 44f, 22f, "", 15, UiWinKit.Gold, TextAnchor.MiddleRight, true);
            PartyArt.ArtButton(waiting, "Cancel", "btn-cancel", (W - BtnW) * 0.5f, 62f, BtnW, BtnH, "Cancel", 18, Color.white, Cancel);
            waiting.gameObject.SetActive(false);
        }

        void Answer(TradeOp op)
        {
            TradeClientState.OpenRequest? open = NetClient.Trade.Incoming;
            if (!open.HasValue) return;
            answered = open.Value.ExpiresAtMs; // closes now; a second press must not answer again
            NetClient.SendTrade(op, open.Value.Other);
        }

        void Cancel()
        {
            TradeClientState.OpenRequest? open = NetClient.Trade.Outgoing;
            if (!open.HasValue) return;
            cancelled = open.Value.ExpiresAtMs;
            NetClient.SendTrade(TradeOp.CancelRequest);
        }

        void Update()
        {
            TradeClientState trade = NetClient.Trade;
            TradeClientState.OpenRequest? open = trade.Incoming, sent = trade.Outgoing;
            bool show = PartyPanel.Live && open.HasValue && open.Value.ExpiresAtMs != answered;
            bool wait = PartyPanel.Live && sent.HasValue && sent.Value.ExpiresAtMs != cancelled;
            if (window.gameObject.activeSelf != show || waiting.gameObject.activeSelf != wait)
            {
                window.gameObject.SetActive(show);
                waiting.gameObject.SetActive(wait);
                if (show || wait) transform.SetAsLastSibling(); // above whichever window was built later
            }
            if (show)
            {
                int cls = open.Value.HeroClass;
                line.text = open.Value.Name + " wants to trade";
                classLine.text = cls < HeroClassDef.All.Length ? HeroClassDef.All[cls].Name : "";
                classIcon.sprite = PartyArt.ClassIcon(cls);
                timer.text = Seconds(open.Value) + " s";
            }
            if (wait)
            {
                waitLine.text = "Waiting for " + sent.Value.Name + " to answer";
                waitTimer.text = Seconds(sent.Value) + " s";
            }
        }

        static int Seconds(TradeClientState.OpenRequest r) { return Mathf.Max(0, Mathf.CeilToInt((float)((r.ExpiresAtMs - NetClient.ClientMs) / 1000.0))); }
    }
}
