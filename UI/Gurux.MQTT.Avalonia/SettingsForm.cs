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
using Avalonia.Controls;
using Gurux.MQTT.Avalonia.Properties;
namespace Gurux.MQTT.Avalonia;
/// <summary>Builds reusable typed settings controls without touching live transport state.</summary>
internal sealed class SettingsForm
{
    /// <summary>Initializes an empty staged settings form.</summary>
    public SettingsForm() { }
    /// <summary>Stores transfers from controls into a private draft.</summary>
    private readonly List<Action> _read = [];
    /// <summary>Gets labeled controls in display order.</summary>
    public List<(string Label, Control View)> Fields { get; } = [];
    /// <summary>Reads all current control values into their draft properties.</summary>
    public void Read() { foreach (var read in _read) { read(); } }
    /// <summary>Adds an editable string field.</summary>
    /// <param name="label">The translated field caption.</param>
    /// <param name="value">The initial draft value.</param>
    /// <param name="set">Writes the edited string to the draft.</param>
    /// <param name="password">Whether to mask the displayed value.</param>
    /// <param name="multiline">Whether to accept multiple lines.</param>
    public void Text(string label, string? value, Action<string> set, bool password = false, bool multiline = false)
    {
        var control = new TextBox { Text = value, AcceptsReturn = multiline, MinHeight = multiline ? 80 : 0, PasswordChar = password ? '\u25cf' : '\0' };
        Fields.Add((label, control));
        _read.Add(() => set(control.Text ?? ""));
    }
    /// <summary>Adds a whole number field using the current culture.</summary>
    /// <param name="label">The translated field caption.</param>
    /// <param name="value">The initial draft value.</param>
    /// <param name="set">Writes the parsed number to the draft.</param>
    public void Number(string label, int value, Action<int> set)
    {
        var control = new TextBox { Text = value.ToString(CultureInfo.CurrentCulture) };
        Fields.Add((label, control));
        _read.Add(() =>
        {
            if (!int.TryParse(control.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsed))
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, Resources.Get("NumberError"), label));
            }
            set(parsed);
        });
    }
    /// <summary>Adds a unsigned whole number field using the current culture.</summary>
    /// <param name="label">The translated field caption.</param>
    /// <param name="value">The initial draft value.</param>
    /// <param name="set">Writes the parsed number to the draft.</param>
    public void UnsignedNumber(string label, uint value, Action<uint> set)
    {
        var control = new TextBox { Text = value.ToString(CultureInfo.CurrentCulture) };
        Fields.Add((label, control));
        _read.Add(() =>
        {
            if (!uint.TryParse(control.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsed))
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, Resources.Get("NumberError"), label));
            }
            set(parsed);
        });
    }
    /// <summary>Adds a Boolean field.</summary>
    /// <param name="label">The translated field caption.</param>
    /// <param name="value">The initial draft value.</param>
    /// <param name="set">Writes the checked state to the draft.</param>
    public void Toggle(string label, bool value, Action<bool> set)
    {
        var control = new CheckBox { IsChecked = value };
        Fields.Add((label, control));
        _read.Add(() => set(control.IsChecked == true));
    }
    /// <summary>Adds a Boolean switch that immediately updates the private draft and dependent controls.</summary>
    /// <param name="label">The translated field caption.</param>
    /// <param name="value">The initial draft state.</param>
    /// <param name="set">Updates the private Boolean value and any dependent controls.</param>
    /// <returns>The switch whose state is also read when the form is committed.</returns>
    public ToggleSwitch Switch(string label, bool value, Action<bool> set)
    {
        var control = new ToggleSwitch { IsChecked = value, OnContent = null, OffContent = null };
        control.IsCheckedChanged += (_, _) => set(control.IsChecked == true);
        Fields.Add((label, control));
        _read.Add(() => set(control.IsChecked == true));
        return control;
    }
    /// <summary>Adds a choice constrained to declared enumeration values.</summary>
    /// <typeparam name="T">The enumeration of allowed values.</typeparam>
    /// <param name="label">The translated field caption.</param>
    /// <param name="value">The initial draft selection.</param>
    /// <param name="set">Writes the selected enumeration value to the draft.</param>
    public void Choice<T>(string label, T value, Action<T> set) where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var control = new ComboBox { ItemsSource = values, SelectedIndex = Array.IndexOf(values, value) };
        Fields.Add((label, control));
        _read.Add(() =>
        {
            if (control.SelectedIndex < 0 || control.SelectedIndex >= values.Length)
            {
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, Resources.Get("ChoiceError"), label));
            }
            set(values[control.SelectedIndex]);
        });
    }
}
