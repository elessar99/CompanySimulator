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

        [MenuItem("Company Simulator/Generate/Sample Economy Content")]
        public static void Generate()
        {
            EnsureFolder(RootFolder);

            var budgetCurve = CreateOrLoadAsset<BudgetResponseCurveDefinition>($"{RootFolder}/BudgetCurve_Default.asset");
            SetIdentity(budgetCurve, "budget_curve_default", "Default Budget Curve");
            SetInt(budgetCurve, "referenceBudget", 100000);
            SetCurve(
                budgetCurve,
                "multiplierCurve",
                new AnimationCurve(
                    new Keyframe(0f, 0.1f),
                    new Keyframe(1f, 1f),
                    new Keyframe(2f, 2f)));

            var balance = CreateOrLoadAsset<EconomyBalanceDefinition>($"{RootFolder}/EconomyBalance_Default.asset");
            SetIdentity(balance, "economy_balance_default", "Default Economy Balance");
            SetFloat(balance, "qualityNormalizationPoint", 100f);
            SetFloat(balance, "employeeProfitImpact", 0.35f);
            SetFloat(balance, "employeeSuccessImpact", 0.5f);
            SetFloat(balance, "investmentProfitImpact", 0.35f);
            SetFloat(balance, "investmentSuccessImpact", 0.5f);
            SetFloat(balance, "competitionImpact", 1f);
            SetFloat(balance, "minimumCompetitionMultiplier", 0.25f);
            SetFloat(balance, "minimumSuccessScore", 0.1f);

            var programmer = CreateOrLoadAsset<EmployeeRoleDefinition>($"{RootFolder}/Role_Programmer.asset");
            SetIdentity(programmer, "role_programmer", "Programmer");
            SetInt(programmer, "baseDailySalary", 180);
            SetFloat(programmer, "qualityWeight", 1.15f);
            SetFloat(programmer, "profitWeight", 1.1f);
            SetBool(programmer, "requiresOffice", true);

            var designer = CreateOrLoadAsset<EmployeeRoleDefinition>($"{RootFolder}/Role_GraphicDesigner.asset");
            SetIdentity(designer, "role_graphic_designer", "Graphic Designer");
            SetInt(designer, "baseDailySalary", 160);
            SetFloat(designer, "qualityWeight", 1.2f);
            SetFloat(designer, "profitWeight", 0.95f);
            SetBool(designer, "requiresOffice", true);

            var sector = CreateOrLoadAsset<SectorDefinition>($"{RootFolder}/Sector_GameDevelopment.asset");
            SetIdentity(sector, "sector_game_development", "Game Development");
            SetFloat(sector, "revenueMultiplier", 1.25f);
            SetFloat(sector, "durationMultiplier", 1f);
            SetFloat(sector, "competitionSensitivity", 1f);
            SetFloat(sector, "successToRevenueWeight", 0.65f);

            var marketing = CreateOrLoadAsset<InvestmentTypeDefinition>($"{RootFolder}/Investment_Marketing.asset");
            SetIdentity(marketing, "investment_marketing", "Marketing");
            SetObjectReference(marketing, "budgetResponseCurve", budgetCurve);
            SetFloat(marketing, "profitWeight", 1.2f);
            SetFloat(marketing, "successWeight", 0.85f);
            SetInt(marketing, "recommendedBudget", 100000);

            var production = CreateOrLoadAsset<InvestmentTypeDefinition>($"{RootFolder}/Investment_ProductionQuality.asset");
            SetIdentity(production, "investment_production_quality", "Production Quality");
            SetObjectReference(production, "budgetResponseCurve", budgetCurve);
            SetFloat(production, "profitWeight", 0.9f);
            SetFloat(production, "successWeight", 1.3f);
            SetInt(production, "recommendedBudget", 120000);

            var project = CreateOrLoadAsset<ProjectTypeDefinition>($"{RootFolder}/Project_IndieGame.asset");
            SetIdentity(project, "project_indie_game", "Indie Game Project");
            SetObjectReference(project, "sector", sector);
            SetInt(project, "baseRevenue", 300000);
            SetInt(project, "fixedCost", 25000);
            SetInt(project, "baseDurationDays", 30);
            SetFloat(project, "baseSuccessScore", 1f);
            SetFloat(project, "demandMultiplier", 1f);
            SetObjectReferenceArray(project, "preferredRoles", programmer, designer);
            SetObjectReferenceArray(project, "recommendedInvestments", marketing, production);

            var execution = CreateOrLoadAsset<ProjectExecutionDefinition>($"{RootFolder}/Execution_FirstGame.asset");
            SetIdentity(execution, "execution_first_game", "First Game Execution");
            SetObjectReference(execution, "projectType", project);
            SetFloat(execution, "marketDemandMultiplier", 1f);
            SetFloat(execution, "competitorPressure", 0.15f);
            SetEmployeeAssignments(execution);
            SetInvestmentAllocations(execution, marketing, production);

            // Sektör ekranı için kullanılacak temel katalog burada oluşturulur.
            var sectorCatalog = CreateOrLoadAsset<SectorCatalogDefinition>($"{RootFolder}/SectorCatalog_Default.asset");
            SetIdentity(sectorCatalog, "sector_catalog_default", "Default Sector Catalog");
            SetObjectReferenceArray(sectorCatalog, "sectors", sector);
            SetObjectReferenceArray(sectorCatalog, "projects", execution);

            var setup = CreateOrLoadAsset<EconomySetupDefinition>($"{RootFolder}/EconomySetup_Default.asset");
            SetIdentity(setup, "economy_setup_default", "Default Economy Setup");
            SetObjectReference(setup, "balanceDefinition", balance);
            SetInt(setup, "startingCapital", 500000);
            SetObjectReferenceArray(setup, "startupProjects", execution);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Company Simulator",
                $"Örnek ekonomi ve sektör içerikleri şu klasöre oluşturuldu:\n{RootFolder}\n\nBoş bir objeye EconomyManager, SectorManager ve SectorPanelUI ekleyip ilgili assetleri bağlayabilirsin.",
                "Tamam");
        }

        private static void SetEmployeeAssignments(ProjectExecutionDefinition execution)
        {
            var serializedObject = new SerializedObject(execution);
            var assignments = serializedObject.FindProperty("employeeAssignments");
            assignments.arraySize = 2;

            var programmerAssignment = assignments.GetArrayElementAtIndex(0);
            programmerAssignment.FindPropertyRelative("role").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EmployeeRoleDefinition>($"{RootFolder}/Role_Programmer.asset");
            programmerAssignment.FindPropertyRelative("count").intValue = 2;
            programmerAssignment.FindPropertyRelative("averageQuality").floatValue = 60f;

            var designerAssignment = assignments.GetArrayElementAtIndex(1);
            designerAssignment.FindPropertyRelative("role").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EmployeeRoleDefinition>($"{RootFolder}/Role_GraphicDesigner.asset");
            designerAssignment.FindPropertyRelative("count").intValue = 1;
            designerAssignment.FindPropertyRelative("averageQuality").floatValue = 55f;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(execution);
        }

        private static void SetInvestmentAllocations(ProjectExecutionDefinition execution, InvestmentTypeDefinition marketing, InvestmentTypeDefinition production)
        {
            var serializedObject = new SerializedObject(execution);
            var allocations = serializedObject.FindProperty("investmentAllocations");
            allocations.arraySize = 2;

            var marketingAllocation = allocations.GetArrayElementAtIndex(0);
            marketingAllocation.FindPropertyRelative("investmentType").objectReferenceValue = marketing;
            marketingAllocation.FindPropertyRelative("allocatedBudget").intValue = 75000;

            var productionAllocation = allocations.GetArrayElementAtIndex(1);
            productionAllocation.FindPropertyRelative("investmentType").objectReferenceValue = production;
            productionAllocation.FindPropertyRelative("allocatedBudget").intValue = 100000;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(execution);
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

        private static void SetCurve(Object asset, string propertyName, AnimationCurve value)
        {
            var serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty(propertyName).animationCurveValue = value;
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
    }
}
