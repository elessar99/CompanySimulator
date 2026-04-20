using CompanySimulator.Features.Furniture.Runtime.Definitions;
using CompanySimulator.Features.Shop.Runtime.Definitions;

namespace CompanySimulator.Features.Inventory.Runtime.Models
{
    public sealed class InventoryItemRuntimeData
    {
        public InventoryItemRuntimeData(ShopProductDefinition product, int quantity, int firstAcquiredDay, int lastAcquiredDay)
        {
            Product = product;
            Quantity = quantity > 0 ? quantity : 0;
            FirstAcquiredDay = firstAcquiredDay;
            LastAcquiredDay = lastAcquiredDay;
        }

        public ShopProductDefinition Product { get; }
        public PlaceableFurnitureDefinition FurnitureDefinition => Product != null ? Product.FurnitureDefinition : null;
        public int FurnitureTier => Product != null ? Product.FurnitureTier : 1;
        public bool IsFurnitureItem => FurnitureDefinition != null;
        public int Quantity { get; private set; }
        public int FirstAcquiredDay { get; }
        public int LastAcquiredDay { get; private set; }

        public void AddQuantity(int amount, int acquisitionDay)
        {
            if (amount <= 0)
            {
                return;
            }

            Quantity += amount;
            LastAcquiredDay = acquisitionDay;
        }

        public bool RemoveQuantity(int amount)
        {
            if (amount <= 0 || Quantity < amount)
            {
                return false;
            }

            Quantity -= amount;
            return true;
        }
    }
}
