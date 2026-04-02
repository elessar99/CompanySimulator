using System;
using System.Collections.Generic;
using CompanySimulator.Features.Inventory.Runtime.Models;
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

        private void UpdateSnapshot()
        {
            inventoryItemCount = ownedItems.Count;
            nonInventoryPurchaseCount = nonInventoryPurchases.Count;
        }
    }
}
