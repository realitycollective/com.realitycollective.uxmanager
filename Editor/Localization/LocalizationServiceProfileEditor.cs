// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using RealityCollective.ServiceFramework.Editor.Profiles;
using RealityCollective.UXManager.Profiles.Localization;
using RealityCollective.Utilities.Logging;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using RealityCollective.UXManager.Services.Localization;

namespace RealityCollective.UXManager.Editor.Localization
{
    /// <summary>
    /// Custom editor inspector for LocalizationServiceProfile with keys management and validation tools.
    /// </summary>
    [CustomEditor(typeof(LocalizationServiceProfile))]
    [CanEditMultipleObjects]
    public class LocalizationServiceProfileEditor : ServiceProfileInspector
    {
        private ReorderableList reorderableList;
        private SerializedProperty localizationKeysProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            localizationKeysProperty = serializedObject.FindProperty("localizationKeys");
            
            InitializeReorderableList();
        }

        private void InitializeReorderableList()
        {
            reorderableList = new ReorderableList(
                serializedObject,
                localizationKeysProperty,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true);

            reorderableList.drawHeaderCallback = DrawHeader;
            reorderableList.drawElementCallback = DrawElement;
            reorderableList.elementHeightCallback = GetElementHeight;
        }

        private void DrawHeader(Rect rect)
        {
            var keyRect = new Rect(rect.x, rect.y, rect.width * 0.5f - 5, rect.height);
            var displayNameRect = new Rect(rect.x + rect.width * 0.5f + 5, rect.y, rect.width * 0.5f - 5, rect.height);

            EditorGUI.LabelField(keyRect, "Key", EditorStyles.boldLabel);
            EditorGUI.LabelField(displayNameRect, "Display Name", EditorStyles.boldLabel);
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = localizationKeysProperty.GetArrayElementAtIndex(index);
            var keyProperty = element.FindPropertyRelative("key");
            var displayNameProperty = element.FindPropertyRelative("displayName");

            var keyRect = new Rect(rect.x, rect.y + 2, rect.width * 0.5f - 5, EditorGUIUtility.singleLineHeight);
            var displayNameRect = new Rect(rect.x + rect.width * 0.5f + 5, rect.y + 2, rect.width * 0.5f - 5, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(keyRect, keyProperty, GUIContent.none);
            EditorGUI.PropertyField(displayNameRect, displayNameProperty, GUIContent.none);
        }

        private float GetElementHeight(int index)
        {
            return EditorGUIUtility.singleLineHeight + 4;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            DrawKeysManagementSection();
        }

        private void DrawKeysManagementSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Keys Management", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Manage localization keys and validate catalogs. These keys drive the generated C# file and validate all catalogs.", MessageType.Info);

            EditorGUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load Keys from Default Locale", GUILayout.Height(30)))
                {
                    LoadKeysFromDefaultLocale();
                }

