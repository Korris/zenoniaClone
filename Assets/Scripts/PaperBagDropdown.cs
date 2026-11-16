using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Paper dropdown of the Adventure Paper kit (the cut piece kit/dropdown-paper carries the arrow): a face showing the current choice
    /// and, when tapped, a paper list of the choices under it. Built for the bag's Sort box; nothing in it knows about the bag, so the
    /// Skill screen can use it too. Put it late in its parent's sibling order: the open list must draw over what is below it.
    /// </summary>
    public sealed class PaperDropdown
    {
        const float RowH = 24f;
        public readonly RectTransform Root;
        readonly Text label;
        readonly RectTransform list;
        readonly Image[] rows;
        readonly string[] options;

        public bool IsOpen { get { return list.gameObject.activeSelf; } }

        /// <summary>onPick gets the index of the tapped choice; the caller then calls Show with what is now current.</summary>
        public PaperDropdown(Transform parent, string name, float x, float y, float w, float h, string[] options, Action<int> onPick)
        {
            this.options = options;
            Root = UiKit.Rect(parent, name, x, y, w, h);
            Image face = PaperArt.Draw(Root, PaperPiece.Dropdown, PaperTheme.Cream, PaperTheme.Wood, 6f);
            bool art = face.sprite != PaperArt.Round();
            label = PaperTheme.Body(Root, "Value", 10f, 0f, w - (art ? h * 1.3f : 26f), h, "", PaperTheme.BarSize + 1, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (!art) PaperTheme.Body(Root, "Arrow", w - 22f, 0f, 16f, h, "v", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
            UiWinKit.MakeButton(face, () => SetOpen(!IsOpen));

            list = UiKit.Rect(Root, "List", 0f, h + 2f, w, options.Length * RowH + 8f);
            PaperArt.Plate(list, PaperTheme.Cream, PaperTheme.Wood, 6f, true);
            rows = new Image[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                RectTransform row = UiKit.Rect(list, "Row" + i, 4f, 4f + i * RowH, w - 8f, RowH);
                rows[i] = PaperArt.Plate(row, Color.clear, Color.clear, 5f);
                PaperTheme.Body(row, "Text", 8f, 0f, w - 24f, RowH, options[i], PaperTheme.BarSize + 1, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true).horizontalOverflow = HorizontalWrapMode.Overflow;
                UiWinKit.MakeButton(rows[i], () => { SetOpen(false); onPick(index); });
            }
            list.gameObject.SetActive(false);
        }

        /// <summary>The choice shown on the face and marked in the list.</summary>
        public void Show(int index)
        {
            label.text = options[Mathf.Clamp(index, 0, options.Length - 1)];
            for (int i = 0; i < rows.Length; i++) rows[i].color = i == index ? PaperTheme.Gold : Color.clear;
        }

        public void SetOpen(bool on) { if (IsOpen != on) list.gameObject.SetActive(on); }
    }
}
