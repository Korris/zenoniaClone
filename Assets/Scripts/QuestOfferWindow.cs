using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Quest offer window (win-panel): quest icon + title + close X, description, Objectives box (checkbox, "Defeat X  0 / N"), Rewards box with
    /// item slots, and Accept (gold) / Decline (navy). Centred; the hero is frozen while it is open (UiWindows.ModalOpen). Esc / X / Decline close it.
    /// </summary>
    public sealed class QuestOfferWindow : MonoBehaviour
    {
        const float W = 420f, H = 336f;
        static QuestOfferWindow instance;

        public static bool IsOpen { get { return instance != null && instance.open; } }

        RectTransform root;
        Text title, description, objective, count;
        Transform rewards;
        bool open;
        Action onAccept;

        public static void Create(Transform canvas)
        {
            instance = UiKit.Fill(canvas, "QuestOfferWindow").gameObject.AddComponent<QuestOfferWindow>(); // full-screen layer: children anchor to the real screen
            instance.Build();
        }

        void OnDestroy() { if (instance == this) instance = null; }

        public static void Open(QuestDef q, Action accepted)
        {
            if (instance == null) return;
            instance.Fill(q);
            instance.onAccept = accepted;
            instance.open = true;
            instance.root.gameObject.SetActive(true);
        }

        public static void Close()
        {
            if (instance == null) return;
            instance.open = false;
            instance.root.gameObject.SetActive(false);
        }

        /// <summary>E / Enter on the open window = Accept.</summary>
        public static void Accept() { if (IsOpen) instance.DoAccept(); }

        void DoAccept()
        {
            Action cb = onAccept;
            Close();
            if (cb != null) cb();
        }

        void Build()
        {
            root = UiWinKit.Anchored(transform, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 8f, W, H);
            UiWinKit.Sliced(root, "panel", W / 639f, true); // swallows taps so they never reach the world
            UiWinKit.Plain(UiKit.Rect(root, "QuestIcon", 30f, 22f, 40f, 40f), "quest-icon");
            title = UiKit.Label(root, "Title", 82f, 20f, 270f, 44f, "", 22, new Color(1f, 0.93f, 0.7f), TextAnchor.MiddleLeft, true);
            UiWinKit.CloseButton(root, W - 62f, 25f, 32f, Close);
            UiKit.Panel(UiKit.Rect(root, "Rule", 30f, 68f, W - 60f, 1.5f), new Color(0.85f, 0.65f, 0.25f, 0.7f));
            description = UiKit.Label(root, "Description", 32f, 74f, W - 64f, 52f, "", 14, UiWinKit.Cream, TextAnchor.UpperLeft);

            RectTransform obj = UiKit.Rect(root, "Objectives", 30f, 130f, W - 60f, 66f);
            UiWinKit.Sliced(obj, "section", 0.3f);
            UiKit.Label(obj, "Header", 12f, 2f, 200f, 24f, "Objectives", 15, UiWinKit.Gold, TextAnchor.MiddleLeft, true);
            Image box = UiKit.Icon(obj, "Check", 12f, 34f, 18f);
            box.sprite = UiSprites.Ring(); box.color = UiWinKit.Dim;
            objective = UiKit.Label(obj, "Goal", 38f, 29f, 220f, 28f, "", 15, Color.white, TextAnchor.MiddleLeft, true);
            count = UiKit.Label(obj, "Count", W - 60f - 110f, 29f, 98f, 28f, "", 15, UiWinKit.Cream, TextAnchor.MiddleRight, true);

            RectTransform rew = UiKit.Rect(root, "Rewards", 30f, 202f, W - 60f, 80f);
            UiWinKit.Sliced(rew, "section", 0.3f);
            UiKit.Label(rew, "Header", 12f, 2f, 200f, 24f, "Rewards", 15, UiWinKit.Gold, TextAnchor.MiddleLeft, true);
            rewards = rew;

            UiWinKit.ArtButton(root, "Accept", "btn-gold", 30f, 288f, 172f, 36f, "Accept", 19, new Color(0.25f, 0.12f, 0.02f), DoAccept);
            UiWinKit.ArtButton(root, "Decline", "btn-navy", W - 30f - 172f, 288f, 172f, 36f, "Decline", 19, UiWinKit.Cream, Close);
            root.gameObject.SetActive(false);
        }

        void Fill(QuestDef q)
        {
            title.text = q.Title;
            description.text = q.Description;
            objective.text = "Defeat " + q.MonsterName;
            count.text = "0 / " + q.Count;
            for (int i = rewards.childCount - 1; i >= 0; i--)
                if (rewards.GetChild(i).name.StartsWith("Slot")) Destroy(rewards.GetChild(i).gameObject);
            int n = 0;
            AddSlot(n++, LootSprites.Coin(), q.RewardGold > 0 ? q.RewardGold + " G" : null);
            AddSlot(n++, LootSprites.Stone(), q.RewardStones > 0 ? "x" + q.RewardStones : null);
            AddSlot(n++, LootSprites.Potion(true), q.RewardHpPotions > 0 ? "x" + q.RewardHpPotions : null);
            for (; n < 5; n++) AddSlot(n, null, null); // empty slots, like the mockup
        }

        void AddSlot(int i, Sprite icon, string caption)
        {
            const float size = 46f, gap = 10f;
            RectTransform slot = UiKit.Rect(rewards, "Slot" + i, 12f + i * (size + gap), 28f, size, size);
            Image bg = UiWinKit.Sliced(slot, "slot", size / 160f);
            if (icon == null) { bg.color = new Color(1f, 1f, 1f, 0.35f); return; }
            UiKit.Icon(slot, "Icon", 8f, 6f, size - 16f).sprite = icon;
            UiKit.Label(slot, "Qty", 0f, size - 14f, size - 3f, 14f, caption, 11, Color.white, TextAnchor.MiddleRight, true);
        }
    }
}
