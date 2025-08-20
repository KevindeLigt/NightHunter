using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NightHunter.combat
{
    public class AbilityRunner : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform firePoint;          // muzzle/hand
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Slots")]
        public SkillId slot1 = SkillId.Shield;
        public SkillId slot2 = SkillId.DashSurge;
        public SkillId slot3 = SkillId.None;

        private readonly Dictionary<SkillId, float> _cooldowns = new();

        void Awake()
        {
            if (!aimCamera) aimCamera = Camera.main;
            if (!firePoint) firePoint = aimCamera ? aimCamera.transform : transform;
            AbilityLibrary.EnsureLoaded();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) TryCast(slot1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TryCast(slot2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TryCast(slot3);
        }

        public SkillId[] GetSlots() => new[] { slot1, slot2, slot3 };
        public float GetCooldown01(SkillId id)
        {
            if (id == SkillId.None) return 0f;
            if (!_cooldowns.TryGetValue(id, out var readyAt)) return 0f;
            var spec = AbilityLibrary.Get(id);
            if (spec == null || spec.cooldown <= 0f) return 0f;
            float remaining = Mathf.Max(0f, readyAt - Time.time);
            return Mathf.Clamp01(remaining / spec.cooldown);
        }

        void TryCast(SkillId id)
        {
            if (id == SkillId.None) return;
            var spec = AbilityLibrary.Get(id);
            if (spec == null || spec.targeting == null || spec.delivery == null) return;

            if (_cooldowns.TryGetValue(id, out var readyAt) && Time.time < readyAt) return;

            StartCoroutine(Run(spec));
            _cooldowns[id] = Time.time + Mathf.Max(0f, spec.cooldown);
        }

        IEnumerator Run(SkillSpec spec)
        {
            // Build context
            var ctx = new AbilityContext
            {
                Caster = transform,
                AimCamera = aimCamera,
                Origin = firePoint,
                AimRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)),
                HitMask = hitMask
            };

            // Acquire targets
            var targets = new List<object>(8);
            spec.targeting.Acquire(ctx, targets);

            // Effects: OnCast
            if (spec.effects != null)
                foreach (var e in spec.effects) if (e) e.OnCast(ctx);

            // Deliver & emit impacts → effects OnImpact
            yield return StartCoroutine(spec.delivery.Execute(ctx, targets, (impact) =>
            {
                if (spec.effects != null)
                    foreach (var e in spec.effects) if (e) e.OnImpact(ctx, impact);
            }));

            // Effects: OnEnd
            if (spec.effects != null)
                foreach (var e in spec.effects) if (e) e.OnEnd(ctx);
        }
    }
}
