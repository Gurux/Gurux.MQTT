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
using MQTTnet;
using MQTTnet.Server;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Gurux.Broker
{
    /// <summary>Parses command-line options and runs the MQTT application.</summary>
    class Program
    {
        /// <summary>Trace verbosity selected by the command-line options.</summary>
        static TraceLevel _trace = TraceLevel.Error;

        /// <summary>Parses broker options and runs the application until shutdown is requested.</summary>
        /// <param name="args">Command-line options selecting broker address, port and trace verbosity.</param>
        /// <returns>Zero after normal shutdown or handled errors, or one for unsupported options.</returns>
        static async Task<int> Main(string[] args)
        {
            try
            {
                int port = 1883;
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
                                _trace = (TraceLevel)Enum.Parse(typeof(TraceLevel), it.Value);
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
                if (port == 0)
                {
                    throw new Exception(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("BrokerPortRequired", global::Gurux.Broker.Properties.Resources.Culture));
                }
                Console.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("BrokerStarted", global::Gurux.Broker.Properties.Resources.Culture), port));
                using MqttServer mqttServer = await Run(port);
                ConsoleKey k;
                while ((k = Console.ReadKey().Key) != ConsoleKey.Escape)
                {
                    if (k == ConsoleKey.Delete)
                    {
                        Console.Clear();
                    }
                    Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("BrokerControls", global::Gurux.Broker.Properties.Resources.Culture));
                }
                await mqttServer.StopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("ExitPrompt", global::Gurux.Broker.Properties.Resources.Culture));
                Console.ReadLine();
            }
            return 0;
        }

        /// <summary>Writes command-line usage and an example invocation to the console.</summary>
        static void ShowHelp()
        {
            Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("BrokerHelpDescription", global::Gurux.Broker.Properties.Resources.Culture));
            Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("BrokerUsage", global::Gurux.Broker.Properties.Resources.Culture));
            Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("BrokerPortOption", global::Gurux.Broker.Properties.Resources.Culture));
            Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("TraceOption", global::Gurux.Broker.Properties.Resources.Culture));
            Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("ExampleHeading", global::Gurux.Broker.Properties.Resources.Culture));
            Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("BrokerExample", global::Gurux.Broker.Properties.Resources.Culture));
        }

        /// <summary>Creates and starts a broker listening on the requested port.</summary>
        /// <param name="port">MQTT broker TCP port.</param>
        /// <returns>A task returning the started MQTT server.</returns>
        static async Task<MqttServer> Run(int port)
        {
            var optionsBuilder = new MqttServerOptionsBuilder()
            .WithConnectionBacklog(100)
            .WithDefaultEndpointPort(port)
            .Build();
            optionsBuilder.DefaultEndpointOptions.IsEnabled = true;
            var mqttServer = new MqttServerFactory().CreateMqttServer(optionsBuilder);
            mqttServer.InterceptingPublishAsync += args =>
            {
                if (args.ClientId != null)
                {
                    if (_trace == TraceLevel.Verbose)
                    {
                        Console.WriteLine(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("ReceivedMessage", global::Gurux.Broker.Properties.Resources.Culture));
                        Console.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("TopicDetail", global::Gurux.Broker.Properties.Resources.Culture), args.ApplicationMessage.Topic));
                        Console.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("PayloadDetail", global::Gurux.Broker.Properties.Resources.Culture), Encoding.UTF8.GetString(args.ApplicationMessage.Payload.ToArray())));
                    }
                }
                return Task.CompletedTask;
            };
            mqttServer.ClientConnectedAsync += OnClientConnectedAsync;
            await mqttServer.StartAsync();
            return mqttServer;
        }

        /// <summary>Writes a client connection notification when verbose tracing is enabled.</summary>
        /// <param name="arg">Details of the newly connected MQTT client.</param>
        /// <returns>An already completed task after handling the notification.</returns>
        private static Task OnClientConnectedAsync(ClientConnectedEventArgs arg)
        {
            if (_trace == TraceLevel.Verbose)
            {
                Console.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("ClientConnected", global::Gurux.Broker.Properties.Resources.Culture), arg.ClientId));
            }
            return Task.CompletedTask;
        }
    }
}
