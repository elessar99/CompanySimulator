using System;
using CompanySimulator.Features.Sectors.Runtime.Components;
using CompanySimulator.Features.Sectors.Runtime.Models;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    internal static class SectorListPage
    {
        public static void Render(SectorManager sectorManager, Action<string> createInfoCard, Action<SectorRuntimeData> createSectorButton)
        {
            if (sectorManager == null || !sectorManager.IsInitialized)
            {
                createInfoCard("Sektör sistemi henüz hazır değil.");
                return;
            }

            var sectors = sectorManager.Sectors;
            if (sectors.Count == 0)
            {
                createInfoCard("Henüz listelenecek sektör bulunmuyor.");
                return;
            }

            for (var i = 0; i < sectors.Count; i++)
            {
                createSectorButton(sectors[i]);
            }
        }
    }
}
