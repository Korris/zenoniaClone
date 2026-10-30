using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Chat on the left under the status panel. Expanded: gold frame, World / Map / Party / Whisper tabs, the last lines, an input field and a
    /// send button. Collapsed (default on phones): just the 3 latest lines, faded, on a dark strip; tap it to expand, the small X to collapse.
    /// Talks only to ChatHub (local echo for now). Enter focuses the input (WindowsHost), Enter again sends, Esc leaves the field.
    /// While the field has focus UiWindows.ChatTyping freezes the hero's keys.
    /// </summary>
    public sealed class ChatWindow : MonoBehaviour
    {
        const float W = 310f, ExpandedH = 196f, CollapsedH = 60f, FadeAfter = 7f;
        static readonly ChatChannel[] Tabs = { ChatChannel.World, ChatChannel.Map, ChatChannel.Party, ChatChannel.Whisper };
        static ChatWindow instance;

        RectTransform root, expandedLayer;
        Image strip;
        Text lines, collapsedLines;
        InputField input;
        readonly Image[] tabImages = new Image[4];
        readonly Text[] tabLabels = new Text[4];
        ChatChannel tab = ChatChannel.World;
        bool expanded;
        int lastTypingFrame = -10;
        float refreshAt;

        /// <summary>The field had focus this frame or the last one (E / Enter typed into it must not also talk to the NPC).</summary>
        public static bool RecentlyTyping { get { return instance != null && Time.frameCount - instance.lastTypingFrame <= 1; } }

        public static bool Expanded { get { return instance != null && instance.expanded; } }

        public static void Create(Transform canvas)
        {
            instance = UiKit.Fill(canvas, "ChatWindow").gameObject.AddComponent<ChatWindow>(); // full-screen layer: children anchor to the real screen
            instance.Build();
            instance.SetExpanded(!Application.isMobilePlatform); // phones start collapsed
            ChatHub.Received -= instance.OnReceived;
            ChatHub.Received += instance.OnReceived;
        }

        void OnDestroy()
        {
            if (instance != this) return;
            ChatHub.Received -= OnReceived;
            UiWindows.ChatTyping = false;
            instance = null;
        }

        public static void SetExpandedState(bool on) { if (instance != null) instance.SetExpanded(on); }

        /// <summary>Enter pressed outside the field: expand and focus the input (ignored right after a send / cancel so Enter does not re-open it).</summary>
        public static void FocusFromKey()
        {
            if (instance == null || RecentlyTyping) return;
            instance.SetExpanded(true);
            instance.input.ActivateInputField();
        }

        void Build()
        {
            root = UiKit.Rect(transform, "Chat", 8f, 134f, W, ExpandedH);
            strip = UiKit.Panel(root, new Color(0.02f, 0.05f, 0.1f, 0.72f), true);
            collapsedLines = UiKit.Label(UiKit.Fill(root, "CollapsedLines", 6f), "", 13, Color.white, TextAnchor.LowerLeft);
            collapsedLines.horizontalOverflow = HorizontalWrapMode.Wrap;
            var tap = root.gameObject.AddComponent<Button>(); // tap the collapsed strip to open the chat
            tap.targetGraphic = strip; tap.transition = Selectable.Transition.None;
            tap.navigation = new Navigation { mode = Navigation.Mode.None };
            tap.onClick.AddListener(() => { if (!expanded) SetExpanded(true); });

            expandedLayer = UiKit.Fill(root, "Expanded");
            UiWinKit.Sliced(expandedLayer, "panel", 0.34f, true);
            for (int i = 0; i < Tabs.Length; i++)
            {
                int index = i;
                RectTransform t = UiKit.Rect(expandedLayer, "Tab" + Tabs[i], 14f + i * 66f, 10f, 64f, 26f);
                tabImages[i] = UiWinKit.Plain(t, "tab-inactive"); tabImages[i].preserveAspect = false;
                UiWinKit.MakeButton(tabImages[i], () => SelectTab(index));
                tabLabels[i] = UiKit.Label(UiKit.Fill(t, "Label"), Tabs[i].ToString(), 13, UiWinKit.Dim, TextAnchor.MiddleCenter, true);
            }
            UiWinKit.CloseButton(expandedLayer, W - 30f, 12f, 20f, () => SetExpanded(false));

            RectTransform clip = UiKit.Rect(expandedLayer, "Lines", 14f, 40f, W - 28f, 104f);
            clip.gameObject.AddComponent<RectMask2D>();
            RectTransform text = UiKit.Fill(clip, "Text");
            lines = UiKit.Label(text, "", 13, Color.white, TextAnchor.LowerLeft);

            RectTransform field = UiKit.Rect(expandedLayer, "Input", 14f, 150f, 238f, 32f);
            Image fieldBg = UiWinKit.Sliced(field, "chat-input", 32f / 96f, true);
            Text placeholder = UiKit.Label(UiKit.Fill(field, "Placeholder", 0f), "Type a message...", 13, new Color(0.6f, 0.65f, 0.75f), TextAnchor.MiddleLeft);
            Text typed = UiKit.Label(UiKit.Fill(field, "Typed", 0f), "", 13, Color.white, TextAnchor.MiddleLeft);
            placeholder.rectTransform.offsetMin = typed.rectTransform.offsetMin = new Vector2(12f, 2f);
            placeholder.rectTransform.offsetMax = typed.rectTransform.offsetMax = new Vector2(-12f, -2f);
            placeholder.horizontalOverflow = typed.horizontalOverflow = HorizontalWrapMode.Overflow;
            input = field.gameObject.AddComponent<InputField>();
            input.targetGraphic = fieldBg; input.textComponent = typed; input.placeholder = placeholder;
            input.lineType = InputField.LineType.SingleLine; input.characterLimit = ChatHub.MaxLength;
            input.onEndEdit.AddListener(OnEndEdit);

            RectTransform send = UiKit.Rect(expandedLayer, "Send", 258f, 150f, 40f, 32f);
            UiWinKit.MakeButton(UiWinKit.Plain(send, "send"), SendCurrent).GetComponent<Image>().preserveAspect = false;
            SelectTab(0);
        }

        void SelectTab(int i)
        {
            tab = Tabs[i];
            for (int k = 0; k < Tabs.Length; k++)
            {
                tabImages[k].sprite = UiArt.Win(k == i ? "tab-active" : "tab-inactive");
                tabLabels[k].color = k == i ? UiWinKit.Cream : UiWinKit.Dim;
            }
            Refresh();
        }

        void SetExpanded(bool on)
        {
            expanded = on;
            expandedLayer.gameObject.SetActive(on);
            collapsedLines.gameObject.SetActive(!on);
            strip.enabled = !on;
            root.sizeDelta = new Vector2(W, on ? ExpandedH : CollapsedH);
            if (!on && input.isFocused) input.DeactivateInputField();
            Refresh();
        }

        void OnReceived(ChatMessage m) { Refresh(); }

        void Refresh()
        {
            if (expanded) lines.text = ChatTextFormat.Build(ChatHub.History, tab, 8);
            else collapsedLines.text = ChatTextFormat.Build(ChatHub.History, ChatChannel.World, 3, FadeAfter, 0.7f);
            refreshAt = Time.unscaledTime + 0.5f;
        }

        void SendCurrent()
        {
            ChatHub.Send(tab, input.text);
            input.text = "";
        }

        /// <summary>Field lost focus: Enter on the keyboard sends (Esc / clicking away cancels).</summary>
        void OnEndEdit(string text)
        {
            lastTypingFrame = Time.frameCount;
            if (InputAdapter.Pressed(GameKey.Chat)) SendCurrent();
        }

        void Update()
        {
            bool typing = input != null && input.isFocused;
            if (typing) lastTypingFrame = Time.frameCount;
            UiWindows.ChatTyping = typing;
            bool hidden = UiWindows.ModalOpen || HeroChangeScreen.IsOpen || InventoryPanel.IsOpen || GameSession.Hero == null;
            if (root.gameObject.activeSelf == hidden) root.gameObject.SetActive(!hidden);
            if (!expanded && Time.unscaledTime >= refreshAt) Refresh(); // old lines fade with time
        }
    }
}
