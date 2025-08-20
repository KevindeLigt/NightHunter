using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NightHunter.combat
{
    public class GameDirector : MonoBehaviour
    {
        [Header("Cycle")]
        [SerializeField] float daySeconds = 40f;
        [SerializeField] float nightPrepSeconds = 2f;  // brief fade into night
        [SerializeField] float nightHardLimit = 180f;  // safety timeout

        [Header("Environment")]
        [SerializeField] Light sunLight;               // your Directional Light
        [SerializeField] float dayLightIntensity = 1.1f;
        [SerializeField] float nightLightIntensity = 0.08f;

        [SerializeField] bool useFog = true;
        [SerializeField] float dayFogDensity = 0.002f;
        [SerializeField] float nightFogDensity = 0.02f;

        [Tooltip("Optional: assign a URP/HDRP Volume here; we'll try to set its 'weight' by reflection.")]
        [SerializeField] Behaviour fogVolumeOptional;
        [SerializeField] float dayVolumeWeight = 0f;
        [SerializeField] float nightVolumeWeight = 1f;

        [SerializeField] float envTransitionSeconds = 1f;

        [Header("Progression")]
        [SerializeField] List<StreetModule> streets = new(); // order = unlock order
        int unlockedCount = 0;
        int nightIndex = 0;

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

        int alive;

        void Start()
        {
            // Lock all streets initially
            void UnlockNextStreet()
            {
                if (unlockedCount < streets.Count) unlockedCount++;
            }
            StartCoroutine(Loop());
        }

        IEnumerator Loop()
        {
            while (true)
            {
                // ---- DAY ----
                yield return StartCoroutine(FadeEnv(0f)); // 0 = day
                yield return new WaitForSeconds(daySeconds);

                // ---- NIGHT PREP ----
                UnlockNextStreet();
                yield return StartCoroutine(FadeEnv(1f)); // 1 = night
                yield return new WaitForSeconds(nightPrepSeconds);

                // ---- NIGHT ----
                nightIndex++;
                var street = GetCurrentStreet();
                if (!street) { Debug.LogWarning("[Director] No street to spawn on."); continue; }

                int budget = startingBudget + (nightIndex - 1) * budgetPerNight;
                yield return StartCoroutine(SpawnNight(street, budget));

                // Wait for clear or timeout
                float t = 0f;
                while (alive > 0 && t < nightHardLimit)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
                // Next loop returns to DAY
            }
        }

        StreetModule GetCurrentStreet()
        {
            int idx = Mathf.Clamp(unlockedCount - 1, 0, streets.Count - 1);
            return (idx >= 0 && idx < streets.Count) ? streets[idx] : null;
        }

        void UnlockNextStreet()
        {
            if (unlockedCount < streets.Count)
            {
                var s = streets[unlockedCount];
                if (s) s.gameObject.SetActive(true);
                unlockedCount++;
            }
        }

        IEnumerator SpawnNight(StreetModule street, int budget)
        {
            var points = street.GetSpawnPoints();
            if (points.Length == 0) yield break;

            var pool = BuildPoolForNight(nightIndex);
            if (pool.Count == 0) { Debug.LogWarning("[Director] No enemies eligible for this night."); yield break; }

            alive = 0;
            var wait = new WaitForSeconds(spawnInterval);

            while (budget > 0)
            {
                var et = Pick(pool);
                if (et.cost > budget)
                { // try another; if none fit, break
                    bool anyFit = false;
                    foreach (var e in pool) if (e.cost <= budget) { anyFit = true; break; }
                    if (!anyFit) break;
                    continue;
                }

                var sp = points[Random.Range(0, points.Length)];
                var go = Instantiate(et.prefab, sp.position, sp.rotation);
                budget -= et.cost;
                alive++;

                var h = go.GetComponentInChildren<Health>();
                if (h) h.OnDeath += () => { alive = Mathf.Max(0, alive - 1); };

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

        IEnumerator FadeEnv(float night01)
        {
            // lerp env over envTransitionSeconds
            float t = 0f;
            float fromLight = sunLight ? sunLight.intensity : 0f;
            float toLight = Mathf.Lerp(dayLightIntensity, nightLightIntensity, night01);

            float fromFog = RenderSettings.fogDensity;
            float toFog = Mathf.Lerp(dayFogDensity, nightFogDensity, night01);

            float fromVol = GetVolumeWeight();
            float toVol = Mathf.Lerp(dayVolumeWeight, nightVolumeWeight, night01);

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
    }
}
