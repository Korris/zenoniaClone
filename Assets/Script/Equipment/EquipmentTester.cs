using UnityEngine;

/// <summary>
/// Temporary test harness for equipment system.
/// Attach to Player GO, assign TestWeapon in Inspector.
/// Press E to equip, U to unequip. DELETE after validation.
/// </summary>
public class EquipmentTester : MonoBehaviour
{
    public EquipmentData TestWeapon;
    public EquipmentData TestArmor;
    public EquipmentData TestHelmet;
    private EquipmentManager _manager;

    private void Start()
    {
        _manager = GetComponent<EquipmentManager>();
    }

    private void Update()
    {
        if (_manager == null) return;

        // E = equip test weapon
        if (Input.GetKeyDown(KeyCode.E) && TestWeapon != null)
        {
            _manager.Equip(TestWeapon);
            Debug.Log($"[EquipTest] Equipped {TestWeapon.DisplayName}. ATK bonus: {_manager.GetAttackBonus()}");
        }

        // R = equip test armor
        if (Input.GetKeyDown(KeyCode.R) && TestArmor != null)
        {
            _manager.Equip(TestArmor);
            Debug.Log($"[EquipTest] Equipped {TestArmor.DisplayName}. DEF bonus: {_manager.GetDefenseBonus()}");
        }

        // T = equip test helmet
        if (Input.GetKeyDown(KeyCode.T) && TestHelmet != null)
        {
            _manager.Equip(TestHelmet);
            Debug.Log($"[EquipTest] Equipped {TestHelmet.DisplayName}");
        }

        // U = unequip all
        if (Input.GetKeyDown(KeyCode.U))
        {
            _manager.Unequip(EquipmentSlotType.Weapon);
            _manager.Unequip(EquipmentSlotType.Armor);
            _manager.Unequip(EquipmentSlotType.Helmet);
            Debug.Log("[EquipTest] Unequipped all slots.");
        }
    }
}
