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

using System.Resources;

namespace Gurux.MQTT.Example.Desktop.Properties;

/// <summary>Provides localized labels and messages for the desktop example.</summary>
internal static class ExampleSettingsResources
{
    /// <summary>Reads this assembly's embedded settings messages.</summary>
    private static readonly ResourceManager _manager = new("Gurux.MQTT.Example.Desktop.Properties.ExampleSettingsResources", typeof(ExampleSettingsResources).Assembly);

    /// <summary>Explains that a settings operation is already in progress.</summary>
    internal static string EditorAlreadyShowing => _manager.GetString(nameof(EditorAlreadyShowing), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(EditorAlreadyShowing);

    /// <summary>Gets the localized label for connection.</summary>
    internal static string ConnectionGroup => _manager.GetString(nameof(ConnectionGroup), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ConnectionGroup);

    /// <summary>Gets the localized label for data exchange.</summary>
    internal static string DataGroup => _manager.GetString(nameof(DataGroup), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(DataGroup);

    /// <summary>Gets the localized label for activity log.</summary>
    internal static string ActivityGroup => _manager.GetString(nameof(ActivityGroup), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ActivityGroup);

    /// <summary>Gets the localized label for connected.</summary>
    internal static string ConnectionActive => _manager.GetString(nameof(ConnectionActive), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ConnectionActive);

    /// <summary>Gets the localized label for disconnected.</summary>
    internal static string ConnectionInactive => _manager.GetString(nameof(ConnectionInactive), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ConnectionInactive);

    /// <summary>Gets the localized label for working.</summary>
    internal static string ConnectionBusy => _manager.GetString(nameof(ConnectionBusy), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ConnectionBusy);

    /// <summary>Gets the localized label for data.</summary>
    internal static string PayloadLabel => _manager.GetString(nameof(PayloadLabel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(PayloadLabel);

    /// <summary>Gets the localized label for minimum reply bytes.</summary>
    internal static string CountLabel => _manager.GetString(nameof(CountLabel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(CountLabel);

    /// <summary>Gets the localized label for end of packet.</summary>
    internal static string TerminatorLabel => _manager.GetString(nameof(TerminatorLabel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(TerminatorLabel);

    /// <summary>Gets the localized label for timeout (ms).</summary>
    internal static string TimeoutLabel => _manager.GetString(nameof(TimeoutLabel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(TimeoutLabel);
    /// <summary>Gets the localized heading for reply framing and timeout controls.</summary>
    internal static string ReplyGroup => _manager.GetString(nameof(ReplyGroup), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReplyGroup);
    /// <summary>Gets the localized state caption for an open media connection.</summary>
    internal static string StatusOpen => _manager.GetString(nameof(StatusOpen), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(StatusOpen);

    /// <summary>Gets the localized state caption for a closed media connection.</summary>
    internal static string StatusClosed => _manager.GetString(nameof(StatusClosed), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(StatusClosed);

    /// <summary>Gets the localized placeholder for an unselected endpoint.</summary>
    internal static string StatusNotConfigured => _manager.GetString(nameof(StatusNotConfigured), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(StatusNotConfigured);

    /// <summary>Gets the localized format for connection state and configured broker and topic.</summary>
    internal static string StatusMqtt => _manager.GetString(nameof(StatusMqtt), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(StatusMqtt);

    /// <summary>Gets the localized connection menu caption.</summary>
    internal static string MenuConnection => _manager.GetString(nameof(MenuConnection), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(MenuConnection);
    /// <summary>Gets the localized view menu caption.</summary>
    internal static string MenuView => _manager.GetString(nameof(MenuView), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(MenuView);
    /// <summary>Gets the localized settings menu caption.</summary>
    internal static string MenuSettings => _manager.GetString(nameof(MenuSettings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(MenuSettings);
    /// <summary>Gets the localized data menu caption.</summary>
    internal static string MenuData => _manager.GetString(nameof(MenuData), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(MenuData);
    /// <summary>Gets the localized communication settings menu caption.</summary>
    internal static string MenuCommunication => _manager.GetString(nameof(MenuCommunication), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(MenuCommunication);
    /// <summary>Gets the localized activity log menu caption.</summary>
    internal static string MenuActivity => _manager.GetString(nameof(MenuActivity), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(MenuActivity);
    /// <summary>Gets the localized navigate menu caption.</summary>
    internal static string MenuNavigate => _manager.GetString(nameof(MenuNavigate), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(MenuNavigate);
    /// <summary>Gets the localized cancel menu caption.</summary>
    internal static string MenuCancel => _manager.GetString(nameof(MenuCancel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(MenuCancel);
    /// <summary>Gets the localized caption for editing the media transport.</summary>
    internal static string MenuMediaSettings => _manager.GetString(nameof(MenuMediaSettings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(MenuMediaSettings);
}
