// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RealityCollective.UXManager.Editor
{
    /// <summary>
    /// Editor tool to validate localization catalog completeness.
    /// Ensures all non-default locale files contain all keys from the default locale.
    /// 
    /// IMPORTANT: Catalog files must be located in a Resources folder (e.g., Assets/Resources/Localization/Catalogs/)
    /// to be included in builds. Check the LocalizationServiceProfile for the configured catalog path.
    /// </summary>
    public static class LocalizationValidator
    {
        [MenuItem("Reality Toolkit/UX Manager/Validate Locale Completeness")]
        public static void Validate()
        {
            string catalogPath = "Assets/Resources/Localization/Catalogs";
            string defaultCatalog = Path.Combine(catalogPath, "en-US.json");

            if (!Directory.Exists(catalogPath))
            {
                Debug.LogError($"[LocalizationValidator] Catalog path not found: {catalogPath}");
                return;
            }

            if (!File.Exists(defaultCatalog))
            {
                Debug.LogError($"[LocalizationValidator] Default catalog not found: {defaultCatalog}");
                return;
            }

            var defaultKeys = LoadKeys(defaultCatalog);
            if (defaultKeys == null || defaultKeys.Count == 0)
            {
                Debug.LogError("[LocalizationValidator] Default catalog is empty or invalid.");
                return;
            }

            Debug.Log($"[LocalizationValidator] Default locale (en-US) has {defaultKeys.Count} keys. Validating other locales...");

            var catalogFiles = Directory.GetFiles(catalogPath, "*.json")
                .Where(f => !Path.GetFileName(f).StartsWith("en-US"))
                .ToList();

            if (catalogFiles.Count == 0)
            {
                Debug.LogWarning("[LocalizationValidator] No additional locale files found.");
                return;
            }

            bool allValid = true;
            int validCount = 0;

            foreach (var file in catalogFiles)
            {
                string locale = Path.GetFileNameWithoutExtension(file);
                var keys = LoadKeys(file);

                if (keys == null)
                {
                    Debug.LogError($"[LocalizationValidator] [{locale}] Failed to load catalog.");
                    allValid = false;
                    continue;
                }

                var missingKeys = defaultKeys.Except(keys).ToList();
                var extraKeys = keys.Except(defaultKeys).ToList();

                if (missingKeys.Count > 0)
                {
                    Debug.LogWarning($"[LocalizationValidator] [{locale}] Missing {missingKeys.Count} keys:\n  {string.Join("\n  ", missingKeys.Take(5))}" + 
                        (missingKeys.Count > 5 ? $"\n  ... and {missingKeys.Count - 5} more" : ""));
                    allValid = false;
                }

                if (extraKeys.Count > 0)
                {
                    Debug.LogWarning($"[LocalizationValidator] [{locale}] Has {extraKeys.Count} extra keys not in default:\n  {string.Join("\n  ", extraKeys.Take(5))}" + 
                        (extraKeys.Count > 5 ? $"\n  ... and {extraKeys.Count - 5} more" : ""));
                }

                if (missingKeys.Count == 0 && extraKeys.Count == 0)
                {
                    Debug.Log($"[LocalizationValidator] [{locale}] ✓ Valid ({keys.Count} keys)");
                    validCount++;
                }
            }

            Debug.Log($"[LocalizationValidator] Validation complete: {validCount}/{catalogFiles.Count} locales are complete.");
            
            if (allValid)
            {
                Debug.Log("[LocalizationValidator] ✓ All locales are complete!");
            }
            else
            {
                Debug.LogWarning("[LocalizationValidator] ⚠ Some locales are incomplete. Please add missing translations.");
            }
        }

        private static HashSet<string> LoadKeys(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var catalog = JsonUtility.FromJson<LocalizationCatalog>(json);
                IEnumerable<string> keys = new string[0];
                if(catalog != null && catalog.keys != null)
                {
                    keys = catalog.keys.Keys;
                }
                return new HashSet<string>(keys);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalizationValidator] Error loading {filePath}: {ex.Message}");
                return null;
            }
        }

        [System.Serializable]
        private class LocalizationCatalog
        {
            public string locale;
            public string version;
            public Dictionary<string, string> keys = new Dictionary<string, string>();
        }
    }
}
