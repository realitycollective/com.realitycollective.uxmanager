// Copyright (c) Ethar. All rights reserved.

using RealityCollective.UXManager.Services.ScreenManagement;
using UnityEditor;
using UnityEngine;

namespace RealityCollective.UXManager.Profiles.ScreenManagement.Editor
{
    [CustomEditor(typeof(UXEditor))]
    public class UXEditorInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            UXEditor uxEditor = (UXEditor)target;
            if (GUILayout.Button("Show selected screen"))
            {
                uxEditor.OnButtonClick();
            }
        }
    }
}