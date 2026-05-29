using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CompanySimulator.Features.Agents.Runtime.Components;
using CompanySimulator.Features.Agents.Runtime.Definitions;
using CompanySimulator.Features.Banking.Runtime.Components;
using CompanySimulator.Features.Employees.Runtime.Components;
using CompanySimulator.Features.Employees.Runtime.Definitions;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Furniture.Runtime.Components;
using CompanySimulator.Features.Inventory.Runtime.Components;
using CompanySimulator.Features.Investments.Runtime.Definitions;
using CompanySimulator.Features.Npcs.Runtime.Interview;
using CompanySimulator.Features.Office.Runtime.Components;
using CompanySimulator.Features.Player.Runtime.Components;
using CompanySimulator.Features.Rivals.Runtime.Components;
using CompanySimulator.Features.Rivals.Runtime.Definitions;
using CompanySimulator.Features.Save.Runtime.Models;
using CompanySimulator.Features.Sectors.Runtime.Components;
using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Features.Shop.Runtime.Definitions;
using CompanySimulator.Features.Time.Runtime.Components;
using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;

namespace CompanySimulator.Features.Save.Runtime.Services
{
    [DisallowMultipleComponent]
    public sealed class GameSaveManager : MonoBehaviour
    {
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private EmployeeManager employeeManager;
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private FurniturePlacementManager furniturePlacementManager;
        [SerializeField] private OfficeManager officeManager;
        [SerializeField] private CompanyBankManager bankManager;
        [SerializeField] private RivalCompanyManager rivalCompanyManager;
        [SerializeField] private AgentManager agentManager;
        [SerializeField] private SectorManager sectorManager;
        [SerializeField] private PlayerMovementController playerMovementController;
        [SerializeField] private InterviewSessionManager interviewSessionManager;

        private readonly List<GameSaveSlotInfo> slots = new List<GameSaveSlotInfo>(16);
        private GameSaveDefinitionResolver resolver;

        public IReadOnlyList<GameSaveSlotInfo> Slots => slots;
        public string SaveDirectory => Path.Combine(Application.persistentDataPath, "CompanySimulator", "Saves");

        private void Awake()
        {
            ResolveSceneReferences();
            RefreshSlots();
        }

        public IReadOnlyList<GameSaveSlotInfo> RefreshSlots()
        {
            slots.Clear();
            Directory.CreateDirectory(SaveDirectory);

            var files = Directory.GetFiles(SaveDirectory, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < files.Length; i++)
            {
                slots.Add(ReadSlotInfo(files[i]));
            }

            slots.Sort((left, right) => string.Compare(right.SavedAtUtc, left.SavedAtUtc, StringComparison.Ordinal));
            return slots;
        }

        public bool TryCreateNewSave(out GameSaveSlotInfo slotInfo, out string resultMessage)
        {
            slotInfo = null;
            var slotId = "save_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var filePath = BuildSlotPath(slotId);
            if (!TryWriteSave(slotId, filePath, out resultMessage))
            {
                return false;
            }

            RefreshSlots();
            slotInfo = FindSlot(slotId);
            resultMessage = "Yeni kayıt oluşturuldu.";
            return true;
        }

        public bool TryOverwriteSave(GameSaveSlotInfo selectedSlot, out string resultMessage)
        {
            if (selectedSlot == null || selectedSlot.IsCorrupt || string.IsNullOrWhiteSpace(selectedSlot.SlotId))
            {
                resultMessage = "Üstüne kaydedilecek geçerli bir kayıt seçilmedi.";
                return false;
            }

            if (!TryWriteSave(selectedSlot.SlotId, selectedSlot.FilePath, out resultMessage))
            {
                return false;
            }

            RefreshSlots();
            resultMessage = "Seçili kayıt güncellendi.";
            return true;
        }

        public bool TryLoadSave(GameSaveSlotInfo selectedSlot, out string resultMessage)
        {
            try
            {
                return TryLoadSaveCore(selectedSlot, out resultMessage);
            }
            catch (Exception exception)
            {
                resultMessage = "Kay\u0131t y\u00fcklenemedi: " + exception.Message;
                RefreshSlots();
                return false;
            }
        }

