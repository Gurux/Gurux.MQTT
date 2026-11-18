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
using System.Text.Json;
using System.Xml.Linq;
using Gurux.MQTT.Properties;
namespace Gurux.MQTT;

/// <summary>Serializes MQTT settings independently of transport and UI state.</summary>
public partial class GXMqtt
{
    /// <summary>Stores the most recently committed immutable connection settings.</summary>
    private MqttSettings _configuration = new();
    /// <summary>Gets or sets the broker host name or IP address while closed.</summary>
    public string ServerAddress { get => _configuration.ServerAddress; set => ChangeSettings(s => s with { ServerAddress = value ?? string.Empty }); }
    /// <summary>Gets or sets the broker TCP port, validated when opening.</summary>
    public int Port { get => _configuration.Port; set => ChangeSettings(s => s with { Port = value }); }
    /// <summary>Gets or sets the exact Gurux bridge meter request topic.</summary>
    public string Topic { get => _configuration.Topic; set => ChangeSettings(s => s with { Topic = value ?? string.Empty }); }
    /// <summary>Gets or sets the client and reply-topic identifier; empty generates a new UUID for each opening.</summary>
    public string ClientId { get => _configuration.ClientId; set => ChangeSettings(s => s with { ClientId = value ?? string.Empty }); }
    /// <summary>Gets or sets the opening/send timeout in seconds; zero removes those deadlines.</summary>
    /// <remarks>Shutdown always bounds the remote close handshake to at most ten seconds.</remarks>
    public uint AsyncWaitTime { get => _configuration.AsyncWaitTime; set => ChangeSettings(s => s with { AsyncWaitTime = value }); }
    /// <summary>Gets or replaces legacy XML fragments with escaped values; null restores defaults.</summary>
    /// <exception cref="System.Xml.XmlException">The XML is malformed.</exception>
    /// <exception cref="InvalidOperationException">A session is active or changing state.</exception>
    public override string? Settings
    {
        get
        {
            MqttSettings s = _configuration;
            return string.Concat(new XElement("Settings", new XElement("IP", s.ServerAddress),
                new XElement("Port", s.Port), new XElement("Topic", s.Topic),
                new XElement("ClientId", s.ClientId), new XElement("AsyncWaitTime", s.AsyncWaitTime))
                .Elements().Select(e => e.ToString(SaveOptions.DisableFormatting)));
        }
        set
        {
            XElement root = XElement.Parse("<Settings>" + value + "</Settings>");
            XElement xml = root.Element("Mqtt") ?? root.Element("MQTT") ?? root;
            var parsed = new MqttSettings
            {
                ServerAddress = (string?)xml.Element("IP") ?? (string?)xml.Element("ServerAddress") ?? string.Empty,
                Port = (int?)xml.Element("Port") ?? 1883,
                Topic = (string?)xml.Element("Topic") ?? string.Empty,
                ClientId = (string?)xml.Element("ClientId") ?? string.Empty,
                AsyncWaitTime = (uint?)xml.Element("AsyncWaitTime") ?? 10
            };
            ChangeSettings(_ => parsed);
        }
    }
    /// <summary>Gets or replaces the portable configuration using source-generated JSON; null restores defaults.</summary>
    /// <exception cref="JsonException">JSON is malformed or contains a null root.</exception>
    /// <exception cref="InvalidOperationException">A session is active or changing state.</exception>
    public override string? JsonSettings
    {
        get => JsonSerializer.Serialize(_configuration, MqttJsonContext.Default.MqttSettings);
        set
        {
            MqttSettings parsed = value is null ? new() : JsonSerializer.Deserialize(value, MqttJsonContext.Default.MqttSettings) ?? throw new JsonException();
            ChangeSettings(_ => parsed);
        }
    }
    /// <summary>Applies a fully parsed configuration only when no session owns the transport.</summary>
    /// <param name="change">Returns the replacement immutable settings.</param>
    /// <exception cref="InvalidOperationException">Opening, closing or an active session prevents editing.</exception>
    private void ChangeSettings(Func<MqttSettings, MqttSettings> change)
    {
        lock (_admission)
        {
            if (_session is not null) { throw new InvalidOperationException(Resources.Get("CloseBeforeSettings")); }
            MqttSettings parsed = change(_configuration);
            _configuration = parsed with
            {
                ServerAddress = parsed.ServerAddress ?? string.Empty,
                Topic = parsed.Topic ?? string.Empty,
                ClientId = parsed.ClientId ?? string.Empty
            };
        }
    }
    /// <inheritdoc/>
    public override IReadOnlyList<GXValidationError> Validate()
    {
        MqttSettings s = _configuration;
        var errors = new List<GXValidationError>();
        if (string.IsNullOrWhiteSpace(s.ServerAddress))
        {
            errors.Add(new(nameof(ServerAddress), "Required", Resources.Get("InvalidBrokerName")));
        }
        if (s.Port < 1 || s.Port > 65535)
        {
            errors.Add(new(nameof(Port), "OutOfRange", Resources.Get("InvalidBrokerPort")));
        }
        if (!IsTopic(s.Topic))
        {
            errors.Add(new(nameof(Topic), string.IsNullOrWhiteSpace(s.Topic) ? "Required" : "InvalidValue", Resources.Get("InvalidTopic")));
        }
        if (s.ClientId.Length != 0 && !IsTopic(s.ClientId))
        {
            errors.Add(new(nameof(ClientId), "InvalidValue", Resources.Get("InvalidClient")));
        }
        else if (s.ClientId.Length != 0 && s.ClientId == s.Topic)
        {
            errors.Add(new(nameof(ClientId), "InvalidCombination", Resources.Get("InvalidClient")));
        }
        return errors;
    }

    /// <summary>Rejects invalid settings before allocating any transport resources.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The broker port is outside the TCP port range.</exception>
    /// <exception cref="ArgumentException">A required broker name or an exact MQTT topic is invalid.</exception>
    private void ValidateForOpen()
    {
        IReadOnlyList<GXValidationError> errors = Validate();
        if (errors.Count != 0)
        {
            GXValidationError error = errors[0];
            if (error.PropertyName == nameof(Port))
            {
                throw new ArgumentOutOfRangeException(error.PropertyName, error.Message);
            }
            throw new ArgumentException(error.Message, error.PropertyName);
        }
    }
    /// <summary>Checks an exact MQTT publish/reply topic without permitting wildcard or NUL characters.</summary>
    /// <param name="value">The topic identifier to validate.</param>
    /// <returns>True for a nonblank exact topic within the MQTT UTF-8 length limit.</returns>
    private static bool IsTopic(string value) => !string.IsNullOrWhiteSpace(value)
        && value.IndexOfAny(['+', '#', '\0']) < 0 && System.Text.Encoding.UTF8.GetByteCount(value) <= ushort.MaxValue;
}
