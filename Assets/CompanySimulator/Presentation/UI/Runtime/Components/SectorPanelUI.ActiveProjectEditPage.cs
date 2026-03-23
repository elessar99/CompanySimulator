using System;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Finance.Runtime.Models;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    internal static class SectorActiveProjectEditPage
    {
        public static void Render(
            ActiveProjectRuntimeEntry activeProject,
            EconomyManager economyManager,
            Action<string> createInfoCard,
            Action<string, float> createSizedInfoCard,
            Action<string> createSectionTitle,
            Action<ProjectExecutionDefinition> createEmployeeRequirementCards,
            Action<ProjectExecutionDefinition> createInvestmentEditors)
        {
            createSizedInfoCard("Bu aktif işte çalışanları değiştirebilir veya yatırımları yalnızca artırabilirsin. Kaydedince gelir döngüsü sıfırlanır.", 82f);
            createSizedInfoCard($"Sonraki gelir: {(economyManager != null ? activeProject.DaysUntilNextPayout(economyManager.CurrentDay) : 0)} gün sonra | Döngü kârı: {activeProject.CycleProfit.Amount:N0}", 62f);

            createSectionTitle("Çalışan Atamaları");
            createEmployeeRequirementCards(activeProject.SourceDefinition);

            createSectionTitle("Yatırımlar");
            createInvestmentEditors(activeProject.SourceDefinition);
        }
    }
}
