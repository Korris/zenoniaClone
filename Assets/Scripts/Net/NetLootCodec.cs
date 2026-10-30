using System;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Loot on the wire. Everyone in the area of interest only needs what draws the drop (kind, icon, rarity glow), not the full item: equipment packs
    /// slot (3 bits) + class (3 bits) + item level (7 bits) into ItemDefId, plus the rarity index and enhance level. The picker receives the real item
    /// through its own RpgState push.
    /// </summary>
    public static class NetLootCodec
    {
        public static void Fill(ref SpawnMsg m, LootEntry e)
        {
            m.Kind = EntityKind.Loot;
            m.Type = (byte)e.Kind;
            m.Amount = e.Value;
            if (e.Kind != LootKind.Equipment || e.Item == null) return;
            Item it = e.Item;
            int slot = Math.Max(0, Slots.Index(it.slot)), cls = Math.Max(0, Array.IndexOf(RpgClasses.Ids, it.classId)), lvl = Math.Max(0, Math.Min(127, it.itemLevel));
            m.ItemDefId = (ushort)(slot | (cls << 3) | (lvl << 6));
            m.Rarity = (byte)Math.Max(0, Array.IndexOf(ItemDefs.RarityOrder, it.rarity));
            m.Enhance = (byte)Math.Max(0, Math.Min(255, it.enhance));
        }

        /// <summary>Client view of a drop; false for an out-of-range kind.</summary>
        public static bool TryToEntry(SpawnMsg m, out LootEntry e)
        {
            e = default(LootEntry);
            if (m.Type > (byte)LootKind.Equipment) return false;
            e.Kind = (LootKind)m.Type;
            e.Value = m.Amount;
            if (e.Kind != LootKind.Equipment) return true;
            int slot = m.ItemDefId & 7, cls = (m.ItemDefId >> 3) & 7, lvl = m.ItemDefId >> 6;
            e.Item = new Item
            {
                uid = "net", templateId = "net", name = "Item", slot = Slots.All[Math.Min(slot, Slots.All.Length - 1)], classId = RpgClasses.Ids[Math.Min(cls, RpgClasses.Ids.Length - 1)],
                itemLevel = lvl, enhance = m.Enhance, rarity = ItemDefs.RarityOrder[Math.Min(m.Rarity, ItemDefs.RarityOrder.Length - 1)],
            };
            return true;
        }
    }
}
