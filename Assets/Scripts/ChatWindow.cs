using UnityEngine;
using UnityEngine.EventSystems;

namespace ZenoniaProto
{
    /// <summary>
    /// In-game chat, laid out per HudLayout and kept inside Screen.safeArea. Talks only to ChatHub. While the field has focus
    /// UiWindows.ChatTyping freezes the hero's keys (and on the mobile HUD the touch controls hide: TouchControls).
    /// Enter focuses / sends, Esc leaves, Tab switches the channel while typing. In the WebGL build the text is typed into an HTML
    /// input placed over the field (WebChatInput, for Telex / IME). Two looks share this logic (ChatPanel / ChatFeed):
    ///  - Old: mobile = collapsed by default (ChatCollapsedView: 3 fading lines + chat button with an unread badge, above the
    ///    joystick), the button opens ChatPanelView under the status plate; web = the panel sits open bottom-left, X collapses it.
    ///  - Compact (UiScreens.UseNew(UiScreen.Chat)): the newest lines are always plain text over the world (PaperChatFeedView: bottom
    ///    centre on mobile, bottom-left on PC). "Expanded" then means the slim typing bar is open under the lines (PaperChatBarView:
    ///    from the tiny chat icon, a tap on the lines or Enter; it closes on send, Esc or a tap outside) or, on PC only, the
    ///    scrollable history board (PaperChatPanelView, from the small icon beside the hint). Mobile has no board at all.
    /// The switch is read every frame, so a look change rebuilds the views.
    /// </summary>
    public sealed partial class ChatWindow : MonoBehaviour
    {
        static ChatWindow instance;

        RectTransform layer, content;
        ChatPanel panel; // the view that owns the input in use: the old panel, or the compact look's typing bar / history board
        ChatFeed collapsed;
        bool expanded, web, paper, webTyping, pendingFocus;
        bool touched; // the chat was opened or closed since Create (a look change then keeps that state instead of the look's default)
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
            instance.layer = layer;
            instance.web = HudLayout.IsWeb;
            instance.Build();
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

        /// <summary>Opens / closes the chat as its button would: the old panel; compact look: the history board on PC, the typing bar on mobile.</summary>
        public static void SetExpandedState(bool on)
        {
            if (instance == null) return;
            if (on && instance.paper) instance.panel = instance.board ?? instance.bar;
            instance.SetExpanded(on);
        }

        /// <summary>Enter pressed outside the field: expand and focus the input (ignored right after a send / cancel so Enter does not re-open it).</summary>
        public static void FocusFromKey()
        {
            if (instance == null || RecentlyTyping) return;
            instance.OpenTyping();
        }

        /// <summary>Builds the views of the look that is switched on (again when the switch changes: capture flags are read after the first build).</summary>
        void Build()
        {
            if (content != null)
            {
                if (webTyping) { webTyping = false; WebChatInput.Close(); }
                Destroy(content.gameObject);
            }
            panel = bar = board = null; collapsed = feed = null;
            closeBarAt = -1f;
            paper = UiScreens.UseNew(UiScreen.Chat);
            if (!touched) expanded = !paper && web; // old web look: open bottom-left; everything else starts on the lines / the chat button
            content = UiKit.Fill(layer, "Content");
            if (paper) BuildCompact();
            else
            {
                Vector2 bubble = web ? new Vector2(34f, 36f) : new Vector2(50f, 262f); // centre of the chat button from the bottom-left of the safe area
                float bubbleSize = web ? 50f : 56f;
                collapsed = ChatCollapsedView.Build(content, bubble, bubbleSize, () => SetExpanded(true));
                panel = ChatPanelView.Build(content, web, Refresh, () => SetExpanded(false), SendCurrent);
            }
            Hook(panel);
            if (paper && expanded) panel = board ?? bar;
            bool was = touched;
            SetExpanded(expanded);
            touched = was;
        }

