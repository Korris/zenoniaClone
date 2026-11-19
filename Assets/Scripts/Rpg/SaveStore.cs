using System;
using System.Collections.Generic;
using System.IO;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Offline saves: one JSON file per character slot (zenonia-save-slot&lt;N&gt;.json, N = 0..MaxSlots-1) in Application.persistentDataPath, so a device holds up to
    /// six heroes and several may share a class; the class and the hero's name are inside the file (SaveData.classId / name). Writes go through a temp file so a
    /// crash never leaves a half-written save; an unreadable file is kept as .bad and ignored. Saves from before slots (one per class) move over by themselves
    /// (SaveStore.Migration.cs). Engine-free so the dotnet tests run it on a temp folder (RootOverride). Disable with Enabled = false (screenshot / smoke runs).
    /// </summary>
    public static partial class SaveStore
    {
        /// <summary>Character slots on one device, like the online account (AccountFlow.MaxCharacters).</summary>
        public const int MaxSlots = 6;

        /// <summary>Frost Hollow's current row count, set at startup (Bootstrap) from FrostHollowLayout.RowCount: this assembly cannot see the map classes. 24 = not grown, never shifts.</summary>
        public static int FrostHollowRows = 24;

        public static bool Enabled = true;
        /// <summary>Test hook: write somewhere else than persistentDataPath.</summary>
        public static string RootOverride;

#if UNITY_5_3_OR_NEWER
        public static string Root { get { return RootOverride ?? UnityEngine.Application.persistentDataPath; } }
        static void Warn(string text) { UnityEngine.Debug.LogWarning("[ZenoniaProto] " + text); }
#else
        public static string Root { get { return RootOverride ?? Path.Combine(Path.GetTempPath(), "zenonia-saves"); } }
        static void Warn(string text) { Console.Error.WriteLine("[ZenoniaProto] " + text); }
#endif

        public static bool IsSlot(int slot) { return slot >= 0 && slot < MaxSlots; }
        public static string PathForSlot(int slot) { return Path.Combine(Root, "zenonia-save-slot" + slot + ".json"); }

        /// <summary>The name a save shows: its own, or the class name for saves made before heroes were named.</summary>
        public static string DisplayName(SaveData d) { return d == null ? "" : !string.IsNullOrEmpty(d.name) ? d.name : RpgClasses.Get(d.classId).Name; }

        public static bool Write(SaveData data, int slot)
        {
            if (!Enabled || data == null || !IsSlot(slot)) return false;
            try { WriteText(PathForSlot(slot), SaveSerializer.ToJson(data)); return true; }
            catch (Exception e) { Warn("save failed: " + e.Message); return false; }
        }

        /// <summary>The validated save in a slot, or null (no file, unreadable, saves off). Moves older class saves into slots first.</summary>
        public static SaveData Load(int slot)
        {
            if (!Enabled || !IsSlot(slot)) return null;
            MigrateLegacy();
            return ReadSlot(slot);
        }

        /// <summary>Every slot that holds a save, in slot order (older class saves are moved into slots first).</summary>
        public static List<KeyValuePair<int, SaveData>> LoadAll()
        {
            var list = new List<KeyValuePair<int, SaveData>>();
            if (!Enabled) return list;
            MigrateLegacy();
            for (int s = 0; s < MaxSlots; s++)
            {
                SaveData d = ReadSlot(s);
                if (d != null) list.Add(new KeyValuePair<int, SaveData>(s, d));
            }
            return list;
        }

        /// <summary>The first slot without a save file, -1 when all six are used.</summary>
        public static int FirstFreeSlot()
        {
            if (!Enabled) return -1;
            MigrateLegacy();
            for (int s = 0; s < MaxSlots; s++) if (!File.Exists(PathForSlot(s))) return s;
            return -1;
        }

        /// <summary>The first slot holding a hero of this class, -1 when none (the dev class keys F1..F6 / 1..6 pick it).</summary>
        public static int SlotOfClass(string classId)
        {
            foreach (KeyValuePair<int, SaveData> e in LoadAll()) if (e.Value.classId == classId) return e.Key;
            return -1;
        }

        /// <summary>Null when `name` may be given to a new hero on this device: the online rules, and no saved hero has it (case-insensitive).</summary>
        public static string NameProblem(string name)
        {
            var taken = new List<string>();
            foreach (KeyValuePair<int, SaveData> e in LoadAll()) taken.Add(DisplayName(e.Value));
            return HeroNameRules.Problem(name, taken);
        }

        public static void Delete(int slot)
        {
            if (!IsSlot(slot)) return;
            try { string p = PathForSlot(slot); if (File.Exists(p)) File.Delete(p); } catch (Exception) { /* best effort */ }
        }

        static SaveData ReadSlot(int slot)
        {
            string path = PathForSlot(slot);
            try
            {
                if (!File.Exists(path)) return null;
                SaveData d = SaveSerializer.Parse(File.ReadAllText(path), FrostHollowRows);
                if (d == null) File.Copy(path, path + ".bad", true); // keep the broken file for inspection; the slot reads as empty
                return d;
            }
            catch (Exception e) { Warn("load failed: " + e.Message); return null; }
        }

        /// <summary>Temp file, then move: a crash leaves either the old file or the new one, never half of it.</summary>
        static void WriteText(string path, string text)
        {
            string tmp = path + ".tmp";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(tmp, text);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }
    }
}
