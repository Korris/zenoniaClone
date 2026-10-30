using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Right pane of the skill tree window: big icon, name, tier / unlock level, rank, prerequisites (red when unmet), MP / cooldown / per-rank
    /// effect lines (SkillText: current -> next rank), the Learn / Rank up button (disabled with the reason printed under it) and Assign.
    /// </summary>
    public sealed class SkillTreeDetailPane
    {
        public const float Width = 268f, Height = 424f;
        static readonly Color Ok = new Color(0.55f, 0.95f, 0.6f), Bad = new Color(1f, 0.5f, 0.45f), BtnText = new Color(0.25f, 0.12f, 0.02f);

        Image icon;
        Text title, tier, rank, prereq, stats, reason, learnLabel, assignLabel;
        Button learn, assign;

        public static SkillTreeDetailPane Build(Transform parent, float x, float y, Action onLearn, Action onAssign)
        {
            var d = new SkillTreeDetailPane();
            RectTransform pane = UiKit.Rect(parent, "Detail", x, y, Width, Height);
            UiWinKit.Sliced(pane, "section", 0.4f);
            RectTransform frame = UiKit.Rect(pane, "IconFrame", 16f, 16f, 88f, 88f);
            UiWinKit.Sliced(frame, "slot", 88f / 160f);
            d.icon = UiKit.Fill(frame, "Icon", 8f).gameObject.AddComponent<Image>();
            d.icon.preserveAspect = true;
            d.title = UiKit.Label(pane, "Name", 114f, 14f, 140f, 56f, "", 21, UiWinKit.Gold, TextAnchor.MiddleLeft, true);
            d.tier = UiKit.Label(pane, "Tier", 114f, 72f, 146f, 34f, "", 13, UiWinKit.Dim, TextAnchor.UpperLeft);
            d.rank = UiKit.Label(pane, "Rank", 16f, 112f, Width - 32f, 22f, "", 16, Color.white, TextAnchor.MiddleLeft, true);
            d.prereq = UiKit.Label(pane, "Prereq", 16f, 136f, Width - 32f, 36f, "", 13, Ok, TextAnchor.UpperLeft);
            d.stats = UiKit.Label(pane, "Stats", 16f, 176f, Width - 32f, 110f, "", 14, UiWinKit.Cream, TextAnchor.UpperLeft);
            d.reason = UiKit.Label(pane, "Reason", 16f, 288f, Width - 32f, 20f, "", 13, Bad, TextAnchor.MiddleCenter, true);
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
            tier.text = d.Tier + " skill, unlocks at Lv " + d.UnlockLevel;
            rank.text = r > 0 ? "Rank " + r + " / " + SkillDef.MaxRank : "Not learned";
            bool met = d.Prereqs.Length == 0 || Met(p, d);
            prereq.text = d.Prereqs.Length == 0 ? "No prerequisites" : (met ? "Prerequisite met: " : "Needs ") + SkillTreeActions.PrereqText(d);
            prereq.color = met ? Ok : Bad;
            stats.text = StatsText(d, r);

            string why = SkillTreeActions.Reason(p, d);
            learn.interactable = why == null;
            learnLabel.text = r > 0 ? "Rank up  (1 SP)" : "Learn  (1 SP)";
            reason.text = why ?? "";
            assign.interactable = r > 0;
            assignLabel.text = assignMode ? "Tap a magazine slot..." : "Assign to magazine";
        }

        static bool Met(PlayerState p, SkillDef d)
        {
            foreach (SkillReq q in d.Prereqs) if (p.Skills[q.Index] < q.Rank) return false;
            return true;
        }

        static string StatsText(SkillDef d, int rank)
        {
            var sb = new StringBuilder();
            foreach (string line in SkillText.RankLines(d, rank)) sb.Append(line).Append('\n');
            sb.Append(SkillText.Cooldown(d));
            if (d.Legacy != LegacySkill.None) sb.Append("\nSignature skill, effect as in the original");
            return sb.ToString();
        }
    }
}
