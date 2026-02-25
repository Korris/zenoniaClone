using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central equipment controller on Player GO.
/// Routes Equip/Unequip calls to correct EquipmentSlot child.
/// Exposes stat bonus aggregation for PlayerController to consume.
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    [Header("Slot References (assign child GOs in Inspector)")]
    public EquipmentSlot HelmetSlot;
    public EquipmentSlot ArmorSlot;
    public EquipmentSlot WeaponSlot;

    private Dictionary<EquipmentSlotType, EquipmentSlot> _slots;

    private void Awake()
    {
        var bodyRenderer = GetComponent<SpriteRenderer>();

        _slots = new Dictionary<EquipmentSlotType, EquipmentSlot>
        {
            { EquipmentSlotType.Helmet, HelmetSlot },
            { EquipmentSlotType.Armor,  ArmorSlot  },
            { EquipmentSlotType.Weapon, WeaponSlot }
        };

        // Init each slot with body SpriteRenderer reference
        foreach (var slot in _slots.Values)
        {
            if (slot != null) slot.Init(bodyRenderer);
            else Debug.LogWarning($"[EquipmentManager] A slot reference is not assigned on {gameObject.name}");
        }
    }

    /// <summary>Routes equipment to the correct slot based on data.SlotType</summary>
    public void Equip(EquipmentData data)
    {
        if (data == null) return;
        if (!_slots.TryGetValue(data.SlotType, out EquipmentSlot slot) || slot == null) return;
        slot.Equip(data);
    }

    /// <summary>Clears the specified equipment slot</summary>
    public void Unequip(EquipmentSlotType slotType)
    {
        if (!_slots.TryGetValue(slotType, out EquipmentSlot slot) || slot == null) return;
        slot.Unequip();
    }

    /// <summary>Sum of AttackBonus from all equipped items</summary>
    public int GetAttackBonus()
    {
        int total = 0;
        foreach (var slot in _slots.Values)
            total += slot?.CurrentEquipment?.AttackBonus ?? 0;
        return total;
    }

    /// <summary>Sum of DefenseBonus from all equipped items</summary>
    public int GetDefenseBonus()
    {
        int total = 0;
        foreach (var slot in _slots.Values)
            total += slot?.CurrentEquipment?.DefenseBonus ?? 0;
        return total;
    }
}
