using UnityEngine;

namespace NightHunter.combat
{
    [RequireComponent(typeof(Collider))]
    public class BloodMoneyPickup : MonoBehaviour
    {
        public int amount = 3;
        public float lifetime = 20f;
        public float bobSpeed = 2f, bobAmp = 0.08f, spin = 90f;
        Vector3 basePos;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            basePos = transform.position;
            Destroy(gameObject, lifetime);
        }

        void Update()
        {
            transform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobAmp);
            transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            CurrencyWallet.Add(amount);
            Destroy(gameObject);
        }
    }
}

