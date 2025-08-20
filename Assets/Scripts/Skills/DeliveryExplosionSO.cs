using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NightHunter.combat
{
    [CreateAssetMenu(menuName = "NightHunter/Skills/Delivery/Explosion AoE", fileName = "D_Explosion")]
    public class DeliveryExplosionSO : DeliverySO
    {
        [Header("Shape")]
        [SerializeField] float radius = 5f;
        [SerializeField] bool requireLineOfSight = false;
        [SerializeField] public float Radius => radius;
        [SerializeField] float losPadding = 0.1f; // avoids hitting own collider

        [Header("Timing")]
        [SerializeField] float fuseSeconds = 0f;   // wait before exploding
        [SerializeField] int pulses = 1;           // 1 = single blast
        [SerializeField] float pulseInterval = 0.25f;

        [Header("FX (optional)")]
        [SerializeField] GameObject vfxPrefab;     // spawned at center
        [SerializeField] AudioClip sfx;
        [SerializeField] float vfxLifetime = 3f;

        [Header("Debug Preview")]
        [SerializeField] bool debugDraw = true;
        [SerializeField] Color debugColor = new Color(1f, 0.6f, 0f, 1f);
        [SerializeField] int debugSegments = 48;
        [SerializeField] float debugPreviewSeconds = 0f; // extra preview time even if fuse == 0 (set >0 only while tuning)

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





        public override IEnumerator Execute(AbilityContext ctx, List<object> targets, System.Action<object> onImpact)
        {
            // Expecting a Vector3 from TargetingAimPointSO; if not present, center on caster
            Vector3 center = ctx.Caster.position;
            foreach (var t in targets)
            {
                if (t is Vector3 p) { center = p; break; }
                if (t is Transform tr) { center = tr.position; break; }
            }

            // FX spawn (optional, early)
            if (vfxPrefab) Object.Instantiate(vfxPrefab, center, Quaternion.identity).AddComponent<AutoDestroy>().Init(vfxLifetime);
            if (sfx) AudioSource.PlayClipAtPoint(sfx, center, 0.9f);

            // if (fuseSeconds > 0f) yield return new WaitForSeconds(fuseSeconds);
            // Draw the ring during fuse; optionally draw a short preview even with fuse == 0
            if (debugDraw)
            {
                float wait = fuseSeconds > 0f ? fuseSeconds : debugPreviewSeconds;
                for (float t = 0f; t < wait; t += Time.deltaTime)
                {
                    // duration slightly > frame so lines persist one frame in the Scene view
                    DrawRing(center, radius, debugColor, Time.deltaTime * 1.2f);
                    yield return null;
                }
            }
            else if (fuseSeconds > 0f)
            {
                yield return new WaitForSeconds(fuseSeconds);
            }

            int count = Mathf.Max(1, pulses);
            for (int i = 0; i < count; i++)
            {
                // Collect hits
                var cols = Physics.OverlapSphere(center, radius, ctx.HitMask, QueryTriggerInteraction.Collide);
                foreach (var c in cols)
                {
                    if (!c) continue;

                    if (requireLineOfSight)
                    {
                        var dir = (c.bounds.center - center);
                        float dist = dir.magnitude;
                        if (dist <= 0.001f) { onImpact?.Invoke(c); continue; }
                        if (Physics.Raycast(center + dir.normalized * losPadding, dir.normalized, out var hit, dist + 0.01f, ctx.HitMask, QueryTriggerInteraction.Ignore))
                        {
                            // Only accept if first thing hit is this collider (basic LOS)
                            if (hit.collider != c) continue;
                        }
                    }

                    onImpact?.Invoke(c); // pass the Collider to effects
                }

                if (i < count - 1) yield return new WaitForSeconds(pulseInterval);
            }
        }

        // helper to auto destroy VFX without extra scripts elsewhere
        private class AutoDestroy : MonoBehaviour
        {
            float dieAt;
            public void Init(float lifetime) { dieAt = Time.time + Mathf.Max(0.05f, lifetime); }
            void Update() { if (Time.time >= dieAt) Destroy(gameObject); }
        }
    }
}
