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
using Avalonia.Controls;
using Gurux.Common;
using Gurux.MQTT;
using Gurux.MQTT.Avalonia.Properties;
namespace Gurux.MQTT.Avalonia;
/// <summary>Owns the typed form and its private validated media draft.</summary>
internal sealed class MediaSettingsDraft : IDisposable
{
    /// <summary>Owns the isolated media copy and conflict checks.</summary>
    private readonly MediaSettingsSession _session;
    /// <summary>Stores the controls and their draft setters.</summary>
    private readonly SettingsForm _form;
    /// <summary>Initializes a staged form.</summary>
    /// <param name="session">The private media session.</param>
    /// <param name="form">The typed controls for this session.</param>
    private MediaSettingsDraft(MediaSettingsSession session, SettingsForm form) { _session = session; _form = form; }
    /// <summary>Gets the translated labels and corresponding controls.</summary>
    internal List<(string Label, Control View)> Fields => _form.Fields;
    /// <summary>Copies settings and constructs this adapter's typed controls.</summary>
    /// <param name="source">The live media, whose exact type must match this adapter.</param>
    /// <returns>An isolated form ready for display.</returns>
    /// <exception cref="NotSupportedException">The source belongs to a different media adapter.</exception>
    internal static MediaSettingsDraft Create(IGXMedia source)
    {
        if (source.GetType() != typeof(GXMqtt)) { throw new NotSupportedException(Resources.Get("Unsupported")); }
        var copy = new GXMqtt();
        var session = new MediaSettingsSession(source, copy);
        try
        {
            var form = new SettingsForm();
            var available = (AvailableMediaSettings)source.ConfigurableSettings;
            if (available.HasFlag(AvailableMediaSettings.Host))
            {
                form.Text(Resources.Get("BrokerAddress"), copy.ServerAddress, value => copy.ServerAddress = value);
            }
            if (available.HasFlag(AvailableMediaSettings.Port))
            {
                form.Number(Resources.Get("Port"), copy.Port, value => copy.Port = value);
            }
            if (available.HasFlag(AvailableMediaSettings.Topic))
            {
                form.Text(Resources.Get("Topic"), copy.Topic, value => copy.Topic = value);
            }
            if (available.HasFlag(AvailableMediaSettings.ClientId))
            {
                form.Text(Resources.Get("ClientId"), copy.ClientId, value => copy.ClientId = value);
            }
            if (available.HasFlag(AvailableMediaSettings.AsyncWaitTime))
            {
                form.UnsignedNumber(Resources.Get("Timeout"), copy.AsyncWaitTime, value => copy.AsyncWaitTime = value);
            }
            return new MediaSettingsDraft(session, form);
        }
        catch { session.Dispose(); throw; }
    }
    /// <summary>Validates form values and saves them if the source remains unchanged and closed.</summary>
    internal void Commit() => _session.Commit(_form.Read);
    /// <inheritdoc/>
    public void Dispose() => _session.Dispose();
}
