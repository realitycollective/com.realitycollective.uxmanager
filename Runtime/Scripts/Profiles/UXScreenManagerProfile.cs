// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.ServiceFramework.Definitions;
using RealityCollective.UXManager.Interfaces.ScreenManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealityCollective.UXManager.Profiles.ScreenManagement
{
    /// <summary>
    /// Marks the Screen Key mappings list for a custom property drawer.
    /// This avoids overriding the base service profile inspector.
    /// </summary>
    public sealed class ScreenKeyMappingsAttribute : PropertyAttribute { }

    [Serializable]
    public sealed class ScreenKeyMapping
    {
        [Tooltip("Key used in code (ex: ScreenNames.Main). This becomes a generated constant.")]
        public string Key;

        [Tooltip("Configured screen id/name that screens register with (ex: GameObject name, Addressables key, etc.).")]
        public string ScreenId;
    }

    [CreateAssetMenu(menuName = "UXScreenManagerProfile", fileName = "UXScreenManagerProfile", order = (int)CreateProfileMenuItemIndices.ServiceConfig)]
    public class UXScreenManagerProfile : BaseServiceProfile<IUXScreenManager>
    {
        [SerializeField, ScreenKeyMappings]
        private List<ScreenKeyMapping> screenKeyMappings = new();

        public IReadOnlyList<ScreenKeyMapping> ScreenKeyMappings => screenKeyMappings;

        private Dictionary<string, string> keyToScreenId;

        private void OnEnable()
        {
            BuildMap();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            BuildMap();
        }
#endif

        private void BuildMap()
        {
            keyToScreenId = new Dictionary<string, string>(StringComparer.Ordinal);

            if (screenKeyMappings == null)
            {
                return;
            }

            foreach (var mapping in screenKeyMappings)
            {
                if (mapping == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(mapping.Key) || string.IsNullOrWhiteSpace(mapping.ScreenId))
                {
                    continue;
                }

                keyToScreenId[mapping.Key] = mapping.ScreenId;
            }
        }

        public bool TryResolveScreenId(string key, out string screenId)
        {
            screenId = null;

            if (keyToScreenId == null)
            {
                BuildMap();
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return keyToScreenId != null && keyToScreenId.TryGetValue(key, out screenId);
        }

        /// <summary>
        /// Resolves a code-facing key to the configured runtime screen id. Falls back to the key.
        /// </summary>
        public string ResolveScreenId(string key)
        {
            return TryResolveScreenId(key, out var screenId) ? screenId : key;
        }
    }
}