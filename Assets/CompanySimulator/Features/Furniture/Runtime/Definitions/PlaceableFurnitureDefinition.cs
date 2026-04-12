using System;
using System.Collections.Generic;
using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Furniture.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "PlaceableFurnitureDefinition", menuName = "Company Simulator/Definitions/Furniture/Placeable Furniture")]
    public sealed class PlaceableFurnitureDefinition : DefinitionBase
    {
        [SerializeField] private string furnitureFamilyId;
        [SerializeField] private string category = "Furniture";
        [SerializeField] private bool requiresPlacement = true;
        [SerializeField] private bool allowsComputerAttachment;
        [SerializeField] private bool uniqueInOffice;
        [SerializeField] private FurnitureTierDefinition[] tiers = Array.Empty<FurnitureTierDefinition>();
        [SerializeField, TextArea(2, 5)] private string designerNotes;

        public string FurnitureFamilyId => string.IsNullOrWhiteSpace(furnitureFamilyId) ? Id : furnitureFamilyId;
        public string Category => string.IsNullOrWhiteSpace(category) ? "Furniture" : category;
        public bool RequiresPlacement => requiresPlacement;
        public bool AllowsComputerAttachment => allowsComputerAttachment;
        public bool UniqueInOffice => uniqueInOffice;
        public IReadOnlyList<FurnitureTierDefinition> Tiers => tiers;
        public string DesignerNotes => designerNotes ?? string.Empty;

        public FurnitureTierDefinition GetTier(int requestedTier)
        {
            if (tiers == null || tiers.Length == 0)
            {
                return null;
            }

            var safeTier = Mathf.Max(1, requestedTier);
            for (var i = 0; i < tiers.Length; i++)
            {
                var tierDefinition = tiers[i];
                if (tierDefinition != null && tierDefinition.Tier == safeTier)
                {
                    return tierDefinition;
                }
            }

            return tiers[0];
        }
    }
}
