// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using RealityCollective.UXManager.Editor;

namespace RealityCollective.UXManager.Profiles.ScreenManagement.Editor
{
    [CustomPropertyDrawer(typeof(ScreenKeyMappingsAttribute))]
    public sealed class ScreenKeyMappingsDrawer : PropertyDrawer
    {
        private const float ColumnSplit = 0.45f;
        private const float ColumnGap = 8f;
        private const float TopPadding = 2f;

        // Cache ReorderableLists per inspected object + propertyPath.
        private static readonly Dictionary<string, ReorderableList> Lists = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null || !property.isArray)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            var y = position.y;
            var width = position.width;
            var x = position.x;
            var line = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            // Title
            var titleRect = new Rect(x, y, width, line);
            EditorGUI.LabelField(titleRect, "Screen Key Mappings", EditorStyles.boldLabel);
            y += line + spacing;

            // Info
            var infoText = "Add mappings from a code-facing key (generated as ScreenNames.<Key>) to a runtime Screen Id (usually the screen GameObject name used during registration).";
            var infoHeight = GetHelpBoxHeightEstimate();
            var infoRect = new Rect(x, y, width, infoHeight);
            EditorGUI.HelpBox(infoRect, infoText, MessageType.Info);
            y += infoHeight + spacing;

            // List
            var list = GetOrCreateList(property);
            var listHeight = list.GetHeight();
            var listRect = new Rect(x, y, width, listHeight);
            list.DoList(listRect);
            y += listHeight + spacing;

            // Validation
            GetDuplicates(property, out var duplicateKeys, out var duplicateIds);
            if (duplicateKeys.Length > 0)
            {
                var warnRect = new Rect(x, y, width, GetHelpBoxHeightEstimate());
                EditorGUI.HelpBox(warnRect, $"Duplicate Keys found: {string.Join(", ", duplicateKeys)}", MessageType.Warning);
                y += warnRect.height + spacing;
            }

            if (duplicateIds.Length > 0)
            {
                var idsRect = new Rect(x, y, width, GetHelpBoxHeightEstimate());
                EditorGUI.HelpBox(idsRect, $"Multiple keys map to the same Screen Id: {string.Join(", ", duplicateIds)}", MessageType.Info);
                y += idsRect.height + spacing;
            }

            // Regenerate button
            var buttonRect = new Rect(x, y, width, line);
            if (GUI.Button(buttonRect, "Regenerate ScreenNames"))
            {
                ScreenNamesGenerator.GenerateForProfile(property.serializedObject.targetObject);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property == null || !property.isArray)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            var line = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            var height = 0f;
            height += line + spacing; // title
            height += GetHelpBoxHeightEstimate() + spacing; // info

            var list = GetOrCreateList(property);
            height += list.GetHeight() + spacing;

            GetDuplicates(property, out var duplicateKeys, out var duplicateIds);
            if (duplicateKeys.Length > 0)
            {
                height += GetHelpBoxHeightEstimate() + spacing;
            }

            if (duplicateIds.Length > 0)
            {
                height += GetHelpBoxHeightEstimate() + spacing;
            }

            height += line; // button
            return height;
        }

        private static float GetHelpBoxHeightEstimate()
        {
            // Property drawers don't get width in GetPropertyHeight, so keep this conservative.
            return (EditorGUIUtility.singleLineHeight * 2f) + 12f;
        }

        private static void GetDuplicates(SerializedProperty mappingsProp, out string[] duplicateKeys, out string[] duplicateIds)
        {
            var keyCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var idCounts = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var i = 0; i < mappingsProp.arraySize; i++)
            {
                var element = mappingsProp.GetArrayElementAtIndex(i);
                var key = element.FindPropertyRelative("Key")?.stringValue?.Trim();
                var id = element.FindPropertyRelative("ScreenId")?.stringValue?.Trim();

                if (!string.IsNullOrWhiteSpace(key))
                {
                    keyCounts[key] = keyCounts.TryGetValue(key, out var c) ? c + 1 : 1;
                }

                if (!string.IsNullOrWhiteSpace(id))
                {
                    idCounts[id] = idCounts.TryGetValue(id, out var c) ? c + 1 : 1;
                }
            }

            duplicateKeys = keyCounts.Where(kvp => kvp.Value > 1).Select(kvp => kvp.Key).ToArray();
            duplicateIds = idCounts.Where(kvp => kvp.Value > 1).Select(kvp => kvp.Key).ToArray();
        }

        private static ReorderableList GetOrCreateList(SerializedProperty mappingsProp)
        {
            var key = GetCacheKey(mappingsProp);

            if (Lists.TryGetValue(key, out var existing) && existing.serializedProperty?.serializedObject == mappingsProp.serializedObject)
            {
                existing.serializedProperty = mappingsProp;
                return existing;
            }

            var list = new ReorderableList(mappingsProp.serializedObject, mappingsProp, true, true, true, true)
            {
                elementHeight = EditorGUIUtility.singleLineHeight + 6
            };

            list.drawHeaderCallback = rect =>
            {
                var left = rect;
                left.width = rect.width * ColumnSplit;

                var right = rect;
                right.xMin = left.xMax + ColumnGap;

                EditorGUI.LabelField(left, "Key (used in code)");
                EditorGUI.LabelField(right, "Screen Id (registered runtime id)");
            };

            list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = mappingsProp.GetArrayElementAtIndex(index);
                var keyProp = element.FindPropertyRelative("Key");
                var idProp = element.FindPropertyRelative("ScreenId");

                rect.y += TopPadding;
                rect.height = EditorGUIUtility.singleLineHeight;

                var left = rect;
                left.width = rect.width * ColumnSplit;

                var right = rect;
                right.xMin = left.xMax + ColumnGap;

                EditorGUI.PropertyField(left, keyProp, GUIContent.none);
                EditorGUI.PropertyField(right, idProp, GUIContent.none);
            };

            Lists[key] = list;
            return list;
        }

        private static string GetCacheKey(SerializedProperty property)
        {
            var instanceId = property.serializedObject?.targetObject != null
                ? property.serializedObject.targetObject.GetInstanceID().ToString()
                : "<null>";
            return instanceId + ":" + property.propertyPath;
        }
    }
}
#endif
