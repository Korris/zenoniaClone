using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// The lines of the notice board: one wrapped rich-text row per message (ChatTextFormat.Paper colours) in a scroll list, oldest on top.
    /// The list stays on the newest line unless the player scrolled up to read. Rows are made once and reused (the history holds at most
    /// ChatHub.MaxLines); they are rewritten only in RefreshLines. Tapping the line of another player who is in this zone opens the
    /// player box (PartyPlayerMenu: Invite / Trade), as a tap on that player would.
    /// </summary>
    public sealed partial class PaperChatPanelView
    {
        readonly List<ChatMessage> shown = new List<ChatMessage>(ChatHub.MaxLines);
        readonly List<Text> rows = new List<Text>(ChatHub.MaxLines);
        ScrollRect scroll;
        RectTransform viewport, list;
        bool jumpToNewest = true;

        void BuildLines()
        {
            viewport = UiKit.Rect(inner, "Lines", 0f, 0f, InnerW, 10f);
            UiKit.Panel(viewport, Color.clear, true); // catches the drag / mouse wheel
            viewport.gameObject.AddComponent<RectMask2D>();
            list = UiKit.Fill(viewport, "List");
            list.anchorMin = new Vector2(0f, 1f); list.pivot = new Vector2(0f, 1f); // top-stretched: its height follows the rows
            list.sizeDelta = Vector2.zero;
            var group = list.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childControlWidth = group.childControlHeight = group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            group.spacing = 3f;
            group.padding = new RectOffset(2, 2, 0, 0);
            list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = list;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
        }

        /// <summary>The list fills the paper between the chips (shown only in the All view) and the input row.</summary>
        void LayoutLines(bool chipsShown)
        {
            float top = chipsShown ? ChipH + Gap : 0f;
            viewport.anchoredPosition = new Vector2(0f, -top);
            viewport.sizeDelta = new Vector2(InnerW, InnerH - InputH - Gap - top);
        }

        void RefreshLines(IList<ChatMessage> history)
        {
            bool stick = jumpToNewest || list.rect.height <= viewport.rect.height + 1f || scroll.verticalNormalizedPosition <= 0.02f;
            jumpToNewest = false;
            filter.Collect(history, shown);
            string self = ChatHub.LocalName();
            for (int i = 0; i < shown.Count; i++)
            {
                if (i == rows.Count) rows.Add(NewRow(i));
                ChatMessage m = shown[i];
                rows[i].text = ChatTextFormat.Line(m, ChatTextFormat.Paper);
                rows[i].raycastTarget = m.Channel != ChatChannel.System && !string.IsNullOrEmpty(m.Sender) && m.Sender != self; // only another player's line is tappable
                rows[i].gameObject.SetActive(true);
            }
            for (int i = shown.Count; i < rows.Count; i++) rows[i].gameObject.SetActive(false);
            LayoutRebuilder.ForceRebuildLayoutImmediate(list);
            if (stick) scroll.verticalNormalizedPosition = 0f;
            if (!viewport.gameObject.activeInHierarchy) jumpToNewest = true; // no layout while hidden: settle on the newest line at the next refresh
        }

        Text NewRow(int index)
        {
            Text t = PaperTheme.Body(list, "Row" + index, 0f, 0f, InnerW, 20f, "", FontSize, PaperTheme.Ink);
            t.font = PaperChatStyle.Font;
            t.alignment = TextAnchor.UpperLeft;
            RectTransform rule = UiKit.Fill(t.transform, "Rule"); // faint line under the row, like the ruled paper of the mockup
            rule.anchorMax = new Vector2(1f, 0f);
            rule.offsetMin = new Vector2(0f, -2f); rule.offsetMax = new Vector2(0f, -1f);
            UiKit.Panel(rule, new Color(PaperTheme.InkSoft.r, PaperTheme.InkSoft.g, PaperTheme.InkSoft.b, 0.22f));
            var tap = t.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None; tap.navigation = new Navigation { mode = Navigation.Mode.None };
            tap.onClick.AddListener(() => RowTapped(index));
            return t;
        }

        /// <summary>A chat line carries only the sender's name: the box opens when a player of that name is in this zone (online play).</summary>
        void RowTapped(int index)
        {
            Zone zone = Zone.Main;
            if (index >= shown.Count || zone == null) return;
            string sender = shown[index].Sender;
            IReadOnlyList<HeroController> heroes = zone.Heroes.All;
            for (int i = 0; i < heroes.Count; i++)
            {
                HeroController h = heroes[i];
                if (h == null || h == LocalPlayer.Hero || h.PlayerId == 0 || h.PlayerName != sender) continue;
                PartyPlayerMenu.OpenFor(h, Input.mousePosition);
                return;
            }
        }
    }
}
