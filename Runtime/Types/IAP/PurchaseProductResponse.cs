using System;
using UnityEngine.Scripting;

namespace YandexGames.Types.IAP
{
    [Serializable]
    public class PurchaseProductResponse
    {
        [field: Preserve]
        public YGPurchasedProduct purchasedProduct;
        [field: Preserve]
        public string signature;
    }
}
