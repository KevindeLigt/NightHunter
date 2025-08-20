using UnityEngine;

namespace NightHunter.combat
{
    [CreateAssetMenu(menuName = "NightHunter/Skills/Effects/Damage", fileName = "E_Damage")]
    public class EffectDamageSO : EffectSO
    {
        [Header("Damage")]
        public int damage = 40;

        [Header("Knockback (optional)")]
        public float impulse = 6f;

        public override void OnImpact(AbilityContext ctx, object target)
        {
            // We expect a Collider from DeliveryExplosionSO; support Transform too
            Collider col = target as Collider;
            if (!col && target is Transform tr) col = tr.GetComponent<Collider>();
            if (!col) return;

            // Damage
            var hp = col.GetComponentInParent<Health>();
            if (hp) hp.TakeDamage(damage);

            // Knockback (from center of explosion)
            Vector3 origin = ctx.AimRay.origin; // default if needed
            if (target is Collider c)
                origin = c.bounds.center - (c.bounds.center - (ctx.Caster ? ctx.Caster.position : origin)); // fallback

            // Better origin = explosion center; try to reconstruct from ray hit point or use caster-forward
            // Since DeliveryExplosionSO knows the true center, pass a collider; we approximate direction:
            Vector3 dir = (col.bounds.center - (ctx.Caster ? ctx.Caster.position : Vector3.zero)).normalized;

            var rb = col.attachedRigidbody;
            if (rb) rb.AddForce(dir * impulse, ForceMode.Impulse);
            else
            {
                var kb = col.GetComponentInParent<KnockbackReceiver>();
                if (kb) kb.AddImpact(dir, impulse);
            }
        }
    }
}
