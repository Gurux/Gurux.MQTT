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
using Gurux.MQTT.Message;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.Broker
{
    /// <summary>
    /// Connection settings.
    /// </summary>
    class Connection
    {
        /// <summary>
        /// Broker address.
        /// </summary>
        public string BrokerAddress
        {
            get;
            set;
        }
        /// <summary>
        /// Broker port.
        /// </summary>
        public int BrokerPort
        {
            get;
            set;
        }

        /// <summary>
        /// (Unique) Bridge name.
        /// </summary>
        public string Name
        {
            get;
            set;
        }
        /// <summary>Gets or sets the media connections exposed by this bridge.</summary>
        public List<Media> Connections
        {
            get;
            set;
        }
    }
}
