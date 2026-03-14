using UnityEngine;

namespace CompanySimulator.Shared.Runtime.Definitions
{
    public abstract class DefinitionBase : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = name.Replace(" ", string.Empty);
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = name;
            }
        }
#endif
    }
}
