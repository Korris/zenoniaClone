using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Numeric keypad for the gold offer. The only text input in the game is the chat's InputField, which on a phone brings up the full system keyboard over the window; big keys on the
    /// window itself are quicker and need nothing from the platform. Amounts above what the player holds stop at what they hold; the server checks again anyway.
    /// </summary>
    public sealed class TradeGoldPad
    {
        const float W = 300f, H = 384f, KeyW = 78f, KeyH = 48f, Gap = 8f;
        static readonly string[] Keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "Max", "0", "Del" };

        readonly RectTransform root;
        readonly Text shown;
        Action<int> done;
        long amount;
        int limit;

        public TradeGoldPad(RectTransform board)
        {
            root = UiKit.Rect(board, "Pad", 0f, 0f, UiKit.RefWidth, UiKit.RefHeight);
            UiKit.Panel(root, new Color(0f, 0f, 0f, 0.5f), true);
            RectTransform box = UiKit.Rect(root, "Box", (UiKit.RefWidth - W) * 0.5f, (UiKit.RefHeight - H) * 0.5f, W, H);
            PartyArt.Sliced(box, "invite-frame", PartyArt.InviteFrameScale, true);
            UiKit.Label(box, "Title", 24f, 12f, W - 48f, 28f, "Offer Gold", 19, UiWinKit.Gold, TextAnchor.MiddleCenter, true);
            RectTransform field = UiKit.Rect(box, "Field", 24f, 42f, W - 48f, 44f);
            TradeArt.ByHeight(field, "gold-field", 44f);
            TradeArt.Plain(UiKit.Rect(field, "Coin", -2f, 5f, 34f, 34f), "coin");
            shown = UiKit.Label(field, "Number", 36f, 0f, W - 48f - 36f - 18f, 44f, "0", 19, Color.white, TextAnchor.MiddleRight, true);
            float gridX = (W - KeyW * 3f - Gap * 2f) * 0.5f;
            for (int i = 0; i < Keys.Length; i++)
            {
                string key = Keys[i];
                int cap = i;
                UiWinKit.ArtButton(box, "Key" + key, "btn-navy", gridX + (i % 3) * (KeyW + Gap), 94f + (i / 3) * (KeyH + 6f), KeyW, KeyH, key, 19, UiWinKit.Cream, () => Press(Keys[cap]));
            }
            PartyArt.ArtButton(box, "OK", "btn-accept", 24f, H - 62f, 120f, 44f, "OK", 18, new Color(0.25f, 0.12f, 0.02f), Ok);
            PartyArt.ArtButton(box, "Back", "btn-decline", W - 24f - 120f, H - 62f, 120f, 44f, "Back", 18, UiWinKit.Cream, Close);
            root.gameObject.SetActive(false);
        }

        public bool IsOpen { get { return root.gameObject.activeSelf; } }

        public void Open(int current, int gold, Action<int> onDone)
        {
            amount = current; limit = gold; done = onDone;
            Show();
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
        }

        public void Close() { root.gameObject.SetActive(false); done = null; }

        void Press(string key)
        {
            if (key == "Max") amount = limit;
            else if (key == "Del") amount /= 10;
            else amount = Math.Min(amount * 10 + (key[0] - '0'), limit);
            Show();
        }

        void Ok()
        {
            Action<int> d = done;
            int chosen = (int)amount;
            Close();
            if (d != null) d(chosen);
        }

        void Show() { shown.text = amount.ToString("N0", CultureInfo.InvariantCulture); }
    }
}
