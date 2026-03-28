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
        [SerializeField, Min(1)] private int sellCheckIntervalDays = 5;
        [SerializeField, Min(1)] private int maxSellsPerCheck = 1;
        [SerializeField, Range(0f, 2f)] private float sellDesireMultiplier = 1f;
        [SerializeField] private RivalCompanyJobDefinition[] availableJobs = Array.Empty<RivalCompanyJobDefinition>();

        public long StartingBalance => startingBalance;
        public int JobCheckIntervalDays => Mathf.Max(1, jobCheckIntervalDays);
        public float JobStartChance => Mathf.Clamp01(jobStartChance);
        public int MaxJobsPerCheck => Mathf.Max(1, maxJobsPerCheck);
        public int SellCheckIntervalDays => Mathf.Max(1, sellCheckIntervalDays);
        public int MaxSellsPerCheck => Mathf.Max(1, maxSellsPerCheck);
        public float SellDesireMultiplier => Mathf.Clamp(sellDesireMultiplier, 0f, 2f);
        public IReadOnlyList<RivalCompanyJobDefinition> AvailableJobs => availableJobs;
    }
}
