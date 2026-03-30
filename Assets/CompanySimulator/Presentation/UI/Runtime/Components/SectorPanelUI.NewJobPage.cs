using System;
using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Sectors.Runtime.Models;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    internal static class SectorNewJobPage
    {
        public static void Render(
            SectorRuntimeData sectorData,
            ProjectExecutionDefinition selectedProjectTemplate,
            Action<string> createInfoCard,
            Action<string, float> createSizedInfoCard,
            Action<string> createSectionTitle,
            Action<ProjectExecutionDefinition> createProjectTemplateSelector,
            Action<ProjectExecutionDefinition> createEmployeeRequirementCards,
            Action<ProjectExecutionDefinition> createInvestmentEditors)
        {
            if (selectedProjectTemplate == null)
            {
                createInfoCard("Bu sektörde kullanılabilecek hazır iş şablonu bulunmuyor.");
                return;
            }

            var summary =
                $"Aktif iş sayısı: {sectorData.ActiveProjectCount}" +
                "\n" +
                $"Gelir döngüsü: {sectorData.Sector.ProfitPayoutIntervalDays} günde bir" +
                "\n" +
                $"Risk seviyesi: {GetRiskLabel(sectorData)}";

            createSizedInfoCard(summary, 108f);

            createSectionTitle("Çalışan Atamaları");
            createEmployeeRequirementCards(selectedProjectTemplate);

            createSectionTitle("Yatırımlar");
            createInvestmentEditors(selectedProjectTemplate);
        }

        private static string GetRiskLabel(SectorRuntimeData sectorData)
        {
            switch (sectorData.Sector.RiskLevel)
            {
                case CompanySimulator.Features.Sectors.Runtime.Definitions.SectorRiskLevel.Orta:
                    return "Orta";
                case CompanySimulator.Features.Sectors.Runtime.Definitions.SectorRiskLevel.Yuksek:
                    return "Yüksek";
                default:
                    return "Düşük";
            }
        }
    }
}
