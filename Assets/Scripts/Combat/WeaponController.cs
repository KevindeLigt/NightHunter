using System.Collections.Generic;
using UnityEngine;

namespace NightHunter.combat
{
    /// Unified weapon driver for Ranged (projectile), Hitscan (raycast), and Melee (spherecast).
    /// - Uses WeaponData from WeaponLibrary
    /// - Handles fire input, cooldown, reload
    /// - Triggers Animator on the equipped weapon prefab: "Fire", "Reload", "Melee"
    /// - Spawns projectile prefab (if set), or raycasts / spherecasts for damage
    /// 
    [RequireComponent(typeof(CharacterController))] // optional but handy for player objects
    public class WeaponController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Camera aimCamera;          // your FPS camera
        [SerializeField] private Transform firePoint;       // muzzle; default to camera transform
        [SerializeField] private Animator weaponAnimator;   // animator on the held weapon prefab (optional)
        [SerializeField] private AudioSource audioSource;   // optional, plays fire/reload SFX
        [SerializeField] private Transform weaponSlot;      // assign the WeaponSlot under the camera

        [Header("Debug Shot Line (optional)")]
        [SerializeField] private bool showShotLine = true;
        [SerializeField] private float shotLineSeconds = 0.06f;
        [SerializeField] private float shotLineWidth = 0.02f;

        [Header("Starting Loadout")]
        [SerializeField] private WeaponId[] startingWeapons;

        [Header("Loadout Control")]
        [SerializeField] private bool manageInternally = true;                 // set FALSE when using WeaponLoadout
        [SerializeField] private NightHunter.combat.WeaponLoadout loadout;     // optional
        [SerializeField] private KeyCode swapKey = KeyCode.Q;
        [SerializeField] private bool mouseWheelSwaps = true;

        [Header("Camera Feel (optional)")]
        [SerializeField] private CameraFeelDriver cameraFeel;

        [Header("Active Weapon")]
        [SerializeField] private WeaponId activeWeapon = WeaponId.Pistol;

        [Header("Ammo Defaults")]
        [SerializeField] private int defaultReserveAmmo = 90;

        [Header("Melee Tuning")]
        [SerializeField] private float meleeRadius = 0.6f;  // local helper; WeaponData.range = reach

        private CharacterController _cc;
        private GameObject _viewInstance;               // current spawned view

        private readonly Dictionary<WeaponId, (int clip, int reserve)> _ammo
            = new Dictionary<WeaponId, (int clip, int reserve)>();
        private float _lastFireTime;
        private bool _isReloading;

        private readonly HashSet<WeaponId> _owned = new();

        void Awake()
        {
            if (!aimCamera) aimCamera = Camera.main;
            if (!firePoint && aimCamera) firePoint = aimCamera.transform;
            if (!loadout) loadout = GetComponent<NightHunter.combat.WeaponLoadout>();
            if (!_cc) _cc = GetComponent<CharacterController>();
            if (!cameraFeel && aimCamera) cameraFeel = aimCamera.GetComponent<CameraFeelDriver>();

            WeaponLibrary.EnsureLoaded();

            _owned.Clear();
            if (startingWeapons != null)
            {
                foreach (var id in startingWeapons)
                {
                    _owned.Add(id);
                    EnsureAmmoEntry(id);
                }
            }

            EnsureAmmoEntry(activeWeapon);

            if (manageInternally)
            {
                // Old behavior: controller picks + equips a starting weapon on its own
                Equip(activeWeapon);
                _owned.Add(activeWeapon);
            }
            // else: external loadout will call Equip(...) during its own initialization
        }

        void Update()
        {
            var wd = WeaponLibrary.Get(activeWeapon);
            if (wd == null || firePoint == null || _isReloading) return;

            // Fire (hold for auto/semi works via fireCooldown)
            if (Input.GetMouseButton(0))
                TryFire(wd);

            // Reload
            if (Input.GetKeyDown(KeyCode.R))
                TryReload(wd);

            // Swap (only if a loadout is present)
            if (loadout != null)
            {
                if (Input.GetKeyDown(swapKey)) loadout.Swap();
                if (mouseWheelSwaps && Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0f) loadout.Swap();
            }
        }


