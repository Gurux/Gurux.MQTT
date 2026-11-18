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

using System.Buffers;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading.Channels;
using Gurux.Common;
using Gurux.MQTT.Message;
using Gurux.MQTT.Properties;
using MQTTnet;
using MQTTnet.Protocol;

namespace Gurux.MQTT;

/// <summary>Asynchronous MQTT transport for the Gurux JSON bridge protocol.</summary>
/// <remarks>Each opening owns a client, reply subscription and bounded receive dispatcher.
/// Broker connection alone is insufficient: the bridge must acknowledge opening the selected meter.</remarks>
public partial class GXMqtt : GXMediaBase, IGXMediaIconProvider
{
    /// <summary>Opens the embedded PNG icon for this media without loading a UI framework.</summary>
    /// <returns>A new readable stream positioned at the beginning, or null if the embedded icon is unavailable.</returns>
    /// <remarks>The caller owns the stream and must dispose it. Each call returns an independent stream.</remarks>
    public Stream? OpenIcon()
    {
        return typeof(GXMqtt).Assembly.GetManifestResourceStream("Gurux.MQTT.MediaIcon.png");
    }

    /// <summary>Protects session ownership and settings replacement without spanning awaits.</summary>
    private readonly object _admission = new();
    /// <summary>Identifies event callbacks that may request shutdown without awaiting their own dispatcher.</summary>
    private static readonly AsyncLocal<Session?> _callbackOwner = new();
    /// <summary>Owns the current session until all of its native and managed work has stopped.</summary>
    private Session? _session;
    /// <summary>Allocates wire identifiers across reopen operations, reserving zero.</summary>
    private ushort _messageId;
    /// <summary>Limits one decoded bridge frame and prevents unbounded wire deserialization.</summary>
    private const int MaximumFrameBytes = 1024 * 1024;

    /// <summary>Owns a single broker connection and all operations tied to its lifetime.</summary>
    private sealed class Session
    {
        /// <summary>Stores the immutable connection parameters captured before opening.</summary>
        internal readonly MqttSettings Settings;
        /// <summary>Stores the exact client ID and private reply subscription topic.</summary>
        internal readonly string ClientId;
        /// <summary>Owns the native MQTT client.</summary>
        internal readonly IMqttClient Client = new MqttClientFactory().CreateMqttClient();
        /// <summary>Cancels opening, application writes and queued receive dispatch.</summary>
        internal readonly CancellationTokenSource Stop = new();
        /// <summary>Serializes application publishes with the shutdown handshake.</summary>
        internal readonly SemaphoreSlim SendLock = new(1);
        /// <summary>Protects reply correlations and the bounded accepted identifier history.</summary>
        internal readonly object Replies = new();
        /// <summary>Maps pending control identifiers to their expected wire type and acknowledgement.</summary>
        internal readonly Dictionary<ushort, (MessageType Type, TaskCompletionSource Completion)> Pending = new();
        /// <summary>Rejects receive packets from prior sessions or unrelated requests.</summary>
        internal readonly HashSet<ushort> Accepted = new();
        /// <summary>Bounds the accepted identifier history to the most recent 256 requests.</summary>
        internal readonly Queue<ushort> History = new();
        /// <summary>Separates user callbacks from MQTTnet's packet dispatch loop with bounded owned messages.</summary>
        internal readonly Channel<GXMessage> Inbox = Channel.CreateBounded<GXMessage>(new BoundedChannelOptions(16)
        {
            SingleReader = true, SingleWriter = false, FullMode = BoundedChannelFullMode.Wait
        });
        /// <summary>Completes after the opening attempt releases its cancellation registration and client usage.</summary>
        internal readonly TaskCompletionSource OpenDone = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Releases receive dispatch only after shared buffers are ready for this session.</summary>
        internal readonly TaskCompletionSource Ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Tracks the asynchronous opening result returned to callers.</summary>
        internal Task Opening = Task.CompletedTask;
        /// <summary>Tracks shutdown shared by all callers.</summary>
        internal Task? Closing;
        /// <summary>Tracks the sole callback dispatcher for this connection.</summary>
        internal Task Worker = Task.CompletedTask;
        /// <summary>Publishes whether the bridge has acknowledged opening.</summary>
        internal volatile bool Open;
        /// <summary>Pins the bridge ownership correlation for unsolicited frames until this session ends.</summary>
        internal ushort OpenId;
        /// <summary>Records that opening may have reached the remote bridge even if its acknowledgement was lost.</summary>
        internal bool OpenPublished;
        /// <summary>Records a transport failure for reporting outside the MQTT callback.</summary>
        internal Exception? Failure;
        /// <summary>Constructs a new isolated connection with a fresh automatic reply identifier if needed.</summary>
        /// <param name="settings">Validated settings captured before opening.</param>
        internal Session(MqttSettings settings)
        {
            Settings = settings;
            ClientId = string.IsNullOrEmpty(settings.ClientId) ? Guid.NewGuid().ToString() : settings.ClientId;
        }
    }

