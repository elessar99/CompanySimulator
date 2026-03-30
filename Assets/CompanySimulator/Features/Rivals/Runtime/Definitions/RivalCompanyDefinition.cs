using System;
using System.Collections.Generic;
using CompanySimulator.Features.Agents.Runtime.Definitions;
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

        [Header("Ajan Gönderme Ayarları")]
        [SerializeField, Min(1)] private int agentSendCheckIntervalDays = 7;
        [SerializeField, Range(0f, 1f)] private float agentSendChance = 0.3f;
        [SerializeField, Min(1)] private int minAgentsPerSend = 1;
        [SerializeField, Min(1)] private int maxAgentsPerSend = 2;
        [SerializeField, Range(0f, 10f)] private float playerInfluenceWeight = 1f;
        [SerializeField, Range(0f, 10f)] private float rivalInfluenceWeight = 1f;
        [SerializeField] private RivalAgentSetupDefinition rivalAgentSetup;

        public long StartingBalance => startingBalance;
        public int JobCheckIntervalDays => Mathf.Max(1, jobCheckIntervalDays);
        public float JobStartChance => Mathf.Clamp01(jobStartChance);
        public int MaxJobsPerCheck => Mathf.Max(1, maxJobsPerCheck);
        public int SellCheckIntervalDays => Mathf.Max(1, sellCheckIntervalDays);
        public int MaxSellsPerCheck => Mathf.Max(1, maxSellsPerCheck);
        public float SellDesireMultiplier => Mathf.Clamp(sellDesireMultiplier, 0f, 2f);
        public IReadOnlyList<RivalCompanyJobDefinition> AvailableJobs => availableJobs;
        public int AgentSendCheckIntervalDays => Mathf.Max(1, agentSendCheckIntervalDays);
        public float AgentSendChance => Mathf.Clamp01(agentSendChance);
        public int MinAgentsPerSend => Mathf.Max(1, minAgentsPerSend);
        public int MaxAgentsPerSend => Mathf.Max(MinAgentsPerSend, maxAgentsPerSend);
        public float PlayerInfluenceWeight => Mathf.Max(0f, playerInfluenceWeight);
        public float RivalInfluenceWeight => Mathf.Max(0f, rivalInfluenceWeight);
        public RivalAgentSetupDefinition RivalAgentSetup => rivalAgentSetup;
    }
}
