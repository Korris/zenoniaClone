using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// TrainingHallWindow: the two-tier shelf with the eight statues (tree index 0-3 on the upper board, 4-7 on the lower one, as the
    /// old window's grid) and what the selected skill requires. The mockup drew the requirements as one straight chain; the real ones
    /// (SkillDef.Prereqs) branch and ask different ranks, so they are shown for the selected statue only, without lines over the names:
    ///   - a wooden sign by the selected statue, "Needs" + one line per required skill and rank, green when reached, red when not
    ///     (above a statue of the upper board, below one of the lower board, so it never covers another statue);
    ///   - a small pill "Rank 3" on each statue it requires, in the same colours.
    /// A level lock is not a requirement between skills: the statue's own pill says "Lv. 40".
    /// </summary>
    public sealed partial class TrainingHallWindow
    {
        const float ShelfX = 204f, ShelfY = 104f, ShelfW = 504f, ShelfH = 346f;
        const int Columns = 4;
        const float StatueX = 288f, StatuePitch = 112f;
        // Where the statues stand: the top faces of the two boards of the cut shelf (rows 160 and 306 of its 349 px), in design units.
        static readonly float[] BoardY = { ShelfY + 159f, ShelfY + 304f };
        const float SignW = 172f, SignH = 55f; // the plaque of the cut sign (its two fixed arrows are masked off: real requirements do not sit side by side)

        readonly PaperSkillStatue[] statues = new PaperSkillStatue[SkillDef.PerClass];
        RectTransform sign, signArrow;
        Text signText;
        readonly StringBuilder signLines = new StringBuilder();

        static float StatueCx(int index) { return StatueX + (index % Columns) * StatuePitch; }
        static float StatueBottom(int index) { return BoardY[index / Columns]; }

        void BuildShelves()
        {
            if (PaperArt.Has(PaperPiece.HallShelf)) PaperArt.Draw(UiKit.Rect(content, "Shelf", ShelfX, ShelfY, ShelfW, ShelfH), PaperPiece.HallShelf, PaperTheme.Wood, PaperTheme.WoodDark, 10f);
            else PaperKit.Frame(content, "Shelf", ShelfX, ShelfY, ShelfW, ShelfH, PaperFrame.Wood, false);
            for (int i = 0; i < statues.Length; i++)
            {
                int index = i;
                statues[i] = new PaperSkillStatue(content, "Statue" + i, StatueCx(i), StatueBottom(i), () => OnStatue(index));
                statues[i].Root.gameObject.AddComponent<TrainingHallDrag>().Init(this, true, i);
            }
        }

        void BuildSign()
        {
            sign = UiKit.Rect(content, "RequirementSign", 0f, 0f, SignW, SignH);
            if (PaperArt.Has(PaperPiece.HallSign))
            {
                sign.gameObject.AddComponent<RectMask2D>(); // only the plaque of the 300 x 167 art
                PaperSkillParts.Pic(sign, "Art", 0f, 0f, SignW, SignW * 167f / 300f, PaperPiece.HallSign);
            }
            else PaperArt.Plate(sign, PaperTheme.WoodDark, PaperTheme.Cream, 8f);
            signText = PaperTheme.Body(sign, "Text", 8f, 3f, SignW - 16f, SignH - 8f, "", PaperTheme.BarSize, PaperTheme.TextLight, PaperInk.Plain, TextAnchor.MiddleCenter, true);
            signText.lineSpacing = 0.85f;
            signArrow = PaperSkillParts.Pic(content, "RequirementArrow", 0f, 0f, 18f, 20f, PaperPiece.HallArrow).rectTransform;
            signArrow.pivot = new Vector2(0.5f, 0.5f);
        }

        void RefreshShelves(PlayerState p, SkillDef shown)
        {
            for (int i = 0; i < statues.Length; i++)
            {
                SkillDef d = SkillTable.Get(p.ClassId, i);
                LearnResult r = p.Skills.CanLearn(d, p.Level);
                int rank = p.Skills[i];
                bool locked = rank == 0 && (r == LearnResult.LevelTooLow || r == LearnResult.PrereqMissing); // as the old window's nodes
                SkillStatueLook look = locked ? SkillStatueLook.Locked : rank > 0 ? SkillStatueLook.Learned : r == LearnResult.Ok ? SkillStatueLook.Learnable : SkillStatueLook.Unlearned;
                statues[i].Set(d, rank, look, locked && r == LearnResult.LevelTooLow ? d.UnlockLevel : 0, i == selected, SkillIcons.Get(d));
                statues[i].Need(null, false);
            }
            ShowRequirements(p, shown);
        }

        /// <summary>The sign and the "Rank N" pills of the selected skill's real prerequisites; hidden while a slot is being picked.</summary>
        void ShowRequirements(PlayerState p, SkillDef d)
        {
            bool show = d != null && d.Prereqs.Length > 0 && !assignMode;
            sign.gameObject.SetActive(show);
            signArrow.gameObject.SetActive(show);
            if (!show) return;
            signLines.Length = 0;
            signLines.Append("Needs");
            foreach (SkillReq req in d.Prereqs)
            {
                bool met = p.Skills[req.Index] >= req.Rank;
                statues[req.Index].Need("Rank " + req.Rank, met);
                signLines.Append("\n<color=#").Append(ColorUtility.ToHtmlStringRGB(met ? GoodOnDark : BadOnDark)).Append('>')
                    .Append(SkillTable.Get(d.Class, req.Index).Name).Append(" Rank ").Append(req.Rank).Append("</color>");
            }
            signText.text = signLines.ToString();
            bool upper = d.Index < Columns;
            float cx = StatueCx(d.Index), top = StatueBottom(d.Index) - PaperSkillStatue.H;
            float y = upper ? top - SignH - 10f : StatueBottom(d.Index) + 12f;
            sign.anchoredPosition = new Vector2(cx - SignW * 0.5f, -y);
            signArrow.anchoredPosition = new Vector2(cx, -(upper ? top - 6f : StatueBottom(d.Index) + 8f));
            signArrow.localRotation = Quaternion.Euler(0f, 0f, upper ? 0f : 180f); // the cut arrow points down
        }
    }
}
