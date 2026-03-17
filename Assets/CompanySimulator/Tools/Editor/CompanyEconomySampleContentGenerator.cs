using System.Collections.Generic;
using System.Linq;
using CompanySimulator.Features.Employees.Runtime.Definitions;
using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Investments.Runtime.Definitions;
using CompanySimulator.Features.Projects.Runtime.Definitions;
using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Definitions;
using UnityEditor;
using UnityEngine;

namespace CompanySimulator.Tools.Editor
{
    public static class CompanyEconomySampleContentGenerator
    {
        private const string RootFolder = "Assets/CompanySimulator/Content/Definitions/Generated/Economy";
        private const string RolesFolder = RootFolder + "/Roles";
        private const string InvestmentsFolder = RootFolder + "/Investments";
        private const string SectorsFolder = RootFolder + "/Sectors";
        private const string ProjectsFolder = RootFolder + "/Projects";
        private const string ExecutionsFolder = RootFolder + "/Executions";
        private const string EmployeesFolder = RootFolder + "/Employees";
        private const string ApplicantsFolder = RootFolder + "/Applicants";

        [MenuItem("Company Simulator/Generate/Sample Economy Content")]
        public static void Generate()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(RolesFolder);
            EnsureFolder(InvestmentsFolder);
            EnsureFolder(SectorsFolder);
            EnsureFolder(ProjectsFolder);
            EnsureFolder(ExecutionsFolder);
            EnsureFolder(EmployeesFolder);
            EnsureFolder(ApplicantsFolder);

