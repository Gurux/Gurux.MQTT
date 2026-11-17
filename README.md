# Gurux.MQTT

Gurux.MQTT provides asynchronous access to remote meter connections over an MQTT broker on .NET 10 and .NET 11. It is useful when the application cannot connect directly to the meter, for example when the meter-side network uses a dynamic IP address.

**GXMqtt uses the Gurux JSON bridge protocol.** A Gurux-compatible bridge must serve the selected meter topic. It is not a generic raw-topic MQTT publisher or subscriber.

The transport derives from `Gurux.Common.GXMediaBase` and has no dependency on MAUI, Avalonia, Blazor or WinForms. Optional settings adapters and the example application provide UI separately.

## Components

~~~text
Application <-> Gurux.MQTT <-> MQTT broker <-> Gurux.Bridge <-> TCP/serial meter
~~~

| Component | Role |
| --- | --- |
| [Gurux.MQTT](Development/Gurux.MQTT.csproj) | Client-side media API used by the application. |
| [Gurux.Broker](Gurux.Broker/Gurux.Broker.csproj) | Included MQTT broker application. A compatible external MQTT broker can also be used. |
| [Gurux.Bridge](Gurux.Bridge/Gurux.Bridge.csproj) | Connects the broker to configured TCP or serial meter connections. |
| [Gurux.MQTT.Example](Gurux.MQTT.Example/Gurux.MQTT.Example.csproj) | Interactive application for configuring a bridge topic and exchanging text or binary data. |

The broker routes MQTT traffic; the bridge opens the actual meter connection and forwards bytes in both directions. Each bridge meter session has one owning client at a time.

## Packages and requirements

| Package | Purpose |
| --- | --- |
| Gurux.MQTT | UI-independent client media and bridge protocol. |
| Gurux.MQTT.Avalonia | Optional Avalonia settings editor. |
| Gurux.MQTT.Blazor | Optional Blazor settings components. |
| Gurux.MQTT.Maui | Optional native MAUI settings editor. |

This README describes the **9.0 API**. Earlier 8.x packages use different APIs; use a matching 9.0 package release or build the current source.

The media library targets `net10.0` and `net11.0`. The repository selects a stable .NET 11 SDK through [global.json](global.json), including for .NET 10 outputs. The library uses MQTTnet 5.x; the included broker application currently uses MQTTnet 4.x, while the bridge uses 5.x. See their project files for exact dependency versions.

The headless media can be used by Windows, Linux, macOS, Android and iOS applications with network access. Native example hosts and MAUI settings require the matching workloads and platform tooling; iOS builds require a Mac with Apple tooling.

## Connect and send

The example requires a running broker and a Gurux bridge serving `bridge-1/meter-1`. Replace these values with the actual broker endpoint and the exact topic printed by your bridge.

~~~csharp
using Gurux.MQTT;

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await using var media = new GXMqtt
{
    ServerAddress = "127.0.0.1",
    Port = 1883,
    Topic = "bridge-1/meter-1",
    AsyncWaitTime = 10
};

media.OnReceived += (_, args) =>
{
    if (args.Data is byte[] data)
    {
        Console.WriteLine(Convert.ToHexString(data));
    }
};

await media.OpenAsync(timeout.Token);
await media.SendAsync(new byte[] { 1, 2, 3 }, cancellationToken: timeout.Token);
await Task.Delay(TimeSpan.FromSeconds(1), timeout.Token);
await media.CloseAsync();
~~~

`OpenAsync()` connects to the broker, subscribes to the private reply topic and waits for the bridge's Open acknowledgement. A successful broker connection alone does not mean the meter session is open.

`SendAsync` publishes a Gurux Send request containing the byte payload encoded in the JSON frame. Completion is not a meter reply; use receive events or buffered receive for the actual response.

The optional `receiver` argument must be omitted or equal the configured `Topic`. A media instance does not switch meter destinations per send.

The delay only keeps this event-driven example alive briefly. Replace it with the lifetime or reply handling required by your application.

## Buffered request and reply

Enter buffered mode **before sending**, then keep the scope alive until the awaited receive finishes.

~~~csharp
using Gurux.Common;
using Gurux.MQTT;

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await using var media = new GXMqtt
{
    ServerAddress = "127.0.0.1",
    Topic = "bridge-1/meter-1"
};

await media.OpenAsync(timeout.Token);

using (media.BeginSynchronous())
{
    var reply = new ReceiveParameters<byte[]>
    {
        Count = 3,
        WaitTime = 5000
    };

    await media.SendAsync(new byte[] { 1, 2, 3 }, cancellationToken: timeout.Token);
    if (await media.ReceiveAsync(reply, timeout.Token) && reply.Reply is byte[] response)
    {
        Console.WriteLine(Convert.ToHexString(response));
    }
}
~~~

