using UnityEngine;

namespace NightHunter.combat
{
    public class StreetModule : MonoBehaviour
    {
        [Tooltip("If empty, all child transforms except self are used as spawn points.")]
        public Transform[] spawnPoints;

        [Header("Optional")]
        public Transform shopPoint; // where the shopkeeper should appear (optional)

        // — cached renderers/lights for cheap show/hide —
        Renderer[] _renderers;
        Light[] _lights;

        void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            _lights = GetComponentsInChildren<Light>(includeInactive: true);

            if (!shopPoint)
            {
                foreach (var t in GetComponentsInChildren<Transform>(true))
                {
                    if (t.CompareTag("ShopPoint")) { shopPoint = t; break; }
                }
                if (!shopPoint)
                {
                    foreach (var t in GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name.Equals("ShopPoint", System.StringComparison.OrdinalIgnoreCase))
                        { shopPoint = t; break; }
                    }
                }
            }
        }

        public Transform[] GetSpawnPoints()
        {
            if (spawnPoints != null && spawnPoints.Length > 0) return spawnPoints;
            var list = new System.Collections.Generic.List<Transform>();
            foreach (Transform t in transform) list.Add(t);
            return list.ToArray();
        }

        public void SetVisible(bool visible)
        {
            if (_renderers == null) Awake();
            foreach (var r in _renderers) if (r) r.enabled = visible;
            foreach (var l in _lights) if (l) l.enabled = visible;
            // Colliders/navmesh stay enabled so AI/pathing doesn’t break while hidden.
        }


    }
}

