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
using System.Globalization;
namespace Gurux.MQTT.Blazor;
/// <summary>Builds localized controls and applies staged values to an owned media copy.</summary>
public sealed class SettingsFields
{
    /// <summary>Gets the controls in display order.</summary>
    public List<SettingsField> Fields { get; } = [];
    /// <summary>Stores callbacks that update only the private copy.</summary>
    private readonly List<Action> _read = [];
    /// <summary>Applies staged control values to the private copy.</summary>
    public void Apply() { foreach (var read in _read) { read(); } }
    /// <summary>Adds a staged text or password control.</summary>
    /// <param name="label">Localized control label.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="set">Applies the staged value to the private media copy.</param>
    /// <param name="password">Whether to mask the value.</param>
    /// <param name="multiline">Whether to allow multiple lines.</param>
    public void Text(string label, string? value, Action<string> set, bool password = false, bool multiline = false)
    {
        var field = new SettingsField(label, value ?? "", password ? "password" : multiline ? "multiline" : "text");
        Fields.Add(field);
        _read.Add(() => set(field.Value));
    }
    /// <summary>Adds a staged integer control.</summary>
    /// <param name="label">Localized control label.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="set">Applies the staged value to the private media copy.</param>
    public void Number(string label, int value, Action<int> set)
    {
        var field = new SettingsField(label, value.ToString(CultureInfo.CurrentCulture), "number");
        Fields.Add(field);
        _read.Add(() =>
        {
            if (!int.TryParse(field.Value, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsed))
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.WholeNumberRequired, label));
            }
            set(parsed);
        });
    }
    /// <summary>Adds a staged unsigned integer control.</summary>
    /// <param name="label">Localized control label.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="set">Applies the staged value to the private media copy.</param>
    public void UnsignedNumber(string label, uint value, Action<uint> set)
    {
        var field = new SettingsField(label, value.ToString(CultureInfo.CurrentCulture), "number");
        Fields.Add(field);
        _read.Add(() =>
        {
            if (!uint.TryParse(field.Value, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsed))
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.WholeNumberRequired, label));
            }
            set(parsed);
        });
    }
    /// <summary>Adds a staged Boolean control.</summary>
    /// <param name="label">Localized control label.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="set">Applies the staged value to the private media copy.</param>
    public void Toggle(string label, bool value, Action<bool> set)
    {
        var field = new SettingsField(label, "", "toggle") { Checked = value };
        Fields.Add(field);
        _read.Add(() => set(field.Checked));
    }
    /// <summary>Adds a staged enumeration control.</summary>
    /// <param name="label">Localized control label.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="set">Applies the staged value to the private media copy.</param>
    /// <typeparam name="T">The enumeration represented by the control.</typeparam>
    public void Choice<T>(string label, T value, Action<T> set) where T : struct, Enum
    {
        var field = new SettingsField(label, value.ToString(), "choice") { Choices = Enum.GetNames<T>() };
        Fields.Add(field);
        _read.Add(() => set(Enum.Parse<T>(field.Value)));
    }
}
