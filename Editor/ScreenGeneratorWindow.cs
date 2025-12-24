using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using RealityCollective.UXManager.Profiles.ScreenManagement;
using RealityCollective.UXManager.Services.ScreenManagement;
using RealityCollective.Utilities.Extensions;

namespace RealityCollective.UXManager.Editor
{
    public class ScreenGeneratorWindow : EditorWindow
    {
        private string screenName = "NewScreen";
        private string rootPath = "Assets/UX";
        private bool createContainer = true;

        [MenuItem("Tools/UX Manager/Generate Screen")]
        private static void ShowWindow()
        {
            var window = GetWindow<ScreenGeneratorWindow>(true, "Screen Generator", true);
            window.minSize = new Vector2(400, 180);
            window.maxSize = new Vector2(400, 180);
            window.Show();
        }

        [MenuItem("Tools/UX Manager/Create UX Container")]
        public static void CreateUXContainer()
        {
            // Check for existing root object with UIDocument
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var obj in rootObjects)
            {
                if (obj.GetComponent<UIDocument>() != null)
                {
                    EditorUtility.DisplayDialog("Error", "A UX Container/Document already exists at the root of the scene.", "OK");
                    return;
                }
            }

            var uxContainer = new GameObject("UXContainer");
            var uiDoc = uxContainer.AddComponent<UIDocument>();
            
            // Try to find a PanelSettings asset
            var guids = AssetDatabase.FindAssets("t:PanelSettings");
            if (guids.Length > 0)
            {
                uiDoc.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            
            uxContainer.AddComponent<UXEditor>();
            
            Undo.RegisterCreatedObjectUndo(uxContainer, "Create UX Container");
            Selection.activeGameObject = uxContainer;

            EditorUtility.DisplayDialog("Success", "UX Container created.\nPlease check the Panel Settings on the new object.", "OK");
        }

        private void OnGUI()
        {
            GUILayout.Label("Screen Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            screenName = EditorGUILayout.TextField("Screen Name", screenName);

            GUILayout.BeginHorizontal();
            rootPath = EditorGUILayout.TextField("Root Path", rootPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Root Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        rootPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        rootPath = path;
                    }
                }
            }
            GUILayout.EndHorizontal();

            createContainer = EditorGUILayout.Toggle("Create UX Container", createContainer);

            EditorGUILayout.HelpBox("Tip: Select the 'UXContainer' object in the hierarchy before generating to ensure the screen is added to the correct parent.", MessageType.Info);

            GUILayout.Space(20);

            if (GUILayout.Button("Generate Screen"))
            {
                GenerateScreen();
            }
        }

