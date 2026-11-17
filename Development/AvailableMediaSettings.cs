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

/// <summary>Selects the MQTT fields exposed by optional settings editors.</summary>
[Flags]
public enum AvailableMediaSettings
{
    /// <summary>Exposes all configurable fields.</summary>
    All = -1,
    /// <summary>Exposes the broker TCP port.</summary>
    Port = 1,
    /// <summary>Exposes the broker host name or IP address.</summary>
    Host = 2,
    /// <summary>Exposes the bridge request topic.</summary>
    Topic = 4,
    /// <summary>Exposes the client and reply-topic identifier.</summary>
    ClientId = 8,
    /// <summary>Exposes the operation timeout in seconds.</summary>
    AsyncWaitTime = 16
}
