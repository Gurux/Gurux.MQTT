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

using System.Reflection;
using System.Text.Json.Nodes;
using Gurux.MQTT.Example;

namespace Gurux.MQTT.Example.Tests;

/// <summary>Runs headless regressions for data conversion, activity limits and isolated preference persistence.</summary>
internal static class Program
{
    /// <summary>Verifies example behavior without requiring a broker or native UI.</summary>
    /// <returns>A task completing when all assertions have passed.</returns>
    /// <exception cref="InvalidOperationException">An example behavior does not meet its contract.</exception>
    private static async Task Main()
    {
        Assert(ExampleDataDisplay.Encode(@"hello\r\n").SequenceEqual("hello\r\n"u8.ToArray()), "Text escape conversion must preserve CRLF.");
        Assert(ExampleDataDisplay.Encode(" 00 7f\tFF ", true).SequenceEqual(new byte[] { 0, 127, 255 }), "Hexadecimal input must accept whitespace.");
        Assert(ExampleDataDisplay.Encode("é").SequenceEqual(new byte[] { 0xC3, 0xA9 }), "Text must use UTF-8.");
        Assert(ExampleDataDisplay.Format(new byte[] { 65, 0x80 }) == "4180", "Mixed non-ASCII data must remain hexadecimal.");
        Assert(ExampleDataDisplay.Format("OK"u8) == "OK", "ASCII replies must retain text.");
        Assert(ExampleDataDisplay.Format("OK"u8, true) == "4F4B", "Hexadecimal mode must override ASCII rendering.");
        Assert(ExampleDataDisplay.IsAscii(ReadOnlySpan<byte>.Empty), "Empty payload must remain valid ASCII.");
        try
        {
            ExampleDataDisplay.Encode("ABC", true);
            throw new InvalidOperationException("Odd hexadecimal digit count was accepted.");
        }
        catch (FormatException)
        {
        }
        Assert(Gurux.MQTT.Example.Properties.ExampleResources.Title.Contains("MQTT", StringComparison.Ordinal), "Localized core resources must be embedded under their declared name.");
        Type logType = typeof(ExampleSession).Assembly.GetType("Gurux.MQTT.Example.SessionLog", true)!;
        object log = Activator.CreateInstance(logType, nonPublic: true)!;
        MethodInfo add = logType.GetMethod("Add")!;
        for (int position = 0; position < 550; ++position)
        {
            add.Invoke(log, new object[] { position.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        }
        var snapshot = (IReadOnlyList<string>)logType.GetProperty("Snapshot")!.GetValue(log)!;
        Assert(snapshot.Count == 500 && snapshot[0].EndsWith(" 50", StringComparison.Ordinal), "Activity must discard oldest entries after 500 messages.");
        add.Invoke(log, new object[] { "new" });
        Assert(snapshot.Count == 500 && !snapshot[^1].EndsWith(" new", StringComparison.Ordinal), "Activity snapshots must remain detached from later changes.");

        string directory = Path.Combine(Path.GetTempPath(), "GuruxMqttExampleTests-" + Guid.NewGuid().ToString("N"));
        string? previous = Environment.GetEnvironmentVariable("GURUX_EXAMPLE_SETTINGS_DIRECTORY");
        Environment.SetEnvironmentVariable("GURUX_EXAMPLE_SETTINGS_DIRECTORY", directory);
        try
        {
            var store = new ExampleSettingsStore();
            Assert(await store.LoadAsync() is null, "A new profile must not contain preferences.");
            var settings = new JsonObject { ["Media"] = new JsonObject { ["Topic"] = "first/topic" }, ["SendText"] = @"hello\r\n", ["Hex"] = true, ["ReceiveReply"] = true };
            await store.SaveAsync(settings);
            settings["SendText"] = "updated";
            await store.SaveAsync(settings);
            JsonObject? restored = await new ExampleSettingsStore().LoadAsync();
            Assert(restored?["SendText"]?.GetValue<string>() == "updated" && restored?["Hex"]?.GetValue<bool>() == true && restored?["ReceiveReply"]?.GetValue<bool>() == true, "Preferences must preserve the latest complete control snapshot.");
            Assert(restored?["Media"]?["Topic"]?.GetValue<string>() == "first/topic", "Media JSON must round-trip independently of UI controls.");
            Assert(Directory.GetFiles(directory, "*.tmp", SearchOption.AllDirectories).Length == 0, "Atomic preference writes must clean temporary files.");
            string file = Path.Combine(directory, "mqtt", "settings.json");
            await File.WriteAllTextAsync(file, "{");
            try
            {
                await new ExampleSettingsStore().LoadAsync();
                throw new InvalidOperationException("Corrupt preferences were accepted.");
            }
            catch (System.Text.Json.JsonException)
            {
            }
            await store.SaveAsync(new JsonObject { ["SendText"] = "recovered" });
            Assert((await store.LoadAsync())?["SendText"]?.GetValue<string>() == "recovered", "Saving defaults must recover a malformed preference file.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("GURUX_EXAMPLE_SETTINGS_DIRECTORY", previous);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>Fails the executable when a contract assertion is false.</summary>
    /// <param name="condition">Whether the expected behavior was observed.</param>
    /// <param name="message">The developer diagnostic reported for a failed assertion.</param>
    /// <exception cref="InvalidOperationException">The assertion fails.</exception>
    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
