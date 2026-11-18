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
namespace Gurux.MQTT.Blazor.Properties;
/// <summary>Provides localized settings labels and complete validation messages.</summary>
internal static class Resources
{
    /// <summary>Looks up the embedded resources using the current UI culture.</summary>
    private static readonly System.Resources.ResourceManager _manager = new("Gurux.MQTT.Blazor.Properties.Resources", typeof(Resources).Assembly);
    /// <summary>Gets the localized WholeNumberRequired text.</summary>
    internal static string WholeNumberRequired => _manager.GetString(nameof(WholeNumberRequired), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Port text.</summary>
    internal static string Port => _manager.GetString(nameof(Port), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Closethemediabeforeeditingsettings text.</summary>
    internal static string Closethemediabeforeeditingsettings => _manager.GetString(nameof(Closethemediabeforeeditingsettings), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized ThemediasettingschangedwhilethisformwasopenReopentheform text.</summary>
    internal static string ThemediasettingschangedwhilethisformwasopenReopentheform => _manager.GetString(nameof(ThemediasettingschangedwhilethisformwasopenReopentheform), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized MediaSettings text.</summary>
    internal static string MediaSettings => _manager.GetString(nameof(MediaSettings), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Save text.</summary>
    internal static string Save => _manager.GetString(nameof(Save), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Cancel text.</summary>
    internal static string Cancel => _manager.GetString(nameof(Cancel), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized UnsupportedMedia text.</summary>
    internal static string UnsupportedMedia => _manager.GetString(nameof(UnsupportedMedia), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized InvalidSettings text.</summary>
    internal static string InvalidSettings => _manager.GetString(nameof(InvalidSettings), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized label for the broker address.</summary>
    internal static string BrokerAddress => _manager.GetString(nameof(BrokerAddress), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized label for the topic.</summary>
    internal static string Topic => _manager.GetString(nameof(Topic), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized label for the client identifier.</summary>
    internal static string ClientId => _manager.GetString(nameof(ClientId), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized label for the response timeout in seconds.</summary>
    internal static string Timeout => _manager.GetString(nameof(Timeout), System.Globalization.CultureInfo.CurrentUICulture)!;
}
