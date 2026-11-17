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
    /// <summary>Describes one bridge topic, its transport settings and current request.</summary>
    class Media
    {
        /// <summary>
        /// Media name.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Media type.
        /// </summary>
        public string Type
        {
            get;
            set;
        }

        /// <summary>
        /// Media settings.
        /// </summary>
        public string Settings
        {
            get;
            set;
        }

        /// <summary>
        /// Is optical head (probe) used to read the meter.
        /// </summary>
        public bool UseOpticalHead
        {
            get;
            set;
        }

        /// <summary>
        /// Wait time in seconds if optical head is used. Default is 5 seconds
        /// </summary>
        [DefaultValue(5)]
        public int WaitTime
        {
            get;
            set;
        }

        /// <summary>
        /// Maximum baud rate. It's not used if value is Zero.
        /// </summary>
        [DefaultValue(0)]
        public int MaximumBaudRate
        {
            get;
            set;
        }


        /// <summary>
        /// Media.
        /// </summary>
        [IgnoreDataMember]
        [System.Text.Json.Serialization.JsonIgnore]
        public GXMediaBase Target
        {
            get;
            set;
        }

        /// <summary>
        /// Last received message.
        /// </summary>
        [IgnoreDataMember]
        [System.Text.Json.Serialization.JsonIgnore]
        public GXMessage Message
        {
            get;
            set;
        }
    }
}
