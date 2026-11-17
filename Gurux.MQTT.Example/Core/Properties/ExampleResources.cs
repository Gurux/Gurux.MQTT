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
    public static string Clear => Manager.GetString(nameof(Clear), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Clear);
    /// <summary>Gets the heading for received payloads, separate from diagnostic activity.</summary>
    public static string ReceivedData => Manager.GetString(nameof(ReceivedData), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReceivedData);
    /// <summary>Reads the text embedded in the example core assembly.</summary>
    private static readonly ResourceManager Manager = new("Gurux.MQTT.Example.Properties.ExampleResources", typeof(ExampleResources).Assembly);
    /// <summary>Gets the localized application title.</summary>
    public static string Title => Manager.GetString(nameof(Title), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Title);
    /// <summary>Gets the settings command caption.</summary>
    public static string Settings => Manager.GetString(nameof(Settings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Settings);
    /// <summary>Gets the open connection command caption.</summary>
    public static string Open => Manager.GetString(nameof(Open), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Open);
    /// <summary>Gets the closed connection state caption.</summary>
    public static string Closed => Manager.GetString(nameof(Closed), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Closed);
    /// <summary>Gets the close and cancel command caption.</summary>
    public static string Close => Manager.GetString(nameof(Close), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Close);
    /// <summary>Gets the send command caption.</summary>
    public static string Send => Manager.GetString(nameof(Send), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Send);
    /// <summary>Gets the canceled operation message.</summary>
    public static string Canceled => Manager.GetString(nameof(Canceled), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Canceled);
    /// <summary>Gets the outgoing payload input hint.</summary>
    public static string SendPlaceholder => Manager.GetString(nameof(SendPlaceholder), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(SendPlaceholder);
    /// <summary>Gets the reply terminator input hint.</summary>
    public static string EopPlaceholder => Manager.GetString(nameof(EopPlaceholder), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(EopPlaceholder);
    /// <summary>Gets the hexadecimal encoding option caption.</summary>
    public static string Hexadecimal => Manager.GetString(nameof(Hexadecimal), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Hexadecimal);
    /// <summary>Gets the synchronous reply option caption.</summary>
    public static string ReceiveReply => Manager.GetString(nameof(ReceiveReply), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReceiveReply);
    /// <summary>Gets the reply framing and timeout input hint.</summary>
    public static string ReplyOptions => Manager.GetString(nameof(ReplyOptions), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReplyOptions);
    /// <summary>Gets the successful connection log message.</summary>
    public static string Opened => Manager.GetString(nameof(Opened), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Opened);
    /// <summary>Gets the connection shutdown log message.</summary>
    public static string ClosedLog => Manager.GetString(nameof(ClosedLog), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ClosedLog);
    /// <summary>Gets the incomplete reply timeout message.</summary>
    public static string Timeout => Manager.GetString(nameof(Timeout), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Timeout);
    /// <summary>Gets the incoming sender, hexadecimal bytes and ASCII text log format.</summary>
    public static string RxBytes => Manager.GetString(nameof(RxBytes), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(RxBytes);
    /// <summary>Gets the incoming sender and payload log format.</summary>
    public static string RxData => Manager.GetString(nameof(RxData), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(RxData);
    /// <summary>Gets the transport error log format.</summary>
    public static string Error => Manager.GetString(nameof(Error), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Error);
    /// <summary>Gets the outgoing hexadecimal payload log format.</summary>
    public static string Tx => Manager.GetString(nameof(Tx), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Tx);
    /// <summary>Gets the hexadecimal reply log format.</summary>
    public static string Reply => Manager.GetString(nameof(Reply), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Reply);
    /// <summary>Gets the outgoing hexadecimal bytes and ASCII text log format.</summary>
    public static string TxBytes => Manager.GetString(nameof(TxBytes), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(TxBytes);
    /// <summary>Gets the reply hexadecimal bytes and ASCII text log format.</summary>
    public static string ReplyBytes => Manager.GetString(nameof(ReplyBytes), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReplyBytes);
}