This example expects three reply bytes. Replace the payload and receive conditions with your meter protocol. Gurux.Common owns byte buffering, count/EOP matching, alternative terminators, peek, timeout and cancellation behavior.

A receive timeout or closure returns `false`; cancellation throws `OperationCanceledException`. With `AllData` enabled, a failed receive can still contain partial data. Do not reuse receive parameters concurrently.

`BeginSynchronous()` does not hold a Monitor lock across `await`. Use the legacy `Synchronous` lock only for entirely blocking exchanges.

## Connection settings

Configure settings while closed.

| Property | Default | Behavior |
| --- | --- | --- |
| `ServerAddress` | Empty | Required broker host name or IP address. |
| `Port` | `1883` | Broker TCP port, from 1 through 65535. |
| `Topic` | Empty | Required exact meter request topic served by the bridge. |
| `ClientId` | Empty | Client identity and private reply topic. Empty generates a fresh UUID on each opening. |
| `AsyncWaitTime` | `10` | Opening/send deadline in seconds. Zero disables these deadlines. |

Topics and explicit client identifiers must be nonblank exact MQTT topics without `+`, `#` or NUL characters, within the MQTT UTF-8 topic-length limit. An explicit `ClientId` must differ from `Topic` and should be unique to the active client. Leave it empty unless a stable identifier is required.

`Validate()` returns localized, property-specific errors without connecting. `OpenAsync()` rejects invalid settings before allocating the session.

`Settings` preserves escaped legacy XML fragments using `IP`, `Port`, `Topic`, `ClientId` and `AsyncWaitTime`. `JsonSettings` uses source-generated JSON metadata for the same configuration. Null resets defaults; malformed input does not partially replace configuration. Opening, open and closing sessions reject edits.

Current connection settings expose a TCP broker endpoint, topics and timeout; they do not expose broker username/password or TLS configuration.

## Local broker and bridge setup

Run these commands from the repository directory, using separate terminals for long-running applications. Building the bridge from source uses the sibling Gurux.Net and Gurux.Serial repositories in the GXMedias workspace.

### 1. Start a broker

~~~powershell
dotnet run --project Gurux.Broker/Gurux.Broker.csproj -f net10.0 -p:GeneratePackageOnBuild=false -- -p 1883
~~~

Alternatively, use a compatible existing broker and configure its address/port in the bridge and client.

### 2. Generate bridge configuration

~~~powershell
dotnet run --project Gurux.Bridge/Gurux.Bridge.csproj -f net10.0 -p:GeneratePackageOnBuild=false
~~~

If `connections.json` is missing, the bridge creates it in the **current working directory** and exits. Edit the generated file before starting it again. The file is read before command-line overrides are processed on later runs.

A configuration with one TCP meter and one serial meter looks like this:

~~~json
{
  "BrokerAddress": "127.0.0.1",
  "BrokerPort": 1883,
  "Name": "bridge-1",
  "Connections": [
    {
      "Name": "meter-1",
      "Type": "Net",
      "Settings": "<IP>127.0.0.1</IP><Port>4061</Port>"
    },
    {
      "Name": "meter-2",
      "Type": "Serial",
      "Settings": "<Port>COM3</Port><Bps>9600</Bps>",
      "UseOpticalHead": false
    }
  ]
}
~~~

Replace the sample addresses and port names with real meter endpoints. Each bridge needs a unique `Name`, including when copying configuration to another machine.

| Field | Meaning |
| --- | --- |
| `BrokerAddress` / `BrokerPort` | Broker endpoint used by the bridge. |
| `Name` | Unique bridge identity and topic prefix. |
| `Connections[].Name` | Meter topic suffix; omitted names use the one-based connection position. |
| `Connections[].Type` | Supported media type: `Net` or `Serial`. |
| `Connections[].Settings` | XML settings for the selected media. |
| `UseOpticalHead` | Enables the bridge's serial optical-head initialization when required by the meter. |

The sample meter topics are `bridge-1/meter-1` and `bridge-1/meter-2`. Use the exact topic printed by the bridge as `GXMqtt.Topic`.

### 3. Start the configured bridge

~~~powershell
dotnet run --project Gurux.Bridge/Gurux.Bridge.csproj -f net10.0 -p:GeneratePackageOnBuild=false -- -h 127.0.0.1 -p 1883 -t Verbose
~~~

The bridge connects to the broker, subscribes to configured meter topics and reconnects/restores subscriptions after broker disconnects. It opens the underlying meter transport when it accepts a client's Open request.

