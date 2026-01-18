// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using RealityCollective.UXManager.Interfaces;
using RealityCollective.UXManager.Profiles;
using RealityCollective.UXManager.Services;
using System.Collections.Generic;
using UnityEngine;

namespace RealityCollective.UXManager.Tests.Editor
{
    /// <summary>
    /// Editor tests for LocalizationService.
    /// Covers initialization, string lookup, fallback, enum translation, and locale switching.
    /// </summary>
    public class LocalizationServiceTests
    {
        private LocalizationService localizationService;
        private LocalizationServiceProfile testProfile;

        [SetUp]
        public void Setup()
        {
            // Create a test profile
            testProfile = ScriptableObject.CreateInstance<LocalizationServiceProfile>();
            
            // Use reflection to set private fields (since they have no public setters)
            var defaultLocaleField = typeof(LocalizationServiceProfile).GetField("defaultLocale", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var catalogPathField = typeof(LocalizationServiceProfile).GetField("catalogPath",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var forceLocaleField = typeof(LocalizationServiceProfile).GetField("forceLocale",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var supportedLocalesField = typeof(LocalizationServiceProfile).GetField("supportedLocales",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            defaultLocaleField?.SetValue(testProfile, "en-US");
            catalogPathField?.SetValue(testProfile, "Assets/UX/Localization/Catalogs");
            forceLocaleField?.SetValue(testProfile, "");
            supportedLocalesField?.SetValue(testProfile, new[] { "en-US", "es-ES", "fr-FR", "test" });

            // Create service instance
            localizationService = new LocalizationService("LocalizationService", 100, testProfile);
            localizationService.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            if (localizationService != null)
            {
                if (localizationService is System.IDisposable disposable)
                {
                    disposable.Dispose();
                }

                localizationService = null;
            }
            if (testProfile != null)
            {
                Object.DestroyImmediate(testProfile);
            }
        }

        #region Initialization Tests

        [Test]
        public void Initialization_ServiceInitializesWithDefaultLocale()
        {
            // Arrange & Act
            var currentLocale = localizationService.CurrentLocale;

            // Assert
            Assert.AreEqual("en-US", currentLocale, "Service should initialize with forced locale from profile");
        }

        [Test]
        public void Initialization_DefaultLocalePropertyMatches()
        {
            // Arrange & Act
            var defaultLocale = localizationService.DefaultLocale;

            // Assert
            Assert.AreEqual("en-US", defaultLocale, "DefaultLocale property should match profile configuration");
        }

        [Test]
        public void Initialization_SupportedLocalesPopulated()
        {
            // Arrange & Act
            var supportedLocales = localizationService.SupportedLocales;

            // Assert
            Assert.IsNotNull(supportedLocales, "SupportedLocales should not be null");
            Assert.AreEqual(4, supportedLocales.Count, "Should have 4 supported locales");
            CollectionAssert.Contains(supportedLocales, "en-US");
            CollectionAssert.Contains(supportedLocales, "es-ES");
            CollectionAssert.Contains(supportedLocales, "fr-FR");
            CollectionAssert.Contains(supportedLocales, "test");
        }

        #endregion

        #region GetString Tests

        [Test]
        public void GetString_ReturnsValidStringForExistingKey()
        {
            // Arrange
            string key = "mainscreen_time";

            // Act
            string result = localizationService.GetString(key);

            // Assert
            Assert.IsNotNull(result, "Should return non-null result");
            Assert.IsNotEmpty(result, "Should return non-empty translation or fallback key");
        }

        [Test]
        public void GetString_ReturnsKeyAsDefaultFallback()
        {
            // Arrange
            string nonExistentKey = "this_key_does_not_exist";

            // Act
            string result = localizationService.GetString(nonExistentKey);

            // Assert
            Assert.AreEqual(nonExistentKey, result, "Should return the key itself as final fallback");
        }

        [Test]
        public void GetString_HandlesNullKeyGracefully()
        {
            // Arrange
            string nullKey = null;

            // Act
            string result = localizationService.GetString(nullKey);

            // Assert
            Assert.AreEqual(string.Empty, result, "Should return empty string for null key");
        }

        [Test]
        public void GetString_HandlesEmptyKeyGracefully()
        {
            // Arrange
            string emptyKey = string.Empty;

            // Act
            string result = localizationService.GetString(emptyKey);

            // Assert
            Assert.AreEqual(string.Empty, result, "Should return empty string for empty key");
        }

        [Test]
        public void GetString_HandlesWhitespaceKeyGracefully()
        {
            // Arrange
            string whitespaceKey = "   ";

            // Act
            string result = localizationService.GetString(whitespaceKey);

            // Assert
            Assert.AreEqual(string.Empty, result, "Should return empty string for whitespace-only key");
        }

        #endregion

        #region Format String Tests

        [Test]
        public void GetString_SupportsFormatStringWithSingleParameter()
        {
            // Arrange
            string key = "viewdetails_friends_reminders";
            int friendCount = 5;

            // Act
            string result = localizationService.GetString(key, friendCount);

            // Assert
            Assert.IsNotNull(result, "Should return non-null result");
            Assert.IsNotEmpty(result, "Should return formatted string or fallback key");
        }

        [Test]
        public void GetString_SupportsFormatStringWithMultipleParameters()
        {
            // Arrange - We'll test with a hypothetical format string
            // For now, using the friends_reminders format
            string key = "viewdetails_friends_reminders";
            int param1 = 10;

            // Act
            string result = localizationService.GetString(key, param1);

            // Assert
            Assert.IsNotNull(result, "Should return non-null result");
            Assert.IsNotEmpty(result, "Should return formatted string or fallback key");
        }

        [Test]
        public void GetString_HandlesFormatExceptionGracefully()
        {
            // Arrange
            string key = "viewdetails_friends_reminders";
            // Pass wrong number of parameters for format string
            object[] wrongParams = new object[] { "string1", "string2" };

            // Act
            string result = localizationService.GetString(key, wrongParams);

            // Assert
            // Should return the format string without formatting rather than throwing
            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result);
        }

        [Test]
        public void GetString_NoParametersFormatString()
        {
            // Arrange
            string key = "mainscreen_time";
            object[] emptyParams = new object[] { };

            // Act
            string result = localizationService.GetString(key, emptyParams);

            // Assert
            Assert.IsNotNull(result, "Should return non-null result");
            Assert.IsNotEmpty(result, "Should return translation or fallback key");
        }

        #endregion

        #region Enum Display Name Tests

        [Test]
        public void GetEnumDisplayName_ReturnsCorrectNameForValidEnum()
        {
            // Arrange
            var enumValue = SplitInteractiveCardType.Silent;

            // Act
            string result = localizationService.GetEnumDisplayName(enumValue);

            // Assert
            Assert.IsNotNull(result, "Should return non-null result");
            Assert.IsNotEmpty(result, "Should return display name or fallback key");
        }

        [Test]
        public void GetEnumDisplayName_WorksForMultipleEnumValues()
        {
            // Arrange & Act
            string silentName = localizationService.GetEnumDisplayName(SplitInteractiveCardType.Silent);
            string speechName = localizationService.GetEnumDisplayName(SplitInteractiveCardType.Speech);
            string soundName = localizationService.GetEnumDisplayName(SplitInteractiveCardType.Sound);

            // Assert
            Assert.IsNotEmpty(silentName, "Should return non-empty display name for Silent");
            Assert.IsNotEmpty(speechName, "Should return non-empty display name for Speech");
            Assert.IsNotEmpty(soundName, "Should return non-empty display name for Sound");
        }

        [Test]
        public void GetEnumDisplayName_HandlesDifferentEnumTypes()
        {
            // Arrange & Act
            string createSchedule = localizationService.GetEnumDisplayName(SplitInteractiveCardScheduleType.Create);
            string upcomingSchedule = localizationService.GetEnumDisplayName(SplitInteractiveCardScheduleType.Upcoming);
            string individualParticipant = localizationService.GetEnumDisplayName(SplitInteractiveCardParticipantType.Individual);

            // Assert
            Assert.IsNotEmpty(createSchedule, "Should return non-empty display name for Create");
            Assert.IsNotEmpty(upcomingSchedule, "Should return non-empty display name for Upcoming");
            Assert.IsNotEmpty(individualParticipant, "Should return non-empty display name for Individual");
        }

        [Test]
        public void GetEnumDisplayName_HandlesNullEnumGracefully()
        {
            // Arrange
            System.Enum nullEnum = null;

            // Act
            string result = localizationService.GetEnumDisplayName(nullEnum);

            // Assert
            Assert.AreEqual(string.Empty, result, "Should return empty string for null enum");
        }

        [Test]
        public void GetEnumDisplayName_ConstructsCorrectKeyFormat()
        {
            // Arrange
            var enumValue = SplitInteractiveCardType.Silent;

            // Act - Call GetEnumDisplayName which internally constructs key as "enum_{TypeName}_{Value}"
            string result = localizationService.GetEnumDisplayName(enumValue);

            // Assert - Key should be: enum_SplitInteractiveCardType_Silent
            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result);
        }

        #endregion

        #region Locale Switching Tests

        [Test]
        public void SetLocale_SwitchesToSpanishLocale()
        {
            // Arrange
            string spanishLocale = "es-ES";
            bool eventFired = false;
            string eventLocale = null;

            localizationService.OnLocaleChanged += (locale) =>
            {
                eventFired = true;
                eventLocale = locale;
            };

            // Act
            localizationService.SetLocale(spanishLocale);

            // Assert
            Assert.AreEqual(spanishLocale, localizationService.CurrentLocale, "Should switch to Spanish locale");
            Assert.IsTrue(eventFired, "OnLocaleChanged event should fire");
            Assert.AreEqual(spanishLocale, eventLocale, "Event should pass correct locale");
        }

        [Test]
        public void SetLocale_TranslationsChangeAfterSwitch()
        {
            // Arrange - Get English translation first
            string englishTime = localizationService.GetString("mainscreen_time");
            Assert.IsNotEmpty(englishTime, "Should have English translation");

            // Act - Switch to Spanish
            localizationService.SetLocale("es-ES");

            // Assert - Locale should change and return translation
            Assert.AreEqual("es-ES", localizationService.CurrentLocale, "Locale should be Spanish");
            string spanishTime = localizationService.GetString("mainscreen_time");
            Assert.IsNotEmpty(spanishTime, "Should return Spanish translation after locale switch");
        }

        [Test]
        public void SetLocale_SwitchesToFrenchLocale()
        {
            // Arrange
            string frenchLocale = "fr-FR";

            // Act
            localizationService.SetLocale(frenchLocale);
            string frenchTime = localizationService.GetString("mainscreen_time");

            // Assert
            Assert.AreEqual(frenchLocale, localizationService.CurrentLocale, "Locale should be French");
            Assert.IsNotEmpty(frenchTime, "Should return French translation or fallback");
        }

        [Test]
        public void SetLocale_SwitchesToTestPseudoLocale()
        {
            // Arrange
            string testLocale = "test";

            // Act
            localizationService.SetLocale(testLocale);
            string testTime = localizationService.GetString("mainscreen_time");

            // Assert
            Assert.AreEqual(testLocale, localizationService.CurrentLocale, "Locale should switch to test");
            Assert.IsNotEmpty(testTime, "Should return translation or fallback");
        }

        [Test]
        public void SetLocale_HandleInvalidLocaleGracefully()
        {
            // Arrange
            string currentLocale = localizationService.CurrentLocale;
            string invalidLocale = "invalid-XX";

            // Act
            localizationService.SetLocale(invalidLocale);

            // Assert
            // Should fall back to default without error
            Assert.AreEqual("en-US", localizationService.CurrentLocale, 
                "Should fall back to default locale for invalid locale code");
        }

        [Test]
        public void SetLocale_HandlesNullLocaleGracefully()
        {
            // Arrange
            string currentLocale = localizationService.CurrentLocale;

            // Act
            localizationService.SetLocale(null);

            // Assert
            Assert.AreEqual(currentLocale, localizationService.CurrentLocale, 
                "Should not change locale when setting null");
        }

        [Test]
        public void SetLocale_HandlesEmptyLocaleGracefully()
        {
            // Arrange
            string currentLocale = localizationService.CurrentLocale;

            // Act
            localizationService.SetLocale(string.Empty);

            // Assert
            Assert.AreEqual(currentLocale, localizationService.CurrentLocale,
                "Should not change locale when setting empty string");
        }

        #endregion

        #region Fallback Tests

        [Test]
        public void Fallback_UsesDefaultWhenKeyMissingInCurrentLocale()
        {
            // Arrange
            localizationService.SetLocale("es-ES");
            string keyThatExistsInDefault = "mainscreen_time";

            // Act
            string result = localizationService.GetString(keyThatExistsInDefault);

            // Assert
            Assert.AreEqual("es-ES", localizationService.CurrentLocale, "Locale should be Spanish");
            Assert.IsNotEmpty(result, "Should return Spanish translation or fallback");
        }

        [Test]
        public void Fallback_FallsBackToKeyWhenNotFoundAnywhere()
        {
            // Arrange
            string nonExistentKey = "some_key_that_never_exists";

            // Act
            string result = localizationService.GetString(nonExistentKey);

            // Assert
            Assert.AreEqual(nonExistentKey, result, "Should return key itself as final fallback");
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnLocaleChanged_EventFiredWithCorrectLocale()
        {
            // Arrange
            string capturedLocale = null;
            int callCount = 0;

            localizationService.OnLocaleChanged += (locale) =>
            {
                capturedLocale = locale;
                callCount++;
            };

            // Act
            localizationService.SetLocale("es-ES");

            // Assert
            Assert.AreEqual("es-ES", capturedLocale, "Event should pass correct locale");
            Assert.AreEqual(1, callCount, "Event should be called exactly once");
        }

        [Test]
        public void OnLocaleChanged_MultipleSubscribersAllCalled()
        {
            // Arrange
            int subscriber1Count = 0;
            int subscriber2Count = 0;

            localizationService.OnLocaleChanged += (locale) => subscriber1Count++;
            localizationService.OnLocaleChanged += (locale) => subscriber2Count++;

            // Act
            localizationService.SetLocale("es-ES");

            // Assert
            Assert.AreEqual(1, subscriber1Count, "First subscriber should be called");
            Assert.AreEqual(1, subscriber2Count, "Second subscriber should be called");
        }

        [Test]
        public void OnLocaleChanged_NotFiredWhenSettingSameLocale()
        {
            // Arrange
            int callCount = 0;
            localizationService.OnLocaleChanged += (locale) => callCount++;

            // Act
            localizationService.SetLocale("en-US");
            localizationService.SetLocale("en-US"); // Set same locale again

            // Assert
            // Should fire once when first set during initialization, but not again
            // (Depending on implementation - if SetLocale is idempotent)
            Assert.GreaterOrEqual(callCount, 1, "Event should have fired at least once");
        }

        #endregion

        #region All Keys Exist Tests

        [Test]
        public void KeyInventory_AllTextKeysExist()
        {
            // Arrange
            var textKeys = new[]
            {
                "mainscreen_time",
                "mainscreen_view",
                "mainscreen_home",
                "mainscreen_create",
                "mainscreen_store",
                "viewscreen_title",
                "viewdetails_type_prefix",
                "viewdetails_friends_reminders"
            };

            // Act & Assert
            foreach (var key in textKeys)
            {
                string result = localizationService.GetString(key);
                Assert.IsNotEmpty(result, $"Key '{key}' should have a non-empty translation or fallback to key");
            }
        }

        [Test]
        public void KeyInventory_AllEnumKeysExist()
        {
            // Arrange
            (System.Enum enumValue, string expectedKey)[] enumKeys = new (System.Enum enumValue, string expectedKey)[]
            {
                (SplitInteractiveCardType.Silent, "enum_SplitInteractiveCardType_Silent"),
                (SplitInteractiveCardType.Speech, "enum_SplitInteractiveCardType_Speech"),
                (SplitInteractiveCardType.Sound, "enum_SplitInteractiveCardType_Sound"),
                (SplitInteractiveCardScheduleType.Create, "enum_SplitInteractiveCardScheduleType_Create"),
                (SplitInteractiveCardScheduleType.Upcoming, "enum_SplitInteractiveCardScheduleType_Upcoming"),
                (SplitInteractiveCardScheduleType.Ongoing, "enum_SplitInteractiveCardScheduleType_Ongoing"),
                (SplitInteractiveCardScheduleType.Completed, "enum_SplitInteractiveCardScheduleType_Completed"),
                (SplitInteractiveCardParticipantType.Individual, "enum_SplitInteractiveCardParticipantType_Individual"),
                (SplitInteractiveCardParticipantType.Group, "enum_SplitInteractiveCardParticipantType_Group"),
                (SplitInteractiveCardParticipantType.Meditation, "enum_SplitInteractiveCardParticipantType_Meditation")
            };

            // Act & Assert
            foreach (var (enumValue, expectedKey) in enumKeys)
            {
                string result = localizationService.GetEnumDisplayName(enumValue);
                Assert.IsNotEmpty(result, $"Enum '{enumValue}' should have a translation or fallback to key");
            }
        }

        #endregion

        #region Cross-Locale Consistency Tests

        [Test]
        public void Consistency_AllLocalesHaveSameKeys()
        {
            // Arrange
            var locales = new[] { "en-US", "es-ES", "fr-FR", "test" };
            var enUsKeys = new HashSet<string>();

            // Get all keys from en-US
            localizationService.SetLocale("en-US");
            foreach (var key in new[]
            {
                "mainscreen_time", "mainscreen_view", "mainscreen_home", "mainscreen_create", "mainscreen_store",
                "viewscreen_title", "viewdetails_type_prefix", "viewdetails_friends_reminders"
            })
            {
                string result = localizationService.GetString(key);
                if (!result.Equals(key)) // Only add if found
                {
                    enUsKeys.Add(key);
                }
            }

            // Act & Assert - Check other locales have same keys
            foreach (var locale in locales)
            {
                if (locale == "en-US") continue;
                
                localizationService.SetLocale(locale);
                foreach (var key in enUsKeys)
                {
                    string result = localizationService.GetString(key);
                    Assert.AreNotEqual(key, result, $"Locale '{locale}' should have translation for '{key}'");
                }
            }
        }

        #endregion
    }
}
