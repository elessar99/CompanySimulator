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
            if (!string.IsNullOrWhiteSpace(sectorData.Sector.Description))
            {
                createSizedInfoCard(sectorData.Sector.Description, 72f);
            }

            if (selectedProjectTemplate == null)
            {
                createInfoCard("Bu sektörde kullanılabilecek hazır iş şablonu bulunmuyor.");
                return;
            }

            createSizedInfoCard($"Bu sektörde iş gelirleri {sectorData.Sector.ProfitPayoutIntervalDays} günde bir döngüsel olarak gelir.", 64f);

            if (sectorData.AvailableProjects.Count > 0)
            {
                createSectionTitle("İş Şablonları");
                for (var i = 0; i < sectorData.AvailableProjects.Count; i++)
                {
                    createProjectTemplateSelector(sectorData.AvailableProjects[i]);
                }
            }
            else
            {
                createSizedInfoCard("Bu sektörde kayıtlı hazır iş şablonu yok. Yeni iş ekranı sektör verilerinden geçici bir taslak kullanıyor.", 72f);
            }

            createSectionTitle("Çalışan Atamaları");
            createEmployeeRequirementCards(selectedProjectTemplate);

            createSectionTitle("Yatırımlar");
            createInvestmentEditors(selectedProjectTemplate);
        }
    }
}
