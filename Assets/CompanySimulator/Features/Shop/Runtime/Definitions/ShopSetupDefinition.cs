using System;
using System.Collections.Generic;
using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Shop.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "ShopSetupDefinition", menuName = "Company Simulator/Definitions/Shop/Setup")]
    public sealed class ShopSetupDefinition : DefinitionBase
    {
        [SerializeField] private ShopCatalogDefinition[] catalogs = Array.Empty<ShopCatalogDefinition>();

        public IReadOnlyList<ShopCatalogDefinition> Catalogs => catalogs;
    }
}
