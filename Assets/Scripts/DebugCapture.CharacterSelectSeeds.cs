using System.Collections;
using System.IO;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>Helpers of --character-select-test: seeded saves (slot files and pre-slot class files), the migration checks, and a held pointer.</summary>
    public partial class DebugCapture
    {
        /// <summary>Removes every save, old class save, slot file and backup in the run's own --save-root (never the player's folder: saves are off without it).</summary>
        static void ClearSaveRoot()
        {
            if (!SaveStore.Enabled || SaveStore.RootOverride == null || !Directory.Exists(SaveStore.Root)) return;
            foreach (string f in Directory.GetFiles(SaveStore.Root, "zenonia-save-*")) File.Delete(f);
            foreach (string f in Directory.GetFiles(SaveStore.Root, "zenonia-slot-*")) File.Delete(f);
        }

        static SaveData SeedData(string classId, int level, int style, int hair, int outfit)
        {
            PlayerState p = PlayerState.NewGame(classId);
            p.Level = level;
            p.Appearance = CharacterAppearanceData.ForNewCharacter(classId, style, hair, outfit);
            return SaveSerializer.Capture(p, false, 0f, 0f, false, 0, "frostHollow");
        }

        /// <summary>A named hero saved in a slot, as if played before.</summary>
        static void Seed(int slot, string classId, int level, int style, int hair, int outfit, string name)
        {
            SaveData d = SeedData(classId, level, style, hair, outfit);
            d.name = name;
            SaveStore.Write(d, slot);
        }

        /// <summary>A save written the old way: zenonia-save-&lt;class&gt;.json without a name, and the slot it was shown in (zenonia-slot-&lt;class&gt;.txt) when slot &gt;= 0.</summary>
        static void SeedLegacy(string classId, int level, int style, int hair, int outfit, int slot)
        {
            File.WriteAllText(SaveStore.LegacyPathFor(classId), SaveSerializer.ToJson(SeedData(classId, level, style, hair, outfit)));
            if (slot >= 0) File.WriteAllText(SaveStore.LegacySlotPathFor(classId), slot.ToString());
        }

        /// <summary>After the list reloaded: the old files are backups now, each save is in its slot with its level, the slot screen places them there.</summary>
        static void CheckMigrated()
        {
            bool gone = !File.Exists(SaveStore.LegacyPathFor("mechanic")) && !File.Exists(SaveStore.LegacyPathFor("wizard")) && !File.Exists(SaveStore.LegacySlotPathFor("mechanic"));
            bool kept = File.Exists(SaveStore.LegacyPathFor("mechanic") + SaveStore.MigratedSuffix) && File.Exists(SaveStore.LegacyPathFor("wizard") + SaveStore.MigratedSuffix) && File.Exists(SaveStore.LegacySlotPathFor("mechanic") + SaveStore.MigratedSuffix);
            Cs(Pf(gone && kept) + " old class saves are moved, the old files kept as " + SaveStore.MigratedSuffix);
            SaveData m = SaveStore.Load(4), w = SaveStore.Load(0);
            Cs(Pf(m != null && m.classId == "mechanic" && m.level == 23 && w != null && w.classId == "wizard" && w.level == 41) + " the mechanic keeps its remembered slot 4, the wizard takes the first free slot 0, levels kept");
            Cs(Pf(CharacterListScreen.TestSlotOf("Mechanic") == 4 && CharacterListScreen.TestSlotOf("Wizard") == 0 && CharacterListScreen.TestSlotOf("Shade") == 2) + " the slot screen shows them in those slots, unnamed old heroes under their class name");
        }

        /// <summary>Holds the pointer down at a screen point for `seconds` (the hold-to-delete button). release = let go afterwards; false keeps it down for the next call.</summary>
        IEnumerator PressHold(Vector2 screen, float seconds, string what, bool release)
        {
            if (fakeInput == null)
            {
                fakeInput = UnityEngine.EventSystems.EventSystem.current.gameObject.AddComponent<FakeInput>();
                UnityEngine.EventSystems.EventSystem.current.currentInputModule.inputOverride = fakeInput;
            }
            UnityEngine.EventSystems.EventSystem.current.SendMessage("OnApplicationFocus", true);
            fakeInput.UseTouch = false;
            fakeInput.Pos = screen;
            if (fakeInput.UpFrame != int.MaxValue)
            {
                yield return null;
                fakeInput.DownFrame = Time.frameCount + 1;
                fakeInput.UpFrame = int.MaxValue;
            }
            yield return WaitSeconds(seconds);
            if (release)
            {
                fakeInput.UpFrame = Time.frameCount + 1;
                while (Time.frameCount <= fakeInput.UpFrame + 1) yield return null;
            }
            Cs("hold " + what + " at " + screen);
        }
    }
}
