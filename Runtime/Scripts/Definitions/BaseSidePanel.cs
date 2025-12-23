// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.Utilities.Extensions;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace RealityCollective.UXManager.Services.ScreenManagement
{
    public class BaseSidePanel : BaseScreen
    {
        const string SIDEBAR_PANEL_CLASS = "sidebar-panel";

        const string SIDEBAR_PANEL_CLICK_COVER_CLASS = "click-cover";
        private VisualElement sidebarPanel;

        private VisualElement sidebarPanelClickCover;
        private VisualElement logoBox;
        private VisualElement controlBox;

        public UnityEvent OnBackButtonClicked;
        public UnityEvent OnSidebarPanelClickCoverClicked;

        public void HideBox(string boxName)
        {
            if (boxName == "logo")
            {
                logoBox.style.display = DisplayStyle.None;
            }
            else if (boxName == "control")
            {
                controlBox.style.display = DisplayStyle.None;
            }
        }

        protected override void GenerateUI(VisualElement root)
        {
            sidebarPanelClickCover = UIToolkitExtensions.CreateVisualElement(root, $"{SIDEBAR_PANEL_CLICK_COVER_CLASS}");
            sidebarPanelClickCover.RegisterCallback<PointerUpEvent>(evt => OnSidebarPanelClickCoverClicked?.Invoke());

            sidebarPanel = UIToolkitExtensions.CreateVisualElement(root, $"{SIDEBAR_PANEL_CLASS}");

            // Control Box - the "back button" to hide the sidebar
            controlBox = UIToolkitExtensions.CreateVisualElement(sidebarPanel, "control", "control-box");
            UIToolkitExtensions.CreateVisualElement(controlBox, "control-button", "back-arrow-icon")
                .RegisterCallback<PointerUpEvent>(evt => OnBackButtonClicked?.Invoke());

            // Logo for the sidebar
            logoBox = UIToolkitExtensions.CreateVisualElement(sidebarPanel, "logo");

            GenerateSidePanelUI(sidebarPanel, controlBox);
        }

        private void ToggleRootPanelClicking(VisualElement root, bool clickable)
        {
            if (clickable)
            {
                root.AddToClassList("clickable-side-panel");
            }
            else
            {
                root.RemoveFromClassList("clickable-side-panel");
            }
        }

        protected virtual void GenerateSidePanelUI(VisualElement sidebarPanel, VisualElement controlBox) { }

        public override void ShowPanel()
        {
            ToggleRootPanelClicking(sidebarPanel.parent, true);
            sidebarPanel.AddToClassList("open");
            sidebarPanelClickCover.AddToClassList("open");
            //IsPanelOpen = true;
        }

        public override void HidePanel()
        {
            if (sidebarPanel != null)
            {
                ToggleRootPanelClicking(sidebarPanel.parent, false);
                sidebarPanel.RemoveFromClassList("open");
                sidebarPanelClickCover.RemoveFromClassList("open");
                //IsPanelOpen = false;
            }
        }
    }
}