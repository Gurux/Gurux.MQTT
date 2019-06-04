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
using Gurux.Common.JSon;
using Gurux.MQTT.Message;
using Gurux.MQTT.Properties;
using Gurux.Shared;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Gurux.MQTT
{
    /// <summary>
    /// GXMqtt implements  MQTT client that sends bytes to the MQTT broker.
    /// </summary>
    public class GXMqtt : IGXMedia2
    {
        /// <summary>
        /// Sync object.
        /// </summary>
        private object sync = new object();
        internal GXSynchronousMediaBase syncBase;

        //Events
        PropertyChangedEventHandler m_OnPropertyChanged;
        MediaStateChangeEventHandler m_OnMediaStateChange;
        TraceEventHandler m_OnTrace;
        ClientConnectedEventHandler m_OnClientConnected;
        ClientDisconnectedEventHandler m_OnClientDisconnected;
        internal ErrorEventHandler m_OnError;
        internal ReceivedEventHandler m_OnReceived;
        internal AutoResetEvent replyReceivedEvent = new AutoResetEvent(false);

        /// <summary>
        /// Last exception.
        /// </summary>
        private string lastException;

        /// <summary>
        /// Unique Message ID.
        /// </summary>
        private UInt16 messageId;

        private UInt16 MessageId
        {
            get
            {
                return ++messageId;
            }
        }

        /// <summary>
        /// Used topic.
        /// </summary>
        string topic;

        /// <summary>
        /// Used client ID.
        /// </summary>
        string clientId;

        /// <summary>
        /// Client ID that user want's to use.
        /// </summary>
        string userClientId;

        /// <summary>
        /// Server address.
        /// </summary>
        string serverAddress;
        /// <summary>
        /// Host port.
        /// </summary>
        int port = 1883;

        static readonly MqttFactory factory = new MqttFactory();
        IMqttClient mqttClient;
        string IGXMedia.Name => "MQTT";

        /// <summary>
        /// What level of tracing is used.
        /// </summary>
        public TraceLevel Trace
        {
            get
            {
                return syncBase.Trace;
            }
            set
            {
                syncBase.Trace = value;
            }
        }

        /// <inheritdoc cref="IGXMedia.IsOpen"/>
        /// <seealso cref="Open">Open</seealso>
        /// <seealso cref="Close">Close</seealso>
        [Browsable(false)]
        public bool IsOpen => mqttClient != null && mqttClient.IsConnected;

        string IGXMedia.MediaType => "MQTT";

        bool IGXMedia.Enabled => true;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXMqtt()
        {
            syncBase = new GXSynchronousMediaBase(1024);
            ConfigurableSettings = AvailableMediaSettings.All;
        }

        string IGXMedia.Settings
        {
            get
            {
                string tmp = "";
                if (!string.IsNullOrEmpty(serverAddress))
                {
                    tmp += "<IP>" + serverAddress + "</IP>" + Environment.NewLine;
                }
                if (port != 0)
                {
                    tmp += "<Port>" + port + "</Port>" + Environment.NewLine;
                }
                tmp += "<Topic>" + topic + "</Topic>" + Environment.NewLine;
                if (!string.IsNullOrEmpty(ClientId))
                {
                    tmp += "<ClientId>" + ClientId + "</ClientId>" + Environment.NewLine;
                }
                return tmp;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    XmlReaderSettings settings = new XmlReaderSettings(); ;
                    settings.ConformanceLevel = ConformanceLevel.Fragment;
                    using (XmlReader xmlReader = XmlReader.Create(new System.IO.StringReader(value), settings))
                    {
                        while (!xmlReader.EOF)
                        {
                            if (xmlReader.IsStartElement())
                            {
                                switch (xmlReader.Name)
                                {
                                    case "Topic":
                                        topic = (string)xmlReader.ReadElementContentAs(typeof(string), null);
                                        break;
                                    case "Port":
                                        port = (int)(xmlReader.ReadElementContentAs(typeof(int), null));
                                        break;
                                    case "IP":
                                        serverAddress = (string)xmlReader.ReadElementContentAs(typeof(string), null);
                                        break;
                                    case "ClientId":
                                        ClientId = (string)xmlReader.ReadElementContentAs(typeof(string), null);
                                        break;
                                }
                            }
                            else
                            {
                                xmlReader.Read();
                            }
                        }
                    }
                }
            }
        }

        /// <inheritdoc cref="IGXMedia.Synchronous"/>
        public object Synchronous { get; } = new object();

        /// <inheritdoc cref="IGXMedia.IsSynchronous"/>
        public bool IsSynchronous
        {
            get
            {
                bool reserved = System.Threading.Monitor.TryEnter(Synchronous, 0);
                if (reserved)
                {
                    System.Threading.Monitor.Exit(Synchronous);
                }
                return !reserved;
            }
        }

        /// <inheritdoc cref="IGXMedia.ResetSynchronousBuffer"/>
        public void ResetSynchronousBuffer()
        {
            lock (syncBase.receivedSync)
            {
                syncBase.receivedSize = 0;
            }
        }

        /// <inheritdoc cref="IGXMedia.Validate"/>
        public void Validate()
        {
            if (port == 0)
            {
                throw new Exception(Resources.InvalidBrokerPort);
            }
            if (!string.IsNullOrEmpty(serverAddress))
            {
                throw new Exception(Resources.InvalidBrokerName);
            }
            if (!string.IsNullOrEmpty(topic))
            {
                throw new Exception(Resources.InvalidTopic);
            }
        }

        /// <summary>
        /// Sent byte count.
        /// </summary>
        /// <seealso cref="BytesReceived">BytesReceived</seealso>
        /// <seealso cref="ResetByteCounters">ResetByteCounters</seealso>
        [Browsable(false)]
        public UInt64 BytesSent
        {
            get;
            private set;
        }

        /// <summary>
        /// Received byte count.
        /// </summary>
        /// <seealso cref="BytesSent">BytesSent</seealso>
        /// <seealso cref="ResetByteCounters">ResetByteCounters</seealso>
        [Browsable(false)]
        public UInt64 BytesReceived
        {
            get;
            private set;
        }

        /// <inheritdoc cref="IGXMedia.Eop"/>
        public object Eop
        {
            get;
            set;
        }

        /// <inheritdoc cref="IGXMedia.ConfigurableSettings"/>
        public AvailableMediaSettings ConfigurableSettings
        {
            get
            {
                return (AvailableMediaSettings)((IGXMedia)this).ConfigurableSettings;
            }
            set
            {
                ((IGXMedia)this).ConfigurableSettings = (int)value;
            }
        }

        /// <inheritdoc cref="IGXMedia.ConfigurableSettings"/>
        int IGXMedia.ConfigurableSettings
        {
            get;
            set;
        }

        /// <inheritdoc cref="IGXMedia.Tag"/>
        object IGXMedia.Tag { get; set; }

        IGXMediaContainer IGXMedia.MediaContainer { get; set; }

        /// <inheritdoc cref="IGXMedia.SyncRoot"/>
        [Browsable(false), ReadOnly(true)]
        public object SyncRoot
        {
            get
            {
                //In some special cases when binary serialization is used this might be null
                //after deserialize. Just set it.
                if (sync == null)
                {
                    sync = new object();
                }
                return sync;
            }
        }

#if !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETSTANDARD2_0
        /// <summary>
        /// Shows MQTT Properties dialog.
        /// </summary>
        /// <param name="parent">Owner window of the Properties dialog.</param>
        /// <returns>True, if the user has accepted the changes.</returns>
        /// <seealso cref="Port">Port</seealso>
        /// <seealso cref="ServerAddress">HostName</seealso>
        /// <seealso href="PropertiesDialog.html">Properties Dialog</seealso>
        public bool Properties(System.Windows.Forms.Form parent)
        {
            return new Gurux.Shared.PropertiesForm(PropertiesForm, Resources.SettingsTxt, IsOpen).ShowDialog(parent) == System.Windows.Forms.DialogResult.OK;
        }

        /// <inheritdoc cref="IGXMedia.PropertiesForm"/>
        public System.Windows.Forms.Form PropertiesForm
        {
            get
            {
                return new Settings(this);
            }
        }
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETSTANDARD2_0

        private void NotifyPropertyChanged(string info)
        {
            if (m_OnPropertyChanged != null)
            {
                m_OnPropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        /// <summary>
        /// Retrieves or sets the name or IP address of the host.
        /// </summary>
        /// <value>
        /// Used topic.
        /// </value>
        /// <seealso cref="Open">Open</seealso>
        /// <seealso cref="Port">Port</seealso>
        /// <seealso cref="ClientId">Protocol</seealso>
        [DefaultValue("")]
        [Category("Communication")]
        [Description("Retrieves or sets used topic.")]
        public string Topic
        {
            get
            {
                return topic;
            }
            set
            {
                if (topic != value)
                {
                    topic = value;
                    NotifyPropertyChanged("Topic");
                }
            }
        }

        /// <summary>
        /// Retrieves or sets the name or IP address of the host.
        /// </summary>
        /// <value>
        /// Used topic.
        /// </value>
        /// <seealso cref="Open">Open</seealso>
        /// <seealso cref="Port">Port</seealso>
        /// <seealso cref="Topic">Protocol</seealso>
        [DefaultValue("")]
        [Category("Communication")]
        [Description("Retrieves or sets used client ID.")]
        public string ClientId
        {
            get
            {
                return userClientId;
            }
            set
            {
                if (userClientId != value)
                {
                    userClientId = value;
                    NotifyPropertyChanged("ClientId");
                }
            }
        }


        /// <summary>
        /// MQTT server address.
        /// </summary>
        /// <value>MQTT server address.</value>
        /// <seealso cref="Open">Open</seealso>
        /// <seealso cref="Port">Port</seealso>
        [DefaultValue("")]
        [Category("Communication")]
        [Description("MQTT server address.")]
        public string ServerAddress
        {
            get
            {
                return serverAddress;
            }
            set
            {
                if (serverAddress != value)
                {
                    serverAddress = value;
                    NotifyPropertyChanged("ServerAddress");
                }
            }
        }

        /// <summary>
        /// MQTT server port number.
        /// </summary>
        /// <value>MQTT server port number.</value>
        /// <seealso cref="Open">Open</seealso>
        /// <seealso cref="ServerAddress">ServerAddress</seealso>
        [DefaultValue(1883)]
        [Category("Communication")]
        [Description("MQTT server port number.")]
        public int Port
        {
            get
            {
                return port;
            }
            set
            {
                if (port != value)
                {
                    port = value;
                    NotifyPropertyChanged("Port");
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(serverAddress);
            sb.Append(':');
            sb.Append(port);
            sb.Append(' ');
            sb.Append(topic);
            return sb.ToString();
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                m_OnPropertyChanged += value;
            }
            remove
            {
                m_OnPropertyChanged -= value;
            }
        }

        /// <summary>
        /// GXNet component sends received data through this method.
        /// </summary>
        [Description("GXNet component sends received data through this method.")]
        public event ReceivedEventHandler OnReceived
        {
            add
            {
                m_OnReceived += value;
            }
            remove
            {
                m_OnReceived -= value;
            }
        }

        /// <summary>
        /// Errors that occur after the connection is established, are sent through this method.
        /// </summary>
        [Description("Errors that occur after the connection is established, are sent through this method.")]
        public event ErrorEventHandler OnError
        {
            add
            {

                m_OnError += value;
            }
            remove
            {
                m_OnError -= value;
            }
        }

        /// <summary>
        /// Media component sends notification, when its state changes.
        /// </summary>
        [Description("Media component sends notification, when its state changes.")]
        public event MediaStateChangeEventHandler OnMediaStateChange
        {
            add
            {
                m_OnMediaStateChange += value;
            }
            remove
            {
                m_OnMediaStateChange -= value;
            }
        }
#if WINDOWS_PHONE
    event ClientConnectedEventHandler IGXMedia.OnClientConnected
    {
        add {
            throw new NotImplementedException();
        }
        remove {
            throw new NotImplementedException();
        }
    }

    event ClientDisconnectedEventHandler IGXMedia.OnClientDisconnected
    {
        add {
            throw new NotImplementedException();
        }
        remove {
            throw new NotImplementedException();
        }
    }
#else
        /// <summary>
        /// Called when the client is establishing a connection with a Net Server.
        /// </summary>
        [Description("Called when the client is establishing a connection with a Net Server.")]
        public event ClientConnectedEventHandler OnClientConnected
        {
            add
            {
                m_OnClientConnected += value;
            }
            remove
            {
                m_OnClientConnected -= value;
            }
        }

        /// <summary>
        /// Called when the client has been disconnected from the network server.
        /// </summary>
        [Description("Called when the client has been disconnected from the network server.")]
        public event ClientDisconnectedEventHandler OnClientDisconnected
        {
            add
            {
                m_OnClientDisconnected += value;
            }
            remove
            {
                m_OnClientDisconnected -= value;
            }
        }

        /// <inheritdoc cref="TraceEventHandler"/>
        [Description("Called when the Media is sending or receiving data.")]
        public event TraceEventHandler OnTrace
        {
            add
            {
                m_OnTrace += value;
            }
            remove
            {
                m_OnTrace -= value;
            }
        }

#endif
        /// <summary>
        /// Publish message.
        /// </summary>
        /// <param name="msg"></param>
        private void PublishMessage(GXMessage msg)
        {
            GXJsonParser parser = new GXJsonParser();
            string str = parser.Serialize(msg);
            MqttApplicationMessage message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(str)
            .WithExactlyOnceQoS()
            .Build();
            mqttClient.PublishAsync(message).Wait();
        }

        public void Close()
        {
            lastException = null;
            if (mqttClient != null && mqttClient.IsConnected)
            {
                GXMessage msg = new GXMessage() { id = MessageId, type = (int)MesssageType.Close, sender = clientId };
                try
                {
                    PublishMessage(msg);
                }
                catch (Exception)
                {
                    replyReceivedEvent.Set();
                }
                if (AsyncWaitTime == 0)
                {
                    replyReceivedEvent.WaitOne();
                }
                else
                {
                    replyReceivedEvent.WaitOne((int)AsyncWaitTime * 1000);
                }
                mqttClient.DisconnectAsync().Wait();
                mqttClient = null;
                if (lastException != null)
                {
                    throw new Exception(lastException);
                }
            }
        }

        void IGXMedia.Copy(object target)
        {
            GXMqtt tmp = (GXMqtt)target;
            port = tmp.port;
            serverAddress = tmp.serverAddress;
            topic = tmp.topic;
        }

        private int HandleReceivedData(int bytes, byte[] buff, string sender)
        {
            BytesReceived += (uint)bytes;
            if (this.IsSynchronous)
            {
                TraceEventArgs arg;
                lock (syncBase.receivedSync)
                {
                    int index = syncBase.receivedSize;
                    syncBase.AppendData(buff, 0, bytes);
                    if (bytes != 0 && Trace == TraceLevel.Verbose && m_OnTrace != null)
                    {
                        arg = new TraceEventArgs(TraceTypes.Received, buff, 0, bytes, null);
                        m_OnTrace(this, arg);
                    }
                    if (bytes != 0 && Eop != null) //Search Eop if given.
                    {
                        if (Eop is Array)
                        {
                            foreach (object eop in (Array)Eop)
                            {
                                bytes = Gurux.Common.GXCommon.IndexOf(syncBase.m_Received, Gurux.Common.GXCommon.GetAsByteArray(eop), index, syncBase.receivedSize);
                                if (bytes != -1)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            bytes = Gurux.Common.GXCommon.IndexOf(syncBase.m_Received, Gurux.Common.GXCommon.GetAsByteArray(Eop), index, syncBase.receivedSize);
                        }
                    }
                    if (bytes != -1)
                    {
                        syncBase.receivedEvent.Set();
                    }
                }
            }
            else
            {
                if (m_OnReceived != null)
                {
                    syncBase.receivedSize = 0;
                    byte[] data = new byte[bytes];
                    Array.Copy(buff, data, bytes);
                    if (Trace == TraceLevel.Verbose && m_OnTrace != null)
                    {
                        m_OnTrace(this, new TraceEventArgs(TraceTypes.Received, data, null));
                    }
                    m_OnReceived(this, new ReceiveEventArgs(data, sender));
                }
                else if (Trace == TraceLevel.Verbose && m_OnTrace != null)
                {
                    m_OnTrace(this, new TraceEventArgs(TraceTypes.Received, buff, 0, bytes, null));
                }
            }
            return bytes;
        }

        public void Open()
        {
            Close();
            mqttClient = factory.CreateMqttClient();
            if (string.IsNullOrEmpty(userClientId))
            {
                clientId = Guid.NewGuid().ToString();
            }
            else
            {
                clientId = userClientId;
            }
            var options = new MqttClientOptionsBuilder()
            .WithTcpServer(serverAddress, port).WithClientId(clientId)
            .Build();
            mqttClient.UseApplicationMessageReceivedHandler(t =>
            {
                string str = ASCIIEncoding.ASCII.GetString(t.ApplicationMessage.Payload);
                GXJsonParser parser = new GXJsonParser();
                GXMessage msg = parser.Deserialize<GXMessage>(str);
                if (msg.id == messageId || (MesssageType)msg.type == MesssageType.Close || (MesssageType)msg.type == MesssageType.Exception)
                {
                    switch ((MesssageType)msg.type)
                    {
                        case MesssageType.Open:
                            m_OnMediaStateChange?.Invoke(this, new MediaStateEventArgs(MediaState.Open));
                            replyReceivedEvent.Set();
                            break;
                        case MesssageType.Send:
                            break;
                        case MesssageType.Receive:
                            byte[] bytes = Gurux.Common.GXCommon.HexToBytes(msg.frame);
                            replyReceivedEvent.Set();
                            if (bytes.Length != 0)
                            {
                                HandleReceivedData(bytes.Length, bytes, t.ClientId);
                            }
                            break;
                        case MesssageType.Close:
                            m_OnMediaStateChange?.Invoke(this, new MediaStateEventArgs(MediaState.Closed));
                            replyReceivedEvent.Set();
                            break;
                        case MesssageType.Exception:
                            lastException = msg.exception;
                            replyReceivedEvent.Set();
                            break;
                    }
                }
                else
                {
                    m_OnTrace?.Invoke(this, new TraceEventArgs(TraceTypes.Info, "Unknown reply. " + msg, msg.sender));
                }
            });
            mqttClient.UseConnectedHandler(t =>
            {
                // Subscribe to a topic
                mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(clientId).Build()).Wait();
                m_OnMediaStateChange?.Invoke(this, new MediaStateEventArgs(MediaState.Opening));
                GXMessage msg = new GXMessage() { id = MessageId, type = (int)MesssageType.Open, sender = clientId };
                PublishMessage(msg);
            });
            mqttClient.UseDisconnectedHandler(t =>
            {
                m_OnMediaStateChange?.Invoke(this, new MediaStateEventArgs(MediaState.Closed));
                replyReceivedEvent.Set();
            });
            try
            {
                replyReceivedEvent.Reset();
                if (AsyncWaitTime == 0)
                {
                    mqttClient.ConnectAsync(options).Wait();
                }
                else
                {
                    mqttClient.ConnectAsync(options).Wait((int)AsyncWaitTime * 1000);
                }
            }
            catch (AggregateException ex)
            {
                if (mqttClient != null)
                {
                    mqttClient.DisconnectAsync().Wait(10000);
                }
                mqttClient = null;
                throw ex.InnerException;
            }
            replyReceivedEvent.WaitOne();
            if (lastException != null)
            {
                throw new Exception(lastException);
            }
        }

        /// <inheritdoc cref="IGXMedia.Receive"/>
        public bool Receive<T>(ReceiveParameters<T> args)
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException("Media is closed.");
            }
            return syncBase.Receive(args);
        }

        /// <summary>
        /// Resets BytesReceived and BytesSent counters.
        /// </summary>
        /// <seealso cref="BytesSent">BytesSent</seealso>
        /// <seealso cref="BytesReceived">BytesReceived</seealso>
        public void ResetByteCounters()
        {
            BytesSent = BytesReceived = 0;
        }

        void IGXMedia.Send(object data, string receiver)
        {
            byte[] tmp = (byte[])data;
            if (tmp != null)
            {
                BytesSent += (ulong)tmp.Length;
                GXMessage msg = new GXMessage() { id = MessageId, type = (int)MesssageType.Send, sender = clientId, frame = Common.GXCommon.ToHex(tmp) };
                PublishMessage(msg);
            }
        }

        public uint AsyncWaitTime
        {
            get;
            set;
        }

        public EventWaitHandle AsyncWaitHandle
        {
            get
            {
                return null;
            }
        }

        public uint ReceiveDelay
        {
            get;
            set;
        }
    }
}
