// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.Utilities.Extensions;
using RealityCollective.Utilities.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace RealityCollective.UXManager.Services.ScreenManagement
{
    //TODO: add css classes for peek and minimize states
    public class BaseLowerPanel : BaseScreen
    {
        const string LOWER_PANEL_CLASS = "lower-panel";

        private VisualElement lowerPanel;
        private VisualElement controlBar;
        private Label titleLabel;
        // Make Control bar public to allow child controls to place / align content within it.
        internal VisualElement ControlBar => controlBar;
        // Make title visible to child components to allow them to update it.
        internal Label TitleLabel => titleLabel;

        private PanelState currentPanelState = PanelState.Closed;
        public PanelState CurrentPanelState => currentPanelState;

        [Header("Buttons")][SerializeField] private Button backButton;
        public Button BackButton => backButton;

        [Header("Events")]
        [Space(10)]
        public UnityEvent OnMenuOpened;
        public UnityEvent OnMenuClosed;
        public UnityEvent OnBackButtonPressed;
        public UnityEvent OnMenuMinimized;

        protected override void GenerateUI(VisualElement root)
        {
            // Containing panel - Panel is animated on and off screen
            lowerPanel = UIToolkitExtensions.CreateVisualElement(root, $"{LOWER_PANEL_CLASS}");
            lowerPanel.pickingMode = PickingMode.Ignore;

            // Top control bar where buttons are placed and aligned
            controlBar = UIToolkitExtensions.CreateVisualElement(lowerPanel, $"control-bar");
            controlBar.pickingMode = PickingMode.Ignore;
            backButton = UIToolkitExtensions.CreateVisualElement<Button>(controlBar, $"back-button");
            backButton.clicked += () => OnBackButtonPressed?.Invoke();
            titleLabel = UIToolkitExtensions.CreateVisualElement<Label>(controlBar, $"title-label");
            titleLabel.pickingMode = PickingMode.Ignore;

            titleLabel.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            GenerateLowerPanelUI(lowerPanel);
        }

        #region UI Handlers
        public void OpenMenu(bool force = false)
        {
            if (currentPanelState == PanelState.Open && !force)
            {
                return;
            }

            // Animate to Open
            uxScreenManager.ShowScreen(ScreenName);
        }

        public void CloseMenu()
        {
            // Animate to state closed
            uxScreenManager.HideScreen(ScreenName);
        }

        public void MinimizeMenu()
        {
            switch (currentPanelState)
            {
                case PanelState.Minimized:
                    return;
                case PanelState.Closed:
                    MenuOpenedHandler();
                    break;
            }

            // Animate to state minimized
            currentPanelState = PanelState.Minimized;
            lowerPanel.AddToClassList("minimized");
            OnMenuMinimized?.Invoke();
        }

        public void PeekMenu()
        {
            if (currentPanelState == PanelState.Peek)
            {
                return;
            }

            if (currentPanelState == PanelState.Closed)
            {
                MenuOpenedHandler();
            }

            // Animate to state Peek
            currentPanelState = PanelState.Peek;
        }

        public void MaximizeMenu()
        {
            if (currentPanelState > PanelState.Open)
            {
                OpenMenu(true);
            }
        }
        #endregion UI Handlers

        #region Event Handlers
        private void MenuOpenedHandler()
        {
            //IsPanelOpen = true;
            OnMenuOpened?.Invoke();
        }

        private void MenuClosedHandler()
        {
            Invoke(nameof(SetPanelClosedAfterAnimationDelay), 0.3f);
            OnMenuClosed?.Invoke();
        }

        private void SetPanelClosedAfterAnimationDelay()
        {
            //IsPanelOpen = false;
        }
        #endregion Event Handlers

        protected virtual void GenerateLowerPanelUI(VisualElement parentPanel) { }

        public override void ShowPanel()
        {
            lowerPanel.AddToClassList("open");
            lowerPanel.RemoveFromClassList("minimized");
            currentPanelState = PanelState.Open;
            MenuOpenedHandler();
            UpdateFontSize();
        }

        public override void HidePanel()
        {
            if (lowerPanel != null)
            {
                lowerPanel.RemoveFromClassList("open");
                lowerPanel.RemoveFromClassList("minimized");
                currentPanelState = PanelState.Closed;
                MenuClosedHandler();
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent e)
        {
            UpdateFontSize();
        }

        private void UpdateFontSize()
        {
            titleLabel.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            var previousWidthStyle = titleLabel.style.width;

            try
            {
                // Set width to auto temporarily to get the actual width of the label
                titleLabel.style.width = StyleKeyword.Auto;
                var currentFontSize = titleLabel.MeasureTextSize(titleLabel.text, 0, TextElement.MeasureMode.Undefined, 0, TextElement.MeasureMode.Undefined);

                StaticLogger.Log("Resolved Style Width: " + controlBar.resolvedStyle.width);
                var multiplier = controlBar.resolvedStyle.width / Mathf.Max(currentFontSize.x, 1);
                var newFontSize = controlBar.resolvedStyle.width * (0.6f /10f);

                if (Mathf.RoundToInt(currentFontSize.y) != newFontSize)
                    titleLabel.style.fontSize = new StyleLength(new Length(newFontSize));
            }
            finally
            {
                titleLabel.style.width = previousWidthStyle;
                titleLabel.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }
    }
}