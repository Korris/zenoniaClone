using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// One slot of the Skill Magazine belt in the Training Hall: the assigned skill's icon in a gold frame, or the dark "+" plate when
    /// empty, under a paper tab with the slot's caption ("1" - "8", "Dash" for the fixed first slot). In assign mode the slot wears a
    /// glowing frame that breathes. The whole slot is one tap target; a drag on it is handed to the window by TrainingHallDrag.
    /// </summary>
    public sealed class PaperSkillBeltSlot
    {
        public const float Size = 68f;
        static readonly Color FixedTint = new Color(0.62f, 0.62f, 0.68f, 1f); // Dash: always there, cannot be changed

        public readonly RectTransform Root;
        readonly Image icon, glow;
        readonly GameObject empty, plate, frame;
        bool filled = true, glowing = true;

        public PaperSkillBeltSlot(Transform parent, string name, float x, float y, string caption, Action onClick)
        {
            Root = UiKit.Rect(parent, name, x, y, Size, Size);
            RectTransform e = UiKit.Fill(Root, "Empty");
            PaperArt.Draw(e, PaperPiece.HallSlotEmpty, PaperTheme.Plate, PaperTheme.BtnGoldEdge, 10f);
            empty = e.gameObject;
            RectTransform p = UiKit.Fill(Root, "Plate"); // the frame's window: masks the icon
            PaperArt.Draw(p, PaperPiece.HallSlotPlate, PaperSkillParts.Well, Color.clear, 10f).color = PaperSkillParts.Well;
            p.gameObject.AddComponent<Mask>();
            plate = p.gameObject;
            icon = UiKit.Fill(p, "Icon", 5f).gameObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            RectTransform f = UiKit.Fill(Root, "Frame");
            PaperArt.Draw(f, PaperPiece.HallSlotFrame, Color.clear, PaperTheme.BtnGoldEdge, 10f);
            frame = f.gameObject;
            glow = PaperArt.Draw(UiKit.Fill(Root, "Assign", -4f), PaperPiece.HallSlotAssign, Color.clear, PaperTheme.Glow, 12f);
            float tabW = caption.Length > 1 ? 44f : 34f;
            RectTransform tab = UiKit.Rect(Root, "Tab", (Size - tabW) * 0.5f, -13f, tabW, 20f);
            PaperArt.Draw(tab, PaperPiece.HallSlotTab, PaperTheme.Paper, PaperTheme.Wood, 5f);
            PaperTheme.Caption(tab, caption, PaperTheme.BarSize + 1, PaperTheme.Ink, PaperInk.Plain);
            Image hit = UiKit.Fill(Root, "Hit").gameObject.AddComponent<Image>(); // a child, so a drag on the slot (handler on Root) cancels the tap
            hit.color = Color.clear;
            UiWinKit.MakeButton(hit, onClick);
            Set(null, false, false);
        }

        /// <summary>sprite null = empty slot; `fixedSlot` dims the icon (Dash); `assign` = the glowing frame of assign mode.</summary>
        public void Set(Sprite sprite, bool fixedSlot, bool assign)
        {
            bool has = sprite != null;
            if (has != filled)
            {
                filled = has;
                empty.SetActive(!has); plate.SetActive(has); frame.SetActive(has);
            }
            if (icon.sprite != sprite) icon.sprite = sprite;
            icon.color = fixedSlot ? FixedTint : Color.white;
            if (assign != glowing) { glowing = assign; glow.gameObject.SetActive(assign); }
        }

        /// <summary>Breathing of the assign frame (time in seconds).</summary>
        public void Pulse(float t)
        {
            if (glowing) glow.color = new Color(1f, 1f, 1f, 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(t * 4f)));
        }
    }
}
