using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// ArmoryWindow: the carousel. The hero (the live paper doll of the old bag, HeroPortraitView) stands on the round platform under the
    /// "Lv. N" ribbon, between the seven pedestals of what is worn. A tap on a pedestal selects it (the right paper shows the worn item);
    /// the Pet pedestal shows the Combat pet and opens the Pet window. Stats and "Enhance Equipped" sit in front of the platform.
    /// Everything of the carousel is on one rect, so the grid view can show it small above the board (ApplyView).
    /// </summary>
    public sealed partial class ArmoryWindow
    {
        const float HeroX = 442f, MiniScale = 0.34f;
        // Centre of the item over each pedestal in design units, Slots.All order (weapon, necklace, armor, gloves, boots, offhand, pet): measured on the mockup.
        static readonly Vector2[] PedestalAt =
        {
            new Vector2(591f, 102f), new Vector2(580f, 255f), new Vector2(442f, 276f), new Vector2(296f, 248f), new Vector2(228f, 173f), new Vector2(657f, 173f), new Vector2(289f, 102f),
            new Vector2(160f, 250f), // headgear (cosmetic hat, 19/11/2026): not on the mockup, left of the arc
        };
        /// <summary>The pedestals from left to right along the arc: the order Left / Right walk them.</summary>
        static readonly string[] Ring = { Slots.Headgear, Slots.Pet, Slots.Boots, Slots.Gloves, Slots.Armor, Slots.Helmet, Slots.Offhand, Slots.Weapon };
        static readonly Color Faint = new Color(0.72f, 0.76f, 0.82f, 0.26f); // outline of what belongs on an empty pedestal

        readonly ArmoryPedestal[] pedestals = new ArmoryPedestal[Slots.All.Length];
        RectTransform carousel, heroRect;
        GameObject carouselButtons;
        HeroPortraitView heroView;
        Text levelText;

        void BuildCarousel()
        {
            carousel = UiKit.Rect(content, "Carousel", 0f, 0f, 732f, 388f);
            PaperArt.Draw(UiKit.Rect(carousel, "Platform", HeroX - 190f, 200f, 380f, 187f), PaperPiece.BagPlatform, PaperTheme.Slate, PaperTheme.Steel, 60f); // the cut dais is 732 x 360 px: the hero stands on its top disc
            heroRect = UiKit.Rect(carousel, "Hero", HeroX - 168f, 46f, 336f, 264f); // the portrait's own 336 x 264 box: feet on the platform
            var host = new GameObject("ArmoryHeroPortrait"); // in the world, not on the canvas: the portrait films a real HeroRenderer
            host.transform.SetParent(UiScreens.Root, false);
            heroView = host.AddComponent<HeroPortraitView>();
            heroView.Init(heroRect.gameObject.AddComponent<RawImage>());
            for (int i = 0; i < pedestals.Length; i++)
            {
                int index = i;
                pedestals[i] = new ArmoryPedestal(carousel, "Pedestal_" + Slots.All[i], PedestalAt[i].x, PedestalAt[i].y, ItemInfo.SlotLabel(Slots.All[i]), () => OnPedestalTap(index));
            }
            RectTransform plate = UiKit.Rect(carousel, "Level", HeroX - 78f, 40f, 156f, 46f); // red plate with crossed swords on its left end
            PaperArt.Draw(plate, PaperPiece.BagLevelPlate, PaperTheme.Red, PaperTheme.BtnRedEdge, 12f);
            levelText = PaperTheme.Caption(plate, "", 20, PaperTheme.TextLight);
            levelText.rectTransform.offsetMin = new Vector2(34f, 4f);

            carouselButtons = UiKit.Rect(content, "CarouselButtons", 0f, 0f, 10f, 10f).gameObject;
            new PaperButton(carouselButtons.transform, "Stats", 20f, 326f, 126f, 40f, "Stats", PaperTone.Blue, ToggleStats);
            RectTransform anvil = UiKit.Rect(carouselButtons.transform, "EnhanceEquipped", HeroX - 118f, 346f, 236f, 44f);
            Image plaque = PaperArt.Draw(anvil, PaperPiece.BagAnvil, PaperTheme.WoodDark, PaperTheme.Wood, 14f);
            PaperTheme.Caption(anvil, "Enhance Equipped", PaperTheme.SectionSize, PaperTheme.TitleGold).rectTransform.offsetMin = new Vector2(46f, 0f); // the anvil sits on the left end
            UiWinKit.MakeButton(plaque, EnhanceEquipped);
        }

        /// <summary>The pedestal of a worn slot is under this screen point (a card was dropped there); the hero himself counts as "whatever slot fits".</summary>
        int PedestalUnder(Vector2 screen, out bool onHero)
        {
            onHero = false;
            for (int i = 0; i < pedestals.Length; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint(pedestals[i].Root, screen, null)) return i;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(heroRect, screen, null, out local);
            onHero = Mathf.Abs(local.x - heroRect.rect.center.x) < 70f && heroRect.rect.Contains(local); // the middle of the portrait box: the hero's body
            return -1;
        }

        void RefreshCarousel(PlayerState p, HeroController hero)
        {
            levelText.text = "Lv. " + p.Level;
            heroView.Show(hero.Class, p.GetEquipped(Slots.Weapon), p.GetEquipped(Slots.Offhand), p);
            for (int i = 0; i < pedestals.Length; i++)
            {
                string slot = Slots.All[i];
                ArmoryPedestal v = pedestals[i];
                bool lit = i == model.Worn || i == dragOver;
                if (slot == Slots.Pet) // the Combat pet of the account, not an item
                {
                    PetDef pet = p.Pet;
                    if (pet != null) v.Set(PetArt.Icon(pet), PetArt.TintOf(pet), PetArt.Stars(p.PetStars), lit);
                    else v.Set(PetArt.Icon(PetConfig.All[0]), Faint, null, lit);
                    continue;
                }
                Item item = p.GetEquipped(slot);
                int plus = EnhanceStats.Level(item);
                if (item != null) v.Set(LootSprites.Icon(item), Color.white, plus > 0 ? "+" + plus : null, lit);
                else
                {
                    bool classGear = slot == Slots.Weapon || slot == Slots.Offhand;
                    v.Set(LootSprites.Icon(new Item { slot = slot, itemLevel = 1, classId = classGear ? p.ClassId : null }), Faint, null, lit);
                }
                ItemEnhanceSlotFx.Show(v.Icon, item);
            }
        }
    }
}
