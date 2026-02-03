// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using RealityCollective.ServiceFramework.Services;
using RealityCollective.UXManager.Extensions;
using RealityCollective.UXManager.Utilities.Localization;
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
        private System.Globalization.CultureInfo cachedCultureInfo;

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

            // Log startup diagnostics
            LogStartupDiagnostics();

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
                StaticLogger.Log($"[LocalizationService] System language detected: {Application.systemLanguage} → {systemLocale}");
                
                if (profile.SupportedLocales.Contains(systemLocale))
                {
                    initialLocale = systemLocale;
                    StaticLogger.Log($"[LocalizationService] System locale '{systemLocale}' is supported. Using it.");
                }
                else
                {
                    StaticLogger.LogWarning($"[LocalizationService] System locale '{systemLocale}' is NOT in supported locales. Falling back to default '{profile.DefaultLocale}'.");
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

        /// <summary>
        /// CENTRAL POINT for device locale discovery.
        /// Returns the CultureInfo for the current locale, which includes date/time formatting, number formatting, and other culture-specific rules.
        /// This should be used by UI components that need locale-aware formatting (dates, numbers, etc).
        /// Screens should call this to get CultureInfo for date/time formatting - DO NOT access Application.systemLanguage directly.
        /// </summary>
        public System.Globalization.CultureInfo GetCurrentCultureInfo()
        {
            if (string.IsNullOrEmpty(currentLocale))
            {
                StaticLogger.LogWarning($"[LocalizationService] GetCurrentCultureInfo called but currentLocale is not set. Using system default culture.");
                return System.Globalization.CultureInfo.CurrentCulture;
            }

            // Return cached CultureInfo to avoid repeated creation
            if (cachedCultureInfo != null)
            {
                return cachedCultureInfo;
            }

            try
            {
                // Convert locale code (e.g., "en-US") to CultureInfo
                // This is the central resolution point - device locale → supported locale → CultureInfo
                cachedCultureInfo = System.Globalization.CultureInfo.GetCultureInfo(currentLocale);
                StaticLogger.Log($"[LocalizationService] Device locale resolved: '{currentLocale}' → CultureInfo({cachedCultureInfo.Name}, {cachedCultureInfo.DisplayName}).");
                return cachedCultureInfo;
            }
            catch (Exception ex)
            {
                StaticLogger.LogWarning($"[LocalizationService] Could not create CultureInfo for locale '{currentLocale}': {ex.Message}. Using system default.");
                cachedCultureInfo = System.Globalization.CultureInfo.CurrentCulture;
                return cachedCultureInfo;
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
            cachedCultureInfo = null; // Clear cache when locale changes

            StaticLogger.Log($"[LocalizationService] ✓ Locale resolved to '{currentLocale}' ({currentCatalog.Count} keys loaded).");
            OnLocaleChanged?.Invoke(currentLocale);
        }

        private Dictionary<string, string> LoadCatalog(string localeCode)
        {
#if UNITY_ADDRESSABLES
            if (profile.LoadingStrategy == CatalogLoadingStrategy.Addressables)
            {
                return AddressablesCatalogLoader.LoadCatalog(profile.CatalogPath, localeCode);
            }
#endif
            return ResourcesCatalogLoader.LoadCatalog(profile.CatalogPath, localeCode);
        }


        private void LogStartupDiagnostics()
        {
            StaticLogger.Log($"[LocalizationService] ========== LOCALIZATION STARTUP DIAGNOSTICS ==========");
            StaticLogger.Log($"[LocalizationService] Loading Strategy: {profile.LoadingStrategy}");
            StaticLogger.Log($"[LocalizationService] Default Locale: {profile.DefaultLocale}");
            StaticLogger.Log($"[LocalizationService] Supported Locales: {string.Join(", ", profile.SupportedLocales)}");
            StaticLogger.Log($"[LocalizationService] Catalog Path: {profile.CatalogPath}");
            
            // Use appropriate loader's diagnostics
#if UNITY_ADDRESSABLES
            if (profile.LoadingStrategy == CatalogLoadingStrategy.Addressables)
            {
                AddressablesCatalogLoader.LogDiagnostics(profile.CatalogPath);
            }
            else
            {
                ResourcesCatalogLoader.LogDiagnostics(profile.CatalogPath);
            }
#else
            ResourcesCatalogLoader.LogDiagnostics(profile.CatalogPath);
#endif

            StaticLogger.Log($"[LocalizationService] System Language: {Application.systemLanguage}");
            StaticLogger.Log($"[LocalizationService] ====================================================");
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
    }
}
