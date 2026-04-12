using System;
using CompanySimulator.Features.Employees.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Furniture.Runtime.Definitions
{
    [Serializable]
    public sealed class FurnitureEffectDefinition
    {
        [SerializeField] private FurnitureEffectType effectType;
        [SerializeField] private float magnitude;
        [SerializeField] private bool scalesWithTier = true;
        [SerializeField] private string targetTag;
        [SerializeField] private EmployeeRoleDefinition targetRole;
        [SerializeField] private string notes;

        public FurnitureEffectType EffectType => effectType;
        public float Magnitude => magnitude;
        public bool ScalesWithTier => scalesWithTier;
        public string TargetTag => targetTag ?? string.Empty;
        public EmployeeRoleDefinition TargetRole => targetRole;
        public string Notes => notes ?? string.Empty;
    }
}