                if (GUILayout.Button("Import Keys from Generated C# File", GUILayout.Height(30)))
                {
                    ImportKeysFromGeneratedFile();
                }
            }

            EditorGUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate C# Keys File", GUILayout.Height(30)))
                {
                    GenerateCSharpKeysFile();
                }

                if (GUILayout.Button("Add Keys to Catalogs", GUILayout.Height(30)))
                {
                    AddKeysToCatalogs();
                }
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Validate All Catalogs", GUILayout.Height(30)))
            {
                ValidateAllCatalogs();
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Localization Keys", EditorStyles.boldLabel);
            serializedObject.Update();
            reorderableList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private void GenerateCSharpKeysFile()
        {
            var locProfile = (LocalizationServiceProfile)ThisProfile;
            if (locProfile.LocalizationKeys.Count == 0)
            {
                EditorUtility.DisplayDialog("No Keys", "Please add keys before generating the C# file.", "OK");
                return;
            }

            string generatedCode = GenerateKeysCode();
            string outputPath = "Assets/Scripts/Generated/LocalizationKeys.cs";

            // Create directory if it doesn't exist
            string directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outputPath, generatedCode);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Success",
                $"Generated C# keys file at:\n{outputPath}\n\nKeys count: {locProfile.LocalizationKeys.Count}",
                "OK");

            Debug.Log($"[LocalizationServiceProfile] Generated {locProfile.LocalizationKeys.Count} keys to {outputPath}");
        }

        private string GenerateKeysCode()
        {
            var locProfile = (LocalizationServiceProfile)ThisProfile;
            var sb = new StringBuilder();

            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("// This file is auto-generated by LocalizationKeysGenerator.");
            sb.AppendLine("// Do not modify this file directly.");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine();
            sb.AppendLine("namespace RealityCollective.UXManager.Services.Localization");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Auto-generated localization key constants from the default (en-US) locale catalog.");
            sb.AppendLine("    /// Use these constants with ILocalizationService.GetString() for type-safe localization lookups.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static class LocalizationKeys");
            sb.AppendLine("    {");

            foreach (var key in locProfile.LocalizationKeys.OrderBy(k => k.key))
            {
                if (string.IsNullOrWhiteSpace(key.key))
                {
                    continue;
                }

                // Use displayName as constant name if provided, otherwise convert from key
                string constantName = string.IsNullOrWhiteSpace(key.displayName) 
                    ? MakeValidCSharpIdentifier(key.key)
                    : key.displayName;

                sb.AppendLine($"        /// <summary>{EscapeXmlComment(key.key)}</summary>");
                sb.AppendLine($"        public const string {constantName} = \"{key.key}\";");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string MakeValidCSharpIdentifier(string input)
        {
            // Replace invalid characters with underscores
            var sb = new StringBuilder();
            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }

            string result = sb.ToString();
            
            // Ensure it doesn't start with a digit
            if (result.Length > 0 && char.IsDigit(result[0]))
            {
                result = "_" + result;
            }

            return result;
        }

        private string EscapeXmlComment(string text)
        {
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private void AddKeysToCatalogs()
        {
            var locProfile = (LocalizationServiceProfile)ThisProfile;
            if (locProfile.LocalizationKeys.Count == 0)
            {
                EditorUtility.DisplayDialog("No Keys", "Please add keys before adding to catalogs.", "OK");
                return;
            }

            string catalogPath = Path.Combine(Application.dataPath, "..", locProfile.CatalogPath);

            if (!Directory.Exists(catalogPath))
            {
                EditorUtility.DisplayDialog("Error", $"Catalog path not found: {catalogPath}", "OK");
                return;
            }

            int totalKeysAdded = 0;
            var catalogFiles = Directory.GetFiles(catalogPath, "*.json");

            foreach (var catalogFile in catalogFiles)
            {
                try
                {
                    string json = File.ReadAllText(catalogFile);
                    var catalog = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                    if (catalog == null)
                    {
                        continue;
                    }

                    int keysAddedInFile = 0;
                    var keys = catalog.ContainsKey("keys") ? catalog["keys"] as Dictionary<string, string> : new Dictionary<string, string>();

                    foreach (var key in locProfile.LocalizationKeys)
                    {
                        if (string.IsNullOrWhiteSpace(key.key))
                        {
                            continue;
                        }

                        if (!keys.ContainsKey(key.key))
                        {
                            keys[key.key] = $"[{key.displayName}]";
                            keysAddedInFile++;
                        }
                    }

                    if (keysAddedInFile > 0)
                    {
                        catalog["keys"] = keys;
                        var settings = new JsonSerializerSettings { Formatting = Formatting.Indented };
                        File.WriteAllText(catalogFile, JsonConvert.SerializeObject(catalog, settings));
                        totalKeysAdded += keysAddedInFile;

                        Debug.Log($"[LocalizationServiceProfile] Added {keysAddedInFile} keys to {Path.GetFileName(catalogFile)}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[LocalizationServiceProfile] Error processing catalog {catalogFile}: {ex.Message}");
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Complete",
                $"Added {totalKeysAdded} keys to catalogs.\nCatalogs processed: {catalogFiles.Length}",
                "OK");
        }

        private void ValidateAllCatalogs()
        {
            var locProfile = (LocalizationServiceProfile)ThisProfile;
            if (locProfile.LocalizationKeys.Count == 0)
            {
                EditorUtility.DisplayDialog("No Keys", "Please add keys to validate against.", "OK");
                return;
            }

            string catalogPath = Path.Combine(Application.dataPath, "..", locProfile.CatalogPath);

            if (!Directory.Exists(catalogPath))
            {
                EditorUtility.DisplayDialog("Error", $"Catalog path not found: {catalogPath}", "OK");
                return;
            }

            var validationResults = new List<string>();
            var catalogFiles = Directory.GetFiles(catalogPath, "*.json");
            var profileKeys = new HashSet<string>(locProfile.LocalizationKeys.Where(k => !string.IsNullOrWhiteSpace(k.key)).Select(k => k.key));

            int catalogsProcessed = 0;
            int totalErrors = 0;

            foreach (var catalogFile in catalogFiles)
            {
                try
                {
                    string json = File.ReadAllText(catalogFile);
                    var catalog = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                    if (catalog == null)
                    {
                        continue;
                    }

                    var keys = catalog.ContainsKey("keys") ? catalog["keys"] as Dictionary<string, string> : new Dictionary<string, string>();
                    string catalogName = Path.GetFileNameWithoutExtension(catalogFile);
                    var missingKeys = profileKeys.Where(k => !keys.ContainsKey(k)).ToList();
                    var extraKeys = keys.Keys.Where(k => !profileKeys.Contains(k)).ToList();

                    if (missingKeys.Count > 0 || extraKeys.Count > 0)
                    {
                        validationResults.Add($"\n📄 {catalogName}.json:");
                        
                        if (missingKeys.Count > 0)
                        {
                            validationResults.Add($"  ❌ Missing {missingKeys.Count} keys:");
                            foreach (var key in missingKeys.Take(5))
                            {
                                validationResults.Add($"     - {key}");
                            }
                            if (missingKeys.Count > 5)
                            {
                                validationResults.Add($"     ... and {missingKeys.Count - 5} more");
                            }
                            totalErrors += missingKeys.Count;
                        }

                        if (extraKeys.Count > 0)
                        {
                            validationResults.Add($"  ⚠️  Extra {extraKeys.Count} keys not in profile:");
                            foreach (var key in extraKeys.Take(5))
                            {
                                validationResults.Add($"     - {key}");
                            }
                            if (extraKeys.Count > 5)
                            {
                                validationResults.Add($"     ... and {extraKeys.Count - 5} more");
                            }
                        }
                    }
                    else
                    {
                        validationResults.Add($"\n✅ {catalogName}.json: OK ({keys.Count} keys)");
                    }

                    catalogsProcessed++;
                }
                catch (Exception ex)
                {
                    validationResults.Add($"\n❌ Error reading {Path.GetFileName(catalogFile)}: {ex.Message}");
                    totalErrors++;
                }
            }

            string resultMessage = $"Validation Results ({catalogsProcessed} catalogs):\n";
            resultMessage += string.Join("\n", validationResults);

            if (totalErrors == 0)
            {
                EditorUtility.DisplayDialog("Validation Successful", "✅ All catalogs are valid!\n" + resultMessage, "OK");
                Debug.Log("[LocalizationServiceProfile] All catalogs passed validation!");
            }
            else
            {
                EditorUtility.DisplayDialog("Validation Failed", "❌ Issues found:\n" + resultMessage, "OK");
                Debug.LogWarning("[LocalizationServiceProfile] Validation found issues. See details above.");
            }
        }

        /// <summary>
        /// Loads localization keys from the default locale catalog file.
        /// This populates the LocalizationKeys list with all keys found in the default catalog.
        /// Useful for bootstrapping key management from existing translations.
        /// </summary>
        public void LoadKeysFromDefaultLocale()
        {
            var locProfile = (LocalizationServiceProfile)ThisProfile;
            LoadKeysFromLocale(locProfile.DefaultLocale);
        }

        /// <summary>
        /// Imports keys from an existing generated C# file (LocalizationKeys.g.cs or LocalizationKeys.cs).
        /// Parses the constant definitions and uses the constant names as display names and values as keys.
        /// This preserves existing implementations while establishing the profile as the source of truth.
        /// </summary>
        public void ImportKeysFromGeneratedFile()
        {
            var locProfile = (LocalizationServiceProfile)ThisProfile;
            string[] searchPaths = new[]
            {
                "Assets/Scripts/Generated/LocalizationKeys.g.cs",
                "Assets/Scripts/Generated/LocalizationKeys.cs",
                "Assets/ServiceProvidersProfile/LocalizationKeys.g.cs",
                "Assets/ServiceProvidersProfile/LocalizationKeys.cs",
            };

            string filePath = null;
            foreach (var path in searchPaths)
            {
                string fullPath = Path.Combine(Application.dataPath, "..", path);
                if (File.Exists(fullPath))
                {
                    filePath = fullPath;
                    break;
                }
            }

            if (string.IsNullOrEmpty(filePath))
            {
                EditorUtility.DisplayDialog(
                    "File Not Found",
                    "Could not find LocalizationKeys.g.cs or LocalizationKeys.cs in:\n" +
                    string.Join("\n", searchPaths),
                    "OK");
                return;
            }

            try
            {
                string content = File.ReadAllText(filePath);
                var keys = ParseLocalizationKeysFromCSharp(content);

                if (keys.Count == 0)
                {
                    EditorUtility.DisplayDialog("No Keys Found", "Could not parse any keys from the C# file.", "OK");
                    return;
                }

                string message = $"Import {keys.Count} keys from:\n{Path.GetFileName(filePath)}\n\n";
                message += "This will add any new keys not already in the profile.\n";
                message += "Existing keys will be preserved.";

                int dialogResult = EditorUtility.DisplayDialogComplex(
                    "Import Keys from C# File",
                    message,
                    "Import",     // 0
                    "Cancel",     // 1
                    "Replace All" // 2
                );

                if (dialogResult == 1) // Cancel
                {
                    return;
                }

                var existingKeySet = new HashSet<string>(locProfile.LocalizationKeys.Select(k => k.key));

                if (dialogResult == 2) // Replace All
                {
                    locProfile.LocalizationKeys.Clear();
                    existingKeySet.Clear();
                }

                int keysAdded = 0;
                foreach (var kvp in keys.OrderBy(k => k.Key))
                {
                    if (!existingKeySet.Contains(kvp.Value))
                    {
                        // displayName is the constant name (e.g., "CirclecardDurationPlural")
                        // key is the actual localization key (e.g., "circlecard_duration_plural")
                        locProfile.LocalizationKeys.Add(new LocalizationKey(kvp.Value, kvp.Key));
                        existingKeySet.Add(kvp.Value);
                        keysAdded++;
                    }
                }

                EditorUtility.SetDirty(locProfile);
                AssetDatabase.SaveAssets();

                EditorUtility.DisplayDialog(
                    "Success",
                    $"Imported keys from C# file.\n\nKeys added: {keysAdded}\nTotal keys: {locProfile.LocalizationKeys.Count}",
                    "OK");

                Debug.Log($"[LocalizationServiceProfile] Imported {keysAdded} keys from C# file. Total: {locProfile.LocalizationKeys.Count}");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to import keys from C# file:\n{ex.Message}", "OK");
                Debug.LogError($"[LocalizationServiceProfile] Error importing keys: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses a C# file containing localization key constants and extracts key/display name pairs.
        /// Expected format: public const string ConstantName = "key_value";
        /// Returns a dictionary where key is the constant name and value is the key value.
        /// </summary>
        private Dictionary<string, string> ParseLocalizationKeysFromCSharp(string content)
        {
            var result = new Dictionary<string, string>();
            
            // Pattern to match: public const string ConstantName = "key_value";
            var regex = new System.Text.RegularExpressions.Regex(
                @"public\s+const\s+string\s+(\w+)\s*=\s*""([^""]+)""",
                System.Text.RegularExpressions.RegexOptions.Multiline);

            var matches = regex.Matches(content);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count == 3)
                {
                    string constantName = match.Groups[1].Value;  // e.g., "CirclecardDurationPlural"
                    string keyValue = match.Groups[2].Value;      // e.g., "circlecard_duration_plural"
                    
                    result[constantName] = keyValue;
                }
            }

            return result;
        }

        /// <summary>
        /// Loads localization keys from a specific locale catalog file.
        /// Can be used programmatically by agents to bootstrap key management.
        /// </summary>
        /// <param name="localeCode">The locale code to load keys from (e.g., 'en-US')</param>
        public void LoadKeysFromLocale(string localeCode)
        {
            var locProfile = (LocalizationServiceProfile)ThisProfile;
            if (string.IsNullOrWhiteSpace(localeCode))
            {
                EditorUtility.DisplayDialog("Error", "Locale code cannot be empty.", "OK");
                return;
            }

            string catalogPath = Path.Combine(Application.dataPath, "..", locProfile.CatalogPath);
            string catalogFile = Path.Combine(catalogPath, $"{localeCode}.json");

            if (!File.Exists(catalogFile))
            {
                EditorUtility.DisplayDialog("Error", $"Catalog file not found:\n{catalogFile}", "OK");
                Debug.LogError($"[LocalizationServiceProfile] Catalog file not found: {catalogFile}");
                return;
            }

            try
            {
                string json = File.ReadAllText(catalogFile);
                var catalog = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                if (catalog == null || !catalog.ContainsKey("keys"))
                {
                    EditorUtility.DisplayDialog("Error", "Invalid catalog format or no 'keys' section found.", "OK");
                    return;
                }

                var catalogKeys = catalog["keys"] as Dictionary<string, string>;
                if (catalogKeys == null || catalogKeys.Count == 0)
                {
                    EditorUtility.DisplayDialog("No Keys", "The catalog file contains no keys.", "OK");
                    return;
                }

                // Show confirmation dialog
                string message = $"Load {catalogKeys.Count} keys from '{localeCode}' locale?\n\n";
                message += "This will add any new keys not already in the profile.\n";
                message += "Existing keys will be preserved.";

                int dialogResult = EditorUtility.DisplayDialogComplex(
                    "Load Keys from Catalog",
                    message,
                    "Load",      // 0
                    "Cancel",    // 1
                    "Replace All" // 2
                );

                if (dialogResult == 1) // Cancel
                {
                    return;
                }

                var existingKeySet = new HashSet<string>(locProfile.LocalizationKeys.Select(k => k.key));

                if (dialogResult == 2) // Replace All
                {
                    locProfile.LocalizationKeys.Clear();
                    existingKeySet.Clear();
                }

                int keysAdded = 0;
                foreach (var kvp in catalogKeys.OrderBy(k => k.Key))
                {
                    if (!existingKeySet.Contains(kvp.Key))
                    {
                        locProfile.LocalizationKeys.Add(new LocalizationKey(kvp.Key, ""));
                        existingKeySet.Add(kvp.Key);
                        keysAdded++;
                    }
                }

                EditorUtility.SetDirty(locProfile);
                AssetDatabase.SaveAssets();

                EditorUtility.DisplayDialog(
                    "Success",
                    $"Loaded keys from '{localeCode}' locale.\n\nKeys added: {keysAdded}\nTotal keys: {locProfile.LocalizationKeys.Count}",
                    "OK");

                Debug.Log($"[LocalizationServiceProfile] Loaded {keysAdded} keys from '{localeCode}'. Total: {locProfile.LocalizationKeys.Count}");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to load keys from catalog:\n{ex.Message}", "OK");
                Debug.LogError($"[LocalizationServiceProfile] Error loading keys from '{localeCode}': {ex.Message}");
            }
        }

        /// <summary>
        /// Public static utility method to import keys from generated C# file into a profile.
        /// Can be used by agents or batch tools to programmatically populate profiles.
        /// </summary>
        /// <summary>
        /// Public static utility method to import keys from generated C# file into a profile.
        /// Can be used by agents or batch tools to programmatically populate profiles.
        /// </summary>
        public static void ImportKeysFromGeneratedFileStatic(LocalizationServiceProfile targetProfile, bool replaceAll = false)
        {
            if (targetProfile == null)
            {
                Debug.LogError("[LocalizationServiceProfile] Target profile is null");
                return;
            }

            string[] searchPaths = new[]
            {
                "Assets/Scripts/Generated/LocalizationKeys.g.cs",
                "Assets/Scripts/Generated/LocalizationKeys.cs",
                "Assets/ServiceProvidersProfile/LocalizationKeys.g.cs",
                "Assets/ServiceProvidersProfile/LocalizationKeys.cs",
            };

            string filePath = null;
            foreach (var path in searchPaths)
            {
                string fullPath = Path.Combine(Application.dataPath, "..", path);
                if (File.Exists(fullPath))
                {
                    filePath = fullPath;
                    break;
                }
            }

            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError($"[LocalizationServiceProfile] Could not find LocalizationKeys.g.cs or LocalizationKeys.cs");
                return;
            }

            try
            {
                string content = File.ReadAllText(filePath);
                var keys = ParseLocalizationKeysFromCSharpStatic(content);

                if (keys.Count == 0)
                {
                    Debug.LogWarning("[LocalizationServiceProfile] No keys found in C# file");
                    return;
                }

                var existingKeySet = new HashSet<string>(targetProfile.LocalizationKeys.Select(k => k.key));

                if (replaceAll)
                {
                    targetProfile.LocalizationKeys.Clear();
                    existingKeySet.Clear();
                }

                int keysAdded = 0;
                foreach (var kvp in keys.OrderBy(k => k.Key))
                {
                    if (!existingKeySet.Contains(kvp.Value))
                    {
                        targetProfile.LocalizationKeys.Add(new LocalizationKey(kvp.Value, kvp.Key));
                        existingKeySet.Add(kvp.Value);
                        keysAdded++;
                    }
                }

                EditorUtility.SetDirty(targetProfile);
                AssetDatabase.SaveAssets();

                Debug.Log($"[LocalizationServiceProfile] Imported {keysAdded} keys from C# file. Total: {targetProfile.LocalizationKeys.Count}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizationServiceProfile] Error importing keys: {ex.Message}");
            }
        }

        /// <summary>
        /// Static version of the C# parser for use in batch operations.
        /// </summary>
        private static Dictionary<string, string> ParseLocalizationKeysFromCSharpStatic(string content)
        {
            var result = new Dictionary<string, string>();
            
            var regex = new System.Text.RegularExpressions.Regex(
                @"public\s+const\s+string\s+(\w+)\s*=\s*""([^""]+)""",
                System.Text.RegularExpressions.RegexOptions.Multiline);

            var matches = regex.Matches(content);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count == 3)
                {
                    string constantName = match.Groups[1].Value;
                    string keyValue = match.Groups[2].Value;
                    result[constantName] = keyValue;
                }
            }

            return result;
        }
    }
}
#endif
