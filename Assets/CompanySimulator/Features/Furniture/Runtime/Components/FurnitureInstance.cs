using CompanySimulator.Features.Furniture.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Furniture.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class FurnitureInstance : MonoBehaviour
    {
        [SerializeField] private PlaceableFurnitureDefinition definition;
        [SerializeField, Min(1)] private int tier = 1;

        public PlaceableFurnitureDefinition Definition => definition;
        public int Tier => Mathf.Max(1, tier);
        public FurnitureTierDefinition ActiveTier => definition != null ? definition.GetTier(Tier) : null;

        public bool SupportsInteraction(FurnitureInteractionType interactionType)
        {
            return ActiveTier != null && (ActiveTier.InteractionTypes & interactionType) == interactionType;
        }

        public void Configure(PlaceableFurnitureDefinition furnitureDefinition, int furnitureTier)
        {
            definition = furnitureDefinition;
            tier = Mathf.Max(1, furnitureTier);
        }
    }
}
