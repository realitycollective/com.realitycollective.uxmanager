// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using RealityCollective.Utilities.Logging;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RealityCollective.UXManager.Utilities.Localization
{
    /// <summary>
    /// Utility for loading localization catalogs from Unity Resources folder.
    /// Catalogs are bundled with the application and loaded into memory.
    /// Supports both editor and runtime loading.
    /// </summary>
    public static class ResourcesCatalogLoader
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
        /// Load a localization catalog from Resources.
        /// </summary>
        /// <param name="catalogPath">Base path to catalogs folder (e.g., "Assets/Resources/Localization/Catalogs")</param>
        /// <param name="localeCode">Locale code (e.g., "en-US")</param>
        /// <returns>Dictionary of key-value pairs, or null if failed</returns>
        public static Dictionary<string, string> LoadCatalog(string catalogPath, string localeCode)
        {
            if (string.IsNullOrWhiteSpace(catalogPath) || string.IsNullOrWhiteSpace(localeCode))
            {
                StaticLogger.LogError($"[ResourcesCatalogLoader] Invalid parameters: catalogPath='{catalogPath}', localeCode='{localeCode}'");
                return null;
            }

            // Convert catalog path to Resources path
            // Expected: "Assets/Resources/Localization/Catalogs" -> "Localization/Catalogs"
            string resourcesPath = catalogPath;
            
            // Strip "Assets/Resources/" prefix if present
            if (resourcesPath.StartsWith("Assets/Resources/"))
            {
                resourcesPath = resourcesPath.Substring("Assets/Resources/".Length);
            }
            
            // Build resource path (without .json extension)
            string resourcePath = Path.Combine(resourcesPath, localeCode).Replace('\\', '/');
            
            // Try loading from Resources first (for builds)
            TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
            
            if (textAsset != null)
            {
                return DeserializeCatalog(textAsset.text, localeCode, "Resources");
            }

            // Fallback to file system for editor
#if UNITY_EDITOR
            return LoadCatalogFromFileSystem(catalogPath, localeCode);
#else
            StaticLogger.LogError($"[ResourcesCatalogLoader] Catalog not found in Resources: {resourcePath}");
            return null;
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Load catalog from file system in editor.
        /// </summary>
        private static Dictionary<string, string> LoadCatalogFromFileSystem(string catalogPath, string localeCode)
        {
            string filePath = Path.Combine(catalogPath, $"{localeCode}.json");
            
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(Application.dataPath, "..", filePath);
            }

            if (!File.Exists(filePath))
            {
                StaticLogger.LogError($"[ResourcesCatalogLoader] Catalog file not found: {filePath}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                return DeserializeCatalog(json, localeCode, "File System");
            }
            catch (Exception ex)
            {
                StaticLogger.LogError($"[ResourcesCatalogLoader] Error loading catalog '{localeCode}': {ex.Message}");
                return null;
            }
        }
#endif

        /// <summary>
        /// Deserialize JSON catalog to dictionary.
        /// </summary>
        private static Dictionary<string, string> DeserializeCatalog(string json, string localeCode, string source)
        {
            try
            {
                var catalog = JsonConvert.DeserializeObject<LocalizationCatalog>(json);
                
                if (catalog == null || catalog.keys == null)
                {
                    StaticLogger.LogError($"[ResourcesCatalogLoader] Invalid catalog format from {source}: {localeCode}");
                    return null;
                }

                StaticLogger.Log($"[ResourcesCatalogLoader] Loaded {catalog.keys.Count} keys from '{localeCode}' ({source}).");
                return catalog.keys;
            }
            catch (Exception ex)
            {
                StaticLogger.LogError($"[ResourcesCatalogLoader] Error deserializing catalog '{localeCode}' from {source}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get diagnostic information about available catalogs.
        /// </summary>
        public static void LogDiagnostics(string catalogPath)
        {
            string resourcesPath = catalogPath;
            if (resourcesPath.StartsWith("Assets/Resources/"))
            {
                resourcesPath = resourcesPath.Substring("Assets/Resources/".Length);
            }

            StaticLogger.Log($"[ResourcesCatalogLoader] Resources Path: {resourcesPath}");

#if UNITY_EDITOR
            // In editor, check file system
            string catalogDir = catalogPath;
            if (!Path.IsPathRooted(catalogDir))
            {
                catalogDir = Path.Combine(Application.dataPath, "..", catalogDir);
            }

            if (Directory.Exists(catalogDir))
            {
                string[] catalogFiles = Directory.GetFiles(catalogDir, "*.json");
                if (catalogFiles.Length > 0)
                {
                    StaticLogger.Log($"[ResourcesCatalogLoader] Available catalog files (File System):");
                    foreach (string file in catalogFiles)
                    {
                        string fileName = Path.GetFileName(file);
                        StaticLogger.Log($"[ResourcesCatalogLoader]   - {fileName}");
                    }
                }
            }
            else
            {
                StaticLogger.LogWarning($"[ResourcesCatalogLoader] Catalog directory does not exist: {catalogDir}");
            }
#endif
        }
    }
}
