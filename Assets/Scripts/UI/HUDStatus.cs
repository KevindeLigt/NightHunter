using UnityEngine;
using TMPro;
using UnityEngine.UI;
using NightHunter.combat;

public class HUDStatus : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WeaponController weapon;     // Player's WeaponController
    [SerializeField] private WeaponLoadout loadout;       // OPTIONAL: assign if using 2-slot
    [SerializeField] private Health playerHealth;         // Player's Health

    [Header("UI - Text")]
    [SerializeField] private TMP_Text weaponText;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text moneyText;

    [Header("UI - Equipped/Backup (optional)")]
    [SerializeField] private Image equippedIcon;
    [SerializeField] private Image backupIcon;
    [SerializeField] private TMP_Text equippedName;
    [SerializeField] private TMP_Text backupName;

    void Awake()
    {
        if (!weapon) weapon = FindObjectOfType<WeaponController>();
        if (!loadout) loadout = weapon ? weapon.GetComponent<WeaponLoadout>() : null;
    }

    void OnEnable()
    {
        CurrencyWallet.OnChanged += UpdateMoney;
        UpdateMoney(CurrencyWallet.Balance);

        if (loadout != null) loadout.OnLoadoutChanged += HandleLoadoutChanged;
        // initial paint
        if (loadout != null) HandleLoadoutChanged(loadout.Equipped, loadout.Backup);
        RefreshWeaponTexts();
        RefreshHP();
    }

    void OnDisable()
    {
        CurrencyWallet.OnChanged -= UpdateMoney;
        if (loadout != null) loadout.OnLoadoutChanged -= HandleLoadoutChanged;
    }

    void Update()
    {
        RefreshWeaponTexts();
        RefreshHP();
    }

    // ---- painters ----
    void RefreshWeaponTexts()
    {
        if (!weapon) return;
        var wd = weapon.ActiveWeaponData; // current equipped
        if (weaponText) weaponText.text = wd ? wd.displayName : "No Weapon";

        if (ammoText)
        {
            if (wd && wd.usesAmmo)
            {
                if (weapon.TryGetAmmo(out int clip, out int reserve))
                    ammoText.text = $"{clip}/{reserve}";
                else
                    ammoText.text = "--/--";
            }
            else ammoText.text = "∞";
        }
    }

    void RefreshHP()
    {
        if (playerHealth && hpText)
            hpText.text = $"{playerHealth.CurrentHP}/{playerHealth.MaxHP}";
    }

    void UpdateMoney(int bal) { if (moneyText) moneyText.text = $"🩸 {bal}"; }

    void HandleLoadoutChanged(WeaponId equipped, WeaponId backup)
    {
        var e = WeaponLibrary.Get(equipped);
        var b = WeaponLibrary.Get(backup);

        if (equippedIcon) { equippedIcon.sprite = e ? e.icon : null; equippedIcon.enabled = e; }
        if (backupIcon) { backupIcon.sprite = b ? b.icon : null; backupIcon.enabled = b; }

        if (equippedName) equippedName.text = e ? e.displayName : "-";
        if (backupName) backupName.text = b ? b.displayName : "-";
    }
}
