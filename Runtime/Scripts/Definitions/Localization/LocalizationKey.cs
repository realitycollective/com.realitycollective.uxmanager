using UnityEngine;

namespace RealityCollective.UXManager.Services.Localization
{
        /// <summary>
        /// Represents a localization key with its display name.
        /// </summary>
        [System.Serializable]
        public class LocalizationKey
        {
            [SerializeField]
            [Tooltip("The unique identifier for this localization key")]
            public string key = "";

            [SerializeField]
            [Tooltip("Display name or description for this key")]
            public string displayName = "";

            public LocalizationKey() { }

            public LocalizationKey(string key, string displayName = "")
            {
                this.key = key;
                this.displayName = displayName;
            }
        }
}