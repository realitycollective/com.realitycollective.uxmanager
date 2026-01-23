// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.Utilities.Extensions;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace RealityCollective.UXManager.Services.ScreenManagement
{
    public class BaseFloatingPanel : BaseScreen
    {
        const string FLOATING_PANEL_CLASS = "floating-panel";

        private VisualElement floatingPanel;
        private VisualElement logoBox;

        private VisualElement controlBox;

        public UnityEvent OnBackButtonPressed;

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
            floatingPanel = UIToolkitExtensions.CreateVisualElement(root, $"{FLOATING_PANEL_CLASS}");

            controlBox = UIToolkitExtensions.CreateVisualElement(floatingPanel, "control", "control-box");
            UIToolkitExtensions.CreateVisualElement(controlBox, "back-arrow").RegisterCallback<ClickEvent>(evt => OnBackButtonPressed?.Invoke());

            logoBox = UIToolkitExtensions.CreateVisualElement(floatingPanel, "logo");

            GenerateFloatingPanelUI(floatingPanel);
        }

        protected virtual void GenerateFloatingPanelUI(VisualElement floatingPanel) { }

        public override void ShowPanel()
        {
            floatingPanel.AddToClassList("open");
        }

        public override void HidePanel()
        {
            if (floatingPanel != null)
            {
                floatingPanel.RemoveFromClassList("open");
            }
        }
    }
}
