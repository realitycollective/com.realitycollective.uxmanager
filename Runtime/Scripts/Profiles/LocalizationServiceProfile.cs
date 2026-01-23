// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using RealityCollective.ServiceFramework.Definitions;
using RealityCollective.UXManager.Interfaces.Localization;
using RealityCollective.UXManager.Services.Localization;

namespace RealityCollective.UXManager.Profiles.Localization
{
    /// <summary>
    /// Configuration profile for the LocalizationService.
    /// Specifies default locale, supported locales, catalog path, and testing options.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Reality Collective/UX Manager/Localization Service Profile",
        fileName = "LocalizationServiceProfile",
        order = 100)]
    public class LocalizationServiceProfile : BaseServiceProfile<ILocalizationService>
    {
        [Header("Locale Configuration")]

        [SerializeField]
        [Tooltip("Default locale code (e.g., 'en-US'). Used as fallback when translations are missing.")]
        private string defaultLocale = "en-US";

        [SerializeField]
        [Tooltip("Path to locale catalog files (e.g., 'Assets/UX/Localization/Catalogs'). Catalog files should be JSON format: {locale}.json")]
        private string catalogPath = "Assets/UX/Localization/Catalogs";

        [Header("Keys Management")]

        [SerializeField]
        [Tooltip("List of all localization keys with their display names")]
        private List<LocalizationKey> localizationKeys = new List<LocalizationKey>();

        [Header("Testing")]

        [SerializeField]
        [Tooltip("Force a specific locale on startup for testing purposes. Leave empty to use system locale or default.")]
        private string forceLocale = "";

        [SerializeField]
        [Tooltip("Available locale codes. Catalog files should match these names exactly (e.g., 'en-US.json', 'es-ES.json')")]
        private string[] supportedLocales = new[] { "en-US", "es-ES", "fr-FR", "test" };

        public string DefaultLocale => defaultLocale;
        public string CatalogPath => catalogPath;
        public string ForceLocale => forceLocale;
        public string[] SupportedLocales => supportedLocales;
        public List<LocalizationKey> LocalizationKeys => localizationKeys;
    }
}
