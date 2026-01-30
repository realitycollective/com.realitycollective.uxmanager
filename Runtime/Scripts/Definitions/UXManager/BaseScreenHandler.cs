// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.ServiceFramework.Services;
using UnityEngine;
using RealityCollective.UXManager.Interfaces.ScreenManagement;
using RealityCollective.UXManager.Interfaces.Localization;

namespace RealityCollective.UXManager.Services.ScreenManagement
{
    public class BaseScreenHandler : MonoBehaviour
    {
        private IUXScreenManager uxScreenManager;
        protected IUXScreenManager UXScreenManager
           => uxScreenManager ??= ServiceManager.Instance?.GetService<IUXScreenManager>();
        internal ILocalizationService localizationService;
        protected ILocalizationService LocalizationService
           => localizationService ??= ServiceManager.Instance?.GetService<ILocalizationService>();

        /// <summary>
        /// Code-facing key (ex: ScreenNames.Main). Typically set by derived handlers.
        /// </summary>
        public virtual string ScreenName { get; protected set; } = "None";

        /// <summary>
        /// Runtime id used for registration in UXScreenManager. Defaults to this GameObject name.
        /// </summary>
        public virtual string ScreenId => gameObject != null ? gameObject.name : string.Empty;

        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;

        void Start()
        {
            UXScreenManager.RegisterScreen(ScreenId, this.gameObject);
            InitializeHandler();
        }

        public virtual void InitializeHandler() { }
    }
}