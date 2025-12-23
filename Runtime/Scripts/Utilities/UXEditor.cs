// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.ServiceFramework.Services;
using UnityEngine;
using RealityCollective.UXManager.Interfaces.ScreenManagement;

namespace RealityCollective.UXManager.Services.ScreenManagement
{
    public class UXEditor : MonoBehaviour
    {
        [SerializeField]
        [ScreenNameKey]
        private string selectedScreenKey = "None";

        private static IUXScreenManager uxScreenManager;
        public static IUXScreenManager UXScreenManager
           => uxScreenManager ??= ServiceManager.Instance?.GetService<IUXScreenManager>();


        // Method to be called when the button is clicked
        public void OnButtonClick()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("UXScreenManager service is not available when the project is not running.");
                return;
            }

            if (UXScreenManager == null)
            {
                Debug.LogWarning("UXScreenManager service is not available.");
                return;
            }
            
            UXScreenManager.HideAllScreens();
            UXScreenManager.ShowScreen(selectedScreenKey);
        }
    }
}
