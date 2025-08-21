using UnityEngine;

namespace NightHunter.combat
{
    public class Shopkeeper : MonoBehaviour
    {
        public KeyCode interactKey = KeyCode.E;
        public int ammoPackSize = 30;
        public int ammoPackCost = 20;

        void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (!Input.GetKeyDown(interactKey)) return;

            var wc = other.GetComponentInChildren<WeaponController>() ?? other.GetComponent<WeaponController>();
            if (!wc) { Debug.Log("No WeaponController on player."); return; }

            var wd = wc.ActiveWeaponData;
            if (wd == null || !wd.usesAmmo) { Debug.Log("Active weapon doesn't use ammo."); return; }

            if (!CurrencyWallet.Spend(ammoPackCost))
            {
                Debug.Log("Not enough Blood Money.");
                return;
            }

            wc.AddReserve(wc.ActiveWeaponId, ammoPackSize);
            Debug.Log($"Bought {ammoPackSize} ammo for {ammoPackCost}. Balance: {CurrencyWallet.Balance}");
        }
    }
}
