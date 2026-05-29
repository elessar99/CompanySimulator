using System;
using System.Collections.Generic;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Features.Save.Runtime.Models;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Office.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class OfficeManager : MonoBehaviour
    {
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private bool discoverRoomsOnAwake = true;
        [SerializeField] private OfficeRoom[] configuredRooms = Array.Empty<OfficeRoom>();

        private readonly List<OfficeRoom> rooms = new List<OfficeRoom>(16);

        public event Action<OfficeRoom> RoomUnlocked;

        public IReadOnlyList<OfficeRoom> Rooms => rooms;

        private void Awake()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();

            if (discoverRoomsOnAwake)
            {
                DiscoverRooms();
            }
            else
            {
                RegisterConfiguredRooms();
            }
        }

        [ContextMenu("Discover Rooms")]
        public void DiscoverRooms()
        {
            rooms.Clear();

            var discoveredRooms = FindObjectsOfType<OfficeRoom>(true);
            for (var i = 0; i < discoveredRooms.Length; i++)
            {
                RegisterRoom(discoveredRooms[i]);
            }
        }

        public void RegisterRoom(OfficeRoom room)
        {
            if (room == null || rooms.Contains(room))
            {
                return;
            }

            room.RefreshState();
            rooms.Add(room);
        }

        public bool CanPurchaseRoom(OfficeRoom room, out string validationMessage)
        {
            validationMessage = string.Empty;

            if (room == null)
            {
                validationMessage = "Oda bulunamadi.";
                return false;
            }

            if (room.IsUnlocked)
            {
                validationMessage = "Oda zaten acik.";
                return false;
            }

            var price = room.Price;
            if (price <= Money.Zero)
            {
                return true;
            }

            economyManager ??= FindObjectOfType<EconomyManager>();
            if (economyManager == null)
            {
                validationMessage = "Ekonomi sistemi bulunamadi.";
                return false;
            }

            if (!economyManager.CanSpend(price))
            {
                validationMessage = "Yetersiz bakiye.";
                return false;
            }

            return true;
        }

        public bool TryPurchaseRoom(OfficeRoom room, out string validationMessage)
        {
            if (!CanPurchaseRoom(room, out validationMessage))
            {
                return false;
            }

            var price = room.Price;
            if (price > Money.Zero)
            {
                economyManager ??= FindObjectOfType<EconomyManager>();
                if (economyManager == null ||
                    !economyManager.TrySpend(price, LedgerEntryType.RentExpense, $"{room.DisplayName} oda satin alma"))
                {
                    validationMessage = "Oda satin alma gideri uygulanamadi.";
                    return false;
                }
            }

            room.SetUnlocked(true);
            RegisterRoom(room);
            validationMessage = string.Empty;
            RoomUnlocked?.Invoke(room);
            return true;
        }

        public bool CanPlaceFurnitureAt(Vector3 worldPosition, Collider hitCollider, out string validationMessage)
        {
            validationMessage = string.Empty;
            var placementArea = FindPlacementArea(worldPosition, hitCollider);
            if (placementArea == null)
            {
                validationMessage = "Mobilyalar yalnizca acilmis odalara yerlestirilebilir.";
                return false;
            }

            var room = placementArea.Room;
            if (room == null)
            {
                validationMessage = "Yerlestirme alani bir oda ile eslesmiyor.";
                return false;
            }

            if (!room.IsUnlocked)
            {
                validationMessage = "Bu oda henuz satin alinmadi.";
                return false;
            }

            if (!placementArea.isActiveAndEnabled)
            {
                validationMessage = "Bu odanin yerlestirme alani aktif degil.";
                return false;
            }

            return true;
        }

        public OfficeSaveData CaptureSaveData()
        {
            var saveData = new OfficeSaveData();
            for (var i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                if (room != null && room.IsUnlocked)
                {
                    saveData.unlockedRoomIds.Add(room.RoomId);
                }
            }

            return saveData;
        }

        public bool RestoreFromSaveData(OfficeSaveData saveData, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (saveData == null)
            {
                validationMessage = "Ofis oda kayıt verisi bulunamadı.";
                return false;
            }

            if (rooms.Count == 0)
            {
                DiscoverRooms();
            }

            var unlockedRoomIds = new HashSet<string>(saveData.unlockedRoomIds ?? new List<string>(), StringComparer.Ordinal);
            for (var i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                if (room == null)
                {
                    continue;
                }

                room.SetUnlocked(unlockedRoomIds.Contains(room.RoomId));
            }

            return true;
        }

        private OfficePlacementArea FindPlacementArea(Vector3 worldPosition, Collider hitCollider)
        {
            var placementArea = hitCollider != null ? hitCollider.GetComponentInParent<OfficePlacementArea>() : null;
            if (placementArea != null)
            {
                return placementArea;
            }

            for (var roomIndex = 0; roomIndex < rooms.Count; roomIndex++)
            {
                var room = rooms[roomIndex];
                if (room == null)
                {
                    continue;
                }

                var areas = room.PlacementAreas;
                for (var areaIndex = 0; areaIndex < areas.Count; areaIndex++)
                {
                    var area = areas[areaIndex];
                    if (area != null && area.ContainsPlacementPoint(worldPosition, hitCollider))
                    {
                        return area;
                    }
                }
            }

            return null;
        }

        private void RegisterConfiguredRooms()
        {
            rooms.Clear();
            if (configuredRooms == null)
            {
                return;
            }

            for (var i = 0; i < configuredRooms.Length; i++)
            {
                RegisterRoom(configuredRooms[i]);
            }
        }
    }
}
