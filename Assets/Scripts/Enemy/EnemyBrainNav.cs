using UnityEngine;
using UnityEngine.AI;

public class EnemyBrainNav : MonoBehaviour
{
    [Header("Perception")]
    [SerializeField] float detectRange = 18f;
    [SerializeField] float fovAngle = 120f;     // degrees
    [SerializeField] float lostSightTime = 2f;  // seconds after losing LOS before giving up

    [Header("Movement")]
    [SerializeField] float stopDistance = 1.8f;

    [Header("Attack")]
    [SerializeField] float attackRange = 1.6f;
    [SerializeField] float attackWindup = 0.25f;
    [SerializeField] float attackCooldown = 0.8f;
    [SerializeField] int attackDamage = 10;
    [SerializeField] Transform attackOrigin;    // empty at chest/hands height
    [SerializeField] float attackRadius = 0.7f;
    [SerializeField] LayerMask playerMask = ~0; // set to your Player layer if you have one
    [SerializeField] private EnemyConfig config;

    [Header("Anim (optional)")]
    [SerializeField] Animator anim;             // bool "Move", trigger "Attack", trigger "Die"

    NavMeshAgent agent;
    Transform target;
    float lastSeenTime = -999f;
    float nextAttackTime;

    enum State { Idle, Chase, Attack }
    State state;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (config)
        {
            // Perception
            detectRange = config.detectRange;
            fovAngle = config.fovAngle;
            lostSightTime = config.lostSightTime;

            // Movement
            stopDistance = config.stopDistance;

            // Attack
            attackRange = config.attackRange;
            attackWindup = config.attackWindup;
            attackCooldown = config.attackCooldown;
            attackDamage = config.attackDamage;

            if (agent)
            {
                agent.speed = config.moveSpeed;
                agent.acceleration = config.acceleration;
                agent.angularSpeed = config.angularSpeed;
                agent.stoppingDistance = stopDistance;
            }
        }


        if (!anim) anim = GetComponentInChildren<Animator>();
        if (!attackOrigin) attackOrigin = transform; // fallback
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (agent) agent.stoppingDistance = stopDistance;
    }

    void Update()
    {
        if (!target) return;

        bool canSee = CanSeeTarget();
        if (canSee) lastSeenTime = Time.time;

        float dist = Vector3.Distance(transform.position, target.position);

        switch (state)
        {
            case State.Idle:
                if (canSee || dist <= detectRange) state = State.Chase;
                break;

            case State.Chase:
                if (anim) anim.SetBool("Move", true);
                if (agent && agent.isOnNavMesh) agent.SetDestination(target.position);

                if (dist <= Mathf.Max(stopDistance, attackRange))
                {
                    if (Time.time >= nextAttackTime)
                        StartCoroutine(DoAttack());
                }

                // lose interest after a while
                if (Time.time - lastSeenTime > lostSightTime && dist > detectRange * 1.2f)
                {
                    if (anim) anim.SetBool("Move", false);
                    state = State.Idle;
                    if (agent) agent.ResetPath();
                }
                break;

            case State.Attack:
                // handled in coroutine, but keep facing target
                Face(target.position);
                break;
        }
    }

    System.Collections.IEnumerator DoAttack()
    {
        state = State.Attack;
        if (agent) agent.isStopped = true;
        if (anim) anim.SetTrigger("Attack");

        // windup
        float t = 0f;
        while (t < attackWindup)
        {
            Face(target.position);
            t += Time.deltaTime;
            yield return null;
        }

        // hitbox
        Vector3 center = attackOrigin ? attackOrigin.position : transform.position + transform.forward * 0.8f;
        var hits = Physics.OverlapSphere(center, attackRadius, playerMask, QueryTriggerInteraction.Collide);
        foreach (var h in hits)
        {
            if (!h) continue;
            if (!h.CompareTag("Player")) continue;

            var hp = h.GetComponentInParent<Health>();
            if (hp) hp.TakeDamage(attackDamage);
        }

        nextAttackTime = Time.time + attackCooldown;

        if (agent) agent.isStopped = false;
        if (anim) anim.SetBool("Move", false);
        state = State.Chase;
    }

    bool CanSeeTarget()
    {
        Vector3 to = (target.position - transform.position);
        float dist = to.magnitude;
        if (dist > detectRange) return false;

        Vector3 dir = to / Mathf.Max(0.001f, dist);
        float ang = Vector3.Angle(transform.forward, dir);
        if (ang > fovAngle * 0.5f) return false;

        // line of sight
        if (Physics.Raycast(transform.position + Vector3.up * 1.6f, dir, out var hit, detectRange, ~0, QueryTriggerInteraction.Ignore))
            return hit.transform == target;

        return false;
    }

    void Face(Vector3 worldPos)
    {
        Vector3 flat = worldPos - transform.position; flat.y = 0f;
        if (flat.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(flat);
    }

    void OnDrawGizmosSelected()
    {
        // view & detection
        Gizmos.color = new Color(1, 1, 0, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectRange);

        // attack area
        Gizmos.color = Color.red;
        Vector3 c = attackOrigin ? attackOrigin.position : transform.position + transform.forward * 0.8f;
        Gizmos.DrawWireSphere(c, attackRadius);

        // FOV arc (scene-only)
        Vector3 left = Quaternion.Euler(0, -fovAngle * 0.5f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, +fovAngle * 0.5f, 0) * transform.forward;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + left * detectRange);
        Gizmos.DrawLine(transform.position, transform.position + right * detectRange);
    }
}