        private void GenerateScreen()
        {
            if (string.IsNullOrEmpty(screenName))
            {
                EditorUtility.DisplayDialog("Error", "Screen Name cannot be empty.", "OK");
                return;
            }

            // 1. Update Profile
            var profilePath = FindProfilePath();
            if (string.IsNullOrEmpty(profilePath))
            {
                EditorUtility.DisplayDialog("Error", "Could not find UXScreenManagerProfile.", "OK");
                return;
            }

            var profile = AssetDatabase.LoadAssetAtPath<UXScreenManagerProfile>(profilePath);
            if (profile == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not load UXScreenManagerProfile.", "OK");
                return;
            }

            // Check if key exists
            bool keyExists = false;
            var serializedProfile = new SerializedObject(profile);
            var mappings = serializedProfile.FindProperty("screenKeyMappings");

            for (int i = 0; i < mappings.arraySize; i++)
            {
                var element = mappings.GetArrayElementAtIndex(i);
                var keyProp = element.FindPropertyRelative("Key");
                if (keyProp.stringValue == screenName)
                {
                    keyExists = true;
                    break;
                }
            }

            if (!keyExists)
            {
                mappings.InsertArrayElementAtIndex(mappings.arraySize);
                var element = mappings.GetArrayElementAtIndex(mappings.arraySize - 1);
                element.FindPropertyRelative("Key").stringValue = screenName;
                element.FindPropertyRelative("ScreenId").stringValue = screenName;
                serializedProfile.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
            }

            // 2. Regenerate ScreenNames via Reflection
            try
            {
                // Try to find the type in the Editor assembly
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var editorAssembly = assemblies.FirstOrDefault(a => a.GetName().Name == "RealityCollective.UXManager.Editor");
                if (editorAssembly != null)
                {
                    var generatorType = editorAssembly.GetType("RealityCollective.UXManager.Editor.ScreenNamesGenerator");
                    if (generatorType != null)
                    {
                        var generateMethod = generatorType.GetMethod("Generate", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                        if (generateMethod != null)
                        {
                            generateMethod.Invoke(null, null);
                        }
                        else
                        {
                            Debug.LogError("Could not find Generate method on ScreenNamesGenerator");
                        }
                    }
                    else
                    {
                        Debug.LogError("Could not find ScreenNamesGenerator type");
                    }
                }
                else
                {
                    Debug.LogError("Could not find RealityCollective.UXManager.Editor assembly");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to regenerate ScreenNames: {e}");
            }

            // 3. Create Scripts
            CreateScripts();

            // 4. Prepare for post-compilation
            EditorPrefs.SetString("UXGenerator_PendingScreen", screenName);
            EditorPrefs.SetString("UXGenerator_RootPath", rootPath);
            EditorPrefs.SetBool("UXGenerator_CreateContainer", createContainer);

            AssetDatabase.Refresh();
        }

        private string FindProfilePath()
        {
            var guids = AssetDatabase.FindAssets("t:UXScreenManagerProfile");
            if (guids.Length > 0) return AssetDatabase.GUIDToAssetPath(guids[0]);
            return null;
        }

        private void CreateScripts()
        {
            string screensFolder = Path.Combine(rootPath, "Screens");
            string handlersFolder = Path.Combine(rootPath, "Handlers");

            if (!Directory.Exists(screensFolder)) Directory.CreateDirectory(screensFolder);
            if (!Directory.Exists(handlersFolder)) Directory.CreateDirectory(handlersFolder);

            string screenTemplate = GetScreenTemplate(screenName);
            string handlerTemplate = GetHandlerTemplate(screenName);

            File.WriteAllText(Path.Combine(screensFolder, $"{screenName}Screen.cs"), screenTemplate);
            File.WriteAllText(Path.Combine(handlersFolder, $"{screenName}ScreenHandler.cs"), handlerTemplate);
        }

        private string GetScreenTemplate(string name)
        {
            return $@"using RealityCollective.Utilities.Extensions;
using RealityCollective.UXManager.Services.ScreenManagement;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class {name}Screen : BaseScreen
{{
    private VisualElement panel;

    protected override void GenerateUI(VisualElement root)
    {{
        ScreenName = ScreenNames.{name};

        panel = UIToolkitExtensions.CreateVisualElement(root, ""fullscreen-panel"");
        panel.pickingMode = PickingMode.Ignore;
    }}

    public override void ShowPanel()
    {{
        if (panel != null)
        {{
            panel.style.display = DisplayStyle.Flex;
        }}
    }}

    public override void HidePanel()
    {{
        if (panel != null)
        {{
            panel.style.display = DisplayStyle.None;
        }}
    }}
}}";
        }

        private string GetHandlerTemplate(string name)
        {
            return $@"using RealityCollective.UXManager.Services.ScreenManagement;
using UnityEngine;

[RequireComponent(typeof({name}Screen))]
public class {name}ScreenHandler : BaseScreenHandler
{{
    public override string ScreenName => ScreenNames.{name};
    private {name}Screen screen;

    #region MonoBehaviours
    void OnEnable()
    {{
        screen = GetComponent<{name}Screen>();
    }}

    private void OnDisable()
    {{
        if (screen != null)
        {{
        }}
    }}
    #endregion MonoBehaviours
}}";
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (!EditorPrefs.HasKey("UXGenerator_PendingScreen")) return;

            string screenName = EditorPrefs.GetString("UXGenerator_PendingScreen");
            string rootPath = EditorPrefs.GetString("UXGenerator_RootPath");
            bool createContainer = EditorPrefs.GetBool("UXGenerator_CreateContainer");

            EditorPrefs.DeleteKey("UXGenerator_PendingScreen");
            EditorPrefs.DeleteKey("UXGenerator_RootPath");
            EditorPrefs.DeleteKey("UXGenerator_CreateContainer");

            SetupScene(screenName, rootPath, createContainer);
        }

        private static void SetupScene(string screenName, string rootPath, bool createContainer)
        {
            GameObject uxContainer = null;

            // 1. Check Selection first
            if (Selection.activeGameObject != null)
            {
                uxContainer = Selection.activeGameObject;
            }

            // 2. If not selected, search in scene for highest level UIDocument with PanelSettings
            if (uxContainer == null)
            {
                var uxObjects = GameObject.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
                
                var bestCandidate = uxObjects
                    .Where(doc => doc.panelSettings != null)
                    .OrderBy(doc => GetHierarchyDepth(doc.transform))
                    .FirstOrDefault();

                if (bestCandidate != null)
                {
                    uxContainer = bestCandidate.gameObject;
                }
            }

            if (uxContainer == null)
            {
                if (createContainer)
                {
                    uxContainer = new GameObject("UXContainer");
                    uxContainer.transform.SetParent(null); // Ensure root
                    
                    var uiDoc = uxContainer.AddComponent<UIDocument>();
                    // Try to find a PanelSettings asset
                    var guids = AssetDatabase.FindAssets("t:PanelSettings");
                    if (guids.Length > 0)
                    {
                        uiDoc.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    }
                    uxContainer.AddComponent<UXEditor>();
                }
                else
                {
                    EditorUtility.DisplayDialog("Action Required", $"Please add the screen '{screenName}' manually to the scene.", "OK");
                    Debug.Log($"[UX Generator] Scripts generated. Please add {screenName} to the scene manually.");
                    return;
                }
            }

            if (uxContainer != null)
            {
                // Create Child
                var screenObj = new GameObject(screenName);
                screenObj.transform.SetParent(uxContainer.transform, false);

                // Add Components
                var screenType = System.Type.GetType($"{screenName}Screen, Assembly-CSharp") ?? System.Type.GetType($"{screenName}Screen");
                var handlerType = System.Type.GetType($"{screenName}ScreenHandler, Assembly-CSharp") ?? System.Type.GetType($"{screenName}ScreenHandler");

                if (screenType != null)
                {
                    screenObj.AddComponent(screenType);
                }
                else
                {
                    Debug.LogError($"Could not find type {screenName}Screen");
                }

                if (handlerType != null)
                {
                    screenObj.AddComponent(handlerType);
                }
                else
                {
                    Debug.LogError($"Could not find type {screenName}ScreenHandler");
                }

                Debug.Log($"[UX Generator] Completed: Generated Screen '{screenName}', updated profile, created scripts in '{rootPath}', and added to scene object '{uxContainer.name}'.");
            }
        }

        private static int GetHierarchyDepth(Transform t)
        {
            int depth = 0;
            while (t.parent != null)
            {
                depth++;
                t = t.parent;
            }
            return depth;
        }
    }
}
