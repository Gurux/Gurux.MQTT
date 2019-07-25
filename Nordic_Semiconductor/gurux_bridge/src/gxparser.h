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

#ifndef PARSER_H__
#define PARSER_H__

#include "gxmessage.h"
#include "gxenums.h"
#define _CRT_SECURE_NO_WARNINGS

#ifdef __cplusplus
extern "C" {
#endif

    char* getValue(const char* pStart, char* value)
    {
        const char* pEnd = strstr(pStart, "\"");
        if (pEnd == NULL)
        {
            return NULL;
        }
        unsigned char len = (pEnd - pStart) + 1;
        if (value != NULL)
        {
            value[len - 1] = '\0';
            memcpy(value, pStart, len - 1);
        }
        return value;
    }

    int saveMessage(const gxMessage* msg, char* data, unsigned short* len)
    {
        int ret; 
        unsigned short pos;
        *len = 0;
        ret = sprintf(data, "{\"id\":%u, \"type\":%u, \"sender\":\"%s\"", msg->id, msg->type, msg->sender);
        if (ret == -1)
        {
            return ERROR_CODE_INVALID_PARAMETER;
        }        
        pos = (unsigned short) ret;
        if (msg->frame[0] != '\0')
        {
            ret = sprintf(data + pos, ", \"frame\":\"%s\"", msg->frame);
            if (ret == -1)
            {
                return ERROR_CODE_INVALID_PARAMETER;
            }        
            pos += (unsigned short) ret;
        }
        sprintf(data + pos, "}");
        *len = pos + 1;
        return ERROR_CODE_OK;
    }

    int loadMessage(const char* data, gxMessage* result)
    {
        int ret;
        msg_clear(result);
        const char* pId = strstr(data, "\"id\":");
        if (pId != NULL)
        {
          pId += 5;
        }
        const char* pType = strstr(data, "\"type\":");
        if (pType != NULL)
        {
          pType += 7;
        }
        const char* pSender = strstr(data, "\"sender\":");
        if (pSender == NULL)
        {
            msg_clear(result);
            return ERROR_CODE_INVALID_PARAMETER;
        }
        pSender += 10;
        if (pId != NULL)
        {
          result->id = atof(pId);
        }
        else
        {
          result->id = 0;
        }
        if (pType != NULL)
        {
          result->type = atof(pType);
        }
        else
        {
          result->type = 0;
        }
        const char* pFrame = NULL;
        if (result->type == GX_MESSAGE_TYPE_SEND)
        {
            pFrame = strstr(data, "\"frame\":");
        }
        getValue(pSender, result->sender);
        if (result->sender[0] == '\0')
        {
            msg_clear(result);
            return ERROR_CODE_INVALID_PARAMETER;
        }
        if (pFrame != NULL)
        {
            pFrame += 9;
            getValue(pFrame, result->frame);
            if (result->frame[0] == '\0')
            {
                msg_clear(result);
                return ERROR_CODE_INVALID_PARAMETER;
            }
        }
        return ERROR_CODE_OK;
    }
#ifdef __cplusplus
}
#endif

#endif //PARSER_H__
