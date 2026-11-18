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
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Gurux.MQTT.Example.Desktop.Properties;
using System.Globalization;
using Gurux.MQTT.Example.Properties;
using System.Text.Json.Nodes;

using StatusResources = Gurux.MQTT.Example.Desktop.Properties.ExampleSettingsResources;

namespace Gurux.MQTT.Example.Desktop;

/// <summary>Provides native controls for the MQTT send and receive example.</summary>
public sealed class MediaExampleWindow : Window, IAsyncDisposable
{
    /// <summary>Displays received payloads independently of the diagnostic activity log.</summary>
    private readonly TextBox _received = new() { Name = "ReceivedData", IsReadOnly = true, AcceptsReturn = true };

    /// <summary>Retains the controls for the dataview destination while navigating.</summary>
    private readonly StackPanel _dataView = new() { Name = "DataView", IsVisible = true, Spacing = 12 };
    /// <summary>Retains the controls for the communicationview destination while navigating.</summary>
    private readonly StackPanel _communicationView = new() { Name = "CommunicationView", IsVisible = false, Spacing = 12 };
    /// <summary>Retains the controls for the activityview destination while navigating.</summary>
    private readonly StackPanel _activityView = new() { Name = "ActivityView", IsVisible = false, Spacing = 12 };
    /// <summary>Maps native commands to their existing toolbar action and availability.</summary>
    private readonly Dictionary<string, (Button Button, Func<Task> Action)> _menuActions = [];
    /// <summary>Prevents overlapping ownership of modal editors and navigation sheets.</summary>
    private bool _commandActive;

    /// <summary>Displays connection state and selected endpoints in the fixed bottom status bar.</summary>
    private readonly TextBlock _mediaStatus = new() { Name = "MediaStatus" };

    /// <summary>Persists this application's media and outgoing data preferences.</summary>
    private readonly ExampleSettingsStore _preferences = new();
    /// <summary>Completes when startup preferences have been applied on the UI thread.</summary>
    private Task _preferencesLoading = Task.CompletedTask;
    /// <summary>Tracks the latest queued write so shutdown waits for the final disk update.</summary>
    private Task _preferencesSaving = Task.CompletedTask;
    /// <summary>Prevents persistence until startup restoration has completed.</summary>
    private bool _preferencesReady;
    /// <summary>Tracks the most recently requested snapshot and avoids saving unchanged controls.</summary>
    private string? _preferencesSnapshot;

