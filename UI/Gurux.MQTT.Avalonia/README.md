# Gurux.MQTT.Avalonia

## Why use this component?

Use this optional package to add a ready-made settings editor for `GXMqtt` to an existing Avalonia desktop application. It supplies localized fields, input validation and a staged Save/Cancel workflow so the host does not have to implement the media settings form itself.

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
| `Gurux.MQTT.Avalonia` | Settings controls and editing transactions for this framework and media type. |
| Host application | Owns the media, opens/closes connections, sends/receives data, persists accepted settings and supplies platform permissions. |
| [Gurux.MQTT.Example](../../Gurux.MQTT.Example/Gurux.MQTT.Example.csproj) | A complete example application with connection/data controls and activity views. Its application screens are separate from this reusable settings adapter. |

This package is for configuration forms. Adding it does not supply a complete communication application, automatically connect a device or persist settings.

## Editing and ownership

Keep the supplied media closed while editing. The editor stages changes in a private, unopened draft; Save validates before applying settings to the caller-owned media. Cancel discards pending edits. Open media and conflicting external changes are rejected.

The host retains ownership of the media and disposes it when appropriate. Dispose host-owned embedded views or drafts when their containing form ends. A successful Save updates the media configuration; the application decides whether to persist it and when to open a connection.

Use AvaloniaMediaSettingsEditor for a modal editor, or MediaSettingsView for controls embedded in a host-owned form.

This editor configures Gurux's JSON bridge protocol. It does not provide a broker, a meter bridge or a generic MQTT topic explorer. The host must supply a reachable broker and a compatible bridge serving the selected meter topic.

## Integration details

Optional Avalonia 12 settings UI for `GXMqtt` on .NET 11 and .NET 10. Register `new Gurux.MQTT.Avalonia.AvaloniaMediaSettingsEditor(owner)` as `media.SettingsEditor`. Show the owner before calling `PropertiesAsync`. `AvaloniaMediaWindow` supplies an optional parent. Calls dispatch to the UI thread; overlapping dialogs on one editor are rejected. Owner closure cancels the draft.

Edits use an unopened private MQTT settings copy. Save validates the copy, rejects open media and detects changes made while editing. Cancel and cancellation discard edits. The caller retains ownership of the original media.

Fields follow `GXMqtt.ConfigurableSettings`: `Host` controls broker address, followed by `Port`, `Topic`, `ClientId` and `AsyncWaitTime`. Timeout values are seconds. Hidden settings retain their original values. All labels and validation messages come from `Properties/Resources.resx`. This UI preserves the MQTT bridge JSON protocol and adds no credential settings.

Portable staging tests for all three adapters and Blazor field validation:

```powershell
dotnet run --project Gurux.MQTT/UI/Gurux.MQTT.Blazor/Tests/Tests.csproj -p:GeneratePackageOnBuild=false
```

These headless tests do not validate rendered controls, platform handlers, broker integration or physical devices.

## Embedded settings

Use `new MediaSettingsView(media)` to place the native settings controls directly
inside a host form. The caller owns the closed media and must keep it closed.
Call `view.Commit()` only from the host's Save action, then capture or persist the
accepted media settings. A successful commit can be retried after a persistence
failure; external media changes still cause a conflict. Dispose the view when the
host closes, including Cancel, to discard uncommitted controls. Dispose the
caller-owned media separately. Creating or committing the view never opens a
transport. `AvaloniaMediaSettingsEditor` remains available for modal settings.
