using System;
using System.Collections.Generic;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Features.Rivals.Runtime.Definitions;
using CompanySimulator.Features.Rivals.Runtime.Models;
using CompanySimulator.Features.Sectors.Runtime.Services;
using UnityEngine;

namespace CompanySimulator.Features.Rivals.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class RivalCompanyManager : MonoBehaviour
    {
        [SerializeField] private RivalCompanySetupDefinition setup;
        [SerializeField] private EconomyManager economyManager;

        private readonly List<RivalCompanyRuntimeData> rivals = new List<RivalCompanyRuntimeData>(8);
        private bool isInitialized;

        public event Action DataChanged;

        public bool IsInitialized => isInitialized;
        public IReadOnlyList<RivalCompanyRuntimeData> Rivals => rivals;

        private void Awake()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();
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

        private void OnDayAdvanced(int currentDay)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            SectorCompetitionService.AdvanceLingeringDay();
            RebuildCompetitionCache();

            for (var i = 0; i < rivals.Count; i++)
            {
                rivals[i].AdvanceDay(currentDay);
            }

            DataChanged?.Invoke();
        }
    }
}
