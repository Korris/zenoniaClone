using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PetCampWindow, Team: the shelf of the pets the account owns ("Owned Pets (n)" on the hanging sign), ten cards a page between two arrows.
    /// A tap selects a pet and starts placing it (then tap a nest); a card can also be dragged up onto a nest. Species not owned yet are
    /// in the Collection tab, not here.
    /// </summary>
    public sealed partial class PetCampWindow
    {
        const int ShelfCards = 10;
        const float ShelfX = 126f, ShelfY = 524f, ShelfW = 1004f, ShelfH = 112f, CardX = 176f, CardStep = 90f, CardY = 531f;

        readonly PetCampCard[] shelfCards = new PetCampCard[ShelfCards];
        readonly List<int> owned = new List<int>(); // PetConfig indices of the account's pets, in PetConfig order
        Text shelfCount, shelfEmpty;
        GameObject shelfPrev, shelfNext;
        int shelfPage;

        void BuildShelf(RectTransform page)
        {
            PaperKit.Frame(page, "Shelf", ShelfX, ShelfY, ShelfW, ShelfH, PaperFrame.Wood);
            RectTransform sign = UiKit.Rect(page, "ShelfSign", 12f, 522f, 112f, 105f);
            PaperArt.Draw(sign, PaperPiece.PetShelfSign, PaperTheme.WoodDark, PaperTheme.Wood, 8f);
            shelfCount = PaperTheme.Title(sign, "Count", 14f, 42f, 84f, 48f, "", PaperTheme.BodySize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter);
            shelfCount.lineSpacing = 0.85f;
            shelfPrev = PaperKit.PageArrow(page, "ShelfPrev", ShelfX + 8f, ShelfY + 37f, 38f, true, () => TurnShelf(-1)).gameObject;
            shelfNext = PaperKit.PageArrow(page, "ShelfNext", ShelfX + ShelfW - 46f, ShelfY + 37f, 38f, false, () => TurnShelf(1)).gameObject;
            for (int i = 0; i < ShelfCards; i++)
            {
                int index = i;
                shelfCards[i] = PetCampCard.Create(page, "Card" + i, CardX + i * CardStep, CardY, 1f, () => OnShelfTap(index));
                shelfCards[i].Index = i;
                shelfCards[i].Drag = OnCardDrag;
            }
            shelfEmpty = PaperTheme.Title(page, "ShelfEmpty", ShelfX, ShelfY + 36f, ShelfW, 40f, "", PaperTheme.SectionSize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter);
        }

        int ShelfPages { get { return Mathf.Max(1, (owned.Count + ShelfCards - 1) / ShelfCards); } }

        /// <summary>PetConfig index of the pet on shelf card `card` of the page shown, -1 for an empty place.</summary>
        int PetOnCard(int card)
        {
            int at = shelfPage * ShelfCards + card;
            return at < owned.Count ? owned[at] : -1;
        }

        void TurnShelf(int by)
        {
            shelfPage = Mathf.Clamp(shelfPage + by, 0, ShelfPages - 1);
            Refresh();
        }

        void RefreshShelf(PlayerState p, AccountState a)
        {
            owned.Clear();
            if (a != null) for (int i = 0; i < PetConfig.All.Length; i++) if (a.OwnsPet(PetConfig.All[i].Id)) owned.Add(i);
            shelfPage = Mathf.Clamp(shelfPage, 0, ShelfPages - 1);
            shelfCount.text = "Owned Pets\n(" + owned.Count + ")";
            shelfEmpty.text = owned.Count > 0 ? "" : "No pets yet: the Gacha and the Capture tab tell how to get one.";
            shelfPrev.SetActive(shelfPage > 0);
            shelfNext.SetActive(shelfPage < ShelfPages - 1);
            for (int i = 0; i < ShelfCards; i++)
            {
                int pet = PetOnCard(i);
                shelfCards[i].gameObject.SetActive(pet >= 0);
                if (pet < 0) continue;
                PetDef d = PetConfig.All[pet];
                bool sel = pet == selected;
                shelfCards[i].Root.anchoredPosition = new Vector2(CardX + i * CardStep, -(sel ? CardY - 6f : CardY)); // the selected card stands a little higher
                shelfCards[i].Set(d, a.Pet(d.Id), sel, System.Array.IndexOf(p.Team, d.Id));
            }
        }
    }
}
