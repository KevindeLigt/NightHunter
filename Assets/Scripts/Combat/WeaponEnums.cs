using UnityEngine;

namespace NightHunter.combat
{
    public enum WeaponId : ushort
    {
        None = 0,
        Pistol = 1,
        Crossbow = 2,
        Stake = 3,
    }

    public enum WeaponKind : byte
    {
        Ranged = 0,
        Hitscan = 1,
        Melee = 2,
        Utility = 3
    }
}
