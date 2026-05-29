using System;
using System.Collections.Generic;

namespace CompanySimulator.Features.Save.Runtime.Models
{
    [Serializable]
    public sealed class GameSaveData
    {
        public int version = 1;
        public GameSaveMetadata metadata = new GameSaveMetadata();
        public EconomySaveData economy = new EconomySaveData();
        public TimeSaveData time = new TimeSaveData();
        public EmployeeManagerSaveData employees = new EmployeeManagerSaveData();
        public InventorySaveData inventory = new InventorySaveData();
        public FurniturePlacementSaveData furniture = new FurniturePlacementSaveData();
        public OfficeSaveData office = new OfficeSaveData();
        public BankSaveData bank = new BankSaveData();
        public RivalCompaniesSaveData rivals = new RivalCompaniesSaveData();
        public AgentManagerSaveData agents = new AgentManagerSaveData();
        public PlayerSaveData player = new PlayerSaveData();
    }

    [Serializable]
    public sealed class GameSaveMetadata
    {
        public string slotId = string.Empty;
        public string displayName = string.Empty;
        public string savedAtUtc = string.Empty;
        public int currentDay = 1;
        public int currentTotalMinutes;
        public long balance;
    }

    public sealed class GameSaveSlotInfo
    {
        public string SlotId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string SavedAtUtc { get; set; } = string.Empty;
        public int CurrentDay { get; set; }
        public int CurrentTotalMinutes { get; set; }
        public long Balance { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public bool IsCorrupt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public string TimeLabel
        {
            get
            {
                var totalMinutes = Math.Max(0, CurrentTotalMinutes);
                return $"{(totalMinutes / 60) % 24:00}:{totalMinutes % 60:00}";
            }
        }
    }

    [Serializable]
    public sealed class SaveVector3
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public sealed class SaveQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w = 1f;
    }

    [Serializable]
    public sealed class TimeSaveData
    {
        public int currentTotalMinutes;
    }

    [Serializable]
    public sealed class PlayerSaveData
    {
        public SaveVector3 position = new SaveVector3();
        public SaveQuaternion rotation = new SaveQuaternion();
        public float cameraPitch;
    }

    [Serializable]
    public sealed class EconomySaveData
    {
        public int currentDay = 1;
        public long balance;
        public int executedProjectCount;
        public string lastExecutionSummary = string.Empty;
        public List<LedgerEntrySaveData> ledgerEntries = new List<LedgerEntrySaveData>();
        public List<ActiveProjectSaveData> activeProjects = new List<ActiveProjectSaveData>();
        public List<ProjectHistorySaveData> executionHistory = new List<ProjectHistorySaveData>();
    }

    [Serializable]
    public sealed class LedgerEntrySaveData
    {
        public int day;
        public int type;
        public long amount;
        public string description = string.Empty;
    }

    [Serializable]
    public sealed class ActiveProjectSaveData
    {
        public string sourceDefinitionId = string.Empty;
        public string projectTypeId = string.Empty;
        public string displayName = string.Empty;
        public int startedDay;
        public int nextPayoutDay;
        public int payoutCount;
        public ProjectResultSaveData result = new ProjectResultSaveData();
        public List<string> assignedEmployeeIds = new List<string>();
        public List<string> assignedEmployeeSlotIds = new List<string>();
        public List<string> assignedEmployeeNames = new List<string>();
        public List<InvestmentAllocationSaveData> investmentAllocations = new List<InvestmentAllocationSaveData>();
        public float marketDemandMultiplier = 1f;
        public float competitorPressure;
        public bool isAgentAffected;
        public float agentRevenueReductionMultiplier = 1f;
    }

    [Serializable]
    public sealed class ProjectHistorySaveData
    {
        public string sourceDefinitionId = string.Empty;
        public string projectTypeId = string.Empty;
        public string displayName = string.Empty;
        public ProjectResultSaveData result = new ProjectResultSaveData();
    }

    [Serializable]
    public sealed class ProjectResultSaveData
    {
        public int durationDays;
        public long revenue;
        public long payrollCost;
        public long upfrontInvestmentCost;
        public long recurringInvestmentCost;
        public long fixedCost;
        public float successScore;
        public float employeeContribution;
        public float investmentContribution;
        public float competitionMultiplier;
    }

    [Serializable]
    public sealed class InvestmentAllocationSaveData
    {
        public string investmentTypeId = string.Empty;
        public int allocatedBudget;
    }

    [Serializable]
    public sealed class EmployeeManagerSaveData
    {
        public int generatedApplicantSequence;
        public List<RoleSpawnScheduleSaveData> roleSpawnSchedule = new List<RoleSpawnScheduleSaveData>();
        public List<EmployeeSaveData> employees = new List<EmployeeSaveData>();
        public List<EmployeeSaveData> applicants = new List<EmployeeSaveData>();
    }

    [Serializable]
    public sealed class RoleSpawnScheduleSaveData
    {
        public string roleId = string.Empty;
        public int nextSpawnDay;
    }

