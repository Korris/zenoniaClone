using System.Collections.Generic;
using System.IO;
using ZenoniaProto.Api;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The offline heroes for the character select screen: one save file per class (SaveStore), so at most one hero of each class and six in all.
    /// Each save is shown as a CharacterSummary (id = class id, name = class name) so the list draws online and offline heroes the same way.
    /// </summary>
    public static class OfflineCharacterRoster
    {
        /// <summary>A small file beside the class's save (zenonia-slot-&lt;class&gt;.txt) holds the character select slot it was created in; the save format stays as it is.</summary>
        static string SlotPath(string classId) { return Path.Combine(SaveStore.Root, "zenonia-slot-" + classId + ".txt"); }

        /// <summary>The slot of a class's save on the character select screen, -1 when none was kept (saves from before slots: placed in free slots).</summary>
        public static int SlotOf(string classId)
        {
            int slot;
            try { return File.Exists(SlotPath(classId)) && int.TryParse(File.ReadAllText(SlotPath(classId)).Trim(), out slot) ? slot : -1; }
            catch (System.Exception) { return -1; }
        }

        /// <summary>Remembers the empty slot a new hero of this class was started in (written when the class is picked; the save follows on Confirm).</summary>
        public static void SetSlot(string classId, int slot)
        {
            if (!SaveStore.Enabled || slot < 0) return;
            try { Directory.CreateDirectory(SaveStore.Root); File.WriteAllText(SlotPath(classId), slot.ToString()); }
            catch (System.Exception e) { UnityEngine.Debug.LogWarning("[ZenoniaProto] slot file: " + e.Message); }
        }

        /// <summary>The offline saves in class order. Empty while SaveStore is off (captures without --save-root).</summary>
        public static List<CharacterSummary> Load()
        {
            var list = new List<CharacterSummary>();
            foreach (HeroClassDef def in HeroClassDef.All)
            {
                string classId = RpgSession.ClassId(def.Id);
                SaveData save = SaveStore.Load(classId);
                if (save == null) continue;
                list.Add(new CharacterSummary { id = classId, name = def.Name, classId = classId, level = save.level, mapId = save.mapId, appearance = save.appearance, slot = SlotOf(classId) });
            }
            return list;
        }

        /// <summary>True when this class already has a save: the create flow offers it greyed out ("You already have this hero").</summary>
        public static bool Has(HeroClass id) { return SaveStore.Load(RpgSession.ClassId(id)) != null; }

        /// <summary>Removes the save of a class (after the hold-to-delete confirmation). The hero being played is never passed here.</summary>
        public static void Delete(string classId)
        {
            if (!SaveStore.Enabled) return; // off = a capture run: never the player's real saves
            SaveStore.Delete(classId);
            try { if (File.Exists(SlotPath(classId))) File.Delete(SlotPath(classId)); } catch (System.Exception) { /* best effort */ }
        }
    }
}
