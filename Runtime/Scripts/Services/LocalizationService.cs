// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RealityCollective.ServiceFramework.Services;
using RealityCollective.UXManager.Extensions;
using RealityCollective.Utilities.Logging;
using UnityEngine;
using RealityCollective.UXManager.Interfaces.Localization;
using RealityCollective.UXManager.Profiles.Localization;

namespace RealityCollective.UXManager.Services.Localization
{
    /// <summary>
    /// Service for managing application localization and translations.
    /// Loads locale catalogs from JSON files and provides key/value lookup with fallback to default locale.
    /// </summary>
    [System.Runtime.InteropServices.Guid("a8f3c2e1-9b4d-4e6f-8a7c-1d2e3f4a5b6c")]
    public class LocalizationService : BaseServiceWithConstructor, ILocalizationService
    {
        private readonly LocalizationServiceProfile profile;
        private Dictionary<string, string> currentCatalog;
        private Dictionary<string, string> defaultCatalog;
        private string currentLocale;

        public string CurrentLocale => currentLocale;
        public string DefaultLocale => profile.DefaultLocale;
        public IReadOnlyList<string> SupportedLocales => profile.SupportedLocales;

        public event Action<string> OnLocaleChanged;

        public LocalizationService(string name, uint priority, LocalizationServiceProfile profile)
            : base(name, priority)
        {
            this.profile = profile;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Load default locale catalog (always needed for fallback)
            defaultCatalog = LoadCatalog(profile.DefaultLocale);
            if (defaultCatalog == null)
            {
                StaticLogger.LogError($"[LocalizationService] Failed to load default locale '{profile.DefaultLocale}'. Service will not function correctly.");
                defaultCatalog = new Dictionary<string, string>();
            }

            // Determine initial locale
            string initialLocale = profile.DefaultLocale;
            
            if (!string.IsNullOrWhiteSpace(profile.ForceLocale))
            {
                // Testing: force specific locale
                initialLocale = profile.ForceLocale;
                StaticLogger.Log($"[LocalizationService] Forcing locale to '{initialLocale}' for testing.");
            }
            else
            {
                // Try to use system locale
                string systemLocale = Application.systemLanguage.ToLocaleCode();
                if (profile.SupportedLocales.Contains(systemLocale))
                {
                    initialLocale = systemLocale;
                    StaticLogger.Log($"[LocalizationService] Detected system locale: {systemLocale}");
                }
            }

            SetLocale(initialLocale);
        }

        public string GetString(string key, params object[] parameters)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            string value = null;

            // 1. Try current locale
            if (currentCatalog != null && currentCatalog.TryGetValue(key, out value))
            {
                return FormatString(value, parameters);
            }

            // 2. Fallback to default locale
            if (defaultCatalog != null && defaultCatalog.TryGetValue(key, out value))
            {
                if (currentLocale != profile.DefaultLocale)
                {
                    StaticLogger.LogWarning($"[LocalizationService] Key '{key}' not found in locale '{currentLocale}', using default '{profile.DefaultLocale}'.");
                }
                return FormatString(value, parameters);
            }

            // 3. Final fallback: return the key itself
            StaticLogger.LogWarning($"[LocalizationService] Key '{key}' not found in any locale. Returning key as fallback.");
            return key;
        }

        public bool TryGetString(string key, out string value, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                value = fallback;
                return false;
            }

            // 1. Try current locale
            if (currentCatalog != null && currentCatalog.TryGetValue(key, out value))
            {
                return true;
            }

            // 2. Fallback to default locale
            if (defaultCatalog != null && defaultCatalog.TryGetValue(key, out value))
            {
                if (currentLocale != profile.DefaultLocale)
                {
                    StaticLogger.LogWarning($"[LocalizationService] Key '{key}' not found in locale '{currentLocale}', using default '{profile.DefaultLocale}'.");
                }
                return true;
            }

            // 3. Use provided fallback
            value = fallback;
            return false;
        }

        public string GetEnumDisplayName(Enum value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            // Key format: enum_{EnumTypeName}_{EnumValue}
            // Example: enum_SplitInteractiveCardType_Silent
            string typeName = value.GetType().Name;
            string key = $"enum_{typeName}_{value}";
            
            return GetString(key);
        }

        public System.Globalization.CultureInfo GetCurrentCultureInfo()
        {
            if (string.IsNullOrEmpty(currentLocale))
            {
                return System.Globalization.CultureInfo.CurrentCulture;
            }

            try
            {
                // Convert locale code (e.g., "en-US") to CultureInfo
                return System.Globalization.CultureInfo.GetCultureInfo(currentLocale);
            }
            catch
            {
                StaticLogger.LogWarning($"[LocalizationService] Could not create CultureInfo for locale '{currentLocale}'. Using system default.");
                return System.Globalization.CultureInfo.CurrentCulture;
            }
        }

        public void SetLocale(string localeCode)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
            {
                StaticLogger.LogWarning("[LocalizationService] Cannot set null or empty locale.");
                return;
            }

            if (!profile.SupportedLocales.Contains(localeCode))
            {
                StaticLogger.LogWarning($"[LocalizationService] Locale '{localeCode}' is not supported. Falling back to default '{profile.DefaultLocale}'.");
                localeCode = profile.DefaultLocale;
            }

            // Load new catalog
            var newCatalog = LoadCatalog(localeCode);
            if (newCatalog == null)
            {
                StaticLogger.LogError($"[LocalizationService] Failed to load locale '{localeCode}'. Keeping current locale '{currentLocale}'.");
                return;
            }

            currentCatalog = newCatalog;
            currentLocale = localeCode;

            StaticLogger.Log($"[LocalizationService] Locale changed to '{currentLocale}' ({currentCatalog.Count} keys loaded).");
            OnLocaleChanged?.Invoke(currentLocale);
        }

        private Dictionary<string, string> LoadCatalog(string localeCode)
        {
            string filePath = Path.Combine(profile.CatalogPath, $"{localeCode}.json");
            
            // Handle both relative and absolute paths
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(Application.dataPath, "..", filePath);
            }

            if (!File.Exists(filePath))
            {
                StaticLogger.LogError($"[LocalizationService] Catalog file not found: {filePath}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var catalog = JsonConvert.DeserializeObject<LocalizationCatalog>(json);
                
                if (catalog == null || catalog.keys == null)
                {
                    StaticLogger.LogError($"[LocalizationService] Invalid catalog format in: {filePath}");
                    return null;
                }

                StaticLogger.Log($"[LocalizationService] Loaded {catalog.keys.Count} keys from '{localeCode}'.");
                return catalog.keys;
            }
            catch (Exception ex)
            {
                StaticLogger.LogError($"[LocalizationService] Error loading catalog '{localeCode}': {ex.Message}");
                return null;
            }
        }

        private string FormatString(string value, object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
            {
                return value;
            }

            try
            {
                return string.Format(value, parameters);
            }
            catch (FormatException ex)
            {
                StaticLogger.LogError($"[LocalizationService] Format error for value '{value}': {ex.Message}");
                return value;
            }
        }

        /// <summary>
        /// Internal catalog structure for JSON deserialization.
        /// </summary>
        private class LocalizationCatalog
        {
            public string locale { get; set; }
            public string version { get; set; }
            public Dictionary<string, string> keys { get; set; } = new Dictionary<string, string>();
        }
    }
}
