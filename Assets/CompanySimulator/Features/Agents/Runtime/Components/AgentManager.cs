using System;
using System.Collections.Generic;
using System.Linq;
using CompanySimulator.Features.Agents.Runtime.Definitions;
using CompanySimulator.Features.Agents.Runtime.Models;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Features.Rivals.Runtime.Components;
using CompanySimulator.Features.Rivals.Runtime.Models;
using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Agents.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class AgentManager : MonoBehaviour
    {
        [SerializeField] private AgentSetupDefinition setup;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private RivalCompanyManager rivalCompanyManager;
        [SerializeField, Min(500)] private long baseAgentSearchCost = 2000;
        [SerializeField, Min(500)] private long searchCostPerActiveProject = 1000;

        private readonly List<DeployedAgentRuntimeData> deployedAgents = new List<DeployedAgentRuntimeData>(8);
        private readonly List<AgentDefinition> availablePool = new List<AgentDefinition>(8);
        private readonly List<DeployedAgentRuntimeData> failedAgents = new List<DeployedAgentRuntimeData>(4);
        private readonly List<PlayerTargetedAgentRuntimeData> playerTargetedAgents = new List<PlayerTargetedAgentRuntimeData>(8);
        private readonly List<PlayerTargetedAgentRuntimeData> failedPlayerTargetedAgents = new List<PlayerTargetedAgentRuntimeData>(4);
        private readonly List<PlayerTargetedAgentRuntimeData> dismissedPlayerAgents = new List<PlayerTargetedAgentRuntimeData>(4);
        private int daysSinceLastRefresh;

        public event Action DataChanged;

        public IReadOnlyList<DeployedAgentRuntimeData> DeployedAgents => deployedAgents;
        public IReadOnlyList<AgentDefinition> AvailablePool => availablePool;
        public IReadOnlyList<DeployedAgentRuntimeData> FailedAgents => failedAgents;
        public IReadOnlyList<PlayerTargetedAgentRuntimeData> PlayerTargetedAgents => playerTargetedAgents;
        public IReadOnlyList<PlayerTargetedAgentRuntimeData> FailedPlayerTargetedAgents => failedPlayerTargetedAgents;
        public IReadOnlyList<PlayerTargetedAgentRuntimeData> DismissedPlayerAgents => dismissedPlayerAgents;
        public AgentSetupDefinition Setup => setup;
        public int DaysUntilNextRefresh => setup != null ? Mathf.Max(0, setup.RefreshIntervalDays - daysSinceLastRefresh) : 0;

        private void Awake()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();
            rivalCompanyManager ??= FindObjectOfType<RivalCompanyManager>();
        }

        private void OnEnable()
        {
            if (economyManager != null)
            {
                economyManager.DayAdvanced -= OnDayAdvanced;
                economyManager.DayAdvanced += OnDayAdvanced;
            }

            if (availablePool.Count == 0 && setup != null)
            {
                RefreshAgentPool();
            }
        }

        private void OnDisable()
        {
            if (economyManager != null)
            {
                economyManager.DayAdvanced -= OnDayAdvanced;
            }
        }

        public IReadOnlyList<AgentDefinition> GetAvailableAgents()
        {
            return availablePool;
        }

        public bool CanDeployAgent(AgentDefinition agentDef)
        {
            if (agentDef == null || economyManager == null)
            {
                return false;
            }

            var cost = Money.From(agentDef.MinimumCost);
            return economyManager.Balance >= cost;
        }

        public bool DeployAgent(AgentDefinition agentDef, RivalCompanyRuntimeData targetRival, SectorDefinition targetSector)
        {
            if (agentDef == null || targetRival == null || targetSector == null || economyManager == null)
            {
                return false;
            }

            if (!availablePool.Contains(agentDef))
            {
                return false;
            }

            var cost = Money.From(UnityEngine.Random.Range((int)agentDef.MinimumCost, (int)agentDef.MaximumCost + 1));
            if (!economyManager.TryRecordExpense(cost, LedgerEntryType.AgentExpense, "Ajan Gönderimi: " + agentDef.DisplayName))
            {
                return false;
            }

            availablePool.Remove(agentDef);

            var agent = new DeployedAgentRuntimeData(agentDef, targetRival, targetSector, cost, economyManager.CurrentDay);
            agent.ApplySabotage();

            if (agent.HasFailed)
            {
                failedAgents.Add(agent);
            }
            else
            {
                deployedAgents.Add(agent);
            }

            if (rivalCompanyManager != null)
            {
                rivalCompanyManager.ForceRebuildCompetitionCache();
            }

            DataChanged?.Invoke();
            return true;
        }

        public int GetActiveAgentCountForRivalSector(RivalCompanyRuntimeData rival, SectorDefinition sector)
        {
            var count = 0;
            for (var i = 0; i < deployedAgents.Count; i++)
            {
                var agent = deployedAgents[i];
                if (agent.IsActive && agent.TargetRival == rival && agent.TargetSector == sector)
                {
                    count++;
                }
            }

            return count;
        }

        private void RefreshAgentPool()
        {
            availablePool.Clear();

            if (setup == null || setup.AvailableAgents.Count == 0)
            {
                return;
            }

            var catalog = setup.AvailableAgents;
            var agentCount = UnityEngine.Random.Range(setup.MinAgentsPerRefresh, setup.MaxAgentsPerRefresh + 1);

            var totalWeight = 0;
            for (var i = 0; i < catalog.Count; i++)
            {
                if (catalog[i] != null)
                {
                    totalWeight += catalog[i].SelectionWeight;
                }
            }

            if (totalWeight <= 0)
            {
                return;
            }

            for (var a = 0; a < agentCount; a++)
            {
                var pick = UnityEngine.Random.Range(0, totalWeight);
                var cumulative = 0;
                for (var i = 0; i < catalog.Count; i++)
                {
                    var def = catalog[i];
                    if (def == null) continue;
                    cumulative += def.SelectionWeight;
                    if (pick < cumulative)
                    {
                        availablePool.Add(def);
                        break;
                    }
                }
            }
        }

        private void OnDayAdvanced(int currentDay)
        {
            var changed = false;

            failedAgents.Clear();

            for (var i = deployedAgents.Count - 1; i >= 0; i--)
            {
                var agent = deployedAgents[i];
                if (!agent.IsActive)
                {
                    deployedAgents.RemoveAt(i);
                    changed = true;
                    continue;
                }

                var stillActive = agent.AdvanceDay();
                if (!stillActive)
                {
                    deployedAgents.RemoveAt(i);
                    changed = true;
                }
            }

            failedPlayerTargetedAgents.Clear();
            dismissedPlayerAgents.Clear();

            for (var i = playerTargetedAgents.Count - 1; i >= 0; i--)
            {
                var agent = playerTargetedAgents[i];
                if (!agent.IsActive)
                {
                    playerTargetedAgents.RemoveAt(i);
                    changed = true;
                    continue;
                }

                agent.AdvanceDay();
                changed = true;
            }

            daysSinceLastRefresh++;
            if (setup != null && daysSinceLastRefresh >= setup.RefreshIntervalDays)
            {
                daysSinceLastRefresh = 0;
                RefreshAgentPool();
                changed = true;
            }

            if (changed)
            {
                if (rivalCompanyManager != null)
                {
                    rivalCompanyManager.ForceRebuildCompetitionCache();
                }

                DataChanged?.Invoke();
            }
        }

        public Money GetAgentSearchCost()
        {
            var projectCount = economyManager != null ? economyManager.ActiveProjects.Count : 0;
            var cost = baseAgentSearchCost + searchCostPerActiveProject * projectCount;
            return Money.From(cost);
        }

        public int GetDetectedAgentCount()
        {
            var count = 0;
            for (var i = 0; i < playerTargetedAgents.Count; i++)
            {
                if (playerTargetedAgents[i].IsDetected && playerTargetedAgents[i].IsActive)
                {
                    count++;
                }
            }

            return count;
        }

        public string SearchForAgents()
        {
            if (economyManager == null)
            {
                return "Ekonomi sistemi bulunamadı.";
            }

            var cost = GetAgentSearchCost();
            if (!economyManager.TryRecordExpense(cost, LedgerEntryType.AgentExpense, "Ajan arama operasyonu"))
            {
                return $"Ajan arama maliyeti ({cost.Amount:N0}) için bakiye yetersiz.";
            }

            var detectedCount = 0;
            for (var i = 0; i < playerTargetedAgents.Count; i++)
            {
                var agent = playerTargetedAgents[i];
                if (agent.IsActive && !agent.IsDetected)
                {
                    agent.Detect();
                    detectedCount++;
                }
            }

            DataChanged?.Invoke();

            if (detectedCount > 0)
            {
                return $"{detectedCount} ajan tespit edildi! Ajanları kovabilirsiniz.";
            }

            return "Şirkette ajan bulunamadı.";
        }

        public string DismissDetectedAgents()
        {
            var dismissedCount = 0;
            for (var i = playerTargetedAgents.Count - 1; i >= 0; i--)
            {
                var agent = playerTargetedAgents[i];
                if (agent.IsDetected && agent.IsActive)
                {
                    agent.Dismiss();
                    dismissedPlayerAgents.Add(agent);
                    playerTargetedAgents.RemoveAt(i);
                    dismissedCount++;
                }
            }

            if (rivalCompanyManager != null)
            {
                rivalCompanyManager.ForceRebuildCompetitionCache();
            }

            DataChanged?.Invoke();

            if (dismissedCount > 0)
            {
                return $"{dismissedCount} ajan kovuldu. Gelir etkileri temizlendi.";
            }

            return "Kovulacak tespit edilmiş ajan bulunamadı.";
        }

        public bool DismissDetectedAgent(PlayerTargetedAgentRuntimeData targetAgent)
        {
            if (targetAgent == null)
            {
                return false;
            }

            var index = playerTargetedAgents.IndexOf(targetAgent);
            if (index < 0)
            {
                return false;
            }

            var agent = playerTargetedAgents[index];
            if (!agent.IsDetected || !agent.IsActive)
            {
                return false;
            }

            agent.Dismiss();
            dismissedPlayerAgents.Add(agent);
            playerTargetedAgents.RemoveAt(index);

            if (rivalCompanyManager != null)
            {
                rivalCompanyManager.ForceRebuildCompetitionCache();
            }

            DataChanged?.Invoke();
            return true;
        }

        public string ForceRivalSendAgent(RivalCompanyRuntimeData rival)
        {
            if (rival == null || economyManager == null)
            {
                return "Rakip veya ekonomi yöneticisi bulunamadı.";
            }

            var agentSetup = rival.Definition.RivalAgentSetup;
            if (agentSetup == null || agentSetup.AvailableAgents.Count == 0)
            {
                return rival.Definition.DisplayName + ": Ajan kataloğu tanımlı değil.";
            }

            var playerProjects = economyManager.ActiveProjects;
            if (playerProjects.Count == 0)
            {
                return rival.Definition.DisplayName + ": Oyuncunun aktif projesi yok.";
            }

            return TrySendRivalAgentToPlayer(rival, playerProjects, true);
        }

        public string TrySendRivalAgentToPlayer(
            RivalCompanyRuntimeData rival,
            IReadOnlyList<ActiveProjectRuntimeEntry> playerProjects,
            bool forceIgnoreChance)
        {
            var definition = rival.Definition;
            var agentSetup = definition.RivalAgentSetup;
            if (agentSetup == null || agentSetup.AvailableAgents.Count == 0)
            {
                return null;
            }

            if (!forceIgnoreChance)
            {
                var roll = UnityEngine.Random.value;
                if (roll > definition.AgentSendChance)
                {
                    return null;
                }
            }

            var targetSector = SelectWeightedSector(rival, playerProjects);
            if (targetSector == null)
            {
                return rival.Definition.DisplayName + ": Hedef sektör bulunamadı.";
            }

            var agentCount = UnityEngine.Random.Range(definition.MinAgentsPerSend, definition.MaxAgentsPerSend + 1);
            var catalog = agentSetup.AvailableAgents;

            var totalWeight = 0;
            for (var i = 0; i < catalog.Count; i++)
            {
                if (catalog[i] != null)
                {
                    totalWeight += catalog[i].SelectionWeight;
                }
            }

            if (totalWeight <= 0)
            {
                return rival.Definition.DisplayName + ": Ajan ağırlığı sıfır.";
            }

            var result = new System.Text.StringBuilder(256);
            result.Append(rival.Definition.DisplayName);
            result.Append(" → ");
            result.Append(targetSector.DisplayName);
            result.Append(" sektörüne ");
            result.Append(agentCount);
            result.Append(" ajan gönderdi:");

            for (var a = 0; a < agentCount; a++)
            {
                AgentDefinition selectedAgent = null;
                var agentPick = UnityEngine.Random.Range(0, totalWeight);
                var agentCumulative = 0;
                for (var ci = 0; ci < catalog.Count; ci++)
                {
                    var cDef = catalog[ci];
                    if (cDef == null) continue;
                    agentCumulative += cDef.SelectionWeight;
                    if (agentPick < agentCumulative)
                    {
                        selectedAgent = cDef;
                        break;
                    }
                }

                if (selectedAgent == null)
                {
                    continue;
                }

                var cost = Money.From(UnityEngine.Random.Range((int)selectedAgent.MinimumCost, (int)selectedAgent.MaximumCost + 1));
                var agent = new PlayerTargetedAgentRuntimeData(
                    selectedAgent,
                    rival,
                    targetSector,
                    cost,
                    economyManager.CurrentDay);

                agent.ApplySabotage(playerProjects);

                result.Append("\n  • ");
                result.Append(selectedAgent.DisplayName);
                result.Append(" (Maliyet: ");
                result.Append(cost.Amount.ToString("N0"));
                result.Append(")");

                if (agent.HasFailed)
                {
                    result.Append(" — BAŞARISIZ");
                    failedPlayerTargetedAgents.Add(agent);
                }
                else
                {
                    result.Append(" — ");
                    result.Append(agent.AffectedProjects.Count);
                    result.Append(" proje etkilendi");
                    playerTargetedAgents.Add(agent);
                }
            }

            if (rivalCompanyManager != null)
            {
                rivalCompanyManager.ForceRebuildCompetitionCache();
            }

            DataChanged?.Invoke();
            return result.ToString();
        }

        private SectorDefinition SelectWeightedSector(
            RivalCompanyRuntimeData rival,
            IReadOnlyList<ActiveProjectRuntimeEntry> playerProjects)
        {
            var definition = rival.Definition;
            var playerWeight = definition.PlayerInfluenceWeight;
            var rivalWeight = definition.RivalInfluenceWeight;

            var sectorWeights = new Dictionary<SectorDefinition, float>(8);

            for (var i = 0; i < playerProjects.Count; i++)
            {
                var sector = playerProjects[i].Sector;
                if (sector == null)
                {
                    continue;
                }

                sectorWeights.TryGetValue(sector, out var current);
                sectorWeights[sector] = current + playerWeight;
            }

            if (sectorWeights.Count == 0)
            {
                return null;
            }

            var rivalJobs = rival.ActiveJobs;
            for (var i = 0; i < rivalJobs.Count; i++)
            {
                var sector = rivalJobs[i].Sector;
                if (sector == null || !sectorWeights.ContainsKey(sector))
                {
                    continue;
                }

                sectorWeights[sector] = sectorWeights[sector] + rivalWeight;
            }

            var totalWeight = 0f;
            foreach (var kvp in sectorWeights)
            {
                totalWeight += kvp.Value;
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            var pick = UnityEngine.Random.Range(0f, totalWeight);
            var cumulative = 0f;
            foreach (var kvp in sectorWeights)
            {
                cumulative += kvp.Value;
                if (pick < cumulative)
                {
                    return kvp.Key;
                }
            }

            return null;
        }
    }
}
