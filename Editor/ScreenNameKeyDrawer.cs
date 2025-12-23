// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RealityCollective.UXManager.Services.ScreenManagement.Editor
{
    [CustomPropertyDrawer(typeof(ScreenNameKeyAttribute))]
    internal sealed class ScreenNameKeyDrawer : PropertyDrawer
    {
        private const string ScreenNamesTypeFullName = "RealityCollective.UXManager.Services.ScreenManagement.ScreenNames";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var options = GetScreenNameOptions();
            if (options.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var current = property.stringValue ?? string.Empty;
            var currentIndex = options.IndexOf(current);

            // If the value isn't one of the generated constants, show it as a temporary first option.
            if (currentIndex < 0)
            {
                options = new List<string>(options.Count + 1)
                {
                    string.IsNullOrWhiteSpace(current) ? "(Custom) <empty>" : $"(Custom) {current}"
                };
                options.AddRange(GetScreenNameOptions());
                currentIndex = 0;
            }

            var display = options.Select(o => o.StartsWith("(Custom)", StringComparison.Ordinal) ? o : o).ToArray();

            EditorGUI.BeginProperty(position, label, property);
            var newIndex = EditorGUI.Popup(position, label.text, currentIndex, display);
            if (newIndex != currentIndex)
            {
                var selected = options[newIndex];
                if (!selected.StartsWith("(Custom)", StringComparison.Ordinal))
                {
                    property.stringValue = selected;
                }
            }
            EditorGUI.EndProperty();
        }

        private static readonly object CacheLock = new();
        private static string[] cached;
        private static double cachedAtTime;

        private static List<string> GetScreenNameOptions()
        {
            // Very small cache to avoid reflection spam during layout/repaint.
            lock (CacheLock)
            {
                if (cached != null && (EditorApplication.timeSinceStartup - cachedAtTime) < 0.5)
                {
                    return cached.ToList();
                }

                cached = ReflectScreenNamesConstants();
                cachedAtTime = EditorApplication.timeSinceStartup;
                return cached.ToList();
            }
        }

        private static string[] ReflectScreenNamesConstants()
        {
            try
            {
                var type = ResolveScreenNamesType();
                if (type == null)
                {
                    return Array.Empty<string>();
                }

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                var values = new HashSet<string>(StringComparer.Ordinal);
                foreach (var field in fields)
                {
                    if (field.FieldType != typeof(string))
                    {
                        continue;
                    }

                    // We generate const strings, which are static + literal.
                    if (!field.IsLiteral)
                    {
                        continue;
                    }

                    var v = field.GetRawConstantValue() as string;
                    if (string.IsNullOrWhiteSpace(v))
                    {
                        continue;
                    }

                    values.Add(v);
                }

                return values.OrderBy(v => v, StringComparer.Ordinal).ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static Type ResolveScreenNamesType()
        {
            // ScreenNames is generated into the client project (Assets/) and may live in a different assembly.
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(ScreenNamesTypeFullName, throwOnError: false);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                    // ignore broken assemblies
                }
            }

            return null;
        }
    }
}
#endif
