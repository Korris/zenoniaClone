using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// TrainingHallWindow: the Respec question ("Reset all skills?", state 2 of the states mockup), in place of the old window's
    /// "tap Respec again to confirm". It shows what the reset really does: every spent point comes back (SkillRanks.Sum), the price
    /// is SkillRespec.Cost(level) (red when the hero cannot pay; the rule itself still answers), all ranks return to 0 and the magazine
    /// is cleared. Respec asks ISkillTreeActions.Respec, exactly as the old second tap did.
    /// </summary>
    public sealed partial class TrainingHallWindow
    {
        GameObject respecPopup;
        Text refundText, costText;

        void BuildRespec()
        {
            RectTransform veil = UiKit.Rect(content, "RespecQuestion", -1200f, -700f, PaperLayout.DesignWidth + 2400f, PaperLayout.DesignHeight + 1400f); // beyond the design frame: wide screens too
            UiKit.Panel(veil, new Color(0f, 0f, 0f, 0.55f), true);
            respecPopup = veil.gameObject;
            const float w = 348f, h = 268f;
            RectTransform frame = PaperKit.Frame(veil, "Paper", 1200f + (PaperLayout.DesignWidth - w) * 0.5f, 700f + (PaperLayout.DesignHeight - h) * 0.5f, w, h, PaperFrame.Paper);
            RectTransform inner = PaperKit.Inner(frame, PaperFrame.Paper);
            float iw = w - 48f;
            PaperTheme.Title(inner, "Title", 0f, 0f, iw, 28f, "Reset all skills?", 19, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter);
            PaperSkillParts.Pic(inner, "SpIcon", 62f, 36f, 24f, 24f, PaperPiece.HallIconSp);
            refundText = PaperTheme.Body(inner, "Refund", 94f, 36f, iw - 100f, 24f, "", PaperTheme.SectionSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            RectTransform cost = UiKit.Rect(inner, "Cost", 40f, 68f, iw - 80f, 28f);
            PaperArt.Plate(cost, PaperTheme.Cream, PaperTheme.PaperEdge, 12f);
            PaperTheme.Body(cost, "Label", 16f, 0f, 60f, 28f, "Cost:", PaperTheme.SectionSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            UiKit.Icon(cost, "Coin", 72f, 3f, 22f).sprite = LootSprites.Coin();
            costText = PaperTheme.Body(cost, "Value", 100f, 0f, iw - 190f, 28f, "", PaperTheme.SectionSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            PaperSkillParts.Pic(inner, "Warning", 34f, 108f, 34f, 30f, PaperPiece.HallIconWarning);
            Text warn = PaperTheme.Body(inner, "WarningText", 78f, 104f, iw - 84f, 38f, "All ranks return to 0.\nMagazine is cleared.", PaperTheme.BodySize, PaperTheme.Red, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            warn.lineSpacing = 0.9f;
            float bw = (iw - 12f) * 0.5f;
            new PaperButton(inner, "Respec", 0f, 168f, bw, 44f, "Respec", PaperTone.Purple, ConfirmRespec);
            new PaperButton(inner, "Cancel", bw + 12f, 168f, bw, 44f, "Cancel", PaperTone.Blue, () => respecPopup.SetActive(false));
            respecPopup.SetActive(false);
        }

        void RefreshRespec(PlayerState p)
        {
            int cost = SkillRespec.Cost(p.Level);
            refundText.text = "Refund " + p.Skills.Sum + " SP";
            costText.text = cost.ToString("N0", CultureInfo.InvariantCulture) + " G";
            costText.color = p.Gold < cost ? PaperTheme.Red : PaperTheme.Ink;
            if (p.Skills.Sum == 0) respecPopup.SetActive(false); // nothing left to reset (the reset just happened, or the server took the ranks)
        }

        /// <summary>The header's Respec button: asks first.</summary>
        void OnRespec()
        {
            PlayerState p = RpgSession.Player;
            if (p == null || p.Skills.Sum == 0) return;
            if (respecPopup.activeSelf) { ConfirmRespec(); return; } // a script's second "respec" step: the old window's second tap
            CancelDrag();
            assignMode = false;
            Refresh();
            respecPopup.transform.SetAsLastSibling();
            respecPopup.SetActive(true);
        }

        void ConfirmRespec()
        {
            respecPopup.SetActive(false);
            Say(SkillTreeActions.Current.Respec());
            Refresh();
        }
    }
}
