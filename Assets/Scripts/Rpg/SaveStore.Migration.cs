using System;
using System.Collections.Generic;
using System.IO;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Moves the saves from before character slots (one per class: zenonia-save-&lt;class&gt;.json, with the slot it was shown in kept beside it in
    /// zenonia-slot-&lt;class&gt;.txt) into slot files. The save text is copied byte for byte (nothing is re-encoded, a hero without a name shows its class name),
    /// then the old files are renamed to .migrated as a backup. Classes whose kept slot is valid and free go first so they keep their place; the rest take the
    /// first free slots in class order. Runs whenever saves are read; with nothing old left it only checks that the files are gone.
    /// A crash between the copy and the rename is safe: the next run finds a slot already holding the same text and only renames.
    /// </summary>
    public static partial class SaveStore
    {
        public const string MigratedSuffix = ".migrated";

        public static string LegacyPathFor(string classId) { return Path.Combine(Root, "zenonia-save-" + classId + ".json"); }
        public static string LegacySlotPathFor(string classId) { return Path.Combine(Root, "zenonia-slot-" + classId + ".txt"); }

        /// <summary>Moves every old class save into a slot. Returns how many moved. A save that finds no free slot stays where it is (and is tried again next time).</summary>
        public static int MigrateLegacy()
        {
            if (!Enabled) return 0;
            var pending = new List<string>();
            foreach (string id in RpgClasses.Ids) if (File.Exists(LegacyPathFor(id))) pending.Add(id);
            if (pending.Count == 0) return 0;
            int moved = 0;
            var rest = new List<string>();
            foreach (string id in pending) // first the saves that remember a free slot keep it ...
            {
                int slot = KeptSlot(id);
                if (slot < 0) rest.Add(id);
                else if (MigrateOne(id, slot)) moved++;
            }
            foreach (string id in rest) if (MigrateOne(id, -1)) moved++; // ... then the others, first free slot, class order
            return moved;
        }

        /// <summary>The slot an old save was shown in (zenonia-slot-&lt;class&gt;.txt) when it is a real slot without a save yet, else -1.</summary>
        static int KeptSlot(string classId)
        {
            try
            {
                string p = LegacySlotPathFor(classId);
                int slot;
                if (!File.Exists(p) || !int.TryParse(File.ReadAllText(p).Trim(), out slot) || !IsSlot(slot)) return -1;
                return File.Exists(PathForSlot(slot)) ? -1 : slot;
            }
            catch (Exception) { return -1; }
        }

        static bool MigrateOne(string classId, int slot)
        {
            string legacy = LegacyPathFor(classId);
            try
            {
                string text = File.ReadAllText(legacy);
                int already = SlotWithText(text);
                if (already >= 0) slot = already; // copied before, the rename did not happen
                else
                {
                    if (slot < 0) for (int s = 0; s < MaxSlots && slot < 0; s++) if (!File.Exists(PathForSlot(s))) slot = s;
                    if (slot < 0) { Warn("no free slot for the old " + classId + " save: left in place"); return false; }
                    WriteText(PathForSlot(slot), text);
                }
                Backup(legacy);
                Backup(LegacySlotPathFor(classId));
                return true;
            }
            catch (Exception e) { Warn("moving the old " + classId + " save failed: " + e.Message); return false; }
        }

        static int SlotWithText(string text)
        {
            for (int s = 0; s < MaxSlots; s++)
            {
                string p = PathForSlot(s);
                if (File.Exists(p) && File.ReadAllText(p) == text) return s;
            }
            return -1;
        }

        /// <summary>Renames a file to &lt;file&gt;.migrated (replacing an older backup of the same name).</summary>
        static void Backup(string path)
        {
            if (!File.Exists(path)) return;
            string to = path + MigratedSuffix;
            if (File.Exists(to)) File.Delete(to);
            File.Move(path, to);
        }
    }
}
