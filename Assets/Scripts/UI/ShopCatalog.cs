using System.Collections.Generic;
using UnityEngine;

namespace NightHunter.combat
{
    [CreateAssetMenu(menuName = "NightHunter/Shop/Catalog", fileName = "SC_Catalog")]
    public class ShopCatalog : ScriptableObject
    {
        public List<ShopItem> items = new();
    }
}
