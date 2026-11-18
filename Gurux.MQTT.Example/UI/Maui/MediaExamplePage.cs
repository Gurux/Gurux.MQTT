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

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System.Globalization;
using Gurux.MQTT.Example.Properties;
using LayoutResources = Gurux.MQTT.Example.Maui.Properties.ExampleSettingsResources;
using System.Text.Json.Nodes;

using StatusResources = Gurux.MQTT.Example.Maui.Properties.ExampleSettingsResources;

namespace Gurux.MQTT.Example.Maui;

/// <summary>Provides native controls for the MQTT send and receive example.</summary>
public sealed class MediaExamplePage : ContentPage, IAsyncDisposable
{
    /// <summary>Displays received payloads independently of the diagnostic activity log.</summary>
    private readonly Editor _received = new() { AutomationId = "ReceivedData", IsReadOnly = true };

    /// <summary>Retains the controls for the dataview destination while navigating.</summary>
    private readonly VerticalStackLayout _dataView = new() { AutomationId = "DataView", IsVisible = true, Spacing = 12 };
    /// <summary>Retains the controls for the communicationview destination while navigating.</summary>
    private readonly VerticalStackLayout _communicationView = new() { AutomationId = "CommunicationView", IsVisible = false, Spacing = 12 };
    /// <summary>Retains the controls for the activityview destination while navigating.</summary>
    private readonly VerticalStackLayout _activityView = new() { AutomationId = "ActivityView", IsVisible = false, Spacing = 12 };
    /// <summary>Maps native commands to their existing toolbar action and availability.</summary>
    private readonly Dictionary<string, (Button Button, Func<Task> Action)> _menuActions = [];
    /// <summary>Prevents overlapping ownership of modal editors and navigation sheets.</summary>
    private bool _commandActive;

    /// <summary>Displays connection state and selected endpoints in the fixed bottom status bar.</summary>
    private readonly Label _mediaStatus = new() { AutomationId = "MediaStatus" };

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

    /// <summary>Cancels UI operations during shutdown.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    /// <summary>Owns the MQTT connection lifecycle.</summary>
    private readonly ExampleSession _session;
    /// <summary>Tracks commands whose availability depends on connection state.</summary>
    private readonly List<Button> _unsafeButtons = [];
    /// <summary>Allows Close to cancel pending sends or close an open connection.</summary>
    private Button? _closeButton;
    /// <summary>Tracks the single shutdown operation.</summary>
    private Task? _disposal;
    /// <summary>Refreshes connection state on the UI thread.</summary>
    private readonly Microsoft.Maui.Dispatching.IDispatcherTimer _timer;
    /// <summary>Displays the current connection state.</summary>
    private readonly Label _state = new();
    /// <summary>Displays action errors and replies.</summary>
    private readonly Label _error = new();
    /// <summary>Displays the bounded activity history.</summary>
    private readonly Editor _log = new() { IsReadOnly = true, HeightRequest = 240, AutomationId = "ActivityLog" };
    /// <summary>Contains the outgoing payload.</summary>
    private readonly Entry _text = new() { AutomationId = "SendText", Placeholder = ExampleResources.SendPlaceholder };
    /// <summary>Contains the optional reply terminator.</summary>
    private readonly Entry _eop = new() { AutomationId = "ReplyEop", Placeholder = ExampleResources.EopPlaceholder };
    /// <summary>Contains the minimum reply byte count.</summary>
    private readonly Entry _count = new() { AutomationId = "ReplyCount", Text = "0" };
    /// <summary>Contains the reply timeout in milliseconds.</summary>
    private readonly Entry _timeout = new() { AutomationId = "ReplyTimeout", Text = "5000" };
    /// <summary>Selects hexadecimal data encoding.</summary>
    private readonly Switch _hex = new() { AutomationId = "HexMode" };
    /// <summary>Selects whether sending waits for a reply.</summary>
    private readonly Switch _reply = new() { AutomationId = "ReceiveReply" };
    /// <summary>Stores the latest connection phase independently of periodic UI refreshes.</summary>
    private int _mediaPhase;
    /// <summary>Prevents callbacks and queued refreshes from touching a disposed view.</summary>
    private int _shuttingDown;
    /// <summary>Marshals transport state notifications onto the native UI thread.</summary>
    private readonly Microsoft.Maui.Dispatching.IDispatcher _uiDispatcher;

