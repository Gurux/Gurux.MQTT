# Gurux.MQTT.Maui

## Why use this component?

Use this optional package to add a ready-made settings editor for `GXMqtt` to an existing native .NET MAUI application. It supplies localized fields, input validation and a staged Save/Cancel workflow so the host does not have to implement the media settings form itself.

This adapter edits broker address, port, exact Gurux bridge meter topic, client identifier and operation timeout.

The media library can be used without this package in services, command-line tools, automated tests or applications that provide their own settings UI. Reference this adapter when users need to edit those settings interactively.

## Choose the UI for your application

| Host application | Settings adapter |
| --- | --- |
| Avalonia desktop | [Gurux.MQTT.Avalonia](../Gurux.MQTT.Avalonia/README.md) |
| Interactive Blazor / Razor UI | [Gurux.MQTT.Blazor](../Gurux.MQTT.Blazor/README.md) |
| Native .NET MAUI | [Gurux.MQTT.Maui](../Gurux.MQTT.Maui/README.md) |

Choose the adapter matching the host's UI framework; installing all three is unnecessary. A native MAUI host can use the MAUI adapter directly without a BlazorWebView. The Blazor adapter provides web components rather than native MAUI or Avalonia controls.

## How the pieces fit together

| Layer | Responsibility |
| --- | --- |
| [Gurux.MQTT transport](../../README.md) | Connection lifecycle, protocol operations and media settings. It remains independent of UI frameworks. |
| `Gurux.MQTT.Maui` | Settings controls and editing transactions for this framework and media type. |
| Host application | Owns the media, opens/closes connections, sends/receives data, persists accepted settings and supplies platform permissions. |
| [Gurux.MQTT.Example](../../Gurux.MQTT.Example/Gurux.MQTT.Example.csproj) | A complete example application with connection/data controls and activity views. Its application screens are separate from this reusable settings adapter. |

This package is for configuration forms. Adding it does not supply a complete communication application, automatically connect a device or persist settings.

## Editing and ownership

Keep the supplied media closed while editing. The editor stages changes in a private, unopened draft; Save validates before applying settings to the caller-owned media. Cancel discards pending edits. Open media and conflicting external changes are rejected.

The host retains ownership of the media and disposes it when appropriate. Dispose host-owned embedded views or drafts when their containing form ends. A successful Save updates the media configuration; the application decides whether to persist it and when to open a connection.

Use MauiMediaSettingsEditor for native modal/overlay editing. Adapters exposing MediaSettingsView also support embedding the same controls in a host-owned form.

This editor configures Gurux's JSON bridge protocol. It does not provide a broker, a meter bridge or a generic MQTT topic explorer. The host must supply a reachable broker and a compatible bridge serving the selected meter topic.

## Integration details

Optional native MAUI settings UI for `GXMqtt`. The transport library remains independent of MAUI.

```csharp
media.SettingsEditor = new Gurux.MQTT.Maui.MauiMediaSettingsEditor(this);
bool saved = await media.PropertiesAsync(cancellationToken: cancellationToken);
```

The owner is a `ContentPage`, a `NavigationPage` showing one, or a MAUI window with a page. `MauiMediaWindow` supplies an optional parent. The overlay restores the owner content and enabled state. Forward platform Back actions to `editor.Cancel()`. Windows and Android target .NET 10 and .NET 11; iOS is opt-in with `-p:EnableIosTarget=true`. Transport mobile targets require `-p:BuildMobile=true`; Apple targets require `-p:BuildApple=true`. iOS deployment requires Mac/Xcode.

Edits use an unopened private MQTT settings copy. Save validates the copy, rejects open media and detects changes made while editing. Cancel and cancellation discard edits. The caller retains ownership of the original media.

Fields follow `GXMqtt.ConfigurableSettings`: `Host` controls broker address, followed by `Port`, `Topic`, `ClientId` and `AsyncWaitTime`. Timeout values are seconds. Hidden settings retain their original values. All labels and validation messages come from `Properties/Resources.resx`. This UI preserves the MQTT bridge JSON protocol and adds no credential settings.

Portable staging tests for all three adapters and Blazor field validation:

```powershell
dotnet run --project Gurux.MQTT/UI/Gurux.MQTT.Blazor/Tests/Tests.csproj -p:GeneratePackageOnBuild=false
```

These headless tests do not validate rendered controls, platform handlers, broker integration or physical devices.


## Framework compatibility

The adapter targets .NET 10 and .NET 11 on the configured platforms. Each target uses its matching Microsoft.Maui.Controls major version: [10.0.110,11.0.0) for net10.0 and [11.0.10,12.0.0) for net11.0. .NET 11 Android targets require API 24 or later; .NET 10 Android targets retain API 21.

The example applications target .NET 10 and .NET 11. Use the .NET 11 SDK and matching MAUI workloads for examples; install the .NET 10 SDK and matching compatibility workloads when building the library's complete multi-target matrix. The SDK 11 installation does not by itself replace the MAUI 10 compatibility workload. Keep both SDK/workload environments available, as in the publishing workflow where present.

## Embedding settings

Use the public `MediaSettingsView(IGXMedia)` to place the existing native controls directly inside an owning form. The supplied media must be closed and remains owned by the caller. Constructing or committing the view never opens its transport.

Call `Commit()` only from the owning form's Save action. Inputs are validated before copying settings to the supplied media. A successful commit allows later edits or a persistence retry, while external settings changes still cause a conflict. Changing which embedded view is visible does not require a commit.

Call `Dispose()` on every embedded view when the owning form is saved, canceled or destroyed, then dispose any caller-owned media. Disposal discards pending input. Existing `MauiMediaSettingsEditor` and `PropertiesAsync` modal editing remain supported.
