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
        public float MaxRange => maxRange;

        public override void Acquire(AbilityContext ctx, List<object> hits)
        {
            var ray = ctx.AimRay;
            Vector3 point = ray.GetPoint(maxRange);
            if (snapToHit && Physics.Raycast(ray, out var hit, maxRange, ctx.HitMask, QueryTriggerInteraction.Ignore))
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
        [SerializeField] bool flatten = true; // ignore vertical aim

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
            yield break;
        }
    }

    // ---------- EFFECTS ----------

    [CreateAssetMenu(menuName = "NightHunter/Skills/Effects/Shield", fileName = "E_Shield")]
    public class EffectShieldSO : EffectSO
    {
        [Header("Shield")]
        public int absorb = 60;
        [Range(0f, 1f)] public float reducePct = 0.3f; // 30% reduction
        public int maxHits = 0;                        // 0 = unlimited until absorb/duration ends
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
}
