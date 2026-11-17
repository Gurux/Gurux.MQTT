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

using System.ComponentModel;

namespace Gurux.MQTT.Properties
{
    /// <summary>Looks up property-grid descriptions in the existing localized resource set.</summary>
    internal sealed class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        /// <summary>Creates a description backed by a resource key.</summary>
        /// <param name="resourceKey">Key of the complete localized description.</param>
        public LocalizedDescriptionAttribute(string resourceKey) : base(resourceKey)
        {
        }

        /// <summary>Gets the description in the currently selected resource culture.</summary>
        public override string Description
        {
            get { return Resources.ResourceManager.GetString(base.Description, Resources.Culture) ?? base.Description; }
        }
    }
}
