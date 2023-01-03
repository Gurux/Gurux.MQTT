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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Gurux.Broker
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                Connection settings = Gurux.Common.JSon.GXJsonParser.Load<Connection>(Path.Combine(Directory.GetCurrentDirectory(), "connections.json"));
                if (settings == null)
                {
                    settings = new Connection();
                    //Add TCP/IP connection example.
                    Media m = new Media();
                    settings.BrokerAddress = "localhost";
                    settings.BrokerPort = 1883;
                    settings.Name = Guid.NewGuid().ToString();
                    m.Name = "1";
                    m.Type = "Net";
                    m.Settings = "<IP>localhost</IP><Port>4061</Port>";
                    settings.Connections = new List<Media>();
                    settings.Connections.Add(m);

                    //Add serial port connection example
                    m = new Media();
                    m.Name = "2";
                    m.Type = "Serial";
                    m.Settings = "<Port>COM1</Port>";
                    settings.Connections.Add(m);
                    Gurux.Common.JSon.GXJsonParser.Save(settings, Path.Combine(Directory.GetCurrentDirectory(), "connections.json"));
                    return 0;
                }
                string host = settings.BrokerAddress;
                int port = settings.BrokerPort;
                TraceLevel trace = TraceLevel.Error;
                List<GXCmdParameter> parameters = GXCommon.GetParameters(args, "h:p:t:");
                foreach (GXCmdParameter it in parameters)
                {
                    switch (it.Tag)
                    {
                        case 'h':
                            //Port.
                            host = it.Value;
                            break;
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
                if (host == "")
                {
                    throw new Exception("Broker address is missing. Example -h localhost");
                }
                if (port == 0)
                {
                    throw new Exception("Broker port is missing. Example -p 1883");
                }
                Console.WriteLine("Connecting to the Broker in address: {0}:{1} ", host, port);
                var cts = new CancellationTokenSource();
                GXMeter.Start(trace, host, port, settings, cts.Token);
                Console.WriteLine("Press Esc to close application or delete clear the console.");
                ConsoleKey k;
                while ((k = Console.ReadKey().Key) != ConsoleKey.Escape)
                {
                    if (k == ConsoleKey.Delete)
                    {
                        Console.Clear();
                        GXMeter.ShowInformation(settings);
                    }
                    Console.WriteLine("Press Esc to close application or delete clear the console.");
                }
                cts.Cancel();
            }
            catch (AggregateException ex)
            {
                Console.WriteLine(ex.InnerException.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return 0;
        }

        static void ShowHelp()
        {
            Console.WriteLine("Gurux.Bridge distribute received connections to several servers.");
            Console.WriteLine("Gurux.Bridge -h Broker Address -p Broker Port numer");
            Console.WriteLine(" -h \tBroker IP address.");
            Console.WriteLine(" -p \tBroker port number.");
            Console.WriteLine(" -t [Error, Warning, Info, Verbose] Trace messages.");
            Console.WriteLine("Example:");
            Console.WriteLine("Gurux.Bridge -h localhost -p 1883");
        }
    }
}
