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

using System.Resources;
namespace Gurux.MQTT.Example.Properties;
/// <summary>Provides localized text for the MQTT example and its native hosts.</summary>
public static class ExampleResources
{
    /// <summary>Gets the caption for clearing displayed history.</summary>
    public static string Clear => _manager.GetString(nameof(Clear), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Clear);
    /// <summary>Gets the heading for received payloads, separate from diagnostic activity.</summary>
    public static string ReceivedData => _manager.GetString(nameof(ReceivedData), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReceivedData);
    /// <summary>Reads the text embedded in the example core assembly.</summary>
    private static readonly ResourceManager _manager = new("Gurux.MQTT.Example.Properties.ExampleResources", typeof(ExampleResources).Assembly);
    /// <summary>Gets the localized application title.</summary>
    public static string Title => _manager.GetString(nameof(Title), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Title);
    /// <summary>Gets the settings command caption.</summary>
    public static string Settings => _manager.GetString(nameof(Settings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Settings);
    /// <summary>Gets the open connection command caption.</summary>
    public static string Open => _manager.GetString(nameof(Open), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Open);
    /// <summary>Gets the closed connection state caption.</summary>
    public static string Closed => _manager.GetString(nameof(Closed), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Closed);
    /// <summary>Gets the close and cancel command caption.</summary>
    public static string Close => _manager.GetString(nameof(Close), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Close);
    /// <summary>Gets the send command caption.</summary>
    public static string Send => _manager.GetString(nameof(Send), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Send);
    /// <summary>Gets the canceled operation message.</summary>
    public static string Canceled => _manager.GetString(nameof(Canceled), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Canceled);
    /// <summary>Gets the outgoing payload input hint.</summary>
    public static string SendPlaceholder => _manager.GetString(nameof(SendPlaceholder), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(SendPlaceholder);
    /// <summary>Gets the reply terminator input hint.</summary>
    public static string EopPlaceholder => _manager.GetString(nameof(EopPlaceholder), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(EopPlaceholder);
    /// <summary>Gets the hexadecimal encoding option caption.</summary>
    public static string Hexadecimal => _manager.GetString(nameof(Hexadecimal), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Hexadecimal);
    /// <summary>Gets the synchronous reply option caption.</summary>
    public static string ReceiveReply => _manager.GetString(nameof(ReceiveReply), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReceiveReply);
    /// <summary>Gets the reply framing and timeout input hint.</summary>
    public static string ReplyOptions => _manager.GetString(nameof(ReplyOptions), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReplyOptions);
    /// <summary>Gets the successful connection log message.</summary>
    public static string Opened => _manager.GetString(nameof(Opened), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Opened);
    /// <summary>Gets the connection shutdown log message.</summary>
    public static string ClosedLog => _manager.GetString(nameof(ClosedLog), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ClosedLog);
    /// <summary>Gets the incomplete reply timeout message.</summary>
    public static string Timeout => _manager.GetString(nameof(Timeout), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Timeout);
    /// <summary>Gets the incoming sender, hexadecimal bytes and ASCII text log format.</summary>
    public static string RxBytes => _manager.GetString(nameof(RxBytes), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(RxBytes);
    /// <summary>Gets the incoming sender and payload log format.</summary>
    public static string RxData => _manager.GetString(nameof(RxData), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(RxData);
    /// <summary>Gets the transport error log format.</summary>
    public static string Error => _manager.GetString(nameof(Error), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Error);
    /// <summary>Gets the outgoing hexadecimal payload log format.</summary>
    public static string Tx => _manager.GetString(nameof(Tx), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Tx);
    /// <summary>Gets the hexadecimal reply log format.</summary>
    public static string Reply => _manager.GetString(nameof(Reply), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Reply);
    /// <summary>Gets the outgoing hexadecimal bytes and ASCII text log format.</summary>
    public static string TxBytes => _manager.GetString(nameof(TxBytes), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(TxBytes);
    /// <summary>Gets the reply hexadecimal bytes and ASCII text log format.</summary>
    public static string ReplyBytes => _manager.GetString(nameof(ReplyBytes), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReplyBytes);
}
