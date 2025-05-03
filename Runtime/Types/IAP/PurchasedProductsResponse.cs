using System;
using UnityEngine.Scripting;

namespace YandexGames.Types.IAP
{
    [Serializable]
    public class PurchasedProductsResponse
    {
        [field: Preserve]
        public YGPurchasedProduct[] purchasedProducts;

        [field: Preserve]
        public string signature;
    }
}
