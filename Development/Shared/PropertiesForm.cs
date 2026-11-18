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

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Gurux.Common;

namespace Gurux.Shared
{
    /// <summary>Hosts a media property page with localized acceptance and cancellation buttons.</summary>
    partial class PropertiesForm : Form
    {
        /// <summary>Property page whose staged changes are applied by the acceptance button.</summary>
        IGXPropertyPage _properties;
        /// <summary>Online help address opened when the user requests help.</summary>
        string _helpLink;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="properties">Properties form.</param>
        /// <param name="title">Window title.</param>
        /// <param name="open">Is media opened.</param>
        /// <param name="okBtnText">Localized OK button text.</param>
        /// <param name="cancelBtnText">Localized cancel button text.</param>
        /// <param name="helpLink">Help link address.</param>
        public PropertiesForm(Form properties, 
            string title, 
            bool open, 
            string okBtnText, 
            string cancelBtnText, 
            string helpLink)
        {
            _helpLink = helpLink;
            _properties = (IGXPropertyPage)properties;
            InitializeComponent();
            _properties.Initialize();
            if (string.IsNullOrEmpty(helpLink))
            {
                HelpButton = false;
            }
            this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(GetType().Assembly.Location);
            this.OKBtn.Enabled = !open;
            this.Text = title;
            this.OKBtn.Text = okBtnText;
            this.CancelBtn.Text = cancelBtnText;
            //Settings can be change if connection is open.
            int h = 0;
            while (properties.Controls.Count != 0)
            {
                Control ctr = properties.Controls[0];
                if (ctr is Panel)
                {
                    if (ctr.Enabled)
                    {
                        h += ctr.Height;
                    }
                    else
                    {
                        properties.Controls.RemoveAt(0);
                        continue;
                    }
                }
                ctr.Enabled = !open;
                FormPanel.Controls.Add(ctr);
            }
            this.LocationChanged += new EventHandler(PropertiesForm_LocationChanged);
            properties.TopLevel = false;
            properties.TopMost = false;
            properties.FormBorderStyle = FormBorderStyle.None;
            this.Controls.Add(properties);
            if (h == 0)
            {
                h = properties.Height;
            }
            h += panel1.Height;
            //Set client size.
            this.ClientSize = new Size(properties.Width, h);
        }

        /// <summary>Keeps the hosted property form aligned with its container window.</summary>
        /// <param name="sender">Object or transport identification that raised the notification.</param>
        /// <param name="e">Event data supplied by the editor control.</param>
        void PropertiesForm_LocationChanged(object sender, EventArgs e)
        {
            (_properties as Form).Location = this.Location;
        }

        /// <summary>Applies the staged settings and displays any validation error.</summary>
        /// <param name="sender">Object or transport identification that raised the notification.</param>
        /// <param name="e">Event data supplied by the editor control.</param>
        private void OKBtn_Click(object sender, EventArgs e)
        {
            try
            {
                _properties.Apply();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Show help not available message.
        /// </summary>
        /// <param name="hevent">A HelpEventArgs that contains the event data.</param>
        protected override void OnHelpRequested(HelpEventArgs hevent)
        {
            try
            {
                // Show online help.
                Process.Start(_helpLink);
                // Set flag to show that the Help event as been handled
                hevent.Handled = true;
            }
            catch (Exception Ex)
            {
                MessageBox.Show(this, Ex.Message, Text, MessageBoxButtons.OK);
            }
        }
    }
}
