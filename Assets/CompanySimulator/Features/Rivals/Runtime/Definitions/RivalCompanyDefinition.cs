using System;
using System.Collections.Generic;
using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Rivals.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "RivalCompanyDefinition", menuName = "Company Simulator/Definitions/Rivals/Company")]
    public sealed class RivalCompanyDefinition : DefinitionBase
    {
        [SerializeField, Min(0)] private long startingBalance = 50000;
        [SerializeField, Min(1)] private int jobCheckIntervalDays = 3;
        [SerializeField, Range(0f, 1f)] private float jobStartChance = 0.5f;
        [SerializeField, Min(1)] private int maxJobsPerCheck = 3;
        [SerializeField] private RivalCompanyJobDefinition[] availableJobs = Array.Empty<RivalCompanyJobDefinition>();

        public long StartingBalance => startingBalance;
        public int JobCheckIntervalDays => Mathf.Max(1, jobCheckIntervalDays);
        public float JobStartChance => Mathf.Clamp01(jobStartChance);
        public int MaxJobsPerCheck => Mathf.Max(1, maxJobsPerCheck);
        public IReadOnlyList<RivalCompanyJobDefinition> AvailableJobs => availableJobs;
    }
}
