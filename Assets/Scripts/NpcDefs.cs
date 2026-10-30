using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>One villager: name, facing row of the HD idle sheet (0 down, 1 left, 2 right), chatter pages and whether he runs the quest.</summary>
    public sealed class NpcDef
    {
        public string Id, Name;
        public int Facing;
        public string[] Lines;
        /// <summary>True = talking opens QuestTracker's quest dialogue (ElderNpc) instead of the idle chatter.</summary>
        public bool QuestGiver;
    }

    /// <summary>
    /// Oakhaven villagers from the web src/data/npc-definitions.js (Oakhaven ids only; the other maps' NPCs come with their maps). Roles without a
    /// game system yet (shop, healing, fishing, crafting) are dialogue-only, as in the web; the merchant says so. The web's village elder Rowan is
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
                "Potions, trinkets, odds and ends... the stall opens as soon as my cart arrives.", "Gold from the slimes spends just fine here.\n(Shop: coming soon)") },
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
        };

        static NpcDef Def(string id, string name, int facing, bool questGiver, params string[] lines)
        {
            return new NpcDef { Id = id, Name = name, Facing = facing, QuestGiver = questGiver, Lines = lines };
        }

        public static NpcDef Get(string id)
        {
            NpcDef d;
            return Defs.TryGetValue(id, out d) ? d : null;
        }
    }
}
