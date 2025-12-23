// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace RealityCollective.UXManager.Services.ScreenManagement
{
    /// <summary>
    /// Marks a string field as a screen key (ex: ScreenNames.Main) so the Editor can render it as a dropdown.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ScreenNameKeyAttribute : PropertyAttribute
    {
    }
}
