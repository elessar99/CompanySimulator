using System;
using System.Collections.Generic;
using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Shop.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "ShopCatalogDefinition", menuName = "Company Simulator/Definitions/Shop/Catalog")]
    public sealed class ShopCatalogDefinition : DefinitionBase
    {
        [SerializeField, TextArea(2, 4)] private string description;
        [SerializeField] private ShopProductDefinition[] products = Array.Empty<ShopProductDefinition>();

        public string Description => description ?? string.Empty;
        public IReadOnlyList<ShopProductDefinition> Products => products;
    }
}