    /// <summary>Stores the latest transport connection phase independently of command activity.</summary>
    private int _connectionPhase;
    /// <summary>Prevents callbacks from refreshing disposed resources.</summary>
    private int _shuttingDown;
    /// <summary>Cancels UI operations during shutdown.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    /// <summary>Owns the MQTT connection lifecycle.</summary>
    private readonly ExampleSession _session;
    /// <summary>Allows closing an open connection, including canceling an active reply.</summary>
    private Button? _closeButton;
    /// <summary>Tracks commands whose availability depends on connection state.</summary>
    private readonly List<Button> _unsafeButtons = [];
    /// <summary>Tracks the single shutdown operation.</summary>
    private Task? _disposal;
    /// <summary>Refreshes connection state on the UI thread.</summary>
    private readonly DispatcherTimer _timer;
    /// <summary>Displays the current connection state.</summary>
    private readonly TextBlock _state = new() { Name = "ConnectionState" };
    /// <summary>Displays action errors and replies.</summary>
    private readonly TextBlock _error = new() { Name = "ActionError", TextWrapping = global::Avalonia.Media.TextWrapping.Wrap };
    /// <summary>Displays the bounded activity history.</summary>
    private readonly TextBox _log = new() { Name = "ActivityLog", IsReadOnly = true, AcceptsReturn = true, MinHeight = 220 };
    /// <summary>Contains the outgoing payload.</summary>
    private readonly TextBox _text = new() { Name = "SendText", PlaceholderText = ExampleResources.SendPlaceholder };
    /// <summary>Contains the optional reply terminator.</summary>
    private readonly TextBox _eop = new() { Name = "ReplyEop", PlaceholderText = ExampleResources.EopPlaceholder };
    /// <summary>Contains the minimum reply byte count.</summary>
    private readonly TextBox _count = new() { Name = "ReplyCount", Text = "0" };
    /// <summary>Contains the reply timeout in milliseconds.</summary>
    private readonly TextBox _timeout = new() { Name = "ReplyTimeout", Text = "5000" };
    /// <summary>Selects hexadecimal data encoding.</summary>
    private readonly ToggleSwitch _hex = new() { Name = "HexMode", Content = ExampleResources.Hexadecimal, OnContent = null, OffContent = null };
    /// <summary>Selects whether sending waits for a reply.</summary>
    private readonly ToggleSwitch _reply = new() { Name = "ReceiveReply", Content = ExampleResources.ReceiveReply, OnContent = null, OffContent = null };
    /// <summary>Creates Serial-style connection, payload and reception cards while retaining MQTT reply settings and activity.</summary>
    /// <remarks>Native close requests await shared cleanup and then close the window even when cleanup fails.</remarks>
    public MediaExampleWindow()
    {
        Title = ExampleResources.Title;
        _session = new();
        ExampleLayout.ConfigureWindow(this);
        var body = ExampleLayout.CreateBody(_state);
        var connection = ExampleLayout.Toolbar(_dataView);
        AddButton(connection, "Open", ExampleResources.Open, () => _session.OpenAsync(_lifetime.Token), true);
        AddButton(connection, "Close", ExampleResources.Close, () => _session.CloseAsync());
        AddButton(null, "Settings", ExampleResources.Settings, async () => { await new global::Gurux.MQTT.Avalonia.AvaloniaMediaSettingsEditor(this).ShowAsync(_session.Media, cancellationToken: _lifetime.Token); }, true);
        _text.FontFamily = new global::Avalonia.Media.FontFamily("Cascadia Mono, DejaVu Sans Mono, Consolas, Menlo, monospace");
        var payload = ExampleLayout.Field(ExampleSettingsResources.PayloadLabel, _text);
        payload.MaxWidth = double.PositiveInfinity;
        payload.HorizontalAlignment = HorizontalAlignment.Stretch;
        var exchange = new StackPanel { Spacing = 10 };
        exchange.Children.Add(payload);
        var actions = ExampleLayout.Row(_hex);
        exchange.Children.Add(actions);
        _dataView.Children.Add(ExampleLayout.Group(ExampleSettingsResources.MenuData, exchange));
        _count.Width = 140;
        _eop.Width = 180;
        _timeout.Width = 140;
        var replySettings = new StackPanel { Spacing = 10 };
        replySettings.Children.Add(_reply);
        replySettings.Children.Add(ExampleLayout.Row(
            ExampleLayout.Field(ExampleSettingsResources.CountLabel, _count),
            ExampleLayout.Field(ExampleSettingsResources.TerminatorLabel, _eop),
            ExampleLayout.Field(ExampleSettingsResources.TimeoutLabel, _timeout)));
        _communicationView.Children.Add(ExampleLayout.Group(ExampleSettingsResources.ReplyGroup, replySettings));
        AddButton(actions, "Send", ExampleResources.Send, async () =>
        {
            string? result = await _session.SendAsync(_text.Text ?? "", _hex.IsChecked == true, _reply.IsChecked == true,
                string.IsNullOrEmpty(_eop.Text) ? null : _eop.Text, int.Parse(_count.Text ?? "0", CultureInfo.CurrentCulture),
                int.Parse(_timeout.Text ?? "5000", CultureInfo.CurrentCulture), _lifetime.Token);
            if (result is not null && System.Threading.Volatile.Read(ref _shuttingDown) == 0) { _error.Text = result; }
        }, true);
        AddButton(actions, "Clear", ExampleResources.Clear, () => { _session.ClearReceived(); return Task.CompletedTask; });
        _dataView.Children.Add(ExampleLayout.Group(ExampleResources.ReceivedData, _received));
        ExampleLayout.ConfigureLog(_received);
        ExampleLayout.ConfigureLog(_log);
        var activityActions = ExampleLayout.Row();
        AddButton(activityActions, "ClearActivity", ExampleResources.Clear, () => { _session.ClearActivity(); return Task.CompletedTask; });
        _activityView.Children.Add(activityActions);
        _activityView.Children.Add(ExampleLayout.Group(ExampleSettingsResources.MenuActivity, _log));
        body.Children.Add(_dataView);
        body.Children.Add(_communicationView);
        body.Children.Add(_activityView);
        body.Children.Add(_error);
        var frame = new DockPanel();
        var menu = CreateApplicationMenu();
        DockPanel.SetDock(menu, Dock.Top);
        frame.Children.Add(menu);
        frame.Children.Add(ExampleLayout.WithStatus(body, _mediaStatus));
        Content = frame;
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Background, (_, _) => Refresh());
        Volatile.Write(ref _connectionPhase, (int)(_session.Media.IsOpen ? MediaState.Open : MediaState.Closed));
        _session.Media.OnMediaStateChange += MediaStateChanged;
        _timer.Start(); Refresh();
        _preferencesLoading = RestorePreferencesAsync();
        Closing += async (_, args) =>
        {
            if (_disposal is null)
            {
                args.Cancel = true;
                await RunAsync(async () =>
                {
                    try
                    {
                        await DisposeAsync();
                    }
                    finally
                    {
                        // Finish the current native closing event before issuing the final close request.
                        Dispatcher.UIThread.Post(() => Close());
                    }
                });
            }
            else if (!_disposal.IsCompleted) { args.Cancel = true; }
        };
    }
    /// <summary>Records transport lifecycle events and refreshes controls on the UI thread.</summary>
    /// <param name="sender">The transport publishing the event.</param>
    /// <param name="args">The new media state.</param>
    private void MediaStateChanged(object? sender, MediaStateEventArgs args)
    {
        if (Volatile.Read(ref _shuttingDown) != 0 || args.State == MediaState.Changed)
        {
            return;
        }
        Volatile.Write(ref _connectionPhase, (int)args.State);
        if (Dispatcher.UIThread.CheckAccess())
        {
            RefreshMediaState();
        }
        else
        {
            Dispatcher.UIThread.Post(RefreshMediaState);
        }
    }
    /// <summary>Ignores queued transport notifications after shutdown begins.</summary>
    private void RefreshMediaState()
    {
        if (Volatile.Read(ref _shuttingDown) == 0)
        {
            Refresh();
        }
    }
    /// <summary>Runs an enabled toolbar command while preserving Close cancellation.</summary>
    /// <param name="name">The existing toolbar identifier.</param>
    /// <returns>A task completing after the command and UI refresh.</returns>
    private async Task ExecuteCommandAsync(string name)
    {
        if (!_preferencesReady || System.Threading.Volatile.Read(ref _shuttingDown) != 0 || !_menuActions.TryGetValue(name, out var action) || !action.Button.IsEnabled || (_commandActive && name != "Close")) { return; }
        if (name == "Close") { await RunAsync(action.Action); return; }
        _commandActive = true;
        Refresh();
        try { await RunAsync(action.Action); }
        finally { _commandActive = false; Refresh(); }
    }
    /// <summary>Reveals retained controls without rebuilding the session or clearing staged data.</summary>
    /// <param name="destination">The persistent destination identifier.</param>
    private void ShowDestination(string destination)
    {
        if (!_preferencesReady || _commandActive || System.Threading.Volatile.Read(ref _shuttingDown) != 0) { return; }
        _dataView.IsVisible = destination == "DataView";
        _communicationView.IsVisible = destination == "CommunicationView";
        _activityView.IsVisible = destination == "ActivityView";
    }
    /// <summary>Creates desktop menu groups outside the scrolling content.</summary>
    /// <returns>The native application menu.</returns>
    private Menu CreateApplicationMenu()
    {
        var connection = new MenuItem { Name = "ConnectionMenu", Header = ExampleSettingsResources.MenuConnection };
        var view = new MenuItem { Name = "ViewMenu", Header = ExampleSettingsResources.MenuView };
        var settings = new MenuItem { Name = "SettingsMenu", Header = ExampleSettingsResources.MenuSettings };
        foreach (string name in new[] { "Open", "Close" }) { connection.Items.Add(CreateCommandMenu(name)); }
        view.Items.Add(CreateNavigationMenu("DataNavigation", ExampleSettingsResources.MenuData, "DataView"));
        view.Items.Add(CreateNavigationMenu("ActivityNavigation", ExampleSettingsResources.MenuActivity, "ActivityView"));
        settings.Items.Add(CreateCommandMenu("Settings"));
        settings.Items.Add(CreateNavigationMenu("CommunicationNavigation", ExampleSettingsResources.MenuCommunication, "CommunicationView"));
        return new Menu { Name = "ApplicationMenu", Items = { connection, view, settings } };
    }
    /// <summary>Creates a menu action that mirrors the existing toolbar button.</summary>
    /// <param name="name">The toolbar identifier.</param>
    /// <returns>The bound native menu item.</returns>
    private MenuItem CreateCommandMenu(string name)
    {
        var item = new MenuItem { Name = name == "Settings" ? name : name + "Menu", Header = name == "Settings" ? ExampleSettingsResources.MenuMediaSettings : _menuActions[name].Button.Content };
        item.Bind(MenuItem.IsEnabledProperty, _menuActions[name].Button.GetObservable(Button.IsEnabledProperty));
        item.Click += async (_, _) => await ExecuteCommandAsync(name);
        return item;
    }
    /// <summary>Creates a menu action revealing a persistent destination.</summary>
    /// <param name="name">The automation identifier.</param>
    /// <param name="caption">The localized caption.</param>
    /// <param name="destination">The destination identifier.</param>
    /// <returns>The native navigation item.</returns>
    private MenuItem CreateNavigationMenu(string name, string caption, string destination)
    {
        var item = new MenuItem { Name = name, Header = caption };
        item.Click += (_, _) => ShowDestination(destination);
        return item;
    }
    /// <summary>Adds an asynchronous command to the native layout.</summary>
    /// <param name="parent">The optional action row; null retains a command for the Settings menu.</param>
    /// <param name="name">The stable automation identifier.</param>
    /// <param name="caption">The localized button caption.</param>
    /// <param name="action">The asynchronous command to execute.</param>
    /// <param name="unsafeAction">Whether connection state restricts this command.</param>
    private void AddButton(Panel? parent, string name, string caption, Func<Task> action, bool unsafeAction = false)
    {
        var button = new Button { Name = name, Content = caption, HorizontalAlignment = HorizontalAlignment.Left };
        ExampleLayout.ConfigureAction(button, name is "Open" or "Send");
        if (name == "Close") { _closeButton = button; }
        _menuActions[name] = (button, action);
        button.Click += async (_, _) => await ExecuteCommandAsync(name);
        parent?.Children.Add(button);
        if (unsafeAction) { _unsafeButtons.Add(button); }
    }
    /// <summary>Executes a command and reports any failure in the native view.</summary>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <returns>A task completing after the UI state is refreshed.</returns>
    private async Task RunAsync(Func<Task> action)
    {
        try { _error.Text = ""; Task operation = action(); Refresh(); await operation; }
        catch (OperationCanceledException) { if (System.Threading.Volatile.Read(ref _shuttingDown) == 0) { _error.Text = ExampleResources.Canceled; } }
        catch (Exception ex) { if (System.Threading.Volatile.Read(ref _shuttingDown) == 0) { _error.Text = ex.Message; } }
        finally { Refresh(); }
    }
    /// <summary>Refreshes connection status, configured broker and topic, commands and activity history.</summary>
    private void Refresh()
    {
        if (Volatile.Read(ref _shuttingDown) != 0)
        {
            if (_closeButton is not null) { _closeButton.IsEnabled = false; }
            foreach (var button in _unsafeButtons) { button.IsEnabled = false; }
            return;
        }
        MediaState phase = (MediaState)Volatile.Read(ref _connectionPhase);
        bool closed = phase == MediaState.Closed;
        bool busy = _session.IsBusy;
        bool open = phase == MediaState.Open;
        ExampleLayout.UpdateStatus(_state, open, busy);
        ExampleLayout.UpdateError(_error);
        if (_closeButton is not null) { _closeButton.IsEnabled = _preferencesReady && (open || busy); }
        foreach (var button in _unsafeButtons)
        {
            button.IsEnabled = _preferencesReady && !_commandActive && !busy && (button.Name is "Settings" or "Open" ? closed : open);
        }
        string receivedSnapshot = _session.ReceivedData(_hex.IsChecked == true);
        if (_received.Text != receivedSnapshot) { _received.Text = receivedSnapshot; }
        string snapshot = string.Join(Environment.NewLine, _session.Log);
        if (_log.Text != snapshot) { _log.Text = snapshot; }
        _ = SavePreferencesAsync();
        _mediaStatus.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, StatusResources.StatusMqtt,
            _session.Media.IsOpen ? StatusResources.StatusOpen : StatusResources.StatusClosed,
            string.IsNullOrWhiteSpace(_session.Media.ServerAddress) ? StatusResources.StatusNotConfigured : _session.Media.ServerAddress,
            _session.Media.Port, string.IsNullOrWhiteSpace(_session.Media.Topic) ? StatusResources.StatusNotConfigured : _session.Media.Topic);

    }
    /// <inheritdoc/>
    public ValueTask DisposeAsync() => new(_disposal ??= DisposeCoreAsync());
    /// <summary>Stops UI refresh and releases the owned MQTT connection.</summary>
    /// <returns>A task completing after transport and cancellation cleanup.</returns>
    private async Task DisposeCoreAsync()
    {
        Volatile.Write(ref _shuttingDown, 1);
        _session.Media.OnMediaStateChange -= MediaStateChanged;
        if (Dispatcher.UIThread.CheckAccess()) { Refresh(); }
        else { Dispatcher.UIThread.Post(Refresh); }
        _timer.Stop(); _lifetime.Cancel();
        await _preferencesLoading;
        await SavePreferencesAsync();
        await _session.DisposeAsync();
        _lifetime.Dispose();
    }
    /// <summary>Captures media configuration and outgoing controls without opening a connection.</summary>
    /// <returns>A detached JSON object containing this view's persistent preferences.</returns>
    private JsonObject CapturePreferences()
    {
        var values = new JsonObject
        {
            ["Media"] = JsonNode.Parse(_session.Media.JsonSettings ?? "{}"),
            ["SendText"] = _text.Text ?? "",
            ["Hex"] = _hex.IsChecked == true,
            ["ReceiveReply"] = _reply.IsChecked == true,
            ["ReplyEop"] = _eop.Text ?? "",
            ["ReplyCount"] = _count.Text ?? "",
            ["ReplyTimeout"] = _timeout.Text ?? ""
        };
        return values;
    }

    /// <summary>Applies stored media configuration and interface values while the transport remains closed.</summary>
    /// <param name="values">The saved preferences; missing entries retain their current defaults.</param>
    private void ApplyPreferences(JsonObject values)
    {
        if (values["Media"] is JsonObject media) { _session.Media.JsonSettings = media.ToJsonString(); }
        _text.Text = values["SendText"]?.GetValue<string>() ?? _text.Text;
        _hex.IsChecked = values["Hex"]?.GetValue<bool>() ?? (_hex.IsChecked == true);
        _reply.IsChecked = values["ReceiveReply"]?.GetValue<bool>() ?? (_reply.IsChecked == true);
        _eop.Text = values["ReplyEop"]?.GetValue<string>() ?? _eop.Text;
        _count.Text = values["ReplyCount"]?.GetValue<string>() ?? _count.Text;
        _timeout.Text = values["ReplyTimeout"]?.GetValue<string>() ?? _timeout.Text;
    }

    /// <summary>Restores preferences asynchronously before enabling interaction and keeps defaults if loading fails.</summary>
    /// <returns>A task completing after the saved values or original defaults have been applied.</returns>
    private async Task RestorePreferencesAsync()
    {
        IsEnabled = false;
        JsonObject defaults = CapturePreferences();
        try
        {
            JsonObject? values = await _preferences.LoadAsync();
            if (System.Threading.Volatile.Read(ref _shuttingDown) != 0) { return; }
            if (values is not null) { ApplyPreferences(values); }
        }
        catch (Exception ex)
        {
            if (System.Threading.Volatile.Read(ref _shuttingDown) != 0) { return; }
            ApplyPreferences(defaults);
            _error.Text = ex.Message;
        }
        _preferencesSnapshot = CapturePreferences().ToJsonString();
        _preferencesReady = true;
        IsEnabled = true;
        Refresh();
    }

    /// <summary>Persists changed controls and reports disk failures without interrupting transport cleanup.</summary>
    /// <returns>A task completing after the current snapshot has been saved or skipped.</returns>
    private async Task SavePreferencesAsync()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            await Dispatcher.UIThread.InvokeAsync(SavePreferencesAsync);
            return;
        }
        if (!_preferencesReady) { return; }
        string? snapshot = null;
        try
        {
            JsonObject values = CapturePreferences();
            snapshot = values.ToJsonString();
            if (snapshot == _preferencesSnapshot) { await _preferencesSaving; return; }
            _preferencesSnapshot = snapshot;
            _preferencesSaving = _preferences.SaveAsync(values);
            await _preferencesSaving;
        }
        catch (Exception ex)
        {
            if (_preferencesSnapshot == snapshot) { _preferencesSnapshot = null; }
            _error.Text = ex.Message;
        }
    }

}