    /// <summary>Creates a closed media with default broker settings.</summary>
    public GXMqtt() { }
    /// <inheritdoc/>
    public override bool IsOpen { get { lock (_admission) { return _session?.Open == true; } } }
    /// <inheritdoc/>
    public override string Name => string.Format(CultureInfo.InvariantCulture, "{0}:{1}/{2}", ServerAddress, Port, Topic);
    /// <inheritdoc/>
    public override string MediaType => "MQTT";

    /// <inheritdoc/>
    public override Task OpenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_admission)
        {
            if (_session is not null)
            {
                if (_session.Closing is not null) { throw new InvalidOperationException(Resources.Get("LifecycleInProgress")); }
                return _session.Open ? Task.CompletedTask : _session.Opening.WaitAsync(cancellationToken);
            }
            ValidateForOpen();
            var current = new Session(_configuration);
            _session = current;
            current.Opening = OpenCoreAsync(current, cancellationToken);
            return current.Opening;
        }
    }

    /// <summary>Connects, subscribes and waits for the bridge acknowledgement before exposing an open media.</summary>
    /// <param name="current">The isolated session being opened.</param>
    /// <param name="cancellationToken">Cancels this opening attempt.</param>
    /// <returns>Completion after successful opening or fully awaited failure cleanup.</returns>
    private async Task OpenCoreAsync(Session current, CancellationToken cancellationToken)
    {
        await Task.Yield();
        Exception? failure = null;
        using (var registration = cancellationToken.Register(() => current.Stop.Cancel()))
        using (var deadline = CreateDeadline(current.Stop.Token, current.Settings.AsyncWaitTime))
        {
            try
            {
                NotifyState(current, MediaState.Opening);
                current.Client.ApplicationMessageReceivedAsync += args => HandleMessageAsync(current, args);
                current.Client.DisconnectedAsync += _ =>
                {
                    bool unexpected;
                    lock (_admission) { unexpected = ReferenceEquals(_session, current) && current.Closing is null; }
                    if (unexpected) { StopFailedSession(current, new IOException(Resources.Get("BrokerDisconnected"))); }
                    return Task.CompletedTask;
                };
                current.Worker = DispatchAsync(current);
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(current.Settings.ServerAddress, current.Settings.Port)
                    .WithClientId(current.ClientId).WithCleanSession().Build();
                var connected = await current.Client.ConnectAsync(options, deadline.Token).ConfigureAwait(false);
                if (connected.ResultCode != MqttClientConnectResultCode.Success)
                {
                    throw new IOException(Resources.Get("BrokerConnectRejected"));
                }
                var subscription = await current.Client.SubscribeAsync(
                    new MqttTopicFilterBuilder().WithTopic(current.ClientId).WithExactlyOnceQoS().Build(),
                    deadline.Token).ConfigureAwait(false);
                if (subscription.Items.Any(item => (int)item.ResultCode >= 128))
                {
                    throw new IOException(Resources.Get("BrokerSubscribeRejected"));
                }
                await ExchangeAsync(current, MessageType.Open, deadline.Token).ConfigureAwait(false);
                lock (_admission)
                {
                    deadline.Token.ThrowIfCancellationRequested();
                    if (!ReferenceEquals(_session, current) || current.Closing is not null) { throw new OperationCanceledException(current.Stop.Token); }
                    ReceiveBuffer.Open();
                    current.Open = true;
                    NotifyState(current, MediaState.Open);
                    current.Ready.TrySetResult();
                }
                current.Stop.Token.ThrowIfCancellationRequested();
            }
            catch (Exception error)
            {
                failure = error is OperationCanceledException && !cancellationToken.IsCancellationRequested && !current.Stop.IsCancellationRequested
                    ? new TimeoutException(string.Format(CultureInfo.CurrentCulture, Resources.Get("BridgeReplyTimeout"), current.Settings.Topic), error)
                    : error;
            }
        }
        current.OpenDone.TrySetResult();
        if (failure is not null)
        {
            await CloseSessionAsync(current).ConfigureAwait(false);
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    /// <summary>Builds a cancellable operation deadline without overflowing millisecond-based timer limits.</summary>
    /// <param name="token">The operation or connection lifetime token.</param>
    /// <param name="seconds">The timeout in seconds, or zero for no deadline.</param>
    /// <returns>A linked token source owned by the caller.</returns>
    private static CancellationTokenSource CreateDeadline(CancellationToken token, uint seconds)
    {
        var result = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (seconds != 0) { result.CancelAfter(TimeSpan.FromMilliseconds(Math.Min((double)seconds * 1000, uint.MaxValue - 1d))); }
        return result;
    }

    /// <summary>Allocates and remembers a bounded request identifier for this connection.</summary>
    /// <param name="current">The session whose replies will be accepted.</param>
    /// <returns>A nonzero wire message identifier.</returns>
    private ushort NextId(Session current)
    {
        lock (_admission)
        {
            if (++_messageId == 0) { ++_messageId; }
            lock (current.Replies)
            {
                current.Accepted.Add(_messageId);
                current.History.Enqueue(_messageId);
                if (current.History.Count > 256) { current.Accepted.Remove(current.History.Dequeue()); }
            }
            return _messageId;
        }
    }

    /// <summary>Publishes an opening or closing command and waits for its exact acknowledgement.</summary>
    /// <param name="current">The session carrying the control request.</param>
    /// <param name="type">The expected control acknowledgement type.</param>
    /// <param name="token">Cancels the control exchange.</param>
    /// <returns>Completion after the corresponding bridge acknowledgement.</returns>
    private async Task ExchangeAsync(Session current, MessageType type, CancellationToken token)
    {
        ushort id = NextId(current);
        if (type == MessageType.Open)
        {
            lock (current.Replies) { current.OpenId = id; current.OpenPublished = true; }
        }
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (current.Replies) { current.Pending.Add(id, (type, completion)); }
        try
        {
            await PublishAsync(current, new GXMessage { id = id, type = (int)type, sender = current.ClientId }, token).ConfigureAwait(false);
            await completion.Task.WaitAsync(token).ConfigureAwait(false);
        }
        finally { lock (current.Replies) { current.Pending.Remove(id); } }
    }

    /// <summary>Publishes a bounded JSON protocol message with broker acknowledgement checking.</summary>
    /// <param name="current">The native session used by this publish.</param>
    /// <param name="message">The existing Gurux wire message.</param>
    /// <param name="token">Cancels publishing.</param>
    /// <returns>Completion after MQTT accepts the publish.</returns>
    private static async Task PublishAsync(Session current, GXMessage message, CancellationToken token)
    {
        var packet = new MqttApplicationMessageBuilder().WithTopic(current.Settings.Topic)
            .WithPayload(JsonSerializer.SerializeToUtf8Bytes(message, MqttJsonContext.Default.GXMessage))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce).Build();
        var result = await current.Client.PublishAsync(packet, token).ConfigureAwait(false);
        if ((int)result.ReasonCode >= 128) { throw new IOException(Resources.Get("BrokerPublishRejected")); }
    }

    /// <summary>Validates and queues protocol input without awaiting application code inside MQTTnet dispatch.</summary>
    /// <param name="current">The originating connection session.</param>
    /// <param name="args">The broker packet and exact reply topic.</param>
    /// <returns>An already completed task after bounded enqueue or control acknowledgement.</returns>
    private Task HandleMessageAsync(Session current, MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            lock (_admission) { if (!ReferenceEquals(_session, current)) { return Task.CompletedTask; } }
            var packet = args.ApplicationMessage;
            if (packet.Topic != current.ClientId || packet.Retain) { return Task.CompletedTask; }
            var payload = packet.Payload;
            if (payload.Length > MaximumFrameBytes * 2 + 4096) { throw new IOException(Resources.Get("PayloadTooLarge")); }
            GXMessage message = JsonSerializer.Deserialize(payload.ToArray(), MqttJsonContext.Default.GXMessage)
                ?? throw new IOException(Resources.Get("InvalidBridgeMessage"));
            if (message.sender != current.Settings.Topic) { return Task.CompletedTask; }
            lock (current.Replies)
            {
                if (message.id != current.OpenId && !current.Accepted.Contains(message.id)) { return Task.CompletedTask; }
                if (current.Pending.TryGetValue(message.id, out var pending))
                {
                    if (message.type == (int)MessageType.Exception)
                    {
                        pending.Completion.TrySetException(new IOException(message.exception ?? Resources.Get("BridgeRequestFailed")));
                        return Task.CompletedTask;
                    }
                    if (message.type == (int)pending.Type)
                    {
                        pending.Completion.TrySetResult();
                        return Task.CompletedTask;
                    }
                }
            }
            if (message.type == (int)MessageType.Receive || message.type == (int)MessageType.Exception || message.type == (int)MessageType.Close)
            {
                if (!current.Stop.IsCancellationRequested && !current.Inbox.Writer.TryWrite(message))
                {
                    throw new IOException(Resources.Get("ReceiveQueueFull"));
                }
            }
        }
        catch (Exception error) { StopFailedSession(current, error); }
        return Task.CompletedTask;
    }

    /// <summary>Delivers validated packets on an independent worker with generation-aware cancellation.</summary>
    /// <param name="current">The session owning the bounded packet queue.</param>
    /// <returns>Completion after all callbacks stop for this connection.</returns>
    private async Task DispatchAsync(Session current)
    {
        Session? previous = _callbackOwner.Value;
        _callbackOwner.Value = current;
        try
        {
            await current.Ready.Task.WaitAsync(current.Stop.Token).ConfigureAwait(false);
            await foreach (GXMessage message in current.Inbox.Reader.ReadAllAsync(current.Stop.Token).ConfigureAwait(false))
            {
                current.Stop.Token.ThrowIfCancellationRequested();
                if (message.type != (int)MessageType.Receive)
                {
                    throw new IOException(message.exception ?? Resources.Get("BridgeDisconnected"));
                }
                string hex = message.frame ?? string.Empty;
                if (hex.Length > MaximumFrameBytes * 3) { throw new IOException(Resources.Get("PayloadTooLarge")); }
                byte[] data = GXCommon.HexToBytes(hex);
                if (data.Length > MaximumFrameBytes) { throw new IOException(Resources.Get("PayloadTooLarge")); }
                PublishReceived(data, current.Settings.Topic);
            }
        }
        catch (OperationCanceledException) when (current.Stop.IsCancellationRequested) { }
        catch (Exception error) { StopFailedSession(current, error); }
        finally { _callbackOwner.Value = previous; }
    }

    /// <inheritdoc/>
    public override async ValueTask SendAsync(ReadOnlyMemory<byte> data, string? receiver = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (data.Length > MaximumFrameBytes) { throw new ArgumentException(Resources.Get("PayloadTooLarge"), nameof(data)); }
        Session current;
        CancellationToken stop;
        lock (_admission)
        {
            current = _session is { Open: true, Closing: null } active ? active
                : throw new InvalidOperationException(Resources.Get("MediaIsClosed"));
            if (receiver is not null && receiver != current.Settings.Topic)
            {
                throw new ArgumentException(Resources.Get("InvalidTopic"), nameof(receiver));
            }
            stop = current.Stop.Token;
        }
        byte[] owned = data.ToArray();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stop);
        using var deadline = CreateDeadline(linked.Token, current.Settings.AsyncWaitTime);
        bool entered = false;
        try
        {
            await current.SendLock.WaitAsync(deadline.Token).ConfigureAwait(false);
            entered = true;
            deadline.Token.ThrowIfCancellationRequested();
            await PublishAsync(current, new GXMessage
            {
                id = NextId(current), type = (int)MessageType.Send,
                sender = current.ClientId, frame = GXCommon.ToHex(owned, false)
            }, deadline.Token).ConfigureAwait(false);
            AddBytesSent(owned.Length);
        }
        catch (OperationCanceledException error) when (!linked.IsCancellationRequested)
        {
            throw new TimeoutException(Resources.Get("PublishTimeout"), error);
        }
        finally { if (entered) { current.SendLock.Release(); } }
    }

    /// <summary>Records the first native failure and starts fully tracked shutdown outside the MQTT callback.</summary>
    /// <param name="current">The failing connection session.</param>
    /// <param name="error">The error exposed to pending requests and subscribers.</param>
    private void StopFailedSession(Session current, Exception error)
    {
        lock (_admission)
        {
            if (!ReferenceEquals(_session, current) || current.Closing is not null) { return; }
            current.Failure ??= error;
            lock (current.Replies)
            {
                foreach (var pending in current.Pending.Values) { pending.Completion.TrySetException(error); }
            }
            _ = CloseSessionAsync(current);
        }
    }

    /// <inheritdoc/>
    public override Task CloseAsync()
    {
        lock (_admission)
        {
            return _session is null ? Task.CompletedTask : CloseSessionAsync(_session);
        }
    }

    /// <summary>Starts one shutdown operation and avoids awaiting a callback dispatcher's own completion.</summary>
    /// <param name="current">The owned session to stop.</param>
    /// <returns>Tracked shutdown, or immediate completion for a reentrant callback on this session.</returns>
    private Task CloseSessionAsync(Session current)
    {
        lock (_admission)
        {
            if (current.Closing is null)
            {
                current.Open = false;
                current.Stop.Cancel();
                ReceiveBuffer.Close();
                current.Inbox.Writer.TryComplete();
                current.Closing = Task.Run(() => CloseCoreAsync(current));
            }
            return ReferenceEquals(_callbackOwner.Value, current) ? Task.CompletedTask : current.Closing;
        }
    }

    /// <summary>Waits for opening and sends, bounds remote cleanup, disposes the client and then closes shared state.</summary>
    /// <param name="current">The session retained until cleanup completes.</param>
    /// <returns>Completion after native client and receive dispatcher cleanup.</returns>
    private async Task CloseCoreAsync(Session current)
    {
        await current.OpenDone.Task.ConfigureAwait(false);
        Session? previous = _callbackOwner.Value;
        _callbackOwner.Value = current;
        try
        {
            try { NotifyState(current, MediaState.Closing); }
            catch (Exception) { /* A state observer must not skip the remote close handshake. */ }
            if (current.Failure is not null)
            {
                try { ReportError(current.Failure); }
                catch (Exception) { /* Observer failures must not prevent transport cleanup. */ }
            }
            await current.SendLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (current.Client.IsConnected)
                {
                    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(
                        current.Settings.AsyncWaitTime == 0 ? 10 : Math.Min(10, current.Settings.AsyncWaitTime)));
                    if (current.OpenPublished)
                    {
                        try { await ExchangeAsync(current, MessageType.Close, deadline.Token).ConfigureAwait(false); }
                        catch (Exception) { /* A missing bridge must not prevent local shutdown. */ }
                    }
                    try
                    {
                        await current.Client.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().Build(), deadline.Token).ConfigureAwait(false);
                    }
                    catch (Exception) { /* Native disposal below also disconnects failed brokers. */ }
                }
            }
            finally { current.SendLock.Release(); }
        }
        finally
        {
            current.Client.Dispose();
            await current.Worker.ConfigureAwait(false);
            lock (_admission)
            {
                if (ReferenceEquals(_session, current))
                {
                    _session = null;
                    try { SetState(MediaState.Closed); }
                    catch (Exception) { /* Cleanup is complete even if a state observer fails. */ }
                }
            }
            current.Stop.Dispose();
            _callbackOwner.Value = previous;
        }
    }

    /// <summary>Raises lifecycle notifications under callback ownership so reentrant close cannot deadlock.</summary>
    /// <param name="current">The session whose transition is being announced.</param>
    /// <param name="state">The lifecycle transition to publish.</param>
    private void NotifyState(Session current, MediaState state)
    {
        Session? previous = _callbackOwner.Value;
        _callbackOwner.Value = current;
        try { SetState(state); }
        finally { _callbackOwner.Value = previous; }
    }
}
