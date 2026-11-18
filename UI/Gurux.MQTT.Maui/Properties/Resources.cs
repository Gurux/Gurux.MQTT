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
using System.Globalization;
using System.Resources;

namespace Gurux.MQTT.Maui.Properties;

/// <summary>Resolves localized native settings labels and messages.</summary>
internal static class Resources
{
    /// <summary>Reads this assembly's settings resources.</summary>
    private static readonly ResourceManager _manager = new("Gurux.MQTT.Maui.Properties.Resources", typeof(Resources).Assembly);
    /// <summary>Gets the localized text: Media settings.</summary>
    internal static string Title => _manager.GetString(nameof(Title), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Save.</summary>
    internal static string Save => _manager.GetString(nameof(Save), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Cancel.</summary>
    internal static string Cancel => _manager.GetString(nameof(Cancel), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: This editor is already showing settings..</summary>
    internal static string Busy => _manager.GetString(nameof(Busy), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Close the media before editing settings..</summary>
    internal static string Open => _manager.GetString(nameof(Open), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: The media settings changed while this form was open. Reopen the form..</summary>
    internal static string Changed => _manager.GetString(nameof(Changed), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: This editor does not support the supplied media type..</summary>
    internal static string WrongMedia => _manager.GetString(nameof(WrongMedia), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: The settings owner must be a ContentPage or a NavigationPage showing a ContentPage..</summary>
    internal static string Owner => _manager.GetString(nameof(Owner), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: The parent must be a MauiMediaWindow from this settings library..</summary>
    internal static string Parent => _manager.GetString(nameof(Parent), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: The MAUI window must have a page..</summary>
    internal static string WindowPage => _manager.GetString(nameof(WindowPage), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: {0} must be a whole number..</summary>
    internal static string WholeNumber => _manager.GetString(nameof(WholeNumber), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Select a value for {0}..</summary>
    internal static string SelectValue => _manager.GetString(nameof(SelectValue), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: The settings could not be saved. Check the values and try again..</summary>
    internal static string Invalid => _manager.GetString(nameof(Invalid), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized response timeout label in seconds.</summary>
    internal static string Timeout => _manager.GetString(nameof(Timeout), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Port.</summary>
    internal static string Port => _manager.GetString(nameof(Port), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized label for the broker address.</summary>
    internal static string BrokerAddress => _manager.GetString(nameof(BrokerAddress), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized label for the topic.</summary>
    internal static string Topic => _manager.GetString(nameof(Topic), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized label for the client identifier.</summary>
    internal static string ClientId => _manager.GetString(nameof(ClientId), System.Globalization.CultureInfo.CurrentUICulture)!;
}
