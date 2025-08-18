using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyChaser : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float pushDamagePerHit = 10f;
    [SerializeField] private float hitCooldown = 0.6f;

    CharacterController controller;
    Transform target;
    Vector3 vel;
    float lastHitTime;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (!target) return;

        Vector3 to = (target.position - transform.position);
        to.y = 0f;
        if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);

        Vector3 move = to.normalized * speed;
        controller.Move(move * Time.deltaTime);

        if (controller.isGrounded && vel.y < 0f) vel.y = -2f;
        vel.y += gravity * Time.deltaTime;
        controller.Move(vel * Time.deltaTime);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (Time.time - lastHitTime < hitCooldown) return;
        if (hit.collider.CompareTag("Player"))
        {
            var h = hit.collider.GetComponent<Health>();
            if (h) h.TakeDamage((int)pushDamagePerHit);
            lastHitTime = Time.time;
        }
    }
}
