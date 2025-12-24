// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.ServiceFramework.Services;
using RealityCollective.Utilities.Logging;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using RealityCollective.UXManager.Interfaces.ScreenManagement;

namespace RealityCollective.UXManager.Services.ScreenManagement
{
    [RequireComponent(typeof(UIDocument))]
    public class BaseScreen : MonoBehaviour, IAnimatedPanel
    {
        private UIDocument document;
        private VisualElement root;
        private bool isLandscape = false;
        private bool isInitialized = false;
        internal IUXScreenManager uxScreenManager;

        private void Start()
        {
            uxScreenManager = ServiceManager.Instance.GetService<IUXScreenManager>();
            if (!isInitialized)
            {
                StartCoroutine(Initialize());
            }
        }

        private void OnEnable()
        {
            if (!isInitialized)
            {
                StartCoroutine(Initialize());
            }
        }

        private void ValidateUIDocumentReference()
        {
            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }
        }

        private void Update()
        {
            if (root == null)
            {
                return;
            }

            if (!isLandscape && (Screen.orientation == ScreenOrientation.LandscapeLeft || Screen.orientation == ScreenOrientation.LandscapeRight))
            {
                // add class to panel
                root.RemoveFromClassList("portrait");
                root.AddToClassList("landscape");
                isLandscape = true;
            }
            else if (isLandscape && Screen.orientation == ScreenOrientation.Portrait)
            {
                // remove class from panel
                root.RemoveFromClassList("landscape");
                root.AddToClassList("portrait");
                isLandscape = false;
            }
        }

        private IEnumerator Initialize()
        {
            if (isInitialized)
            {
                yield break;
            }
            isInitialized = true;

            yield return null;

            ValidateUIDocumentReference();

            if (document == null && Application.isPlaying)
            {
                StaticLogger.LogError($"{name} - Document / style is not set");
                yield break;
            }

            root = document.rootVisualElement;

            root.Clear();

            root.styleSheets.Add(document.panelSettings.themeStyleSheet);
            root.AddToClassList("uxScreenContainer");
            root.AddToClassList("portrait");

            // Unity 6 can introduce competing USS selectors (ex: .container) and/or different
            // panel defaults. Force the screen root to be fullscreen/absolute so multiple
            // UIDocuments do not participate in parent flex layout.
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.flexGrow = 0;
            root.style.flexShrink = 0;
            root.style.justifyContent = Justify.Center;

            root.pickingMode = PickingMode.Ignore;

            GenerateUI(root);
        }

        /// <summary>
        /// Code-facing key (ex: ScreenNames.Main). Used by gameplay code; resolved via profile.
        /// </summary>
        public virtual string ScreenName { get; protected set; } = "None";

        /// <summary>
        /// Runtime id used for registration in UXScreenManager. Defaults to this GameObject name.
        /// </summary>
        public virtual string ScreenId => gameObject != null ? gameObject.name : string.Empty;

        protected virtual void GenerateUI(VisualElement root) { }

        public virtual void ShowPanel() { }

        public virtual void HidePanel() { }
    }
}