        void Hook(ChatPanel p)
        {
            p.input.onEndEdit.AddListener(OnEndEdit);
            if (!WebChatInput.Enabled) return;
            p.input.enabled = false; // the HTML input does the typing; a click on the field opens it
            var click = p.field.gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(_ => FocusInput());
            click.triggers.Add(entry);
        }

        void SetExpanded(bool on)
        {
            expanded = on;
            touched = true;
            if (!on && paper) panel = bar; // board closed: Enter types on the bar again
            if (paper) ShowCompact(on);
            else
            {
                panel.root.gameObject.SetActive(on);
                collapsed.root.gameObject.SetActive(!on);
            }
            if (on) { unread = 0; collapsed.SetUnread(0); }
            else if (panel.input.isFocused) panel.input.DeactivateInputField();
            if (!on && webTyping) { webTyping = false; WebChatInput.Close(); }
            Refresh();
        }

        void FocusInput()
        {
            closeBarAt = -1f;
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
            if (panel == null) return; // a view reported a change while it was still being built
            if (expanded) panel.Refresh(ChatHub.History);
            else if (bar != null) bar.Refresh(ChatHub.History); // closed: lets the block down once the phone keyboard is gone
            if (FeedShown) collapsed.Refresh(ChatHub.History);
            refreshAt = Time.unscaledTime + 0.1f;
        }

        /// <summary>A line goes out on the panel's send channel (old look: the open tab, the World tab sends to World).</summary>
        void SendCurrent()
        {
            ChatHub.Send(panel.SendChannel, panel.input.text);
            panel.input.text = "";
        }

        /// <summary>Field lost focus: Enter (or the phone keyboard's Done) sends; Esc / clicking away cancels. Either way the typing bar closes.</summary>
        void OnEndEdit(string text)
        {
            lastTypingFrame = Time.frameCount;
            TouchScreenKeyboard k = panel.input.touchScreenKeyboard; // phone: the keyboard's Done / Send key also sends
            if (InputAdapter.Pressed(GameKey.Chat) || (k != null && k.status == TouchScreenKeyboard.Status.Done)) SendCurrent();
            if (panel == bar) closeBarAt = Time.unscaledTime + BarCloseDelay;
        }

        // ---- WebGL HTML input callbacks (SendMessage from ChatInput.jslib) ----

        void OnWebChatSubmit(string encoded) { EndWebTyping(); ChatHub.Send(panel.SendChannel, WebChatInput.Decode(encoded)); }
        void OnWebChatCancel(string unused) { EndWebTyping(); }
        void OnWebChatTab(string unused) { panel.NextChannel(); }

        void EndWebTyping()
        {
            webTyping = false;
            panel.placeholder.enabled = true;
            lastTypingFrame = Time.frameCount;
            if (panel == bar) closeBarAt = Time.unscaledTime;
        }

        void Update()
        {
            if (paper != UiScreens.UseNew(UiScreen.Chat)) Build();
            bool typing = webTyping || (panel.input != null && panel.input.isFocused);
            if (typing) { lastTypingFrame = Time.frameCount; closeBarAt = -1f; }
            UiWindows.ChatTyping = typing;
            if (web && !webTyping && panel != bar) panel.placeholder.text = typing ? "Type a message..." : "Press Enter to chat";
            if (typing && !webTyping && InputAdapter.RawPressed(GameKey.SkillPage)) panel.NextChannel(); // Tab while typing: next channel, not the skill page
            bool hidden = UiWindows.ModalOpen || HeroChangeScreen.IsOpen || InventoryPanel.IsOpen || GameSession.Hero == null;
            if (content.gameObject.activeSelf == hidden)
            {
                content.gameObject.SetActive(!hidden);
                if (hidden && webTyping) { webTyping = false; panel.placeholder.enabled = true; WebChatInput.Close(); }
                if (!hidden) Refresh(); // lines that arrived while a window covered the chat
            }
            if (pendingFocus && !hidden) { pendingFocus = false; FocusInput(); }
            TickBarClose();
            if (FeedShown && Time.unscaledTime >= refreshAt) Refresh(); // lines fade with time
        }
    }
}
