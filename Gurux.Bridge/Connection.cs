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
        public List<Media> Connections
        {
            get;
            set;
        }
    }

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
        public IGXMedia Target
        {
            get;
            set;
        }

        /// <summary>
        /// Last received message.
        /// </summary>
        [IgnoreDataMember]
        public GXMessage Message
        {
            get;
            set;
        }
    }
}
