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
using Gurux.Common;
using Microsoft.Maui.Controls;
using Gurux.MQTT.Maui.Properties;

namespace Gurux.MQTT.Maui;

/// <summary>Connects native controls to an unopened private media copy.</summary>
internal sealed class MediaSettingsDraft : IDisposable
{
    /// <summary>Owns the staged copy and conflict checks.</summary>
    private readonly MediaSettingsSession _session;
    /// <summary>Reads this media's native fields.</summary>
    private readonly SettingsFields _fields;
    /// <summary>Gets the native form layout.</summary>
    internal View Form => _fields.Layout;
    /// <summary>Copies settings and creates fields without opening a transport.</summary>
    /// <param name="source">The closed media whose settings will be staged.</param>
    internal MediaSettingsDraft(GXMqtt source)
    {
        var copy = new GXMqtt();
        _session = new MediaSettingsSession(source, copy);
        _fields = new SettingsFields();
        try
        {
            SettingsFields fields = _fields;
            var available = (AvailableMediaSettings)source.ConfigurableSettings;
            if (available.HasFlag(AvailableMediaSettings.Host))
            {
                fields.Text(Resources.BrokerAddress, copy.ServerAddress, value => copy.ServerAddress = value);
            }
            if (available.HasFlag(AvailableMediaSettings.Port))
            {
                fields.Number(Resources.Port, copy.Port, value => copy.Port = value);
            }
            if (available.HasFlag(AvailableMediaSettings.Topic))
            {
                fields.Text(Resources.Topic, copy.Topic, value => copy.Topic = value);
            }
            if (available.HasFlag(AvailableMediaSettings.ClientId))
            {
                fields.Text(Resources.ClientId, copy.ClientId, value => copy.ClientId = value);
            }
            if (available.HasFlag(AvailableMediaSettings.AsyncWaitTime))
            {
                fields.UnsignedNumber(Resources.Timeout, copy.AsyncWaitTime, value => copy.AsyncWaitTime = value);
            }

        }
        catch { _session.Dispose(); throw; }
    }
    /// <summary>Reads native controls and saves only after validation and conflict checks.</summary>
    internal void Commit() => _session.Commit(_fields.Read);
    /// <summary>Releases the unopened private media instance.</summary>
    public void Dispose() => _session.Dispose();
}
