// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_EDITOR

using RealityCollective.ServiceFramework.Services;
using RealityCollective.UXManager.Interfaces.ScreenManagement;
using RealityCollective.UXManager.Profiles.ScreenManagement;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealityCollective.UXManager.Editor.UXManager
{
    /// <summary>
    /// Provides a context menu option to add configured screens as child GameObjects.
    /// </summary>
    public static class AddScreenContextMenu
    {
        private const string MenuPath = "GameObject/UX Manager/Add Screen";

        [MenuItem(MenuPath, priority = 10, validate = true)]
        public static bool ValidateAddScreen(MenuCommand menuCommand)
        {
            // Always show the menu item, validation is done in the action
            return Selection.activeGameObject != null;
        }

        [MenuItem(MenuPath, priority = 10)]
        public static void AddScreen(MenuCommand menuCommand)
        {
            // Get the selected GameObject
            GameObject selectedObject = menuCommand.context as GameObject;
            
            if (selectedObject == null)
            {
                EditorUtility.DisplayDialog("Error", "No GameObject selected.", "OK");
                return;
            }

            // Try to get the UXScreenManager and its profile
            if (!TryGetUXScreenManager(out var uxScreenManager, out var profile))
            {
                EditorUtility.DisplayDialog(
                    "UX Manager Configuration",
                    "UX Manager is not configured or has no screens.",
                    "OK");
                return;
            }

            // Get available screen names
            var screenKeyMappings = profile.ScreenKeyMappings;
            if (screenKeyMappings == null || screenKeyMappings.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "UX Manager Configuration",
                    "UX Manager is not configured or has no screens.",
                    "OK");
                return;
            }

            // Build list of screen names
            var screenNames = new List<string>();
            foreach (var mapping in screenKeyMappings)
            {
                if (!string.IsNullOrWhiteSpace(mapping.ScreenId))
                {
                    screenNames.Add(mapping.ScreenId);
                }
            }

            if (screenNames.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "UX Manager Configuration",
                    "UX Manager is not configured or has no screens.",
                    "OK");
                return;
            }

            // Open the screen selection window
            ScreenSelectionWindow.ShowWindow(selectedObject, screenNames);
        }

        private static bool TryGetUXScreenManager(out IUXScreenManager uxScreenManager, out UXScreenManagerProfile profile)
        {
            uxScreenManager = null;
            profile = null;

            if (ServiceManager.Instance == null)
            {
                return false;
            }

            uxScreenManager = ServiceManager.Instance.GetService<IUXScreenManager>();
            if (uxScreenManager == null)
            {
                return false;
            }

            var serviceType = uxScreenManager.GetType();
            var profileField = serviceType.GetField("profile", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);

            if (profileField != null)
            {
                profile = profileField.GetValue(uxScreenManager) as UXScreenManagerProfile;
            }

            if (profile == null)
            {
                profileField = serviceType.GetField("m_profile", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                
                if (profileField != null)
                {
                    profile = profileField.GetValue(uxScreenManager) as UXScreenManagerProfile;
                }
            }

            return profile != null;
        }

        public static void CreateScreenGameObject(GameObject parentObject, string screenName)
        {
            // Create a new GameObject as child of the selected object
            var screenObject = new GameObject(screenName);
            screenObject.transform.SetParent(parentObject.transform, false);

            // Add required components
            // First, add UIDocument which is required by BaseScreen
            screenObject.AddComponent<UIDocument>();

            // Try to find and add the Screen component type dynamically
            // Since we don't know the exact screen type, we'll try to find it by name pattern
            AddScreenComponentByName(screenObject, screenName);

            // Try to find and add the Handler component type dynamically
            AddHandlerComponentByName(screenObject, screenName);

            // Mark scene as dirty for saving
            EditorSceneManager.MarkSceneDirty(screenObject.scene);

            // Select the new object
            Selection.activeObject = screenObject;

            Debug.Log($"Screen '{screenName}' created as child of '{parentObject.name}'.");
        }

        private static void AddScreenComponentByName(GameObject targetObject, string screenName)
        {
            // Build expected screen class name
            // E.g., "EditProfile" -> "EditProfileScreen"
            string screenClassName = screenName + "Screen";

            // Find the type by name across all loaded assemblies
            var screenType = FindTypeByName(screenClassName);
            if (screenType != null && typeof(MonoBehaviour).IsAssignableFrom(screenType))
            {
                targetObject.AddComponent(screenType);
                return;
            }

            // Log warning if screen type not found
            Debug.LogWarning($"Could not find Screen type '{screenClassName}'. " +
                $"Please manually add the Screen component to GameObject '{targetObject.name}'.");
        }

        private static void AddHandlerComponentByName(GameObject targetObject, string screenName)
        {
            // Build expected handler class name
            // E.g., "EditProfile" -> "EditProfileScreenHandler"
            string handlerClassName = screenName + "ScreenHandler";

            // Find the type by name across all loaded assemblies
            var handlerType = FindTypeByName(handlerClassName);
            if (handlerType != null && typeof(MonoBehaviour).IsAssignableFrom(handlerType))
            {
                targetObject.AddComponent(handlerType);
                return;
            }

            // Log warning if handler type not found
            Debug.LogWarning($"Could not find Handler type '{handlerClassName}'. " +
                $"Please manually add the Handler component to GameObject '{targetObject.name}'.");
        }

        private static Type FindTypeByName(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(typeName, false);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                    // Ignore errors when searching assemblies
                }
            }

            return null;
        }
    }

    /// <summary>
    /// EditorWindow for selecting and creating screens.
    /// </summary>
    public class ScreenSelectionWindow : EditorWindow
    {
        private GameObject parentObject;
        private List<string> screenNames;
        private Vector2 scrollPosition;
        private bool shouldClose = false;
        private string selectedScreenName = null;

        public static void ShowWindow(GameObject parent, List<string> screens)
        {
            var window = GetWindow<ScreenSelectionWindow>("Select Screen");
            window.parentObject = parent;
            window.screenNames = screens;
            window.minSize = new Vector2(300, 200);
            window.maxSize = new Vector2(400, 600);
        }

        private void OnGUI()
        {
            // Close window at the start if flag is set (before any layout calls)
            if (shouldClose)
            {
                Close();
                return;
            }

            if (parentObject == null || screenNames == null)
            {
                EditorGUILayout.HelpBox("Window data is invalid. Please try again.", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("Select a Screen to Add", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"Creating screen as child of: {parentObject.name}", MessageType.Info);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Available Screens:", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var screenName in screenNames)
            {
                if (GUILayout.Button(screenName, GUILayout.Height(30)))
                {
                    selectedScreenName = screenName;
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            
            if (GUILayout.Button("Cancel", GUILayout.Height(30)))
            {
                shouldClose = true;
            }

            // Handle selected screen after layout is complete
            if (selectedScreenName != null)
            {
                string capturedScreenName = selectedScreenName;
                GameObject capturedParent = parentObject;
                EditorApplication.delayCall += () =>
                {
                    AddScreenContextMenu.CreateScreenGameObject(capturedParent, capturedScreenName);
                };
                shouldClose = true;
                selectedScreenName = null;
            }
        }
    }
}

#endif
