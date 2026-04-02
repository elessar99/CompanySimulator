using CompanySimulator.Features.Shop.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Inventory.Runtime.Models
{
    public sealed class NonInventoryPurchaseRuntimeData
    {
        public NonInventoryPurchaseRuntimeData(ShopProductDefinition product, int quantity, int purchaseDay, Money totalPrice)
        {
            Product = product;
            Quantity = quantity > 0 ? quantity : 0;
            PurchaseDay = purchaseDay;
            TotalPrice = totalPrice;
        }

        public ShopProductDefinition Product { get; }
        public int Quantity { get; }
        public int PurchaseDay { get; }
        public Money TotalPrice { get; }
    }
}
