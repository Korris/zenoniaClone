using UnityEngine;

/// <summary>
/// ScriptableObject holding per-equipment config: sprites + stat modifiers.
/// Sprites[] must match body sprite sheet frame order exactly.
/// Create via: right-click Project → Create → Equipment → EquipmentData
/// </summary>
[CreateAssetMenu(fileName = "NewEquipment", menuName = "Equipment/EquipmentData")]
public class EquipmentData : ScriptableObject
{
    [Header("Identity")]
    public string DisplayName;
    public EquipmentSlotType SlotType;

    [Header("Sprites")]
    [Tooltip("Must match body sprite sheet frame order exactly (same grid layout as character.png)")]
    public Sprite[] Sprites;

    [Header("Stats")]
    public int AttackBonus;
    public int DefenseBonus;
}
