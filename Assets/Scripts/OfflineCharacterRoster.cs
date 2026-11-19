using System.Collections.Generic;
using ZenoniaProto.Api;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The offline heroes for the character select screen: one save per slot (SaveStore, up to six, several of a class allowed), each shown as a CharacterSummary
    /// (id = "slot&lt;N&gt;", the hero's name, slot = N) so the list draws online and offline heroes the same way. Older one-per-class saves are moved into slots by SaveStore.
    /// </summary>
    public static class OfflineCharacterRoster
    {
        const string IdPrefix = "slot";

        public static string IdOf(int slot) { return IdPrefix + slot; }

        /// <summary>The slot of a listed offline hero ("slot3" -> 3), -1 for anything else.</summary>
        public static int SlotOf(string id)
        {
            int slot;
            return id != null && id.StartsWith(IdPrefix) && int.TryParse(id.Substring(IdPrefix.Length), out slot) && SaveStore.IsSlot(slot) ? slot : -1;
        }

        /// <summary>The offline saves in slot order. Empty while SaveStore is off (captures without --save-root).</summary>
        public static List<CharacterSummary> Load()
        {
            var list = new List<CharacterSummary>();
            foreach (KeyValuePair<int, SaveData> e in SaveStore.LoadAll())
            {
                SaveData save = e.Value;
                list.Add(new CharacterSummary { id = IdOf(e.Key), name = SaveStore.DisplayName(save), classId = save.classId, level = save.level, mapId = save.mapId, appearance = save.appearance, slot = e.Key });
            }
            return list;
        }

        /// <summary>Null when a new offline hero may take this name (online rules + not used by another hero on this device), else what is wrong.</summary>
        public static string NameProblem(string name) { return SaveStore.Enabled ? SaveStore.NameProblem(name) : HeroNameRules.Problem(name); }

        /// <summary>Removes the save in a slot (after the hold-to-delete confirmation). The hero being played is never passed here.</summary>
        public static void Delete(int slot)
        {
            if (!SaveStore.Enabled) return; // off = a capture run: never the player's real saves
            SaveStore.Delete(slot);
        }
    }
}
