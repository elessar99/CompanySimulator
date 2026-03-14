using System;
using System.Collections.Generic;
using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Sectors.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "SectorCatalogDefinition", menuName = "Company Simulator/Definitions/Sectors/Sector Catalog")]
    public sealed class SectorCatalogDefinition : DefinitionBase
    {
        [SerializeField] private SectorDefinition[] sectors = Array.Empty<SectorDefinition>();
        [SerializeField] private ProjectExecutionDefinition[] projects = Array.Empty<ProjectExecutionDefinition>();

        // Sektör panelinde hangi sektörlerin hangi sırada görüneceğini belirler.
        public IReadOnlyList<SectorDefinition> Sectors => sectors;

        // Sektörlerin içine girildiğinde listelenecek iş tanımlarını tutar.
        public IReadOnlyList<ProjectExecutionDefinition> Projects => projects;
    }
}
