﻿//
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
using Gurux.Common.JSon;
using Gurux.MQTT.Message;
using Gurux.Net;
using Gurux.Serial;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace Gurux.Broker
{
    class GXMeter
    {
        static readonly MqttFactory factory = new MqttFactory();

        public static void ShowInformation(Connection connection)
        {
            Console.WriteLine("Bridge topic: " + connection.Name);
            // Subscribe to a topic
            foreach (Media it in connection.Connections)
            {
                Console.WriteLine("Media topic: " + it.Name);
                Console.WriteLine("Settings: " + it.Target.ToString());
            }
        }

        public static async void Start(
            TraceLevel trace,
            string server,
            int port,
            Connection connection,
            CancellationToken cancellationToken)
        {
            // Create a new MQTT client.
            var mqttClient = factory.CreateMqttClient();
            int pos = 1;
            foreach (var it in connection.Connections)
            {
                if (it.Type == "Net")
                {
                    it.Target = new GXNet() { Settings = it.Settings };
                }
                else if (it.Type == "Serial")
                {
                    it.Target = new GXSerial() { Settings = it.Settings };
                }
                else
                {
                    throw new Exception("Unknown media type." + it.Type);
                }
                if (string.IsNullOrEmpty(it.Name))
                {
                    it.Name = connection.Name + "/" + pos.ToString();
                    ++pos;
                }
                else
                {
                    it.Name = connection.Name + "/" + it.Name;
                }
                it.Target.OnReceived += (s, e) =>
                {
                    string tmp = GXCommon.ToHex((byte[])e.Data, true);
                    if (trace == TraceLevel.Verbose)
                    {
                        Console.WriteLine("Received: " + tmp);
                    }
                    foreach (var it2 in connection.Connections)
                    {
                        if (it2.Target == s)
                        {
                            GXMessage msg = new GXMessage() { id = it2.Message.id, type = (int)MessageType.Receive, sender = it2.Name, frame = tmp };
                            GXJsonParser parser = new GXJsonParser();
                            string str = parser.Serialize(msg);
                            var message = new MqttApplicationMessageBuilder()
                            .WithTopic(it2.Message.sender)
                            .WithPayload(str)
                            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                            .Build();
                            mqttClient.PublishAsync(message, cancellationToken);
                            break;
                        }
                    }
                };
            }
            ShowInformation(connection);

            var options = new MqttClientOptionsBuilder()
            .WithTcpServer(server, port)
            .WithClientId(connection.Name)
            .Build();
            mqttClient.ApplicationMessageReceivedAsync += async t =>
            {
                string str = ASCIIEncoding.ASCII.GetString(t.ApplicationMessage.Payload);
                GXJsonParser parser = new GXJsonParser();
                GXMessage msg = parser.Deserialize<GXMessage>(str);
                if (trace == TraceLevel.Verbose)
                {
                    Console.WriteLine("---Received message");
                    Console.WriteLine($"+ Topic = {t.ApplicationMessage.Topic}");
                    Console.WriteLine($"+ Type = {(MessageType)msg.type}");
                    Console.WriteLine($"+ Payload = {msg.frame}");
                    Console.WriteLine($"+ QoS = {t.ApplicationMessage.QualityOfServiceLevel}");
                    Console.WriteLine($"+ Retain = {t.ApplicationMessage.Retain}");
                }
                GXMessage msg2;
                MqttApplicationMessage message;
                foreach (var it in connection.Connections)
                {
                    if (it.Name == t.ApplicationMessage.Topic)
                    {
                        it.Message = msg;
                        try
                        {
                            switch ((MessageType)msg.type)
                            {
                                case MessageType.Open:
                                    it.Target.Open();
                                    try
                                    {
                                        //Move to mode E if optical head is used.
                                        if (it.Target is GXSerial && it.UseOpticalHead)
                                        {
                                            InitializeIEC(trace, it);
                                        }
                                        if (it.Target is IGXMedia2)
                                        {
                                            ((IGXMedia2)it.Target).ReceiveDelay = 500;
                                        }
                                        //Mark EOP so reading is faster.
                                        //   it.Target.Eop = (byte)0x7e;
                                    }
                                    catch (Exception ex)
                                    {
                                        it.Target.Close();
                                        throw ex;
                                    }
                                    msg2 = new GXMessage() { id = msg.id, type = (int)MessageType.Open, sender = it.Name };
                                    str = parser.Serialize(msg2);
                                    message = new MqttApplicationMessageBuilder()
                                    .WithTopic(msg.sender)
                                    .WithPayload(str)
                                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                                    .Build();
                                    await mqttClient.PublishAsync(message, cancellationToken);
                                    break;
                                case MessageType.Send:
                                    it.Target.Send(GXCommon.HexToBytes(msg.frame), null);
                                    break;
                                case MessageType.Close:
                                    it.Target.Close();
                                    msg2 = new GXMessage() { id = msg.id, type = (int)MessageType.Close, sender = it.Name };
                                    str = parser.Serialize(msg2);
                                    message = new MqttApplicationMessageBuilder()
                                    .WithTopic(msg.sender)
                                    .WithPayload(str)
                                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                                    .Build();
                                    await mqttClient.PublishAsync(message, cancellationToken);
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            msg2 = new GXMessage() { id = msg.id, type = (int)MessageType.Exception, sender = it.Name, exception = ex.Message };
                            str = parser.Serialize(msg2);
                            message = new MqttApplicationMessageBuilder()
                            .WithTopic(msg.sender)
                            .WithPayload(str)
                            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                            .Build();
                            await mqttClient.PublishAsync(message, cancellationToken);
                        }
                        break;
                    }
                }
            };
            mqttClient.ConnectedAsync += async t =>
            {
                if (trace > TraceLevel.Warning)
                {
                    Console.WriteLine("--- Connected with the server. " + t.ConnectResult.IsSessionPresent);
                }
                // Subscribe to a topic(s)
                foreach (Media it in connection.Connections)
                {
                    var mqttSubscribeOptions = factory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(
                    f =>
                    {
                        f.WithTopic(it.Name);
                    })
                .Build();
                    await mqttClient.SubscribeAsync(mqttSubscribeOptions, cancellationToken);
                    if (trace > TraceLevel.Warning)
                    {
                        Console.WriteLine("--- {0} Subscribed. ", it.Name);
                    }
                }
            };
            mqttClient.DisconnectedAsync += async t =>
           {
               Console.WriteLine("--- Disconnected from the server. " + t.Exception);
               //Try to re-connect.
               while (true)
               {
                   try
                   {
                       foreach (var it in connection.Connections)
                       {
                           it.Target.Close();
                       }
                       Console.WriteLine("--- Try to reconnect to the server.");
                       await mqttClient.ConnectAsync(options, cancellationToken);
                       Console.WriteLine("--- Connected with the server. ");
                       break;
                   }
                   catch (Exception ex)
                   {
                       Console.WriteLine("--- Failed to reconnect to the server. " + ex.Message);
                   }
               }
           };
            await mqttClient.ConnectAsync(options, cancellationToken);
        }

        private static void InitializeIEC(TraceLevel trace, Media media)
        {
            GXSerial serial = (GXSerial)media.Target;
            serial.BaudRate = 300;
            serial.DataBits = 7;
            serial.Parity = Parity.Even;
            serial.StopBits = StopBits.One;
            byte Terminator = (byte)0x0A;
            //Some meters need a little break.
            Thread.Sleep(1000);
            //Query device information.
            string data = "/?!\r\n";
            WriteLog(trace, "IEC Sending:" + data);
            if (media.WaitTime == 0)
            {
                media.WaitTime = 5;
            }
            ReceiveParameters<string> p = new ReceiveParameters<string>()
            {
                AllData = false,
                Eop = Terminator,
                WaitTime = media.WaitTime * 1000
            };
            lock (media.Target.Synchronous)
            {
                media.Target.Send(data, null);
                if (!media.Target.Receive(p))
                {
                    DiscIEC(media);
                    string str = "Failed to receive reply from the device in given time.";
                    WriteLog(trace, str);
                    media.Target.Send(data, null);
                    if (!media.Target.Receive(p))
                    {
                        throw new Exception(str);
                    }
                }
                //If echo is used.
                if (p.Reply == data)
                {
                    p.Reply = null;
                    if (!media.Target.Receive(p))
                    {
                        data = "Failed to receive reply from the device in given time.";
                        WriteLog(trace, data);
                        throw new Exception(data);
                    }
                }
            }
            WriteLog(trace, "IEC received: " + p.Reply);
            if (p.Reply[0] != '/')
            {
                p.WaitTime = 100;
                media.Target.Receive(p);
                throw new Exception("Invalid responce.");
            }
            string manufactureID = p.Reply.Substring(1, 3);
            char baudrate = p.Reply[4];
            int BaudRate;
            switch (baudrate)
            {
                case '0':
                    BaudRate = 300;
                    break;
                case '1':
                    BaudRate = 600;
                    break;
                case '2':
                    BaudRate = 1200;
                    break;
                case '3':
                    BaudRate = 2400;
                    break;
                case '4':
                    BaudRate = 4800;
                    break;
                case '5':
                    BaudRate = 9600;
                    break;
                case '6':
                    BaudRate = 19200;
                    break;
                default:
                    throw new Exception("Unknown baud rate.");
            }
            if (media.MaximumBaudRate != 0)
            {
                BaudRate = media.MaximumBaudRate;
                baudrate = GetIecBaudRate(BaudRate);
                WriteLog(trace, "Maximum BaudRate is set to : " + BaudRate.ToString());
            }
            WriteLog(trace, "BaudRate is : " + BaudRate.ToString());
            //Send ACK
            //Send Protocol control character
            // "2" HDLC protocol procedure (Mode E)
            byte controlCharacter = (byte)'2';
            //Send Baud rate character
            //Mode control character
            byte ModeControlCharacter = (byte)'2';
            //"2" //(HDLC protocol procedure) (Binary mode)
            //Set mode E.
            byte[] arr = new byte[] { 0x06, controlCharacter, (byte)baudrate, ModeControlCharacter, 13, 10 };
            WriteLog(trace, "Moving to mode E. " + GXCommon.ToHex(arr));
            lock (media.Target.Synchronous)
            {
                p.Reply = null;
                media.Target.Send(arr, null);
                p.WaitTime = 2000;
                //Note! All meters do not echo this.
                media.Target.Receive(p);
                if (p.Reply != null)
                {
                    WriteLog(trace, "Received: " + p.Reply);
                }
                media.Target.Close();
                serial.BaudRate = BaudRate;
                serial.DataBits = 8;
                serial.Parity = Parity.None;
                serial.StopBits = StopBits.One;
                serial.Open();
                //Some meters need this sleep. Do not remove.
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Send IEC disconnect message.
        /// </summary>
        private static void DiscIEC(Media media)
        {
            ReceiveParameters<string> p = new ReceiveParameters<string>()
            {
                AllData = false,
                Eop = (byte)0x0A,
                WaitTime = media.WaitTime * 1000
            };
            string data = (char)0x01 + "B0" + (char)0x03 + "\r\n";
            media.Target.Send(data, null);
            p.Count = 1;
            media.Target.Receive(p);
        }


        private static char GetIecBaudRate(int baudrate)
        {
            char rate;
            switch (baudrate)
            {
                case 300:
                    rate = '0';
                    break;
                case 600:
                    rate = '1';
                    break;
                case 1200:
                    rate = '2';
                    break;
                case 2400:
                    rate = '3';
                    break;
                case 4800:
                    rate = '4';
                    break;
                case 9600:
                    rate = '5';
                    break;
                case 19200:
                    rate = '6';
                    break;
                default:
                    throw new Exception("Unknown baud rate.");
            }
            return rate;
        }


        static void WriteLog(TraceLevel trace, string message)
        {
            if (trace > TraceLevel.Warning)
            {
                Console.WriteLine(message);
            }
        }
    }
}
