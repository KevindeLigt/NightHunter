using UnityEngine;

namespace NightHunter.combat
{
    [CreateAssetMenu(menuName = "NightHunter/Shop/Item", fileName = "SI_NewItem")]
    public class ShopItem : ScriptableObject
    {
        public string displayName = "New Item";
        public Sprite icon;
        public ShopItemKind kind;
        public int price = 10;

        [Header("Payload")]
        public int amount = 30;           // Ammo/Health amount
        public WeaponId weaponId;         // for WeaponUnlock
        public SkillId skillId;           // for SkillUnlock

        [TextArea] public string description;
    }
}
