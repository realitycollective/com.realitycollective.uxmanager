// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using RealityCollective.ServiceFramework.Interfaces;
using UnityEngine;

namespace RealityCollective.UXManager.Interfaces.ScreenManagement
{
    public interface IUXScreenManager : IService
    {
        /// <summary>
        /// Registers a screen instance under its runtime id (usually the screen GameObject name).
        /// </summary>
        void RegisterScreen(string screenId, GameObject document);

        void RemoveScreen(string screenId);

        /// <summary>
        /// Shows a screen by code-facing key (ex: ScreenNames.Main). Key is resolved via UXScreenManagerProfile.
        /// </summary>
        void ShowScreen(string screenKey);

        void HideScreen(string screenKey, bool force = false);
        void HideAllScreens();

        void DisableScreen(string screenKey);
        void EnableScreen(string screenKey);
        void TransitionToScreen(string fromScreenKey, string toScreenKey);
        List<string> VisibleScreens { get; }
        T GetScreen<T>();
    }
}