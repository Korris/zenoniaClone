using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    public struct ActionResult
    {
        public bool Ok; public string Text;
        public static ActionResult Success(string text = "") { return new ActionResult { Ok = true, Text = text }; }
        public static ActionResult Fail(string text) { return new ActionResult { Ok = false, Text = text }; }
    }

    /// <summary>
    /// The hero's RPG data and rules: level / EXP, base + equipment stat aggregation, resources, potions, materials, bag and the six equipment
    /// slots (web entities/player.js + equipment-system.js + loot-system.js applyPickup/usePotion). No Unity types: fully unit-testable.
    /// </summary>
    public sealed partial class PlayerState
    {
        public const int InventorySize = 20;
        public const int MaxStones = 9999;

        public string ClassId { get; private set; }
        public int Level = 1, Exp, Gold, HpPotions, MpPotions, UpgradeStone;
        public float Hp, Mp;
        public readonly List<Item> Inventory = new List<Item>();
        readonly Item[] equipment = new Item[Slots.All.Length];
        public readonly SkillRanks Skills;      // learned skill ranks; SP is derived (SkillPointsFree), never stored
        public readonly SkillLoadout Loadout;   // skill magazine slots

        // Derived by RecomputeStats().
        public int MaxHp, MaxMp, Atk, Def;
        public float Speed, CritBonus; // CritBonus = equipment crit + class bonus + second-weapon crit rate, added to the hero's base crit chance
        public SecondWeaponStats Second; // offhand-derived specialised stats (Zero when no offhand is worn)

        public PlayerState(string classId)
        {
            ClassId = RpgClasses.Get(classId).Id;
            Skills = new SkillRanks(ClassId); Loadout = new SkillLoadout();
            RecomputeStats();
            Hp = MaxHp; Mp = MaxMp;
        }

        /// <summary>New game: level 1 with the class starter weapon + offhand equipped.</summary>
        public static PlayerState NewGame(string classId)
        {
            var p = new PlayerState(classId);
            p.EquipStarterGear();
            p.Hp = p.MaxHp; p.Mp = p.MaxMp; // starter gear can raise the maxima: start full
            p.LearnSkill(0); // the Lv 1 point goes into the first skill so the skill button is never empty
            return p;
        }

        public int SkillPointsFree { get { return Skills.Free(Level); } }

        /// <summary>Spends 1 SP on a skill (learn or rank up) and auto-places a newly learned one in the magazine.</summary>
        public LearnResult LearnSkill(int index)
        {
            LearnResult r = Skills.Learn(SkillTable.Get(ClassId, index), Level);
            if (r == LearnResult.Ok && Skills[index] == 1) Loadout.AutoFill(index);
            return r;
        }

        public Item GetEquipped(string slot) { int i = Slots.Index(slot); return i >= 0 ? equipment[i] : null; }
        public void SetEquippedRaw(string slot, Item item) { int i = Slots.Index(slot); if (i >= 0) equipment[i] = item; }

        /// <summary>Recomputes derived stats from level + equipment (class passives do not exist in the prototype: multipliers are 1).</summary>
        public void RecomputeStats()
        {
            BaseStats b = Leveling.StatsForLevel(Level, ClassId);
            int atkAdd = 0, defAdd = 0, hpAdd = 0, mpAdd = 0;
            double speedMult = 1, crit = 0;
            foreach (Item item in equipment)
            {
                if (item == null) continue;
                ItemStats s = EnhanceStats.Effective(item);
                atkAdd += s.atk; defAdd += s.def; hpAdd += s.maxHp; mpAdd += s.maxMp; speedMult += s.speedMult; crit += s.critChance;
            }
            Item off = GetEquipped(Slots.Offhand);
            Second = off != null ? SecondWeapon.For(ClassId, EnhanceStats.Level(off)) : SecondWeaponStats.Zero;
            MaxHp = RpgMath.Round((b.MaxHp + hpAdd) * (1 + Second.HpPct)); MaxMp = RpgMath.Round((b.MaxMp + mpAdd) * (1 + Second.MpPct));
            Atk = b.Atk + atkAdd; Def = RpgMath.Round((b.Def + defAdd) * (1 + Second.DefPct));
            Speed = (float)(b.Speed * speedMult);
            CritBonus = (float)(crit + RpgClasses.Get(ClassId).CritBonus + Second.CritRate);
            if (Hp > MaxHp) Hp = MaxHp;
            if (Mp > MaxMp) Mp = MaxMp;
        }

        /// <summary>Adds EXP; each level-up recomputes stats and fully heals. Returns levels gained.</summary>
        public int GainExp(int amount)
        {
            ExpResult r = Leveling.ApplyExpGain(Level, Exp, amount);
            Level = r.Level; Exp = r.Exp;
            if (r.LevelsGained > 0) { RecomputeStats(); Hp = MaxHp; Mp = MaxMp; }
            return r.LevelsGained;
        }

        /// <summary>Death: lose 10% of the current-level EXP (never a level), restore resources. Returns the EXP lost.</summary>
        public int Revive()
        {
            int lost = Leveling.DeathPenalty(Exp);
            Exp -= lost;
            Hp = MaxHp; Mp = MaxMp;
            return lost;
        }
    }
}
