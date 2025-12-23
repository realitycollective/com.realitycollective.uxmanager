// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.ServiceFramework.Definitions;
using RealityCollective.ServiceFramework.Editor.Packages;
using RealityCollective.ServiceFramework.Services;
using System.Linq;
using UnityEditor;

namespace RealityToolkit.UPMTEMPLATE.Editor
{
    /// <summary>
    /// Installs <see cref="IUPMTEMPLATEModule"/>s coming from a third party package
    /// into the <see cref="UPMTEMPLATEProfile"/> in the <see cref="ServiceManager.ActiveProfile"/>.
    /// </summary>
    [InitializeOnLoad]
    public sealed class UPMTEMPLATEPackageModulesInstaller : IPackageModulesInstaller
    {
        /// <summary>
        /// Static initializer for the installer instance.
        /// </summary>
        static UPMTEMPLATEPackageModulesInstaller()
        {
            if (Instance == null)
            {
                Instance = new UPMTEMPLATEPackageModulesInstaller();
            }

            PackageInstaller.RegisterModulesInstaller(Instance);
        }

        /// <summary>
        /// Internal singleton instance of the installer.
        /// </summary>
        private static UPMTEMPLATEPackageModulesInstaller Instance { get; }

        /// <inheritdoc/>
        public bool Install(ServiceConfiguration serviceConfiguration)
        {
/*
-------------------------------------------------------
TO install modules for the service, uncomment the code below.

Note, in order to correctly assign modules for the service, you need to replace the following (Where UPMTEMPLATE is the service name used to generate this repository):

- UPMTEMPLATE with the correct service type.
- UPMTEMPLATEModule with the correct module type.
- IUPMTEMPLATEModule with the correct module interface.
- UPMTEMPLATEProfile with the correct profile type.

These are collated from the service and module definitions generated using the Service Template Generator
-------------------------------------------------------
            if (!typeof(IUPMTEMPLATEModule).IsAssignableFrom(serviceConfiguration.InstancedType.Type))
            {
                // This module installer does not accept the configuration type.
                return false;
            }

            if (!ServiceManager.IsActiveAndInitialized)
            {
                UnityEngine.Debug.LogWarning($"Could not install {serviceConfiguration.InstancedType.Type.Name}.{nameof(ServiceManager)} is not initialized.");
                return false;
            }

            if (!ServiceManager.Instance.HasActiveProfile)
            {
                UnityEngine.Debug.LogWarning($"Could not install {serviceConfiguration.InstancedType.Type.Name}.{nameof(ServiceManager)} has no active profile.");
                return false;
            }

            if (!ServiceManager.Instance.TryGetServiceProfile<IUPMTEMPLATE, UPMTEMPLATEProfile>(out var UPMTEMPLATEProfile))
            {
                UnityEngine.Debug.LogWarning($"Could not install {serviceConfiguration.InstancedType.Type.Name}.{nameof(UPMTEMPLATEProfile)} not found.");
                return false;
            }

            // Setup the configuration.
            var typedServiceConfiguration = new ServiceConfiguration<IUPMTEMPLATEModule>(serviceConfiguration.InstancedType.Type, serviceConfiguration.Name, serviceConfiguration.Priority, serviceConfiguration.RuntimePlatforms, serviceConfiguration.Profile);

            // Make sure it is not already in the target profile.
            if (UPMTEMPLATEProfile.ServiceConfigurations.All(sc => sc.InstancedType.Type != serviceConfiguration.InstancedType.Type))
            {
                UPMTEMPLATEProfile.AddConfiguration(typedServiceConfiguration);
                UnityEngine.Debug.Log($"Successfully installed the {serviceConfiguration.InstancedType.Type.Name} to {UPMTEMPLATEProfile.name}.");
            }
            else
            {
                UnityEngine.Debug.Log($"Skipped installing the {serviceConfiguration.InstancedType.Type.Name} to {UPMTEMPLATEProfile.name}. Already installed.");
            }
*/
            return true;
        }
    }
}