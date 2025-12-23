// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_EDITOR
using RealityCollective.UXManager.Editor;
using RealityCollective.ServiceFramework.Editor.Profiles;
using UnityEditor;
using UnityEngine;

namespace RealityCollective.UXManager.Profiles.ScreenManagement.Editor
{
    [CustomEditor(typeof(UXScreenManagerProfile))]
    [CanEditMultipleObjects]
    internal sealed class UXScreenManagerProfileInspector : ServiceProfileInspector 
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Regenerate ScreenNames", GUILayout.Width(220f)))
                {
                    ScreenNamesGenerator.GenerateForProfile(target);
                }
                GUILayout.FlexibleSpace();
            }
        }
    }
}
#endif