        private void SpawnOrSwapView(WeaponData wd)
        {
            // Clean up old
            if (_viewInstance) Destroy(_viewInstance);

            // If we have a view prefab and a slot, spawn it
            if (wd != null && wd.viewPrefab != null && weaponSlot != null)
            {
                _viewInstance = Instantiate(wd.viewPrefab, weaponSlot);

                // Find Animator on the spawned view (root or children)
                weaponAnimator = _viewInstance.GetComponentInChildren<Animator>();

                // Find a child named "Muzzle" for firing point; fallback to weaponSlot/camera
                var muzzle = _viewInstance.transform.Find("Muzzle");
                firePoint = muzzle ? muzzle : (firePoint ? firePoint : (aimCamera ? aimCamera.transform : transform));
            }
            else
            {
                // No view prefab provided: keep using existing firePoint/camera
                weaponAnimator = null;
                if (!firePoint) firePoint = aimCamera ? aimCamera.transform : transform;
            }
        }

        // ---- Public surface (for UI/other systems) ----
        public WeaponId ActiveWeaponId => activeWeapon;
        public WeaponData ActiveWeaponData => WeaponLibrary.Get(activeWeapon);

        public void Equip(WeaponId id, Animator newAnimator = null)
        {
            activeWeapon = id;
            EnsureAmmoEntry(id);

            var wd = WeaponLibrary.Get(id);
            SpawnOrSwapView(wd);

            // If you pass a manual animator, it overrides the one we find
            if (newAnimator) weaponAnimator = newAnimator;

            _isReloading = false;
        }

        public bool TryGetAmmo(WeaponId id, out int clip, out int reserve)
        {
            (clip, reserve) = (0, 0);
            if (!_ammo.TryGetValue(id, out var st)) return false;
            clip = st.clip; reserve = st.reserve;
            return true;
        }

        // convenience overload for the active weapon
        public bool TryGetAmmo(out int clip, out int reserve) =>
            TryGetAmmo(activeWeapon, out clip, out reserve);

        // ---- Firing paths ----
        private void TryFire(WeaponData wd)
        {
            // Cooldown
            if (Time.time - _lastFireTime < Mathf.Max(0f, wd.fireCooldown)) return;

            // Ammo gate
            if (wd.usesAmmo)
            {
                var st = _ammo[wd.id];
                if (st.clip <= 0)
                {
                    TryReload(wd);
                    return;
                }
                st.clip -= 1;
                _ammo[wd.id] = st;
            }

            _lastFireTime = Time.time;

            // Animator + SFX + Muzzle
            if (weaponAnimator)
            {
                if (wd.kind == WeaponKind.Melee) weaponAnimator.SetTrigger("Melee");
                else weaponAnimator.SetTrigger("Fire");
            }
            if (wd.fireSfx) PlayOneShot(wd.fireSfx);
            if (wd.muzzleFxPrefab) Instantiate(wd.muzzleFxPrefab, firePoint.position, firePoint.rotation, firePoint);

            // FEEL (triggered on shot, not every frame)
            if (cameraFeel)
            {
                if (wd.kind != WeaponKind.Melee)
                {
                    cameraFeel.ShotKick(wd.recoilUp, wd.recoilBack);
                    if (wd.shotShakeDuration > 0f) cameraFeel.Shake(wd.shotShakeAmplitude, wd.shotShakeDuration);
                    if (wd.fovPunch > 0f) cameraFeel.FovPunch(wd.fovPunch, wd.fovReturnSpeed);
                }
                else
                {
                    cameraFeel.Shake(0.2f, 0.05f); // tiny pre-swing rumble
                }
            }

            // OPTIONAL: start a short lunge on melee
            if (wd.kind == WeaponKind.Melee && wd.meleeLungeDistance > 0f)
                StartCoroutine(MeleeLunge(wd.meleeLungeDistance, wd.meleeLungeTime));
            // Aim from the center of the screen
            Ray aimRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float aimRange = Mathf.Max(0.01f, wd.range > 0f ? wd.range : 100f);

            // Find a point we're aiming at (hit or far point)
            Vector3 aimPoint = aimRay.GetPoint(aimRange);
            if (Physics.Raycast(aimRay, out var aimHit, aimRange, ~0, QueryTriggerInteraction.Ignore))
                aimPoint = aimHit.point;

            // Now build shot from our muzzle toward that point
            Vector3 origin = firePoint.position;
            Vector3 dir = (aimPoint - origin).normalized;

            switch (wd.kind)
            {
                case WeaponKind.Ranged:
                    FireProjectile(wd, origin, dir);
                    break;

                case WeaponKind.Hitscan:
                    DoHitscan(wd, origin, dir);
                    break;

                case WeaponKind.Melee:
                    DoMelee(wd, origin, dir);
                    break;
            }
        }

