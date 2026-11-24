namespace ZenoniaProto.Rpg
{
    /// <summary>The 40 pre-Taurus card rows (hidden from the Album since Taurus V4, still owned / droppable with --legacy-maps) + the 12 Taurus rows (CardTableTaurus.cs, appended). Edge numbers (top, right, bottom, left) sum inside the rarity band and each card has one clearly strongest edge.</summary>
    public static partial class CardTable
    {
        const CardRarity N = CardRarity.N, R = CardRarity.R, SR = CardRarity.SR, SSR = CardRarity.SSR;
        const CardCategory Atk = CardCategory.Atk, Def = CardCategory.Def, Hp = CardCategory.Hp, Util = CardCategory.Utility;

        static CardDef C(string id, string name, string region, CardRarity rarity, string element, CardCategory cat, int t, int r, int b, int l, string desc, bool mp = false, bool boss = false)
        {
            return new CardDef(id, name, region, rarity, boss, element, cat, mp, t, r, b, l, desc);
        }

        public static readonly CardDef[] All =
        {
            // Willowbrook Meadows
            C("meadowSlime", "Meadow Slime", "willowbrookMeadows", N, "Nature", Hp, 6, 3, 5, 4, "A wobbly green slime that bounces around the meadow gate."),
            C("hornRabbit", "Horn Rabbit", "willowbrookMeadows", N, "Wind", Atk, 3, 7, 2, 4, "Quick rabbit with a sharp horn; it charges anything that moves."),
            C("thornBoar", "Thorn Boar", "willowbrookMeadows", N, "Earth", Def, 4, 3, 7, 2, "Bristling boar with a hide as tough as bark."),
            // Bramblewood Forest
            C("meadowMantis", "Meadow Mantis", "bramblewoodForest", N, "Nature", Atk, 2, 8, 3, 4, "A green mantis that strikes from the side with both scythes."),
            C("forestWolf", "Forest Wolf", "bramblewoodForest", N, "Nature", Atk, 7, 4, 2, 3, "Grey wolf that hunts the forest paths in packs."),
            C("brambleTreant", "Bramble Treant", "bramblewoodForest", N, "Nature", Def, 3, 2, 8, 4, "A walking tangle of roots that guards its clearing."),
            C("thornSprite", "Thorn Sprite", "bramblewoodForest", N, "Nature", Util, 4, 6, 2, 5, "A tiny spirit that throws thorns and hums to the forest.", mp: true),
            C("thornfangAlpha", "Thornfang Alpha", "bramblewoodForest", SSR, "Nature", Atk, 9, 6, 5, 6, "The alpha of Bramblewood, its fangs split armour.", boss: true),
            // Rootburrow
            C("caveMantis", "Cave Mantis", "rootburrow", N, "Earth", Atk, 3, 7, 4, 2, "A pale mantis grown fast in the dark of the roots."),
            C("gloomWolf", "Gloom Wolf", "rootburrow", R, "Shadow", Atk, 8, 4, 3, 5, "A wolf that lost its colour deep underground."),
            C("crystalSprite", "Crystal Sprite", "rootburrow", R, "Earth", Util, 5, 3, 7, 4, "Glass-bodied sprite that stores light in its wings.", mp: true),
            C("hollowTreant", "Hollow Treant", "rootburrow", R, "Nature", Def, 3, 4, 9, 5, "A hollowed trunk, slow but nearly impossible to push through."),
            C("ambercapWarden", "Ambercap Warden", "rootburrow", SSR, "Nature", Def, 5, 7, 8, 9, "Warden of the deepest chamber, capped with glowing amber.", boss: true),
            // Frost Hollow
            C("frostSlime", "Frost Slime", "frostHollow", R, "Ice", Hp, 5, 7, 3, 4, "Cold jelly that chills whoever touches it."),
            C("snowFox", "Snow Fox", "frostHollow", R, "Ice", Util, 4, 8, 3, 5, "White fox that slips away and strikes at weak spots."),
            C("iceImp", "Ice Imp", "frostHollow", R, "Ice", Util, 6, 3, 4, 7, "A giggling imp that flings shards of ice.", mp: true),
            C("iceWraith", "Ice Wraith", "frostHollow", R, "Ice", Util, 7, 2, 5, 5, "A freezing ghost that drains the warmth from the air.", mp: true),
            C("frostBear", "Frost Bear", "frostHollow", R, "Ice", Def, 4, 3, 8, 6, "Huge bear wrapped in frozen fur."),
            C("frostbiteYeti", "Frostbite Yeti", "frostHollow", R, "Ice", Hp, 3, 5, 9, 4, "A towering yeti with a fist that cracks the ice."),
            C("crystalGolem", "Crystal Golem", "frostHollow", R, "Earth", Def, 2, 5, 8, 6, "A golem of frozen crystal, slow and heavy."),
            C("frostfangKing", "Frostfang King", "frostHollow", SSR, "Ice", Atk, 7, 9, 5, 6, "The king of Frost Hollow, ruler of every frost wolf.", boss: true),
            C("rimeclawTyrant", "Rimeclaw Tyrant", "frostHollow", SSR, "Ice", Util, 9, 7, 6, 6, "A wanted tyrant whose rime claws freeze what they cut.", boss: true),
            // Frostfang Den
            C("iceGoblin", "Ice Goblin", "frostfangDen", R, "Ice", Atk, 7, 6, 3, 4, "A goblin with a frozen club and a cruel grin."),
            C("spectralWolf", "Spectral Wolf", "frostfangDen", R, "Shadow", Atk, 8, 5, 4, 3, "A ghostly wolf that hunts the Den tunnels."),
            // Amber Dunes
            C("duneScarab", "Dune Scarab", "amberDunes", SR, "Earth", Def, 5, 4, 9, 5, "A shining scarab whose shell turns blades aside."),
            C("sandRaider", "Sand Raider", "amberDunes", SR, "Fire", Atk, 8, 6, 5, 4, "A raider of the dunes, swift with a curved blade."),
            C("amberWisp", "Amber Wisp", "amberDunes", SR, "Fire", Util, 5, 9, 4, 6, "A drifting flame that follows travellers at dusk.", mp: true),
            // Emberfen Marsh
            C("bogSlime", "Bog Slime", "emberfenMarsh", SR, "Poison", Hp, 9, 5, 4, 5, "Murky slime thick with marsh poison."),
            C("mireToad", "Mire Toad", "emberfenMarsh", SR, "Poison", Def, 5, 4, 9, 6, "A toad the size of a cart, skin slick with venom."),
            C("marshStalker", "Marsh Stalker", "emberfenMarsh", SR, "Poison", Atk, 6, 9, 5, 4, "A lean hunter that waits in the reeds."),
            // Sunken Reliquary
            C("ruinWisp", "Ruin Wisp", "sunkenReliquary", SR, "Shadow", Util, 4, 5, 6, 9, "A pale light that haunts the flooded halls."),
            C("mirefangHydra", "Mirefang Hydra", "sunkenReliquary", SSR, "Poison", Hp, 6, 5, 9, 8, "A many-headed hydra that guards the reliquary depths.", boss: true),
            // Sunbreak Foothills
            C("highlandIbex", "Highland Ibex", "sunbreakFoothills", R, "Earth", Hp, 5, 4, 3, 8, "A surefooted ibex that rams down the slope."),
            C("dustscaleLizard", "Dustscale Lizard", "sunbreakFoothills", R, "Fire", Util, 3, 7, 6, 4, "A lizard that bakes on warm rocks and spits dust."),
            C("cliffHarpy", "Cliff Harpy", "sunbreakFoothills", R, "Wind", Atk, 8, 5, 2, 5, "A screaming harpy that dives from the cliffs."),
            C("stonebackGuardian", "Stoneback Guardian", "sunbreakFoothills", R, "Earth", Def, 4, 3, 9, 6, "A guardian with a back of living stone."),
            // Scorpion Gorge
            C("giantScorpion", "Giant Scorpion", "scorpionGorge", SR, "Poison", Atk, 9, 6, 5, 3, "A scorpion whose sting can pierce plate."),
            C("sandWraith", "Sand Wraith", "scorpionGorge", SR, "Shadow", Util, 5, 3, 8, 7, "A wraith that rises out of the sandstorm."),
            C("rockBasilisk", "Rock Basilisk", "scorpionGorge", SR, "Earth", Hp, 6, 4, 9, 5, "A stone-scaled basilisk, patient and strong."),
            C("duneGolem", "Dune Golem", "scorpionGorge", SR, "Earth", Def, 3, 6, 9, 6, "A golem packed from the gorge's red sand."),
            // Taurus V4 (24/11/2026, BIBLE part 3 X.2): the live collection. Ids = the Taurus monster ids (phase 05 / 06 add the monsters; a card without its monster just never drops yet).
            // Shapes follow BIBLE X.3 (Slime balanced, Rabbit strong right, Frog top, Mushroom bottom, Mantis right) on this table's 14-18 band, one strongest edge each.
            C("tauCloverSlime", "Clover Slime", "tauCloverMeadows", N, "Nature", Hp, 5, 3, 4, 3, "A soft green slime that bounces along the meadow road."),
            C("tauHornRabbit", "Horn Rabbit", "tauCloverMeadows", N, "Wind", Atk, 3, 7, 2, 3, "A quick rabbit whose little horn always leads the charge."),
            C("tauReedFrog", "Reed Frog", "tauCloverMeadows", N, "Nature", Atk, 6, 3, 3, 3, "A brook frog that leaps first and thinks later."),
            C("tauMossMushroom", "Moss Mushroom", "tauCloverMeadows", N, "Nature", Def, 3, 3, 6, 3, "A mossy cap that plants its roots and does not budge."),
            C("tauBrambleBoar", "Bramble Boar", "tauCloverMeadows", N, "Earth", Atk, 6, 5, 3, 3, "A bristly boar that rushes anything in its thicket."),
            C("tauLeafMantis", "Leaf Mantis", "tauCloverMeadows", N, "Wind", Atk, 3, 6, 3, 4, "A leaf-green mantis that strikes hard from one side."),
            C("tauRootling", "Rootling", "tauAncientRootShrine", N, "Earth", Def, 4, 3, 5, 4, "A knot of living roots that woke inside the old shrine."),
            C("tauAmberSporeling", "Amber Sporeling", "tauAncientRootShrine", N, "Nature", Util, 3, 4, 3, 6, "A small amber cap that drifts spores toward its left.", mp: true),
            C("tauThornSprite", "Thorn Sprite", "tauCloverMeadows", R, "Nature", Util, 4, 7, 3, 6, "A tiny sprite that throws thorns and hums to the forest.", mp: true),
            C("tauAncientBarkling", "Ancient Barkling", "tauCloverMeadows", R, "Nature", Def, 3, 5, 8, 4, "A barked-over sapling older than the village oak."),
            C("tauRuneBarkGuardian", "Rune Bark Guardian", "tauAncientRootShrine", R, "Earth", Def, 5, 4, 8, 5, "A shrine guard whose bark glows with old runes."),
            C("tauElderRootGuardian", "Elder Root Guardian", "tauAncientRootShrine", SR, "Nature", Hp, 6, 5, 9, 6, "The root-bound keeper of the Ancient Root Shrine.", boss: true),
            // Field Boss card (24/11, ChatGPT taurus-field-boss-chatgpt.txt IV.2): Rare, one strong edge, own region = not in the Album (12 stays 12), drops 2 % from the Clover Meadows boss only.
            C("tauBramblebackMatriarch", "Brambleback Matriarch", CardTable.FieldBossRegion, R, "Earth", Atk, 7, 5, 3, 4, "The thorn-backed matriarch of the Bramble Thicket, rare prize of the meadow hunters.", boss: true),
        };
    }
}
