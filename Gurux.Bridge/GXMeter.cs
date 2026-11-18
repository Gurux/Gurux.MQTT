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
using Gurux.MQTT.Message;
using Gurux.Net;
using Gurux.Serial;
using System.Buffers;
using MQTTnet;
using MQTTnet.Protocol;
using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Gurux.Broker
{
    /// <summary>Owns broker connectivity and independent serialized meter sessions.</summary>
    class GXMeter
    {
        /// <summary>Displays the configured meter topics.</summary>
        /// <param name="connection">Configuration to display.</param>
        public static void ShowInformation(Connection connection)
        {
            foreach (Media media in connection.Connections) { Console.WriteLine(media.Name); }
        }

        /// <summary>Runs the bridge and awaits complete shutdown after cancellation.</summary>
        /// <param name="trace">Diagnostic verbosity.</param>
        /// <param name="server">Broker host.</param>
        /// <param name="port">Broker port.</param>
        /// <param name="connection">Meter configuration.</param>
        /// <param name="cancellationToken">Requests shutdown.</param>
        /// <returns>The bridge lifetime.</returns>
        /// <exception cref="ArgumentException">A configured transport type or its settings are invalid.</exception>
        public static async Task RunAsync(TraceLevel trace, string server, int port, Connection connection, CancellationToken cancellationToken)
        {
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken token = lifetime.Token;
            var factory = new MqttClientFactory();
            using var client = factory.CreateMqttClient();
            var sessions = new List<Session>();
            int position = 0;
            try
            {
                foreach (Media media in connection.Connections)
                {
                    ++position;
                    if (string.IsNullOrEmpty(media.Name) || !media.Name.StartsWith(connection.Name + "/", StringComparison.Ordinal))
                    {
                        media.Name = connection.Name + "/" + (string.IsNullOrEmpty(media.Name) ? position.ToString(System.Globalization.CultureInfo.InvariantCulture) : media.Name);
                    }
                    media.Target = media.Type switch
                    {
                        "Net" => new GXNet { Settings = media.Settings },
                        "Serial" => new GXSerial { Settings = media.Settings },
                        _ => throw new ArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Text("UnknownMediaType"), media.Type))
                    };
                    sessions.Add(new Session(media, client, lifetime, trace));
                }
            }
            catch
            {
                lifetime.Cancel();
                foreach (Session session in sessions) { await session.StopAsync(); }
                throw;
            }
            client.ApplicationMessageReceivedAsync += args =>
            {
                try
                {
                    if (args.ApplicationMessage.Retain) { return Task.CompletedTask; }
                    GXMessage message = JsonSerializer.Deserialize<GXMessage>(args.ApplicationMessage.Payload.ToArray());
                    if (message != null && !string.IsNullOrEmpty(message.sender))
                    {
                        foreach (Session session in sessions)
                        {
                            if (session.Media.Name == args.ApplicationMessage.Topic) { session.Enqueue(message); }
                        }
                    }
                }
                catch (JsonException) { }
                return Task.CompletedTask;
            };
            try
            {
                var options = new MqttClientOptionsBuilder().WithTcpServer(server, port).WithClientId(connection.Name).Build();
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await client.ConnectAsync(options, token);
                        foreach (Session session in sessions)
                        {
                            await client.SubscribeAsync(factory.CreateSubscribeOptionsBuilder().WithTopicFilter(f => f.WithTopic(session.Media.Name)).Build(), token);
                        }
                        while (client.IsConnected) { await Task.Delay(100, token); }
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
                    catch (Exception ex) { WriteLog(trace, ex.Message); }
                    foreach (Session session in sessions) { await session.ResetAsync(); }
                    await Task.Delay(1000, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            finally
            {
                lifetime.Cancel();
                foreach (Session session in sessions) { await session.StopAsync(); }
                if (client.IsConnected) { await client.DisconnectAsync(); }
            }
            foreach (Session session in sessions)
            {
                if (session.Failure != null && !cancellationToken.IsCancellationRequested) { throw session.Failure; }
            }
        }

        /// <summary>Looks up a translated bridge message.</summary>
        /// <param name="key">Resource identifier.</param>
        /// <returns>The translated message.</returns>
        private static string Text(string key) => Properties.Resources.ResourceManager.GetString(key, Properties.Resources.Culture);

        /// <summary>Serializes one meter's requests and keeps its owner stable until close.</summary>
        private sealed class Session
        {
            /// <summary>The configured transport.</summary>
            public readonly Media Media;
            /// <summary>Bounded queue separating MQTT dispatch from transport operations.</summary>
            private readonly Channel<GXMessage> _requests = Channel.CreateBounded<GXMessage>(64);
            /// <summary>Bounded queue for received transport frames.</summary>
            private readonly Channel<(GXMessage Owner, byte[] Data)> _received = Channel.CreateBounded<(GXMessage, byte[])>(128);
            /// <summary>Records a bounded queue or worker failure for the bridge lifetime task.</summary>
            public Exception Failure;
            /// <summary>MQTT connection owned by the bridge.</summary>
            private readonly IMqttClient _client;
            /// <summary>Bridge cancellation source, also canceled on bounded queue overflow.</summary>
            private readonly CancellationTokenSource _lifetime;
            /// <summary>Gate serializing reset with requests.</summary>
            private readonly SemaphoreSlim _gate = new(1, 1);
            /// <summary>Open request whose sender exclusively owns the transport.</summary>
            private GXMessage _owner;
            /// <summary>Cancellation for the currently open transport operation.</summary>
            private CancellationTokenSource _sessionCancellation;
            /// <summary>Protects cancellation source replacement against dispatch callbacks.</summary>
            private readonly object _cancellationSync = new();
            /// <summary>Transport request worker.</summary>
            private readonly Task _worker;
            /// <summary>Receive publication worker.</summary>
            private readonly Task _publisher;
            /// <summary>Diagnostic verbosity.</summary>
            private readonly TraceLevel _trace;
            /// <summary>Creates workers and attaches the bounded receive callback.</summary>
            /// <param name="media">Meter transport.</param>
            /// <param name="mqtt">Shared MQTT client.</param>
            /// <param name="cancellation">Bridge cancellation.</param>
            /// <param name="verbosity">Diagnostic verbosity.</param>
            public Session(Media media, IMqttClient mqtt, CancellationTokenSource cancellation, TraceLevel verbosity)
            {
                Media = media; _client = mqtt; _lifetime = cancellation; _trace = verbosity;
                Media.Target.OnReceived += Received;
                _worker = WorkAsync(); _publisher = PublishReceivedAsync();
            }
            /// <summary>Queues a request and interrupts an owner-requested close without blocking MQTT dispatch.</summary>
            /// <param name="request">Incoming protocol request.</param>
            /// <returns>Whether the bounded request queue accepted the request.</returns>
            public bool Enqueue(GXMessage request)
            {
                GXMessage current = Volatile.Read(ref _owner);
                if ((MessageType)request.type == MessageType.Close && current != null && current.sender == request.sender)
                {
                    CancelSession();
                }
                bool accepted = _requests.Writer.TryWrite(request);
                if (!accepted) { Fail(new System.IO.IOException(Text("BridgeQueueFull"))); }
                return accepted;
            }
            /// <summary>Snapshots the current owner and copies a received frame without blocking dispatch.</summary>
            /// <param name="sender">Transport producing the frame.</param>
            /// <param name="args">Received frame.</param>
            private void Received(object sender, ReceiveEventArgs args)
            {
                GXMessage current = Volatile.Read(ref _owner);
                if (current != null && !_received.Writer.TryWrite((current, ((byte[])args.Data).AsSpan().ToArray()))) { Fail(new System.IO.IOException(Text("BridgeQueueFull"))); }
            }
            /// <summary>Publishes a protocol reply to its requesting sender.</summary>
            /// <param name="request">Request identifying the recipient and correlation identifier.</param>
            /// <param name="type">Reply operation.</param>
            /// <param name="frame">Optional hexadecimal frame.</param>
            /// <param name="error">Optional localized failure.</param>
            /// <returns>The publication operation.</returns>
            private async Task ReplyAsync(GXMessage request, MessageType type, string frame = null, string error = null)
            {
                var reply = new GXMessage { id = request.id, sender = Media.Name, type = (int)type, frame = frame, exception = error };
                await _client.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(request.sender).WithPayload(JsonSerializer.Serialize(reply)).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce).Build(), _lifetime.Token);
            }
            /// <summary>Publishes transport frames only while the snapshotted session still owns the meter.</summary>
            /// <returns>The receive worker lifetime.</returns>
            private async Task PublishReceivedAsync()
            {
                try
                {
                    await foreach (var item in _received.Reader.ReadAllAsync(_lifetime.Token))
                    {
                        if (ReferenceEquals(item.Owner, Volatile.Read(ref _owner))) { await ReplyAsync(item.Owner, MessageType.Receive, Convert.ToHexString(item.Data)); }
                    }
                }
                catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
                catch (Exception ex) { Fail(ex); }
            }
            /// <summary>Processes requests sequentially, rejects foreign owners and reopens a new same-owner generation.</summary>
            /// <returns>The request worker lifetime.</returns>
            private async Task WorkAsync()
            {
                try
                {
                    await foreach (GXMessage request in _requests.Reader.ReadAllAsync(_lifetime.Token))
                    {
                        await _gate.WaitAsync(_lifetime.Token);
                        try
                        {
                            if (_owner != null && _owner.sender != request.sender) { throw new InvalidOperationException(Text("SessionOwned")); }
                            switch ((MessageType)request.type)
                            {
                                case MessageType.Open:
                                    if (_owner != null && _owner.id != request.id)
                                    {
                                        CancelSession();
                                        Volatile.Write(ref _owner, null);
                                        await Media.Target.CloseAsync();
                                    }
                                    if (_owner == null)
                                    {
                                        lock (_cancellationSync)
                                        {
                                            _sessionCancellation?.Dispose();
                                            _sessionCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
                                        }
                                        Volatile.Write(ref _owner, request);
                                        try
                                        {
                                            if (Media.UseOpticalHead && Media.Target is GXSerial optical)
                                            {
                                                optical.BaudRate = 300; optical.DataBits = 7; optical.Parity = Parity.Even; optical.StopBits = StopBits.One;
                                            }
                                            await Media.Target.OpenAsync(_sessionCancellation.Token);
                                            if (Media.UseOpticalHead && Media.Target is GXSerial) { await InitializeIECAsync(_trace, Media, _sessionCancellation.Token); }
                                        }
                                        catch { Volatile.Write(ref _owner, null); await Media.Target.CloseAsync(); throw; }
                                        Volatile.Write(ref _owner, request);
                                    }
                                    await ReplyAsync(request, MessageType.Open);
                                    break;
                                case MessageType.Send:
                                    if (_owner == null) { throw new InvalidOperationException(Text("SessionNotOpen")); }
                                    await Media.Target.SendAsync(Convert.FromHexString((request.frame ?? "").Replace(" ", "")), cancellationToken: _sessionCancellation.Token);
                                    break;
                                case MessageType.Close:
                                    CancelSession();
                                    Volatile.Write(ref _owner, null);
                                    await Media.Target.CloseAsync();
                                    await ReplyAsync(request, MessageType.Close);
                                    break;
                            }
                        }
                        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { break; }
                        catch (Exception ex) { await ReplyAsync(request, MessageType.Exception, error: ex.Message); }
                        finally { _gate.Release(); }
                    }
                }
                catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
                catch (Exception ex) { Fail(ex); }
            }
            /// <summary>Records the first worker failure and stops the bridge to prevent silent message loss.</summary>
            /// <param name="error">Failure returned by the bridge lifetime task.</param>
            private void Fail(Exception error)
            {
                Interlocked.CompareExchange(ref Failure, error, null);
                _lifetime.Cancel();
            }
            /// <summary>Interrupts the current operation while preserving cancellation-source lifetime.</summary>
            private void CancelSession()
            {
                lock (_cancellationSync) { _sessionCancellation?.Cancel(); }
            }
            /// <summary>Closes the meter and clears ownership after broker disconnect.</summary>
            /// <returns>The asynchronous reset.</returns>
            public async Task ResetAsync()
            {
                CancelSession();
                await _gate.WaitAsync();
                try
                {
                    while (_requests.Reader.TryRead(out _)) { }
                    Volatile.Write(ref _owner, null); await Media.Target.CloseAsync();
                }
                finally { _gate.Release(); }
            }
            /// <summary>Awaits workers, detaches receive handling and disposes the transport.</summary>
            /// <returns>The complete session shutdown.</returns>
            public async Task StopAsync()
            {
                _requests.Writer.TryComplete(); _received.Writer.TryComplete();
                await Task.WhenAll(_worker, _publisher);
                Media.Target.OnReceived -= Received;
                await ResetAsync(); await Media.Target.DisposeAsync();
                lock (_cancellationSync) { _sessionCancellation?.Dispose(); _sessionCancellation = null; }
                _gate.Dispose();
            }
        }

        /// <summary>Negotiates the IEC optical-head session and selected serial baud rate.</summary>
        /// <param name="trace">Trace verbosity used for console diagnostics.</param>
        /// <param name="media">Configured meter transport whose optical session is being controlled.</param>
        /// <param name="cancellationToken">Cancels negotiation and receive waits.</param>
        /// <returns>The asynchronous negotiation operation.</returns>
        private static async Task InitializeIECAsync(TraceLevel trace, Media media, CancellationToken cancellationToken)
        {
            GXSerial serial = (GXSerial)media.Target;
            serial.BaudRate = 300;
            serial.DataBits = 7;
            serial.Parity = Parity.Even;
            serial.StopBits = StopBits.One;
            byte Terminator = (byte)0x0A;
            //Some meters need a little break.
            await Task.Delay(1000, cancellationToken);
            //Query device information.
            string data = "/?!\r\n";
            WriteLog(trace, string.Format(System.Globalization.CultureInfo.CurrentCulture, global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("IecSending", global::Gurux.Broker.Properties.Resources.Culture), data));
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
            using (media.Target.BeginSynchronous())
            {
                await media.Target.SendAsync(Encoding.ASCII.GetBytes(data), cancellationToken: cancellationToken);
                if (!await media.Target.ReceiveAsync(p, cancellationToken))
                {
                    await DiscIECAsync(media, cancellationToken);
                    string str = global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("DeviceReplyTimeout", global::Gurux.Broker.Properties.Resources.Culture);
                    WriteLog(trace, str);
                    await media.Target.SendAsync(Encoding.ASCII.GetBytes(data), cancellationToken: cancellationToken);
                    if (!await media.Target.ReceiveAsync(p, cancellationToken))
                    {
                        throw new Exception(str);
                    }
                }
                //If echo is used.
                if (p.Reply == data)
                {
                    p.Reply = null;
                    if (!await media.Target.ReceiveAsync(p, cancellationToken))
                    {
                        data = global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("DeviceReplyTimeout", global::Gurux.Broker.Properties.Resources.Culture);
                        WriteLog(trace, data);
                        throw new Exception(data);
                    }
                }
            }
            WriteLog(trace, string.Format(System.Globalization.CultureInfo.CurrentCulture, global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("IecReceived", global::Gurux.Broker.Properties.Resources.Culture), p.Reply));
            if (string.IsNullOrEmpty(p.Reply) || p.Reply.Length < 5 || p.Reply[0] != '/')
            {
                p.WaitTime = 100;
                await media.Target.ReceiveAsync(p, cancellationToken);
                throw new Exception(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("InvalidDeviceResponse", global::Gurux.Broker.Properties.Resources.Culture));
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
                    throw new Exception(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("UnknownBaudRate", global::Gurux.Broker.Properties.Resources.Culture));
            }
            if (media.MaximumBaudRate != 0)
            {
                BaudRate = media.MaximumBaudRate;
                baudrate = GetIecBaudRate(BaudRate);
                WriteLog(trace, string.Format(System.Globalization.CultureInfo.CurrentCulture, global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("MaximumBaudRate", global::Gurux.Broker.Properties.Resources.Culture), BaudRate));
            }
            WriteLog(trace, string.Format(System.Globalization.CultureInfo.CurrentCulture, global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("CurrentBaudRate", global::Gurux.Broker.Properties.Resources.Culture), BaudRate));
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
            WriteLog(trace, string.Format(System.Globalization.CultureInfo.CurrentCulture, global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("MovingToModeE", global::Gurux.Broker.Properties.Resources.Culture), Convert.ToHexString(arr)));
            using (media.Target.BeginSynchronous())
            {
                p.Reply = null;
                await media.Target.SendAsync(arr, cancellationToken: cancellationToken);
                p.WaitTime = 2000;
                //Note! All meters do not echo this.
                await media.Target.ReceiveAsync(p, cancellationToken);
                if (p.Reply != null)
                {
                    WriteLog(trace, string.Format(System.Globalization.CultureInfo.CurrentCulture, global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("ReceivedPayload", global::Gurux.Broker.Properties.Resources.Culture), p.Reply));
                }
                await media.Target.CloseAsync();
                serial.BaudRate = BaudRate;
                serial.DataBits = 8;
                serial.Parity = Parity.None;
                serial.StopBits = StopBits.One;
                await serial.OpenAsync(cancellationToken);
                //Some meters need this sleep. Do not remove.
                await Task.Delay(1000, cancellationToken);
            }
        }

        /// <summary>
        /// Send IEC disconnect message.
        /// </summary>
        /// <param name="media">Configured meter transport whose optical session is being controlled.</param>
        /// <param name="cancellationToken">Cancels negotiation and receive waits.</param>
        /// <returns>The asynchronous negotiation operation.</returns>
        private static async Task DiscIECAsync(Media media, CancellationToken cancellationToken)
        {
            ReceiveParameters<string> p = new ReceiveParameters<string>()
            {
                AllData = false,
                Eop = (byte)0x0A,
                WaitTime = media.WaitTime * 1000
            };
            string data = (char)0x01 + "B0" + (char)0x03 + "\r\n";
            await media.Target.SendAsync(Encoding.ASCII.GetBytes(data), cancellationToken: cancellationToken);
            p.Count = 1;
            await media.Target.ReceiveAsync(p, cancellationToken);
        }


        /// <summary>Converts a supported serial baud rate to its IEC identification character.</summary>
        /// <param name="baudrate">Serial baud rate to encode as an IEC character.</param>
        /// <returns>The IEC baud-rate selection character from 0 through 6.</returns>
        /// <exception cref="Exception">The baud rate has no supported IEC selection character.</exception>
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
                    throw new Exception(global::Gurux.Broker.Properties.Resources.ResourceManager.GetString("UnknownBaudRate", global::Gurux.Broker.Properties.Resources.Culture));
            }
            return rate;
        }


        /// <summary>Writes an informational transport message when tracing is enabled.</summary>
        /// <param name="trace">Trace verbosity used for console diagnostics.</param>
        /// <param name="message">Complete diagnostic message to write when tracing permits.</param>
        static void WriteLog(TraceLevel trace, string message)
        {
            if (trace > TraceLevel.Warning)
            {
                Console.WriteLine(message);
            }
        }
    }
}
