using UnityEngine;

namespace NightHunter.combat
{
    [CreateAssetMenu(menuName = "NightHunter/Skill Data", fileName = "NewSkillData")]
    public class SkillData : ScriptableObject
    {
        [Header("Identity")]
        public SkillId id = SkillId.None;
        public string displayName = "New Skill";
        public SkillKind kind = SkillKind.ShieldSelf;
        public Sprite icon;

        [Header("Timing")]
        public float cooldown = 8f;    // seconds between casts
        public float duration = 5f;    // active time (shield/dash window)

        // ---- Shield settings ----
        [Header("Shield (ShieldSelf)")]
        public int shieldAbsorb = 50;           // flat absorb pool
        [Range(0f, 1f)] public float shieldDamageReduce = 0.0f; // % reduce after absorb (0..1)
        public int shieldMaxHits = 0;           // 0 = unlimited hits until duration ends or absorb runs out

        // ---- Dash settings ----
        [Header("Dash (DashUpgrade)")]
        public float dashDistance = 6f;         // meters
        public float dashDuration = 0.15f;      // seconds to cover distance
        public bool spawnDecoy = false;
        public GameObject decoyPrefab;          // optional
        public float decoyLifetime = 3f;
    }
}
