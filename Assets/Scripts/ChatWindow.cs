using UnityEngine;
using UnityEngine.EventSystems;

namespace ZenoniaProto
{
    /// <summary>
    /// In-game chat, laid out per HudLayout and kept inside Screen.safeArea:
    ///  - Mobile: collapsed by default (ChatCollapsedView: 3 fading lines + chat button with an unread badge, above the joystick);
    ///    the button opens ChatPanelView under the status plate; while the keyboard is up the touch controls hide (TouchControls).
    ///  - Web: the panel sits open bottom-left ("Press Enter to chat"); X collapses it to the button. Enter focuses / sends, Esc leaves,
    ///    Tab switches the channel while typing. In the WebGL build the text is typed into an HTML input (WebChatInput, for Telex / IME).
    /// Talks only to ChatHub. While the field has focus UiWindows.ChatTyping freezes the hero's keys.
    /// </summary>
    public sealed class ChatWindow : MonoBehaviour
    {
        static ChatWindow instance;

        RectTransform content;
        ChatPanelView panel;
        ChatCollapsedView collapsed;
        ChatChannel tab = ChatChannel.World;
        bool expanded, web, webTyping, pendingFocus;
        int lastTypingFrame = -10, unread;
        float refreshAt;

        /// <summary>The field had focus this frame or the last one (E / Enter typed into it must not also talk to the NPC).</summary>
        public static bool RecentlyTyping { get { return instance != null && Time.frameCount - instance.lastTypingFrame <= 1; } }

        public static bool Expanded { get { return instance != null && instance.expanded; } }

        public static void Create(Transform canvas)
        {
            RectTransform layer = UiKit.Fill(canvas, "ChatWindow"); // full-screen layer (fitted to the safe area): children anchor to the real screen
            layer.gameObject.AddComponent<SafeAreaFitter>();
            instance = layer.gameObject.AddComponent<ChatWindow>();
            instance.Build(layer);
            ChatHub.Received -= instance.OnReceived;
            ChatHub.Received += instance.OnReceived;
        }

        void OnDestroy()
        {
            if (instance != this) return;
            ChatHub.Received -= OnReceived;
            if (webTyping) WebChatInput.Close();
            UiWindows.ChatTyping = false;
            instance = null;
        }

        public static void SetExpandedState(bool on) { if (instance != null) instance.SetExpanded(on); }

        /// <summary>Enter pressed outside the field: expand and focus the input (ignored right after a send / cancel so Enter does not re-open it).</summary>
        public static void FocusFromKey()
        {
            if (instance == null || RecentlyTyping) return;
            instance.SetExpanded(true);
            instance.FocusInput();
        }

        void Build(RectTransform layer)
        {
            web = HudLayout.IsWeb;
            content = UiKit.Fill(layer, "Content");
            collapsed = web ? ChatCollapsedView.Build(content, new Vector2(34f, 36f), 50f, () => SetExpanded(true))
                            : ChatCollapsedView.Build(content, new Vector2(50f, 262f), 56f, () => SetExpanded(true));
            panel = ChatPanelView.Build(content, web, SelectTab, () => SetExpanded(false), SendCurrent);
            panel.input.onEndEdit.AddListener(OnEndEdit);
            if (WebChatInput.Enabled)
            {
                panel.input.enabled = false; // the HTML input does the typing; a click on the field opens it
                var click = panel.field.gameObject.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                entry.callback.AddListener(_ => FocusInput());
                click.triggers.Add(entry);
            }
            SelectTab(0);
            SetExpanded(web); // web: open bottom-left; mobile: collapsed until the chat button is tapped
        }

        void SelectTab(int i)
        {
            tab = ChatPanelView.Tabs[i];
            panel.ShowTab(i);
            Refresh();
        }

        void SetExpanded(bool on)
        {
            expanded = on;
            panel.root.gameObject.SetActive(on);
            collapsed.root.gameObject.SetActive(!on);
            if (on) { unread = 0; collapsed.SetUnread(0); }
            else if (panel.input.isFocused) panel.input.DeactivateInputField();
            if (!on && webTyping) { webTyping = false; WebChatInput.Close(); }
            Refresh();
        }

        void FocusInput()
        {
            if (!content.gameObject.activeInHierarchy) { pendingFocus = true; return; } // hidden this frame (hero spawning): focus once it shows
            if (!WebChatInput.Enabled) { panel.input.ActivateInputField(); return; }
            webTyping = true;
            panel.placeholder.enabled = false;
            WebChatInput.Open(gameObject.name, panel.field, "Type a message...", panel.placeholder.fontSize);
        }

        void OnReceived(ChatMessage m)
        {
            if (!expanded && m.Sender != ChatHub.LocalName()) { unread++; collapsed.SetUnread(unread); }
            Refresh();
        }

        void Refresh()
        {
            if (expanded) panel.lines.text = ChatTextFormat.Build(ChatHub.History, tab, panel.maxLines);
            else collapsed.Refresh(ChatHub.History);
            refreshAt = Time.unscaledTime + 0.1f;
        }

        /// <summary>A line goes out on the open tab (the World tab sends to World).</summary>
        void SendCurrent()
        {
            ChatHub.Send(tab, panel.input.text);
            panel.input.text = "";
        }

        /// <summary>Field lost focus: Enter (or the phone keyboard's Done) sends; Esc / clicking away cancels.</summary>
        void OnEndEdit(string text)
        {
            lastTypingFrame = Time.frameCount;
            TouchScreenKeyboard k = panel.input.touchScreenKeyboard; // phone: the keyboard's Done / Send key also sends
            if (InputAdapter.Pressed(GameKey.Chat) || (k != null && k.status == TouchScreenKeyboard.Status.Done)) SendCurrent();
        }

        // ---- WebGL HTML input callbacks (SendMessage from ChatInput.jslib) ----

        void OnWebChatSubmit(string encoded) { EndWebTyping(); ChatHub.Send(tab, WebChatInput.Decode(encoded)); }
        void OnWebChatCancel(string unused) { EndWebTyping(); }
        void OnWebChatTab(string unused) { NextTab(); }

        void EndWebTyping()
        {
            webTyping = false;
            panel.placeholder.enabled = true;
            lastTypingFrame = Time.frameCount;
        }

        void NextTab() { SelectTab((System.Array.IndexOf(ChatPanelView.Tabs, tab) + 1) % ChatPanelView.Tabs.Length); }

        void Update()
        {
            bool typing = webTyping || (panel.input != null && panel.input.isFocused);
            if (typing) lastTypingFrame = Time.frameCount;
            UiWindows.ChatTyping = typing;
            if (web && !webTyping) panel.placeholder.text = typing ? "Type a message..." : "Press Enter to chat";
            if (typing && !webTyping && InputAdapter.RawPressed(GameKey.SkillPage)) NextTab(); // Tab while typing: next channel, not the skill page
            bool hidden = UiWindows.ModalOpen || HeroChangeScreen.IsOpen || InventoryPanel.IsOpen || GameSession.Hero == null;
            if (content.gameObject.activeSelf == hidden)
            {
                content.gameObject.SetActive(!hidden);
                if (hidden && webTyping) { webTyping = false; panel.placeholder.enabled = true; WebChatInput.Close(); }
            }
            if (pendingFocus && !hidden) { pendingFocus = false; FocusInput(); }
            if (!expanded && Time.unscaledTime >= refreshAt) Refresh(); // collapsed lines fade with time
        }
    }
}