        private bool TryLoadSaveCore(GameSaveSlotInfo selectedSlot, out string resultMessage)
        {
            resultMessage = string.Empty;
            if (selectedSlot == null || selectedSlot.IsCorrupt || string.IsNullOrWhiteSpace(selectedSlot.FilePath))
            {
                resultMessage = "Yüklenecek geçerli bir kayıt seçilmedi.";
                return false;
            }

            if (IsInterviewActive())
            {
                resultMessage = "Aktif görüşme varken kayıt yüklenemez.";
                return false;
            }

            if (!File.Exists(selectedSlot.FilePath))
            {
                resultMessage = "Kayıt dosyası bulunamadı.";
                RefreshSlots();
                return false;
            }

            GameSaveData saveData;
            try
            {
                saveData = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(selectedSlot.FilePath));
            }
            catch (Exception exception)
            {
                resultMessage = "Kayıt dosyası okunamadı: " + exception.Message;
                RefreshSlots();
                return false;
            }

            ResolveSceneReferences();
            EnsureResolver().Refresh();

            if (!ValidateManagers(out resultMessage) ||
                !ValidateSaveData(saveData, out resultMessage))
            {
                return false;
            }

            if (!employeeManager.RestoreFromSaveData(saveData.employees, resolver, out var employeeLookup, out resultMessage) ||
                !inventoryManager.RestoreFromSaveData(saveData.inventory, resolver, out resultMessage) ||
                !officeManager.RestoreFromSaveData(saveData.office, out resultMessage) ||
                !economyManager.RestoreFromSaveData(saveData.economy, resolver, employeeLookup, out resultMessage) ||
                !bankManager.RestoreFromSaveData(saveData.bank, out resultMessage) ||
                !rivalCompanyManager.RestoreFromSaveData(saveData.rivals, resolver, out resultMessage) ||
                !agentManager.RestoreFromSaveData(saveData.agents, resolver, out resultMessage) ||
                !furniturePlacementManager.RestoreFromSaveData(saveData.furniture, resolver, out resultMessage))
            {
                return false;
            }

            timeManager.RestoreFromSaveData(saveData.time);
            playerMovementController.RestoreFromSaveData(saveData.player);
            ClearTransientUiState();
            sectorManager?.ForceRefreshFromEconomy();
            rivalCompanyManager?.ForceRebuildCompetitionCache();
            RefreshSlots();
            resultMessage = "Kayıt yüklendi.";
            return true;
        }

        public bool TryDeleteSave(GameSaveSlotInfo selectedSlot, out string resultMessage)
        {
            if (selectedSlot == null || string.IsNullOrWhiteSpace(selectedSlot.FilePath))
            {
                resultMessage = "Silinecek bir kay\u0131t se\u00e7ilmedi.";
                return false;
            }

            try
            {
                if (File.Exists(selectedSlot.FilePath))
                {
                    File.Delete(selectedSlot.FilePath);
                }

                RefreshSlots();
                resultMessage = "Se\u00e7ili kay\u0131t silindi.";
                return true;
            }
            catch (Exception exception)
            {
                resultMessage = "Kay\u0131t silinemedi: " + exception.Message;
                RefreshSlots();
                return false;
            }
        }

        private bool TryWriteSave(string slotId, string filePath, out string resultMessage)
        {
            resultMessage = string.Empty;
            if (IsInterviewActive())
            {
                resultMessage = "Aktif görüşme varken kayıt alınamaz.";
                return false;
            }

            ResolveSceneReferences();
            if (!ValidateManagers(out resultMessage))
            {
                return false;
            }

            var saveData = CaptureSaveData(slotId);
            try
            {
                Directory.CreateDirectory(SaveDirectory);
                File.WriteAllText(filePath, JsonUtility.ToJson(saveData, true));
                return true;
            }
            catch (Exception exception)
            {
                resultMessage = "Kayıt dosyası yazılamadı: " + exception.Message;
                return false;
            }
        }

