using System;
using System.Collections.Generic;
using CompanySimulator.Features.Employees.Runtime.Definitions;
using CompanySimulator.Features.Employees.Runtime.Models;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Features.Save.Runtime.Models;
using CompanySimulator.Features.Save.Runtime.Services;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Employees.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class EmployeeManager : MonoBehaviour
    {
        [SerializeField] private EmployeeRosterSetupDefinition setup;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private int employeeCount;
        [SerializeField] private int applicantCount;

        private readonly List<EmployeeRuntimeData> employees = new List<EmployeeRuntimeData>(64);
        private readonly List<EmployeeRuntimeData> applicants = new List<EmployeeRuntimeData>(64);
        private readonly List<EmployeeRoleDefinition> roles = new List<EmployeeRoleDefinition>(32);
        private readonly Dictionary<EmployeeRoleDefinition, int> nextApplicantSpawnDayByRole = new Dictionary<EmployeeRoleDefinition, int>(32);
        private int generatedApplicantSequence;
        private bool isInitialized;
        private const int QualityUpgradeResponseDays = 3;

        private static readonly string[] FirstNames =
        {
            "Ali", "Ayşe", "Mehmet", "Zeynep", "Can", "Elif", "Mert", "Ece", "Berk", "Deniz",
            "Arda", "İrem", "Emre", "Sude", "Kerem", "Ceren", "Ozan", "Selin", "Eren", "Aslı"
        };

        private static readonly string[] LastNames =
        {
            "Yılmaz", "Kaya", "Demir", "Şahin", "Çelik", "Koç", "Aydın", "Yıldız", "Arslan", "Kaplan",
            "Doğan", "Aksoy", "Taş", "Kurt", "Öztürk", "Polat", "Erdoğan", "Özdemir", "Bulut", "Acar"
        };

        public event Action DataChanged;

        public bool IsInitialized => isInitialized;
        public IReadOnlyList<EmployeeRuntimeData> Employees => employees;
        public IReadOnlyList<EmployeeRuntimeData> Applicants => applicants;
        public IReadOnlyList<EmployeeRoleDefinition> Roles => roles;

        private void Awake()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();
            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        private void OnEnable()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();
            if (economyManager != null)
            {
                economyManager.DayAdvanced -= HandleDayAdvanced;
                economyManager.DayAdvanced += HandleDayAdvanced;
            }
        }

        private void OnDisable()
        {
            if (economyManager != null)
            {
                economyManager.DayAdvanced -= HandleDayAdvanced;
            }
        }

        [ContextMenu("Çalışanları Başlat")]
        public void Initialize()
        {
            if (setup == null)
            {
                Debug.LogError("EmployeeManager için kurulum verisi atanmadı.", this);
                isInitialized = false;
                return;
            }

            employees.Clear();
            applicants.Clear();
            roles.Clear();
            nextApplicantSpawnDayByRole.Clear();
            generatedApplicantSequence = 0;

            RegisterRoles(setup.AvailableRoles);

            CreateRuntimeList(setup.StartingEmployees, employees, "employee");
            CreateRuntimeList(setup.JobApplicants, applicants, "applicant", setup.ApplicantLifetimeDays);

            if (setup.AutoGenerateApplicants)
            {
                GenerateInitialApplicantsForAllRoles();
            }

            InitializeApplicantSpawnSchedule(GetCurrentDay());

            UpdateSnapshot();
            isInitialized = true;
            DataChanged?.Invoke();
        }

        public IReadOnlyList<EmployeeRuntimeData> GetEmployeesByRole(EmployeeRoleDefinition role)
        {
            return FilterByRole(employees, role);
        }

        public IReadOnlyList<EmployeeRuntimeData> GetIdleEmployeesByRole(EmployeeRoleDefinition role)
        {
            if (!EnsureInitialized())
            {
                return Array.Empty<EmployeeRuntimeData>();
            }

            var result = new List<EmployeeRuntimeData>(8);
            for (var i = 0; i < employees.Count; i++)
            {
                var employee = employees[i];
                if (employee.Role == role && !employee.IsAssigned)
                {
                    result.Add(employee);
                }
            }

            return result;
        }

        public IReadOnlyList<EmployeeRuntimeData> GetApplicantsByRole(EmployeeRoleDefinition role)
        {
            return FilterByRole(applicants, role);
        }

        public int GetEmployeeCount(EmployeeRoleDefinition role)
        {
            return CountByRole(employees, role);
        }

        public int GetApplicantCount(EmployeeRoleDefinition role)
        {
            return CountByRole(applicants, role);
        }

        public bool TryHireApplicant(EmployeeRuntimeData applicant)
        {
            if (!EnsureInitialized() || applicant == null)
            {
                return false;
            }

            if (!applicants.Remove(applicant))
            {
                return false;
            }

            applicant.MarkAsEmployee();
            employees.Add(applicant);
            RegisterRole(applicant.Role);
            UpdateSnapshot();
            DataChanged?.Invoke();
            return true;
        }

        public bool TryHireApplicant(EmployeeRuntimeData applicant, CompanySimulator.Shared.Runtime.Economy.Money agreedDailySalary)
        {
            if (!TryHireApplicant(applicant))
            {
                return false;
            }

            applicant.SetAgreedDailySalary(agreedDailySalary);
            DataChanged?.Invoke();
            return true;
        }

        public bool TryRejectApplicant(EmployeeRuntimeData applicant)
        {
            if (!EnsureInitialized() || applicant == null)
            {
                return false;
            }

            if (!applicants.Remove(applicant))
            {
                return false;
            }

            UpdateSnapshot();
            DataChanged?.Invoke();
            return true;
        }

        public bool TryClearAssignment(EmployeeRuntimeData employee, string expectedAssignmentName = null)
        {
            if (!EnsureInitialized() || employee == null || !employees.Contains(employee) || !employee.IsAssigned)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(expectedAssignmentName) && !string.Equals(employee.CurrentAssignmentName, expectedAssignmentName, StringComparison.Ordinal))
            {
                return false;
            }

            employee.ClearAssignment();
            DataChanged?.Invoke();
            return true;
        }

        public bool CanFireEmployee(EmployeeRuntimeData employee)
        {
            if (!EnsureInitialized() || employee == null)
            {
                return false;
            }

            return employees.Contains(employee) && !employee.IsAssigned;
        }

        public bool TryFireEmployee(EmployeeRuntimeData employee)
        {
            if (!CanFireEmployee(employee))
            {
                return false;
            }

            return TryTerminateEmployeeWithSeverance(employee, "Çalışan kovma tazminatı");
        }

        public IReadOnlyList<EmployeeRuntimeData> GetEmployeesWithQualityUpgradeRequests()
        {
            if (!EnsureInitialized())
            {
                return Array.Empty<EmployeeRuntimeData>();
            }

            var result = new List<EmployeeRuntimeData>(8);
            for (var i = 0; i < employees.Count; i++)
            {
                var employee = employees[i];
                if (employee != null && (employee.HasPendingQualityUpgrade || employee.IsQualityUpgradeNegotiationActive))
                {
                    result.Add(employee);
                }
            }

            return result;
        }

        public bool TryBeginQualityUpgradeNegotiation(EmployeeRuntimeData employee)
        {
            if (!EnsureInitialized() || employee == null || !employees.Contains(employee) || !employee.HasPendingQualityUpgrade)
            {
                return false;
            }

            employee.BeginQualityUpgradeNegotiation();
            DataChanged?.Invoke();
            return true;
        }

        public bool TryAcceptQualityUpgradeSalary(EmployeeRuntimeData employee, Money agreedDailySalary)
        {
            if (!EnsureInitialized() || employee == null || !employees.Contains(employee) || !employee.IsQualityUpgradeNegotiationActive)
            {
                return false;
            }

            employee.CompleteQualityUpgradeNegotiation(agreedDailySalary);
            DataChanged?.Invoke();
            return true;
        }

        public bool TryRejectQualityUpgradeNegotiation(EmployeeRuntimeData employee)
        {
            if (!EnsureInitialized() || employee == null || !employees.Contains(employee))
            {
                return false;
            }

            return TryTerminateEmployeeWithSeverance(employee, "Maaş düzenlemesi reddi tazminatı");
        }

        public Money CalculateSeverancePay(EmployeeRuntimeData employee)
        {
            if (employee == null || employee.EffectiveDailySalary <= Money.Zero || employee.EmploymentDays <= 0)
            {
                return Money.Zero;
            }

            var salary = employee.EffectiveDailySalary.Amount;
            var calculatedAmount = employee.EmploymentDays * salary * 0.1d;
            var cappedAmount = salary * 10d;
            return Money.From(Math.Min(calculatedAmount, cappedAmount));
        }

        public string GetEmployeeAssignedSectorName(EmployeeRuntimeData employee)
        {
            if (!EnsureInitialized() || employee == null || economyManager == null)
            {
                return "Boşta";
            }

            var activeProjects = economyManager.ActiveProjects;
            for (var i = 0; i < activeProjects.Count; i++)
            {
                var activeProject = activeProjects[i];
                if (activeProject == null)
                {
                    continue;
                }

                var assignedEmployees = activeProject.AssignedEmployees;
                for (var j = 0; j < assignedEmployees.Count; j++)
                {
                    if (assignedEmployees[j] != employee)
                    {
                        continue;
                    }

                    return activeProject.Sector != null ? activeProject.Sector.DisplayName : "Sektör Yok";
                }
            }

            return employee.IsAssigned ? "Bilinmiyor" : "Boşta";
        }

        public bool TryTerminateEmployeeWithSeverance(EmployeeRuntimeData employee, string reason)
        {
            if (!EnsureInitialized() || employee == null || !employees.Contains(employee))
            {
                return false;
            }

            var severancePay = CalculateSeverancePay(employee);
            if (economyManager != null && severancePay > Money.Zero)
            {
                var description = string.IsNullOrWhiteSpace(reason)
                    ? $"{employee.DisplayName} tazminat ödemesi"
                    : $"{employee.DisplayName}: {reason}";
                if (!economyManager.TrySpendWithAutoLoan(severancePay, LedgerEntryType.PayrollExpense, description))
                {
                    return false;
                }
            }

            if (employee.IsAssigned)
            {
                employee.ClearAssignment();
            }

            if (!employees.Remove(employee))
            {
                return false;
            }

            UpdateSnapshot();
            DataChanged?.Invoke();
            return true;
        }

        public bool ForceRemoveEmployee(EmployeeRuntimeData employee)
        {
            if (!EnsureInitialized() || employee == null)
            {
                return false;
            }

            if (!employees.Remove(employee))
            {
                return false;
            }

            if (employee.IsAssigned)
            {
                employee.ClearAssignment();
            }

            UpdateSnapshot();
            DataChanged?.Invoke();
            return true;
        }

        public bool CanReassignEmployees(IReadOnlyList<EmployeeRuntimeData> currentEmployees, IReadOnlyList<EmployeeRuntimeData> newEmployees)
        {
            if (!EnsureInitialized() || newEmployees == null)
            {
                return false;
            }

            var allowedEmployees = currentEmployees != null ? new HashSet<EmployeeRuntimeData>(currentEmployees) : new HashSet<EmployeeRuntimeData>();
            var uniqueEmployees = new HashSet<EmployeeRuntimeData>();
            for (var i = 0; i < newEmployees.Count; i++)
            {
                var employee = newEmployees[i];
                if (employee == null || !employees.Contains(employee) || !uniqueEmployees.Add(employee))
                {
                    return false;
                }

                if (employee.IsAssigned && !allowedEmployees.Contains(employee))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryReassignEmployees(IReadOnlyList<EmployeeRuntimeData> currentEmployees, IReadOnlyList<EmployeeRuntimeData> newEmployees, string assignmentName)
        {
            if (!CanReassignEmployees(currentEmployees, newEmployees) || string.IsNullOrWhiteSpace(assignmentName))
            {
                return false;
            }

            var currentLookup = currentEmployees != null ? new HashSet<EmployeeRuntimeData>(currentEmployees) : new HashSet<EmployeeRuntimeData>();
            var newLookup = new HashSet<EmployeeRuntimeData>(newEmployees);

            var assignedNow = new List<EmployeeRuntimeData>(newLookup.Count);
            foreach (var employee in newLookup)
            {
                if (employee != null && !currentLookup.Contains(employee) && !employee.TryAssign(assignmentName))
                {
                    for (var i = 0; i < assignedNow.Count; i++)
                    {
                        assignedNow[i].ClearAssignment();
                    }

                    return false;
                }

                if (employee != null && !currentLookup.Contains(employee))
                {
                    assignedNow.Add(employee);
                }
            }

            foreach (var employee in currentLookup)
            {
                if (employee != null && !newLookup.Contains(employee))
                {
                    employee.ClearAssignment();
                }
            }

            DataChanged?.Invoke();
            return true;
        }

        public bool CanAssignEmployees(IReadOnlyList<EmployeeRuntimeData> selectedEmployees)
        {
            if (!EnsureInitialized() || selectedEmployees == null)
            {
                return false;
            }

            for (var i = 0; i < selectedEmployees.Count; i++)
            {
                var employee = selectedEmployees[i];
                if (employee == null || employee.IsAssigned || !employees.Contains(employee))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryAssignEmployees(IReadOnlyList<EmployeeRuntimeData> selectedEmployees, string assignmentName)
        {
            if (!CanAssignEmployees(selectedEmployees) || string.IsNullOrWhiteSpace(assignmentName))
            {
                return false;
            }

            for (var i = 0; i < selectedEmployees.Count; i++)
            {
                if (!selectedEmployees[i].TryAssign(assignmentName))
                {
                    return false;
                }
            }

            DataChanged?.Invoke();
            return true;
        }

        public EmployeeManagerSaveData CaptureSaveData()
        {
            EnsureInitialized();

            var saveData = new EmployeeManagerSaveData
            {
                generatedApplicantSequence = generatedApplicantSequence
            };

            foreach (var pair in nextApplicantSpawnDayByRole)
            {
                if (pair.Key == null)
                {
                    continue;
                }

                saveData.roleSpawnSchedule.Add(new RoleSpawnScheduleSaveData
                {
                    roleId = pair.Key.Id,
                    nextSpawnDay = pair.Value
                });
            }

            for (var i = 0; i < employees.Count; i++)
            {
                if (employees[i] != null)
                {
                    saveData.employees.Add(CaptureEmployee(employees[i]));
                }
            }

            for (var i = 0; i < applicants.Count; i++)
            {
                if (applicants[i] != null)
                {
                    saveData.applicants.Add(CaptureEmployee(applicants[i]));
                }
            }

            return saveData;
        }

        public bool RestoreFromSaveData(
            EmployeeManagerSaveData saveData,
            GameSaveDefinitionResolver resolver,
            out Dictionary<string, EmployeeRuntimeData> employeeLookup,
            out string validationMessage)
        {
            employeeLookup = new Dictionary<string, EmployeeRuntimeData>(StringComparer.Ordinal);
            validationMessage = string.Empty;

            if (saveData == null)
            {
                validationMessage = "Çalışan kayıt verisi bulunamadı.";
                return false;
            }

            if (resolver == null)
            {
                validationMessage = "Tanım çözücü bulunamadı.";
                return false;
            }

            if (!ValidateEmployeeList(saveData.employees, resolver, out validationMessage) ||
                !ValidateEmployeeList(saveData.applicants, resolver, out validationMessage))
            {
                return false;
            }

            employees.Clear();
            applicants.Clear();
            roles.Clear();
            nextApplicantSpawnDayByRole.Clear();

            RegisterRoles(setup != null ? setup.AvailableRoles : Array.Empty<EmployeeRoleDefinition>());

            for (var i = 0; i < saveData.employees.Count; i++)
            {
                var restoredEmployee = RestoreEmployee(saveData.employees[i], resolver);
                employees.Add(restoredEmployee);
                RegisterRole(restoredEmployee.Role);
                if (!employeeLookup.ContainsKey(restoredEmployee.Id))
                {
                    employeeLookup.Add(restoredEmployee.Id, restoredEmployee);
                }
            }

            for (var i = 0; i < saveData.applicants.Count; i++)
            {
                var restoredApplicant = RestoreEmployee(saveData.applicants[i], resolver);
                applicants.Add(restoredApplicant);
                RegisterRole(restoredApplicant.Role);
            }

            generatedApplicantSequence = Mathf.Max(0, saveData.generatedApplicantSequence);

            for (var i = 0; i < saveData.roleSpawnSchedule.Count; i++)
            {
                var schedule = saveData.roleSpawnSchedule[i];
                if (resolver.TryResolve<EmployeeRoleDefinition>(schedule.roleId, out var role))
                {
                    nextApplicantSpawnDayByRole[role] = Mathf.Max(1, schedule.nextSpawnDay);
                    RegisterRole(role);
                }
            }

            isInitialized = true;
            UpdateSnapshot();
            DataChanged?.Invoke();
            return true;
        }

        private bool EnsureInitialized()
        {
            if (isInitialized)
            {
                return true;
            }

            Initialize();
            return isInitialized;
        }

        private static EmployeeSaveData CaptureEmployee(EmployeeRuntimeData employee)
        {
            return new EmployeeSaveData
            {
                id = employee.Id,
                displayName = employee.DisplayName,
                roleId = employee.Role != null ? employee.Role.Id : string.Empty,
                quality = employee.Quality,
                expectedDailySalary = employee.ExpectedDailySalary.Amount,
                agreedDailySalary = employee.AgreedDailySalary.Amount,
                qualityTier = (int)employee.QualityTier,
                applicantRemainingDays = employee.ApplicantRemainingDays,
                employmentDays = employee.EmploymentDays,
                qualityProgressDays = employee.QualityProgressDays,
                hasPendingQualityUpgrade = employee.HasPendingQualityUpgrade,
                isQualityUpgradeNegotiationActive = employee.IsQualityUpgradeNegotiationActive,
                qualityUpgradeSourceTier = (int)employee.QualityUpgradeSourceTier,
                pendingQualityUpgradeTier = (int)employee.PendingQualityUpgradeTier,
                qualityUpgradeRequestRemainingDays = employee.QualityUpgradeRequestRemainingDays,
                currentAssignmentName = employee.CurrentAssignmentName
            };
        }

        private static bool ValidateEmployeeList(IReadOnlyList<EmployeeSaveData> source, GameSaveDefinitionResolver resolver, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (source == null)
            {
                return true;
            }

            for (var i = 0; i < source.Count; i++)
            {
                var employee = source[i];
                if (employee == null || string.IsNullOrWhiteSpace(employee.roleId))
                {
                    continue;
                }

                if (!resolver.TryResolve<EmployeeRoleDefinition>(employee.roleId, out _))
                {
                    validationMessage = $"Çalışan rol tanımı bulunamadı: {employee.roleId}";
                    return false;
                }
            }

            return true;
        }

        private static EmployeeRuntimeData RestoreEmployee(EmployeeSaveData saveData, GameSaveDefinitionResolver resolver)
        {
            resolver.TryResolve<EmployeeRoleDefinition>(saveData.roleId, out var role);
            var restoredEmployee = new EmployeeRuntimeData(
                saveData.id,
                saveData.displayName,
                role,
                saveData.quality,
                Money.From(saveData.expectedDailySalary),
                saveData.applicantRemainingDays);

            restoredEmployee.RestoreRuntimeState(
                saveData.quality,
                Money.From(saveData.agreedDailySalary),
                (EmployeeQualityTier)saveData.qualityTier,
                saveData.applicantRemainingDays,
                saveData.employmentDays,
                saveData.qualityProgressDays,
                saveData.hasPendingQualityUpgrade,
                saveData.isQualityUpgradeNegotiationActive,
                (EmployeeQualityTier)saveData.qualityUpgradeSourceTier,
                (EmployeeQualityTier)saveData.pendingQualityUpgradeTier,
                saveData.qualityUpgradeRequestRemainingDays,
                saveData.currentAssignmentName);

            return restoredEmployee;
        }

        private void CreateRuntimeList(IReadOnlyList<EmployeeProfileDefinition> sourceList, List<EmployeeRuntimeData> targetList, string prefix, int applicantLifetimeDays = 0)
        {
            for (var i = 0; i < sourceList.Count; i++)
            {
                var profile = sourceList[i];
                if (profile == null)
                {
                    continue;
                }

                var runtimeData = new EmployeeRuntimeData($"{prefix}_{profile.Id}_{i}", profile, applicantLifetimeDays);
                targetList.Add(runtimeData);
                RegisterRole(runtimeData.Role);
            }
        }

        private void RegisterRoles(IReadOnlyList<EmployeeRoleDefinition> sourceRoles)
        {
            for (var i = 0; i < sourceRoles.Count; i++)
            {
                RegisterRole(sourceRoles[i]);
            }
        }

        private void RegisterRole(EmployeeRoleDefinition role)
        {
            if (role == null || roles.Contains(role))
            {
                return;
            }

            roles.Add(role);
        }

        private IReadOnlyList<EmployeeRuntimeData> FilterByRole(List<EmployeeRuntimeData> sourceList, EmployeeRoleDefinition role)
        {
            if (!EnsureInitialized())
            {
                return Array.Empty<EmployeeRuntimeData>();
            }

            var result = new List<EmployeeRuntimeData>(8);
            for (var i = 0; i < sourceList.Count; i++)
            {
                if (sourceList[i].Role == role)
                {
                    result.Add(sourceList[i]);
                }
            }

            return result;
        }

        private int CountByRole(List<EmployeeRuntimeData> sourceList, EmployeeRoleDefinition role)
        {
            if (!EnsureInitialized())
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < sourceList.Count; i++)
            {
                if (sourceList[i].Role == role)
                {
                    count++;
                }
            }

            return count;
        }

        private void HandleDayAdvanced(int currentDay)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            var hasChanges = AdvanceApplicantPool(currentDay);
            if (AdvanceEmployeeDevelopment())
            {
                hasChanges = true;
            }
            if (!hasChanges)
            {
                return;
            }

            UpdateSnapshot();
            DataChanged?.Invoke();
        }

        private void GenerateInitialApplicantsForAllRoles()
        {
            for (var i = 0; i < roles.Count; i++)
            {
                GenerateInitialApplicantsForRole(roles[i]);
            }
        }

        private void GenerateInitialApplicantsForRole(EmployeeRoleDefinition role)
        {
            if (role == null)
            {
                return;
            }

            var applicantCountToCreate = GetInitialApplicantCountForRole(role);
            for (var i = 0; i < applicantCountToCreate; i++)
            {
                applicants.Add(CreateRandomApplicant(role, setup.ApplicantLifetimeDays));
            }
        }

        private void GenerateApplicantsForRole(EmployeeRoleDefinition role)
        {
            if (role == null)
            {
                return;
            }

            var applicantCountToCreate = UnityEngine.Random.Range(setup.MinGeneratedApplicantsPerRole, setup.MaxGeneratedApplicantsPerRole + 1);
            for (var i = 0; i < applicantCountToCreate; i++)
            {
                applicants.Add(CreateRandomApplicant(role, setup.ApplicantLifetimeDays));
            }
        }

        private EmployeeRuntimeData CreateRandomApplicant(EmployeeRoleDefinition role, int applicantLifetimeDays)
        {
            generatedApplicantSequence++;

            var qualityTier = RollQualityTier(role);
            var quality = RollQualityValue(qualityTier);
            var salary = RollExpectedSalary(role, qualityTier);
            var displayName = $"{RollFirstName()} {RollLastName()}";

            return new EmployeeRuntimeData(
                $"generated_applicant_{generatedApplicantSequence}",
                displayName,
                role,
                quality,
                Money.From(salary),
                applicantLifetimeDays);
        }

        private bool AdvanceApplicantPool(int currentDay)
        {
            var hasChanges = RemoveExpiredApplicants();
            if (!setup.AutoGenerateApplicants)
            {
                return hasChanges;
            }

            for (var i = 0; i < roles.Count; i++)
            {
                var role = roles[i];
                if (role == null)
                {
                    continue;
                }

                if (!nextApplicantSpawnDayByRole.TryGetValue(role, out var nextSpawnDay))
                {
                    nextSpawnDay = currentDay + role.ApplicantRefreshIntervalDays;
                }

                var generatedForRole = false;
                while (currentDay >= nextSpawnDay)
                {
                    GenerateApplicantsForRole(role);
                    nextSpawnDay += role.ApplicantRefreshIntervalDays;
                    generatedForRole = true;
                }

                if (generatedForRole)
                {
                    hasChanges = true;
                }

                nextApplicantSpawnDayByRole[role] = nextSpawnDay;
            }

            return hasChanges;
        }

        private bool RemoveExpiredApplicants()
        {
            var removedAny = false;
            for (var i = applicants.Count - 1; i >= 0; i--)
            {
                var applicant = applicants[i];
                if (applicant == null || applicant.ApplicantRemainingDays <= 0)
                {
                    continue;
                }

                if (applicant.AdvanceApplicantDay())
                {
                    continue;
                }

                applicants.RemoveAt(i);
                removedAny = true;
            }

            return removedAny;
        }

        private bool AdvanceEmployeeDevelopment()
        {
            var hasChanges = false;
            for (var i = employees.Count - 1; i >= 0; i--)
            {
                var employee = employees[i];
                if (employee == null)
                {
                    continue;
                }

                employee.AdvanceEmploymentDay();
                hasChanges = true;

                if (employee.HasPendingQualityUpgrade)
                {
                    var stillWaitingForResponse = employee.AdvanceQualityUpgradeRequestDay();
                    if (!stillWaitingForResponse)
                    {
                        if (TryTerminateEmployeeWithSeverance(employee, "Maaş düzenlemesi talebi süresi doldu"))
                        {
                            hasChanges = true;
                        }
                    }

                    continue;
                }

                if (employee.IsQualityUpgradeNegotiationActive)
                {
                    continue;
                }

                employee.AdvanceQualityProgressDay();
                var nextTier = GetNextQualityTier(employee.QualityTier);
                if (nextTier == employee.QualityTier || employee.Role == null)
                {
                    continue;
                }

                var requiredDays = employee.Role.GetQualityUpgradeDays(employee.QualityTier);
                if (requiredDays > 0 && employee.QualityProgressDays >= requiredDays)
                {
                    employee.StartQualityUpgradeRequest(nextTier, QualityUpgradeResponseDays);
                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        private static EmployeeQualityTier GetNextQualityTier(EmployeeQualityTier qualityTier)
        {
            switch (qualityTier)
            {
                case EmployeeQualityTier.Kotu:
                    return EmployeeQualityTier.Ortalama;
                case EmployeeQualityTier.Ortalama:
                    return EmployeeQualityTier.Iyi;
                case EmployeeQualityTier.Iyi:
                    return EmployeeQualityTier.Profesyonel;
                default:
                    return qualityTier;
            }
        }

        private void InitializeApplicantSpawnSchedule(int currentDay)
        {
            nextApplicantSpawnDayByRole.Clear();
            for (var i = 0; i < roles.Count; i++)
            {
                var role = roles[i];
                if (role == null)
                {
                    continue;
                }

                nextApplicantSpawnDayByRole[role] = currentDay + role.ApplicantRefreshIntervalDays;
            }
        }

        private int GetInitialApplicantCountForRole(EmployeeRoleDefinition role)
        {
            if (role == null)
            {
                return 0;
            }

            var sectorMultiplier = Mathf.Max(1, role.AllowedSectors.Count > 0 ? role.AllowedSectors.Count : 1);
            return setup.InitialGeneratedApplicantsPerAllowedSector * sectorMultiplier;
        }

        private int GetCurrentDay()
        {
            return economyManager != null ? Mathf.Max(1, economyManager.CurrentDay) : 1;
        }

        private EmployeeQualityTier RollQualityTier(EmployeeRoleDefinition role)
        {
            if (role == null)
            {
                return EmployeeQualityTier.Ortalama;
            }

            var kotuWeight = role.GetSpawnChanceWeight(EmployeeQualityTier.Kotu);
            var ortalamaWeight = role.GetSpawnChanceWeight(EmployeeQualityTier.Ortalama);
            var iyiWeight = role.GetSpawnChanceWeight(EmployeeQualityTier.Iyi);
            var profesyonelWeight = role.GetSpawnChanceWeight(EmployeeQualityTier.Profesyonel);
            var totalWeight = kotuWeight + ortalamaWeight + iyiWeight + profesyonelWeight;

            if (totalWeight <= 0f)
            {
                return EmployeeQualityTier.Ortalama;
            }

            var rolledValue = UnityEngine.Random.Range(0f, totalWeight);
            if (rolledValue < kotuWeight)
            {
                return EmployeeQualityTier.Kotu;
            }

            rolledValue -= kotuWeight;
            if (rolledValue < ortalamaWeight)
            {
                return EmployeeQualityTier.Ortalama;
            }

            rolledValue -= ortalamaWeight;
            if (rolledValue < iyiWeight)
            {
                return EmployeeQualityTier.Iyi;
            }

            return EmployeeQualityTier.Profesyonel;
        }

        private float RollQualityValue(EmployeeQualityTier qualityTier)
        {
            switch (qualityTier)
            {
                case EmployeeQualityTier.Kotu:
                    return UnityEngine.Random.Range(10f, 25f);
                case EmployeeQualityTier.Ortalama:
                    return UnityEngine.Random.Range(25f, 50f);
                case EmployeeQualityTier.Iyi:
                    return UnityEngine.Random.Range(50f, 75f);
                default:
                    return UnityEngine.Random.Range(75f, 100f);
            }
        }

        private int RollExpectedSalary(EmployeeRoleDefinition role, EmployeeQualityTier qualityTier)
        {
            if (role == null)
            {
                return 100;
            }

            var minimum = role.GetMinimumExpectedSalary(qualityTier);
            var maximum = role.GetMaximumExpectedSalary(qualityTier);
            if (maximum <= minimum)
            {
                return minimum;
            }

            return UnityEngine.Random.Range(minimum, maximum + 1);
        }

        private string RollFirstName()
        {
            return FirstNames[UnityEngine.Random.Range(0, FirstNames.Length)];
        }

        private string RollLastName()
        {
            return LastNames[UnityEngine.Random.Range(0, LastNames.Length)];
        }

        private void UpdateSnapshot()
        {
            employeeCount = employees.Count;
            applicantCount = applicants.Count;
        }
    }
}
