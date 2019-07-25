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

#ifndef ENUMS_H__
#define ENUMS_H__

#ifdef __cplusplus
extern "C" {
#endif

    /*
    * Specifies trace levels.
    *
    */
    typedef enum {
        /*
        * Output no tracing and debugging messages.
        */
        GX_TRACE_LEVEL_OFF,

        /*
        * Output error-handling messages.
        */
        GX_TRACE_LEVEL_ERROR,

        /*
        * Output warnings and error-handling messages.
        */
        GX_TRACE_LEVEL_WARNING,

        /*
        * Output informational messages, warnings, and error-handling messages.
        */
        GX_TRACE_LEVEL_INFO,

        /*
        * Output all debugging and tracing messages.
        */
        GX_TRACE_LEVEL_VERBOSE
    }GX_TRACE_LEVEL;



    /**
     * Describes message types.
     */
    typedef enum {
        /**
         * Connection is open.
         */
        GX_MESSAGE_TYPE_OPEN,
        /**
         * Message is sent by client.
         */
        GX_MESSAGE_TYPE_SEND,
        /**
         * Message is received from the meter/device.
         */
        GX_MESSAGE_TYPE_RECEIVE,
        /**
         * Connection is closed.
         */
        GX_MESSAGE_TYPE_CLOSE,
        /**
         * Occurred exception.
         */
        GX_MESSAGE_TYPE_EXCEPTION
    }GX_MESSAGE_TYPE;

    typedef enum
    {
        ERROR_CODE_OK = 0,
        //Invalid parameter.
        ERROR_CODE_INVALID_PARAMETER,
    }ERROR_CODE;
#ifdef __cplusplus
}
#endif

#endif //ENUMS_H__
