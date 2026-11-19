using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using W = ZenoniaProto.InventoryRoWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// RoGachaWindow, centre: "Gacha Pet" ribbon, "Permanent event" (the pet gacha has no end date), the previewed pet on its glowing pedestal
    /// with its rarity (UR crest, else a rarity pill) and name, a carousel of every pet of this gacha (5 a page, arrows and page dots; a tap
    /// previews it, the UR shard target is shown first), and Pull x1 / Pull x10 (cost in Gacha Coins).
    /// </summary>
    public sealed partial class RoGachaWindow
    {
        const int CarouselSize = 5;
        static readonly List<PetDef> GachaPets = new List<PetDef>();

        PetDef preview;
        int carouselPage;
        Image stagePet, urCrest;
        Text previewName;
        InventoryRoBoxes.PillBox previewPill;
        readonly W.Cell[] carousel = new W.Cell[CarouselSize];
        readonly InventoryRoBoxes.PillBox[] carouselPills = new InventoryRoBoxes.PillBox[CarouselSize];
        readonly List<Image> dots = new List<Image>();
        Image arrowLeft, arrowRight;
        CanvasGroup pull1Group, pull10Group;

        /// <summary>The pets this gacha can give, rarest first (PetConfig order inside a rarity).</summary>
        static List<PetDef> Pets()
        {
            if (GachaPets.Count > 0) return GachaPets;
            for (int r = (int)PetRarity.UR; r >= 0; r--)
                foreach (PetDef d in PetConfig.All) if (!d.Legacy && (int)d.Rarity == r) GachaPets.Add(d);
            return GachaPets;
        }

        void BuildCentre()
        {
            Image ribbon = W.Img(window, "Ribbon", X(570f), Y(88f), D(502f), D(108f), "gacha-ribbon");
            RectTransform head = UiKit.Rect(ribbon.transform, "Head", 0f, D(16f), D(502f), D(64f));
            var row = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter; row.spacing = 8f;
            row.childControlWidth = row.childControlHeight = false; row.childForceExpandWidth = row.childForceExpandHeight = false;
            Image paw = W.Img(head, "Paw", 0f, 0f, D(52f), D(52f), "gacha-ico-paw");
            Text title = W.Txt(head, "Title", 0f, 0f, 10f, D(60f), "Gacha Pet", 32, Color.white, TextAnchor.MiddleCenter);
            title.rectTransform.sizeDelta = new Vector2(title.preferredWidth + 4f, D(60f));
            Outline o = title.gameObject.AddComponent<Outline>();
            o.effectColor = RoSoftTheme.Hex("#5B3F9E"); o.effectDistance = new Vector2(1.6f, -1.6f);

            W.Img(window, "Stage", X(548f), Y(174f), D(556f), D(470f), "gacha-stage");
            stagePet = UiKit.Rect(window, "Pet", X(584f), Y(244f), D(476f), D(340f)).gameObject.AddComponent<Image>();
            stagePet.preserveAspect = true;
            stagePet.raycastTarget = false;

            Image timer = W.Img(window, "Timer", X(712f), Y(184f), D(226f), D(32f), "bag-pill-white");
            timer.color = new Color(1f, 1f, 1f, 0.75f);
            timer.pixelsPerUnitMultiplier = timer.sprite != null ? timer.sprite.rect.height / (2f * D(32f)) : 1f;
            W.Img(timer.transform, "Clock", D(8f), D(2f), D(28f), D(28f), "wanted-icon-clock");
            W.Txt(timer.transform, "Text", D(42f), 0f, D(180f), D(32f), "Permanent event", 14, W.Ink);

            Image plate = W.Img(window, "NamePlate", X(700f), Y(238f), D(286f), D(54f), "bag-pill-white");
            plate.pixelsPerUnitMultiplier = plate.sprite != null ? plate.sprite.rect.height / (2f * D(54f)) : 1f;
            previewName = W.Txt(plate.transform, "Name", D(52f), 0f, D(226f), D(54f), "", 21, W.Ink, TextAnchor.MiddleCenter);
            urCrest = W.Img(window, "UrCrest", X(612f), Y(204f), D(138f), D(110f), "gacha-ur-crest");
            Text urText = W.Txt(urCrest.transform, "UR", 0f, D(30f), D(138f), D(48f), "UR", 24, Color.white, TextAnchor.MiddleCenter);
            Outline uo = urText.gameObject.AddComponent<Outline>();
            uo.effectColor = RoSoftTheme.Hex("#7A2E8F"); uo.effectDistance = new Vector2(1.4f, -1.4f);
            previewPill = RarityPill(window, "PreviewRarity", X(650f), Y(248f), D(36f), PetRarity.SSR, 17, D(70f));

            arrowLeft = W.Img(window, "ArrowLeft", X(540f), Y(662f), D(30f), D(42f), "gacha-arrow-left", true);
            arrowRight = W.Img(window, "ArrowRight", X(1074f), Y(662f), D(30f), D(42f), "gacha-arrow-right", true);
            arrowLeft.color = arrowRight.color = RoSoftTheme.Hex("#9FB0D6"); // a softer slate than the painted navy
            W.Clickable(arrowLeft, () => TurnCarousel(-1));
            W.Clickable(arrowRight, () => TurnCarousel(1));
            for (int i = 0; i < CarouselSize; i++)
            {
                int slot = i;
                carousel[i] = new W.Cell(window, "Card" + i, X(583f + i * 97f), Y(634f), D(92f), () => PreviewSlot(slot));
                carouselPills[i] = RarityPill(carousel[i].Root, "Rarity", -2f, -3f, 15f, PetRarity.N, 10, 22f);
            }
            int pages = (Pets().Count + CarouselSize - 1) / CarouselSize;
            float dotsW = pages * D(28f);
            for (int i = 0; i < pages; i++) dots.Add(W.Img(window, "Dot" + i, X(817f) - dotsW * 0.5f + i * D(28f), Y(741f), D(17f), D(17f), "hud-count-badge"));

            var p1 = PullButton("Pull1", X(532f), "btn-mint", "Pull x1", 1);
            var p10 = PullButton("Pull10", X(828f), "btn-gold", "Pull x" + PetRules.MaxPullsAtOnce, PetRules.MaxPullsAtOnce);
            pull1Group = p1; pull10Group = p10;
        }

        /// <summary>Big two-line pull button: "Pull xN" over the coin icon and "x N" (cost in coins).</summary>
        CanvasGroup PullButton(string name, float x, string piece, string text, int count)
        {
            float w = D(276f), h = D(96f);
            Image face = W.Img(window, name, x, Y(782f), w, h, piece, true);
            face.pixelsPerUnitMultiplier = 0.55f; // tall pill corners as in the mockup
            Color rim = piece == "btn-mint" ? RoSoftTheme.Hex("#1E7F70") : RoSoftTheme.Hex("#B9811C");
            Text t = W.Txt(face.transform, "Label", 0f, D(10f), w, D(40f), text, 20, Color.white, TextAnchor.MiddleCenter);
            Outline o = t.gameObject.AddComponent<Outline>(); o.effectColor = rim; o.effectDistance = new Vector2(1.3f, -1.3f);
            RectTransform cost = UiKit.Rect(face.transform, "Cost", 0f, D(48f), w, D(36f));
            var row = cost.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter; row.spacing = 6f;
            row.childControlWidth = row.childControlHeight = false; row.childForceExpandWidth = row.childForceExpandHeight = false;
            W.Img(cost, "Coin", 0f, 0f, D(36f), D(36f), "gacha-coin");
            Text c = W.Txt(cost, "Count", 0f, 0f, 10f, D(36f), "x " + count, 19, Color.white, TextAnchor.MiddleCenter);
            c.rectTransform.sizeDelta = new Vector2(c.preferredWidth + 2f, D(36f));
            Outline co = c.gameObject.AddComponent<Outline>(); co.effectColor = rim; co.effectDistance = new Vector2(1.3f, -1.3f);
            W.Clickable(face, () => Pull(count));
            return face.gameObject.AddComponent<CanvasGroup>();
        }

        void TurnCarousel(int step)
        {
            int pages = dots.Count;
            carouselPage = (carouselPage + step + pages) % pages;
            Refresh();
        }

        void PreviewSlot(int slot)
        {
            int i = carouselPage * CarouselSize + slot;
            if (i < Pets().Count) PreviewPet(Pets()[i]);
        }

        void PreviewPet(PetDef d)
        {
            preview = d;
            Refresh();
        }

        void RefreshCentre(AccountState a)
        {
            PetDef d = preview ?? Featured;
            Texture2D tex = Resources.Load<Texture2D>("Sprites/pets/" + d.ArtId + "/art"); // the big art; the icon when a pet has none
            stagePet.sprite = tex != null ? StageSprite(d.ArtId, tex) : PetArt.Icon(d);
            stagePet.enabled = stagePet.sprite != null;
            previewName.text = d.Name;
            bool ur = d.Rarity == PetRarity.UR;
            urCrest.gameObject.SetActive(ur);
            previewPill.Root.gameObject.SetActive(!ur);
            if (!ur) SetRarity(previewPill, d.Rarity);

            List<PetDef> pets = Pets();
            for (int i = 0; i < CarouselSize; i++)
            {
                int k = carouselPage * CarouselSize + i;
                bool has = k < pets.Count;
                carousel[i].Root.gameObject.SetActive(has);
                if (!has) continue;
                PetDef p = pets[k];
                PetCard(carousel[i], p.Rarity, p == d);
                carousel[i].SetIcon(PetArt.Icon(p), Color.white, 0.9f);
                SetRarity(carouselPills[i], p.Rarity);
            }
            for (int i = 0; i < dots.Count; i++) dots[i].color = i == carouselPage ? RoSoftTheme.Hex("#5C8FE0") : new Color(0.8f, 0.84f, 0.92f, 1f);
            bool idle = pendingCount == 0;
            pull1Group.alpha = a.tickets >= 1 && idle ? 1f : 0.6f;
            pull10Group.alpha = a.tickets >= PetRules.MaxPullsAtOnce && idle ? 1f : 0.6f;
        }

        static readonly Dictionary<string, Sprite> StageSprites = new Dictionary<string, Sprite>();

        static Sprite StageSprite(string id, Texture2D tex)
        {
            Sprite s;
            if (StageSprites.TryGetValue(id, out s) && s != null) return s;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 100f);
            StageSprites[id] = s;
            return s;
        }
    }
}
