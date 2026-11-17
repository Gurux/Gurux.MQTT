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

using System;

namespace Gurux.MQTT.Message
{
    /// <summary>
    /// Carries a bridge request or reply with its identifier, sender, frame and failure details.
    /// </summary>
    public class GXMessage
    {
        /// <summary>
        /// Message Id.
        /// </summary>
        public UInt16 id
        {
            get;
            set;
        }


        /// <summary>
        /// Message type.
        /// </summary>
        public int type
        {
            get;
            set;
        }

        /// <summary>
        /// Sender ID.
        /// </summary>
        public string? sender
        {
            get;
            set;
        }

        /// <summary>
        /// Sent or received frame.
        /// </summary>
        public string? frame
        {
            get;
            set;
        }

        /// <summary>
        /// Occurred exception.
        /// </summary>
        public string? exception
        {
            get;
            set;
        }
    }
}
