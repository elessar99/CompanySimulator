using System;
using System.Collections.Generic;
using CompanySimulator.Features.Employees.Runtime.Definitions;
using CompanySimulator.Features.Employees.Runtime.Models;
using CompanySimulator.Features.Finance.Runtime.Components;
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

            foreach (var employee in currentLookup)
            {
                if (employee != null && !newLookup.Contains(employee))
                {
                    employee.ClearAssignment();
                }
            }

            foreach (var employee in newLookup)
            {
                if (employee != null && !currentLookup.Contains(employee) && !employee.TryAssign(assignmentName))
                {
                    return false;
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

        private bool EnsureInitialized()
        {
            if (isInitialized)
            {
                return true;
            }

            Initialize();
            return isInitialized;
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
