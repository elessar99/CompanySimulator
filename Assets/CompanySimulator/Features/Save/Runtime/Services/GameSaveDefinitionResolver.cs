using System;
using System.Collections.Generic;
using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Save.Runtime.Services
{
    public sealed class GameSaveDefinitionResolver
    {
        private readonly Dictionary<Type, Dictionary<string, DefinitionBase>> definitionsByType =
            new Dictionary<Type, Dictionary<string, DefinitionBase>>();

        public void Refresh()
        {
            definitionsByType.Clear();

            var loadedDefinitions = Resources.FindObjectsOfTypeAll<DefinitionBase>();
            for (var i = 0; i < loadedDefinitions.Length; i++)
            {
                Register(loadedDefinitions[i]);
            }
        }

        public bool TryResolve<TDefinition>(string id, out TDefinition definition)
            where TDefinition : DefinitionBase
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            if (!definitionsByType.TryGetValue(typeof(TDefinition), out var definitions) ||
                !definitions.TryGetValue(id, out var rawDefinition))
            {
                return false;
            }

            definition = rawDefinition as TDefinition;
            return definition != null;
        }

        private void Register(DefinitionBase definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
            {
                return;
            }

            var type = definition.GetType();
            RegisterForType(type, definition);

            var current = type.BaseType;
            while (current != null && current != typeof(UnityEngine.Object))
            {
                if (typeof(DefinitionBase).IsAssignableFrom(current))
                {
                    RegisterForType(current, definition);
                }

                current = current.BaseType;
            }
        }

        private void RegisterForType(Type type, DefinitionBase definition)
        {
            if (!definitionsByType.TryGetValue(type, out var definitions))
            {
                definitions = new Dictionary<string, DefinitionBase>(StringComparer.Ordinal);
                definitionsByType.Add(type, definitions);
            }

            if (!definitions.ContainsKey(definition.Id))
            {
                definitions.Add(definition.Id, definition);
            }
        }
    }
}
