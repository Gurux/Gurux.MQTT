# Gurux.MQTT.Example

This independent example owns its Core controller, Avalonia Desktop views, native
MAUI views, platform hosts, localized resources and MQTT icons. The root application
compiles its Desktop or MAUI UI sources directly and references the Core controller,
MQTT library and optional MQTT settings adapters through project references.

Configure the broker host, port, topic and client identifier in Settings, then open
the connection. This media uses Gurux's JSON request/reply bridge protocol; a Gurux
bridge must serve the selected topic. It is not a generic raw MQTT publisher.

Send UTF-8 text or hexadecimal bytes. Text mode expands literal \r and \n
sequences, including the optional reply terminator. Receive reply uses the shared
buffering contract with a minimum byte count, optional terminator and timeout.
Closing cancels an active reply. The bounded activity log displays byte values and
ASCII text; non-ASCII data stays hexadecimal.

The lower status bar shows open/closed, configured broker host and port, and topic.
Media JSON and outgoing text, hex mode and reply controls persist under the user's
local application data in Gurux/Examples/mqtt/settings.json. Restoring preferences
never opens the connection. Set GURUX_EXAMPLE_SETTINGS_DIRECTORY to isolate profiles.

Windows defaults to native MAUI; Linux uses Avalonia. Android and iOS targets are
opt-in and use the same MQTT library without USB or Bluetooth dependencies.

From this folder:

```powershell
dotnet run --project Gurux.MQTT.Example.csproj -f net11.0-windows10.0.19041.0 -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false
dotnet run --project Gurux.MQTT.Example.csproj -f net11.0 -p:BuildDesktop=true -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false
dotnet build Gurux.MQTT.Example.csproj -f net11.0-android -p:BuildMobile=true -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false
dotnet build Gurux.MQTT.Example.csproj -f net11.0-ios -p:BuildApple=true -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false
```

Run the isolated headless regressions with:

```powershell
dotnet run --project Tests/Gurux.MQTT.Example.Tests.csproj -p:GeneratePackageOnBuild=false
```

Mobile builds require their installed workloads and iOS requires an Apple build host.
Compilation and headless tests do not validate physical devices or broker deployment.


## Build requirements

Use .NET SDK 11.0.100 or a newer stable .NET 11 feature band selected by the repository's global.json. The example application and its Core library target both .NET 10 and .NET 11. Select the desired target with `-f`; the repository still selects the .NET 11 SDK for building, including .NET 10 outputs.

For Windows MAUI, install the matching .NET 10 or .NET 11 `maui-windows` workload. For Android, install `maui-android`; for iOS, install `maui-ios` on a Mac with matching Xcode and signing tools. Use `dotnet workload list` to check the selected SDK's workloads. Linux uses Avalonia and does not require MAUI.

Microsoft.Maui.Controls is bounded to 10.x for .NET 10 targets and 11.x for .NET 11 targets. A MAUI major-version upgrade requires matching target frameworks and workloads; updating NuGet alone is insufficient. Building all library compatibility targets also requires the .NET 10 toolchain and matching workloads.

## Linux publication on .NET 10

Publish the application `.csproj` from this directory with the repository-selected SDK. Do not publish the `.sln`: it also contains platform-specific MAUI projects, and a shared solution output directory can mix unrelated project outputs. A Linux runtime identifier selects the Avalonia desktop target automatically, including when publishing from Windows:

```sh
dotnet publish Gurux.MQTT.Example.csproj -c Release -f net10.0 -r linux-arm64 --self-contained false -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false -o publish/linux-net10
```

The target Linux machine needs the .NET 10 runtime for this framework-dependent build. Use `--self-contained true` to include the runtime, or replace `linux-arm64` with `linux-x64` for an x64 destination. .NET 11 remains available with `-f net11.0`. Publishing Linux desktop output does not require MAUI.

## Project layout

The application project stays in the example root and compiles the platform UI sources directly. Shared application logic remains in the separate `Core/` library; Avalonia sources and resources are in `UI/Desktop/`, and native MAUI sources and resources are in `UI/Maui/`. These UI folders have no separate projects. Desktop entry points and Linux packaging are in `UI/Desktop/`; MAUI startup and native platform sources are in `UI/Maui/`. Optional media settings adapter libraries remain in the media repository's separate `UI/Gurux.*` directories and are referenced directly by the application for the selected platform.

## Platform navigation

Windows MAUI and Avalonia desktop hosts expose a top menu bar with Connection, View and Settings groups. View contains independent Data and Activity Log choices; Settings contains the media editor and Communication settings. Activity history is kept in its own view instead of Communication settings. Existing connection and data actions remain available in the Data view.

Android and iOS use a native navigation-bar Menu action and platform action sheet. Opening an editor or navigation sheet temporarily reserves input, and media settings respect connection state and active operations. Switching views preserves the media session, staged data, counters and activity history. Closing the application cancels pending operations and ignores late menu results.

## Application icons

The iOS app explicitly selects the generated `appicon` asset catalog set.
Rebuild and deploy the updated app to refresh its Home screen icon.

Linux uses the example's own icon and a matching X11 window class and desktop
launcher. After publishing, register the launcher and icon for the current user
from the published application directory:

```sh
python3 Linux/install-desktop.py
```

The installer uses `XDG_DATA_HOME`, or `~/.local/share` when it is unset, without
root access. Keep the published application in that location, or run the installer
again after moving it, because the launcher uses its absolute executable path.
## Serial-style layout

Data uses separate Connection, Data and Received data cards. The connection card
contains Open and Close / cancel; media settings remain available through Settings.
The payload row contains Hexadecimal, Send and Clear. Buffered reply selection,
count, terminator and timeout remain in Communication settings. Activity Log keeps
its own Clear button. Native Windows menus use NavigationPage hosting and the same
window dimensions as Serial; Android uses an accessible hamburger navigation icon.