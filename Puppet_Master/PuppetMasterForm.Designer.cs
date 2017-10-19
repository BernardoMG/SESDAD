using System.Threading.Tasks;


namespace Projecto_DAD
{
    partial class MasterForm 
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label logboxLabel;


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
            this.logboxLabel = new System.Windows.Forms.Label();
            this.portBox = new System.Windows.Forms.TextBox();
            this.messageBox = new System.Windows.Forms.TextBox();
            this.portLabel = new System.Windows.Forms.Label();
            this.commandBoxLabel = new System.Windows.Forms.Label();
            this.sendButton = new System.Windows.Forms.Button();
            this.slavesInfoLog = new System.Windows.Forms.RichTextBox();
            this.LogBox = new System.Windows.Forms.TextBox();
            this.ShowSlavesInfo = new System.Windows.Forms.Button();
            this.runScript = new System.Windows.Forms.Label();
            this.runScriptText = new System.Windows.Forms.TextBox();
            this.runScriptButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // logboxLabel
            // 
            this.logboxLabel.AutoSize = true;
            this.logboxLabel.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.logboxLabel.Location = new System.Drawing.Point(514, 22);
            this.logboxLabel.Name = "logboxLabel";
            this.logboxLabel.Size = new System.Drawing.Size(119, 13);
            this.logboxLabel.TabIndex = 1;
            this.logboxLabel.Text = "Action Log From Slaves";
            // 
            // portBox
            // 
            this.portBox.Location = new System.Drawing.Point(0, 0);
            this.portBox.Name = "portBox";
            this.portBox.Size = new System.Drawing.Size(100, 20);
            this.portBox.TabIndex = 0;
            // 
            // messageBox
            // 
            this.messageBox.Location = new System.Drawing.Point(31, 83);
            this.messageBox.Name = "messageBox";
            this.messageBox.Size = new System.Drawing.Size(222, 20);
            this.messageBox.TabIndex = 3;
            // 
            // portLabel
            // 
            this.portLabel.Location = new System.Drawing.Point(0, 0);
            this.portLabel.Name = "portLabel";
            this.portLabel.Size = new System.Drawing.Size(100, 23);
            this.portLabel.TabIndex = 0;
            // 
            // commandBoxLabel
            // 
            this.commandBoxLabel.AutoSize = true;
            this.commandBoxLabel.Location = new System.Drawing.Point(31, 65);
            this.commandBoxLabel.Name = "commandBoxLabel";
            this.commandBoxLabel.Size = new System.Drawing.Size(118, 13);
            this.commandBoxLabel.TabIndex = 5;
            this.commandBoxLabel.Text = "Insert Command/Script:";
            // 
            // sendButton
            // 
            this.sendButton.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.sendButton.FlatAppearance.BorderSize = 3;
            this.sendButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.sendButton.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.sendButton.Location = new System.Drawing.Point(259, 83);
            this.sendButton.Name = "sendButton";
            this.sendButton.Size = new System.Drawing.Size(75, 23);
            this.sendButton.TabIndex = 6;
            this.sendButton.Text = "Send";
            this.sendButton.UseVisualStyleBackColor = true;
            this.sendButton.Click += new System.EventHandler(this.sendCommandToSlave);
            // 
            // slavesInfoLog
            // 
            this.slavesInfoLog.Location = new System.Drawing.Point(31, 336);
            this.slavesInfoLog.Name = "slavesInfoLog";
            this.slavesInfoLog.Size = new System.Drawing.Size(275, 164);
            this.slavesInfoLog.TabIndex = 7;
            this.slavesInfoLog.Text = "";
            // 
            // LogBox
            // 
            this.LogBox.Location = new System.Drawing.Point(357, 47);
            this.LogBox.Multiline = true;
            this.LogBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.LogBox.Name = "LogBox";
            this.LogBox.Size = new System.Drawing.Size(380, 453);
            this.LogBox.TabIndex = 0;
            // 
            // ShowSlavesInfo
            // 
            this.ShowSlavesInfo.Location = new System.Drawing.Point(31, 307);
            this.ShowSlavesInfo.Name = "ShowSlavesInfo";
            this.ShowSlavesInfo.Size = new System.Drawing.Size(75, 23);
            this.ShowSlavesInfo.TabIndex = 8;
            this.ShowSlavesInfo.Text = "Show Slaves Info";
            this.ShowSlavesInfo.UseVisualStyleBackColor = true;
            this.ShowSlavesInfo.Click += new System.EventHandler(this.ShowSlavesInfo_Click);
            // 
            // runScript
            // 
            this.runScript.AutoSize = true;
            this.runScript.Location = new System.Drawing.Point(28, 125);
            this.runScript.Name = "runScript";
            this.runScript.Size = new System.Drawing.Size(66, 13);
            this.runScript.TabIndex = 9;
            this.runScript.Text = "Script name:";
            // 
            // runScriptText
            // 
            this.runScriptText.Location = new System.Drawing.Point(31, 141);
            this.runScriptText.Name = "runScriptText";
            this.runScriptText.Size = new System.Drawing.Size(222, 20);
            this.runScriptText.TabIndex = 10;
            // 
            // runScriptButton
            // 
            this.runScriptButton.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.runScriptButton.FlatAppearance.BorderSize = 3;
            this.runScriptButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.runScriptButton.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.runScriptButton.Location = new System.Drawing.Point(259, 138);
            this.runScriptButton.Name = "runScriptButton";
            this.runScriptButton.Size = new System.Drawing.Size(75, 23);
            this.runScriptButton.TabIndex = 6;
            this.runScriptButton.Text = "Run Script";
            this.runScriptButton.UseVisualStyleBackColor = true;
            this.runScriptButton.Click += new System.EventHandler(this.runScriptButton_Click);
            // 
            // MasterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(749, 512);
            this.Controls.Add(this.ShowSlavesInfo);
            this.Controls.Add(this.slavesInfoLog);
            this.Controls.Add(this.sendButton);
            this.Controls.Add(this.commandBoxLabel);
            this.Controls.Add(this.messageBox);
            this.Controls.Add(this.logboxLabel);
            this.Controls.Add(this.LogBox);
            this.Controls.Add(this.runScript);
            this.Controls.Add(this.runScriptText);
            this.Controls.Add(this.runScriptButton);
            this.Name = "MasterForm";
            this.Text = "MasterForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }


        #endregion

        private System.Windows.Forms.TextBox portBox;
        private System.Windows.Forms.TextBox messageBox;
        private System.Windows.Forms.Label portLabel;
        private System.Windows.Forms.Label commandBoxLabel;
        private System.Windows.Forms.Button sendButton;
        private System.Windows.Forms.RichTextBox slavesInfoLog;
        private System.Windows.Forms.TextBox LogBox;
        private System.Windows.Forms.Button ShowSlavesInfo;
        private System.Windows.Forms.TextBox runScriptText;
        private System.Windows.Forms.Label runScript;
        private System.Windows.Forms.Button runScriptButton;


    }
}