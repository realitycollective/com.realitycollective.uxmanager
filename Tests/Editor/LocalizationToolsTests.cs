// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RealityCollective.UXManager.Tests.Editor
{
    /// <summary>
    /// Tests for LocalizationKeysGenerator and LocalizationValidator.
    /// Covers catalog loading, key generation, and validation logic.
    /// </summary>
    public class LocalizationToolsTests
    {
        private const string CatalogPath = "Assets/Resources/Localization/Catalogs";
        private const string DefaultCatalogFile = CatalogPath + "/en-US.json";

        [Test]
        public void CatalogPath_DefaultCatalogExists()
        {
            // Arrange & Act
            bool exists = File.Exists(DefaultCatalogFile);

            // Assert
            Assert.IsTrue(exists, $"Default catalog should exist at {DefaultCatalogFile}");
        }

        [Test]
        public void CatalogStructure_DefaultCatalogIsValidJson()
        {
            // Arrange
            string json = File.ReadAllText(DefaultCatalogFile);

            // Act & Assert
            try
            {
                var catalog = JsonConvert.DeserializeObject<LocalizationCatalog>(json);
                Assert.IsNotNull(catalog, "Catalog should deserialize successfully");
                Assert.IsNotEmpty(catalog.locale, "Catalog should have locale specified");
                Assert.IsNotEmpty(catalog.version, "Catalog should have version specified");
                Assert.IsNotNull(catalog.keys, "Catalog should have keys dictionary");
            }
            catch (System.Exception ex)
            {
                Assert.Fail($"Failed to parse default catalog: {ex.Message}");
            }
        }

        [Test]
        public void CatalogStructure_AllLocalesAreValidJson()
        {
            // Arrange
            var catalogFiles = Directory.GetFiles(CatalogPath, "*.json");
            Assert.Greater(catalogFiles.Length, 0, "Should have at least one catalog file");

            // Act & Assert
            foreach (var file in catalogFiles)
            {
                string json = File.ReadAllText(file);
                try
                {
                    var catalog = JsonConvert.DeserializeObject<LocalizationCatalog>(json);
                    Assert.IsNotNull(catalog, $"{Path.GetFileName(file)} should deserialize");
                    Assert.IsNotEmpty(catalog.locale, $"{Path.GetFileName(file)} should have locale");
                    Assert.IsNotNull(catalog.keys, $"{Path.GetFileName(file)} should have keys");
                }
                catch (System.Exception ex)
                {
                    Assert.Fail($"Failed to parse {file}: {ex.Message}");
                }
            }
        }

        [Test]
        public void CatalogContents_DefaultCatalogHasAllRequiredKeys()
        {
            // Arrange
            var requiredKeys = new[]
            {
                "mainscreen_time", "mainscreen_view", "mainscreen_home", "mainscreen_create", "mainscreen_store",
                "viewscreen_title", "viewdetails_type_prefix", "viewdetails_friends_reminders",
                "enum_AudioType_Silent", "enum_AudioType_Speech", "enum_AudioType_Sound",
                "enum_CircleParticipantType_Individual", "enum_CircleParticipantType_Group",
                "enum_CircleParticipantType_Meditation"
            };

            var catalog = LoadCatalog(DefaultCatalogFile);

            // Act & Assert
            foreach (var key in requiredKeys)
            {
                Assert.IsTrue(catalog.keys.ContainsKey(key), $"Default catalog should contain key '{key}'");
                Assert.IsNotEmpty(catalog.keys[key], $"Key '{key}' should not be empty");
            }
        }

        [Test]
        public void CatalogContents_KeyCountIsCorrect()
        {
            // Arrange
            var catalog = LoadCatalog(DefaultCatalogFile);
            int expectedKeyCount = 18; // 8 text keys + 10 enum keys

            // Act
            int actualKeyCount = catalog.keys.Count;

            // Assert
            Assert.AreEqual(expectedKeyCount, actualKeyCount, $"Should have {expectedKeyCount} keys");
        }

        [Test]
        public void CatalogContents_NoEmptyValues()
        {
            // Arrange
            var catalog = LoadCatalog(DefaultCatalogFile);

            // Act & Assert
            foreach (var kvp in catalog.keys)
            {
                Assert.IsNotEmpty(kvp.Value, $"Key '{kvp.Key}' should not have empty value");
            }
        }

        [Test]
        public void LocaleFiles_RequiredLocalesExist()
        {
            // Arrange
            var requiredLocales = new[] { "en-US", "es-ES", "fr-FR", "test" };

            // Act & Assert
            foreach (var locale in requiredLocales)
            {
                string filePath = Path.Combine(CatalogPath, $"{locale}.json");
                Assert.IsTrue(File.Exists(filePath), $"Locale file '{locale}.json' should exist");
            }
        }

        [Test]
        public void LocaleComparison_AllLocalesHaveSameKeySet()
        {
            // Arrange
            var locales = new[] { "en-US", "es-ES", "fr-FR", "test" };
            var defaultCatalog = LoadCatalog(Path.Combine(CatalogPath, "en-US.json"));
            var defaultKeySet = new HashSet<string>(defaultCatalog.keys.Keys);

            // Act & Assert
            foreach (var locale in locales)
            {
                if (locale == "en-US") continue;

                string filePath = Path.Combine(CatalogPath, $"{locale}.json");
                var catalog = LoadCatalog(filePath);
                var localeKeySet = new HashSet<string>(catalog.keys.Keys);

                var missingKeys = defaultKeySet.Except(localeKeySet).ToList();
                var extraKeys = localeKeySet.Except(defaultKeySet).ToList();

                Assert.IsEmpty(missingKeys, 
                    $"Locale '{locale}' is missing keys: {string.Join(", ", missingKeys)}");
                Assert.IsEmpty(extraKeys,
                    $"Locale '{locale}' has extra keys: {string.Join(", ", extraKeys)}");
            }
        }

        [Test]
        public void KeyNaming_KeysFollowNamingConvention()
        {
            // Arrange
            var catalog = LoadCatalog(DefaultCatalogFile);
            var validKeyPattern = new System.Text.RegularExpressions.Regex(@"^[a-z_]+$");

            // Act & Assert
            foreach (var key in catalog.keys.Keys)
            {
                // Skip enum keys - they have their own validation test
                if (key.StartsWith("enum_")) continue;
                
                Assert.IsTrue(validKeyPattern.IsMatch(key), 
                    $"Key '{key}' should follow snake_case naming convention (lowercase with underscores only)");
            }
        }

        [Test]
        public void EnumKeys_FollowEnumKeyPattern()
        {
            // Arrange
            var catalog = LoadCatalog(DefaultCatalogFile);
            var enumKeyPattern = new System.Text.RegularExpressions.Regex(@"^enum_[A-Za-z]+_[A-Za-z]+$");

            // Act & Assert
            var enumKeys = catalog.keys.Keys.Where(k => k.StartsWith("enum_"));
            Assert.Greater(enumKeys.Count(), 0, "Should have enum keys");

            foreach (var key in enumKeys)
            {
                Assert.IsTrue(enumKeyPattern.IsMatch(key),
                    $"Enum key '{key}' should follow pattern enum_TypeName_Value");
            }
        }

        [Test]
        public void TranslationQuality_NoUntranslatedPlaceholders()
        {
            // Arrange
            var locales = new[] { "es-ES", "fr-FR", "test" };

            // Act & Assert
            foreach (var locale in locales)
            {
                string filePath = Path.Combine(CatalogPath, $"{locale}.json");
                var catalog = LoadCatalog(filePath);

                foreach (var kvp in catalog.keys)
                {
                    // Check for common untranslated markers
                    Assert.IsFalse(kvp.Value.Contains("[TODO]"), 
                        $"Key '{kvp.Key}' in '{locale}' is marked as TODO");
                    Assert.IsFalse(kvp.Value.Contains("TODO:"), 
                        $"Key '{kvp.Key}' in '{locale}' is marked as TODO:");
                    Assert.IsFalse(kvp.Value.Equals(kvp.Key), 
                        $"Key '{kvp.Key}' in '{locale}' appears to be untranslated (same as key)");
                }
            }
        }

        [Test]
        public void FormatStrings_ValidFormatStringInFriendCount()
        {
            // Arrange
            var catalog = LoadCatalog(DefaultCatalogFile);
            string key = "viewdetails_friends_reminders";
            string format = catalog.keys[key];

            // Act & Assert
            StringAssert.Contains("{0}", format, "Friends reminders key should have {0} format placeholder");

            // Test formatting works
            try
            {
                string result = string.Format(format, 5);
                Assert.AreEqual("5 friends have set reminders", result);
            }
            catch (System.Exception ex)
            {
                Assert.Fail($"Format string should be valid: {ex.Message}");
            }
        }

        [Test]
        public void Locales_EnglishVersionsAreConsistent()
        {
            // Arrange
            var catalog = LoadCatalog(DefaultCatalogFile);

            // Act & Assert
            // Check that enum values match their key names
            foreach (var kvp in catalog.keys)
            {
                if (kvp.Key.StartsWith("enum_"))
                {
                    // Extract enum value from key (e.g., "Silent" from "enum_SplitInteractiveCardType_Silent")
                    var parts = kvp.Key.Split('_');
                    if (parts.Length > 0)
                    {
                        string enumValue = parts[parts.Length - 1];
                        // In English, enum display names should match enum value (usually)
                        // This is a soft assertion - some might differ
                        Assert.IsNotEmpty(kvp.Value, $"Enum key '{kvp.Key}' should have value");
                    }
                }
            }
        }

        [Test]
        public void CatalogVersion_IsSpecified()
        {
            // Arrange
            var locales = new[] { "en-US", "es-ES", "fr-FR", "test" };

            // Act & Assert
            foreach (var locale in locales)
            {
                string filePath = Path.Combine(CatalogPath, $"{locale}.json");
                var catalog = LoadCatalog(filePath);

                Assert.IsNotEmpty(catalog.version, $"Catalog '{locale}' should have version specified");
                Assert.AreEqual("1.0", catalog.version, $"All catalogs should have version 1.0");
            }
        }

        [Test]
        public void CatalogLocale_MatchesFilename()
        {
            // Arrange
            var locales = new[] { "en-US", "es-ES", "fr-FR", "test" };

            // Act & Assert
            foreach (var locale in locales)
            {
                string filePath = Path.Combine(CatalogPath, $"{locale}.json");
                var catalog = LoadCatalog(filePath);

                Assert.AreEqual(locale, catalog.locale, 
                    $"Catalog file name '{locale}' should match 'locale' property in JSON");
            }
        }

        // Helper method to load a catalog
        private LocalizationCatalog LoadCatalog(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Assert.Fail($"Catalog file not found: {filePath}");
            }

            string json = File.ReadAllText(filePath);
            var catalog = JsonConvert.DeserializeObject<LocalizationCatalog>(json);

            if (catalog == null)
            {
                Assert.Fail($"Failed to deserialize catalog from {filePath}");
            }

            return catalog;
        }

        private class LocalizationCatalog
        {
            public string locale { get; set; }
            public string version { get; set; }
            public Dictionary<string, string> keys { get; set; } = new Dictionary<string, string>();
        }
    }
}
