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

#ifndef MESSAGE_H__
#define MESSAGE_H__

#ifdef __cplusplus
extern "C" {
#endif
    typedef struct
    {
        /**
        * Message Id.
        */
        unsigned short id;

        /**
         * Message type.
         */
        unsigned char type;

        /**
         * Sender ID.
         */
        char sender[100];

        /**
         * Sent or received frame.
         */
        char frame[1024];
    }gxMessage;

    void msg_init(gxMessage* msg)
    {
        msg->id = 0;
        msg->type = 0;
        msg->sender[0] = '\0';
        msg->frame[0] = '\0';
    }
    void msg_clear(gxMessage* msg)
    {
        msg->id = 0;
        msg->type = 0;
        msg->sender[0] = '\0';
        msg->frame[0] = '\0';
    }
#ifdef __cplusplus
}
#endif

#endif //MESSAGE_H__
