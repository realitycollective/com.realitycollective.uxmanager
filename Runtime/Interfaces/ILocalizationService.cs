// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;

namespace RealityCollective.UXManager.Interfaces
{
    /// <summary>
    /// Service for managing application localization and translations.
    /// Provides flat key/value lookup with automatic fallback to default locale.
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>
        /// Gets the current active locale code (e.g., "en-US", "es-ES", "test")
        /// </summary>
        string CurrentLocale { get; }

        /// <summary>
        /// The default locale used as fallback when keys are missing in current locale
        /// </summary>
        string DefaultLocale { get; }

        /// <summary>
        /// All supported locales configured in the profile
        /// </summary>
        IReadOnlyList<string> SupportedLocales { get; }

        /// <summary>
        /// Gets a localized string by key. 
        /// Falls back to default locale if not found in current locale.
        /// Returns the key itself if not found anywhere.
        /// </summary>
        /// <param name="key">Localization key (e.g., "mainscreen_time")</param>
        /// <param name="parameters">Optional format parameters for string.Format()</param>
        /// <returns>Localized string or key as fallback</returns>
        string GetString(string key, params object[] parameters);

        /// <summary>
        /// Gets a localized display name for an enum value.
        /// Key format: "enum_{EnumTypeName}_{EnumValue}" (e.g., "enum_SplitInteractiveCardType_Silent")
        /// </summary>
        /// <param name="value">Enum value to localize</param>
        /// <returns>Localized enum display name</returns>
        string GetEnumDisplayName(Enum value);

        /// <summary>
        /// Sets the current locale and reloads localization data.
        /// Fires OnLocaleChanged event when successful.
        /// </summary>
        /// <param name="localeCode">Locale code to switch to (e.g., "es-ES")</param>
        void SetLocale(string localeCode);

        /// <summary>
        /// Fired when locale changes successfully. 
        /// UI systems should subscribe to refresh text content.
        /// </summary>
        event Action<string> OnLocaleChanged;
    }
}
