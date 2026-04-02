using System;
using System.Collections.Generic;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Features.Inventory.Runtime.Components;
using CompanySimulator.Features.Shop.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Shop.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class ShopManager : MonoBehaviour
    {
        [SerializeField] private ShopSetupDefinition setup;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private bool initializeOnAwake = true;

        private readonly List<ShopCatalogDefinition> catalogs = new List<ShopCatalogDefinition>(16);
        private bool isInitialized;

        public event Action DataChanged;

        public bool IsInitialized => isInitialized;
        public IReadOnlyList<ShopCatalogDefinition> Catalogs => catalogs;

        private void Awake()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();
            inventoryManager ??= FindObjectOfType<InventoryManager>();
            if (inventoryManager == null)
            {
                inventoryManager = new GameObject("InventoryManager", typeof(InventoryManager)).GetComponent<InventoryManager>();
            }

            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        [ContextMenu("Initialize Shop")]
        public void Initialize()
        {
            catalogs.Clear();

            if (setup == null)
            {
                Debug.LogError("ShopManager için kurulum verisi atanmadı.", this);
                isInitialized = false;
                return;
            }

            var sourceCatalogs = setup.Catalogs;
            for (var i = 0; i < sourceCatalogs.Count; i++)
            {
                var catalog = sourceCatalogs[i];
                if (catalog == null || catalogs.Contains(catalog))
                {
                    continue;
                }

                catalogs.Add(catalog);
            }

            isInitialized = true;
            DataChanged?.Invoke();
        }

        public IReadOnlyList<ShopProductDefinition> GetProducts(ShopCatalogDefinition catalog)
        {
            if (!EnsureInitialized() || catalog == null)
            {
                return Array.Empty<ShopProductDefinition>();
            }

            return catalog.Products;
        }

        public bool HasPurchasedProduct(ShopProductDefinition product)
        {
            return inventoryManager != null && inventoryManager.HasPurchasedProduct(product);
        }

        public int GetOwnedQuantity(ShopProductDefinition product)
        {
            return inventoryManager != null ? inventoryManager.GetOwnedQuantity(product) : 0;
        }

        public bool CanPurchaseProduct(ShopProductDefinition product, out string validationMessage)
        {
            validationMessage = string.Empty;

            if (!EnsureInitialized())
            {
                validationMessage = "Mağaza sistemi hazır değil.";
                return false;
            }

            if (product == null)
            {
                validationMessage = "Satın alınacak ürün bulunamadı.";
                return false;
            }

            if (economyManager == null)
            {
                validationMessage = "Bakiye sistemi bulunamadı.";
                return false;
            }

            if (!product.AllowMultiplePurchases && HasPurchasedProduct(product))
            {
                validationMessage = "Bu ürün zaten satın alındı.";
                return false;
            }

            if (!economyManager.CanSpend(product.PurchasePrice))
            {
                validationMessage = "Yeterli bakiye yok.";
                return false;
            }

            return true;
        }

        public bool TryPurchaseProduct(ShopProductDefinition product, out string resultMessage)
        {
            resultMessage = string.Empty;
            if (!CanPurchaseProduct(product, out resultMessage))
            {
                return false;
            }

            if (!economyManager.TrySpend(product.PurchasePrice, LedgerEntryType.MiscExpense, product.DisplayName + " satın alındı"))
            {
                resultMessage = "Satın alma sırasında bakiye düşülemedi.";
                return false;
            }

            var purchaseDay = economyManager != null ? economyManager.CurrentDay : 1;
            if (inventoryManager == null || !inventoryManager.RecordPurchase(product, product.GrantedQuantity, purchaseDay, product.PurchasePrice))
            {
                resultMessage = "Satın alma kaydı envantere işlenemedi.";
                return false;
            }

            resultMessage = product.GoesToInventory
                ? product.DisplayName + " envantere eklendi."
                : product.DisplayName + " satın alındı.";

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
    }
}
