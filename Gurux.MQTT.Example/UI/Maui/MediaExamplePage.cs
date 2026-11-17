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
    private readonly Editor received = new() { AutomationId = "ReceivedData", IsReadOnly = true };

    /// <summary>Retains the controls for the dataview destination while navigating.</summary>
    private readonly VerticalStackLayout dataView = new() { AutomationId = "DataView", IsVisible = true, Spacing = 12 };
    /// <summary>Retains the controls for the communicationview destination while navigating.</summary>
    private readonly VerticalStackLayout communicationView = new() { AutomationId = "CommunicationView", IsVisible = false, Spacing = 12 };
    /// <summary>Retains the controls for the activityview destination while navigating.</summary>
    private readonly VerticalStackLayout activityView = new() { AutomationId = "ActivityView", IsVisible = false, Spacing = 12 };
    /// <summary>Maps native commands to their existing toolbar action and availability.</summary>
    private readonly Dictionary<string, (Button Button, Func<Task> Action)> menuActions = [];
    /// <summary>Prevents overlapping ownership of modal editors and navigation sheets.</summary>
    private bool commandActive;

    /// <summary>Displays connection state and selected endpoints in the fixed bottom status bar.</summary>
    private readonly Label mediaStatus = new() { AutomationId = "MediaStatus" };

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

    /// <summary>Cancels UI operations during shutdown.</summary>
    private readonly CancellationTokenSource lifetime = new();
    /// <summary>Owns the MQTT connection lifecycle.</summary>
    private readonly ExampleSession session;
    /// <summary>Tracks commands whose availability depends on connection state.</summary>
    private readonly List<Button> unsafeButtons = [];
    /// <summary>Allows Close to cancel pending sends or close an open connection.</summary>
    private Button? closeButton;
    /// <summary>Tracks the single shutdown operation.</summary>
    private Task? disposal;
    /// <summary>Refreshes connection state on the UI thread.</summary>
    private readonly Microsoft.Maui.Dispatching.IDispatcherTimer timer;
    /// <summary>Displays the current connection state.</summary>
    private readonly Label state = new();
    /// <summary>Displays action errors and replies.</summary>
    private readonly Label error = new();
    /// <summary>Displays the bounded activity history.</summary>
    private readonly Editor log = new() { IsReadOnly = true, HeightRequest = 240, AutomationId = "ActivityLog" };
    /// <summary>Contains the outgoing payload.</summary>
    private readonly Entry text = new() { AutomationId = "SendText", Placeholder = ExampleResources.SendPlaceholder };
    /// <summary>Contains the optional reply terminator.</summary>
    private readonly Entry eop = new() { AutomationId = "ReplyEop", Placeholder = ExampleResources.EopPlaceholder };
    /// <summary>Contains the minimum reply byte count.</summary>
    private readonly Entry count = new() { AutomationId = "ReplyCount", Text = "0" };
    /// <summary>Contains the reply timeout in milliseconds.</summary>
    private readonly Entry timeout = new() { AutomationId = "ReplyTimeout", Text = "5000" };
    /// <summary>Selects hexadecimal data encoding.</summary>
    private readonly Switch hex = new() { AutomationId = "HexMode" };
    /// <summary>Selects whether sending waits for a reply.</summary>
    private readonly Switch reply = new() { AutomationId = "ReceiveReply" };
    /// <summary>Stores the latest connection phase independently of periodic UI refreshes.</summary>
    private int mediaPhase;
    /// <summary>Prevents callbacks and queued refreshes from touching a disposed view.</summary>
    private int shuttingDown;
    /// <summary>Marshals transport state notifications onto the native UI thread.</summary>
    private readonly Microsoft.Maui.Dispatching.IDispatcher uiDispatcher;

    /// <summary>Creates Serial-style connection, payload and reception cards while retaining MQTT reply settings and activity.</summary>
    public MediaExamplePage()
    {
        Title = ExampleResources.Title;
        session = new();
        ExampleLayout.Apply(this);
        ExampleLayout.Activity(received);
        ExampleLayout.Activity(log);
        ExampleLayout.Notice(error);
        var body = ExampleLayout.Body();
        var toolbar = ExampleLayout.Row();
        var options = ExampleLayout.Row(ExampleLayout.Toggle(hex, ExampleResources.Hexadecimal));
        var sendActions = OperatingSystem.IsWindows() ? options : ExampleLayout.Row();
        if (OperatingSystem.IsWindows()) { text.FontFamily = "Consolas"; }
        body.Children.Add(ExampleLayout.Header(state));
        AddButton(toolbar, "Open", ExampleResources.Open, () => session.OpenAsync(lifetime.Token), true);
        AddButton(toolbar, "Close", ExampleResources.Close, () => session.CloseAsync());
        AddButton(null, "Settings", ExampleResources.Settings, async () => { await new global::Gurux.MQTT.Maui.MauiMediaSettingsEditor(this).ShowAsync(session.Media, cancellationToken: lifetime.Token); }, true);
        AddButton(sendActions, "Send", ExampleResources.Send, async () =>
        {
            string? result = await session.SendAsync(text.Text ?? "", hex.IsToggled, reply.IsToggled,
                string.IsNullOrEmpty(eop.Text) ? null : eop.Text, int.Parse(count.Text ?? "0", CultureInfo.CurrentCulture),
                int.Parse(timeout.Text ?? "5000", CultureInfo.CurrentCulture), lifetime.Token);
            if (result is not null && System.Threading.Volatile.Read(ref shuttingDown) == 0) { error.Text = result; }
        }, true);
        AddButton(sendActions, "Clear", ExampleResources.Clear, () => { session.ClearReceived(); return Task.CompletedTask; });
        dataView.Children.Add(ExampleLayout.Group(LayoutResources.ConnectionGroup, toolbar));
        var data = ExampleLayout.Stack(ExampleLayout.Field(LayoutResources.PayloadLabel, text), options);
        if (!OperatingSystem.IsWindows()) { data.Children.Add(sendActions); }
        var replies = ExampleLayout.Row(ExampleLayout.Field(LayoutResources.CountLabel, count, 164),
            ExampleLayout.Field(LayoutResources.TerminatorLabel, eop, 164), ExampleLayout.Field(LayoutResources.TimeoutLabel, timeout, 164));
        dataView.Children.Add(ExampleLayout.Group(LayoutResources.MenuData, data));
        communicationView.Children.Add(ExampleLayout.Group(LayoutResources.ReplyGroup, ExampleLayout.Stack(ExampleLayout.Toggle(reply, ExampleResources.ReceiveReply), replies)));
        dataView.Children.Add(ExampleLayout.Group(ExampleResources.ReceivedData, received));
        var activityActions = ExampleLayout.Row();
        AddButton(activityActions, "ClearActivity", ExampleResources.Clear, () => { session.ClearActivity(); return Task.CompletedTask; });
        activityView.Children.Add(activityActions);
        activityView.Children.Add(ExampleLayout.Group(LayoutResources.MenuActivity, log));
        body.Children.Add(dataView);
        body.Children.Add(communicationView);
        body.Children.Add(activityView);
        body.Children.Add(error);
        Content = ExampleLayout.WithStatus(body, mediaStatus);
        CreateApplicationMenu();
        uiDispatcher = Dispatcher;
        mediaPhase = (int)(session.Media.IsOpen ? Gurux.Common.MediaState.Open : Gurux.Common.MediaState.Closed);
        session.Media.OnMediaStateChange += MediaStateChanged;
        timer = Dispatcher.CreateTimer(); timer.Interval = TimeSpan.FromMilliseconds(200);
        timer.Tick += (_, _) => Refresh(); timer.Start(); Refresh();
        preferencesLoading = RestorePreferencesAsync();

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
        var item = new MenuFlyoutItem { AutomationId = name == "Settings" ? name : name + "Menu", Text = name == "Settings" ? LayoutResources.MenuMediaSettings : menuActions[name].Button.Text };
        item.SetBinding(MenuFlyoutItem.IsEnabledProperty, new Binding(nameof(Button.IsEnabled), source: menuActions[name].Button));
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
        if (!preferencesReady || commandActive || System.Threading.Volatile.Read(ref shuttingDown) != 0) { return; }
        bool availableSettings = menuActions["Settings"].Button.IsEnabled;
        var choices = new List<string> { LayoutResources.MenuData };
        if (availableSettings) { choices.Add(LayoutResources.MenuMediaSettings); }
        choices.Add(LayoutResources.MenuCommunication);
        choices.Add(LayoutResources.MenuActivity);
        commandActive = true;
        string? selected = null;
        try
        {
            Refresh();
            selected = await DisplayActionSheetAsync(LayoutResources.MenuNavigate, LayoutResources.MenuCancel, null,
                choices.ToArray());
        }
        finally { commandActive = false; Refresh(); }
        if (System.Threading.Volatile.Read(ref shuttingDown) != 0) { return; }
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
        menuActions[name] = (button, action);
        button.Clicked += async (_, _) => await ExecuteCommandAsync(name);
        parent?.Children.Add(button);
        if (name == "Close") { closeButton = button; }
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
    /// <summary>Caches connection phases and schedules immediate native control updates.</summary>
    /// <param name="sender">The media reporting its lifecycle state.</param>
    /// <param name="args">The reported lifecycle phase or settings-change notification.</param>
    private void MediaStateChanged(object? sender, Gurux.Common.MediaStateEventArgs args)
    {
        if (System.Threading.Volatile.Read(ref shuttingDown) != 0) { return; }
        if (args.State != Gurux.Common.MediaState.Changed)
        {
            System.Threading.Volatile.Write(ref mediaPhase, (int)args.State);
        }
        if (uiDispatcher.IsDispatchRequired) { uiDispatcher.Dispatch(Refresh); }
        else { Refresh(); }
    }
    /// <summary>Polls selected port availability and refreshes connection status, controls, and activity history.</summary>
    private void Refresh()
    {
        if (System.Threading.Volatile.Read(ref shuttingDown) != 0)
        {
            foreach (var button in unsafeButtons) { button.IsEnabled = false; }
            if (closeButton is not null) { closeButton.IsEnabled = false; }
            return;
        }
        var phase = (Gurux.Common.MediaState)System.Threading.Volatile.Read(ref mediaPhase);
        bool closed = phase == Gurux.Common.MediaState.Closed;
        bool busy = session.IsBusy;
        bool open = phase == Gurux.Common.MediaState.Open;
        ExampleLayout.Status(state, busy, open);
        if (closeButton is not null) { closeButton.IsEnabled = preferencesReady && (open || busy); }
        foreach (var button in unsafeButtons)
        {
            button.IsEnabled = preferencesReady && !commandActive && !busy && (button.AutomationId is "Settings" or "Open" ? closed : open);
        }
        string receivedSnapshot = session.ReceivedData(hex.IsToggled);
        if (received.Text != receivedSnapshot) { received.Text = receivedSnapshot; }
        string snapshot = string.Join(Environment.NewLine, session.Log);
        if (log.Text != snapshot) { log.Text = snapshot; }
        _ = SavePreferencesAsync();
        mediaStatus.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, StatusResources.StatusMqtt,
            session.Media.IsOpen ? StatusResources.StatusOpen : StatusResources.StatusClosed,
            string.IsNullOrWhiteSpace(session.Media.ServerAddress) ? StatusResources.StatusNotConfigured : session.Media.ServerAddress,
            session.Media.Port, string.IsNullOrWhiteSpace(session.Media.Topic) ? StatusResources.StatusNotConfigured : session.Media.Topic);

    }
    /// <summary>Shuts down the page and displays any transport shutdown error.</summary>
    /// <returns>A task completing when shutdown has finished.</returns>
    public async Task ShutdownAsync()
    {
        try { await DisposeAsync(); }
        catch (Exception ex) { if (System.Threading.Volatile.Read(ref shuttingDown) == 0) { error.Text = ex.Message; } }
    }
    /// <inheritdoc/>
    public ValueTask DisposeAsync() => new(disposal ??= DisposeCoreAsync());
    /// <summary>Stops UI refresh and releases the owned MQTT connection.</summary>
    /// <returns>A task completing after transport and cancellation cleanup.</returns>
    private async Task DisposeCoreAsync()
    {
        System.Threading.Interlocked.Exchange(ref shuttingDown, 1);
        if (uiDispatcher.IsDispatchRequired) { uiDispatcher.Dispatch(Refresh); }
        else { Refresh(); }
        session.Media.OnMediaStateChange -= MediaStateChanged;
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
            ["Hex"] = hex.IsToggled,
            ["ReceiveReply"] = reply.IsToggled,
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
        hex.IsToggled = values["Hex"]?.GetValue<bool>() ?? (hex.IsToggled);
        reply.IsToggled = values["ReceiveReply"]?.GetValue<bool>() ?? (reply.IsToggled);
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
        if (uiDispatcher.IsDispatchRequired)
        {
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(SavePreferencesAsync);
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
