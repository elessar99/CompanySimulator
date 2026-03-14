using System;
using System.Collections.Generic;
using CompanySimulator.Shared.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Finance.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "EconomySetupDefinition", menuName = "Company Simulator/Definitions/Finance/Economy Setup")]
    public sealed class EconomySetupDefinition : DefinitionBase
    {
        [SerializeField] private EconomyBalanceDefinition balanceDefinition;
        [SerializeField, Min(0)] private int startingCapital = 500000;
        [SerializeField] private ProjectExecutionDefinition[] startupProjects = Array.Empty<ProjectExecutionDefinition>();

        public EconomyBalanceDefinition BalanceDefinition => balanceDefinition;
        public Money StartingCapital => Money.From(startingCapital);
        public IReadOnlyList<ProjectExecutionDefinition> StartupProjects => startupProjects;
    }
}
