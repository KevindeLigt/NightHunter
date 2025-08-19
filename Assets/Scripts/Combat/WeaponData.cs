using UnityEngine;

namespace NightHunter.combat
{
    [CreateAssetMenu(menuName = "NightHunter/Weapon Data", fileName = "NewWeaponData")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public WeaponId id = WeaponId.None;
        public string displayName = "Unnamed Weapon";
        public WeaponKind kind = WeaponKind.Ranged;
        public Sprite icon;

        [Header("View (held model)")]
        public GameObject viewPrefab;   // the in-hand model with an Animator + a child "Muzzle"


        [Header("Core Stats")]
        public int damage = 20;
        public float fireCooldown = 0.25f;
        public float reloadTime = 1.4f;
        public float range = 30f;

        [Header("Ammo (optional)")]
        public bool usesAmmo = false;
        public int clipSize = 1;
        public int maxReserveAmmo = 0;

        [Header("Projectile (for Ranged)")]
        public GameObject projectilePrefab;
        public float projectileSpeed = 26f;
        public float projectileLifetime = 5f;

        [Header("FX (optional)")]
        public AudioClip fireSfx;
        public AudioClip reloadSfx;
        public GameObject muzzleFxPrefab;
    }
}
