using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Test resources on start, read from Assets/Resources/Data/dev-start.json (edit the numbers, save, press Play).
    /// Player builds read dev-start.json placed next to Zenonia.exe instead (see ReadJson).
    /// Every time a class is loaded or created, its upgrade stones / gold / potions are topped UP to these values (never lowered),
    /// so enhancing can be tested right away. The bundled Resources copy only works in the editor and development builds.
    /// </summary>
    [Serializable]
    public class DevStartConfig
    {
        public bool enabled = true;
        public int upgradeStones, gold, hpPotions, mpPotions;
        public int level; // 0 = keep; otherwise the character is raised to this level (skill tests need SP), never lowered
        public int offhandPlus = -1; // -1 = keep; otherwise the worn offhand is set to this enhance level (second-weapon captures / tests)
        public int skillPointBonus; // extra SP on top of the level (SP is level - ranks, so the level cap alone cannot give more)
        public bool noCooldowns; // skills start no cooldown (the cast lock still applies)
        public bool noMpCost; // skills cost no MP
        public bool allSkills; // learn every skill as far as the SP allow (Lv 60 + 20 bonus = all 8 at rank 10, 0 SP left)
        public bool allWeapons; // every art tier of the class's weapon and offhand (epic, +0 so stones stay testable); the best ones are worn on the first grant

        /// <summary>Call before a save is restored: loading trims ranks above the SP budget, so the bonus must already count.</summary>
        public static void ApplySkillBonus()
        {
            string json = ReadJson();
            DevStartConfig c = null;
            try { if (json != null) c = JsonUtility.FromJson<DevStartConfig>(json); }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] dev-start.json ignored: " + e.Message); }
            SkillRanks.DevBonus = c != null && c.enabled ? Mathf.Max(0, c.skillPointBonus) : 0;
            SkillCooldowns.DevNone = c != null && c.enabled && c.noCooldowns;
            SkillCooldowns.DevFreeMp = c != null && c.enabled && c.noMpCost;
        }

        /// <summary>
        /// A dev-start.json next to the player executable (testers drop it there; a shipped build has none) wins; otherwise the bundled
        /// Resources copy, but only in the editor and development builds.
        /// </summary>
        static string ReadJson()
        {
            try
            {
                string beside = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath) ?? ".", "dev-start.json");
                if (!Application.isEditor && System.IO.File.Exists(beside)) return System.IO.File.ReadAllText(beside);
            }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] dev-start.json next to the game unreadable: " + e.Message); }
            if (!Application.isEditor && !Debug.isDebugBuild) return null;
            var asset = Resources.Load<TextAsset>("Data/dev-start");
            return asset != null ? asset.text : null;
        }

        /// <summary>Spends every free SP through the normal learn path, pass after pass so prerequisites settle. Needs level + skillPointBonus for the ranks wanted.</summary>
        static void LearnAllSkills(PlayerState p)
        {
            bool learned = true;
            while (learned)
            {
                learned = false;
                for (int i = 0; i < SkillDef.PerClass; i++) learned |= p.LearnSkill(i) == LearnResult.Ok;
            }
        }

        /// <summary>One epic per art tier (0-9, item level = tier x 10). Fixed uids, so a reload does not grant them again.</summary>
        static void GrantAllWeapons(PlayerState p)
        {
            const int Tiers = 10;
            foreach (string slot in new[] { Slots.Weapon, Slots.Offhand })
            {
                Item best = null;
                for (int tier = 0; tier < Tiers; tier++)
                {
                    string uid = "dev-" + slot + "-" + tier;
                    Item worn = p.GetEquipped(slot);
                    if (p.Inventory.Exists(i => i.uid == uid) || (worn != null && worn.uid == uid)) continue;
                    Item item = ItemFactory.Generate(RpgRandom.Default, Mathf.Max(1, tier * 10), slot, "epic", p.ClassId);
                    item.uid = uid;
                    if (!p.AddToInventory(item)) { Debug.LogWarning("[ZenoniaProto] dev-start: bag full, " + uid + " not granted"); break; }
                    best = item;
                }
                if (best != null) p.Equip(best);
            }
        }

        public static void Apply(PlayerState p)
        {
            if (p == null) return;
            string json = ReadJson();
            if (json == null) return;
            DevStartConfig c;
            try { c = JsonUtility.FromJson<DevStartConfig>(json); }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] dev-start.json ignored: " + e.Message); return; }
            if (c == null || !c.enabled) return;
            p.UpgradeStone = Mathf.Clamp(Mathf.Max(p.UpgradeStone, c.upgradeStones), 0, PlayerState.MaxStones);
            p.Gold = Mathf.Max(p.Gold, c.gold);
            p.HpPotions = Mathf.Max(p.HpPotions, c.hpPotions);
            p.MpPotions = Mathf.Max(p.MpPotions, c.mpPotions);
            if (c.level > p.Level)
            {
                p.Level = Mathf.Min(c.level, Leveling.MaxLevel);
                p.Exp = 0;
                p.RecomputeStats();
                p.Hp = p.MaxHp; p.Mp = p.MaxMp;
            }
            if (c.allSkills) LearnAllSkills(p);
            if (c.allWeapons) { GrantAllWeapons(p); p.Hp = p.MaxHp; p.Mp = p.MaxMp; }
            Item off = p.GetEquipped(Slots.Offhand);
            if (c.offhandPlus >= 0 && off != null) { off.enhance = Mathf.Clamp(c.offhandPlus, 0, EnhanceConfig.MaxEnhance); p.RecomputeStats(); }
            Debug.Log("[ZenoniaProto] dev-start: stones " + p.UpgradeStone + ", gold " + p.Gold + ", potions " + p.HpPotions + "/" + p.MpPotions);
        }
    }
}