    [Serializable]
    public sealed class EmployeeSaveData
    {
        public string id = string.Empty;
        public string displayName = string.Empty;
        public string roleId = string.Empty;
        public float quality;
        public long expectedDailySalary;
        public long agreedDailySalary;
        public int qualityTier;
        public int applicantRemainingDays;
        public int employmentDays;
        public int qualityProgressDays;
        public bool hasPendingQualityUpgrade;
        public bool isQualityUpgradeNegotiationActive;
        public int qualityUpgradeSourceTier;
        public int pendingQualityUpgradeTier;
        public int qualityUpgradeRequestRemainingDays;
        public string currentAssignmentName = string.Empty;
    }

    [Serializable]
    public sealed class InventorySaveData
    {
        public List<InventoryItemSaveData> ownedItems = new List<InventoryItemSaveData>();
        public List<NonInventoryPurchaseSaveData> nonInventoryPurchases = new List<NonInventoryPurchaseSaveData>();
    }

    [Serializable]
    public sealed class InventoryItemSaveData
    {
        public string productId = string.Empty;
        public int quantity;
        public int firstAcquiredDay;
        public int lastAcquiredDay;
    }

    [Serializable]
    public sealed class NonInventoryPurchaseSaveData
    {
        public string productId = string.Empty;
        public int quantity;
        public int purchaseDay;
        public long totalPrice;
    }

    [Serializable]
    public sealed class FurniturePlacementSaveData
    {
        public int nextRuntimeId = 1;
        public List<PlacedFurnitureSaveData> placedFurniture = new List<PlacedFurnitureSaveData>();
    }

    [Serializable]
    public sealed class PlacedFurnitureSaveData
    {
        public int runtimeId;
        public string sourceProductId = string.Empty;
        public string furnitureDefinitionId = string.Empty;
        public int tier = 1;
        public SaveVector3 position = new SaveVector3();
        public SaveQuaternion rotation = new SaveQuaternion();
    }

    [Serializable]
    public sealed class OfficeSaveData
    {
        public List<string> unlockedRoomIds = new List<string>();
    }

    [Serializable]
    public sealed class BankSaveData
    {
        public string lastBankSummary = string.Empty;
        public List<ActiveLoanSaveData> activeLoans = new List<ActiveLoanSaveData>();
    }

    [Serializable]
    public sealed class ActiveLoanSaveData
    {
        public string offerId = string.Empty;
        public string displayName = string.Empty;
        public bool isSpecialOffer;
        public long principalAmount;
        public float interestRate;
        public int installmentIntervalDays;
        public int totalTermDays;
        public int startedDay;
        public int nextDueDay;
        public int remainingInstallmentCount;
        public long remainingPrincipalAmount;
        public long remainingDebt;
    }

    [Serializable]
    public sealed class RivalCompaniesSaveData
    {
        public List<RivalCompanySaveData> rivals = new List<RivalCompanySaveData>();
    }

    [Serializable]
    public sealed class RivalCompanySaveData
    {
        public string definitionId = string.Empty;
        public long balance;
        public int daysSinceLastJobCheck;
        public int daysSinceLastSellCheck;
        public int daysSinceLastAgentCheck;
        public List<RivalCompanyJobSaveData> activeJobs = new List<RivalCompanyJobSaveData>();
        public List<RivalJobLogSaveData> jobStartLog = new List<RivalJobLogSaveData>();
        public List<RivalJobLogSaveData> jobSellLog = new List<RivalJobLogSaveData>();
    }

    [Serializable]
    public sealed class RivalCompanyJobSaveData
    {
        public string definitionId = string.Empty;
        public int startDay;
        public int daysSinceLastPayout;
        public long lastEarnedIncome;
        public bool isAgentAffected;
        public float agentRevenueReductionMultiplier = 1f;
    }

    [Serializable]
    public sealed class RivalJobLogSaveData
    {
        public string jobName = string.Empty;
        public string sectorId = string.Empty;
        public int day;
        public long amount;
    }

    [Serializable]
    public sealed class AgentManagerSaveData
    {
        public int daysSinceLastRefresh;
        public int playerTargetedAgentSequence;
        public List<string> availableAgentIds = new List<string>();
        public List<DeployedAgentSaveData> deployedAgents = new List<DeployedAgentSaveData>();
        public List<PlayerTargetedAgentSaveData> playerTargetedAgents = new List<PlayerTargetedAgentSaveData>();
    }

    [Serializable]
    public sealed class DeployedAgentSaveData
    {
        public string definitionId = string.Empty;
        public string targetRivalId = string.Empty;
        public string targetSectorId = string.Empty;
        public long cost;
        public int deployDay;
        public int remainingDays;
        public bool isActive;
        public bool hasFailed;
        public List<int> affectedJobIndexes = new List<int>();
    }

    [Serializable]
    public sealed class PlayerTargetedAgentSaveData
    {
        public string runtimeId = string.Empty;
        public string definitionId = string.Empty;
        public string sourceRivalId = string.Empty;
        public string targetSectorId = string.Empty;
        public long cost;
        public int deployDay;
        public int remainingDays;
        public bool isActive;
        public bool hasFailed;
        public bool isDetected;
        public bool isExpired;
        public List<int> affectedProjectIndexes = new List<int>();
    }
}
