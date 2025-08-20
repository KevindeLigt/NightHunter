using UnityEngine;

namespace NightHunter.combat
{
    // One “chunk/street” you unlock per night. Add spawn points as children or assign explicitly.
    public class StreetModule : MonoBehaviour
    {
        [Tooltip("If empty, all child transforms except self are used.")]
        public Transform[] spawnPoints;

        public Transform[] GetSpawnPoints()
        {
            if (spawnPoints != null && spawnPoints.Length > 0) return spawnPoints;
            var list = new System.Collections.Generic.List<Transform>();
            foreach (Transform t in transform) list.Add(t);
            return list.ToArray();
        }
    }
}
