using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NightHunter.combat
{
    // ---------- TARGETING ----------

    [CreateAssetMenu(menuName = "NightHunter/Skills/Targeting/Self", fileName = "T_Self")]
    public class TargetingSelfSO : TargetingSO
    {
        public override void Acquire(AbilityContext ctx, List<object> hits)
        {
            hits.Add(ctx.Caster);
        }
    }

    [CreateAssetMenu(menuName = "NightHunter/Skills/Targeting/AimPoint", fileName = "T_AimPoint")]
    public class TargetingAimPointSO : TargetingSO
    {
        [SerializeField] float maxRange = 30f;
        [SerializeField] bool snapToHit = true;
        [SerializeField] LayerMask overrideMask = ~0; // leave as ~0 to use ctx.HitMask

        public float MaxRange => maxRange;

        public override void Acquire(AbilityContext ctx, List<object> hits)
        {
            var ray = ctx.AimRay;
            float range = Mathf.Max(0.01f, maxRange);
            int mask = (overrideMask == ~0) ? ctx.HitMask : overrideMask;

            Vector3 point = ray.GetPoint(range);
            if (snapToHit && Physics.Raycast(ray, out var hit, range, mask, QueryTriggerInteraction.Ignore))
                point = hit.point;

            hits.Add(point);
        }
    }

    // ---------- DELIVERY ----------

    [CreateAssetMenu(menuName = "NightHunter/Skills/Delivery/Instant", fileName = "D_Instant")]
    public class DeliveryInstantSO : DeliverySO
    {
        public override IEnumerator Execute(AbilityContext ctx, List<object> targets, System.Action<object> onImpact)
        {
            foreach (var t in targets) onImpact?.Invoke(t);
            yield break;
        }
    }

    [CreateAssetMenu(menuName = "NightHunter/Skills/Delivery/Dash", fileName = "D_Dash")]
    public class DeliveryDashSO : DeliverySO
    {
        [SerializeField] float distance = 6f;
        [SerializeField] float duration = 0.15f;
        [SerializeField] bool flatten = true;

        public override IEnumerator Execute(AbilityContext ctx, List<object> targets, System.Action<object> onImpact)
        {
            var cc = ctx.Caster.GetComponent<CharacterController>();
            if (!cc) yield break;

            Vector3 dir = ctx.AimRay.direction.normalized;
            if (flatten) { dir.y = 0f; dir.Normalize(); }

            Vector3 start = ctx.Caster.position;
            Vector3 end = start + dir * Mathf.Max(0f, distance);

            float t = 0f;
            while (t < duration)
            {
                Vector3 pos = Vector3.Lerp(start, end, t / Mathf.Max(0.01f, duration));
                cc.Move(pos - ctx.Caster.position);
                t += Time.deltaTime;
                yield return null;
            }
            cc.Move(end - ctx.Caster.position);
        }
    }

    [CreateAssetMenu(menuName = "NightHunter/Skills/Delivery/Explosion AoE", fileName = "D_Explosion")]
    public class DeliveryExplosionSO : DeliverySO
    {
        [Header("Shape")]
        [SerializeField] float radius = 5f;
        [SerializeField] bool requireLineOfSight = false;
        [SerializeField] float losPadding = 0.1f;

        [Header("Timing")]
        [SerializeField] float fuseSeconds = 0f;
        [SerializeField] int pulses = 1;
        [SerializeField] float pulseInterval = 0.25f;

        [Header("FX (optional)")]
        [SerializeField] GameObject vfxPrefab;
        [SerializeField] AudioClip sfx;
        [SerializeField] float vfxLifetime = 3f;

        [Header("Debug Preview")]
        [SerializeField] bool debugDraw = true;
        [SerializeField] Color debugColor = new Color(1f, 0.6f, 0f, 1f);
        [SerializeField] int debugSegments = 48;
        [SerializeField] float debugPreviewSeconds = 0f; // small flash even if fuse=0

        public float Radius => radius;

        public override IEnumerator Execute(AbilityContext ctx, List<object> targets, System.Action<object> onImpact)
        {
            // center from first target (Vector3/Transform), else caster
            Vector3 center = ctx.Caster.position;
            foreach (var t in targets)
            {
                if (t is Vector3 p) { center = p; break; }
                if (t is Transform tr) { center = tr.position; break; }
            }

            if (vfxPrefab) Object.Instantiate(vfxPrefab, center, Quaternion.identity).AddComponent<AutoDestroy>().Init(vfxLifetime);
            if (sfx) AudioSource.PlayClipAtPoint(sfx, center, 0.9f);

            // Fuse / preview
            if (debugDraw)
            {
                float wait = fuseSeconds > 0f ? fuseSeconds : debugPreviewSeconds;
                for (float t = 0f; t < wait; t += Time.deltaTime)
                {
                    DrawRing(center, radius, debugColor, Time.deltaTime * 1.2f);
                    yield return null;
                }
            }
            else if (fuseSeconds > 0f) { yield return new WaitForSeconds(fuseSeconds); }

            // Pulses
            int count = Mathf.Max(1, pulses);
            for (int i = 0; i < count; i++)
            {
                var cols = Physics.OverlapSphere(center, radius, ctx.HitMask, QueryTriggerInteraction.Collide);
                foreach (var c in cols)
                {
                    if (!c) continue;

                    if (requireLineOfSight)
                    {
                        var dir = (c.bounds.center - center);
                        float dist = dir.magnitude;
                        if (dist > 0.001f)
                        {
                            if (Physics.Raycast(center + dir.normalized * losPadding, dir.normalized, out var hit, dist + 0.01f, ctx.HitMask, QueryTriggerInteraction.Ignore))
                                if (hit.collider != c) continue; // blocked
                        }
                    }

                    onImpact?.Invoke(c); // pass Collider
                }

                if (i < count - 1) yield return new WaitForSeconds(pulseInterval);
            }
        }

        void DrawRing(Vector3 center, float r, Color c, float dur)
        {
            if (r <= 0f) return;
            int segs = Mathf.Max(8, debugSegments);
            Vector3 prev = center + new Vector3(r, 0f, 0f);
            for (int i = 1; i <= segs; i++)
            {
                float a = (i / (float)segs) * Mathf.PI * 2f;
                Vector3 next = center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                Debug.DrawLine(prev, next, c, dur, true);
                prev = next;
            }
        }

        private class AutoDestroy : MonoBehaviour
        {
            float dieAt;
            public void Init(float lifetime) { dieAt = Time.time + Mathf.Max(0.05f, lifetime); }
            void Update() { if (Time.time >= dieAt) Destroy(gameObject); }
        }
    }

    // ---------- EFFECTS ----------

    [CreateAssetMenu(menuName = "NightHunter/Skills/Effects/Shield", fileName = "E_Shield")]
    public class EffectShieldSO : EffectSO
    {
        [Header("Shield")]
        public int absorb = 60;
        [Range(0f, 1f)] public float reducePct = 0.3f;
        public int maxHits = 0;
        public float duration = 5f;

        private class ShieldRuntime : MonoBehaviour, IDamageModifier
        {
            int pool; float reduce; int maxHits; int hits; float endAt; System.Action onEnd;

            public void Init(int absorb, float reducePct, int maxHits, float duration, System.Action onEnd)
            {
                pool = Mathf.Max(0, absorb);
                reduce = Mathf.Clamp01(reducePct);
                this.maxHits = Mathf.Max(0, maxHits);
                endAt = Time.time + Mathf.Max(0f, duration);
                this.onEnd = onEnd;
                StartCoroutine(Life());
            }

            IEnumerator Life()
            {
                while (Time.time < endAt && (maxHits == 0 || hits < maxHits) && (pool > 0 || reduce > 0f))
                    yield return null;

                onEnd?.Invoke();
                Destroy(this);
            }

            public int ModifyIncomingDamage(int incoming)
            {
                if (Time.time >= endAt) return incoming;
                int reduced = Mathf.RoundToInt(incoming * (1f - reduce));
                int afterAbsorb = Mathf.Max(0, reduced - pool);
                pool -= (reduced - afterAbsorb);
                hits++;
                return afterAbsorb;
            }
        }

        public override void OnCast(AbilityContext ctx)
        {
            var r = ctx.Caster.gameObject.AddComponent<ShieldRuntime>();
            r.Init(absorb, reducePct, maxHits, duration, onEnd: null);
        }
    }

    [CreateAssetMenu(menuName = "NightHunter/Skills/Effects/Spawn Decoy", fileName = "E_SpawnDecoy")]
    public class EffectSpawnDecoySO : EffectSO
    {
        public GameObject decoyPrefab;
        public float lifetime = 3f;

        public override void OnCast(AbilityContext ctx)
        {
            if (!decoyPrefab) return;
            var d = Object.Instantiate(decoyPrefab, ctx.Caster.position, ctx.Caster.rotation);
            Object.Destroy(d, Mathf.Max(0.1f, lifetime));
        }
    }

    [CreateAssetMenu(menuName = "NightHunter/Skills/Effects/Damage", fileName = "E_Damage")]
    public class EffectDamageSO : EffectSO
    {
        public int damage = 40;
        public float impulse = 6f;

        public override void OnImpact(AbilityContext ctx, object target)
        {
            var col = target as Collider;
            if (!col) return;

            var hp = col.GetComponentInParent<Health>();
            if (hp) hp.TakeDamage(damage);

            var rb = col.attachedRigidbody;
            if (rb)
            {
                Vector3 dir = (col.bounds.center - (ctx.Caster ? ctx.Caster.position : Vector3.zero)).normalized;
                rb.AddForce(dir * impulse, ForceMode.Impulse);
            }
            else
            {
                var kb = col.GetComponentInParent<KnockbackReceiver>();
                if (kb)
                {
                    Vector3 dir = (col.bounds.center - (ctx.Caster ? ctx.Caster.position : Vector3.zero)).normalized;
                    kb.AddImpact(dir, impulse);
                }
            }
        }
    }
}
