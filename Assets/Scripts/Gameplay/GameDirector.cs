using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NightHunter.combat
{
    public class GameDirector : MonoBehaviour
    {
        public enum Phase { Day, Night }
        public Phase CurrentPhase { get; private set; } = Phase.Day;

        [Header("Cycle")]
        [SerializeField] float nightHardLimit = 180f;   // safety timeout

        [Header("Environment")]
        [SerializeField] Light sunLight;
        [SerializeField] float dayLightIntensity = 1.1f;
        [SerializeField] float nightLightIntensity = 0.08f;
        [SerializeField] bool useFog = true;
        [SerializeField] float dayFogDensity = 0.002f;
        [SerializeField] float nightFogDensity = 0.02f;
        [SerializeField] Behaviour fogVolumeOptional;   // URP/HDRP Volume
        [SerializeField] float dayVolumeWeight = 0f;
        [SerializeField] float nightVolumeWeight = 1f;
        [SerializeField] float envTransitionSeconds = 1f;

        [Header("Progression")]
        [SerializeField] List<StreetModule> streets = new(); // order = path order
        [SerializeField] int startStreetIndex = 0;
        int currentStreetIndex = 0;

        [Header("Shop")]
        [SerializeField] GameObject shopkeeperPrefab;
        GameObject _shopInstance;

        [System.Serializable]
        public struct EnemyType
        {
            public GameObject prefab;
            public int minNight;
            public int cost;
            public int weight;
        }
        [Header("Spawning")]
        [SerializeField] EnemyType[] enemies;
        [SerializeField] int startingBudget = 8;
        [SerializeField] int budgetPerNight = 4;
        [SerializeField] float spawnInterval = 0.25f;

        int _nightNumber = 0; // 1-based nights
        int _alive;

        void Start()
        {
            // Hide everything first
            foreach (var s in streets) if (s) s.SetVisible(false);
            currentStreetIndex = Mathf.Clamp(startStreetIndex, 0, Mathf.Max(0, streets.Count - 1));

            // Show current street, and preview the next
            ShowCurrentStreet(true);
            PreviewNextStreet(true);

            // Start in Day lighting
            StartCoroutine(FadeEnv(toNight01: 0f));
            CurrentPhase = Phase.Day;
            SpawnShopkeeperOnCurrentStreet();
        }

        // Called by FogGateTrigger when the player steps through a gate during Day
        public void PlayerEnteredGate(int targetStreetIdx)
        {
            if (CurrentPhase != Phase.Day) return;
            if (targetStreetIdx < 0 || targetStreetIdx >= streets.Count) return;

            // Hide previous street visuals
            if (currentStreetIndex >= 0 && currentStreetIndex < streets.Count)
                streets[currentStreetIndex].SetVisible(false);

            // Advance
            currentStreetIndex = targetStreetIdx;
            ShowCurrentStreet(true);
            PreviewNextStreet(false); // hide preview until next day
            DespawnShopkeeper();

            // Begin Night on the new street
            StartCoroutine(NightRoutine());
        }

        IEnumerator NightRoutine()
        {
            CurrentPhase = Phase.Night;
            yield return StartCoroutine(FadeEnv(toNight01: 1f));

            _nightNumber++;
            int budget = startingBudget + (_nightNumber - 1) * budgetPerNight;

            yield return StartCoroutine(SpawnNight(streets[currentStreetIndex], budget));

            // Wait for clear or timeout
            float t = 0f;
            while (_alive > 0 && t < nightHardLimit) { t += Time.deltaTime; yield return null; }

            // Back to Day
            CurrentPhase = Phase.Day;
            yield return StartCoroutine(FadeEnv(toNight01: 0f));
            SpawnShopkeeperOnCurrentStreet();
            PreviewNextStreet(true);
        }

        void ShowCurrentStreet(bool visible)
        {
            var s = GetStreet(currentStreetIndex);
            if (s) s.SetVisible(visible);
        }

        void PreviewNextStreet(bool visible)
        {
            var s = GetStreet(currentStreetIndex + 1);
            if (s) s.SetVisible(visible); // remains behind your fog gate
        }

        StreetModule GetStreet(int idx)
        {
            if (idx < 0 || idx >= streets.Count) return null;
            return streets[idx];
        }

        // -------- Spawning --------
        IEnumerator SpawnNight(StreetModule street, int budget)
        {
            var points = street ? street.GetSpawnPoints() : System.Array.Empty<Transform>();
            if (points.Length == 0) yield break;

            var pool = BuildPoolForNight(_nightNumber);
            if (pool.Count == 0) yield break;

            _alive = 0;
            var wait = new WaitForSeconds(spawnInterval);

            while (budget > 0)
            {
                var et = Pick(pool);
                if (!et.prefab) { yield return null; continue; }

                if (et.cost > budget)
                {
                    bool anyFit = false;
                    foreach (var e in pool) if (e.cost <= budget) { anyFit = true; break; }
                    if (!anyFit) break;
                    yield return null;
                    continue;
                }

                var sp = points[Random.Range(0, points.Length)];
                var go = Instantiate(et.prefab, sp.position, sp.rotation);
                budget -= et.cost;
                _alive++;

                // optional: listen on death if your Health exposes an event
                var h = go.GetComponentInChildren<Health>();
                if (h)
                {
                    // If you have an event, hook it here. Otherwise, you can add a tiny "OnDeath" invoker in Health later.
                    h.OnDeath += () => { _alive = Mathf.Max(0, _alive - 1); };
                }

                yield return wait;
            }
        }

        List<EnemyType> BuildPoolForNight(int night)
        {
            var list = new List<EnemyType>();
            foreach (var e in enemies)
            {
                if (!e.prefab || e.weight <= 0) continue;
                if (night < Mathf.Max(1, e.minNight)) continue;
                list.Add(e);
            }
            return list;
        }

        EnemyType Pick(List<EnemyType> pool)
        {
            int total = 0;
            foreach (var e in pool) total += Mathf.Max(1, e.weight);
            int r = Random.Range(0, Mathf.Max(1, total));
            foreach (var e in pool)
            {
                int w = Mathf.Max(1, e.weight);
                if (r < w) return e;
                r -= w;
            }
            return pool[pool.Count - 1];
        }

        // -------- Day/Night visuals --------
        IEnumerator FadeEnv(float toNight01)
        {
            float t = 0f;
            float fromLight = sunLight ? sunLight.intensity : 0f;
            float toLight = Mathf.Lerp(dayLightIntensity, nightLightIntensity, toNight01);

            float fromFog = RenderSettings.fogDensity;
            float toFog = Mathf.Lerp(dayFogDensity, nightFogDensity, toNight01);

            float fromVol = GetVolumeWeight();
            float toVol = Mathf.Lerp(dayVolumeWeight, nightVolumeWeight, toNight01);

            while (t < envTransitionSeconds)
            {
                float a = envTransitionSeconds <= 0f ? 1f : t / envTransitionSeconds;
                if (sunLight) sunLight.intensity = Mathf.Lerp(fromLight, toLight, a);
                if (useFog)
                {
                    RenderSettings.fog = true;
                    RenderSettings.fogDensity = Mathf.Lerp(fromFog, toFog, a);
                }
                SetVolumeWeight(Mathf.Lerp(fromVol, toVol, a));
                t += Time.deltaTime;
                yield return null;
            }

            if (sunLight) sunLight.intensity = toLight;
            if (useFog) RenderSettings.fogDensity = toFog;
            SetVolumeWeight(toVol);
        }

        float GetVolumeWeight()
        {
            if (!fogVolumeOptional) return 0f;
            var prop = fogVolumeOptional.GetType().GetProperty("weight");
            if (prop != null) { object v = prop.GetValue(fogVolumeOptional); if (v is float f) return f; }
            return 0f;
        }
        void SetVolumeWeight(float w)
        {
            if (!fogVolumeOptional) return;
            var prop = fogVolumeOptional.GetType().GetProperty("weight");
            prop?.SetValue(fogVolumeOptional, Mathf.Clamp01(w));
        }

        // -------- Shopkeeper --------
        void SpawnShopkeeperOnCurrentStreet()
        {
            if (!shopkeeperPrefab) { Debug.LogWarning("[Director] No shopkeeperPrefab assigned."); return; }

            var street = GetStreet(currentStreetIndex);
            if (!street) { Debug.LogWarning("[Director] No current street."); return; }

            // Fallback: use street center if no ShopPoint set
            Vector3 pos = street.shopPoint ? street.shopPoint.position : street.transform.position + Vector3.up * 0.1f;
            Quaternion rot = street.shopPoint ? street.shopPoint.rotation : Quaternion.LookRotation(street.transform.forward, Vector3.up);

            Debug.Log($"[Director] Spawning shop on street {currentStreetIndex} ({GetStreet(currentStreetIndex)?.name})");
            if (!street.shopPoint) Debug.LogWarning("[Director] No ShopPoint found—using fallback.");

            DespawnShopkeeper();
            _shopInstance = Instantiate(shopkeeperPrefab, pos, rot);
        }
        [ContextMenu("DEBUG: Spawn Shopkeeper Now")]
        void DebugSpawnShop() => SpawnShopkeeperOnCurrentStreet();


        void DespawnShopkeeper()
        {
            if (_shopInstance) Destroy(_shopInstance);
            _shopInstance = null;
        }
    }
}
