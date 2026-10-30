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
    public sealed class PlayerState
    {
        public const int InventorySize = 20;
        public const double PotionHealFraction = 0.3;
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
        public float Speed, CritBonus; // CritBonus = equipment crit + class bonus, added to the hero's base crit chance

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
            MaxHp = b.MaxHp + hpAdd; MaxMp = b.MaxMp + mpAdd; Atk = b.Atk + atkAdd; Def = b.Def + defAdd;
            Speed = (float)(b.Speed * speedMult);
            CritBonus = (float)(crit + RpgClasses.Get(ClassId).CritBonus);
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

        // ---- bag + equipment ----

        /// <summary>Class gear (weapon / offhand carry classId) only fits its own class; armor fits everyone.</summary>
        public bool CanEquip(Item item) { return string.IsNullOrEmpty(item.classId) || item.classId == ClassId; }

        public bool AddToInventory(Item item)
        {
            if (Inventory.Count >= InventorySize) return false;
            Inventory.Add(item);
            return true;
        }

        public bool RemoveFromInventory(string uid)
        {
            int i = Inventory.FindIndex(x => x.uid == uid);
            if (i < 0) return false;
            Inventory.RemoveAt(i);
            return true;
        }

        /// <summary>Equips an item (from the bag or fresh), swapping the previous one into the bag. Recomputes stats on success.</summary>
        public ActionResult Equip(Item item)
        {
            int slot = Slots.Index(item.slot);
            if (slot < 0) return ActionResult.Fail("Invalid slot");
            if (!CanEquip(item)) return ActionResult.Fail("Wrong class");
            bool inBag = Inventory.Exists(i => i.uid == item.uid);
            Item previous = equipment[slot];
            int after = Inventory.Count - (inBag ? 1 : 0) + (previous != null ? 1 : 0);
            if (after > InventorySize) return ActionResult.Fail("Inventory full");
            if (inBag) RemoveFromInventory(item.uid);
            equipment[slot] = item;
            if (previous != null) Inventory.Add(previous);
            RecomputeStats();
            return ActionResult.Success();
        }

        /// <summary>Unequips a slot back into the bag (fails when the bag is full).</summary>
        public ActionResult Unequip(string slot)
        {
            int i = Slots.Index(slot);
            if (i < 0 || equipment[i] == null) return ActionResult.Fail("Empty slot");
            if (Inventory.Count >= InventorySize) return ActionResult.Fail("Inventory full");
            Inventory.Add(equipment[i]);
            equipment[i] = null;
            RecomputeStats();
            return ActionResult.Success();
        }

        /// <summary>Fills EMPTY weapon / offhand slots with the class starter (never replaces gear, never duplicates a starter in the bag).</summary>
        public bool EquipStarterGear()
        {
            bool changed = false;
            foreach (string slot in new[] { Slots.Weapon, Slots.Offhand })
            {
                if (GetEquipped(slot) != null || Inventory.Exists(i => i.uid == "starter-" + slot)) continue;
                SetEquippedRaw(slot, ItemFactory.CreateStarterItem(slot, ClassId));
                changed = true;
            }
            if (changed) RecomputeStats();
            return changed;
        }

        // ---- consumables ----

        /// <summary>Drinks a potion only when it can restore something. Heals 30% of max.</summary>
        public ActionResult UsePotion(bool hp)
        {
            int count = hp ? HpPotions : MpPotions;
            if (count <= 0) return ActionResult.Fail("No potion");
            float cur = hp ? Hp : Mp, max = hp ? MaxHp : MaxMp;
            if (cur >= max) return ActionResult.Fail(hp ? "HP full" : "MP full");
            float v = System.Math.Min(max, cur + RpgMath.Round(max * PotionHealFraction));
            if (hp) { Hp = v; HpPotions--; } else { Mp = v; MpPotions--; }
            return ActionResult.Success((hp ? "HP +" : "MP +") + RpgMath.Round(max * PotionHealFraction));
        }

        /// <summary>Applies a picked-up drop (web applyPickup). Equipment fails with "Inventory full" when the bag has no room.</summary>
        public ActionResult ApplyPickup(LootEntry drop)
        {
            switch (drop.Kind)
            {
                case LootKind.Coin: Gold += drop.Value; return ActionResult.Success("+" + drop.Value + " G");
                case LootKind.HpPotion: HpPotions++; return ActionResult.Success("+1 HP Potion");
                case LootKind.MpPotion: MpPotions++; return ActionResult.Success("+1 MP Potion");
                case LootKind.UpgradeStone:
                    int n = System.Math.Max(1, drop.Value);
                    UpgradeStone = System.Math.Min(MaxStones, UpgradeStone + n);
                    return ActionResult.Success("+" + n + " Upgrade Stone" + (n > 1 ? "s" : ""));
                case LootKind.Equipment:
                    if (drop.Item == null) return ActionResult.Fail("");
                    return AddToInventory(drop.Item) ? ActionResult.Success(EnhanceStats.DisplayName(drop.Item)) : ActionResult.Fail("Inventory full");
                default: return ActionResult.Fail("");
            }
        }
    }
}
