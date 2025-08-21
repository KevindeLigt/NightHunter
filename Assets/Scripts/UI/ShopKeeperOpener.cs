using UnityEngine;

namespace NightHunter.combat
{
    [RequireComponent(typeof(Collider))]
    public class ShopkeeperOpener : MonoBehaviour
    {
        public ShopUI ui;
        public KeyCode interactKey = KeyCode.E;

        void Reset() { GetComponent<Collider>().isTrigger = true; }

        void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Player") || !ui) return;
            if (Input.GetKeyDown(interactKey))
            {
                if (ui.gameObject.activeSelf) ui.Close();
                else ui.Open();
            }
        }
    }
}
