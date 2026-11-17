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
using System.Threading;
using Gurux.Common;

namespace Gurux.Shared
{
    /// <summary>Stores incoming bytes and waits for synchronous receive conditions.</summary>
    class GXSynchronousMediaBase
    {
        /// <summary>Pending transport failure rethrown by synchronous receive operations.</summary>
        internal Exception Exception;
        /// <summary>Owned storage containing received bytes awaiting consumption.</summary>
        internal byte[] m_Received = null;
        /// <summary>Signals that new bytes or connection shutdown are available to receivers.</summary>
        internal AutoResetEvent receivedEvent = new AutoResetEvent(false);
        /// <summary>Serializes access to receive storage.</summary>
        internal readonly object receivedSync = new object();
        /// <summary>Number of unread bytes, or -1 when the connection is closed.</summary>
        internal int receivedSize;
        /// <summary>Last scanned offset used to continue end-of-packet searches.</summary>
        internal int lastPosition;
        /// <summary>
        /// Trace level.
        /// </summary>
        public System.Diagnostics.TraceLevel Trace;

        /// <summary>Allocates receive storage with the requested initial capacity.</summary>
        /// <param name="bufferSize">Initial receive storage capacity in bytes.</param>
        public GXSynchronousMediaBase(int bufferSize)
        {
            m_Received = new byte[bufferSize];
            lastPosition = 0;
        }

        /// <summary>
        /// Connection is closed.
        /// </summary>
        public void Close()
        {
            lock (receivedSync)
            {
                receivedSize = -1;
                receivedEvent.Set();
            }
        }

        /// <summary>
        /// Connection is open.
        /// </summary>
        public void Open()
        {
            lock (receivedSync)
            {
                receivedEvent.Reset();
                receivedSize = lastPosition = 0;
            }
        }

        /// <summary>Copies a range of source bytes into the receive storage.</summary>
        /// <param name="data">Payload bytes to copy or publish.</param>
        /// <param name="index">Offset of the first source byte.</param>
        /// <param name="count">Number of source values or bytes to process.</param>
        public void AppendData(byte[] data, int index, int count)
        {
            lock (receivedSync)
            {
                //Alocate new buffer.
                if (receivedSize + count > m_Received.Length)
                {
                    byte[] tmp = new byte[2 * m_Received.Length];
                    Array.Copy(m_Received, 0, tmp, 0, receivedSize);
                    m_Received = tmp;
                }
                Array.Copy(data, index, m_Received, receivedSize, count);
                receivedSize += count - index;
            }
        }

