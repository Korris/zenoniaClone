using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// LumiCounterWindow: what a pull gave (pet-gacha-result-states mockup, states 1 and 2), read back from the account's history so it is
    /// the same offline and online. x1: the pet over the broken shell on the dais (or the shard), its rarity and name, NEW / the shards a
    /// duplicate gave. x10: ten cards that turn up one after the other (Skip shows them all), and the sum of the pull. Both: Pull again,
    /// OK and the pulls left to the two guarantees. The counter layer is hidden meanwhile; Lumi stays, the dais and the nests only under a x1 result.
    /// </summary>
    public sealed partial class LumiCounterWindow
    {
        const float RevealStep = 0.12f;

        readonly List<PullRecord> results = new List<PullRecord>();
        readonly PaperGachaCard[] cards = new PaperGachaCard[PetRules.MaxPullsAtOnce];
        GameObject resultRoot, oneRoot, tenRoot, oneNew, skip;
        Image oneRarity, onePet;
        Text oneName, oneLine, tenSum, resultPity;
        PaperGachaButton again1, again10;
        RectTransform okButton, pityPlate;
        int resultCount, revealed;
        float nextReveal;

        void BuildResult()
        {
            RectTransform root = UiKit.Rect(content, "Result", 0f, 0f, PaperLayout.DesignWidth, PaperLayout.DesignHeight);
            resultRoot = root.gameObject;
            RectTransform one = UiKit.Rect(root, "One", 0f, 0f, PaperLayout.DesignWidth, PaperLayout.DesignHeight);
            oneRoot = one.gameObject;
            Art(one, "Starburst", PaperPiece.GachaStarburst, 306f, 136f, 260f, 238f); // grey halo on light grounds: only over the dark stall
            Art(one, "Shell", PaperPiece.GachaEggshell, 376f, 314f, 120f, 58f);
            onePet = UiKit.Icon(one, "Pet", 366f, 186f, 140f);
            RectTransform name = Plate(one, "Name", 296f, 92f, 280f, 40f);
            oneRarity = UiKit.Icon(name, "Rarity", 10f, -4f, 48f);
            oneName = PaperTheme.Title(name, "Text", 60f, 0f, 210f, 40f, "", PaperTheme.TitleSize - 3, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter);
            RectTransform fresh = Art(one, "New", PaperPiece.GachaRibbonNew, 376f, 134f, 120f, 40f);
            PaperTheme.Caption(fresh, "NEW!", PaperTheme.SectionSize, PaperTheme.TextLight).rectTransform.offsetMin = new Vector2(0f, 12f);
            oneNew = fresh.gameObject;
            oneLine = PaperTheme.Title(Plate(one, "Line", 286f, 378f, 300f, 32f), "Text", 0f, 0f, 300f, 32f, "", PaperTheme.SectionSize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter);

            RectTransform ten = UiKit.Rect(root, "Ten", 0f, 0f, PaperLayout.DesignWidth, PaperLayout.DesignHeight);
            tenRoot = ten.gameObject;
            for (int i = 0; i < cards.Length; i++) cards[i] = new PaperGachaCard(ten, "Card" + i, 414f + (i % 5) * 114f, 96f + (i / 5) * 126f);
            tenSum = PaperTheme.Title(Plate(ten, "Sum", 414f, 350f, 560f, 32f), "Text", 0f, 0f, 560f, 32f, "", PaperTheme.SectionSize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter);
            skip = new PaperGachaButton(ten, "Skip", 414f, 406f, 90f, 40f, PaperPiece.GachaBtnSlate, PaperTheme.Slate, PaperTheme.PlateEdge, PaperTheme.TextLight, "▶▶ Skip", RevealAll).Root.gameObject;

            again1 = new PaperGachaButton(one, "Again", 266f, 422f, 170f, 66f, PaperPiece.GachaBtnPullBlue, PaperTheme.BtnBlue, PaperTheme.BtnBlueEdge, PaperTheme.TextLight, "Pull again x1", () => Pull(1), PaperPiece.IconGachaCoin);
            again10 = new PaperGachaButton(ten, "Again", 529f, 396f, 170f, 66f, PaperPiece.GachaBtnPullPurple, PaperTheme.Purple, PaperTheme.BtnPurpleEdge, PaperTheme.TextLight, "Pull again x" + PetRules.MaxPullsAtOnce, () => Pull(PetRules.MaxPullsAtOnce), PaperPiece.IconGachaCoin);
            again1.SetSub("x1");
            again10.SetSub("x" + PetRules.MaxPullsAtOnce);
            okButton = new PaperGachaButton(root, "Ok", 0f, 0f, 160f, 62f, PaperPiece.GachaBtnBuy, PaperTheme.BtnGold, PaperTheme.BtnGoldEdge, PaperTheme.Ink, "OK", CloseResult).Root;
            pityPlate = Plate(root, "Pity", 0f, 0f, 300f, 26f);
            resultPity = PaperTheme.Body(pityPlate, "Text", 0f, 0f, 300f, 26f, "", PaperTheme.BodySize, PaperTheme.TitleGold, PaperInk.World, TextAnchor.MiddleCenter, true);
            resultRoot.SetActive(false);
        }

        /// <summary>The dark plate of the result mockup (its laurel piece is not cut: flat).</summary>
        static RectTransform Plate(Transform parent, string name, float x, float y, float w, float h)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, h);
            PaperArt.Draw(rt, PaperPiece.GachaResultPlate, new Color(PaperTheme.Plate.r, PaperTheme.Plate.g, PaperTheme.Plate.b, 0.94f), PaperTheme.BtnGoldEdge, h * 0.4f);
            return rt;
        }

        /// <summary>The last `count` pulls of the history are the result (a copy: the history may move on while it shows).</summary>
        void ShowResult(AccountState a, int count)
        {
            count = Mathf.Min(count, a.history.Count);
            if (count <= 0) return;
            results.Clear();
            for (int i = a.history.Count - count; i < a.history.Count; i++) results.Add(a.history[i]);
            resultCount = count;
            revealed = count == 1 ? 1 : 0;
            nextReveal = Time.unscaledTime + RevealStep;
            dialog = Dialog.None;
        }

        void CloseResult() { resultCount = 0; }

        void RevealAll() { revealed = resultCount; }

        void TickReveal()
        {
            if (revealed >= resultCount || Time.unscaledTime < nextReveal) return;
            revealed++;
            nextReveal = Time.unscaledTime + RevealStep;
        }

        void RefreshResult(AccountState a)
        {
            bool on = resultCount > 0, single = resultCount == 1;
            resultRoot.SetActive(on);
            if (!on) return;
            oneRoot.SetActive(single);
            tenRoot.SetActive(!single);
            int fresh = 0, kind = 0, shared = 0, urShards = 0;
            foreach (PullRecord r in results)
            {
                PetDef d = PetConfig.Get(r.id);
                if (d == null) shared += r.shards; else if (r.dup) kind += r.shards; else fresh++;
                if (d == null || d.Rarity != PetRarity.UR) urShards++; // every pull that is not a UR gave a UR shard
            }
            if (single) ShowOne(results[0]);
            else
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    bool show = i < revealed && i < results.Count;
                    cards[i].Root.gameObject.SetActive(show);
                    if (!show) continue;
                    OwnedPet o = a.Pet(results[i].id);
                    cards[i].Set(results[i], o != null ? o.stars : 0);
                }
                tenSum.text = fresh + (fresh == 1 ? " new pet" : " new pets") + (kind > 0 ? "  ·  +" + kind + " pet shards" : "") + (shared > 0 ? "  ·  +" + shared + " shared shards" : "") + "  ·  +" + urShards + " UR shards";
                skip.SetActive(revealed < resultCount);
                bubbleText.text = fresh > 0 ? fresh + (fresh == 1 ? " new friend!" : " new friends!") : "No new friend this time. The shards still count!";
            }
            PaperGachaButton again = single ? again1 : again10; // OK and the guarantee line sit beside / under it
            Vector2 at = again.Root.anchoredPosition;
            okButton.anchoredPosition = at + new Vector2(178f, -2f);
            pityPlate.anchoredPosition = at + new Vector2(19f, -74f);
            again.SetLit(a.tickets >= resultCount && pendingCount == 0);
            resultPity.text = "SSR+ in " + PetGacha.PullsToSsr(a) + " pulls  ·  UR in " + PetGacha.PullsToUr(a) + " pulls";
        }

        void ShowOne(PullRecord r)
        {
            PetDef d = PetConfig.Get(r.id);
            onePet.sprite = d != null ? PetArt.Icon(d) : PaperArt.Get(PaperPiece.IconShard);
            onePet.color = d != null ? PetArt.TintOf(d) : Color.white;
            onePet.enabled = onePet.sprite != null;
            ShowRarity(oneRarity, d);
            oneName.text = d != null ? d.Name : "Shared Shards";
            oneNew.SetActive(d != null && !r.dup);
            oneLine.text = d == null ? "+" + r.shards + " shared shards" : r.dup ? "Already yours:  +" + r.shards + " " + d.Name + " shards" : "Joined your collection";
            bubbleText.text = d == null ? "Shards this time. They feed any pet you own." : r.dup ? "An old friend! Its shards raise its stars." : "Wow, a new friend!";
        }
    }
}
