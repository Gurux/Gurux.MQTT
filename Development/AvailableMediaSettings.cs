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

namespace Gurux.MQTT
{
    /// <summary>
    /// Describes available settings for the media.
    /// </summary>
    [Flags]
    public enum AvailableMediaSettings
    {
        /// <summary>
        /// All network properties are shown.
        /// </summary>
        All = -1,
        /// <summary>
        /// Port number is shown.
        /// </summary>
        Port = 0x1,
        /// <summary>
        /// Host Name is shown.
        /// </summary>
        Host = 0x2,
		/// <summary>
		/// Is topic shown.
		/// </summary>
		Topic = 0x4
    }
}
