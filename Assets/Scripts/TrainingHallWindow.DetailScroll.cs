using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// TrainingHallWindow: the scroll on the right, everything the old detail pane told about the selected skill: medallion, name, tier,
    /// unlock level and rank; what it does at this rank (SkillDescribe, generated from the data); MP and cooldown; status effects and
    /// boss exceptions; what the next rank adds; the requirement met or the reason it cannot be learned (SkillTreeActions.Reason); the
    /// Learn / Rank Up and Assign buttons. The text block between the header and the buttons is laid out top-down by its real height
    /// and shrunk as a whole when a long skill would run into the buttons. No per-skill painting: the game has none (the mockup's is
    /// an illustration).
    /// </summary>
    public sealed partial class TrainingHallWindow
    {
        const float ScrollX = 712f, ScrollY = 58f, ScrollW = 418f, ScrollH = 408f, Pad = 32f, FlowTop = 102f, ButtonY = ScrollH - 76f;
        const float FlowW = ScrollW - 2f * Pad;
        // SkillDescribe colours its status tags for the old dark pane; the same tags in inks that read on paper.
        static readonly string[] DarkPaneInk = { "#FF9566", "#79D9FF", "#C8A5FF", "#FFCA70" }, PaperInkOf = { "#B8400E", "#12679C", "#7438B8", "#8A5A00" };

        Image scrollIcon, scrollRing, reasonIcon, reasonPlate;
        Text scrollName, scrollTier, scrollRank, description, mpText, cooldownText, statuses, nextHeader, nextLines, reasonText;
        RectTransform flow, costRow, nextStrip;
        PaperButton learnButton, assignButton;
        readonly StringBuilder lines = new StringBuilder();

        void BuildScroll()
        {
            RectTransform scroll = UiKit.Rect(content, "DetailScroll", ScrollX, ScrollY, ScrollW, ScrollH);
            PaperArt.Draw(scroll, PaperPiece.HallScroll, PaperTheme.Paper, PaperTheme.Wood, 10f, true);
            RectTransform well = UiKit.Rect(scroll, "Medallion", Pad + 8f, 36f + 8f, 48f, 48f);
            PaperArt.Plate(well, PaperSkillParts.Well, Color.clear, 24f);
            well.gameObject.AddComponent<Mask>();
            scrollIcon = UiKit.Fill(well, "Icon").gameObject.AddComponent<Image>();
            scrollIcon.raycastTarget = false;
            scrollRing = PaperArt.Draw(UiKit.Rect(scroll, "Ring", Pad, 36f, 64f, 64f), null, Color.clear, PaperTheme.BtnGoldEdge, 32f);
            float tx = Pad + 74f;
            scrollName = PaperTheme.Title(scroll, "Name", tx, 34f, ScrollW - tx - Pad, 28f, "", 20, PaperTheme.Ink);
            scrollName.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform pill = UiKit.Rect(scroll, "Tier", tx, 66f, 78f, 21f);
            PaperArt.Draw(pill, PaperPiece.ChipActive, PaperTheme.BtnGold, PaperTheme.BtnGoldEdge, 10f);
            scrollTier = PaperTheme.Caption(pill, "", PaperTheme.BarSize + 1, PaperTheme.Ink, PaperInk.Plain);
            scrollRank = PaperTheme.Body(scroll, "Rank", tx + 86f, 66f, ScrollW - tx - 86f - Pad, 21f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);

            flow = UiKit.Rect(scroll, "Flow", Pad, FlowTop, FlowW, 10f);
            description = PaperTheme.Body(flow, "Description", 0f, 0f, FlowW, 20f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.UpperLeft);
            costRow = UiKit.Rect(flow, "Cost", 0f, 0f, FlowW, 20f);
            PaperSkillParts.Pic(costRow, "MpIcon", 0f, 1f, 16f, 18f, PaperPiece.IconMp);
            mpText = PaperTheme.Body(costRow, "Mp", 22f, 0f, 110f, 20f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            PaperSkillParts.Pic(costRow, "CooldownIcon", 130f, 1f, 16f, 18f, PaperPiece.HallIconCooldown);
            cooldownText = PaperTheme.Body(costRow, "Cooldown", 152f, 0f, FlowW - 152f, 20f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            statuses = PaperTheme.Body(flow, "Statuses", 0f, 0f, FlowW, 20f, "", PaperTheme.BarSize + 1, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.UpperLeft, true);
            nextHeader = PaperKit.SectionStrip(flow, "NextRank", -6f, 0f, 190f, 22f, "");
            nextStrip = (RectTransform)nextHeader.transform.parent;
            nextLines = PaperTheme.Body(flow, "NextLines", 6f, 0f, FlowW - 6f, 20f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.UpperLeft, true);
            reasonPlate = PaperArt.Plate(UiKit.Rect(flow, "Reason", 0f, 0f, FlowW, 24f), Color.white, Color.clear, 6f);
            reasonIcon = PaperSkillParts.Pic(reasonPlate.transform, "Icon", 6f, 3f, 18f, 18f, PaperPiece.MarkCheck);
            reasonText = PaperTheme.Body(reasonPlate.transform, "Text", 30f, 4f, FlowW - 38f, 16f, "", PaperTheme.BarSize + 1, PaperTheme.Ink, PaperInk.Plain, TextAnchor.UpperLeft, true);

            float bw = (FlowW - 10f) * 0.5f;
            learnButton = TwoLineButton(scroll, "Learn", Pad, bw, PaperTone.Gold, PaperPiece.HallIconRankUp, OnLearn);
            assignButton = TwoLineButton(scroll, "Assign", Pad + bw + 10f, bw, PaperTone.Blue, PaperPiece.HallIconAssign, OnAssign);
        }

        /// <summary>The scroll's big buttons: an emblem on the left end and a caption of two short lines ("Rank Up" / "(1 SP)").</summary>
        static PaperButton TwoLineButton(Transform parent, string name, float x, float w, PaperTone tone, string icon, System.Action onClick)
        {
            var b = new PaperButton(parent, name, x, ButtonY, w, 46f, "", tone, onClick);
            b.Label.fontSize = PaperTheme.BodySize + 1;
            b.Label.lineSpacing = 0.8f;
            b.Label.rectTransform.offsetMin = new Vector2(34f, 4f);
            PaperSkillParts.Pic(b.Root, "Icon", 14f, 9f, 26f, 26f, icon);
            return b;
        }

        void RefreshScroll(PlayerState p, SkillDef d)
        {
            int r = p.Skills[d.Index];
            bool isMax = r >= SkillDef.MaxRank;
            LearnResult can = p.Skills.CanLearn(d, p.Level);
            string why = SkillTreeActions.Reason(p, d);
            bool locked = r == 0 && (can == LearnResult.LevelTooLow || can == LearnResult.PrereqMissing);
            Sprite sprite = SkillIcons.Get(d);
            scrollIcon.sprite = sprite; scrollIcon.enabled = sprite != null;
            PaperArt.Redraw(scrollRing, PaperPiece.HallRingPrefix + (isMax ? "purple" : r > 0 ? "learned" : locked ? "locked" : "unlearned"), Color.clear, PaperTheme.BtnGoldEdge, 32f);
            scrollName.text = d.Name;
            scrollTier.text = d.Tier.ToString();
            scrollRank.text = "Lv. " + d.UnlockLevel + "   |   " + (r <= 0 ? "Not learned" : "Rank " + r + " / " + SkillDef.MaxRank + (isMax ? "  <color=#7438B8>(MAX)</color>" : ""));

            description.text = SkillDescribe.Description(d, r);
            mpText.text = "MP: " + d.MpAt(Mathf.Max(1, r));
            cooldownText.text = SkillText.Cooldown(d);
            string status = SkillDescribe.StatusLines(d);
            for (int i = 0; i < DarkPaneInk.Length; i++) status = status.Replace(DarkPaneInk[i], PaperInkOf[i]);
            statuses.text = status;
            nextHeader.text = isMax ? "Max Rank" : r <= 0 ? "At Rank 1" : "Next Rank: " + (r + 1) + " / " + SkillDef.MaxRank;
            lines.Length = 0;
            foreach (string line in SkillDescribe.RankLines(d, r)) // "Damage 124% -> 136%": the new value in green
            {
                int arrow = line.IndexOf(" -> ", System.StringComparison.Ordinal);
                if (lines.Length > 0) lines.Append('\n');
                if (arrow < 0) lines.Append(line);
                else lines.Append(line, 0, arrow).Append("  »  <color=#188231>").Append(line, arrow + 4, line.Length - arrow - 4).Append("</color>");
            }
            nextLines.text = lines.ToString();

            bool good = why == null || isMax; // as the old pane: a met requirement and "Max rank" are good news
            string reason = why ?? (d.Prereqs.Length > 0 ? "Prerequisite met: " + SkillTreeActions.PrereqText(d) : "");
            reasonPlate.gameObject.SetActive(reason.Length > 0);
            reasonText.text = reason;
            reasonText.color = good ? PaperTheme.LabelGreen : PaperTheme.Red;
            reasonPlate.color = good ? new Color(0.78f, 0.86f, 0.55f, 0.6f) : new Color(0.95f, 0.66f, 0.55f, 0.55f);
            Sprite mark = PaperArt.Get(good ? PaperPiece.MarkCheck : PaperPiece.HallIconWarning);
            reasonIcon.sprite = mark; reasonIcon.enabled = mark != null;
            LayOutFlow(status.Length > 0, reason.Length > 0);

            string verb = r > 0 ? "Rank Up" : "Learn";
            Enable(learnButton, why == null, PaperTheme.Ink);
            learnButton.Label.text = can == LearnResult.Ok ? verb + "\n(1 SP)" : can == LearnResult.MaxRank ? "Max Rank" : can == LearnResult.NoSp ? verb + "\nNeed 1 SP" : verb + "\nLocked";
            Enable(assignButton, r > 0, PaperTheme.TextLight);
            assignButton.Label.text = assignMode ? "Cancel" : "Assign to\nMagazine";
        }

        /// <summary>A scroll button on / greyed out. The greyed caption stays light with its dark edge (the mockup's "Max Rank", "Need 1 SP"): it carries the reason.</summary>
        static void Enable(PaperButton b, bool on, Color caption)
        {
            b.SetEnabled(on);
            b.Label.color = on ? caption : PaperTheme.TextLight;
            b.Label.GetComponent<Outline>().enabled = !on || caption != PaperTheme.Ink;
        }

        /// <summary>Stacks the text block by the real height of each part; a block taller than the room above the buttons is scaled down to fit.</summary>
        void LayOutFlow(bool hasStatus, bool hasReason)
        {
            float y = Put(description.rectTransform, 0f, description.preferredHeight) + 5f;
            y = Put(costRow, y, 20f) + 3f;
            statuses.gameObject.SetActive(hasStatus);
            if (hasStatus) y = Put(statuses.rectTransform, y, statuses.preferredHeight) + 4f;
            y = Put(nextStrip, y + 2f, 22f) + 3f;
            y = Put(nextLines.rectTransform, y, nextLines.preferredHeight) + 6f;
            if (hasReason)
            {
                float h = reasonText.preferredHeight + 8f;
                reasonPlate.rectTransform.sizeDelta = new Vector2(FlowW, h);
                y = Put(reasonPlate.rectTransform, y, h);
            }
            float scale = Mathf.Min(1f, (ButtonY - 8f - FlowTop) / Mathf.Max(1f, y));
            flow.localScale = new Vector3(scale, scale, 1f);
        }

        static float Put(RectTransform rt, float y, float height)
        {
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -y);
            return y + height;
        }
    }
}
