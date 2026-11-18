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

using System.Globalization;
using Gurux.Common;
using Gurux.MQTT.Example.Properties;
namespace Gurux.MQTT.Example;
/// <summary>Owns one MQTT connection and serializes sending and receiving operations.</summary>
public sealed class ExampleSession : IAsyncDisposable
{
    /// <summary>Clears retained activity without changing received payloads, connection state or traffic counters.</summary>
    public void ClearActivity() => _log.Clear();
    /// <summary>Stores received payloads independently of diagnostic activity.</summary>
    private readonly Queue<byte[]> _receivedData = new();
    /// <summary>Tracks retained payload bytes to bound display history to 64 KiB.</summary>
    private int _receivedDataSize;

    /// <summary>Clears received display history without changing the connection, counters or activity log.</summary>
    public void ClearReceived()
    {
        lock (_sync)
        {
            _receivedData.Clear();
            _receivedDataSize = 0;
        }
    }

    /// <summary>Formats the bounded received payload history in arrival order.</summary>
    /// <param name="hex">Whether every payload must be shown as hexadecimal.</param>
    /// <returns>The received frames separated by platform line endings.</returns>
    public string ReceivedData(bool hex = false)
    {
        lock (_sync)
        {
            return string.Join(Environment.NewLine, _receivedData.Select(data => ExampleDataDisplay.Format(data, hex)));
        }
    }

    /// <summary>Copies incoming data into the independently bounded received display history.</summary>
    /// <param name="data">The received bytes or textual payload reported by the media.</param>
    private void RecordReceived(object? data)
    {
        byte[] payload = data is byte[] bytes ? bytes : System.Text.Encoding.UTF8.GetBytes(Convert.ToString(data, CultureInfo.CurrentCulture) ?? "");
        if (payload.Length == 0)
        {
            return;
        }
        byte[] retained = payload.AsSpan(Math.Max(0, payload.Length - 65536)).ToArray();
        lock (_sync)
        {
            _receivedData.Enqueue(retained);
            _receivedDataSize += retained.Length;
            while (_receivedData.Count > 500 || _receivedDataSize > 65536)
            {
                _receivedDataSize -= _receivedData.Dequeue().Length;
            }
        }
    }

