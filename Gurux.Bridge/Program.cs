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

using Gurux.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace Gurux.Broker
{
    /// <summary>Parses command-line options and runs the MQTT application.</summary>
    class Program
    {
        /// <summary>Parses broker options and runs the application until shutdown is requested.</summary>
        /// <param name="args">Command-line options selecting broker address, port and trace verbosity.</param>
        /// <returns>Zero after normal shutdown or handled errors, or one for unsupported options.</returns>
        static async Task<int> Main(string[] args)
        {
            try
            {
                Connection settings = File.Exists("connections.json") ? JsonSerializer.Deserialize<Connection>(await File.ReadAllTextAsync("connections.json")) : null;
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
                    await File.WriteAllTextAsync("connections.json", JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
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
                                throw new ArgumentException(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("InvalidTraceLevel", global::Gurux.Broker.Properties.Resources.Culture));
                            }
                            break;
                        default:
                            ShowHelp();
                            return 1;
                    }
                }
                if (host == "")
                {
                    throw new Exception(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("BrokerAddressRequired", global::Gurux.Broker.Properties.Resources.Culture));
                }
                if (port == 0)
                {
                    throw new Exception(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("BrokerPortRequired", global::Gurux.Broker.Properties.Resources.Culture));
                }
                Console.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("ConnectingToBroker", global::Gurux.Broker.Properties.Resources.Culture), host, port));
                using var cts = new CancellationTokenSource();
                Task bridge = GXMeter.RunAsync(trace, host, port, settings, cts.Token);
                Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("BridgeControls", global::Gurux.Broker.Properties.Resources.Culture));
                ConsoleKey k;
                while ((k = Console.ReadKey().Key) != ConsoleKey.Escape)
                {
                    if (k == ConsoleKey.Delete)
                    {
                        Console.Clear();
                        GXMeter.ShowInformation(settings);
                    }
                    Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("BridgeControls", global::Gurux.Broker.Properties.Resources.Culture));
                }
                cts.Cancel();
                await bridge;
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

        /// <summary>Writes command-line usage and an example invocation to the console.</summary>
        static void ShowHelp()
        {
            Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("BridgeHelpDescription", global::Gurux.Broker.Properties.Resources.Culture));
            Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("BridgeUsage", global::Gurux.Broker.Properties.Resources.Culture));
            Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("BridgeHostOption", global::Gurux.Broker.Properties.Resources.Culture));
            Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("BridgePortOption", global::Gurux.Broker.Properties.Resources.Culture));
            Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("TraceOption", global::Gurux.Broker.Properties.Resources.Culture));
            Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("ExampleHeading", global::Gurux.Broker.Properties.Resources.Culture));
            Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("BridgeExample", global::Gurux.Broker.Properties.Resources.Culture));
        }
    }
}
