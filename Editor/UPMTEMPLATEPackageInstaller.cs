// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.Editor.Utilities;
using RealityCollective.Extensions;
using RealityCollective.ServiceFramework.Editor;
using RealityCollective.ServiceFramework.Editor.Packages;
using RealityToolkit.Editor;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RealityToolkit.UPMTEMPLATE.Editor
{
    [InitializeOnLoad]
    internal static class UPMTEMPLATEPackageInstaller
    {
        private static readonly string destinationPath = Application.dataPath + "/RealityToolkit.Generated/UPMTEMPLATE";
        private static readonly string sourcePath = Path.GetFullPath($"{PathFinderUtility.ResolvePath<IPathFinder>(typeof(UPMTEMPLATEPackagePathFinder)).ForwardSlashes()}{Path.DirectorySeparatorChar}{"Assets~"}");

        static UPMTEMPLATEPackageInstaller()
        {
            EditorApplication.delayCall += CheckPackage;
        }

        [MenuItem(RealityToolkitPreferences.Editor_Menu_Keyword + "/Packages/Install UPMTEMPLATE Package Assets...", true)]
        private static bool ImportPackageAssetsValidation()
        {
            return !Directory.Exists($"{destinationPath}{Path.DirectorySeparatorChar}");
        }

        [MenuItem(RealityToolkitPreferences.Editor_Menu_Keyword + "/Packages/Install UPMTEMPLATE Package Assets...")]
        private static void ImportPackageAssets()
        {
            EditorPreferences.Set($"{nameof(UPMTEMPLATEPackageInstaller)}.Assets", false);
            EditorApplication.delayCall += CheckPackage;
        }

        private static void CheckPackage()
        {
            if (!EditorPreferences.Get($"{nameof(UPMTEMPLATEPackageInstaller)}.Assets", false))
            {
                EditorPreferences.Set($"{nameof(UPMTEMPLATEPackageInstaller)}.Assets", AssetsInstaller.TryInstallAssets(sourcePath, destinationPath));
            }
        }
    }
}