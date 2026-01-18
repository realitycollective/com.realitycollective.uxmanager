// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace RealityCollective.UXManager.Extensions
{
    /// <summary>
    /// Extension methods for Unity SystemLanguage enum to convert to locale codes.
    /// </summary>
    public static class SystemLanguageExtensions
    {
        /// <summary>
        /// Converts a Unity SystemLanguage to a standard locale code (e.g., "en-US").
        /// </summary>
        /// <param name="language">Unity system language</param>
        /// <returns>Locale code string</returns>
        public static string ToLocaleCode(this SystemLanguage language)
        {
            return language switch
            {
                SystemLanguage.English => "en-US",
                SystemLanguage.Spanish => "es-ES",
                SystemLanguage.French => "fr-FR",
                SystemLanguage.German => "de-DE",
                SystemLanguage.Italian => "it-IT",
                SystemLanguage.Portuguese => "pt-BR",
                SystemLanguage.Japanese => "ja-JP",
                SystemLanguage.Chinese => "zh-CN",
                SystemLanguage.Korean => "ko-KR",
                SystemLanguage.Arabic => "ar-SA",
                SystemLanguage.Russian => "ru-RU",
                SystemLanguage.Dutch => "nl-NL",
                SystemLanguage.Swedish => "sv-SE",
                SystemLanguage.Norwegian => "no-NO",
                SystemLanguage.Danish => "da-DK",
                SystemLanguage.Finnish => "fi-FI",
                SystemLanguage.Polish => "pl-PL",
                SystemLanguage.Turkish => "tr-TR",
                SystemLanguage.Hungarian => "hu-HU",
                SystemLanguage.Czech => "cs-CZ",
                SystemLanguage.Romanian => "ro-RO",
                SystemLanguage.Thai => "th-TH",
                SystemLanguage.Greek => "el-GR",
                SystemLanguage.Bulgarian => "bg-BG",
                SystemLanguage.SerboCroatian => "sh-SH",
                SystemLanguage.Slovak => "sk-SK",
                SystemLanguage.Slovenian => "sl-SI",
                SystemLanguage.Estonian => "et-EE",
                SystemLanguage.Latvian => "lv-LV",
                SystemLanguage.Lithuanian => "lt-LT",
                SystemLanguage.Vietnamese => "vi-VN",
                SystemLanguage.Indonesian => "id-ID",
                SystemLanguage.Catalan => "ca-ES",
                _ => "en-US" // Default fallback
            };
        }
    }
}
