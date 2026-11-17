// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Microsoft.Maui;
using Microsoft.Maui.Controls;
namespace Gurux.MQTT.Example.Maui;
/// <summary>Creates native windows for the MQTT example.</summary>
public sealed class MediaExampleApplication : Application
{
    /// <inheritdoc/>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var page = new MediaExamplePage();
        var window = new Window(new NavigationPage(page)) { Title = global::Gurux.MQTT.Example.Properties.ExampleResources.Title };
        if (OperatingSystem.IsWindows())
        {
            window.Width = 820;
            window.Height = 800;
            window.MinimumWidth = 420;
            window.MinimumHeight = 560;
        }
        window.Destroying += async (_, _) => { await page.ShutdownAsync(); };
        return window;
    }
}
