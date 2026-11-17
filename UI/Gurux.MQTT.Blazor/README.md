# Gurux.MQTT.Blazor

## Why use this component?

Use this optional package to add a ready-made settings editor for `GXMqtt` to an interactive Blazor application or a Razor-based settings surface. It supplies localized fields, input validation and a staged Save/Cancel workflow so the host does not have to implement the media settings form itself.

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
| `Gurux.MQTT.Blazor` | Settings controls and editing transactions for this framework and media type. |
| Host application | Owns the media, opens/closes connections, sends/receives data, persists accepted settings and supplies platform permissions. |
| [Gurux.MQTT.Example](../../Gurux.MQTT.Example/Gurux.MQTT.Example.csproj) | A complete example application with connection/data controls and activity views. Its application screens are separate from this reusable settings adapter. |

This package is for configuration forms. Adding it does not supply a complete communication application, automatically connect a device or persist settings.

## Editing and ownership

Keep the supplied media closed while editing. The editor stages changes in a private, unopened draft; Save validates before applying settings to the caller-owned media. Cancel discards pending edits. Open media and conflicting external changes are rejected.

The host retains ownership of the media and disposes it when appropriate. Dispose host-owned embedded views or drafts when their containing form ends. A successful Save updates the media configuration; the application decides whether to persist it and when to open a connection.

Render MediaSettingsEditor for a standalone editing session. For a containing settings dialog, create and dispose a MediaSettingsDraft and render MediaSettingsForm with Embedded=true when that mode is available in the adapter.

Blazor Server edits media on the server: device discovery and transport access refer to that server, not the browser's computer. Browser WebAssembly cannot use these adapters to gain arbitrary serial-port or TCP-socket access. The host must select an interactive render mode and serve the required static web assets.

This editor configures Gurux's JSON bridge protocol. It does not provide a broker, a meter bridge or a generic MQTT topic explorer. The host must supply a reachable broker and a compatible bridge serving the selected meter topic.

## Integration details

Optional Razor settings components for `GXMqtt` on .NET 11 and .NET 10. Uses the existing Gurux.UI.Components package and its adaptive appearance controls.

Register `builder.Services.AddGuruxMediaComponents()` and add `@using Gurux.MQTT.Blazor` to the host imports. Load the package static CSS:

```html
<link data-gx-bootstrap rel="stylesheet" href="_content/Gurux.UI.Components/Appearance/themes/default.min.css" />
<link rel="stylesheet" href="_content/Gurux.UI.Components/Appearance/layout.css" />
```

Render `<MediaSettingsEditor Media="media" Completed="SettingsClosed" />` in an interactive Blazor host. `Completed` receives whether Save succeeded; remove/recreate the editor for another session. An optional cancellation token discards pending edits. Custom hosts may use `MediaSettingsDraft` and `MediaSettingsForm` directly and must dispose the draft. Blazor Server transports run on the server; browser WebAssembly cannot open arbitrary MQTT TCP sockets.

Edits use an unopened private MQTT settings copy. Save validates the copy, rejects open media and detects changes made while editing. Cancel and cancellation discard edits. The caller retains ownership of the original media.

Fields follow `GXMqtt.ConfigurableSettings`: `Host` controls broker address, followed by `Port`, `Topic`, `ClientId` and `AsyncWaitTime`. Timeout values are seconds. Hidden settings retain their original values. All labels and validation messages come from `Properties/Resources.resx`. This UI preserves the MQTT bridge JSON protocol and adds no credential settings.

Portable staging tests for all three adapters and Blazor field validation:

```powershell
dotnet run --project Gurux.MQTT/UI/Gurux.MQTT.Blazor/Tests/Tests.csproj -p:GeneratePackageOnBuild=false
```

These headless tests do not validate rendered controls, platform handlers, broker integration or physical devices.

## Embedded controls

Create a host-owned `MediaSettingsDraft` from a closed native media and render
`<MediaSettingsForm Draft="draft" Embedded="true" />` inside the containing dialog.
Embedded mode hides the form's Save and Cancel buttons and ignores local submits.
The outer Save calls `draft.Commit()` before persisting the media settings; the
outer Cancel disposes the draft without committing. Dispose both the draft and
the host-owned media when the containing session ends. Keep separate drafts to
retain uncommitted inputs when switching media types. Repeated successful commits
are supported for persistence retries while external settings changes are rejected.
The default `Embedded="false"` retains the standalone Save/Cancel behavior.
