using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PaperHotbar: the thin EXP line right under the row (hud-compact-pc-v1.png), three looks: gold with "EXP 42.6%", bright yellow
    /// from 95 % ("level up soon"), purple "Lv. 90 MAX" at the level cap. On PC this is the only EXP bar (the status block has none).
    /// </summary>
    public sealed partial class PaperHotbar
    {
        const float ExpBottom = 5f, ExpH = 7f, AlmostShare = 0.95f;
        static readonly Color ExpNormal = PaperTheme.Hex("#F0B429"), ExpAlmost = PaperTheme.Glow, ExpMax = PaperTheme.Purple;

        RectTransform expFill;
        Image expFillImage;
        Text expText;
        int shownExp = -1, shownLevel = -1;

        void BuildExp()
        {
            RectTransform frame = UiWinKit.Anchored(root, "Exp", BottomCentre, BottomCentre, 0f, ExpBottom, Width, ExpH);
            PaperArt.Draw(frame, PaperPiece.CExpFrame, new Color(0.03f, 0.04f, 0.06f, 0.78f), new Color(0f, 0f, 0f, 0f), ExpH * 0.5f);
            expFill = UiKit.Fill(frame, "Fill", 1.5f);
            expFillImage = PaperArt.Draw(expFill, PaperPiece.CExpFill, Color.white, Color.clear, (ExpH - 3f) * 0.5f);
            expText = PaperTheme.Title(root, "ExpText", 0f, Height - ExpBottom - ExpH - 3f, Width, 13f, "", PaperTheme.BarSize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter);
        }

        void TickExp(PlayerState p)
        {
            if (p.Exp == shownExp && p.Level == shownLevel) return;
            shownExp = p.Exp; shownLevel = p.Level;
            bool max = p.Level >= Leveling.MaxLevel;
            float share = max ? 1f : Mathf.Clamp01(Leveling.ProgressRatio(p.Level, p.Exp));
            expFill.anchorMax = new Vector2(share, 1f);
            expFill.offsetMax = new Vector2(share > 0.02f ? -1.5f : 0f, -1.5f);
            expFillImage.color = max ? ExpMax : share >= AlmostShare ? ExpAlmost : ExpNormal;
            expText.text = max ? "Lv. " + p.Level + " MAX" : "EXP " + (share * 100f).ToString("F1", CultureInfo.InvariantCulture) + "%";
        }
    }
}
