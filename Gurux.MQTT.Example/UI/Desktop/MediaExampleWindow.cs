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
    private readonly TextBox received = new() { Name = "ReceivedData", IsReadOnly = true, AcceptsReturn = true };

    /// <summary>Retains the controls for the dataview destination while navigating.</summary>
    private readonly StackPanel dataView = new() { Name = "DataView", IsVisible = true, Spacing = 12 };
    /// <summary>Retains the controls for the communicationview destination while navigating.</summary>
    private readonly StackPanel communicationView = new() { Name = "CommunicationView", IsVisible = false, Spacing = 12 };
    /// <summary>Retains the controls for the activityview destination while navigating.</summary>
    private readonly StackPanel activityView = new() { Name = "ActivityView", IsVisible = false, Spacing = 12 };
    /// <summary>Maps native commands to their existing toolbar action and availability.</summary>
    private readonly Dictionary<string, (Button Button, Func<Task> Action)> menuActions = [];
    /// <summary>Prevents overlapping ownership of modal editors and navigation sheets.</summary>
    private bool commandActive;

    /// <summary>Displays connection state and selected endpoints in the fixed bottom status bar.</summary>
    private readonly TextBlock mediaStatus = new() { Name = "MediaStatus" };

    /// <summary>Persists this application's media and outgoing data preferences.</summary>
    private readonly ExampleSettingsStore preferences = new();
    /// <summary>Completes when startup preferences have been applied on the UI thread.</summary>
    private Task preferencesLoading = Task.CompletedTask;
    /// <summary>Tracks the latest queued write so shutdown waits for the final disk update.</summary>
    private Task preferencesSaving = Task.CompletedTask;
    /// <summary>Prevents persistence until startup restoration has completed.</summary>
    private bool preferencesReady;
    /// <summary>Tracks the most recently requested snapshot and avoids saving unchanged controls.</summary>
    private string? preferencesSnapshot;

    /// <summary>Stores the latest transport connection phase independently of command activity.</summary>
    private int connectionPhase;
    /// <summary>Prevents callbacks from refreshing disposed resources.</summary>
    private int shuttingDown;
    /// <summary>Cancels UI operations during shutdown.</summary>
    private readonly CancellationTokenSource lifetime = new();
    /// <summary>Owns the MQTT connection lifecycle.</summary>
    private readonly ExampleSession session;
    /// <summary>Allows closing an open connection, including canceling an active reply.</summary>
    private Button? closeButton;
    /// <summary>Tracks commands whose availability depends on connection state.</summary>
    private readonly List<Button> unsafeButtons = [];
    /// <summary>Tracks the single shutdown operation.</summary>
    private Task? disposal;
    /// <summary>Refreshes connection state on the UI thread.</summary>
    private readonly DispatcherTimer timer;
    /// <summary>Displays the current connection state.</summary>
    private readonly TextBlock state = new() { Name = "ConnectionState" };
    /// <summary>Displays action errors and replies.</summary>
    private readonly TextBlock error = new() { Name = "ActionError", TextWrapping = global::Avalonia.Media.TextWrapping.Wrap };
    /// <summary>Displays the bounded activity history.</summary>
    private readonly TextBox log = new() { Name = "ActivityLog", IsReadOnly = true, AcceptsReturn = true, MinHeight = 220 };
    /// <summary>Contains the outgoing payload.</summary>
    private readonly TextBox text = new() { Name = "SendText", PlaceholderText = ExampleResources.SendPlaceholder };
    /// <summary>Contains the optional reply terminator.</summary>
    private readonly TextBox eop = new() { Name = "ReplyEop", PlaceholderText = ExampleResources.EopPlaceholder };
    /// <summary>Contains the minimum reply byte count.</summary>
    private readonly TextBox count = new() { Name = "ReplyCount", Text = "0" };
    /// <summary>Contains the reply timeout in milliseconds.</summary>
    private readonly TextBox timeout = new() { Name = "ReplyTimeout", Text = "5000" };
    /// <summary>Selects hexadecimal data encoding.</summary>
    private readonly ToggleSwitch hex = new() { Name = "HexMode", Content = ExampleResources.Hexadecimal, OnContent = null, OffContent = null };
    /// <summary>Selects whether sending waits for a reply.</summary>
    private readonly ToggleSwitch reply = new() { Name = "ReceiveReply", Content = ExampleResources.ReceiveReply, OnContent = null, OffContent = null };
    /// <summary>Creates Serial-style connection, payload and reception cards while retaining MQTT reply settings and activity.</summary>
    /// <remarks>Native close requests await shared cleanup and then close the window even when cleanup fails.</remarks>
    public MediaExampleWindow()
    {
        Title = ExampleResources.Title;
        session = new();
        ExampleLayout.ConfigureWindow(this);
        var body = ExampleLayout.CreateBody(state);
        var connection = ExampleLayout.Toolbar(dataView);
        AddButton(connection, "Open", ExampleResources.Open, () => session.OpenAsync(lifetime.Token), true);
        AddButton(connection, "Close", ExampleResources.Close, () => session.CloseAsync());
        AddButton(null, "Settings", ExampleResources.Settings, async () => { await new global::Gurux.MQTT.Avalonia.AvaloniaMediaSettingsEditor(this).ShowAsync(session.Media, cancellationToken: lifetime.Token); }, true);
        text.FontFamily = new global::Avalonia.Media.FontFamily("Cascadia Mono, DejaVu Sans Mono, Consolas, Menlo, monospace");
        var payload = ExampleLayout.Field(ExampleSettingsResources.PayloadLabel, text);
        payload.MaxWidth = double.PositiveInfinity;
        payload.HorizontalAlignment = HorizontalAlignment.Stretch;
        var exchange = new StackPanel { Spacing = 10 };
        exchange.Children.Add(payload);
        var actions = ExampleLayout.Row(hex);
        exchange.Children.Add(actions);
        dataView.Children.Add(ExampleLayout.Group(ExampleSettingsResources.MenuData, exchange));
        count.Width = 140;
        eop.Width = 180;
        timeout.Width = 140;
        var replySettings = new StackPanel { Spacing = 10 };
        replySettings.Children.Add(reply);
        replySettings.Children.Add(ExampleLayout.Row(
            ExampleLayout.Field(ExampleSettingsResources.CountLabel, count),
            ExampleLayout.Field(ExampleSettingsResources.TerminatorLabel, eop),
            ExampleLayout.Field(ExampleSettingsResources.TimeoutLabel, timeout)));
        communicationView.Children.Add(ExampleLayout.Group(ExampleSettingsResources.ReplyGroup, replySettings));
        AddButton(actions, "Send", ExampleResources.Send, async () =>
        {
            string? result = await session.SendAsync(text.Text ?? "", hex.IsChecked == true, reply.IsChecked == true,
                string.IsNullOrEmpty(eop.Text) ? null : eop.Text, int.Parse(count.Text ?? "0", CultureInfo.CurrentCulture),
                int.Parse(timeout.Text ?? "5000", CultureInfo.CurrentCulture), lifetime.Token);
            if (result is not null && System.Threading.Volatile.Read(ref shuttingDown) == 0) { error.Text = result; }
        }, true);
        AddButton(actions, "Clear", ExampleResources.Clear, () => { session.ClearReceived(); return Task.CompletedTask; });
        dataView.Children.Add(ExampleLayout.Group(ExampleResources.ReceivedData, received));
        ExampleLayout.ConfigureLog(received);
        ExampleLayout.ConfigureLog(log);
        var activityActions = ExampleLayout.Row();
        AddButton(activityActions, "ClearActivity", ExampleResources.Clear, () => { session.ClearActivity(); return Task.CompletedTask; });
        activityView.Children.Add(activityActions);
        activityView.Children.Add(ExampleLayout.Group(ExampleSettingsResources.MenuActivity, log));
        body.Children.Add(dataView);
        body.Children.Add(communicationView);
        body.Children.Add(activityView);
        body.Children.Add(error);
        var frame = new DockPanel();
        var menu = CreateApplicationMenu();
        DockPanel.SetDock(menu, Dock.Top);
        frame.Children.Add(menu);
        frame.Children.Add(ExampleLayout.WithStatus(body, mediaStatus));
        Content = frame;
        timer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Background, (_, _) => Refresh());
        Volatile.Write(ref connectionPhase, (int)(session.Media.IsOpen ? MediaState.Open : MediaState.Closed));
        session.Media.OnMediaStateChange += MediaStateChanged;
        timer.Start(); Refresh();
        preferencesLoading = RestorePreferencesAsync();
        Closing += async (_, args) =>
        {
            if (disposal is null)
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
            else if (!disposal.IsCompleted) { args.Cancel = true; }
        };
    }
    /// <summary>Records transport lifecycle events and refreshes controls on the UI thread.</summary>
    /// <param name="sender">The transport publishing the event.</param>
    /// <param name="args">The new media state.</param>
    private void MediaStateChanged(object? sender, MediaStateEventArgs args)
    {
        if (Volatile.Read(ref shuttingDown) != 0 || args.State == MediaState.Changed)
        {
            return;
        }
        Volatile.Write(ref connectionPhase, (int)args.State);
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
        if (Volatile.Read(ref shuttingDown) == 0)
        {
            Refresh();
        }
    }
    /// <summary>Runs an enabled toolbar command while preserving Close cancellation.</summary>
    /// <param name="name">The existing toolbar identifier.</param>
    /// <returns>A task completing after the command and UI refresh.</returns>
    private async Task ExecuteCommandAsync(string name)
    {
        if (!preferencesReady || System.Threading.Volatile.Read(ref shuttingDown) != 0 || !menuActions.TryGetValue(name, out var action) || !action.Button.IsEnabled || (commandActive && name != "Close")) { return; }
        if (name == "Close") { await RunAsync(action.Action); return; }
        commandActive = true;
        Refresh();
        try { await RunAsync(action.Action); }
        finally { commandActive = false; Refresh(); }
    }
    /// <summary>Reveals retained controls without rebuilding the session or clearing staged data.</summary>
    /// <param name="destination">The persistent destination identifier.</param>
    private void ShowDestination(string destination)
    {
        if (!preferencesReady || commandActive || System.Threading.Volatile.Read(ref shuttingDown) != 0) { return; }
        dataView.IsVisible = destination == "DataView";
        communicationView.IsVisible = destination == "CommunicationView";
        activityView.IsVisible = destination == "ActivityView";
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
        var item = new MenuItem { Name = name == "Settings" ? name : name + "Menu", Header = name == "Settings" ? ExampleSettingsResources.MenuMediaSettings : menuActions[name].Button.Content };
        item.Bind(MenuItem.IsEnabledProperty, menuActions[name].Button.GetObservable(Button.IsEnabledProperty));
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
        if (name == "Close") { closeButton = button; }
        menuActions[name] = (button, action);
        button.Click += async (_, _) => await ExecuteCommandAsync(name);
        parent?.Children.Add(button);
        if (unsafeAction) { unsafeButtons.Add(button); }
    }
    /// <summary>Executes a command and reports any failure in the native view.</summary>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <returns>A task completing after the UI state is refreshed.</returns>
    private async Task RunAsync(Func<Task> action)
    {
        try { error.Text = ""; Task operation = action(); Refresh(); await operation; }
        catch (OperationCanceledException) { if (System.Threading.Volatile.Read(ref shuttingDown) == 0) { error.Text = ExampleResources.Canceled; } }
        catch (Exception ex) { if (System.Threading.Volatile.Read(ref shuttingDown) == 0) { error.Text = ex.Message; } }
        finally { Refresh(); }
    }
    /// <summary>Refreshes connection status, configured broker and topic, commands and activity history.</summary>
    private void Refresh()
    {
        if (Volatile.Read(ref shuttingDown) != 0)
        {
            if (closeButton is not null) { closeButton.IsEnabled = false; }
            foreach (var button in unsafeButtons) { button.IsEnabled = false; }
            return;
        }
        MediaState phase = (MediaState)Volatile.Read(ref connectionPhase);
        bool closed = phase == MediaState.Closed;
        bool busy = session.IsBusy;
        bool open = phase == MediaState.Open;
        ExampleLayout.UpdateStatus(state, open, busy);
        ExampleLayout.UpdateError(error);
        if (closeButton is not null) { closeButton.IsEnabled = preferencesReady && (open || busy); }
        foreach (var button in unsafeButtons)
        {
            button.IsEnabled = preferencesReady && !commandActive && !busy && (button.Name is "Settings" or "Open" ? closed : open);
        }
        string receivedSnapshot = session.ReceivedData(hex.IsChecked == true);
        if (received.Text != receivedSnapshot) { received.Text = receivedSnapshot; }
        string snapshot = string.Join(Environment.NewLine, session.Log);
        if (log.Text != snapshot) { log.Text = snapshot; }
        _ = SavePreferencesAsync();
        mediaStatus.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, StatusResources.StatusMqtt,
            session.Media.IsOpen ? StatusResources.StatusOpen : StatusResources.StatusClosed,
            string.IsNullOrWhiteSpace(session.Media.ServerAddress) ? StatusResources.StatusNotConfigured : session.Media.ServerAddress,
            session.Media.Port, string.IsNullOrWhiteSpace(session.Media.Topic) ? StatusResources.StatusNotConfigured : session.Media.Topic);

    }
    /// <inheritdoc/>
    public ValueTask DisposeAsync() => new(disposal ??= DisposeCoreAsync());
    /// <summary>Stops UI refresh and releases the owned MQTT connection.</summary>
    /// <returns>A task completing after transport and cancellation cleanup.</returns>
    private async Task DisposeCoreAsync()
    {
        Volatile.Write(ref shuttingDown, 1);
        session.Media.OnMediaStateChange -= MediaStateChanged;
        if (Dispatcher.UIThread.CheckAccess()) { Refresh(); }
        else { Dispatcher.UIThread.Post(Refresh); }
        timer.Stop(); lifetime.Cancel();
        await preferencesLoading;
        await SavePreferencesAsync();
        await session.DisposeAsync();
        lifetime.Dispose();
    }
    /// <summary>Captures media configuration and outgoing controls without opening a connection.</summary>
    /// <returns>A detached JSON object containing this view's persistent preferences.</returns>
    private JsonObject CapturePreferences()
    {
        var values = new JsonObject
        {
            ["Media"] = JsonNode.Parse(session.Media.JsonSettings ?? "{}"),
            ["SendText"] = text.Text ?? "",
            ["Hex"] = hex.IsChecked == true,
            ["ReceiveReply"] = reply.IsChecked == true,
            ["ReplyEop"] = eop.Text ?? "",
            ["ReplyCount"] = count.Text ?? "",
            ["ReplyTimeout"] = timeout.Text ?? ""
        };
        return values;
    }

    /// <summary>Applies stored media configuration and interface values while the transport remains closed.</summary>
    /// <param name="values">The saved preferences; missing entries retain their current defaults.</param>
    private void ApplyPreferences(JsonObject values)
    {
        if (values["Media"] is JsonObject media) { session.Media.JsonSettings = media.ToJsonString(); }
        text.Text = values["SendText"]?.GetValue<string>() ?? text.Text;
        hex.IsChecked = values["Hex"]?.GetValue<bool>() ?? (hex.IsChecked == true);
        reply.IsChecked = values["ReceiveReply"]?.GetValue<bool>() ?? (reply.IsChecked == true);
        eop.Text = values["ReplyEop"]?.GetValue<string>() ?? eop.Text;
        count.Text = values["ReplyCount"]?.GetValue<string>() ?? count.Text;
        timeout.Text = values["ReplyTimeout"]?.GetValue<string>() ?? timeout.Text;
    }

    /// <summary>Restores preferences asynchronously before enabling interaction and keeps defaults if loading fails.</summary>
    /// <returns>A task completing after the saved values or original defaults have been applied.</returns>
    private async Task RestorePreferencesAsync()
    {
        IsEnabled = false;
        JsonObject defaults = CapturePreferences();
        try
        {
            JsonObject? values = await preferences.LoadAsync();
            if (System.Threading.Volatile.Read(ref shuttingDown) != 0) { return; }
            if (values is not null) { ApplyPreferences(values); }
        }
        catch (Exception ex)
        {
            if (System.Threading.Volatile.Read(ref shuttingDown) != 0) { return; }
            ApplyPreferences(defaults);
            error.Text = ex.Message;
        }
        preferencesSnapshot = CapturePreferences().ToJsonString();
        preferencesReady = true;
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
        if (!preferencesReady) { return; }
        string? snapshot = null;
        try
        {
            JsonObject values = CapturePreferences();
            snapshot = values.ToJsonString();
            if (snapshot == preferencesSnapshot) { await preferencesSaving; return; }
            preferencesSnapshot = snapshot;
            preferencesSaving = preferences.SaveAsync(values);
            await preferencesSaving;
        }
        catch (Exception ex)
        {
            if (preferencesSnapshot == snapshot) { preferencesSnapshot = null; }
            error.Text = ex.Message;
        }
    }

}
