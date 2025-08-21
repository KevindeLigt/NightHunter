using UnityEngine;
using UnityEngine.UI;

namespace NightHunter.combat
{
    public class ShopUI : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private ShopCatalog catalog;
        [SerializeField] private WeaponController weapon;
        [SerializeField] private AbilityRunner abilities;
        [SerializeField] private Health playerHealth;

        [Header("UI")]
        [SerializeField] private GameObject panel;       // root panel to show/hide
        [SerializeField] private Transform listParent;   // Content of a ScrollRect (Vertical Layout)
        [SerializeField] private ShopItemRow rowPrefab;  // simple row prefab

        void Start() { BuildList(); Close(); }

        public void Open() { if (panel) panel.SetActive(true); }
        public void Close() { if (panel) panel.SetActive(false); }

        void BuildList()
        {
            foreach (Transform c in listParent) Destroy(c.gameObject);
            if (!catalog) return;

            foreach (var item in catalog.items)
            {
                if (!item) continue;
                var row = Instantiate(rowPrefab, listParent);
                row.Bind(item, TryBuy);
            }
        }

        void TryBuy(ShopItem item)
        {
            if (item == null) return;
            if (!CurrencyWallet.Spend(item.price)) { Debug.Log("Not enough Blood Money."); return; }

            switch (item.kind)
            {
                case ShopItemKind.AmmoPack:
                    if (weapon) weapon.AddReserve(weapon.ActiveWeaponId, item.amount);
                    break;

                case ShopItemKind.HealthPack:
                    if (playerHealth) playerHealth.Heal(item.amount);
                    break;

                case ShopItemKind.WeaponUnlock:
                    if (weapon && item.weaponId != WeaponId.None && !weapon.HasWeapon(item.weaponId))
                        weapon.AddWeapon(item.weaponId, reserve: item.amount, autoEquip: true);
                    break;

                case ShopItemKind.SkillUnlock:
                    if (abilities && item.skillId != SkillId.None)
                    {
                        // drop into first empty slot, else replace slot3
                        if (abilities.slot1 == SkillId.None) abilities.slot1 = item.skillId;
                        else if (abilities.slot2 == SkillId.None) abilities.slot2 = item.skillId;
                        else if (abilities.slot3 == SkillId.None) abilities.slot3 = item.skillId;
                        else abilities.slot3 = item.skillId;
                    }
                    break;
            }
        }
    }
}
