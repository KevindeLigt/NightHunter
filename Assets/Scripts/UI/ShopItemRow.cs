using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightHunter.combat
{
    public class ShopItemRow : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private Button buyButton;

        ShopItem item;
        System.Action<ShopItem> onBuy;

        public void Bind(ShopItem item, System.Action<ShopItem> onBuy)
        {
            this.item = item; this.onBuy = onBuy;
            if (icon) icon.sprite = item.icon;
            if (nameText) nameText.text = item.displayName;
            if (priceText) priceText.text = $"🩸 {item.price}";
            if (descText) descText.text = item.description;
            if (buyButton)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => this.onBuy?.Invoke(this.item));
            }
        }
    }
}
