using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanySimulator.Features.Furniture.Runtime.Definitions
{
    [Serializable]
    public sealed class FurnitureTierDefinition
    {
        [SerializeField, Min(1)] private int tier = 1;
        [SerializeField, Min(0)] private int purchasePrice;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Vector2Int footprint = Vector2Int.one;
        [SerializeField] private FurnitureInteractionType interactionTypes;
        [SerializeField] private FurnitureEffectDefinition[] effects = Array.Empty<FurnitureEffectDefinition>();
        [SerializeField, TextArea(1, 4)] private string notes;

        public int Tier => Mathf.Max(1, tier);
        public int PurchasePrice => Mathf.Max(0, purchasePrice);
        public GameObject Prefab => prefab;
        public Vector2Int Footprint => new Vector2Int(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
        public FurnitureInteractionType InteractionTypes => interactionTypes;
        public IReadOnlyList<FurnitureEffectDefinition> Effects => effects;
        public string Notes => notes ?? string.Empty;
    }
}
