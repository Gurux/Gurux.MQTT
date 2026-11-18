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

#nullable enable

using System;

namespace Gurux.MQTT.Message
{
    /// <summary>
    /// Carries a bridge request or reply with its identifier, sender, frame and failure details.
    /// </summary>
    public class GXMessage
    {
        /// <summary>
        /// Gets or sets the identifier used to correlate a bridge request and its reply.
        /// </summary>
        public UInt16 id
        {
            get;
            set;
        }


        /// <summary>
        /// Gets or sets the numeric bridge operation or reply code.
        /// </summary>
        public int type
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the sender identifier, or null when the message has no sender.
        /// </summary>
        public string? sender
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the encoded transport frame, or null when no payload is present.
        /// </summary>
        public string? frame
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the reported failure details, or null when no failure is reported.
        /// </summary>
        public string? exception
        {
            get;
            set;
        }
    }
}
