using System;
using System.Collections.Generic;
using CompanySimulator.Features.Npcs.Runtime.Office;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Office.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class OfficeRoom : MonoBehaviour
    {
        [SerializeField] private string roomId;
        [SerializeField] private string displayName = "Oda";
        [SerializeField] private string sizeLabel = "Boyut belirtilmedi";
        [SerializeField, Min(0)] private long priceAmount;
        [SerializeField] private bool startsUnlocked;
        [SerializeField] private GameObject lockedVisualRoot;
        [SerializeField] private GameObject unlockedContentRoot;
        [SerializeField] private OfficePlacementArea[] placementAreas = Array.Empty<OfficePlacementArea>();
        [SerializeField] private OfficePointOfInterest[] roomPointsOfInterest = Array.Empty<OfficePointOfInterest>();

        private bool isUnlocked;

        public string RoomId => string.IsNullOrWhiteSpace(roomId) ? name : roomId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string SizeLabel => string.IsNullOrWhiteSpace(sizeLabel) ? "Boyut belirtilmedi" : sizeLabel;
        public Money Price => Money.From(Math.Max(0, priceAmount));
        public bool IsUnlocked => isUnlocked;
        public IReadOnlyList<OfficePlacementArea> PlacementAreas => placementAreas;

        private void Awake()
        {
            ResolveReferences();
            isUnlocked = startsUnlocked;
            ApplyUnlockedState();
        }

        private void OnValidate()
        {
            if (priceAmount < 0)
            {
                priceAmount = 0;
            }

            ResolveReferences();
        }

        public void SetUnlocked(bool unlocked)
        {
            if (isUnlocked == unlocked)
            {
                ApplyUnlockedState();
                return;
            }

            isUnlocked = unlocked;
            ApplyUnlockedState();
        }

        public void RefreshState()
        {
            ResolveReferences();
            ApplyUnlockedState();
        }

        private void ResolveReferences()
        {
            if (placementAreas == null || placementAreas.Length == 0)
            {
                placementAreas = GetComponentsInChildren<OfficePlacementArea>(true);
            }

            if (roomPointsOfInterest == null || roomPointsOfInterest.Length == 0)
            {
                roomPointsOfInterest = GetComponentsInChildren<OfficePointOfInterest>(true);
            }

            for (var i = 0; i < placementAreas.Length; i++)
            {
                if (placementAreas[i] != null)
                {
                    placementAreas[i].AssignRoom(this);
                }
            }
        }

        private void ApplyUnlockedState()
        {
            if (lockedVisualRoot != null)
            {
                lockedVisualRoot.SetActive(!isUnlocked);
            }

            if (unlockedContentRoot != null)
            {
                unlockedContentRoot.SetActive(isUnlocked);
            }

            for (var i = 0; i < placementAreas.Length; i++)
            {
                if (placementAreas[i] != null)
                {
                    placementAreas[i].enabled = isUnlocked;
                }
            }

            for (var i = 0; i < roomPointsOfInterest.Length; i++)
            {
                if (roomPointsOfInterest[i] != null)
                {
                    roomPointsOfInterest[i].enabled = isUnlocked;
                }
            }
        }
    }
}
