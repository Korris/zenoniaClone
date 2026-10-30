namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Specialised stats granted by the equipped second weapon (offhand) at its enhance level. Fractions (0.3 = +30%), except CritRate / CritDmg
    /// which are added points. Only the owning class's keys are non-zero. Derived in PlayerState.RecomputeStats, never stored.
    /// </summary>
    public struct SecondWeaponStats
    {
        public bool Equipped;               // offhand worn: signature combos are active even at +0
        public int Level;                   // enhance level 0..15
        public double F;                    // curve value 0..1 (SecondWeapon.Curve)
        public double DefPct, HpPct, DmgReduction;          // warrior Kite Shield
        public double MpPct, CastSpeed, MpCostCut;          // wizard Spellbook (CastSpeed = cast lock reduction)
        public double RobotDmg, RobotRate;                  // mechanic Drone (RobotRate: interval / (1 + x))
        public double CritRate, CritDmg;                    // assassin Off-hand Dagger (points added)
        public double HealPower, ShieldPower;               // cleric Spirit

        public static readonly SecondWeaponStats Zero = new SecondWeaponStats();
    }
}
