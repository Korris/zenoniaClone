using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>One villager: name, facing row of the HD idle sheet (0 down, 1 left, 2 right), chatter pages and whether he runs the quest.</summary>
    public sealed class NpcDef
    {
        public string Id, Name;
        /// <summary>File name of the idle sheet and face in Sprites/village/npcs: the Id.</summary>
        public string Art;
        public int Facing;
        public string[] Lines;
        /// <summary>True = talking opens the quest dialogue (ElderNpc) instead of the idle chatter, and he shows quest markers. QuestDef.GiverNpcId says which quests.</summary>
        public bool QuestGiver;
    }

    /// <summary>
    /// Oakhaven villagers from the web src/data/npc-definitions.js, then Sunspire's townsfolk (the other maps' NPCs come with their maps). Potion sellers
    /// (PotionShops) open the shop; roles without a game system yet (healing, fishing, crafting, bounties) are dialogue-only, as in the web. The web's village elder Rowan is
    /// the quest giver Elder Thomas here: same spot, same HD art, the existing Frost Slime quest and dialogue window.
    /// </summary>
    public static class NpcDefs
    {
        const int Down = 0, Left = 1, Right = 2;

        static readonly Dictionary<string, NpcDef> Defs = new Dictionary<string, NpcDef>
        {
            { "elderRowan", Def("elderRowan", "Elder Thomas", Down, true, "Welcome to Oakhaven Village, young one.") },
            { "villagerMia", Def("villagerMia", "Mia", Down, false,
                "Did you know? Spending skill points raises your power a lot.", "Press TAB to open the skill tree.") },
            { "hunterLena", Def("hunterLena", "Hunter Lena", Left, false,
                "The bats near the south woods keep stealing my arrows.", "Press J four times fast for a full combo!") },
            { "merchantDale", Def("merchantDale", "Merchant Dale", Down, false,
                "Potions, trinkets, odds and ends...", "Gold from the slimes spends just fine here.") },
            { "fishermanRoy", Def("fishermanRoy", "Fisherman Roy", Right, false,
                "The river runs clear past the dock. Best bite is at dawn.", "Mind the current east of the bridge.") },
            { "carpenterBrol", Def("carpenterBrol", "Carpenter Brol", Left, false,
                "Oak for beams, pine for kindling. Everything here I cut myself.", "Bring me good timber and I might sharpen that sword of yours.") },
            { "farmerGuri", Def("farmerGuri", "Farmer Guri", Down, false,
                "Turnips, carrots and the odd pumpkin. The slimes keep trampling the rows.", "Shut the gate behind you, would you?") },
            { "guardRook", Def("guardRook", "Guard Rook", Down, false,
                "Halt... ah, it is you. The south road leads to Frost Hollow.", "Nothing gets past this gate without my say-so.") },
            { "childTim", Def("childTim", "Tim", Down, false,
                "Are you a real adventurer? Can I see your sword?", "Mum says I am not allowed past the bridge.") },
            { "healerNina", Def("healerNina", "Nina", Down, false,
                "Herbs from the riverbank, honey from the woods. Rest here if you are hurt.", "Red potions mend the body, blue ones the spirit.") },
            // Sunspire Oasis
            { "elderSahir", Def("elderSahir", "Elder Sahir", Down, false,
                "Peace on your road, traveller. The pool has never run dry, and neither has our welcome.", "Caravans still leave at dawn, though fewer come back from the dunes.") },
            { "apothecaryNara", Def("apothecaryNara", "Nara", Down, false,
                "Desert aloe and date syrup: nothing better when the heat gets into your bones.", "Fresh bottles every morning, friend.") },
            { "blacksmithKhaled", Def("blacksmithKhaled", "Khaled", Left, false,
                "Sand gets into everything, even the bellows. Clean your blade every night.", "Desert steel, tempered for the dunes. Have a look at the rack.") },
            { "bountyLeila", Def("bountyLeila", "Leila", Left, false,
                "Every name on this board once cost a merchant his cargo.", "Nothing worth your time is posted today. Ask me again later.\n(Bounties: coming soon)") },
            // Thatchwick Village (world-map-2 step 1, plans/261113-0106-world-map-step1): herbalist, two blacksmith tiers and a market broker.
            // Reused portraits, no new art: Wren = apothecaryNara's basket-and-herbs look, Holt = guardRook's armoured look, Pell = elderSahir's trader look.
            { "herbalistWren", DefArt("herbalistWren", "Wren", "apothecaryNara", Down, false,
                "Frostmint and riverbank root, the hill folk swear by both.", "A draught a day keeps the frostbite at bay, or so they say.") },
            { "smithHolt", DefArt("smithHolt", "Holt", "guardRook", Down, false,
                "Thatchwick steel outlasts anything from the lowland forges.", "Come back once you've climbed higher. I will have better on the rack.") },
            { "brokerPell", DefArt("brokerPell", "Pell", "elderSahir", Down, false,
                "This square is the only market worth the name between here and the dunes.", "Stand in the square if you want to trade with another traveller. I only point the way.") },
            // Oakhaven's blacksmith (world-map-2 step 1): same look as the carpenter, a hammer at the forge.
            { "smithBram", DefArt("smithBram", "Bram", "carpenterBrol", Down, false,
                "Steel and leather, nothing fancy, but it holds an edge.", "Oakhaven forges plain gear. Good enough until you see higher country.") },
        };

        static NpcDef Def(string id, string name, int facing, bool questGiver, params string[] lines)
        {
            return new NpcDef { Id = id, Name = name, Art = id, Facing = facing, QuestGiver = questGiver, Lines = lines };
        }

        /// <summary>Same as Def, but reusing another villager's sprite sheet / face (no new art drawn for this NPC).</summary>
        static NpcDef DefArt(string id, string name, string art, int facing, bool questGiver, params string[] lines)
        {
            return new NpcDef { Id = id, Name = name, Art = art, Facing = facing, QuestGiver = questGiver, Lines = lines };
        }

        public static NpcDef Get(string id)
        {
            NpcDef d;
            return Defs.TryGetValue(id, out d) ? d : null;
        }
    }
}
