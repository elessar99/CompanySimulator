using CompanySimulator.Features.Furniture.Runtime.Components;
using CompanySimulator.Features.Furniture.Runtime.Definitions;
using CompanySimulator.Features.Shop.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Furniture.Runtime.Models
{
    public sealed class PlacedFurnitureRuntimeData
    {
        public PlacedFurnitureRuntimeData(int runtimeId, ShopProductDefinition sourceProduct, PlaceableFurnitureDefinition furnitureDefinition, int tier, Vector3 position, Quaternion rotation, FurnitureInstance instance)
        {
            RuntimeId = runtimeId;
            SourceProduct = sourceProduct;
            FurnitureDefinition = furnitureDefinition;
            Tier = Mathf.Max(1, tier);
            Position = position;
            Rotation = rotation;
            Instance = instance;
        }

        public int RuntimeId { get; }
        public ShopProductDefinition SourceProduct { get; }
        public PlaceableFurnitureDefinition FurnitureDefinition { get; }
        public int Tier { get; }
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public FurnitureInstance Instance { get; }

        public void UpdateTransform(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}
