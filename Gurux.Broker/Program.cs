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
using MQTTnet;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Gurux.Broker
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                int port = 1883;
                TraceLevel trace = TraceLevel.Error;
                List<GXCmdParameter> parameters = GXCommon.GetParameters(args, "p:t:");
                foreach (GXCmdParameter it in parameters)
                {
                    switch (it.Tag)
                    {
                        case 'p':
                            //Port.
                            port = int.Parse(it.Value);
                            break;
                        case 't':
                            //Trace.
                            try
                            {
                                trace = (TraceLevel)Enum.Parse(typeof(TraceLevel), it.Value);
                            }
                            catch (Exception)
                            {
                                throw new ArgumentException("Invalid trace level option. (Error, Warning, Info, Verbose, Off)");
                            }
                            break;
                        default:
                            ShowHelp();
                            return 1;
                    }
                }
                if (port == 0)
                {
                    throw new Exception("Broker port is missing. Example -p 1883");
                }
                Console.WriteLine("Broker started in port {0}.", port);
                Run(trace, port).Wait();
                ConsoleKey k;
                while ((k = Console.ReadKey().Key) != ConsoleKey.Escape)
                {
                    if (k == ConsoleKey.Delete)
                    {
                        Console.Clear();
                    }
                    Console.WriteLine("Press Esc to close Broker or delete clear the console.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Press any key to exit.");
                Console.ReadLine();
            }
            return 0;
        }

        static void ShowHelp()
        {
            Console.WriteLine("Gurux.Broker distribute received connections to several servers.");
            Console.WriteLine("Gurux.Broker -p Port numer");
            Console.WriteLine(" -p \tport number.");
            Console.WriteLine(" -t [Error, Warning, Info, Verbose] Trace messages.");
            Console.WriteLine("Example:");
            Console.WriteLine("Gurux.Broker -p 1883");
        }

        static async Task Run(TraceLevel trace, int port)
        {
            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithConnectionBacklog(100)
                .WithDefaultEndpointPort(port);

            var mqttServer = new MqttFactory().CreateMqttServer();
            await mqttServer.StartAsync(optionsBuilder.Build());
            mqttServer.ApplicationMessageReceived += (s, e) =>
            {
                if (e.ClientId != null)
                {
                    if (trace == TraceLevel.Verbose)
                    {
                        Console.WriteLine("### Received message ###");
                        Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                        Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                    }
                }
            };
        }
    }
}
