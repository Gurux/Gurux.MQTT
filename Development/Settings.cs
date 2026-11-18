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
using System.ComponentModel;
using System.Reflection;
using Gurux.Common;
using System.Windows.Forms;
using Gurux.MQTT.Properties;

namespace Gurux.MQTT
{
    /// <summary>Edits MQTT broker, topic and client settings in a Windows Forms property page.</summary>
    partial class Settings : Form, IGXPropertyPage, INotifyPropertyChanged
    {
        /// <summary>MQTT media instance whose settings are edited by this page.</summary>
        GXMqtt _target;
        /// <summary>Subscribers notified when an editor field changes.</summary>
        PropertyChangedEventHandler _propertyChanged;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                _propertyChanged += value;
            }
            remove
            {
                _propertyChanged -= value;
            }
        }

        /// <summary>Creates an editor bound to the supplied MQTT media.</summary>
        /// <param name="target">MQTT media instance whose settings are read or edited.</param>
        public Settings(GXMqtt target)
        {
            this._target = target;
            InitializeComponent();
            Text = Resources.SettingsTxt;
            ClientIdLbl.Text = Resources.ResourceManager.GetString("ClientIdLabel", Resources.Culture);
        }

#region Assembly Attribute Accessors

        /// <summary>Gets the executing assembly title or its filename when no title is specified.</summary>
        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        /// <summary>Gets the executing assembly version as text.</summary>
        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        /// <summary>Gets the executing assembly description, or an empty string when unspecified.</summary>
        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        /// <summary>Gets the executing assembly product name, or an empty string when unspecified.</summary>
        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        /// <summary>Gets the executing assembly copyright notice, or an empty string when unspecified.</summary>
        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        /// <summary>Gets the executing assembly company name, or an empty string when unspecified.</summary>
        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }

        /// <summary>Gets or sets whether the staged settings contain unapplied changes.</summary>
        public bool Dirty
        {
            get;
            set;
        }
#endregion


#region IGXPropertyPage Members

        /// <summary>Loads localized labels and current media settings into the editor controls.</summary>
        void IGXPropertyPage.Initialize()
        {
            this.TopicLbl.Text = Resources.TopicTxt;
            this.IPAddressLbl.Text = Resources.BrokerTxt;
            this.PortLbl.Text = Resources.PortTxt;
            this.TopicTb.Text = _target.Topic;
            this.PortTB.Text = _target.Port.ToString();
            this.IPAddressTB.Text = _target.ServerAddress;
            this.ClientIdTb.Text = _target.ClientId;
            //Hide controls which user do not want to show.
            HostPanel.Enabled = (_target.ConfigurableSettings & AvailableMediaSettings.Host) != 0;
            PortPanel.Enabled = (_target.ConfigurableSettings & AvailableMediaSettings.Port) != 0;
            TopicPanel.Enabled = (_target.ConfigurableSettings & AvailableMediaSettings.Topic) != 0;
            ClientIdPanel.Enabled = false;
            Dirty = false;
        }

        /// <summary>Applies the staged broker, port, topic and client identifier to the media.</summary>
        void IGXPropertyPage.Apply()
        {
            _target.Port = Convert.ToInt32(this.PortTB.Text);
            _target.ServerAddress = this.IPAddressTB.Text;
            _target.Topic = this.TopicTb.Text;
            _target.ClientId  = this.ClientIdTb.Text;
            Dirty = false;
        }

#endregion

        /// <summary>Marks the page dirty and notifies subscribers that the broker address changed.</summary>
        /// <param name="sender">Object or transport identification that raised the notification.</param>
        /// <param name="e">Event data supplied by the editor control.</param>
        private void IPAddressTB_TextChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (_propertyChanged != null)
            {
                _propertyChanged(this, new PropertyChangedEventArgs("IPAddress"));
            }
        }

        /// <summary>Marks the page dirty and notifies subscribers that the broker port changed.</summary>
        /// <param name="sender">Object or transport identification that raised the notification.</param>
        /// <param name="e">Event data supplied by the editor control.</param>
        private void PortTB_TextChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (_propertyChanged != null)
            {
                _propertyChanged(this, new PropertyChangedEventArgs("Port"));
            }
        }

        /// <summary>Marks the page dirty and notifies subscribers that the topic changed.</summary>
        /// <param name="sender">Object or transport identification that raised the notification.</param>
        /// <param name="e">Event data supplied by the editor control.</param>
        private void TopicTb_TextChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (_propertyChanged != null)
            {
                _propertyChanged(this, new PropertyChangedEventArgs("Topic"));
            }
        }

        /// <summary>Marks the page dirty and notifies subscribers that the client identifier changed.</summary>
        /// <param name="sender">Object or transport identification that raised the notification.</param>
        /// <param name="e">Event data supplied by the editor control.</param>
        private void ClientIdTb_TextChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (_propertyChanged != null)
            {
                _propertyChanged(this, new PropertyChangedEventArgs("ClientId"));
            }
        }

    }
}
