using System;
using System.Collections.Generic;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Features.Projects.Runtime.Definitions;
using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Features.Sectors.Runtime.Models;
using UnityEngine;

namespace CompanySimulator.Features.Sectors.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class SectorManager : MonoBehaviour
    {
        [SerializeField] private SectorCatalogDefinition catalog;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private bool initializeOnStart = true;

        private readonly List<SectorRuntimeData> sectors = new List<SectorRuntimeData>(8);
        private readonly Dictionary<SectorDefinition, SectorRuntimeData> sectorLookup = new Dictionary<SectorDefinition, SectorRuntimeData>(8);
        private bool isInitialized;

        public event Action DataChanged;

        public bool IsInitialized => isInitialized;
        public IReadOnlyList<SectorRuntimeData> Sectors => sectors;

        private void Awake()
        {
            // Referanslar inspector'dan verilmediyse sahneden otomatik bulunur.
            economyManager ??= FindObjectOfType<EconomyManager>();
        }

        private void OnEnable()
        {
            SubscribeToEconomy();
        }

        private void Start()
        {
            if (initializeOnStart)
            {
                Initialize();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromEconomy();
        }

        [ContextMenu("Sektörleri Başlat")]
        public void Initialize()
        {
            if (catalog == null)
            {
                Debug.LogError("SectorManager için katalog atanmadı.", this);
                isInitialized = false;
                return;
            }

            BuildCatalogState();
            RefreshActiveProjectCounts();
            isInitialized = true;
            DataChanged?.Invoke();
        }

        public bool TryGetSectorData(SectorDefinition sector, out SectorRuntimeData sectorData)
        {
            if (!EnsureInitialized() || sector == null)
            {
                sectorData = null;
                return false;
            }

            return sectorLookup.TryGetValue(sector, out sectorData);
        }

        public void ForceRefreshFromEconomy()
        {
            if (!isInitialized)
            {
                Initialize();
                return;
            }

            RefreshActiveProjectCounts();
            DataChanged?.Invoke();
        }

        private bool EnsureInitialized()
        {
            if (isInitialized)
            {
                return true;
            }

            Initialize();
            return isInitialized;
        }

        private void BuildCatalogState()
        {
            sectors.Clear();
            sectorLookup.Clear();

            var catalogSectors = catalog.Sectors;
            for (var i = 0; i < catalogSectors.Count; i++)
            {
                var sector = catalogSectors[i];
                RegisterSector(sector);
            }

            var projects = catalog.Projects;
            for (var i = 0; i < projects.Count; i++)
            {
                var project = projects[i];
                var sector = project != null && project.ProjectType != null ? project.ProjectType.Sector : null;
                var sectorData = RegisterSector(sector);
                sectorData?.AddProject(project);
            }
        }

        private SectorRuntimeData RegisterSector(SectorDefinition sector)
        {
            if (sector == null)
            {
                return null;
            }

            if (sectorLookup.TryGetValue(sector, out var existingSectorData))
            {
                return existingSectorData;
            }

            var sectorData = new SectorRuntimeData(sector);
            sectors.Add(sectorData);
            sectorLookup.Add(sector, sectorData);
            return sectorData;
        }

        private void RefreshActiveProjectCounts()
        {
            for (var i = 0; i < sectors.Count; i++)
            {
                sectors[i].ResetProgress();
            }

            if (economyManager == null)
            {
                return;
            }

            var activeProjects = economyManager.ActiveProjects;
            for (var i = 0; i < activeProjects.Count; i++)
            {
                RegisterActiveProject(activeProjects[i]);
            }
        }

        private void SubscribeToEconomy()
        {
            if (economyManager == null)
            {
                economyManager = FindObjectOfType<EconomyManager>();
            }

            if (economyManager != null)
            {
                economyManager.ProjectExecuted -= HandleProjectExecuted;
                economyManager.ProjectExecuted += HandleProjectExecuted;
                economyManager.ProjectSold -= HandleProjectSold;
                economyManager.ProjectSold += HandleProjectSold;
            }
        }

        private void UnsubscribeFromEconomy()
        {
            if (economyManager != null)
            {
                economyManager.ProjectExecuted -= HandleProjectExecuted;
                economyManager.ProjectSold -= HandleProjectSold;
            }
        }

        private void HandleProjectExecuted(ProjectExecutionDefinition executionDefinition, ProjectEconomyResult result)
        {
            RefreshActiveProjectCounts();
            DataChanged?.Invoke();
        }

        private void HandleProjectSold(ActiveProjectRuntimeEntry activeProject)
        {
            RefreshActiveProjectCounts();
            DataChanged?.Invoke();
        }

        private void RegisterActiveProject(ActiveProjectRuntimeEntry activeProject)
        {
            if (activeProject == null)
            {
                return;
            }

            var sector = activeProject.ProjectType != null ? activeProject.ProjectType.Sector : null;
            if (sector == null)
            {
                return;
            }

            var sectorData = RegisterSector(sector);
            sectorData?.RegisterActiveProject(activeProject);
        }
    }
}
