using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Right pane of the skill tree window, answering "what does it do, and what do I gain for 1 SP?": icon, name, tier / unlock level, rank; what the
    /// skill does (SkillDescribe, generated from the data); status effects and boss exceptions; MP and cooldown; what the next rank adds;
    /// the requirement or the reason it cannot be learned; the Learn / Rank up and Assign buttons.
    /// </summary>
    public sealed class SkillTreeDetailPane
    {
        public const float Width = 268f, Height = 424f;
        const float Pad = 12f;
        static readonly Color Ok = new Color(0.55f, 0.95f, 0.6f), Bad = new Color(1f, 0.5f, 0.45f), BtnText = new Color(0.25f, 0.12f, 0.02f);

        Image icon;
        Text title, tier, rank, description, statuses, cost, nextRank, reason, learnLabel, assignLabel;
        Button learn, assign;

        public static SkillTreeDetailPane Build(Transform parent, float x, float y, Action onLearn, Action onAssign)
        {
            var d = new SkillTreeDetailPane();
            RectTransform pane = UiKit.Rect(parent, "Detail", x, y, Width, Height);
            UiWinKit.Sliced(pane, "section", 0.4f);
            RectTransform frame = UiKit.Rect(pane, "IconFrame", Pad, 10f, 60f, 60f);
            UiWinKit.Sliced(frame, "slot", 60f / 160f);
            d.icon = UiKit.Fill(frame, "Icon", 6f).gameObject.AddComponent<Image>();
            d.icon.preserveAspect = true;
            float textX = Pad + 68f, textW = Width - textX - Pad, bodyW = Width - 2f * Pad;
            d.title = UiKit.Label(pane, "Name", textX, 8f, textW, 24f, "", 18, UiWinKit.Gold, TextAnchor.MiddleLeft, true);
            d.tier = UiKit.Label(pane, "Tier", textX, 32f, textW, 18f, "", 13, UiWinKit.Dim, TextAnchor.MiddleLeft);
            d.rank = UiKit.Label(pane, "Rank", textX, 50f, textW, 20f, "", 14, Color.white, TextAnchor.MiddleLeft, true);
            d.description = UiKit.Label(pane, "Description", Pad, 76f, bodyW, 68f, "", 14, Color.white, TextAnchor.UpperLeft);
            d.statuses = UiKit.Label(pane, "Statuses", Pad, 146f, bodyW, 34f, "", 13, UiWinKit.Cream, TextAnchor.UpperLeft);
            d.cost = UiKit.Label(pane, "Cost", Pad, 182f, bodyW, 20f, "", 14, UiWinKit.Cream, TextAnchor.MiddleLeft, true);
            d.nextRank = UiKit.Label(pane, "NextRank", Pad, 204f, bodyW, 72f, "", 14, UiWinKit.Cream, TextAnchor.UpperLeft);
            d.reason = UiKit.Label(pane, "Reason", Pad, 278f, bodyW, 34f, "", 13, Bad, TextAnchor.MiddleCenter, true);
            d.learn = UiWinKit.ArtButton(pane, "Learn", "btn-gold", 16f, 314f, Width - 32f, 42f, "Learn", 19, BtnText, onLearn);
            d.assign = UiWinKit.ArtButton(pane, "Assign", "btn-navy", 16f, 364f, Width - 32f, 40f, "Assign to magazine", 17, UiWinKit.Cream, onAssign);
            d.learnLabel = d.learn.GetComponentInChildren<Text>();
            d.assignLabel = d.assign.GetComponentInChildren<Text>();
            return d;
        }

        public void Show(PlayerState p, SkillDef d, bool assignMode, Sprite sprite)
        {
            int r = p.Skills[d.Index];
            icon.sprite = sprite; icon.enabled = sprite != null;
            title.text = d.Name;
            tier.text = d.Tier + ", Lv " + d.UnlockLevel;
            rank.text = r > 0 ? "Rank " + r + " / " + SkillDef.MaxRank : "Not learned";
            description.text = SkillDescribe.Description(d, r);
            statuses.text = SkillDescribe.StatusLines(d);
            cost.text = SkillDescribe.CostLine(d, r);
            nextRank.text = NextRankText(d, r);

            string why = SkillTreeActions.Reason(p, d);
            learn.interactable = why == null;
            learnLabel.text = r > 0 ? "Rank up  (1 SP)" : "Learn  (1 SP)";
            reason.text = why ?? (d.Prereqs.Length > 0 ? "Prerequisite met: " + SkillTreeActions.PrereqText(d) : "");
            reason.color = why == null || r >= SkillDef.MaxRank ? Ok : Bad;
            assign.interactable = r > 0;
            assignLabel.text = assignMode ? "Tap a magazine slot..." : "Assign to magazine";
        }

        static string NextRankText(SkillDef d, int rank)
        {
            var sb = new StringBuilder("<b><color=#E8C25A>" + SkillDescribe.RankHeader(rank) + "</color></b>");
            foreach (string line in SkillDescribe.RankLines(d, rank)) sb.Append('\n').Append(line);
            return sb.ToString();
        }
    }
}
