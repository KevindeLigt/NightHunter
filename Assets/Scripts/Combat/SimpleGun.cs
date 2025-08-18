using UnityEngine;

public class SimpleGun : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private float fireRate = 9f;
    [SerializeField] private float range = 60f;
    [SerializeField] private int damage = 20;

    [Header("Feedback")]
    [SerializeField] private CameraShaker shaker;
    [SerializeField] private float shakeIntensity = 0.18f;
    [SerializeField] private float shakeDuration = 0.06f;

    [Header("Hit Reaction")]
    [SerializeField] private float knockbackImpulse = 6f; // tweak per taste

    float nextFireTime;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!shaker && cam) shaker = cam.GetComponent<CameraShaker>();
    }

    void Update()
    {
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        if (shaker) shaker.Kick(shakeIntensity, shakeDuration);

        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        if (Physics.Raycast(origin, dir, out var hit, range, ~0, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.TryGetComponent<Health>(out var hp))
                hp.TakeDamage(damage);

            // Prefer Rigidbody knockback if present
            if (hit.rigidbody)
            {
                hit.rigidbody.AddForce(dir * knockbackImpulse, ForceMode.Impulse);
            }
            else
            {
                // CharacterController enemies: use our receiver
                var kb = hit.collider.GetComponentInParent<KnockbackReceiver>();
                if (kb) kb.AddImpact(dir, knockbackImpulse);
            }

#if UNITY_EDITOR
            Debug.DrawRay(hit.point, hit.normal * 0.3f, Color.red, 0.5f);
#endif
        }

#if UNITY_EDITOR
        Debug.DrawRay(origin, dir * range, Color.yellow, 0.05f);
#endif
    }
}
