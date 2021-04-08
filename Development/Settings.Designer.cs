
namespace Gurux.MQTT
{
    partial class Settings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.TopicPanel = new System.Windows.Forms.Panel();
            this.TopicTb = new System.Windows.Forms.TextBox();
            this.TopicLbl = new System.Windows.Forms.Label();
            this.PortPanel = new System.Windows.Forms.Panel();
            this.PortTB = new System.Windows.Forms.TextBox();
            this.PortLbl = new System.Windows.Forms.Label();
            this.HostPanel = new System.Windows.Forms.Panel();
            this.IPAddressTB = new System.Windows.Forms.TextBox();
            this.IPAddressLbl = new System.Windows.Forms.Label();

            this.ClientIdPanel = new System.Windows.Forms.Panel();
            this.ClientIdTb = new System.Windows.Forms.TextBox();
            this.ClientIdLbl = new System.Windows.Forms.Label();

            this.TopicPanel.SuspendLayout();
            this.PortPanel.SuspendLayout();
            this.HostPanel.SuspendLayout();
            this.ClientIdPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // TopicPanel
            //
            this.TopicPanel.Controls.Add(this.TopicTb);
            this.TopicPanel.Controls.Add(this.TopicLbl);
            this.TopicPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.TopicPanel.Location = new System.Drawing.Point(0, 49);
            this.TopicPanel.Name = "TopicPanel";
            this.TopicPanel.Size = new System.Drawing.Size(235, 25);
            this.TopicPanel.TabIndex = 23;
            //
            // TopicTb
            //
            this.TopicTb.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TopicTb.Location = new System.Drawing.Point(78, 3);
            this.TopicTb.Name = "TopicTb";
            this.TopicTb.Size = new System.Drawing.Size(136, 20);
            this.TopicTb.TabIndex = 16;
            this.TopicTb.TextChanged += new System.EventHandler(this.TopicTb_TextChanged);
            //
            // TopicLbl
            //
            this.TopicLbl.AutoSize = true;
            this.TopicLbl.Location = new System.Drawing.Point(4, 5);
            this.TopicLbl.Name = "TopicLbl";
            this.TopicLbl.Size = new System.Drawing.Size(41, 13);
            this.TopicLbl.TabIndex = 15;
            this.TopicLbl.Text = "TopicX";
            //
            // PortPanel
            //
            this.PortPanel.Controls.Add(this.PortTB);
            this.PortPanel.Controls.Add(this.PortLbl);
            this.PortPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.PortPanel.Location = new System.Drawing.Point(0, 25);
            this.PortPanel.Name = "PortPanel";
            this.PortPanel.Size = new System.Drawing.Size(235, 24);
            this.PortPanel.TabIndex = 22;
            //
            // PortTB
            //
            this.PortTB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PortTB.Location = new System.Drawing.Point(78, 3);
            this.PortTB.Name = "PortTB";
            this.PortTB.Size = new System.Drawing.Size(136, 20);
            this.PortTB.TabIndex = 11;
            this.PortTB.TextChanged += new System.EventHandler(this.PortTB_TextChanged);
            //
            // PortLbl
            //
            this.PortLbl.AutoSize = true;
            this.PortLbl.Location = new System.Drawing.Point(4, 5);
            this.PortLbl.Name = "PortLbl";
            this.PortLbl.Size = new System.Drawing.Size(33, 13);
            this.PortLbl.TabIndex = 12;
            this.PortLbl.Text = "PortX";
            //
            // HostPanel
            //
            this.HostPanel.Controls.Add(this.IPAddressTB);
            this.HostPanel.Controls.Add(this.IPAddressLbl);
            this.HostPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.HostPanel.Location = new System.Drawing.Point(0, 0);
            this.HostPanel.Name = "HostPanel";
            this.HostPanel.Size = new System.Drawing.Size(235, 25);
            this.HostPanel.TabIndex = 21;
            //
            // IPAddressTB
            //
            this.IPAddressTB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.IPAddressTB.Location = new System.Drawing.Point(78, 2);
            this.IPAddressTB.Name = "IPAddressTB";
            this.IPAddressTB.Size = new System.Drawing.Size(136, 20);
            this.IPAddressTB.TabIndex = 9;
            this.IPAddressTB.TextChanged += new System.EventHandler(this.IPAddressTB_TextChanged);
            //
            // IPAddressLbl
            //
            this.IPAddressLbl.AutoSize = true;
            this.IPAddressLbl.Location = new System.Drawing.Point(4, 5);
            this.IPAddressLbl.Name = "IPAddressLbl";
            this.IPAddressLbl.Size = new System.Drawing.Size(45, 13);
            this.IPAddressLbl.TabIndex = 10;
            this.IPAddressLbl.Text = "BrokerX";
            //
            // ClientIdPanel
            //
            this.ClientIdPanel.Controls.Add(this.ClientIdTb);
            this.ClientIdPanel.Controls.Add(this.ClientIdLbl);
            this.ClientIdPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.ClientIdPanel.Location = new System.Drawing.Point(0, 49);
            this.ClientIdPanel.Name = "ClientIdPanel";
            this.ClientIdPanel.Size = new System.Drawing.Size(235, 25);
            this.ClientIdPanel.TabIndex = 23;
            //
            // ClientIdTb
            //
            this.ClientIdTb.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ClientIdTb.Location = new System.Drawing.Point(78, 3);
            this.ClientIdTb.Name = "ClientIdTb";
            this.ClientIdTb.Size = new System.Drawing.Size(136, 20);
            this.ClientIdTb.TabIndex = 16;
            this.ClientIdTb.TextChanged += new System.EventHandler(this.ClientIdTb_TextChanged);
            //
            // ClientIdLbl
            //
            this.ClientIdLbl.AutoSize = true;
            this.ClientIdLbl.Location = new System.Drawing.Point(4, 5);
            this.ClientIdLbl.Name = "ClientIdLbl";
            this.ClientIdLbl.Size = new System.Drawing.Size(41, 13);
            this.ClientIdLbl.TabIndex = 15;
            this.ClientIdLbl.Text = "ClientIdX";

            //
            // Settings
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(235, 152);
            this.Controls.Add(this.ClientIdPanel);
            this.Controls.Add(this.TopicPanel);
            this.Controls.Add(this.PortPanel);
            this.Controls.Add(this.HostPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Settings";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Network SettingsX";
            this.TopicPanel.ResumeLayout(false);
            this.TopicPanel.PerformLayout();
            this.PortPanel.ResumeLayout(false);
            this.PortPanel.PerformLayout();
            this.HostPanel.ResumeLayout(false);
            this.HostPanel.PerformLayout();
            this.ClientIdPanel.ResumeLayout(false);
            this.ClientIdPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel TopicPanel;
        private System.Windows.Forms.Label TopicLbl;
        private System.Windows.Forms.Panel PortPanel;
        private System.Windows.Forms.TextBox PortTB;
        private System.Windows.Forms.Label PortLbl;
        private System.Windows.Forms.Panel HostPanel;
        private System.Windows.Forms.TextBox IPAddressTB;
        private System.Windows.Forms.Label IPAddressLbl;
        private System.Windows.Forms.TextBox TopicTb;

        private System.Windows.Forms.Panel ClientIdPanel;
        private System.Windows.Forms.Label ClientIdLbl;
        private System.Windows.Forms.TextBox ClientIdTb;
    }
}
