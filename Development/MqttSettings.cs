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

namespace Gurux.MQTT;

/// <summary>Stores an immutable, portable broker and bridge settings snapshot.</summary>
internal sealed record MqttSettings
{
    /// <summary>Gets the broker host name or IP address.</summary>
    public string ServerAddress { get; init; } = string.Empty;
    /// <summary>Gets the broker TCP port.</summary>
    public int Port { get; init; } = 1883;
    /// <summary>Gets the exact bridge meter request topic.</summary>
    public string Topic { get; init; } = string.Empty;
    /// <summary>Gets the explicit client/reply topic identifier, or empty to allocate one when opening.</summary>
    public string ClientId { get; init; } = string.Empty;
    /// <summary>Gets the operation timeout in seconds; zero disables opening and send deadlines.</summary>
    public uint AsyncWaitTime { get; init; } = 10;
}