        private GameSaveData CaptureSaveData(string slotId)
        {
            var saveData = new GameSaveData
            {
                metadata = new GameSaveMetadata
                {
                    slotId = slotId,
                    displayName = slotId,
                    savedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                    currentDay = economyManager.CurrentDay,
                    currentTotalMinutes = timeManager.CurrentTotalMinutes,
                    balance = economyManager.Balance.Amount
                },
                economy = economyManager.CaptureSaveData(),
                time = timeManager.CaptureSaveData(),
                employees = employeeManager.CaptureSaveData(),
                inventory = inventoryManager.CaptureSaveData(),
                furniture = furniturePlacementManager.CaptureSaveData(),
                office = officeManager.CaptureSaveData(),
                bank = bankManager.CaptureSaveData(),
                rivals = rivalCompanyManager.CaptureSaveData(),
                agents = agentManager.CaptureSaveData(),
                player = playerMovementController.CaptureSaveData()
            };

            return saveData;
        }

        private bool ValidateManagers(out string validationMessage)
        {
            validationMessage = string.Empty;
            if (economyManager == null || timeManager == null || employeeManager == null ||
                inventoryManager == null || furniturePlacementManager == null || officeManager == null ||
                bankManager == null || rivalCompanyManager == null || agentManager == null ||
                playerMovementController == null)
            {
                validationMessage = "Kayıt sistemi için gerekli sahne yöneticilerinden biri bulunamadı.";
                return false;
            }

            return true;
        }

        private bool ValidateSaveData(GameSaveData saveData, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (saveData == null)
            {
                validationMessage = "Kayıt verisi boş veya bozuk.";
                return false;
            }

            var employeeIds = new HashSet<string>(StringComparer.Ordinal);
            if (!ValidateEmployees(saveData.employees, employeeIds, out validationMessage) ||
                !ValidateInventory(saveData.inventory, out validationMessage) ||
                !ValidateEconomy(saveData.economy, employeeIds, out validationMessage) ||
                !ValidateFurniture(saveData.furniture, out validationMessage) ||
                !ValidateRivals(saveData.rivals, out validationMessage) ||
                !ValidateAgents(saveData.agents, saveData.economy, saveData.rivals, out validationMessage))
            {
                return false;
            }

            return true;
        }

