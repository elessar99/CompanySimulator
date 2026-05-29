using System;
using System.Collections.Generic;
using CompanySimulator.Features.Agents.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Features.Rivals.Runtime.Definitions;
using CompanySimulator.Features.Rivals.Runtime.Models;
using CompanySimulator.Features.Save.Runtime.Models;
using CompanySimulator.Features.Save.Runtime.Services;
using CompanySimulator.Features.Sectors.Runtime.Services;
using UnityEngine;

namespace CompanySimulator.Features.Rivals.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class RivalCompanyManager : MonoBehaviour
    {
        [SerializeField] private RivalCompanySetupDefinition setup;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private AgentManager agentManager;

        private readonly List<RivalCompanyRuntimeData> rivals = new List<RivalCompanyRuntimeData>(8);
        private bool isInitialized;

        public event Action DataChanged;

        public bool IsInitialized => isInitialized;
        public IReadOnlyList<RivalCompanyRuntimeData> Rivals => rivals;

        private void Awake()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();
            agentManager ??= FindObjectOfType<AgentManager>();
        }

        private void OnEnable()
        {
            if (economyManager != null)
            {
                economyManager.DayAdvanced -= OnDayAdvanced;
                economyManager.DayAdvanced += OnDayAdvanced;
                economyManager.ProjectExecuted -= OnProjectChanged;
                economyManager.ProjectExecuted += OnProjectChanged;
                economyManager.ProjectSold -= OnProjectSold;
                economyManager.ProjectSold += OnProjectSold;
            }
        }

        private void OnDisable()
        {
            if (economyManager != null)
            {
                economyManager.DayAdvanced -= OnDayAdvanced;
                economyManager.ProjectExecuted -= OnProjectChanged;
                economyManager.ProjectSold -= OnProjectSold;
            }
        }

        public void Initialize()
        {
            if (setup == null)
            {
                Debug.LogError("RivalCompanyManager için kurulum verisi atanmadı.", this);
                isInitialized = false;
                return;
            }

            rivals.Clear();

            for (var i = 0; i < setup.RivalCompanies.Count; i++)
            {
                var definition = setup.RivalCompanies[i];
                if (definition == null)
                {
                    continue;
                }

                rivals.Add(new RivalCompanyRuntimeData(definition));
            }

            isInitialized = true;
            DataChanged?.Invoke();
        }

        private void OnProjectChanged(ProjectExecutionDefinition executionDefinition, ProjectEconomyResult result)
        {
            RebuildCompetitionCache();
        }

        private void OnProjectSold(ActiveProjectRuntimeEntry activeProject)
        {
            RebuildCompetitionCache();
        }

        private void RebuildCompetitionCache()
        {
            if (economyManager != null)
            {
                SectorCompetitionService.BuildProjectCountCache(economyManager.ActiveProjects, rivals);
            }
            else
            {
                SectorCompetitionService.BuildProjectCountCache(null, rivals);
            }
        }

        public void ForceRebuildCompetitionCache()
        {
            RebuildCompetitionCache();
        }

        public RivalCompaniesSaveData CaptureSaveData()
        {
            if (!isInitialized)
            {
                Initialize();
            }

            var saveData = new RivalCompaniesSaveData();
            for (var i = 0; i < rivals.Count; i++)
            {
                if (rivals[i] != null)
                {
                    saveData.rivals.Add(rivals[i].CaptureSaveData());
                }
            }

            return saveData;
        }

        public bool RestoreFromSaveData(RivalCompaniesSaveData saveData, GameSaveDefinitionResolver resolver, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (saveData == null)
            {
                validationMessage = "Rakip firma kayıt verisi bulunamadı.";
                return false;
            }

            rivals.Clear();
            for (var i = 0; i < saveData.rivals.Count; i++)
            {
                var savedRival = saveData.rivals[i];
                if (!resolver.TryResolve<RivalCompanyDefinition>(savedRival.definitionId, out var definition))
                {
                    validationMessage = $"Rakip firma tanımı bulunamadı: {savedRival.definitionId}";
                    return false;
                }

                var rival = new RivalCompanyRuntimeData(definition);
                if (!rival.RestoreFromSaveData(savedRival, resolver, out validationMessage))
                {
                    return false;
                }

                rivals.Add(rival);
            }

            isInitialized = true;
            RebuildCompetitionCache();
            DataChanged?.Invoke();
            return true;
        }

        private void OnDayAdvanced(int currentDay)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            SectorCompetitionService.AdvanceLingeringDay();
            RebuildCompetitionCache();

            var playerProjects = economyManager != null ? economyManager.ActiveProjects : null;

            for (var i = 0; i < rivals.Count; i++)
            {
                var shouldSendAgent = rivals[i].AdvanceDay(currentDay);
                if (shouldSendAgent && agentManager != null && playerProjects != null && playerProjects.Count > 0)
                {
                    agentManager.TrySendRivalAgentToPlayer(rivals[i], playerProjects, false);
                }
            }

            DataChanged?.Invoke();
        }
    }
}
