using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PetCampWindow, Team: the Bond cards (PetRules.Bonds: two species in the team, any nests, give a bonus once). A Bond that is on wears the
    /// gold card and the green ACTIVE tag; the two pets of a Bond that is off are dimmed until they are in the team. A Bond switching on while the
    /// screen is open is announced once ("Bond active: Wild Hunt +2% move speed").
    /// </summary>
    public sealed partial class PetCampWindow
    {
        const float BondX = 184f, BondY = 437f, BondW = 146f, BondH = 84f, BondStep = 150f;

        sealed class BondCard { public Image Face, PetA, PetB; public GameObject Active; public int Shown = -1; }

        readonly BondCard[] bondCards = new BondCard[PetRules.Bonds.Length];
        int bondsOn = -1; // bit per Bond that was active at the last refresh; -1 = not shown yet

        static string BondEffect(PetBond b) { return PetConfig.PassiveName(b.Kind, PetViewModel.Pct(b.Pct).TrimEnd('%')) + (b.BossOnly ? " (bosses only)" : ""); }

        void BuildBonds(RectTransform page)
        {
            RectTransform label = UiKit.Rect(page, "BondLabel", BondX - 4f, BondY - 25f, 76f, 24f);
            PaperArt.Draw(label, PaperPiece.LabelDark, PaperTheme.WoodDark, PaperTheme.Wood, 6f);
            PaperTheme.Caption(label, "Bond", PaperTheme.BodySize, PaperTheme.TextLight).rectTransform.offsetMax = new Vector2(-8f, 0f);
            for (int i = 0; i < bondCards.Length; i++)
            {
                PetBond b = PetRules.Bonds[i];
                var c = new BondCard();
                RectTransform rt = UiKit.Rect(page, "Bond" + i, BondX + i * BondStep, BondY, BondW, BondH);
                c.Face = PaperArt.Draw(rt, null, Color.white, Color.clear);
                PaperTheme.Title(rt, "Name", 0f, 5f, BondW, 18f, b.Name, PaperTheme.BodySize + 1, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter);
                c.PetA = UiKit.Icon(rt, "PetA", BondW * 0.5f - 40f, 23f, 30f);
                c.PetB = UiKit.Icon(rt, "PetB", BondW * 0.5f + 10f, 23f, 30f);
                c.PetA.sprite = PetArt.Icon(PetConfig.Get(b.PetA));
                c.PetB.sprite = PetArt.Icon(PetConfig.Get(b.PetB));
                PaperTheme.Title(rt, "Plus", BondW * 0.5f - 10f, 26f, 20f, 24f, "+", PaperTheme.SectionSize + 2, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter);
                Text effect = PaperTheme.Body(rt, "Effect", 8f, 52f, BondW - 16f, 28f, BondEffect(b), PaperTheme.BarSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
                effect.lineSpacing = 0.85f;
                RectTransform tag = UiKit.Rect(rt, "Active", BondW - 58f, -7f, 56f, 18f);
                PaperArt.Draw(tag, PaperPiece.PetTagActive, PaperTheme.LabelGreen, PaperTheme.TextLight, 8f);
                PaperTheme.Caption(tag, "ACTIVE", PaperTheme.BarSize, PaperTheme.TextLight);
                c.Active = tag.gameObject;
                bondCards[i] = c;
            }
        }

        void RefreshBonds(PlayerState p)
        {
            List<PetBond> active = PetTeam.ActiveBonds(p.Team);
            int on = 0;
            for (int i = 0; i < bondCards.Length; i++)
            {
                PetBond b = PetRules.Bonds[i];
                BondCard c = bondCards[i];
                bool lit = active.Contains(b);
                if (lit) on |= 1 << i;
                if (c.Shown != (lit ? 1 : 0))
                {
                    c.Shown = lit ? 1 : 0;
                    PaperArt.Redraw(c.Face, lit ? PaperPiece.PetBondCardActive : PaperPiece.PetBondCard, lit ? PaperTheme.TitleGold : PaperTheme.Paper, lit ? PaperTheme.BtnGoldEdge : PaperTheme.PaperEdge, 8f);
                    c.Active.SetActive(lit);
                }
                c.PetA.color = BondTint(p, b.PetA);
                c.PetB.color = BondTint(p, b.PetB);
                if (lit && bondsOn >= 0 && (bondsOn & (1 << i)) == 0) RpgHud.Toast("Bond active: " + b.Name + " " + BondEffect(b), new Color(0.75f, 1f, 0.6f));
            }
            bondsOn = on;
        }

        /// <summary>A Bond's pet in its own colours when it is in the team, dimmed when it is not.</summary>
        static Color BondTint(PlayerState p, string petId)
        {
            Color c = PetArt.TintOf(PetConfig.Get(petId));
            if (!PetTeam.Has(p.Team, petId)) c.a = 0.45f;
            return c;
        }
    }
}
