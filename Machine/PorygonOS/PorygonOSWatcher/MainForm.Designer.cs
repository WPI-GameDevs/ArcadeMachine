namespace PorygonOSWatcher
{
    partial class MainForm
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.logOutBox = new System.Windows.Forms.RichTextBox();
            this.sendLogButton = new System.Windows.Forms.Button();
            this.logInText = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.launchButton = new System.Windows.Forms.Button();
            this.shutdownButton = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.logOutBox);
            this.groupBox1.Controls.Add(this.sendLogButton);
            this.groupBox1.Controls.Add(this.logInText);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(658, 282);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Log";
            // 
            // logOutBox
            // 
            this.logOutBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.logOutBox.Location = new System.Drawing.Point(6, 19);
            this.logOutBox.Name = "logOutBox";
            this.logOutBox.ReadOnly = true;
            this.logOutBox.Size = new System.Drawing.Size(646, 228);
            this.logOutBox.TabIndex = 2;
            this.logOutBox.Text = "";
            this.logOutBox.WordWrap = false;
            // 
            // sendLogButton
            // 
            this.sendLogButton.Location = new System.Drawing.Point(577, 253);
            this.sendLogButton.Name = "sendLogButton";
            this.sendLogButton.Size = new System.Drawing.Size(75, 23);
            this.sendLogButton.TabIndex = 1;
            this.sendLogButton.Text = "Post";
            this.sendLogButton.UseVisualStyleBackColor = true;
            this.sendLogButton.Click += new System.EventHandler(this.sendLogButton_Click);
            // 
            // logInText
            // 
            this.logInText.Location = new System.Drawing.Point(6, 255);
            this.logInText.Name = "logInText";
            this.logInText.Size = new System.Drawing.Size(565, 20);
            this.logInText.TabIndex = 0;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.launchButton);
            this.groupBox2.Controls.Add(this.shutdownButton);
            this.groupBox2.Location = new System.Drawing.Point(12, 300);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(658, 112);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Tools";
            // 
            // launchButton
            // 
            this.launchButton.Location = new System.Drawing.Point(6, 19);
            this.launchButton.Name = "launchButton";
            this.launchButton.Size = new System.Drawing.Size(75, 23);
            this.launchButton.TabIndex = 1;
            this.launchButton.Text = "Launch";
            this.launchButton.UseVisualStyleBackColor = true;
            this.launchButton.Click += new System.EventHandler(this.launchButton_Click);
            // 
            // shutdownButton
            // 
            this.shutdownButton.Location = new System.Drawing.Point(87, 19);
            this.shutdownButton.Name = "shutdownButton";
            this.shutdownButton.Size = new System.Drawing.Size(75, 23);
            this.shutdownButton.TabIndex = 0;
            this.shutdownButton.Text = "Shutdown";
            this.shutdownButton.UseVisualStyleBackColor = true;
            this.shutdownButton.Click += new System.EventHandler(this.shutdownButton_Click);
            // 
            // MainForm
            // 
            this.AcceptButton = this.sendLogButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(682, 424);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PorygonOS Control Panel";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RichTextBox logOutBox;
        private System.Windows.Forms.Button sendLogButton;
        private System.Windows.Forms.TextBox logInText;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button launchButton;
        private System.Windows.Forms.Button shutdownButton;
    }
}

