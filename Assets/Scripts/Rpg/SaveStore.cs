using System;
using System.IO;
using UnityEngine;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// One JSON file per class in Application.persistentDataPath (zenonia-save-&lt;class&gt;.json). Writes go through a temp file so a crash
    /// never leaves a half-written save; an unreadable file is kept as .bad and ignored. Disable with Enabled = false (screenshot / smoke runs).
    /// </summary>
    public static class SaveStore
    {
        public static bool Enabled = true;
        /// <summary>Test hook: write somewhere else than persistentDataPath.</summary>
        public static string RootOverride;

        public static string Root { get { return RootOverride ?? Application.persistentDataPath; } }
        public static string PathFor(string classId) { return Path.Combine(Root, "zenonia-save-" + RpgClasses.Get(classId).Id + ".json"); }

        public static bool Write(SaveData data)
        {
            if (!Enabled || data == null) return false;
            try
            {
                string path = PathFor(data.classId), tmp = path + ".tmp";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(tmp, SaveSerializer.ToJson(data));
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
                return true;
            }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] save failed: " + e.Message); return false; }
        }

        /// <summary>The validated save of a class, or null (no file, unreadable, or belongs to another class).</summary>
        public static SaveData Load(string classId)
        {
            if (!Enabled) return null;
            string path = PathFor(classId);
            try
            {
                if (!File.Exists(path)) return null;
                SaveData d = SaveSerializer.Parse(File.ReadAllText(path));
                if (d == null)
                {
                    File.Copy(path, path + ".bad", true); // keep the broken file for inspection, start fresh
                    return null;
                }
                return d.classId == RpgClasses.Get(classId).Id ? d : null;
            }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] load failed: " + e.Message); return null; }
        }

        public static void Delete(string classId)
        {
            try { string p = PathFor(classId); if (File.Exists(p)) File.Delete(p); } catch (Exception) { /* best effort */ }
        }
    }
}
