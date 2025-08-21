using UnityEngine;

namespace NightHunter.combat
{
    [RequireComponent(typeof(Health))]
    public class EnemyLootDrop : MonoBehaviour
    {
        public GameObject pickupPrefab;           // assign BloodCoin
        public Vector2Int amountRange = new(2, 6);
        public float scatterImpulse = 1.5f;

        Health hp;

        void Awake()
        {
            hp = GetComponent<Health>();
            hp.OnDeath += SpawnLoot;              // see Health patch below
        }

        void OnDestroy()
        {
            if (hp) hp.OnDeath -= SpawnLoot;
        }

        void SpawnLoot()
        {
            if (!pickupPrefab) return;
            var go = Instantiate(pickupPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            var coin = go.GetComponent<BloodMoneyPickup>();
            if (coin) coin.amount = Random.Range(amountRange.x, amountRange.y + 1);

            var rb = go.GetComponent<Rigidbody>();
            if (rb) rb.AddForce(Random.insideUnitSphere * scatterImpulse, ForceMode.Impulse);
        }
    }
}
