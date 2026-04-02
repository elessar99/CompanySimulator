using CompanySimulator.Shared.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Shop.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "ShopProductDefinition", menuName = "Company Simulator/Definitions/Shop/Product")]
    public sealed class ShopProductDefinition : DefinitionBase
    {
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField, Min(0)] private int purchasePrice = 1000;
        [SerializeField] private ShopProductDeliveryType deliveryType = ShopProductDeliveryType.AddToInventory;
        [SerializeField, Min(1)] private int grantedQuantity = 1;
        [SerializeField] private bool allowMultiplePurchases = true;
        [SerializeField] private string inventoryCategory = "Genel";
        [SerializeField] private string futureUsageHint;

        public string Description => description ?? string.Empty;
        public Money PurchasePrice => Money.From(purchasePrice);
        public ShopProductDeliveryType DeliveryType => deliveryType;
        public int GrantedQuantity => Mathf.Max(1, grantedQuantity);
        public bool AllowMultiplePurchases => allowMultiplePurchases;
        public string InventoryCategory => string.IsNullOrWhiteSpace(inventoryCategory) ? "Genel" : inventoryCategory;
        public string FutureUsageHint => futureUsageHint ?? string.Empty;
        public bool GoesToInventory => deliveryType == ShopProductDeliveryType.AddToInventory;
    }
}