    /// <summary>Serializes connection and send operations.</summary>
    private readonly SemaphoreSlim _operations = new(1, 1);
    /// <summary>Protects cancellation and disposal state.</summary>
    private readonly object _sync = new();
    /// <summary>Stores the bounded activity history.</summary>
    private readonly SessionLog _log = new();
    /// <summary>Cancels active operations when closing.</summary>
    private CancellationTokenSource _lifetime = new();
    /// <summary>Tracks whether disposal has started.</summary>
    private bool _disposed;
    /// <summary>Tracks the active serialized operation.</summary>
    private int _busy;
    /// <summary>Gets the MQTT media owned by this session.</summary>
    public GXMqtt Media { get; }
    /// <summary>Gets whether an open or send operation is running.</summary>
    public bool IsBusy => Volatile.Read(ref _busy) != 0;
    /// <summary>Gets a snapshot of recent activity.</summary>
    public IReadOnlyList<string> Log => _log.Snapshot;
    /// <summary>Creates a session owning an independent MQTT bridge connection.</summary>
    public ExampleSession() : this(new GXMqtt()) { }
    /// <summary>Takes ownership of an existing MQTT connection.</summary>
    /// <param name="media">The connection owned and disposed by this session.</param>
    /// <exception cref="ArgumentNullException">The MQTT connection is null.</exception>
    public ExampleSession(GXMqtt media)
    {
        Media = media ?? throw new ArgumentNullException(nameof(media));
        Media.OnReceived += Received;
        Media.OnError += Error;
    }
    /// <summary>Records incoming bytes as hexadecimal and ASCII text, omitting decoded text for non-ASCII payloads.</summary>
    /// <param name="sender">The originating MQTT media.</param>
    /// <param name="args">The received payload.</param>
    private void Received(object? sender, ReceiveEventArgs args)
    {
        RecordReceived(args.Data);
        if (args.Data is byte[] bytes)
        {
            _log.Add(ExampleDataDisplay.IsAscii(bytes)
                ? string.Format(CultureInfo.CurrentCulture, ExampleResources.RxBytes, args.SenderInfo, Convert.ToHexString(bytes), ExampleDataDisplay.Format(bytes))
                : string.Format(CultureInfo.CurrentCulture, ExampleResources.RxData, args.SenderInfo, ExampleDataDisplay.Format(bytes)));
        }
        else
        {
            _log.Add(string.Format(CultureInfo.CurrentCulture, ExampleResources.RxData, args.SenderInfo, args.Data));
        }
    }
    /// <summary>Records transport errors for the native activity view.</summary>
    /// <param name="sender">The originating MQTT media.</param>
    /// <param name="args">The reported transport error.</param>
    private void Error(object? sender, MediaErrorEventArgs args) => _log.Add(string.Format(CultureInfo.CurrentCulture, ExampleResources.Error, args.Exception.Message));
    /// <summary>Runs one operation under the session cancellation lifetime.</summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="action">The asynchronous operation.</param>
    /// <param name="cancellationToken">Cancels waiting and execution.</param>
    /// <returns>The result produced by the operation.</returns>
    /// <exception cref="ObjectDisposedException">Disposal has started.</exception>
    private async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        CancellationToken token;
        lock (_sync) { ObjectDisposedException.ThrowIf(_disposed, this); token = _lifetime.Token; }
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationToken);
        await _operations.WaitAsync(linked.Token).ConfigureAwait(false);
        Interlocked.Exchange(ref _busy, 1);
        try { linked.Token.ThrowIfCancellationRequested(); return await action(linked.Token).ConfigureAwait(false); }
        finally { Interlocked.Exchange(ref _busy, 0); _operations.Release(); }
    }
    /// <summary>Opens the selected MQTT transport.</summary>
    /// <param name="cancellationToken">Cancels the open operation.</param>
    /// <returns>A task completing when the connection opens.</returns>
    public Task OpenAsync(CancellationToken cancellationToken = default) => RunAsync(async token =>
    {
        await Media.OpenAsync(token).ConfigureAwait(false);
        _log.Add(ExampleResources.Opened);
        return true;
    }, cancellationToken);
    /// <summary>Cancels active work and closes the MQTT transport.</summary>
    /// <returns>A task completing after active work and transport shutdown finish.</returns>
    public async Task CloseAsync()
    {
        lock (_sync) { _lifetime.Cancel(); }
        var closing = Media.CloseAsync();
        await _operations.WaitAsync().ConfigureAwait(false);
        try
        {
            await closing.ConfigureAwait(false);
            lock (_sync) { _lifetime.Dispose(); _lifetime = new(); }
            _log.Add(ExampleResources.ClosedLog);
        }
        finally { _operations.Release(); }
    }
    /// <summary>Sends text or hexadecimal bytes and optionally waits for a framed reply.</summary>
    /// <param name="text">The UTF-8 text or hexadecimal bytes to send; in text mode, literal \r and \n sequences become carriage returns and line feeds.</param>
    /// <param name="hex">Whether the text and end marker are hexadecimal.</param>
    /// <param name="receiveReply">Whether to wait synchronously for a reply.</param>
    /// <param name="eop">The optional end of packet marker.</param>
    /// <param name="count">The minimum reply byte count, or zero for marker based framing.</param>
    /// <param name="waitTime">The receive timeout in milliseconds.</param>
    /// <param name="cancellationToken">Cancels sending and receiving.</param>
    /// <returns>The reply as ASCII text or hexadecimal when selected or non-ASCII, or null when no reply was requested.</returns>
    /// <exception cref="TimeoutException">No complete reply arrives before the timeout.</exception>
    /// <exception cref="FormatException">A hexadecimal value is malformed.</exception>
    public Task<string?> SendAsync(string text, bool hex = false, bool receiveReply = false, string? eop = null, int count = 0, int waitTime = 5000, CancellationToken cancellationToken = default)
        => RunAsync<string?>(async token =>
        {
            byte[] data = ExampleDataDisplay.Encode(text, hex);
            var parameters = new ReceiveParameters<byte[]> { Count = count, WaitTime = waitTime, Eop = !receiveReply || string.IsNullOrEmpty(eop) ? null : ExampleDataDisplay.Encode(eop, hex), AllData = count == 0 && string.IsNullOrEmpty(eop) };
            using var scope = receiveReply ? Media.BeginSynchronous() : null;
            if (receiveReply) { Media.ResetSynchronousBuffer(); }
            await Media.SendAsync(data, cancellationToken: token).ConfigureAwait(false);
            _log.Add(ExampleDataDisplay.IsAscii(data)
                ? string.Format(CultureInfo.CurrentCulture, ExampleResources.TxBytes, Convert.ToHexString(data), ExampleDataDisplay.Format(data))
                : string.Format(CultureInfo.CurrentCulture, ExampleResources.Tx, ExampleDataDisplay.Format(data, hex)));
            if (!receiveReply) { return null; }
            if (!await Media.ReceiveAsync(parameters, token).ConfigureAwait(false)) { throw new TimeoutException(ExampleResources.Timeout); }
            var reply = parameters.Reply ?? [];
            RecordReceived(reply);
            var result = ExampleDataDisplay.Format(reply, hex);
            _log.Add(ExampleDataDisplay.IsAscii(reply)
                ? string.Format(CultureInfo.CurrentCulture, ExampleResources.ReplyBytes, Convert.ToHexString(reply), ExampleDataDisplay.Format(reply))
                : string.Format(CultureInfo.CurrentCulture, ExampleResources.Reply, result));
            return result;
        }, cancellationToken);
    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        lock (_sync) { if (_disposed) { return; } _disposed = true; }
        await CloseAsync().ConfigureAwait(false);
        Media.OnReceived -= Received;
        Media.OnError -= Error;
        await Media.DisposeAsync().ConfigureAwait(false);
    }
}
