// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.UXManager.Interfaces.ScreenManagement;
using RealityCollective.UXManager.Profiles.ScreenManagement;
using RealityCollective.ServiceFramework.Services;
using RealityCollective.Utilities.Extensions;
using RealityCollective.Utilities.Logging;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealityCollective.UXManager.Services.ScreenManagement
{
    [System.Runtime.InteropServices.Guid("c7c6f69f-9045-4af7-800e-d2f2e46cee3d")]
    public class UXScreenManager : BaseServiceWithConstructor, IUXScreenManager
    {
        private class Screen
        {
            public GameObject screenObject;
            public UIDocument screenDocument;
            public IAnimatedPanel animatedPanel;
        }

        private readonly Dictionary<string, Screen> ScreenCache = new();
        private UXScreenManagerProfile profile;
        private List<string> visibleScreens = new();
        public List<string> VisibleScreens => visibleScreens;
        private bool pauseNewScreens = false;

        public UXScreenManager(string name, uint priority, UXScreenManagerProfile profile)
            : base(name, priority)
        {
            this.profile = profile;
        }

        #region IUXScreenManager implementation

        public void RegisterScreen(string screenId, GameObject screenObject)
        {
            if (screenObject == null)
            {
                StaticLogger.LogError("Cannot register a null document for screen");
                return;
            }

            if (string.IsNullOrWhiteSpace(screenId))
            {
                StaticLogger.LogError("Cannot register a screen with a null/empty id");
                return;
            }

            var screen = GenerateScreen(screenObject);
            if (ScreenCache.ContainsKey(screenId))
            {
                ScreenCache[screenId] = screen;
                return;
            }
            ScreenCache.Add(screenId, screen);
        }

        private string ResolveScreenId(string screenKeyOrId)
        {
            if (profile == null)
            {
                return screenKeyOrId;
            }

            return profile.ResolveScreenId(screenKeyOrId);
        }

        private Screen GenerateScreen(GameObject screenObject)
        {
            var screen = new Screen
            {
                screenObject = screenObject,
                screenDocument = screenObject.GetComponent<UIDocument>(),
                animatedPanel = screenObject.GetComponent<IAnimatedPanel>()
            };
            return screen;
        }

        public void RemoveScreen(string screenId)
        {
            if (string.IsNullOrWhiteSpace(screenId))
            {
                return;
            }

            if (ScreenCache.ContainsKey(screenId))
            {
                ScreenCache.Remove(screenId);
            }
        }

        // When a screen is shown, check the Tutorial collection for any tutorial images to display.  If found, then setup the tutorial, wire up the events and show the tutorial screen.
        // On complete, hide the tutorial screen and unregister the events.
        public void ShowScreen(string screenKey)
        {
            var screenId = ResolveScreenId(screenKey);

            if (!pauseNewScreens && !string.IsNullOrWhiteSpace(screenId) && ScreenCache.ContainsKey(screenId))
            {
                var selectedScreen = ScreenCache[screenId];
                if(selectedScreen.screenObject.TryGetComponent<BaseScreenHandler>(out var handler))
                {
                    if (!handler.IsInitialized)
                    {
                        handler.InitializeHandler();
                    }
                }
                selectedScreen.screenDocument.rootVisualElement.BringToFront();
                selectedScreen.animatedPanel?.ShowPanel();

                visibleScreens.EnsureListItem(screenKey);
            }

            if(screenId == "None")
            {
                HideAllScreens();
                StaticLogger.Log("ShowScreen called with 'None' screen key.  Hiding all screens.");
            }
        }

        public void HideScreen(string screenKey, bool force = false)
        {
            var screenId = ResolveScreenId(screenKey);
            if (!string.IsNullOrWhiteSpace(screenId) && ScreenCache.ContainsKey(screenId))
            {
                if (ScreenCache[screenId].screenDocument.rootVisualElement == null)
                {
                    // If the screen has been disabled, skip it.
                    return;
                }
                ScreenCache[screenId].animatedPanel?.HidePanel();
                ScreenCache[screenId].screenDocument.rootVisualElement.SendToBack();
                visibleScreens.SafeRemoveListItem(screenKey);
            }
        }

        public void HideAllScreens()
        {
            foreach (var screen in ScreenCache)
            {
                // screen.Key is already a runtime id, so bypass profile resolution by passing id.
                HideScreen(screen.Key, true);
            }
        }

        public void DisableScreen(string screenKey)
        {
            var screenId = ResolveScreenId(screenKey);
            if (!string.IsNullOrWhiteSpace(screenId) && ScreenCache.ContainsKey(screenId))
            {
                ScreenCache[screenId].screenDocument.rootVisualElement.style.display = DisplayStyle.None;
            }
        }

        public void EnableScreen(string screenKey)
        {
            var screenId = ResolveScreenId(screenKey);
            if (!string.IsNullOrWhiteSpace(screenId) && ScreenCache.ContainsKey(screenId))
            {
                ScreenCache[screenId].screenDocument.rootVisualElement.style.display = DisplayStyle.Flex;
            }
        }

        public void TransitionToScreen(string fromScreenKey, string toScreenKey)
        {
            HideScreen(fromScreenKey);
            ShowScreen(toScreenKey);
        }

        public T GetScreen<T>()
        {
            foreach (var screen in ScreenCache)
            {
                if (screen.Value.screenObject.TryGetComponent<T>(out var component))
                {
                    return component;
                }
            }
            return default;
        }
        #endregion IUXScreenManager implementation
    }
}