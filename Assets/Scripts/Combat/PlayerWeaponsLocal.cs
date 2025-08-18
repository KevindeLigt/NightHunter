using UnityEngine;

namespace NightHunter.combat
{
    [RequireComponent(typeof(Collider))]
    public class ProjectileSimple : MonoBehaviour
    {
        int damage;
        float speed;
        float life;
        float hitImpulse;
        Vector3 dir;
        float deathAt;

        public void Initialize(Vector3 direction, float projectileSpeed, int dmg, float lifetime, float impulse)
        {
            dir = direction.normalized;
            speed = projectileSpeed;
            damage = dmg;
            life = Mathf.Max(0.1f, lifetime);
            hitImpulse = impulse;
            deathAt = Time.time + life;

            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        void Update()
        {
            transform.position += dir * speed * Time.deltaTime;
            if (Time.time >= deathAt) Destroy(gameObject);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other) return;

            var hp = other.GetComponentInParent<Health>();
            if (hp) hp.TakeDamage(damage);

            var rb = other.attachedRigidbody;
            if (rb) rb.AddForce(dir * hitImpulse, ForceMode.Impulse);
            else
            {
                var kb = other.GetComponentInParent<KnockbackReceiver>();
                if (kb) kb.AddImpact(dir, hitImpulse);
            }

            Destroy(gameObject);
        }
    }
}
