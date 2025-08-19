using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NightHunter.combat
{
    // Lets shields modify incoming damage without touching every weapon.
    public interface IDamageModifier
    {
        // Return the damage that should reach Health after modification.
        int ModifyIncomingDamage(int incoming);
    }

    /// <summary>
    /// Player skill hotbar: three slots mapped to keys 1/2/3.
    /// Implements ShieldSelf and DashUpgrade.
    /// </summary>
    public class SkillController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Camera aimCamera;           // FPS camera
        [SerializeField] private CharacterController cc;     // player controller (for dash)
        [SerializeField] private Transform weaponSlotOrRoot; // where to spawn decoy (optional)

        [Header("Slots")]
        public SkillId slot1 = SkillId.Shield;
        public SkillId slot2 = SkillId.DashSurge;
        public SkillId slot3 = SkillId.None;

        private readonly Dictionary<SkillId, float> _cooldowns = new();
        private ShieldEffect _activeShield;

        void Awake()
        {
            if (!aimCamera) aimCamera = Camera.main;
            if (!cc) cc = GetComponent<CharacterController>();
            if (!weaponSlotOrRoot) weaponSlotOrRoot = transform;

            SkillLibrary.EnsureLoaded();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) TryActivate(slot1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TryActivate(slot2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TryActivate(slot3);
        }

        // ---- Activation router ----
        void TryActivate(SkillId id)
        {
            if (id == SkillId.None) return;
            var data = SkillLibrary.Get(id);
            if (data == null) return;

            if (_cooldowns.TryGetValue(id, out var readyAt) && Time.time < readyAt) return;

            switch (data.kind)
            {
                case SkillKind.ShieldSelf:
                    ActivateShield(data);
                    break;

                case SkillKind.DashUpgrade:
                    StartCoroutine(ActivateDash(data));
                    break;

                case SkillKind.SelfBuff:
                    // Reserved for future (speed/regen/etc.)
                    break;
            }

            _cooldowns[id] = Time.time + Mathf.Max(0f, data.cooldown);
        }

        // ---- ShieldSelf ----
        void ActivateShield(SkillData s)
        {
            if (_activeShield) Destroy(_activeShield);

            _activeShield = gameObject.AddComponent<ShieldEffect>();
            _activeShield.Begin(s.shieldAbsorb, s.shieldDamageReduce, s.shieldMaxHits, s.duration, onEnd: () =>
            {
                _activeShield = null;
            });
        }

        // ---- DashUpgrade ----
        IEnumerator ActivateDash(SkillData s)
        {
            // Optional decoy
            if (s.spawnDecoy && s.decoyPrefab)
            {
                var d = Instantiate(s.decoyPrefab, weaponSlotOrRoot.position, weaponSlotOrRoot.rotation);
                Destroy(d, Mathf.Max(0.1f, s.decoyLifetime));
            }

            if (!cc) yield break;

            // Aim: center of screen
            var aimRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            Vector3 dir = aimRay.direction.normalized; dir.y = 0f; // flat ground dash (tweak if you want vertical dashes)

            float elapsed = 0f;
            Vector3 start = transform.position;
            Vector3 end = start + dir * Mathf.Max(0f, s.dashDistance);

            while (elapsed < s.dashDuration)
            {
                Vector3 target = Vector3.Lerp(start, end, elapsed / Mathf.Max(0.01f, s.dashDuration));
                cc.Move(target - transform.position);
                elapsed += Time.deltaTime;
                yield return null;
            }

            cc.Move(end - transform.position);
        }

        // ---- Shield effect component ----
        private class ShieldEffect : MonoBehaviour, IDamageModifier
        {
            int _absorbPool;
            float _reducePct;
            int _maxHits;
            int _hitCount;
            float _endAt;
            System.Action _onEnd;

            public void Begin(int absorb, float reducePct, int maxHits, float duration, System.Action onEnd)
            {
                _absorbPool = Mathf.Max(0, absorb);
                _reducePct = Mathf.Clamp01(reducePct);
                _maxHits = Mathf.Max(0, maxHits);
                _endAt = Time.time + Mathf.Max(0f, duration);
                _onEnd = onEnd;
                StartCoroutine(Life());
            }

            IEnumerator Life()
            {
                while (Time.time < _endAt && (_maxHits == 0 || _hitCount < _maxHits) && (_absorbPool > 0 || _reducePct > 0f))
                    yield return null;

                _onEnd?.Invoke();
                Destroy(this);
            }

            // IDamageModifier — returns the damage that should reach Health.
            public int ModifyIncomingDamage(int incoming)
            {
                if (Time.time >= _endAt) return incoming;

                // First apply % reduction
                int reduced = Mathf.RoundToInt(incoming * (1f - _reducePct));

                // Then spend absorb pool
                int afterAbsorb = Mathf.Max(0, reduced - _absorbPool);
                int spent = reduced - afterAbsorb;
                _absorbPool = Mathf.Max(0, _absorbPool - spent);

                _hitCount++;

                return afterAbsorb;
            }
        }
        public SkillId[] GetSlots() => new[] { slot1, slot2, slot3 };

        public float GetCooldown01(SkillId id)
        {
            if (id == SkillId.None) return 0f;
            if (!_cooldowns.TryGetValue(id, out var readyAt)) return 0f;
            var data = SkillLibrary.Get(id);
            if (data == null || data.cooldown <= 0f) return 0f;
            float remaining = Mathf.Max(0f, readyAt - Time.time);
            return Mathf.Clamp01(remaining / data.cooldown); // 1 = cooling down, 0 = ready
        }

    }
}
