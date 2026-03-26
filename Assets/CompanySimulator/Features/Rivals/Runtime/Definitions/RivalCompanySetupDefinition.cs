using System;
using System.Collections.Generic;
using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Rivals.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "RivalCompanySetupDefinition", menuName = "Company Simulator/Definitions/Rivals/Setup")]
    public sealed class RivalCompanySetupDefinition : DefinitionBase
    {
        [SerializeField] private RivalCompanyDefinition[] rivalCompanies = Array.Empty<RivalCompanyDefinition>();

        public IReadOnlyList<RivalCompanyDefinition> RivalCompanies => rivalCompanies;
    }
}
