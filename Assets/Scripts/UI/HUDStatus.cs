using UnityEngine;
using TMPro;
using NightHunter.combat;

public class HUDStatus : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WeaponController weapon; // drag your Player (with WeaponController)
    [SerializeField] private Health playerHealth;     // drag your Player's Health

    [Header("UI")]
    [SerializeField] private TMP_Text weaponText;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] TMP_Text moneyText; // assign in Inspector

    void Update()
    {
        // Weapon name + ammo
        if (weapon && weaponText && ammoText)
        {
            var wd = weapon.ActiveWeaponData;
            weaponText.text = wd ? wd.displayName : "No Weapon";

            if (wd && wd.usesAmmo)
            {
                int clip, reserve;
                if (weapon.TryGetAmmo(out clip, out reserve))
                    ammoText.text = $"{clip}/{reserve}";
                else
                    ammoText.text = "--/--";
            }
            else
            {
                ammoText.text = "∞";
            }
        }

        // HP
        if (playerHealth && hpText)
        {
            hpText.text = $"{playerHealth.CurrentHP}/{playerHealth.MaxHP}";
        }
    }

    void OnEnable() { NightHunter.combat.CurrencyWallet.OnChanged += UpdateMoney; UpdateMoney(NightHunter.combat.CurrencyWallet.Balance); }
    void OnDisable() { NightHunter.combat.CurrencyWallet.OnChanged -= UpdateMoney; }
    void UpdateMoney(int bal) { if (moneyText) moneyText.text = $"🩸 {bal}"; }


}
