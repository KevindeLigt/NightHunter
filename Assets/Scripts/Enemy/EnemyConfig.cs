using UnityEngine;

[CreateAssetMenu(menuName = "NightHunter/AI/Enemy Config", fileName = "EC_NewEnemy")]
public class EnemyConfig : ScriptableObject
{
    [Header("Perception")]
    public float detectRange = 18f;
    public float fovAngle = 120f;
    public float lostSightTime = 2f;

    [Header("Movement")]
    public float stopDistance = 1.8f;
    public float moveSpeed = 3.5f;
    public float acceleration = 8f;
    public float angularSpeed = 120f;

    [Header("Attack")]
    public float attackRange = 1.6f;
    public float attackWindup = 0.25f;
    public float attackCooldown = 0.8f;
    public int attackDamage = 10;
}