        private void TryReload(WeaponData wd)
        {
            if (!wd.usesAmmo || _isReloading) return;

            var st = _ammo[wd.id];
            if (st.clip >= wd.clipSize || st.reserve <= 0) return;

            _isReloading = true;
            if (weaponAnimator) weaponAnimator.SetTrigger("Reload");
            if (wd.reloadSfx) PlayOneShot(wd.reloadSfx);

            // Simple instant reload after delay (animation should match reloadTime)
            StartCoroutine(ReloadAfterDelay(wd));
        }

        private System.Collections.IEnumerator ReloadAfterDelay(WeaponData wd)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, wd.reloadTime));

            var st = _ammo[wd.id];
            int need = wd.clipSize - st.clip;
            int take = Mathf.Min(need, st.reserve);
            st.clip += take;
            st.reserve -= take;
            _ammo[wd.id] = st;

            _isReloading = false;
        }

        // ---- Implementations per kind ----
        private void FireProjectile(WeaponData wd, Vector3 origin, Vector3 dir)
        {
            if (wd.projectilePrefab == null)
            {
                Debug.LogWarning($"[{nameof(WeaponController)}] {wd.displayName} has no projectilePrefab.");
                return;
            }

            // Spawn and push (expects prefab to handle its own collision/damage)
            var go = Instantiate(wd.projectilePrefab, origin, Quaternion.LookRotation(dir, Vector3.up));

            // If there is a Rigidbody, give it velocity
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
                rb.velocity = dir.normalized * wd.projectileSpeed;

            // Failsafe lifetime if prefab doesn't self-destroy
            if (wd.projectileLifetime > 0f)
                Destroy(go, wd.projectileLifetime);
        }

        private void DoHitscan(WeaponData wd, Vector3 origin, Vector3 dir)
        {
            float dist = Mathf.Max(0.01f, wd.range);
            Ray ray = new Ray(origin, dir);

            if (Physics.Raycast(ray, out var hit, dist, ~0, QueryTriggerInteraction.Collide))
            {
                ApplyDamage(hit.collider, wd.damage);

                // Impact candy
                if (wd.impactFxPrefab)
                    Instantiate(wd.impactFxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                if (wd.impactSfx)
                    AudioSource.PlayClipAtPoint(wd.impactSfx, hit.point, 0.9f);

                ShowShotLine(origin, hit.point, Color.green);   // HIT = green
            }
            else
            {
                var end = origin + dir * dist;
                ShowShotLine(origin, end, Color.red);           // MISS = red
            }

#if UNITY_EDITOR
            Debug.DrawRay(origin, dir * dist, Color.yellow, 0.05f);
#endif
        }

        private void DoMelee(WeaponData wd, Vector3 origin, Vector3 dir)
        {
            float reach = Mathf.Clamp(wd.range, 0.5f, 2.5f);
            Ray ray = new Ray(origin, dir);

            if (Physics.SphereCast(ray, meleeRadius, out var hit, reach, ~0, QueryTriggerInteraction.Collide))
            {
                ApplyDamage(hit.collider, wd.damage);

                // VFX/SFX
                if (wd.meleeHitFxPrefab)
                    Instantiate(wd.meleeHitFxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                if (wd.meleeWhooshSfx)
                    PlayOneShot(wd.meleeWhooshSfx);

                // Camera thump + micro hitstop
                if (cameraFeel) cameraFeel.Shake(Mathf.Max(0.25f, wd.shotShakeAmplitude), 0.08f);
                if (wd.meleeHitstop > 0f) StartCoroutine(Hitstop(wd.meleeHitstop));

#if UNITY_EDITOR
                Debug.DrawRay(hit.point, -dir.normalized * 0.3f, Color.cyan, 0.35f);
#endif
            }

#if UNITY_EDITOR
            Debug.DrawRay(origin, dir * reach, Color.cyan, 0.05f);
#endif
        }

        // ---- Damage hook (expects your enemies to have a Health component) ----
        private void ApplyDamage(Collider col, int damage)
        {
            if (!col) return;
            var hp = col.GetComponentInParent<Health>();
            if (hp != null) hp.TakeDamage(damage);
        }

        // ---- Utilities ----
        private void EnsureAmmoEntry(WeaponId id)
        {
            var wd = WeaponLibrary.Get(id);
            if (wd == null) return;

            if (!_ammo.ContainsKey(id))
            {
                if (wd.usesAmmo)
                    _ammo[id] = (wd.clipSize, Mathf.Max(0, defaultReserveAmmo));
                else
                    _ammo[id] = (0, 0);
            }
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (!clip) return;
            if (audioSource)
                audioSource.PlayOneShot(clip, 0.9f);
            else
                AudioSource.PlayClipAtPoint(clip, firePoint ? firePoint.position : transform.position, 0.9f);
        }

        public void AddReserve(WeaponId id, int amount)
        {
            if (amount <= 0) return;
            var wd = WeaponLibrary.Get(id);
            if (wd == null || !wd.usesAmmo) return;

            // ensure entry exists
            var has = _ammo.TryGetValue(id, out var st);
            if (!has) { EnsureAmmoEntry(id); _ammo.TryGetValue(id, out st); }

            st.reserve += amount;
            _ammo[id] = st;
        }

        private void ShowShotLine(Vector3 start, Vector3 end, Color color)
        {
            if (!showShotLine) return;
            var go = new GameObject("ShotLineTemp");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.startWidth = lr.endWidth = shotLineWidth;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = color;
            Destroy(go, shotLineSeconds);
        }

        public bool HasWeapon(WeaponId id) => _owned.Contains(id);

        public void AddWeapon(WeaponId id, int reserve = 0, bool autoEquip = true)
        {
            if (!_owned.Contains(id)) _owned.Add(id);
            var wd = WeaponLibrary.Get(id);
            if (wd != null && wd.usesAmmo && reserve > 0) AddReserve(id, reserve);
            if (autoEquip) Equip(id);
        }

        // ---- Feel helpers ----
        private System.Collections.IEnumerator MeleeLunge(float distance, float time)
        {
            if (_cc == null || time <= 0f || distance <= 0f) yield break;

            Vector3 dir = aimCamera ? aimCamera.transform.forward : transform.forward;
            float speed = distance / time;
            float t = 0f;
            while (t < time)
            {
                float dt = Time.deltaTime;
                _cc.Move(dir * speed * dt);
                t += dt;
                yield return null;
            }
        }

        private bool _hitstopping;
        private System.Collections.IEnumerator Hitstop(float seconds)
        {
            if (_hitstopping || seconds <= 0f) yield break;
            _hitstopping = true;

            float prev = Time.timeScale;
            Time.timeScale = 0.12f;  // crunchy but readable
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = prev;
            Time.fixedDeltaTime = 0.02f;
            _hitstopping = false;
        }
    }
}
