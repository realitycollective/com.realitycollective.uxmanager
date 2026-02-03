// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_ADDRESSABLES

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RealityCollective.Utilities.Logging;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RealityCollective.UXManager.Utilities.Localization
{
    /// <summary>
    /// Utility for loading localization catalogs from Unity Addressables.
    /// </summary>
    public static class AddressablesCatalogLoader
    {
        /// <summary>
        /// Internal catalog structure for JSON deserialization.
        /// </summary>
        private class LocalizationCatalog
        {
            public string locale { get; set; }
            public string version { get; set; }
            public Dictionary<string, string> keys { get; set; } = new Dictionary<string, string>();
        }

        /// <summary>
        /// Load a localization catalog from Addressables synchronously.
        /// </summary>
        public static Dictionary<string, string> LoadCatalog(string catalogPath, string localeCode)
        {
            if (string.IsNullOrWhiteSpace(catalogPath) || string.IsNullOrWhiteSpace(localeCode))
            {
                StaticLogger.LogError($"[AddressablesCatalogLoader] Invalid parameters: catalogPath='{catalogPath}', localeCode='{localeCode}'");
                return null;
            }

            string addressableKey = BuildAddressableKey(catalogPath, localeCode);
            StaticLogger.Log($"[AddressablesCatalogLoader] Loading catalog '{localeCode}' from Addressables: {addressableKey}");

            try
            {
                // Load the TextAsset asynchronously
                var handle = Addressables.LoadAssetAsync<UnityEngine.TextAsset>(addressableKey);
                
                // Wait for completion synchronously
                handle.WaitForCompletion();

                // Check operation result
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    if (handle.Result != null)
                    {
                        var result = DeserializeCatalog(handle.Result.text, localeCode);
                        Addressables.Release(handle);
                        return result;
                    }
                    else
                    {
                        StaticLogger.LogError($"[AddressablesCatalogLoader] Result is null for key: {addressableKey}");
                        Addressables.Release(handle);
                        return null;
                    }
                }
                else
                {
                    StaticLogger.LogError($"[AddressablesCatalogLoader] Load failed with status: {handle.Status}");
                    if (handle.OperationException != null)
                    {
                        StaticLogger.LogError($"[AddressablesCatalogLoader] Exception: {handle.OperationException.Message}");
                    }
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                StaticLogger.LogError($"[AddressablesCatalogLoader] Exception loading catalog '{localeCode}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Build the appropriate addressable key.
        /// </summary>
        private static string BuildAddressableKey(string catalogPath, string localeCode)
        {
            if (catalogPath.Contains("/"))
            {
                return $"{catalogPath}/{localeCode}";
            }
            return $"{catalogPath}_{localeCode}";
        }

        /// <summary>
        /// Deserialize JSON catalog to dictionary.
        /// </summary>
        private static Dictionary<string, string> DeserializeCatalog(string json, string localeCode)
        {
            try
            {
                var catalog = JsonConvert.DeserializeObject<LocalizationCatalog>(json);
                
                if (catalog == null || catalog.keys == null)
                {
                    StaticLogger.LogError($"[AddressablesCatalogLoader] Invalid catalog format: {localeCode}");
                    return null;
                }

                StaticLogger.Log($"[AddressablesCatalogLoader] ✓ Loaded {catalog.keys.Count} keys from '{localeCode}'");
                return catalog.keys;
            }
            catch (Exception ex)
            {
                StaticLogger.LogError($"[AddressablesCatalogLoader] Deserialization error for '{localeCode}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Log diagnostic information.
        /// </summary>
        public static void LogDiagnostics(string catalogPath)
        {
            StaticLogger.Log($"[AddressablesCatalogLoader] Addressables Path: {catalogPath}");
        }
    }
}

#endif
