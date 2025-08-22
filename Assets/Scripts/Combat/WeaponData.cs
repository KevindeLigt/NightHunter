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

        // In WeaponData.cs
        [Header("Feel")]
        public float recoilUp = 2f;           // degrees pitch up
        public float recoilBack = 0.04f;      // meters back (camera kick)
        public float shotShakeAmplitude = 0.35f;
        public float shotShakeDuration = 0.08f;
        public float fovPunch = 2f;           // +FOV on shot (snappy)
        public float fovReturnSpeed = 8f;     // how fast it returns

        [Header("Impact VFX/SFX")]
        public GameObject impactFxPrefab;     // bullet hit / blood spark
        public AudioClip impactSfx;

        [Header("Melee Feel")]
        public float meleeLungeDistance = 1.2f;
        public float meleeLungeTime = 0.08f;
        public float meleeHitstop = 0.05f;    // optional tiny slowdown
        public AudioClip meleeWhooshSfx;
        public GameObject meleeHitFxPrefab;

    }
}