    /// <summary>Creates Serial-style connection, payload and reception cards while retaining MQTT reply settings and activity.</summary>
    public MediaExamplePage()
    {
        Title = ExampleResources.Title;
        _session = new();
        ExampleLayout.Apply(this);
        ExampleLayout.Activity(_received);
        ExampleLayout.Activity(_log);
        ExampleLayout.Notice(_error);
        var body = ExampleLayout.Body();
        var toolbar = ExampleLayout.Row();
        var options = ExampleLayout.Row(ExampleLayout.Toggle(_hex, ExampleResources.Hexadecimal));
        var sendActions = OperatingSystem.IsWindows() ? options : ExampleLayout.Row();
        if (OperatingSystem.IsWindows()) { _text.FontFamily = "Consolas"; }
        body.Children.Add(ExampleLayout.Header(_state));
        AddButton(toolbar, "Open", ExampleResources.Open, () => _session.OpenAsync(_lifetime.Token), true);
        AddButton(toolbar, "Close", ExampleResources.Close, () => _session.CloseAsync());
        AddButton(null, "Settings", ExampleResources.Settings, async () => { await new global::Gurux.MQTT.Maui.MauiMediaSettingsEditor(this).ShowAsync(_session.Media, cancellationToken: _lifetime.Token); }, true);
        AddButton(sendActions, "Send", ExampleResources.Send, async () =>
        {
            string? result = await _session.SendAsync(_text.Text ?? "", _hex.IsToggled, _reply.IsToggled,
                string.IsNullOrEmpty(_eop.Text) ? null : _eop.Text, int.Parse(_count.Text ?? "0", CultureInfo.CurrentCulture),
                int.Parse(_timeout.Text ?? "5000", CultureInfo.CurrentCulture), _lifetime.Token);
            if (result is not null && System.Threading.Volatile.Read(ref _shuttingDown) == 0) { _error.Text = result; }
        }, true);
        AddButton(sendActions, "Clear", ExampleResources.Clear, () => { _session.ClearReceived(); return Task.CompletedTask; });
        _dataView.Children.Add(ExampleLayout.Group(LayoutResources.ConnectionGroup, toolbar));
        var data = ExampleLayout.Stack(ExampleLayout.Field(LayoutResources.PayloadLabel, _text), options);
        if (!OperatingSystem.IsWindows()) { data.Children.Add(sendActions); }
        var replies = ExampleLayout.Row(ExampleLayout.Field(LayoutResources.CountLabel, _count, 164),
            ExampleLayout.Field(LayoutResources.TerminatorLabel, _eop, 164), ExampleLayout.Field(LayoutResources.TimeoutLabel, _timeout, 164));
        _dataView.Children.Add(ExampleLayout.Group(LayoutResources.MenuData, data));
        _communicationView.Children.Add(ExampleLayout.Group(LayoutResources.ReplyGroup, ExampleLayout.Stack(ExampleLayout.Toggle(_reply, ExampleResources.ReceiveReply), replies)));
        _dataView.Children.Add(ExampleLayout.Group(ExampleResources.ReceivedData, _received));
        var activityActions = ExampleLayout.Row();
        AddButton(activityActions, "ClearActivity", ExampleResources.Clear, () => { _session.ClearActivity(); return Task.CompletedTask; });
        _activityView.Children.Add(activityActions);
        _activityView.Children.Add(ExampleLayout.Group(LayoutResources.MenuActivity, _log));
        body.Children.Add(_dataView);
        body.Children.Add(_communicationView);
        body.Children.Add(_activityView);
        body.Children.Add(_error);
        Content = ExampleLayout.WithStatus(body, _mediaStatus);
        CreateApplicationMenu();
        _uiDispatcher = Dispatcher;
        _mediaPhase = (int)(_session.Media.IsOpen ? Gurux.Common.MediaState.Open : Gurux.Common.MediaState.Closed);
        _session.Media.OnMediaStateChange += MediaStateChanged;
        _timer = Dispatcher.CreateTimer(); _timer.Interval = TimeSpan.FromMilliseconds(200);
        _timer.Tick += (_, _) => Refresh(); _timer.Start(); Refresh();
        _preferencesLoading = RestorePreferencesAsync();

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
        if (!OperatingSystem.IsWindows())
        {
            Title = ExampleResources.Title;
        }
    }
    /// <summary>Creates Windows menus or the mobile native navigation sheet entry.</summary>
    private void CreateApplicationMenu()
    {
        if (OperatingSystem.IsWindows())
        {
            var connection = new MenuBarItem { Text = LayoutResources.MenuConnection, AutomationId = "ConnectionMenu" };
            var view = new MenuBarItem { Text = LayoutResources.MenuView, AutomationId = "ViewMenu" };
            var settings = new MenuBarItem { Text = LayoutResources.MenuSettings, AutomationId = "SettingsMenu" };
            foreach (string name in new[] { "Open", "Close" }) { connection.Add(CreateCommandMenu(name)); }
            view.Add(CreateNavigationMenu("DataNavigation", LayoutResources.MenuData, "DataView"));
            view.Add(CreateNavigationMenu("ActivityNavigation", LayoutResources.MenuActivity, "ActivityView"));
            settings.Add(CreateCommandMenu("Settings"));
            settings.Add(CreateNavigationMenu("CommunicationNavigation", LayoutResources.MenuCommunication, "CommunicationView"));
            MenuBarItems.Add(connection); MenuBarItems.Add(view); MenuBarItems.Add(settings);
        }
        else
        {
            Title = ExampleResources.Title;
            var navigate = new ToolbarItem { AutomationId = "NavigationMenu", Text = LayoutResources.MenuNavigate };
            if (OperatingSystem.IsAndroid())
            {
                var icon = new FontImageSource { Glyph = "\u2630", FontFamily = "sans-serif", Size = 24 };
                icon.SetAppThemeColor(FontImageSource.ColorProperty, Microsoft.Maui.Graphics.Colors.Black, Microsoft.Maui.Graphics.Colors.White);
                navigate.IconImageSource = icon;
                navigate.Order = ToolbarItemOrder.Primary;
                navigate.Text = string.Empty;
                SemanticProperties.SetDescription(navigate, LayoutResources.MenuNavigate);
            }
            navigate.Clicked += async (_, _) => await RunAsync(ShowNavigationAsync);
            ToolbarItems.Add(navigate);
        }
    }
    /// <summary>Creates a menu action synchronized with its toolbar control.</summary>
    /// <param name="name">The existing toolbar identifier.</param>
    /// <returns>The bound native menu action.</returns>
    private MenuFlyoutItem CreateCommandMenu(string name)
    {
        var item = new MenuFlyoutItem { AutomationId = name == "Settings" ? name : name + "Menu", Text = name == "Settings" ? LayoutResources.MenuMediaSettings : _menuActions[name].Button.Text };
        item.SetBinding(MenuFlyoutItem.IsEnabledProperty, new Binding(nameof(Button.IsEnabled), source: _menuActions[name].Button));
        item.Clicked += async (_, _) => await ExecuteCommandAsync(name);
        return item;
    }
    /// <summary>Creates an action revealing a retained destination.</summary>
    /// <param name="name">The automation identifier.</param>
    /// <param name="caption">The localized caption.</param>
    /// <param name="destination">The destination identifier.</param>
    /// <returns>The native navigation action.</returns>
    private MenuFlyoutItem CreateNavigationMenu(string name, string caption, string destination)
    {
        return new MenuFlyoutItem { AutomationId = name, Text = caption, Command = new Command(() => ShowDestination(destination)) };
    }
    /// <summary>Uses the platform action sheet to navigate and open media settings.</summary>
    /// <returns>A task completing after the sheet and selected editor close.</returns>
    private async Task ShowNavigationAsync()
    {
        if (!_preferencesReady || _commandActive || System.Threading.Volatile.Read(ref _shuttingDown) != 0) { return; }
        bool availableSettings = _menuActions["Settings"].Button.IsEnabled;
        var choices = new List<string> { LayoutResources.MenuData };
        if (availableSettings) { choices.Add(LayoutResources.MenuMediaSettings); }
        choices.Add(LayoutResources.MenuCommunication);
        choices.Add(LayoutResources.MenuActivity);
        _commandActive = true;
        string? selected = null;
        try
        {
            Refresh();
            selected = await DisplayActionSheetAsync(LayoutResources.MenuNavigate, LayoutResources.MenuCancel, null,
                choices.ToArray());
        }
        finally { _commandActive = false; Refresh(); }
        if (System.Threading.Volatile.Read(ref _shuttingDown) != 0) { return; }
        if (selected == LayoutResources.MenuData) { ShowDestination("DataView"); }
        else if (selected == LayoutResources.MenuCommunication) { ShowDestination("CommunicationView"); }
        else if (selected == LayoutResources.MenuActivity) { ShowDestination("ActivityView"); }
        else if (selected == LayoutResources.MenuMediaSettings) { await ExecuteCommandAsync("Settings"); }
    }
    /// <summary>Adds an asynchronous command to the native layout.</summary>
    /// <param name="parent">The optional action row; null retains a command for the Settings menu.</param>
    /// <param name="name">The stable automation identifier.</param>
    /// <param name="caption">The localized button caption.</param>
    /// <param name="action">The asynchronous command to execute.</param>
    /// <param name="unsafeAction">Whether connection state restricts this command.</param>
    private void AddButton(Layout? parent, string name, string caption, Func<Task> action, bool unsafeAction = false)
    {
        var button = new Button { AutomationId = name, Text = caption, HorizontalOptions = LayoutOptions.Start };
        ExampleLayout.StyleButton(button, name is "Open" or "Send");
        button.Margin = new Thickness(0, 0, 8, 8);
        _menuActions[name] = (button, action);
        button.Clicked += async (_, _) => await ExecuteCommandAsync(name);
        parent?.Children.Add(button);
        if (name == "Close") { _closeButton = button; }
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
    /// <summary>Caches connection phases and schedules immediate native control updates.</summary>
    /// <param name="sender">The media reporting its lifecycle state.</param>
    /// <param name="args">The reported lifecycle phase or settings-change notification.</param>
    private void MediaStateChanged(object? sender, Gurux.Common.MediaStateEventArgs args)
    {
        if (System.Threading.Volatile.Read(ref _shuttingDown) != 0) { return; }
        if (args.State != Gurux.Common.MediaState.Changed)
        {
            System.Threading.Volatile.Write(ref _mediaPhase, (int)args.State);
        }
        if (_uiDispatcher.IsDispatchRequired) { _uiDispatcher.Dispatch(Refresh); }
        else { Refresh(); }
    }
    /// <summary>Polls selected port availability and refreshes connection status, controls, and activity history.</summary>
    private void Refresh()
    {
        if (System.Threading.Volatile.Read(ref _shuttingDown) != 0)
        {
            foreach (var button in _unsafeButtons) { button.IsEnabled = false; }
            if (_closeButton is not null) { _closeButton.IsEnabled = false; }
            return;
        }
        var phase = (Gurux.Common.MediaState)System.Threading.Volatile.Read(ref _mediaPhase);
        bool closed = phase == Gurux.Common.MediaState.Closed;
        bool busy = _session.IsBusy;
        bool open = phase == Gurux.Common.MediaState.Open;
        ExampleLayout.Status(_state, busy, open);
        if (_closeButton is not null) { _closeButton.IsEnabled = _preferencesReady && (open || busy); }
        foreach (var button in _unsafeButtons)
        {
            button.IsEnabled = _preferencesReady && !_commandActive && !busy && (button.AutomationId is "Settings" or "Open" ? closed : open);
        }
        string receivedSnapshot = _session.ReceivedData(_hex.IsToggled);
        if (_received.Text != receivedSnapshot) { _received.Text = receivedSnapshot; }
        string snapshot = string.Join(Environment.NewLine, _session.Log);
        if (_log.Text != snapshot) { _log.Text = snapshot; }
        _ = SavePreferencesAsync();
        _mediaStatus.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, StatusResources.StatusMqtt,
            _session.Media.IsOpen ? StatusResources.StatusOpen : StatusResources.StatusClosed,
            string.IsNullOrWhiteSpace(_session.Media.ServerAddress) ? StatusResources.StatusNotConfigured : _session.Media.ServerAddress,
            _session.Media.Port, string.IsNullOrWhiteSpace(_session.Media.Topic) ? StatusResources.StatusNotConfigured : _session.Media.Topic);

    }
    /// <summary>Shuts down the page and displays any transport shutdown error.</summary>
    /// <returns>A task completing when shutdown has finished.</returns>
    public async Task ShutdownAsync()
    {
        try { await DisposeAsync(); }
        catch (Exception ex) { if (System.Threading.Volatile.Read(ref _shuttingDown) == 0) { _error.Text = ex.Message; } }
    }
    /// <inheritdoc/>
    public ValueTask DisposeAsync() => new(_disposal ??= DisposeCoreAsync());
    /// <summary>Stops UI refresh and releases the owned MQTT connection.</summary>
    /// <returns>A task completing after transport and cancellation cleanup.</returns>
    private async Task DisposeCoreAsync()
    {
        System.Threading.Interlocked.Exchange(ref _shuttingDown, 1);
        if (_uiDispatcher.IsDispatchRequired) { _uiDispatcher.Dispatch(Refresh); }
        else { Refresh(); }
        _session.Media.OnMediaStateChange -= MediaStateChanged;
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
            ["Hex"] = _hex.IsToggled,
            ["ReceiveReply"] = _reply.IsToggled,
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
        _hex.IsToggled = values["Hex"]?.GetValue<bool>() ?? (_hex.IsToggled);
        _reply.IsToggled = values["ReceiveReply"]?.GetValue<bool>() ?? (_reply.IsToggled);
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
        if (_uiDispatcher.IsDispatchRequired)
        {
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(SavePreferencesAsync);
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
