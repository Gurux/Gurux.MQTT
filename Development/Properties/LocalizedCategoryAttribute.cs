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
    /// <summary>Looks up property-grid category names in the existing localized resource set.</summary>
    internal sealed class LocalizedCategoryAttribute : CategoryAttribute
    {
        /// <summary>Creates a category backed by a resource key.</summary>
        /// <param name="resourceKey">Key of the localized category name.</param>
        public LocalizedCategoryAttribute(string resourceKey) : base(resourceKey)
        {
        }

        /// <summary>Looks up a category name using the selected resource culture.</summary>
        /// <param name="value">Resource key supplied by the category attribute.</param>
        /// <returns>The localized category name, or its key when no resource exists.</returns>
        protected override string GetLocalizedString(string value)
        {
            return Resources.ResourceManager.GetString(value, Resources.Culture) ?? value;
        }
    }
}