            var budgetCurve = CreateBudgetCurve();
            var balance = CreateBalanceDefinition();
            var roleAssets = CreateRoles();
            var investmentAssets = CreateInvestments(budgetCurve);
            var sectorAssets = CreateSectors(roleAssets, investmentAssets);
            UpdateRoleAllowedSectors(roleAssets, sectorAssets);
            var executionAssets = CreateProjectsAndExecutions(roleAssets, investmentAssets, sectorAssets);
            CreateSectorCatalog(sectorAssets, executionAssets);
            CreateEconomySetup(balance);
            CreateEmployeeRosterSetup(roleAssets);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Company Simulator",
                $"Tüm temel sektör, rol, çalışan ve yatırım içerikleri şu klasöre oluşturuldu:\n{RootFolder}\n\nBoş bir objeye EconomyManager, SectorManager, EmployeeManager, SectorPanelUI ve EmployeePanelUI ekleyip assetleri bağlayabilirsin.",
                "Tamam");
        }

        private static BudgetResponseCurveDefinition CreateBudgetCurve()
        {
            var budgetCurve = CreateOrLoadAsset<BudgetResponseCurveDefinition>($"{RootFolder}/BudgetCurve_Default.asset");
            SetIdentity(budgetCurve, "budget_curve_default", "Varsayılan Bütçe Eğrisi");
            SetInt(budgetCurve, "referenceBudget", 100000);
            SetCurve(
                budgetCurve,
                "multiplierCurve",
                new AnimationCurve(
                    new Keyframe(0f, 0.1f),
                    new Keyframe(0.5f, 0.55f),
                    new Keyframe(1f, 1f),
                    new Keyframe(2f, 1.9f)));
            return budgetCurve;
        }

        private static EconomyBalanceDefinition CreateBalanceDefinition()
        {
            var balance = CreateOrLoadAsset<EconomyBalanceDefinition>($"{RootFolder}/EconomyBalance_Default.asset");
            SetIdentity(balance, "economy_balance_default", "Varsayılan Ekonomi Dengesi");
            SetFloat(balance, "qualityNormalizationPoint", 100f);
            SetFloat(balance, "employeeProfitImpact", 0.35f);
            SetFloat(balance, "employeeSuccessImpact", 0.5f);
            SetFloat(balance, "investmentProfitImpact", 0.35f);
            SetFloat(balance, "investmentSuccessImpact", 0.5f);
            SetFloat(balance, "competitionImpact", 1f);
            SetFloat(balance, "minimumCompetitionMultiplier", 0.25f);
            SetFloat(balance, "minimumSuccessScore", 0.1f);
            return balance;
        }

        private static Dictionary<string, EmployeeRoleDefinition> CreateRoles()
        {
            var roles = new[]
            {
                new RoleSeed("yazilimci", "Yazılımcı", 220, 300, 800, 1.15f, 1.1f, true, 1),
                new RoleSeed("grafiker", "Grafiker", 180, 260, 650, 1.1f, 0.95f, true, 1),
                new RoleSeed("ses_sanatcisi", "Ses Sanatçısı", 190, 280, 700, 1.2f, 1f, false, 1),
                new RoleSeed("yonetmen", "Yönetmen", 260, 400, 1000, 1.3f, 1.1f, false, 1),
                new RoleSeed("yazar", "Yazar", 170, 220, 600, 1.1f, 1.05f, true, 1),
                new RoleSeed("oyuncu", "Oyuncu", 210, 300, 900, 1.15f, 1f, false, 1),
                new RoleSeed("asci", "Aşçı", 160, 240, 620, 1.05f, 1.1f, false, 1),
                new RoleSeed("personel", "Personel", 110, 180, 450, 0.9f, 0.9f, false, 1),
                new RoleSeed("editor", "Editör", 175, 240, 620, 1.1f, 1f, true, 1),
                new RoleSeed("sunucu", "Sunucu", 190, 260, 700, 1.05f, 1f, false, 1),
                new RoleSeed("ziraat_muhendisi", "Ziraat Mühendisi", 200, 300, 780, 1.15f, 1.05f, false, 1),
                new RoleSeed("veteriner", "Veteriner", 220, 320, 820, 1.2f, 1.05f, false, 1),
                new RoleSeed("analist", "Analist", 230, 320, 850, 1.15f, 1.15f, true, 1)
            };

            var assets = new Dictionary<string, EmployeeRoleDefinition>(roles.Length);
            for (var i = 0; i < roles.Length; i++)
            {
                var role = roles[i];
                var asset = CreateOrLoadAsset<EmployeeRoleDefinition>($"{RolesFolder}/Role_{role.Id}.asset");
                SetIdentity(asset, role.Id, role.DisplayName);
                SetInt(asset, "baseDailySalary", role.BaseDailySalary);
                SetTierSettings(asset, "kotuSettings", role.KotuMinimumExpectedSalary, role.KotuMaximumExpectedSalary, role.KotuIncomeMultiplier, role.KotuSpawnChanceWeight);
                SetTierSettings(asset, "ortalamaSettings", role.OrtalamaMinimumExpectedSalary, role.OrtalamaMaximumExpectedSalary, role.OrtalamaIncomeMultiplier, role.OrtalamaSpawnChanceWeight);
                SetTierSettings(asset, "iyiSettings", role.IyiMinimumExpectedSalary, role.IyiMaximumExpectedSalary, role.IyiIncomeMultiplier, role.IyiSpawnChanceWeight);
                SetTierSettings(asset, "profesyonelSettings", role.ProfesyonelMinimumExpectedSalary, role.ProfesyonelMaximumExpectedSalary, role.ProfesyonelIncomeMultiplier, role.ProfesyonelSpawnChanceWeight);
                SetFloat(asset, "qualityWeight", role.QualityWeight);
                SetFloat(asset, "profitWeight", role.ProfitWeight);
                SetBool(asset, "requiresOffice", role.RequiresOffice);
                SetInt(asset, "maxConcurrentAssignmentsPerEmployee", role.MaxConcurrentAssignmentsPerEmployee);
                assets.Add(role.Id, asset);
            }

            return assets;
        }

        private static Dictionary<string, InvestmentTypeDefinition> CreateInvestments(BudgetResponseCurveDefinition budgetCurve)
        {
            var investments = new[]
            {
                new InvestmentSeed("animasyon", "Animasyon", InvestmentExpenseMode.Pesin, 50000, 90000, 1.05f, 1.2f),
                new InvestmentSeed("pazarlama", "Pazarlama", InvestmentExpenseMode.Pesin, 40000, 100000, 1.2f, 0.9f),
                new InvestmentSeed("kast_ajansi", "Kast Ajansı", InvestmentExpenseMode.Pesin, 35000, 80000, 1f, 1.05f),
                new InvestmentSeed("ekipman", "Ekipman", InvestmentExpenseMode.Pesin, 50000, 120000, 0.95f, 1.2f),
                new InvestmentSeed("kira", "Kira", InvestmentExpenseMode.GelirdenDus, 30000, 70000, 0.9f, 0.95f),
                new InvestmentSeed("satinalma", "Satın Alma", InvestmentExpenseMode.Pesin, 150000, 350000, 1f, 1.05f),
                new InvestmentSeed("mutfak", "Mutfak", InvestmentExpenseMode.Pesin, 60000, 120000, 1f, 1.1f),
                new InvestmentSeed("studyo", "Stüdyo", InvestmentExpenseMode.Pesin, 70000, 150000, 1.05f, 1.2f),
                new InvestmentSeed("matbaa", "Matbaa", InvestmentExpenseMode.Pesin, 70000, 140000, 1f, 1.1f),
                new InvestmentSeed("techizat", "Teçhizat", InvestmentExpenseMode.Pesin, 60000, 130000, 1f, 1.1f),
                new InvestmentSeed("arazi", "Arazi", InvestmentExpenseMode.Pesin, 100000, 220000, 1f, 1.15f),
                new InvestmentSeed("yem", "Yem", InvestmentExpenseMode.GelirdenDus, 25000, 60000, 0.95f, 1.05f),
                new InvestmentSeed("arac_filosu", "Araç Filosu", InvestmentExpenseMode.Pesin, 120000, 250000, 1.1f, 1.05f),
                new InvestmentSeed("sermaye", "Sermaye", InvestmentExpenseMode.Pesin, 200000, 400000, 1.2f, 1f)
            };

            var assets = new Dictionary<string, InvestmentTypeDefinition>(investments.Length);
            for (var i = 0; i < investments.Length; i++)
            {
                var investment = investments[i];
                var asset = CreateOrLoadAsset<InvestmentTypeDefinition>($"{InvestmentsFolder}/Investment_{investment.Id}.asset");
                SetIdentity(asset, investment.Id, investment.DisplayName);
                SetObjectReference(asset, "budgetResponseCurve", budgetCurve);
                SetFloat(asset, "profitWeight", investment.ProfitWeight);
                SetFloat(asset, "successWeight", investment.SuccessWeight);
                SetEnum(asset, "expenseMode", (int)investment.ExpenseMode);
                SetInt(asset, "minimumBudget", investment.MinimumBudget);
                SetInt(asset, "recommendedBudget", investment.RecommendedBudget);
                assets.Add(investment.Id, asset);
            }

            return assets;
        }

        private static Dictionary<string, SectorDefinition> CreateSectors(
            Dictionary<string, EmployeeRoleDefinition> roleAssets,
            Dictionary<string, InvestmentTypeDefinition> investmentAssets)
        {
            var sectors = GetSectorSeeds();
            var assets = new Dictionary<string, SectorDefinition>(sectors.Length);

            for (var i = 0; i < sectors.Length; i++)
            {
                var sector = sectors[i];
                var asset = CreateOrLoadAsset<SectorDefinition>($"{SectorsFolder}/Sector_{sector.Id}.asset");
                SetIdentity(asset, sector.Id, sector.DisplayName);
                SetString(asset, "description", sector.Description);
                SetFloat(asset, "revenueMultiplier", sector.RevenueMultiplier);
                SetFloat(asset, "durationMultiplier", sector.DurationMultiplier);
                SetFloat(asset, "competitionSensitivity", sector.CompetitionSensitivity);
                SetFloat(asset, "successToRevenueWeight", sector.SuccessToRevenueWeight);
                SetInt(asset, "profitPayoutIntervalDays", sector.ProfitPayoutIntervalDays);
                SetObjectReferenceArray(asset, "supportedRoles", ResolveRoles(roleAssets, sector.RoleIds));
                SetObjectReferenceArray(asset, "availableInvestments", ResolveInvestments(investmentAssets, sector.InvestmentIds));
                assets.Add(sector.Id, asset);
            }

            return assets;
        }

        private static void UpdateRoleAllowedSectors(
            Dictionary<string, EmployeeRoleDefinition> roleAssets,
            Dictionary<string, SectorDefinition> sectorAssets)
        {
            var sectors = GetSectorSeeds();
            var roleSectorMap = new Dictionary<string, List<Object>>(roleAssets.Count);

            foreach (var roleId in roleAssets.Keys)
            {
                roleSectorMap.Add(roleId, new List<Object>(8));
            }

            for (var i = 0; i < sectors.Length; i++)
            {
                var sector = sectors[i];
                var sectorAsset = sectorAssets[sector.Id];
                for (var j = 0; j < sector.RoleIds.Length; j++)
                {
                    roleSectorMap[sector.RoleIds[j]].Add(sectorAsset);
                }
            }

            foreach (var pair in roleAssets)
            {
                SetObjectReferenceArray(pair.Value, "allowedSectors", roleSectorMap[pair.Key].ToArray());
            }
        }

        private static Dictionary<string, ProjectExecutionDefinition> CreateProjectsAndExecutions(
            Dictionary<string, EmployeeRoleDefinition> roleAssets,
            Dictionary<string, InvestmentTypeDefinition> investmentAssets,
            Dictionary<string, SectorDefinition> sectorAssets)
        {
            var sectors = GetSectorSeeds();
            var executions = new Dictionary<string, ProjectExecutionDefinition>(sectors.Length);

            for (var i = 0; i < sectors.Length; i++)
            {
                var sector = sectors[i];
                var project = CreateOrLoadAsset<ProjectTypeDefinition>($"{ProjectsFolder}/Project_{sector.Id}.asset");
                SetIdentity(project, $"project_{sector.Id}", sector.ProjectDisplayName);
                SetObjectReference(project, "sector", sectorAssets[sector.Id]);
                SetInt(project, "baseRevenue", sector.BaseRevenue);
                SetInt(project, "fixedCost", sector.FixedCost);
                SetInt(project, "baseDurationDays", sector.BaseDurationDays);
                SetFloat(project, "baseSuccessScore", 1f);
                SetFloat(project, "demandMultiplier", 1f);
                SetObjectReferenceArray(project, "preferredRoles", ResolveRoles(roleAssets, sector.RoleIds));
                SetObjectReferenceArray(project, "recommendedInvestments", ResolveInvestments(investmentAssets, sector.InvestmentIds));

                var execution = CreateOrLoadAsset<ProjectExecutionDefinition>($"{ExecutionsFolder}/Execution_{sector.Id}.asset");
                SetIdentity(execution, $"execution_{sector.Id}", sector.ProjectDisplayName);
                SetObjectReference(execution, "projectType", project);
                SetFloat(execution, "marketDemandMultiplier", 1f);
                SetFloat(execution, "competitorPressure", sector.DefaultCompetitorPressure);
                SetEmployeeAssignments(execution, sector, roleAssets);
                SetInvestmentAllocations(execution, sector, investmentAssets);
                executions.Add(sector.Id, execution);
            }

            return executions;
        }

        private static void CreateSectorCatalog(
            Dictionary<string, SectorDefinition> sectorAssets,
            Dictionary<string, ProjectExecutionDefinition> executionAssets)
        {
            var sectors = GetSectorSeeds();
            var sectorArray = new Object[sectors.Length];
            var executionArray = new Object[sectors.Length];

            for (var i = 0; i < sectors.Length; i++)
            {
                sectorArray[i] = sectorAssets[sectors[i].Id];
                executionArray[i] = executionAssets[sectors[i].Id];
            }

            var sectorCatalog = CreateOrLoadAsset<SectorCatalogDefinition>($"{RootFolder}/SectorCatalog_Default.asset");
            SetIdentity(sectorCatalog, "sector_catalog_default", "Varsayılan Sektör Kataloğu");
            SetObjectReferenceArray(sectorCatalog, "sectors", sectorArray);
            SetObjectReferenceArray(sectorCatalog, "projects", executionArray);
        }

        private static void CreateEconomySetup(EconomyBalanceDefinition balance)
        {
            var setup = CreateOrLoadAsset<EconomySetupDefinition>($"{RootFolder}/EconomySetup_Default.asset");
            SetIdentity(setup, "economy_setup_default", "Varsayılan Ekonomi Kurulumu");
            SetObjectReference(setup, "balanceDefinition", balance);
            SetInt(setup, "startingCapital", 3000000);
            SetObjectReferenceArray(setup, "startupProjects");
        }

        private static void CreateEmployeeRosterSetup(Dictionary<string, EmployeeRoleDefinition> roleAssets)
        {
            var employeeProfiles = new List<Object>(32);
            var applicantProfiles = new List<Object>(32);

            foreach (var pair in roleAssets)
            {
                var role = pair.Value;
                var startingCount = GetStartingEmployeeProfileCount(pair.Key);
                var applicantCount = GetApplicantProfileCount(pair.Key);

                for (var i = 0; i < startingCount; i++)
                {
                    var asset = CreateOrLoadAsset<EmployeeProfileDefinition>($"{EmployeesFolder}/{pair.Key}_employee_{i + 1}.asset");
                    SetIdentity(asset, $"{pair.Key}_employee_{i + 1}", $"{role.DisplayName} Çalışan {i + 1}");
                    SetObjectReference(asset, "role", role);
                    SetFloat(asset, "quality", GetEmployeeProfileQuality(pair.Key, i, false));
                    SetInt(asset, "expectedDailySalary", GetEmployeeProfileSalary(role, false));
                    employeeProfiles.Add(asset);
                }

                for (var i = 0; i < applicantCount; i++)
                {
                    var asset = CreateOrLoadAsset<EmployeeProfileDefinition>($"{ApplicantsFolder}/{pair.Key}_applicant_{i + 1}.asset");
                    SetIdentity(asset, $"{pair.Key}_applicant_{i + 1}", $"{role.DisplayName} Aday {i + 1}");
                    SetObjectReference(asset, "role", role);
                    SetFloat(asset, "quality", GetEmployeeProfileQuality(pair.Key, i, true));
                    SetInt(asset, "expectedDailySalary", GetEmployeeProfileSalary(role, true));
                    applicantProfiles.Add(asset);
                }
            }

            var rosterSetup = CreateOrLoadAsset<EmployeeRosterSetupDefinition>($"{RootFolder}/EmployeeRosterSetup_Default.asset");
            SetIdentity(rosterSetup, "employee_roster_setup_default", "Varsayılan Çalışan Kurulumu");
            SetObjectReferenceArray(rosterSetup, "availableRoles", roleAssets.Values.ToArray());
            SetObjectReferenceArray(rosterSetup, "startingEmployees", employeeProfiles.ToArray());
            SetObjectReferenceArray(rosterSetup, "jobApplicants", applicantProfiles.ToArray());
            SetBool(rosterSetup, "autoGenerateApplicants", true);
            SetInt(rosterSetup, "minGeneratedApplicantsPerRole", 2);
            SetInt(rosterSetup, "maxGeneratedApplicantsPerRole", 4);
        }

        private static void SetEmployeeAssignments(
            ProjectExecutionDefinition execution,
            SectorSeed sector,
            Dictionary<string, EmployeeRoleDefinition> roleAssets)
        {
            var serializedObject = new SerializedObject(execution);
            var assignments = serializedObject.FindProperty("employeeAssignments");
            assignments.arraySize = sector.RoleIds.Length;

            for (var i = 0; i < sector.RoleIds.Length; i++)
            {
                var assignment = assignments.GetArrayElementAtIndex(i);
                var roleId = sector.RoleIds[i];
                assignment.FindPropertyRelative("role").objectReferenceValue = roleAssets[roleId];
                assignment.FindPropertyRelative("count").intValue = GetDefaultEmployeeCount(roleId);
                assignment.FindPropertyRelative("averageQuality").floatValue = GetDefaultEmployeeQuality(roleId);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(execution);
        }

        private static void SetInvestmentAllocations(
            ProjectExecutionDefinition execution,
            SectorSeed sector,
            Dictionary<string, InvestmentTypeDefinition> investmentAssets)
        {
            var serializedObject = new SerializedObject(execution);
            var allocations = serializedObject.FindProperty("investmentAllocations");
            allocations.arraySize = sector.InvestmentIds.Length;

            for (var i = 0; i < sector.InvestmentIds.Length; i++)
            {
                var allocation = allocations.GetArrayElementAtIndex(i);
                var investment = investmentAssets[sector.InvestmentIds[i]];
                allocation.FindPropertyRelative("investmentType").objectReferenceValue = investment;
                allocation.FindPropertyRelative("allocatedBudget").intValue = investment.RecommendedBudget;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(execution);
        }

        private static Object[] ResolveRoles(Dictionary<string, EmployeeRoleDefinition> roleAssets, string[] roleIds)
        {
            var result = new Object[roleIds.Length];
            for (var i = 0; i < roleIds.Length; i++)
            {
                result[i] = roleAssets[roleIds[i]];
            }

            return result;
        }

        private static Object[] ResolveInvestments(Dictionary<string, InvestmentTypeDefinition> investmentAssets, string[] investmentIds)
        {
            var result = new Object[investmentIds.Length];
            for (var i = 0; i < investmentIds.Length; i++)
            {
                result[i] = investmentAssets[investmentIds[i]];
            }

            return result;
        }

        private static int GetDefaultEmployeeCount(string roleId)
        {
            switch (roleId)
            {
                case "personel":
                    return 4;
                case "yazilimci":
                    return 2;
                case "grafiker":
                    return 2;
                case "yazar":
                    return 2;
                default:
                    return 1;
            }
        }

        private static float GetDefaultEmployeeQuality(string roleId)
        {
            switch (roleId)
            {
                case "yonetmen":
                case "analist":
                case "veteriner":
                    return 65f;
                case "personel":
                    return 50f;
                default:
                    return 55f;
            }
        }

        private static int GetStartingEmployeeProfileCount(string roleId)
        {
            switch (roleId)
            {
                case "personel":
                    return 3;
                case "yazilimci":
                case "grafiker":
                case "yazar":
                    return 2;
                default:
                    return 1;
            }
        }

        private static int GetApplicantProfileCount(string roleId)
        {
            switch (roleId)
            {
                case "personel":
                    return 3;
                default:
                    return 2;
            }
        }

        private static float GetEmployeeProfileQuality(string roleId, int index, bool isApplicant)
        {
            var baseQuality = GetDefaultEmployeeQuality(roleId);
            var qualityOffset = isApplicant ? -5f : 5f;
            return Mathf.Clamp(baseQuality + qualityOffset + (index * 3f), 35f, 95f);
        }

        private static int GetEmployeeProfileSalary(EmployeeRoleDefinition role, bool isApplicant)
        {
            var baseSalary = role != null ? (int)role.BaseDailySalary.Amount : 100;
            return isApplicant ? baseSalary + 20 : baseSalary;
        }

        private static SectorSeed[] GetSectorSeeds()
        {
            return new[]
            {
                new SectorSeed("oyun_gelistirme", "Oyun Geliştirme", "Oyun projeleri üretir.", new[] { "yazilimci", "grafiker", "ses_sanatcisi" }, new[] { "animasyon", "pazarlama" }, 1.3f, 1f, 1f, 0.7f, 420000, 30000, 35, 7, 0.15f),
                new SectorSeed("uygulama_gelistirme", "Uygulama Geliştirme", "Uygulama ve yazılım işleri üretir.", new[] { "yazilimci", "grafiker" }, new[] { "pazarlama" }, 1.15f, 0.9f, 0.9f, 0.55f, 280000, 20000, 24, 5, 0.1f),
                new SectorSeed("film_dizi", "Film / Dizi Yapımı", "Film ve dizi prodüksiyonları üretir.", new[] { "yonetmen", "yazar", "oyuncu", "grafiker" }, new[] { "kast_ajansi", "pazarlama", "animasyon" }, 1.45f, 1.2f, 1.1f, 0.8f, 520000, 50000, 40, 10, 0.18f),
                new SectorSeed("motion_capture", "Motion Capture", "Hareket yakalama üretimleri yapar.", new[] { "oyuncu", "grafiker" }, new[] { "ekipman" }, 1.1f, 0.95f, 0.8f, 0.55f, 240000, 18000, 18, 4, 0.08f),
                new SectorSeed("reklam", "Reklam", "Reklam filmi ve kampanya üretir.", new[] { "yonetmen", "oyuncu" }, new[] { "kast_ajansi" }, 1.2f, 0.8f, 1f, 0.65f, 260000, 20000, 14, 3, 0.12f),
                new SectorSeed("restoran", "Restoran", "Yemek servisi ve restoran işletir.", new[] { "asci", "personel" }, new[] { "kira", "satinalma" }, 1.1f, 1f, 0.9f, 0.5f, 230000, 16000, 20, 2, 0.1f),
                new SectorSeed("catering", "Catering", "Toplu yemek hizmeti verir.", new[] { "asci", "personel" }, new[] { "mutfak" }, 1.05f, 0.9f, 0.85f, 0.45f, 210000, 15000, 16, 3, 0.08f),
                new SectorSeed("muzik", "Müzik", "Müzik üretimi ve yayın işleri yapar.", new[] { "ses_sanatcisi" }, new[] { "studyo", "pazarlama" }, 1.15f, 0.85f, 0.95f, 0.6f, 250000, 17000, 18, 6, 0.12f),
                new SectorSeed("yayincilik", "Yayıncılık", "Dergi, kitap ve manga yayınlar.", new[] { "yazar", "grafiker", "editor" }, new[] { "pazarlama", "matbaa" }, 1.1f, 1f, 0.9f, 0.55f, 260000, 19000, 22, 5, 0.1f),
                new SectorSeed("gazete", "Gazete", "Günlük veya periyodik gazete basar.", new[] { "yazar", "editor" }, new[] { "matbaa" }, 1.05f, 0.85f, 1f, 0.45f, 220000, 18000, 15, 1, 0.14f),
                new SectorSeed("matbaa_sektoru", "Matbaa", "Baskı ve üretim işleri yapar.", new[] { "personel" }, new[] { "techizat" }, 1f, 0.95f, 0.7f, 0.4f, 200000, 16000, 18, 4, 0.06f),
                new SectorSeed("televizyon", "Televizyon", "Yayın ve program işleri yapar.", new[] { "sunucu", "yonetmen", "editor" }, new[] { "ekipman" }, 1.2f, 0.9f, 1.05f, 0.6f, 300000, 24000, 20, 3, 0.14f),
                new SectorSeed("tarim", "Tarım", "Tarım üretimi ve arazi yönetimi yapar.", new[] { "ziraat_muhendisi", "personel" }, new[] { "arazi" }, 1.1f, 1.1f, 0.8f, 0.55f, 250000, 20000, 28, 7, 0.07f),
                new SectorSeed("hayvancilik", "Hayvancılık", "Hayvan yetiştiriciliği ve bakım işleri yapar.", new[] { "veteriner", "personel", "ziraat_muhendisi" }, new[] { "yem", "arazi" }, 1.15f, 1.15f, 0.8f, 0.6f, 280000, 24000, 30, 5, 0.08f),
                new SectorSeed("veteriner_klinigi", "Veteriner Kliniği", "Hayvan sağlık hizmeti verir.", new[] { "veteriner" }, new[] { "kira", "satinalma" }, 1.08f, 0.95f, 0.75f, 0.5f, 230000, 17000, 18, 3, 0.08f),
                new SectorSeed("market", "Market", "Perakende market işletir.", new[] { "personel" }, new[] { "kira", "satinalma" }, 1.02f, 0.9f, 0.95f, 0.4f, 210000, 15000, 15, 2, 0.11f),
                new SectorSeed("kargo_lojistik", "Kargo Lojistik", "Dağıtım ve lojistik işi yapar.", new[] { "personel" }, new[] { "arac_filosu", "kira", "satinalma" }, 1.18f, 1f, 0.95f, 0.5f, 300000, 22000, 22, 3, 0.12f),
                new SectorSeed("banka", "Banka", "Finansal hizmet ve bankacılık yapar.", new[] { "analist", "personel", "yazilimci" }, new[] { "kira", "satinalma", "sermaye" }, 1.3f, 1f, 1.2f, 0.65f, 420000, 40000, 24, 4, 0.16f),
                new SectorSeed("sigorta", "Sigorta Şirketi", "Sigorta ürünleri ve risk yönetimi yapar.", new[] { "personel", "analist" }, new[] { "kira", "satinalma" }, 1.18f, 0.95f, 1f, 0.55f, 310000, 26000, 20, 4, 0.13f),
                new SectorSeed("otel", "Otel", "Konaklama hizmeti verir.", new[] { "personel" }, new[] { "kira", "satinalma" }, 1.12f, 1.05f, 0.9f, 0.5f, 320000, 28000, 24, 2, 0.12f)
            };
        }

        private static T CreateOrLoadAsset<T>(string assetPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static void SetIdentity(DefinitionBase asset, string id, string displayName)
        {
            var serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty("id").stringValue = id;
            serializedObject.FindProperty("displayName").stringValue = displayName;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void SetInt(Object asset, string propertyName, int value)
        {
            var serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty(propertyName).intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void SetFloat(Object asset, string propertyName, float value)
        {
            var serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty(propertyName).floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void SetBool(Object asset, string propertyName, bool value)
        {
            var serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty(propertyName).boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void SetString(Object asset, string propertyName, string value)
        {
            var serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty(propertyName).stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void SetCurve(Object asset, string propertyName, AnimationCurve value)
        {
            var serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty(propertyName).animationCurveValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void SetEnum(Object asset, string propertyName, int enumValue)
        {
            var serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty(propertyName).enumValueIndex = enumValue;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void SetTierSettings(Object asset, string propertyName, int minimumExpectedSalary, int maximumExpectedSalary, float incomeMultiplier, float spawnChanceWeight)
        {
            var serializedObject = new SerializedObject(asset);
            var tierProperty = serializedObject.FindProperty(propertyName);
            tierProperty.FindPropertyRelative("minimumExpectedSalary").intValue = minimumExpectedSalary;
            tierProperty.FindPropertyRelative("maximumExpectedSalary").intValue = maximumExpectedSalary;
            tierProperty.FindPropertyRelative("incomeMultiplier").floatValue = incomeMultiplier;
            tierProperty.FindPropertyRelative("spawnChanceWeight").floatValue = spawnChanceWeight;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void SetObjectReference(Object asset, string propertyName, Object reference)
        {
            var serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty(propertyName).objectReferenceValue = reference;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void SetObjectReferenceArray(Object asset, string propertyName, params Object[] references)
        {
            var serializedObject = new SerializedObject(asset);
            var arrayProperty = serializedObject.FindProperty(propertyName);
            arrayProperty.arraySize = references.Length;

            for (var i = 0; i < references.Length; i++)
            {
                arrayProperty.GetArrayElementAtIndex(i).objectReferenceValue = references[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void EnsureFolder(string folderPath)
        {
            var parts = folderPath.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private readonly struct RoleSeed
        {
            public RoleSeed(string id, string displayName, int baseDailySalary, int minimumExpectedSalary, int maximumExpectedSalary, float qualityWeight, float profitWeight, bool requiresOffice, int maxConcurrentAssignmentsPerEmployee)
            {
                Id = id;
                DisplayName = displayName;
                BaseDailySalary = baseDailySalary;
                var salaryRange = Mathf.Max(4, maximumExpectedSalary - minimumExpectedSalary);
                var firstCut = minimumExpectedSalary + Mathf.RoundToInt(salaryRange * 0.25f);
                var secondCut = minimumExpectedSalary + Mathf.RoundToInt(salaryRange * 0.5f);
                var thirdCut = minimumExpectedSalary + Mathf.RoundToInt(salaryRange * 0.75f);

                KotuMinimumExpectedSalary = minimumExpectedSalary;
                KotuMaximumExpectedSalary = Mathf.Max(KotuMinimumExpectedSalary, firstCut);
                OrtalamaMinimumExpectedSalary = Mathf.Max(KotuMaximumExpectedSalary + 1, firstCut + 1);
                OrtalamaMaximumExpectedSalary = Mathf.Max(OrtalamaMinimumExpectedSalary, secondCut);
                IyiMinimumExpectedSalary = Mathf.Max(OrtalamaMaximumExpectedSalary + 1, secondCut + 1);
                IyiMaximumExpectedSalary = Mathf.Max(IyiMinimumExpectedSalary, thirdCut);
                ProfesyonelMinimumExpectedSalary = Mathf.Max(IyiMaximumExpectedSalary + 1, thirdCut + 1);
                ProfesyonelMaximumExpectedSalary = Mathf.Max(ProfesyonelMinimumExpectedSalary, maximumExpectedSalary);

                KotuIncomeMultiplier = 0.5f;
                OrtalamaIncomeMultiplier = 1f;
                IyiIncomeMultiplier = 1.5f;
                ProfesyonelIncomeMultiplier = 3f;
                KotuSpawnChanceWeight = 35f;
                OrtalamaSpawnChanceWeight = 35f;
                IyiSpawnChanceWeight = 20f;
                ProfesyonelSpawnChanceWeight = 10f;
                QualityWeight = qualityWeight;
                ProfitWeight = profitWeight;
                RequiresOffice = requiresOffice;
                MaxConcurrentAssignmentsPerEmployee = maxConcurrentAssignmentsPerEmployee;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public int BaseDailySalary { get; }
            public int KotuMinimumExpectedSalary { get; }
            public int KotuMaximumExpectedSalary { get; }
            public int OrtalamaMinimumExpectedSalary { get; }
            public int OrtalamaMaximumExpectedSalary { get; }
            public int IyiMinimumExpectedSalary { get; }
            public int IyiMaximumExpectedSalary { get; }
            public int ProfesyonelMinimumExpectedSalary { get; }
            public int ProfesyonelMaximumExpectedSalary { get; }
            public float KotuIncomeMultiplier { get; }
            public float OrtalamaIncomeMultiplier { get; }
            public float IyiIncomeMultiplier { get; }
            public float ProfesyonelIncomeMultiplier { get; }
            public float KotuSpawnChanceWeight { get; }
            public float OrtalamaSpawnChanceWeight { get; }
            public float IyiSpawnChanceWeight { get; }
            public float ProfesyonelSpawnChanceWeight { get; }
            public float QualityWeight { get; }
            public float ProfitWeight { get; }
            public bool RequiresOffice { get; }
            public int MaxConcurrentAssignmentsPerEmployee { get; }
        }

        private readonly struct InvestmentSeed
        {
            public InvestmentSeed(string id, string displayName, InvestmentExpenseMode expenseMode, int minimumBudget, int recommendedBudget, float profitWeight, float successWeight)
            {
                Id = id;
                DisplayName = displayName;
                ExpenseMode = expenseMode;
                MinimumBudget = minimumBudget;
                RecommendedBudget = recommendedBudget;
                ProfitWeight = profitWeight;
                SuccessWeight = successWeight;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public InvestmentExpenseMode ExpenseMode { get; }
            public int MinimumBudget { get; }
            public int RecommendedBudget { get; }
            public float ProfitWeight { get; }
            public float SuccessWeight { get; }
        }

        private readonly struct SectorSeed
        {
            public SectorSeed(
                string id,
                string displayName,
                string description,
                string[] roleIds,
                string[] investmentIds,
                float revenueMultiplier,
                float durationMultiplier,
                float competitionSensitivity,
                float successToRevenueWeight,
                int baseRevenue,
                int fixedCost,
                int baseDurationDays,
                int profitPayoutIntervalDays,
                float defaultCompetitorPressure)
            {
                Id = id;
                DisplayName = displayName;
                Description = description;
                RoleIds = roleIds;
                InvestmentIds = investmentIds;
                RevenueMultiplier = revenueMultiplier;
                DurationMultiplier = durationMultiplier;
                CompetitionSensitivity = competitionSensitivity;
                SuccessToRevenueWeight = successToRevenueWeight;
                BaseRevenue = baseRevenue;
                FixedCost = fixedCost;
                BaseDurationDays = baseDurationDays;
                ProfitPayoutIntervalDays = profitPayoutIntervalDays;
                DefaultCompetitorPressure = defaultCompetitorPressure;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Description { get; }
            public string[] RoleIds { get; }
            public string[] InvestmentIds { get; }
            public float RevenueMultiplier { get; }
            public float DurationMultiplier { get; }
            public float CompetitionSensitivity { get; }
            public float SuccessToRevenueWeight { get; }
            public int BaseRevenue { get; }
            public int FixedCost { get; }
            public int BaseDurationDays { get; }
            public int ProfitPayoutIntervalDays { get; }
            public float DefaultCompetitorPressure { get; }
            public string ProjectDisplayName => DisplayName + " İşi";
        }
    }
}
