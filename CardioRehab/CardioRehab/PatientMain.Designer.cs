using System;
using System.Windows.Forms;
namespace CardioRehab
{
    partial class PatientMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
        /// 
        /// All icons were by Freepik at http://www.flaticon.com/authors/freepik
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PatientMain));
            this.panel1 = new System.Windows.Forms.Panel();
            this.doctorPanel = new System.Windows.Forms.Panel();
            this.patientPanel = new System.Windows.Forms.Panel();
            this.hrLabel = new System.Windows.Forms.Label();
            this.BiostatPanel = new System.Windows.Forms.Panel();
            this.oxiValue = new System.Windows.Forms.Label();
            this.oxiLabel = new System.Windows.Forms.Label();
            this.bpValue = new System.Windows.Forms.Label();
            this.bpLabel = new System.Windows.Forms.Label();
            this.hrValue = new System.Windows.Forms.Label();
            this.BiostatPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Location = new System.Drawing.Point(12, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(586, 587);
            this.panel1.TabIndex = 0;
            // 
            // doctorPanel
            // 
            this.doctorPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.doctorPanel.Location = new System.Drawing.Point(616, 12);
            this.doctorPanel.Name = "doctorPanel";
            this.doctorPanel.Size = new System.Drawing.Size(202, 151);
            this.doctorPanel.TabIndex = 1;
            // 
            // patientPanel
            // 
            this.patientPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.patientPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.patientPanel.Location = new System.Drawing.Point(616, 169);
            this.patientPanel.Name = "patientPanel";
            this.patientPanel.Size = new System.Drawing.Size(202, 151);
            this.patientPanel.TabIndex = 2;
            // 
            // hrLabel
            // 
            this.hrLabel.AutoSize = true;
            this.hrLabel.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.hrLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.hrLabel.Image = ((System.Drawing.Image)(resources.GetObject("hrLabel.Image")));
            this.hrLabel.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.hrLabel.Location = new System.Drawing.Point(3, 17);
            this.hrLabel.Name = "hrLabel";
            this.hrLabel.Size = new System.Drawing.Size(147, 26);
            this.hrLabel.TabIndex = 0;
            this.hrLabel.Text = "     Heart Rate";
            this.hrLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // BiostatPanel
            // 
            this.BiostatPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BiostatPanel.BackColor = System.Drawing.SystemColors.Menu;
            this.BiostatPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.BiostatPanel.Controls.Add(this.oxiValue);
            this.BiostatPanel.Controls.Add(this.oxiLabel);
            this.BiostatPanel.Controls.Add(this.bpValue);
            this.BiostatPanel.Controls.Add(this.bpLabel);
            this.BiostatPanel.Controls.Add(this.hrValue);
            this.BiostatPanel.Controls.Add(this.hrLabel);
            this.BiostatPanel.Location = new System.Drawing.Point(616, 336);
            this.BiostatPanel.Name = "BiostatPanel";
            this.BiostatPanel.Size = new System.Drawing.Size(202, 263);
            this.BiostatPanel.TabIndex = 4;
            // 
            // oxiValue
            // 
            this.oxiValue.AutoSize = true;
            this.oxiValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.oxiValue.Location = new System.Drawing.Point(72, 172);
            this.oxiValue.Name = "oxiValue";
            this.oxiValue.Size = new System.Drawing.Size(26, 26);
            this.oxiValue.TabIndex = 6;
            this.oxiValue.Text = "--";
            // 
            // oxiLabel
            // 
            this.oxiLabel.AutoSize = true;
            this.oxiLabel.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.oxiLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.oxiLabel.Image = ((System.Drawing.Image)(resources.GetObject("oxiLabel.Image")));
            this.oxiLabel.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.oxiLabel.Location = new System.Drawing.Point(3, 139);
            this.oxiLabel.Name = "oxiLabel";
            this.oxiLabel.Size = new System.Drawing.Size(156, 26);
            this.oxiLabel.TabIndex = 5;
            this.oxiLabel.Text = "     Oxygen Sat";
            this.oxiLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // bpValue
            // 
            this.bpValue.AutoSize = true;
            this.bpValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bpValue.Location = new System.Drawing.Point(72, 107);
            this.bpValue.Name = "bpValue";
            this.bpValue.Size = new System.Drawing.Size(26, 26);
            this.bpValue.TabIndex = 4;
            this.bpValue.Text = "--";
            // 
            // bpLabel
            // 
            this.bpLabel.AutoSize = true;
            this.bpLabel.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.bpLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bpLabel.Image = ((System.Drawing.Image)(resources.GetObject("bpLabel.Image")));
            this.bpLabel.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.bpLabel.Location = new System.Drawing.Point(3, 79);
            this.bpLabel.Name = "bpLabel";
            this.bpLabel.Size = new System.Drawing.Size(191, 26);
            this.bpLabel.TabIndex = 3;
            this.bpLabel.Text = "     Blood Pressure";
            this.bpLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // hrValue
            // 
            this.hrValue.AutoSize = true;
            this.hrValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.hrValue.Location = new System.Drawing.Point(72, 43);
            this.hrValue.Name = "hrValue";
            this.hrValue.Size = new System.Drawing.Size(26, 26);
            this.hrValue.TabIndex = 2;
            this.hrValue.Text = "--";
            // 
            // PatientMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(829, 611);
            this.Controls.Add(this.BiostatPanel);
            this.Controls.Add(this.patientPanel);
            this.Controls.Add(this.doctorPanel);
            this.Controls.Add(this.panel1);
            this.Name = "PatientMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Welcome to CardioRehab Application!";
            this.BiostatPanel.ResumeLayout(false);
            this.BiostatPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Panel panel1;
        private Panel doctorPanel;
        private Panel patientPanel;
        private Label hrLabel;
        private Panel BiostatPanel;
        private Label hrValue;
        private Label bpLabel;
        private Label bpValue;
        private Label oxiLabel;
        private Label oxiValue;
    }
}