Use the client code above or configure broker address, port and meter topic in the MQTT example. Applications such as GXDLMSDirector need a media integration compatible with this API/protocol.

## Session lifecycle and limits

Each media opening creates a clean MQTT session with its own subscriptions, pending bridge acknowledgements and receive queue. Requests use MQTT QoS 2. Incoming retained messages and messages outside the expected reply topic/sender/correlation are not accepted as current session replies.

Payload counters count the original meter bytes, excluding JSON and MQTT overhead. Individual decoded frames are limited to **1 MiB**. The asynchronous callback queue holds at most **16 messages**; overflow fails the session explicitly.

A broker failure closes the media. After cleanup, explicitly call `OpenAsync` to reconnect; `GXMqtt` does not silently reconnect the application's session. The bridge application's broker reconnection is separate from this client lifecycle.

`CloseAsync` cancels active work, attempts the remote Close handshake while connected, disposes the MQTT client and awaits receive cleanup. The remote handshake is bounded to at most ten seconds even when `AsyncWaitTime` is zero. Closing never requires an unlimited remote wait.

Events are not UI-dispatched. Keep handlers short, marshal UI updates to the application dispatcher and never hold a Monitor lock across `await`.

The media implements `IGXMediaIconProvider` and returns its embedded MQTT PNG through a caller-owned `OpenIcon()` stream.

## Optional UI and example application

Each settings editor stages an unopened private draft and rejects invalid or stale saves. UI remains outside the transport library.

[Gurux.MQTT.Example](Gurux.MQTT.Example/Gurux.MQTT.Example.csproj) provides its own Core controller, Avalonia views and native MAUI hosts. It includes persisted connection/send preferences, text and hexadecimal input, buffered replies, byte logs and connection status. Restoring preferences does not open a connection.

Run the Avalonia desktop example:

~~~powershell
dotnet run --project Gurux.MQTT.Example/Gurux.MQTT.Example.csproj -f net11.0 -p:BuildDesktop=true -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false
~~~

Build its Android host:

~~~powershell
dotnet build Gurux.MQTT.Example/Gurux.MQTT.Example.csproj -f net11.0-android -p:BuildMobile=true -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false
~~~

The example has Windows, Linux, Android and opt-in iOS hosts. Native builds require matching MAUI/platform tooling. The MQTT media itself does not require serial, USB or Bluetooth access on the client device.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Opening times out | Broker reachability, exact meter topic, running bridge and bridge-side meter access. A broker connection alone is insufficient. |
| Bridge creates a file and exits | First-run behavior: edit `connections.json` in the current working directory and restart. |
| Bridge reports the session is owned | Another client already owns that meter connection. Close its session before opening from another client. |
| No reply after sending | Meter protocol, request bytes, framing/count/timeout and whether the meter actually sends a response. |
| Explicit client identifier causes conflicts | Use a unique `ClientId` distinct from `Topic`, or allow an automatically generated identity. |
| Settings edits fail | Await `CloseAsync` and session cleanup before changing connection properties. |

Use `-t Verbose` on the broker or bridge to inspect routing and protocol activity when diagnosing a local setup.

## Build and test

Run from this repository directory:

~~~powershell
dotnet build Development/Gurux.MQTT.csproj -p:GeneratePackageOnBuild=false
dotnet run --project Tests/Mqtt.Tests.csproj -p:GeneratePackageOnBuild=false
dotnet run --project Gurux.Bridge/Tests/Gurux.Bridge.Tests.csproj -p:GeneratePackageOnBuild=false
dotnet run --project UI/Gurux.MQTT.Blazor/Tests/Tests.csproj -p:GeneratePackageOnBuild=false
dotnet run --project Gurux.MQTT.Example/Tests/Gurux.MQTT.Example.Tests.csproj -p:GeneratePackageOnBuild=false
~~~

The loopback tests use a real MQTTnet broker and TCP connections. Coverage includes bridge handshakes, binary frames, per-client ownership, timeout/cancellation, reentrant shutdown, bridge reconnect/subscription restoration and cleanup. UI/example tests cover their respective headless contracts.

These tests do not validate physical serial meters, deployed broker configurations or native mobile UI/background behavior. Validate those scenarios in the intended environment.

## License and support

Gurux.MQTT is available under either [GPL-2.0-only](LICENSE) or a commercial license from Gurux Ltd. See [commercial licensing information](LICENSES/LicenseRef-Gurux-Commercial), visit [Gurux Ltd](https://www.gurux.fi/) or ask in the [Gurux forum](https://www.gurux.fi/forum).