        /// <summary>Waits for receive conditions and appends decoded bytes to the supplied reply.</summary>
        /// <param name="args">Receive conditions and accumulated reply to update.</param>
        /// <typeparam name="T">Managed reply type supported by the byte conversion helper.</typeparam>
        /// <returns>Whether the requested receive conditions were satisfied.</returns>
        public bool Receive<T>(ReceiveParameters<T> args)
        {
            if (args.Eop == null && args.Count == 0 && !args.AllData)
            {
                throw new ArgumentException(global::Gurux.MQTT.Properties.Resources.ResourceManager.GetString("ReceiveConditionsRequired", global::Gurux.MQTT.Properties.Resources.Culture));
            }
            int nSize = 0;
            byte[] terminator = null;
            if (args.Eop != null)
            {
                if (args.Eop is Array)
                {
                    Array arr = args.Eop as Array;
                    terminator = GXCommon.GetAsByteArray(arr.GetValue(0));
                }
                else
                {
                    terminator = GXCommon.GetAsByteArray(args.Eop);
                }
                nSize = terminator.Length;
            }

            int nMinSize = (int)Math.Max(args.Count, nSize);
            if (nMinSize == 0)
            {
                nMinSize = 1;
            }
            int waitTime = args.WaitTime;
            if (waitTime <= 0)
            {
                waitTime = -1;
            }

            //Wait until reply occurred.		
            int nFound = -1;
            int LastBuffSize = 0;
            DateTime StartTime = DateTime.Now;
            bool retValue = true;
            do
            {
                if (waitTime == 0)
                {
                    //If we do not want to read all data.
                    if (!args.AllData)
                    {
                        return false;
                    }
                    retValue = false;
                    break;
                }
                if (waitTime != -1)
                {
                    waitTime -= (int)(DateTime.Now - StartTime).TotalMilliseconds;
                    StartTime = DateTime.Now;
                    if (waitTime < 0)
                    {
                        waitTime = 0;
                    }
                }
                bool received;
                lock (receivedSync)
                {
                    received = !(LastBuffSize == receivedSize || receivedSize < nMinSize);
                }
                //Do not wait if there is data on the buffer...
                if (!received)
                {
                    if (waitTime == -1)
                    {
                        received = receivedEvent.WaitOne();
                    }
                    else
                    {
                        received = receivedEvent.WaitOne(waitTime);
                    }
                    if (!received && args.Eop == null)
                    {
                        lock (receivedSync)
                        {
                            received = !(LastBuffSize == receivedSize || receivedSize < nMinSize);
                        }
                    }
                }
                if (received)
                {
                    lock (receivedSync)
                    {
                        if (receivedSize == -1)
                        {
                            receivedSize = 0;
                            return false;
                        }
                    }
                }
                if (this.Exception != null)
                {
                    Exception ex = this.Exception;
                    this.Exception = null;
                    throw ex;
                }
                //If timeout occurred.
                if (!received)
                {
                    //If we do not want to read all data.
                    if (!args.AllData)
                    {
                        return false;
                    }
                    retValue = false;
                    break;
                }
                lock (receivedSync)
                {
                    LastBuffSize = receivedSize;
                    //Read more data, if not enough
                    if (receivedSize < nMinSize)
                    {
                        continue;
                    }
                    //If only byte count matters.
                    if (nSize == 0)
                    {
                        nFound = args.Count;
                    }
                    else
                    {
                        int index = lastPosition != 0 && lastPosition < receivedSize ? lastPosition : args.Count;
                        //If terminator found.
                        if (args.Eop is Array)
                        {
                            foreach (object it in args.Eop as Array)
                            {
                                byte[] term = GXCommon.GetAsByteArray(it);
                                if (term.Length != 1 && receivedSize - index < term.Length)
                                {
                                    index = receivedSize - term.Length;
                                }
                                nFound = GXCommon.IndexOf(m_Received, term, index, receivedSize);
                                if (nFound != -1)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (terminator.Length != 1 && receivedSize - index < terminator.Length)
                            {
                                index = receivedSize - terminator.Length;
                            }
                            nFound = GXCommon.IndexOf(m_Received, terminator, index, receivedSize);
                        }
                        lastPosition = receivedSize;
                        if (nFound != -1)
                        {
                            nFound += terminator.Length;
                        }
                    }
                }
            }
            while (nFound == -1);
            if (nSize == 0) //If terminator is not given read only bytes that are needed.
            {
                nFound = args.Count;
            }
            if (args.AllData) //If all data is copied.
            {
                nFound = receivedSize;
            }
            if (nFound == 0)
            {
                retValue = false;
            }
            //Convert bytes to object.
            byte[] tmp = new byte[nFound];
            lock (receivedSync)
            {
                Array.Copy(m_Received, tmp, nFound);
            }
            int readBytes = 0;
            object data = GXCommon.ByteArrayToObject(tmp, typeof(T), out readBytes);
            //Remove read data.
            receivedSize -= nFound;
            //Received size can go less than zero if we have received data and we try to read more.
            if (receivedSize < 0)
            {
                receivedSize = 0;
            }
            if (receivedSize != 0)
            {
                lock (receivedSync)
                {
                    Array.Copy(m_Received, nFound, m_Received, 0, receivedSize);
                }
            }
            else
            {
                lastPosition = 0;
            }
            //Reset count after read.
            args.Count = 0;
            //Append data.
            int oldReplySize = 0;
            if (args.Reply == null)
            {
                args.Reply = (T)data;
            }
            else
            {
                if (args.Reply is Array)
                {
                    Array oldArray = args.Reply as Array;
                    Array newArray = data as Array;
                    if (newArray == null)
                    {
                        throw new ArgumentException();
                    }
                    oldReplySize = oldArray.Length;
                    int len = oldArray.Length + newArray.Length;
                    Array arr = (Array)Activator.CreateInstance(typeof(T), len);
                    //Copy old values.
                    Array.Copy(args.Reply as Array, arr, oldArray.Length);
                    //Copy new values.
                    Array.Copy(newArray, 0, arr, oldArray.Length, newArray.Length);
                    object tmp2 = arr;
                    args.Reply = (T)tmp2;
                }
                else if (args.Reply is string)
                {
                    string str = args.Reply as string;
                    str += (string)data;
                    data = str;
                    args.Reply = (T)data;
                }
            }
            return retValue;
        }
    }
}
