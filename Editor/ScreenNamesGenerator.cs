// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RealityCollective.UXManager.Editor
{
    [InitializeOnLoad]
    internal static class ScreenNamesGenerator
    {
        private const string DefaultGeneratedPath = "Assets/UX/Generated/ScreenNames.g.cs";
        private const string GeneratedFileName = "ScreenNames.g.cs";

        static ScreenNamesGenerator()
        {
            // Defer to avoid import-time ordering issues.
            EditorApplication.delayCall += Generate;
        }

        internal static void Generate()
        {
            try
            {
                var keys = CollectKeysFromProfiles();
                var generatedAssetPath = DetermineGeneratedAssetPath();
                WriteGeneratedFile(generatedAssetPath, keys);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UXManager] ScreenNames generation failed: {ex}");
            }
        }

        internal static void GenerateForProfile(UnityEngine.Object profileAsset)
        {
            try
            {
                var preferredProfilePath = profileAsset != null ? AssetDatabase.GetAssetPath(profileAsset) : null;
                var keys = CollectKeysFromProfiles();
                var generatedAssetPath = DetermineGeneratedAssetPath(preferredProfilePath);
                WriteGeneratedFile(generatedAssetPath, keys);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UXManager] ScreenNames generation failed: {ex}");
            }
        }

        private static string DetermineGeneratedAssetPath(string preferredProfilePath = null)
        {
            if (!string.IsNullOrWhiteSpace(preferredProfilePath) && preferredProfilePath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                var preferredFolder = Path.GetDirectoryName(preferredProfilePath)?.Replace('\\', '/');
                if (!string.IsNullOrWhiteSpace(preferredFolder))
                {
                    return $"{preferredFolder}/{GeneratedFileName}";
                }
            }

            // Prefer generating next to the first UXScreenManagerProfile found (in the client project).
            var guids = AssetDatabase.FindAssets("t:UXScreenManagerProfile");
            if (guids != null && guids.Length > 0)
            {
                var firstProfilePath = guids
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .OrderBy(p => p, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(firstProfilePath) && firstProfilePath.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    var folder = Path.GetDirectoryName(firstProfilePath)?.Replace('\\', '/');
                    if (!string.IsNullOrWhiteSpace(folder))
                    {
                        return $"{folder}/{GeneratedFileName}";
                    }
                }
            }

            // Fallback to a stable, client-project location.
            return DefaultGeneratedPath;
        }

        private static IReadOnlyList<string> CollectKeysFromProfiles()
        {
            var results = new HashSet<string>(StringComparer.Ordinal);
            results.Add("None");

            // Use type-name string so this generator does not require compile-time references
            // to UXScreenManagerProfile (and its base classes) in the Editor assembly.
            var guids = AssetDatabase.FindAssets("t:UXScreenManagerProfile");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                var profileObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (profileObject == null)
                {
                    continue;
                }

                if (!string.Equals(profileObject.GetType().Name, "UXScreenManagerProfile", StringComparison.Ordinal))
                {
                    continue;
                }

                var serialized = new SerializedObject(profileObject);
                var mappings = serialized.FindProperty("screenKeyMappings");
                if (mappings == null || !mappings.isArray)
                {
                    continue;
                }

                for (var i = 0; i < mappings.arraySize; i++)
                {
                    var element = mappings.GetArrayElementAtIndex(i);
                    var keyProp = element?.FindPropertyRelative("Key");
                    if (keyProp == null)
                    {
                        continue;
                    }

                    var key = keyProp.stringValue;
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    results.Add(key.Trim());
                }
            }

            return results.OrderBy(k => k, StringComparer.Ordinal).ToArray();
        }

        private static void WriteGeneratedFile(string generatedAssetPath, IReadOnlyList<string> keys)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                Debug.LogWarning("[UXManager] Could not determine project root. Skipping ScreenNames generation.");
                return;
            }

            if (string.IsNullOrWhiteSpace(generatedAssetPath) || !generatedAssetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Debug.LogWarning($"[UXManager] ScreenNames generated path must be under Assets/. Got '{generatedAssetPath}'. Skipping generation.");
                return;
            }

            var fullPath = Path.GetFullPath(Path.Combine(projectRoot, generatedAssetPath));
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? projectRoot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UXManager] Could not create directory for '{generatedAssetPath}': {ex}");
                return;
            }

            var content = BuildFileContent(keys);

            var existing = File.Exists(fullPath) ? File.ReadAllText(fullPath, Encoding.UTF8) : null;
            if (string.Equals(existing, content, StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                File.WriteAllText(fullPath, content, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UXManager] Could not write ScreenNames file '{generatedAssetPath}': {ex}");
                return;
            }

            AssetDatabase.ImportAsset(generatedAssetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[UXManager] ScreenNames generated at: {generatedAssetPath}");
        }

        private static string BuildFileContent(IReadOnlyList<string> keys)
        {
            var usedIdentifiers = new HashSet<string>(StringComparer.Ordinal);
            var sb = new StringBuilder();

            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("// Generated by UXManager (Editor-only). Do not edit by hand.");
            sb.AppendLine("// Manages the list of configured screen names from the UXManager profile.");
            sb.AppendLine();
            sb.AppendLine("namespace RealityCollective.UXManager.Services.ScreenManagement");
            sb.AppendLine("{");
            sb.AppendLine("    public static partial class ScreenNames");
            sb.AppendLine("    {");

            foreach (var key in keys)
            {
                var identifier = ToIdentifier(key);
                if (string.IsNullOrWhiteSpace(identifier))
                {
                    continue;
                }

                identifier = MakeUnique(identifier, usedIdentifiers);
                usedIdentifiers.Add(identifier);

                sb.Append("        public const string ");
                sb.Append(identifier);
                sb.Append(" = ");
                sb.Append('"');
                sb.Append(Escape(key));
                sb.AppendLine("\";");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string Escape(string value) =>
            (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static string MakeUnique(string identifier, HashSet<string> used)
        {
            if (!used.Contains(identifier))
            {
                return identifier;
            }

            var i = 2;
            while (used.Contains($"{identifier}_{i}"))
            {
                i++;
            }

            return $"{identifier}_{i}";
        }

        private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
        {
            "class","namespace","public","private","protected","internal","static","partial",
            "string","int","float","double","bool","new","return","void","null","true","false"
        };

        private static string ToIdentifier(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            var trimmed = key.Trim();
            var sb = new StringBuilder(trimmed.Length);

            for (var i = 0; i < trimmed.Length; i++)
            {
                var c = trimmed[i];
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    if (sb.Length == 0 && char.IsDigit(c))
                    {
                        sb.Append('_');
                    }
                    sb.Append(c);
                    continue;
                }

                // Common separators become underscores.
                if (c == ' ' || c == '-' || c == '.')
                {
                    if (sb.Length == 0)
                    {
                        sb.Append('_');
                    }
                    sb.Append('_');
                }
            }

            var identifier = sb.ToString();
            while (identifier.Contains("__", StringComparison.Ordinal))
            {
                identifier = identifier.Replace("__", "_", StringComparison.Ordinal);
            }

            identifier = identifier.Trim('_');
            if (string.IsNullOrWhiteSpace(identifier))
            {
                identifier = "_";
            }

            if (CSharpKeywords.Contains(identifier))
            {
                identifier = "_" + identifier;
            }

            return identifier;
        }
    }

    internal sealed class ScreenNamesGeneratorPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // If any UXScreenManagerProfile changed, regenerate.
            if (importedAssets.Any(IsProfileAsset) || movedAssets.Any(IsProfileAsset) || movedFromAssetPaths.Any(IsProfileAsset))
            {
                ScreenNamesGenerator.Generate();
            }
        }

        private static bool IsProfileAsset(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            return asset != null && string.Equals(asset.GetType().Name, "UXScreenManagerProfile", StringComparison.Ordinal);
        }
    }
}
#endif
