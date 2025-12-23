// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using UnityEngine.UIElements;

namespace RealityCollective.UXManager.Services.ScreenManagement
{
    public class TestStylesheetAccessor : MonoBehaviour
    {
        [SerializeField]
        private StyleSheet applicationStyleSheet;
        public StyleSheet ApplicationStyleSheet => applicationStyleSheet;
    }
}
