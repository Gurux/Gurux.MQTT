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
namespace Gurux.MQTT.Blazor;

/// <summary>A staged control value that never writes browser storage.</summary>
public sealed class SettingsField
{
    /// <summary>Creates a control descriptor.</summary>
    /// <param name="label">Localized label.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="kind">HTML control category.</param>
    public SettingsField(string label, string value, string kind) { Label = label; Value = value; Kind = kind; }
    /// <summary>Gets the unique DOM identifier.</summary>
    public string Id { get; } = "field-" + Guid.NewGuid().ToString("N");
    /// <summary>Gets the localized label.</summary>
    public string Label { get; }
    /// <summary>Gets or sets the staged text value.</summary>
    public string Value { get; set; }
    /// <summary>Gets the control category.</summary>
    public string Kind { get; }
    /// <summary>Gets or sets the staged toggle value.</summary>
    public bool Checked { get; set; }
    /// <summary>Gets or sets whether this Boolean control uses switch semantics.</summary>
    public bool IsSwitch { get; set; }
    /// <summary>Gets or sets a predicate evaluated when rendering the control's disabled state.</summary>
    public Func<bool>? IsDisabled { get; set; }
    /// <summary>Gets whether the current staged settings disable this control.</summary>
    public bool Disabled => IsDisabled?.Invoke() ?? false;
    /// <summary>Gets the allowed protocol enumeration identifiers.</summary>
    public string[] Choices { get; init; } = [];
}