        private bool ValidateEmployees(EmployeeManagerSaveData saveData, HashSet<string> employeeIds, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (saveData == null)
            {
                validationMessage = "Çalışan kayıt verisi bulunamadı.";
                return false;
            }

            for (var i = 0; i < saveData.employees.Count; i++)
            {
                if (!ValidateEmployee(saveData.employees[i], out validationMessage))
                {
                    return false;
                }

                employeeIds.Add(saveData.employees[i].id);
            }

            for (var i = 0; i < saveData.applicants.Count; i++)
            {
                if (!ValidateEmployee(saveData.applicants[i], out validationMessage))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ValidateEmployee(EmployeeSaveData employee, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (employee == null || string.IsNullOrWhiteSpace(employee.roleId))
            {
                return true;
            }

            if (!resolver.TryResolve<EmployeeRoleDefinition>(employee.roleId, out _))
            {
                validationMessage = "Çalışan rol tanımı bulunamadı: " + employee.roleId;
                return false;
            }

            return true;
        }

        private bool ValidateInventory(InventorySaveData saveData, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (saveData == null)
            {
                validationMessage = "Envanter kayıt verisi bulunamadı.";
                return false;
            }

            for (var i = 0; i < saveData.ownedItems.Count; i++)
            {
                if (!ValidateProduct(saveData.ownedItems[i].productId, out validationMessage))
                {
                    return false;
                }
            }

            for (var i = 0; i < saveData.nonInventoryPurchases.Count; i++)
            {
                if (!ValidateProduct(saveData.nonInventoryPurchases[i].productId, out validationMessage))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ValidateProduct(string productId, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (!string.IsNullOrWhiteSpace(productId) && !resolver.TryResolve<ShopProductDefinition>(productId, out _))
            {
                validationMessage = "Ürün tanımı bulunamadı: " + productId;
                return false;
            }

            return true;
        }

        private bool ValidateEconomy(EconomySaveData saveData, HashSet<string> employeeIds, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (saveData == null)
            {
                validationMessage = "Ekonomi kayıt verisi bulunamadı.";
                return false;
            }

            for (var i = 0; i < saveData.activeProjects.Count; i++)
            {
                var project = saveData.activeProjects[i];
                if (!string.IsNullOrWhiteSpace(project.sourceDefinitionId) &&
                    !resolver.TryResolve<ProjectExecutionDefinition>(project.sourceDefinitionId, out _))
                {
                    validationMessage = "Aktif iş tanımı bulunamadı: " + project.sourceDefinitionId;
                    return false;
                }

                for (var employeeIndex = 0; employeeIndex < project.assignedEmployeeIds.Count; employeeIndex++)
                {
                    var employeeId = project.assignedEmployeeIds[employeeIndex];
                    if (!string.IsNullOrWhiteSpace(employeeId) && !employeeIds.Contains(employeeId))
                    {
                        validationMessage = "Aktif iş çalışanı bulunamadı: " + employeeId;
                        return false;
                    }
                }

                for (var allocationIndex = 0; allocationIndex < project.investmentAllocations.Count; allocationIndex++)
                {
                    var investmentId = project.investmentAllocations[allocationIndex].investmentTypeId;
                    if (!string.IsNullOrWhiteSpace(investmentId) &&
                        !resolver.TryResolve<InvestmentTypeDefinition>(investmentId, out _))
                    {
                        validationMessage = "Yatırım tanımı bulunamadı: " + investmentId;
                        return false;
                    }
                }
            }

            return true;
        }

        private bool ValidateFurniture(FurniturePlacementSaveData saveData, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (saveData == null)
            {
                validationMessage = "Mobilya kayıt verisi bulunamadı.";
                return false;
            }

            for (var i = 0; i < saveData.placedFurniture.Count; i++)
            {
                var placed = saveData.placedFurniture[i];
                if (!resolver.TryResolve<ShopProductDefinition>(placed.sourceProductId, out var product) ||
                    product.FurnitureDefinition == null ||
                    product.FurnitureDefinition.GetTier(placed.tier)?.Prefab == null)
                {
                    validationMessage = "Mobilya tanımı bulunamadı: " + placed.sourceProductId;
                    return false;
                }
            }

            return true;
        }

        private bool ValidateRivals(RivalCompaniesSaveData saveData, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (saveData == null)
            {
                validationMessage = "Rakip firma kayıt verisi bulunamadı.";
                return false;
            }

            for (var i = 0; i < saveData.rivals.Count; i++)
            {
                var rival = saveData.rivals[i];
                if (!resolver.TryResolve<RivalCompanyDefinition>(rival.definitionId, out _))
                {
                    validationMessage = "Rakip firma tanımı bulunamadı: " + rival.definitionId;
                    return false;
                }

                for (var jobIndex = 0; jobIndex < rival.activeJobs.Count; jobIndex++)
                {
                    var jobId = rival.activeJobs[jobIndex].definitionId;
                    if (!resolver.TryResolve<RivalCompanyJobDefinition>(jobId, out _))
                    {
                        validationMessage = "Rakip iş tanımı bulunamadı: " + jobId;
                        return false;
                    }
                }
            }

            return true;
        }

        private bool ValidateAgents(AgentManagerSaveData saveData, EconomySaveData economySaveData, RivalCompaniesSaveData rivalSaveData, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (saveData == null)
            {
                validationMessage = "Ajan kayıt verisi bulunamadı.";
                return false;
            }

            for (var i = 0; i < saveData.availableAgentIds.Count; i++)
            {
                if (!resolver.TryResolve<AgentDefinition>(saveData.availableAgentIds[i], out _))
                {
                    validationMessage = "Ajan tanımı bulunamadı: " + saveData.availableAgentIds[i];
                    return false;
                }
            }

            for (var i = 0; i < saveData.deployedAgents.Count; i++)
            {
                var agent = saveData.deployedAgents[i];
                if (!ValidateAgentBase(agent.definitionId, agent.targetRivalId, agent.targetSectorId, out validationMessage))
                {
                    return false;
                }
            }

            for (var i = 0; i < saveData.playerTargetedAgents.Count; i++)
            {
                var agent = saveData.playerTargetedAgents[i];
                if (!ValidateAgentBase(agent.definitionId, agent.sourceRivalId, agent.targetSectorId, out validationMessage))
                {
                    return false;
                }

                for (var projectIndex = 0; projectIndex < agent.affectedProjectIndexes.Count; projectIndex++)
                {
                    var affectedIndex = agent.affectedProjectIndexes[projectIndex];
                    if (economySaveData == null || affectedIndex < 0 || affectedIndex >= economySaveData.activeProjects.Count)
                    {
                        validationMessage = "Ajan etkilediği proje indeksini bulamadı.";
                        return false;
                    }
                }
            }

            return true;
        }

        private bool ValidateAgentBase(string agentDefinitionId, string rivalId, string sectorId, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (!resolver.TryResolve<AgentDefinition>(agentDefinitionId, out _))
            {
                validationMessage = "Ajan tanımı bulunamadı: " + agentDefinitionId;
                return false;
            }

            if (!resolver.TryResolve<RivalCompanyDefinition>(rivalId, out _))
            {
                validationMessage = "Ajan hedef rakibi bulunamadı: " + rivalId;
                return false;
            }

            if (!resolver.TryResolve<SectorDefinition>(sectorId, out _))
            {
                validationMessage = "Ajan hedef sektörü bulunamadı: " + sectorId;
                return false;
            }

            return true;
        }

        private void ClearTransientUiState()
        {
            rootCanvas ??= FindObjectOfType<Canvas>();
            RuntimePanelUiUtility.SetComputerPanelActive(rootCanvas, false);
            furniturePlacementManager?.SetBuildMode(false);
        }

        private void ResolveSceneReferences()
        {
            rootCanvas ??= FindObjectOfType<Canvas>();
            economyManager ??= FindObjectOfType<EconomyManager>();
            timeManager ??= FindObjectOfType<TimeManager>();
            employeeManager ??= FindObjectOfType<EmployeeManager>();
            inventoryManager ??= FindObjectOfType<InventoryManager>();
            furniturePlacementManager ??= FindObjectOfType<FurniturePlacementManager>();
            officeManager ??= FindObjectOfType<OfficeManager>();
            bankManager ??= FindObjectOfType<CompanyBankManager>();
            rivalCompanyManager ??= FindObjectOfType<RivalCompanyManager>();
            agentManager ??= FindObjectOfType<AgentManager>();
            sectorManager ??= FindObjectOfType<SectorManager>();
            playerMovementController ??= FindObjectOfType<PlayerMovementController>();
            interviewSessionManager ??= FindObjectOfType<InterviewSessionManager>();
        }

        private bool IsInterviewActive()
        {
            interviewSessionManager ??= FindObjectOfType<InterviewSessionManager>();
            return interviewSessionManager != null && interviewSessionManager.HasActiveSession;
        }

        private GameSaveDefinitionResolver EnsureResolver()
        {
            return resolver ??= new GameSaveDefinitionResolver();
        }

        private GameSaveSlotInfo ReadSlotInfo(string filePath)
        {
            try
            {
                var saveData = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(filePath));
                if (saveData == null || saveData.metadata == null)
                {
                    throw new InvalidOperationException("Metadata bulunamadı.");
                }

                return new GameSaveSlotInfo
                {
                    SlotId = string.IsNullOrWhiteSpace(saveData.metadata.slotId)
                        ? Path.GetFileNameWithoutExtension(filePath)
                        : saveData.metadata.slotId,
                    DisplayName = string.IsNullOrWhiteSpace(saveData.metadata.displayName)
                        ? Path.GetFileNameWithoutExtension(filePath)
                        : saveData.metadata.displayName,
                    SavedAtUtc = saveData.metadata.savedAtUtc,
                    CurrentDay = saveData.metadata.currentDay,
                    CurrentTotalMinutes = saveData.metadata.currentTotalMinutes,
                    Balance = saveData.metadata.balance,
                    FilePath = filePath
                };
            }
            catch (Exception exception)
            {
                return new GameSaveSlotInfo
                {
                    SlotId = Path.GetFileNameWithoutExtension(filePath),
                    DisplayName = Path.GetFileNameWithoutExtension(filePath),
                    FilePath = filePath,
                    IsCorrupt = true,
                    ErrorMessage = exception.Message
                };
            }
        }

        private GameSaveSlotInfo FindSlot(string slotId)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                if (string.Equals(slots[i].SlotId, slotId, StringComparison.Ordinal))
                {
                    return slots[i];
                }
            }

            return null;
        }

        private string BuildSlotPath(string slotId)
        {
            var safeSlotId = string.IsNullOrWhiteSpace(slotId) ? "save" : slotId;
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                safeSlotId = safeSlotId.Replace(invalidChar, '_');
            }

            return Path.Combine(SaveDirectory, safeSlotId + ".json");
        }
    }
}
