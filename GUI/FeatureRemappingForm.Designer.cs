﻿#region copyright
// Copyright 2013 Matthew S. Gerber (gerber.matthew@gmail.com)
// 
// This file is part of the Asymmetric Threat Tracker (ATT).
// 
// The ATT is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// The ATT is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with the ATT.  If not, see <http://www.gnu.org/licenses/>.
#endregion
 
namespace PTL.ATT.GUI
{
    partial class FeatureRemappingForm
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
        /// </summary>
        private void InitializeComponent()
        {
            this.current = new System.Windows.Forms.ListBox();
            this.available = new System.Windows.Forms.ListBox();
            this.ok = new System.Windows.Forms.Button();
            this.reset = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // current
            // 
            this.current.FormattingEnabled = true;
            this.current.HorizontalScrollbar = true;
            this.current.Location = new System.Drawing.Point(12, 12);
            this.current.Name = "current";
            this.current.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.current.Size = new System.Drawing.Size(266, 290);
            this.current.TabIndex = 0;
            // 
            // available
            // 
            this.available.FormattingEnabled = true;
            this.available.HorizontalScrollbar = true;
            this.available.Location = new System.Drawing.Point(284, 12);
            this.available.Name = "available";
            this.available.Size = new System.Drawing.Size(266, 290);
            this.available.TabIndex = 1;
            this.available.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.available_MouseDoubleClick);
            // 
            // ok
            // 
            this.ok.Location = new System.Drawing.Point(475, 308);
            this.ok.Name = "ok";
            this.ok.Size = new System.Drawing.Size(75, 23);
            this.ok.TabIndex = 2;
            this.ok.Text = "OK";
            this.ok.UseVisualStyleBackColor = true;
            this.ok.Click += new System.EventHandler(this.ok_Click);
            // 
            // reset
            // 
            this.reset.Location = new System.Drawing.Point(394, 308);
            this.reset.Name = "reset";
            this.reset.Size = new System.Drawing.Size(75, 23);
            this.reset.TabIndex = 3;
            this.reset.Text = "Reset";
            this.reset.UseVisualStyleBackColor = true;
            this.reset.Click += new System.EventHandler(this.reset_Click);
            // 
            // FeatureRemappingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(560, 341);
            this.Controls.Add(this.reset);
            this.Controls.Add(this.ok);
            this.Controls.Add(this.available);
            this.Controls.Add(this.current);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "FeatureRemappingForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Remap features during prediction...";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox current;
        private System.Windows.Forms.ListBox available;
        private System.Windows.Forms.Button ok;
        private System.Windows.Forms.Button reset;
    }
}