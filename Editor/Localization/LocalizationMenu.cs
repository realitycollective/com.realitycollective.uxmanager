// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.UXManager.Profiles.Localization;
using UnityEditor;
using UnityEngine;

namespace RealityCollective.UXManager.Editor.Localization
{
    /// <summary>
    /// Menu items for quick localization setup and management.
    /// </summary>
    public static class LocalizationMenu
    {
        private const string MenuPrefix = "Tools/Reality Collective/UX Manager/Localization/";

        [MenuItem(MenuPrefix + "Import Keys from Generated C# File")]
        public static void ImportKeysFromGeneratedFile()
        {
            var profiles = Resources.FindObjectsOfTypeAll<LocalizationServiceProfile>();
            
            if (profiles.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Profile Found",
                    "Please create a LocalizationServiceProfile first:\n\n" +
                    "Right-click in project > Create > Reality Collective > UX Manager > Localization Service Profile",
                    "OK");
                return;
            }

            if (profiles.Length == 1)
            {
                LocalizationServiceProfileEditor.ImportKeysFromGeneratedFileStatic(profiles[0], replaceAll: false);
                return;
            }

            // Multiple profiles found - show selection dialog
            var options = new string[profiles.Length];
            for (int i = 0; i < profiles.Length; i++)
            {
                options[i] = profiles[i].name;
            }

            int selected = EditorUtility.DisplayDialogComplex(
                "Multiple Profiles Found",
                $"Found {profiles.Length} LocalizationServiceProfile assets. Which one should be updated?",
                "OK",
                "Cancel",
                "");

            if (selected == 0)
            {
                EditorGUILayout.Popup(0, options);
                LocalizationServiceProfileEditor.ImportKeysFromGeneratedFileStatic(profiles[0], replaceAll: false);
            }
        }

        [MenuItem(MenuPrefix + "Replace All Keys from Generated C# File")]
        public static void ReplaceAllKeysFromGeneratedFile()
        {
            var profiles = Resources.FindObjectsOfTypeAll<LocalizationServiceProfile>();
            
            if (profiles.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Profile Found",
                    "Please create a LocalizationServiceProfile first.",
                    "OK");
                return;
            }

            if (profiles.Length == 1)
            {
                int confirm = EditorUtility.DisplayDialogComplex(
                    "Replace All Keys?",
                    "This will clear all existing keys and replace them with keys from the generated C# file.",
                    "Replace",
                    "Cancel",
                    "");

                if (confirm == 0)
                {
                    LocalizationServiceProfileEditor.ImportKeysFromGeneratedFileStatic(profiles[0], replaceAll: true);
                }
                return;
            }

            EditorUtility.DisplayDialog(
                "Multiple Profiles",
                $"Found {profiles.Length} profiles. Please use the first menu item to select which profile to update.",
                "OK");
        }
    }
}
