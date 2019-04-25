//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System;

namespace Gurux.MQTT.Message
{
    /// <summary>
    /// Describes available settings for the media.
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
        public string sender
        {
            get;
            set;
        }

        /// <summary>
        /// Sent or received frame.
        /// </summary>
        public string frame
        {
            get;
            set;
        }

        /// <summary>
        /// Occurred exception.
        /// </summary>
        public string exception
        {
            get;
            set;
        }
    }
}
