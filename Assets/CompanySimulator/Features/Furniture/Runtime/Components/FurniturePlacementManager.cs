using System;
using System.Collections.Generic;
using CompanySimulator.Features.Furniture.Runtime.Definitions;
using CompanySimulator.Features.Furniture.Runtime.Models;
using CompanySimulator.Features.Inventory.Runtime.Components;
using CompanySimulator.Features.Shop.Runtime.Definitions;
using CompanySimulator.Presentation.UI.Runtime.Components;
using UnityEngine;

namespace CompanySimulator.Features.Furniture.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class FurniturePlacementManager : MonoBehaviour
    {
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private Transform placedFurnitureRoot;
        [SerializeField] private LayerMask placementSurfaceMask = Physics.DefaultRaycastLayers;
        [SerializeField] private LayerMask overlapBlockingMask = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0.5f)] private float maxPlacementDistance = 8f;
        [SerializeField, Min(0f)] private float placementSurfaceOffset = 0.01f;
        [SerializeField, Min(0f)] private float overlapPadding = 0.01f;
        [SerializeField, Min(1f)] private float rotationStepDegrees = 15f;
        [SerializeField] private Color previewValidColor = new Color(0.302f, 0.886f, 0.816f, 0.42f);
        [SerializeField] private Color previewInvalidColor = new Color(1f, 0.42f, 0.506f, 0.42f);

        private readonly List<PlacedFurnitureRuntimeData> placedFurniture = new List<PlacedFurnitureRuntimeData>(64);
        private ShopProductDefinition pendingProduct;
        private int nextRuntimeId = 1;
        private bool isBuildModeActive;
        private GameObject previewInstanceObject;
        private FurnitureInstance previewFurnitureInstance;
        private Renderer[] previewRenderers = Array.Empty<Renderer>();
        private MaterialPropertyBlock previewPropertyBlock;
        private float pendingYawDegrees;
        private bool previewCanPlace;
        private readonly List<Transform> previewLayerTransforms = new List<Transform>(32);
        private readonly List<int> previewOriginalLayers = new List<int>(32);

        public event Action PlacementChanged;
        public event Action PlacementModeChanged;

        public IReadOnlyList<PlacedFurnitureRuntimeData> PlacedFurniture => placedFurniture;
        public bool IsBuildModeActive => isBuildModeActive;
        public bool HasPendingPlacement => pendingProduct != null;
        public ShopProductDefinition PendingProduct => pendingProduct;
        public string PendingPlacementLabel => pendingProduct != null ? pendingProduct.DisplayName : string.Empty;
        public bool PreviewCanPlace => previewCanPlace;

        private void Awake()
        {
            inventoryManager ??= FindObjectOfType<InventoryManager>();
        }

        private void Start()
        {
            if (FindObjectOfType<BuildModePanelUI>() == null)
            {
                new GameObject("BuildModePanelUI", typeof(BuildModePanelUI));
            }
        }

        public void SetBuildMode(bool isActive)
        {
            if (isBuildModeActive == isActive)
            {
                if (!isActive && pendingProduct != null)
                {
                    pendingProduct = null;
                    ClearPreview();
                    PlacementModeChanged?.Invoke();
                }

                return;
            }

            isBuildModeActive = isActive;
            if (!isBuildModeActive)
            {
                pendingProduct = null;
                ClearPreview();
            }

            PlacementModeChanged?.Invoke();
        }

        public bool ToggleBuildMode()
        {
            SetBuildMode(!isBuildModeActive);
            return isBuildModeActive;
        }

        public bool BeginPlacement(ShopProductDefinition product, out string validationMessage)
        {
            validationMessage = string.Empty;

            if (product == null)
            {
                validationMessage = "Yerleştirilecek ürün bulunamadı.";
                return false;
            }

            if (!product.IsFurnitureProduct || product.FurnitureDefinition == null)
            {
                validationMessage = "Seçilen ürün yerleştirilebilir mobilya değil.";
                return false;
            }

            inventoryManager ??= FindObjectOfType<InventoryManager>();
            if (inventoryManager == null)
            {
                validationMessage = "Envanter sistemi bulunamadı.";
                return false;
            }

            if (inventoryManager.GetOwnedQuantity(product) <= 0)
            {
                validationMessage = "Bu üründen envanterde kalmadı.";
                return false;
            }

            var tierDefinition = product.FurnitureDefinition.GetTier(product.FurnitureTier);
            if (tierDefinition == null || tierDefinition.Prefab == null)
            {
                validationMessage = "Furniture tier prefabı atanmadı.";
                return false;
            }

            if (!isBuildModeActive)
            {
                isBuildModeActive = true;
            }

            pendingProduct = product;
            pendingYawDegrees = 0f;
            EnsurePreviewInstance(tierDefinition, product);
            PlacementModeChanged?.Invoke();
            return true;
        }

        public void CancelPlacement()
        {
            if (pendingProduct == null)
            {
                return;
            }

            pendingProduct = null;
            ClearPreview();
            PlacementModeChanged?.Invoke();
        }

        public void RotatePendingPlacement(float deltaDegrees)
        {
            if (pendingProduct == null)
            {
                return;
            }

            pendingYawDegrees += deltaDegrees;
            if (pendingYawDegrees >= 360f || pendingYawDegrees <= -360f)
            {
                pendingYawDegrees %= 360f;
            }
        }

        public bool UpdatePreviewFromRay(Ray ray, Vector3 forward, out string validationMessage)
        {
            validationMessage = string.Empty;
            previewCanPlace = false;

            if (pendingProduct == null)
            {
                ClearPreview();
                validationMessage = "Yerleştirme için seçili eşya yok.";
                return false;
            }

            if (!Physics.Raycast(ray, out var hit, maxPlacementDistance, placementSurfaceMask, QueryTriggerInteraction.Ignore))
            {
                SetPreviewVisible(false);
                validationMessage = "Yerleştirme yüzeyi bulunamadı.";
                return false;
            }

            return UpdatePreviewAt(hit.point, hit.normal, hit.collider, forward, out validationMessage);
        }

        public bool TryPlaceFromRay(Ray ray, Vector3 forward, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (pendingProduct == null)
            {
                validationMessage = "Yerleştirme için seçili eşya yok.";
                return false;
            }

            if (!Physics.Raycast(ray, out var hit, maxPlacementDistance, placementSurfaceMask, QueryTriggerInteraction.Ignore))
            {
                validationMessage = "Yerleştirme yüzeyi bulunamadı.";
                return false;
            }

            if (!UpdatePreviewAt(hit.point, hit.normal, hit.collider, forward, out validationMessage))
            {
                return false;
            }

            if (!previewCanPlace || previewInstanceObject == null)
            {
                validationMessage = string.IsNullOrEmpty(validationMessage) ? "Bu eşya buraya yerleştirilemiyor." : validationMessage;
                return false;
            }

            return CommitPreviewPlacement(out validationMessage);
        }

        private bool UpdatePreviewAt(Vector3 hitPoint, Vector3 surfaceNormal, Collider hitCollider, Vector3 forward, out string validationMessage)
        {
            validationMessage = string.Empty;
            var product = pendingProduct;
            if (product == null || product.FurnitureDefinition == null)
            {
                validationMessage = "Yerleştirme verisi geçersiz.";
                return false;
            }

            var tierDefinition = product.FurnitureDefinition.GetTier(product.FurnitureTier);
            if (tierDefinition == null || tierDefinition.Prefab == null)
            {
                validationMessage = "Yerleştirilecek prefab bulunamadı.";
                return false;
            }

            EnsurePlacementRoot();
            EnsurePreviewInstance(tierDefinition, product);
            if (previewInstanceObject == null || previewFurnitureInstance == null)
            {
                validationMessage = "Preview oluşturulamadı.";
                return false;
            }

            var projectedForward = Vector3.ProjectOnPlane(forward, surfaceNormal);
            if (projectedForward.sqrMagnitude < 0.001f)
            {
                projectedForward = Vector3.ProjectOnPlane(Vector3.forward, surfaceNormal);
            }

            if (projectedForward.sqrMagnitude < 0.001f)
            {
                projectedForward = Vector3.forward;
            }

            var rotation = Quaternion.LookRotation(projectedForward.normalized, surfaceNormal) * Quaternion.Euler(0f, pendingYawDegrees, 0f);
            var position = hitPoint + (surfaceNormal.normalized * placementSurfaceOffset);
            previewInstanceObject.transform.SetPositionAndRotation(position, rotation);
            previewFurnitureInstance.Configure(product.FurnitureDefinition, product.FurnitureTier);
            SetPreviewVisible(true);

            previewCanPlace = !HasBlockingOverlap(previewInstanceObject, hitCollider);
            ApplyPreviewColor(previewCanPlace ? previewValidColor : previewInvalidColor);

            if (!previewCanPlace)
            {
                validationMessage = "Bu eşya başka bir nesnenin içine giriyor.";
            }

            return previewCanPlace;
        }

        private bool CommitPreviewPlacement(out string validationMessage)
        {
            validationMessage = string.Empty;
            var product = pendingProduct;
            if (product == null || previewInstanceObject == null || previewFurnitureInstance == null)
            {
                validationMessage = "Yerleştirme preview verisi geçersiz.";
                return false;
            }

            inventoryManager ??= FindObjectOfType<InventoryManager>();
            if (inventoryManager == null || !inventoryManager.TryConsumeOwnedItem(product, 1))
            {
                validationMessage = "Envanterden ürün düşülemedi.";
                return false;
            }

            RestorePreviewAsPlaced(previewInstanceObject, previewLayerTransforms, previewOriginalLayers);
            placedFurniture.Add(new PlacedFurnitureRuntimeData(nextRuntimeId++, product, product.FurnitureDefinition, product.FurnitureTier, previewInstanceObject.transform.position, previewInstanceObject.transform.rotation, previewFurnitureInstance));
            previewInstanceObject = null;
            previewFurnitureInstance = null;
            previewRenderers = Array.Empty<Renderer>();
            previewCanPlace = false;
            pendingProduct = null;
            PlacementChanged?.Invoke();
            PlacementModeChanged?.Invoke();
            return true;
        }

        private bool HasBlockingOverlap(GameObject instanceObject, Collider ignoredCollider)
        {
            var colliders = instanceObject.GetComponentsInChildren<Collider>();
            for (var i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider == null || !collider.enabled || collider.isTrigger)
                {
                    continue;
                }

                var bounds = collider.bounds;
                var overlaps = Physics.OverlapBox(
                    bounds.center,
                    MaxVector(bounds.extents - Vector3.one * overlapPadding, Vector3.one * 0.001f),
                    Quaternion.identity,
                    overlapBlockingMask,
                    QueryTriggerInteraction.Ignore);

                for (var j = 0; j < overlaps.Length; j++)
                {
                    var other = overlaps[j];
                    if (other == null || other.transform.IsChildOf(instanceObject.transform) || other.isTrigger)
                    {
                        continue;
                    }

                    if (ignoredCollider != null &&
                        (other == ignoredCollider ||
                         other.transform.IsChildOf(ignoredCollider.transform) ||
                         ignoredCollider.transform.IsChildOf(other.transform)))
                    {
                        continue;
                    }

                    if (Physics.ComputePenetration(
                            collider,
                            collider.transform.position,
                            collider.transform.rotation,
                            other,
                            other.transform.position,
                            other.transform.rotation,
                            out _,
                            out var distance) && distance > 0.001f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void EnsurePreviewInstance(FurnitureTierDefinition tierDefinition, ShopProductDefinition product)
        {
            if (tierDefinition == null || tierDefinition.Prefab == null || product == null)
            {
                ClearPreview();
                return;
            }

            if (previewInstanceObject != null && previewFurnitureInstance != null && previewFurnitureInstance.Definition == product.FurnitureDefinition && previewFurnitureInstance.Tier == product.FurnitureTier)
            {
                return;
            }

            ClearPreview();
            previewInstanceObject = Instantiate(tierDefinition.Prefab, Vector3.zero, Quaternion.identity, placedFurnitureRoot);
            previewInstanceObject.name = tierDefinition.Prefab.name + "_Preview";
            previewFurnitureInstance = previewInstanceObject.GetComponent<FurnitureInstance>();
            if (previewFurnitureInstance == null)
            {
                previewFurnitureInstance = previewInstanceObject.AddComponent<FurnitureInstance>();
            }

            previewFurnitureInstance.Configure(product.FurnitureDefinition, product.FurnitureTier);
            previewRenderers = previewInstanceObject.GetComponentsInChildren<Renderer>(true);
            previewPropertyBlock ??= new MaterialPropertyBlock();

            var colliders = previewInstanceObject.GetComponentsInChildren<Collider>(true);
            previewLayerTransforms.Clear();
            previewOriginalLayers.Clear();
            for (var i = 0; i < colliders.Length; i++)
            {
                var colliderTransform = colliders[i].transform;
                previewLayerTransforms.Add(colliderTransform);
                previewOriginalLayers.Add(colliderTransform.gameObject.layer);
                colliderTransform.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            }

            SetPreviewVisible(false);
            ApplyPreviewColor(previewValidColor);
        }

        private void ClearPreview()
        {
            if (previewInstanceObject != null)
            {
                Destroy(previewInstanceObject);
            }

            previewInstanceObject = null;
            previewFurnitureInstance = null;
            previewRenderers = Array.Empty<Renderer>();
            previewCanPlace = false;
            previewLayerTransforms.Clear();
            previewOriginalLayers.Clear();
        }

        private void SetPreviewVisible(bool isVisible)
        {
            if (previewInstanceObject == null)
            {
                return;
            }

            previewInstanceObject.SetActive(isVisible);
        }

        private void ApplyPreviewColor(Color color)
        {
            if (previewRenderers == null)
            {
                return;
            }

            for (var i = 0; i < previewRenderers.Length; i++)
            {
                var renderer = previewRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var material = renderer.sharedMaterial;
                if (material == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(previewPropertyBlock);
                previewPropertyBlock.Clear();

                var hasColor = material.HasProperty("_Color");
                var hasBaseColor = material.HasProperty("_BaseColor");
                if (!hasColor && !hasBaseColor)
                {
                    renderer.SetPropertyBlock(null);
                    continue;
                }

                if (hasColor)
                {
                    previewPropertyBlock.SetColor("_Color", color);
                }

                if (hasBaseColor)
                {
                    previewPropertyBlock.SetColor("_BaseColor", color);
                }

                renderer.SetPropertyBlock(previewPropertyBlock);
            }
        }

        private static void RestorePreviewAsPlaced(GameObject instanceObject, List<Transform> layerTransforms, List<int> originalLayers)
        {
            if (instanceObject == null)
            {
                return;
            }

            var renderers = instanceObject.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].SetPropertyBlock(null);
            }

            var colliders = instanceObject.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = true;
            }

            var restoreCount = Mathf.Min(layerTransforms != null ? layerTransforms.Count : 0, originalLayers != null ? originalLayers.Count : 0);
            for (var i = 0; i < restoreCount; i++)
            {
                if (layerTransforms[i] != null)
                {
                    layerTransforms[i].gameObject.layer = originalLayers[i];
                }
            }

            instanceObject.name = instanceObject.name.Replace("_Preview", string.Empty);
        }

        private static Vector3 MaxVector(Vector3 a, Vector3 b)
        {
            return new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z));
        }

        private void EnsurePlacementRoot()
        {
            if (placedFurnitureRoot != null)
            {
                return;
            }

            var existing = transform.Find("PlacedFurnitureRoot");
            if (existing != null)
            {
                placedFurnitureRoot = existing;
                return;
            }

            placedFurnitureRoot = new GameObject("PlacedFurnitureRoot").transform;
            placedFurnitureRoot.SetParent(transform, false);
        }
    }
}
