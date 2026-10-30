using System.Collections.Generic;
using System.Globalization;

namespace ZenoniaProto.Rpg
{
    /// <summary>Display text of a second weapon's specialised stats (enhance preview, loadout panel). Pure.</summary>
    public static class SecondWeaponText
    {
        /// <summary>(label, value) pairs of the class's stats in table order; percent values are fractions.</summary>
        public static List<KeyValuePair<string, double>> Stats(string classId, SecondWeaponStats s)
        {
            var l = new List<KeyValuePair<string, double>>();
            switch (RpgClasses.Get(classId).Id)
            {
                case "warrior": Add(l, "DEF", s.DefPct); Add(l, "Max HP", s.HpPct); Add(l, "Damage taken -", s.DmgReduction); break;
                case "wizard": Add(l, "Max MP", s.MpPct); Add(l, "Cast lock -", s.CastSpeed); Add(l, "MP cost -", s.MpCostCut); break;
                case "mechanic": Add(l, "Robot damage", s.RobotDmg); Add(l, "Robot fire rate", s.RobotRate); break;
                case "assassin": Add(l, "Crit rate", s.CritRate); Add(l, "Crit damage", s.CritDmg); break;
                case "cleric": Add(l, "Healing", s.HealPower); Add(l, "Shield", s.ShieldPower); break;
            }
            return l;
        }

        static void Add(List<KeyValuePair<string, double>> l, string label, double v) { l.Add(new KeyValuePair<string, double>(label, v)); }

        /// <summary>"+3.6%" (one decimal at most).</summary>
        public static string Pct(double fraction) { return "+" + (fraction * 100.0).ToString("0.#", CultureInfo.InvariantCulture) + "%"; }

        /// <summary>One line per stat, e.g. "Crit rate +1.8% -> +2.4%", between enhance levels `from` and `to`.</summary>
        public static List<string> Lines(string classId, int from, int to)
        {
            List<KeyValuePair<string, double>> a = Stats(classId, SecondWeapon.For(classId, from)), b = Stats(classId, SecondWeapon.For(classId, to));
            var lines = new List<string>();
            for (int i = 0; i < a.Count; i++) lines.Add(a[i].Key + " " + Pct(a[i].Value) + " -> " + Pct(b[i].Value));
            return lines;
        }

        /// <summary>Stat lines of an offhand now -> next level, joined with newlines.</summary>
        public static string Preview(Item item)
        {
            int level = EnhanceStats.Level(item);
            return string.Join("\n", Lines(item.classId, level, level + 1).ToArray());
        }
    }
}
