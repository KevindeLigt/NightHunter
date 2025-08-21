using UnityEngine;

namespace NightHunter.combat
{
    [RequireComponent(typeof(Collider))]
    public class FogGateTrigger : MonoBehaviour
    {
        [Tooltip("Director to notify when the player passes the gate.")]
        public GameDirector director;

        [Tooltip("Index of the street you’re entering when stepping through this gate.")]
        public int targetStreetIndex = 1; // e.g., gate at end of Street 0 points to 1

        [Tooltip("Only allow advancing during DAY (recommended).")]
        public bool onlyDuringDay = true;

        void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (!director) return;
            if (onlyDuringDay && director.CurrentPhase != GameDirector.Phase.Day) return;

            director.PlayerEnteredGate(targetStreetIndex);
        }
    }
}
