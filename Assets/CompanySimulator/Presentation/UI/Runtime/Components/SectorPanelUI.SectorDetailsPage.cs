using System;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Features.Sectors.Runtime.Models;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    internal static class SectorDetailsPage
    {
        public static void Render(
            SectorRuntimeData sectorData,
            EconomyManager economyManager,
            Action<string> createInfoCard,
            Action<string, float> createSizedInfoCard,
            Action<string> createSectionTitle,
            Action<SectorRuntimeData> createActiveProjectCards)
        {
            if (!string.IsNullOrWhiteSpace(sectorData.Sector.Description))
            {
                createSizedInfoCard(sectorData.Sector.Description, 72f);
            }

            createInfoCard($"Aktif iş sayısı: {sectorData.ActiveProjectCount}");
            createInfoCard($"Bu sektörde gelir döngüsü: {sectorData.Sector.ProfitPayoutIntervalDays} günde bir");
            createInfoCard($"Risk Seviyesi: {GetRiskLabel(sectorData)}");
            createInfoCard($"Sektörde çalışabilecek meslek sayısı: {sectorData.Sector.SupportedRoles.Count}");
            createSectionTitle("Aktif İşler");
            createActiveProjectCards(sectorData);
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

        public static void RenderActiveProjects(
            SectorRuntimeData sectorData,
            EconomyManager economyManager,
            Action<ActiveProjectRuntimeEntry> createActiveProjectCard,
            Action<string, float> createSizedInfoCard)
        {
            var activeProjects = sectorData.ActiveProjects;
            if (activeProjects.Count == 0)
            {
                createSizedInfoCard("Bu sektörde şu anda aktif iş bulunmuyor.", 62f);
                return;
            }

            for (var i = 0; i < activeProjects.Count; i++)
            {
                createActiveProjectCard(activeProjects[i]);
            }
        }
    }
}
