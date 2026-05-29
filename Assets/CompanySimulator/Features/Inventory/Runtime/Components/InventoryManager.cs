using System;
using System.Collections.Generic;
using CompanySimulator.Features.Furniture.Runtime.Definitions;
using CompanySimulator.Features.Inventory.Runtime.Models;
using CompanySimulator.Features.Save.Runtime.Models;
using CompanySimulator.Features.Save.Runtime.Services;
using CompanySimulator.Features.Shop.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Inventory.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class InventoryManager : MonoBehaviour
    {
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private int inventoryItemCount;
        [SerializeField] private int nonInventoryPurchaseCount;

        private readonly List<InventoryItemRuntimeData> ownedItems = new List<InventoryItemRuntimeData>(64);
        private readonly List<NonInventoryPurchaseRuntimeData> nonInventoryPurchases = new List<NonInventoryPurchaseRuntimeData>(64);
        private bool isInitialized;

        public event Action DataChanged;

        public bool IsInitialized => isInitialized;
        public IReadOnlyList<InventoryItemRuntimeData> OwnedItems => ownedItems;
        public IReadOnlyList<NonInventoryPurchaseRuntimeData> NonInventoryPurchases => nonInventoryPurchases;

        private void Awake()
        {
            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        [ContextMenu("Initialize Inventory")]
        public void Initialize()
        {
            ownedItems.Clear();
            nonInventoryPurchases.Clear();
            UpdateSnapshot();
            isInitialized = true;
            DataChanged?.Invoke();
        }

        public int GetOwnedQuantity(ShopProductDefinition product)
        {
            if (!EnsureInitialized() || product == null)
            {
                return 0;
            }

            for (var i = 0; i < ownedItems.Count; i++)
            {
                if (ownedItems[i].Product == product)
                {
                    return ownedItems[i].Quantity;
                }
            }

            return 0;
        }

        public bool HasPurchasedProduct(ShopProductDefinition product)
        {
            if (!EnsureInitialized() || product == null)
            {
                return false;
            }

            if (GetOwnedQuantity(product) > 0)
            {
                return true;
            }

            for (var i = 0; i < nonInventoryPurchases.Count; i++)
            {
                if (nonInventoryPurchases[i].Product == product)
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<InventoryItemRuntimeData> GetOwnedFurnitureItems()
        {
            if (!EnsureInitialized())
            {
                return Array.Empty<InventoryItemRuntimeData>();
            }

            var result = new List<InventoryItemRuntimeData>(ownedItems.Count);
            for (var i = 0; i < ownedItems.Count; i++)
            {
                var item = ownedItems[i];
                if (item != null && item.IsFurnitureItem)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        public int GetOwnedFurnitureQuantity(PlaceableFurnitureDefinition furnitureDefinition, int tier = 0)
        {
            if (!EnsureInitialized() || furnitureDefinition == null)
            {
                return 0;
            }

            var total = 0;
            for (var i = 0; i < ownedItems.Count; i++)
            {
                var item = ownedItems[i];
                if (item == null || item.FurnitureDefinition != furnitureDefinition)
                {
                    continue;
                }

                if (tier > 0 && item.FurnitureTier != tier)
                {
                    continue;
                }

                total += item.Quantity;
            }

            return total;
        }

        public bool HasOwnedFurniture(PlaceableFurnitureDefinition furnitureDefinition, int tier = 0)
        {
            return GetOwnedFurnitureQuantity(furnitureDefinition, tier) > 0;
        }

        public bool TryConsumeOwnedItem(ShopProductDefinition product, int quantity = 1)
        {
            if (!EnsureInitialized() || product == null || quantity <= 0)
            {
                return false;
            }

            var existingItem = FindOwnedItem(product);
            if (existingItem == null || !existingItem.RemoveQuantity(quantity))
            {
                return false;
            }

            if (existingItem.Quantity <= 0)
            {
                ownedItems.Remove(existingItem);
            }

            UpdateSnapshot();
            DataChanged?.Invoke();
            return true;
        }

        public bool RecordPurchase(ShopProductDefinition product, int quantity, int purchaseDay, Money totalPrice)
        {
            if (!EnsureInitialized() || product == null || quantity <= 0)
            {
                return false;
            }

            if (product.GoesToInventory)
            {
                var existingItem = FindOwnedItem(product);
                if (existingItem == null)
                {
                    ownedItems.Add(new InventoryItemRuntimeData(product, quantity, purchaseDay, purchaseDay));
                }
                else
                {
                    existingItem.AddQuantity(quantity, purchaseDay);
                }
            }
            else
            {
                nonInventoryPurchases.Add(new NonInventoryPurchaseRuntimeData(product, quantity, purchaseDay, totalPrice));
            }

            UpdateSnapshot();
            DataChanged?.Invoke();
            return true;
        }

        public InventorySaveData CaptureSaveData()
        {
            EnsureInitialized();

            var saveData = new InventorySaveData();
            for (var i = 0; i < ownedItems.Count; i++)
            {
                var item = ownedItems[i];
                if (item == null || item.Product == null)
                {
                    continue;
                }

                saveData.ownedItems.Add(new InventoryItemSaveData
                {
                    productId = item.Product.Id,
                    quantity = item.Quantity,
                    firstAcquiredDay = item.FirstAcquiredDay,
                    lastAcquiredDay = item.LastAcquiredDay
                });
            }

            for (var i = 0; i < nonInventoryPurchases.Count; i++)
            {
                var purchase = nonInventoryPurchases[i];
                if (purchase == null || purchase.Product == null)
                {
                    continue;
                }

                saveData.nonInventoryPurchases.Add(new NonInventoryPurchaseSaveData
                {
                    productId = purchase.Product.Id,
                    quantity = purchase.Quantity,
                    purchaseDay = purchase.PurchaseDay,
                    totalPrice = purchase.TotalPrice.Amount
                });
            }

            return saveData;
        }

        public bool RestoreFromSaveData(InventorySaveData saveData, GameSaveDefinitionResolver resolver, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (saveData == null)
            {
                validationMessage = "Envanter kayıt verisi bulunamadı.";
                return false;
            }

            if (!ValidateProducts(saveData, resolver, out validationMessage))
            {
                return false;
            }

            ownedItems.Clear();
            nonInventoryPurchases.Clear();

            for (var i = 0; i < saveData.ownedItems.Count; i++)
            {
                var savedItem = saveData.ownedItems[i];
                resolver.TryResolve<ShopProductDefinition>(savedItem.productId, out var product);
                ownedItems.Add(new InventoryItemRuntimeData(
                    product,
                    savedItem.quantity,
                    savedItem.firstAcquiredDay,
                    savedItem.lastAcquiredDay));
            }

            for (var i = 0; i < saveData.nonInventoryPurchases.Count; i++)
            {
                var savedPurchase = saveData.nonInventoryPurchases[i];
                resolver.TryResolve<ShopProductDefinition>(savedPurchase.productId, out var product);
                nonInventoryPurchases.Add(new NonInventoryPurchaseRuntimeData(
                    product,
                    savedPurchase.quantity,
                    savedPurchase.purchaseDay,
                    Money.From(savedPurchase.totalPrice)));
            }

            isInitialized = true;
            UpdateSnapshot();
            DataChanged?.Invoke();
            return true;
        }

        private bool EnsureInitialized()
        {
            if (isInitialized)
            {
                return true;
            }

            Initialize();
            return isInitialized;
        }

        private InventoryItemRuntimeData FindOwnedItem(ShopProductDefinition product)
        {
            for (var i = 0; i < ownedItems.Count; i++)
            {
                if (ownedItems[i].Product == product)
                {
                    return ownedItems[i];
                }
            }

            return null;
        }

        private static bool ValidateProducts(InventorySaveData saveData, GameSaveDefinitionResolver resolver, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (resolver == null)
            {
                validationMessage = "Tanım çözücü bulunamadı.";
                return false;
            }

            for (var i = 0; i < saveData.ownedItems.Count; i++)
            {
                var productId = saveData.ownedItems[i].productId;
                if (!string.IsNullOrWhiteSpace(productId) && !resolver.TryResolve<ShopProductDefinition>(productId, out _))
                {
                    validationMessage = $"Envanter ürün tanımı bulunamadı: {productId}";
                    return false;
                }
            }

            for (var i = 0; i < saveData.nonInventoryPurchases.Count; i++)
            {
                var productId = saveData.nonInventoryPurchases[i].productId;
                if (!string.IsNullOrWhiteSpace(productId) && !resolver.TryResolve<ShopProductDefinition>(productId, out _))
                {
                    validationMessage = $"Satın alma ürün tanımı bulunamadı: {productId}";
                    return false;
                }
            }

            return true;
        }

        private void UpdateSnapshot()
        {
            inventoryItemCount = ownedItems.Count;
            nonInventoryPurchaseCount = nonInventoryPurchases.Count;
        }
    }
}
