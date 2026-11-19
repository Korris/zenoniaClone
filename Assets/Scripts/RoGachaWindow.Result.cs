using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using W = ZenoniaProto.InventoryRoWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// RoGachaWindow: what a pull gave, read back from the account history (the same offline and online). A veil over the counter and a
    /// RO Soft card: "Pull Result" ribbon; x1 = the pet on the pedestal with rarity, name and NEW / the shards a duplicate gave (or the
    /// shared shards); x10 = ten cards turning up one after the other (Skip shows all), SSR and UR with the gold glow, and the sum.
    /// Pull again (same count), OK (J / Space) and the pulls left to the two guarantees.
    /// </summary>
    public sealed partial class RoGachaWindow
    {
        const float RevealStep = 0.12f, CardSize = 92f, CardPitchX = 112f, CardPitchY = 150f;
        const float RX = 230f, RY = 64f, RW = 700f, RH = 540f; // the result card in window units

        readonly List<PullRecord> results = new List<PullRecord>();
        readonly W.Cell[] cards = new W.Cell[PetRules.MaxPullsAtOnce];
        readonly InventoryRoBoxes.PillBox[] cardPills = new InventoryRoBoxes.PillBox[PetRules.MaxPullsAtOnce];
        readonly Text[] cardNames = new Text[PetRules.MaxPullsAtOnce], cardTags = new Text[PetRules.MaxPullsAtOnce];
        GameObject resultRoot, oneRoot, tenRoot;
        Image onePet, oneCrest;
        InventoryRoBoxes.PillBox onePill;
        Text oneName, oneLine, sumText, resultPity;
        InventoryRoBoxes.IconButton again, skip;
        int resultCount, revealed;
        float nextReveal;

        void BuildResult()
        {
            RectTransform root = UiKit.Rect(window, "Result", 0f, 0f, WinW, WinH);
            resultRoot = root.gameObject;
            Image veil = UiKit.Panel(UiKit.Rect(root, "Veil", 6f, 6f, WinW - 12f, WinH - 12f), new Color(0.2f, 0.24f, 0.36f, 0.45f), true);
            veil.sprite = W.Art("bag-section"); veil.type = Image.Type.Sliced;
            RectTransform card = W.Img(root, "Card", RX, RY, RW, RH, "bag-window", true).rectTransform;
            Image ribbon = W.Img(card, "Ribbon", RW * 0.5f - 170f, -30f, 340f, 72f, "gacha-ribbon");
            Text title = W.Txt(ribbon.transform, "Title", 0f, 8f, 340f, 46f, "Pull Result", 26, Color.white, TextAnchor.MiddleCenter);
            Outline o = title.gameObject.AddComponent<Outline>(); o.effectColor = RoSoftTheme.Hex("#5B3F9E"); o.effectDistance = new Vector2(1.5f, -1.5f);

            RectTransform one = UiKit.Rect(card, "One", 0f, 0f, RW, RH);
            oneRoot = one.gameObject;
            W.Img(one, "Stage", RW * 0.5f - 200f, 60f, 400f, 340f, "gacha-stage");
            onePet = UiKit.Rect(one, "Pet", RW * 0.5f - 150f, 120f, 300f, 220f).gameObject.AddComponent<Image>();
            onePet.preserveAspect = true; onePet.raycastTarget = false;
            Image plate = W.Img(one, "NamePlate", RW * 0.5f - 110f, 64f, 250f, 42f, "bag-pill-white");
            plate.pixelsPerUnitMultiplier = plate.sprite != null ? plate.sprite.rect.height / 84f : 1f;
            oneName = W.Txt(plate.transform, "Name", 40f, 0f, 200f, 42f, "", 20, W.Ink, TextAnchor.MiddleCenter);
            oneCrest = W.Img(one, "Crest", RW * 0.5f - 160f, 50f, 88f, 70f, "gacha-ur-crest");
            Text ur = W.Txt(oneCrest.transform, "UR", 0f, 18f, 88f, 32f, "UR", 17, Color.white, TextAnchor.MiddleCenter);
            Outline uo = ur.gameObject.AddComponent<Outline>(); uo.effectColor = RoSoftTheme.Hex("#7A2E8F");
            onePill = RarityPill(one, "Rarity", RW * 0.5f - 140f, 72f, 28f, PetRarity.N, 15, 52f);
            oneLine = W.Txt(one, "Line", 0f, 400f, RW, 30f, "", 18, W.Ink, TextAnchor.MiddleCenter);

            RectTransform ten = UiKit.Rect(card, "Ten", 0f, 0f, RW, RH);
            tenRoot = ten.gameObject;
            float x0 = (RW - (4f * CardPitchX + CardSize)) * 0.5f;
            for (int i = 0; i < cards.Length; i++)
            {
                float x = x0 + (i % 5) * CardPitchX, y = 66f + (i / 5) * CardPitchY;
                cards[i] = new W.Cell(ten, "Card" + i, x, y, CardSize, null);
                cardPills[i] = RarityPill(cards[i].Root, "Rarity", -3f, -4f, 18f, PetRarity.N, 11, 26f);
                cardNames[i] = W.Txt(cards[i].Root, "Name", -10f, CardSize + 2f, CardSize + 20f, 18f, "", 12, W.Ink, TextAnchor.UpperCenter);
                cardTags[i] = W.Txt(cards[i].Root, "Tag", -10f, CardSize + 19f, CardSize + 20f, 16f, "", 11, W.Good, TextAnchor.UpperCenter);
            }
            sumText = W.Txt(ten, "Sum", 0f, 372f, RW, 28f, "", 15, W.Ink, TextAnchor.MiddleCenter);

            again = new InventoryRoBoxes.IconButton(card, "Again", RW * 0.5f - 230f, 440f, 220f, 56f, "btn-mint", "gacha-coin", "Pull again", 17, () => Pull(resultCount));
            new InventoryRoBoxes.IconButton(card, "Ok", RW * 0.5f + 10f, 440f, 220f, 56f, "btn-sky", null, "OK", 18, CloseResult);
            skip = new InventoryRoBoxes.IconButton(card, "Skip", RW - 130f, 372f, 110f, 40f, "btn-ivory", null, "Skip", 14, RevealAll);
            skip.Label.color = W.Ink;
            resultPity = W.Txt(card, "Pity", 0f, 500f, RW, 26f, "", 13, W.InkSoft, TextAnchor.MiddleCenter, false);
            resultRoot.SetActive(false);
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
            popup = Popup.None;
            overlay.SetAsLastSibling(); // over the pull summary toast, which says the same as this card
        }

        void CloseResult() { resultCount = 0; ToastsOnTop(); Refresh(); }

        void RevealAll() { revealed = resultCount; Refresh(); }

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
                if (d == null || d.Rarity != PetRarity.UR) urShards++; // every pull that is not a UR gives a UR shard
            }
            if (single) { ShowOne(results[0]); skip.Set(false); }
            else
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    bool show = i < revealed && i < results.Count;
                    cards[i].Root.gameObject.SetActive(show);
                    if (show) ShowCard(i, results[i]);
                }
                sumText.text = fresh + (fresh == 1 ? " new pet" : " new pets") + (kind > 0 ? "  ·  +" + kind + " pet shards" : "") + (shared > 0 ? "  ·  +" + shared + " shared shards" : "") + "  ·  +" + urShards + " UR shards";
                skip.Set(revealed < resultCount);
                bubbleText.text = fresh > 0 ? fresh + (fresh == 1 ? " new friend!" : " new friends!") : "No new friend this time. The shards still count!";
            }
            again.Set(true, "Pull again x" + resultCount, a.tickets < resultCount || pendingCount > 0);
            W.SetArt(again.Root.GetComponent<Image>(), single ? "btn-mint" : "btn-gold");
            resultPity.text = "SSR+ guaranteed in " + PetGacha.PullsToSsr(a) + " pulls  ·  UR guaranteed in " + PetGacha.PullsToUr(a) + " pulls  ·  Coins " + a.tickets;
        }

        void ShowOne(PullRecord r)
        {
            PetDef d = PetConfig.Get(r.id);
            Texture2D tex = d != null ? Resources.Load<Texture2D>("Sprites/pets/" + d.ArtId + "/art") : null;
            onePet.sprite = tex != null ? StageSprite(d.ArtId, tex) : d != null ? PetArt.Icon(d) : PetArt.ShardIcon();
            onePet.enabled = onePet.sprite != null;
            float pw = d != null ? 300f : 110f, ph = d != null ? 220f : 110f; // the shard picture is a small icon: never blown up
            onePet.rectTransform.anchoredPosition = new Vector2(RW * 0.5f - pw * 0.5f, -(d != null ? 120f : 220f));
            onePet.rectTransform.sizeDelta = new Vector2(pw, ph);
            oneName.text = d != null ? d.Name : "Shared Shards";
            oneCrest.gameObject.SetActive(d != null && d.Rarity == PetRarity.UR);
            onePill.Root.gameObject.SetActive(d != null && d.Rarity != PetRarity.UR);
            if (d != null) SetRarity(onePill, d.Rarity);
            oneLine.text = d == null ? "+" + r.shards + " shared shards (feed any pet you own)" : r.dup ? "Already yours:  +" + r.shards + " " + d.Name + " shards" : "<color=#2E9E57>NEW!</color>  " + d.Name + " joined your pets";
            bubbleText.text = d == null ? "Shards this time. They feed any pet you own." : r.dup ? "An old friend! Its shards raise its stars." : "Wow, a new friend!";
        }

        void ShowCard(int i, PullRecord r)
        {
            PetDef d = PetConfig.Get(r.id);
            bool rare = d != null && d.Rarity >= PetRarity.SSR;
            if (d != null) PetCard(cards[i], d.Rarity, rare); else cards[i].SetState(false, false, Color.white);
            cards[i].SetIcon(d != null ? PetArt.Icon(d) : PetArt.ShardIcon(), Color.white, d != null ? 0.9f : 0.6f);
            cardPills[i].Root.gameObject.SetActive(d != null);
            if (d != null) SetRarity(cardPills[i], d.Rarity);
            cardNames[i].text = d != null ? d.Name : "Shared shards";
            cardTags[i].text = d == null ? "<color=#4F94D6>+" + r.shards + "</color>" : r.dup ? "<color=#B9811C>+" + r.shards + " shards</color>" : "NEW!";
        }
    }
}
