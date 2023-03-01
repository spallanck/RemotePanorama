namespace RemotePanorama
{
    partial class Client
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.serverName = new System.Windows.Forms.TextBox();
            this.username = new System.Windows.Forms.TextBox();
            this.password = new System.Windows.Forms.TextBox();
            this.viewFolders = new System.Windows.Forms.Button();
            this.urlLabel = new System.Windows.Forms.Label();
            this.folderLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(56, 48);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "Username:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(56, 82);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "Password:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(56, 14);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(42, 15);
            this.label3.TabIndex = 3;
            this.label3.Text = "Server:";
            // 
            // serverName
            // 
            this.serverName.Location = new System.Drawing.Point(125, 12);
            this.serverName.Name = "serverName";
            this.serverName.PlaceholderText = "https://panoramaweb.org";
            this.serverName.Size = new System.Drawing.Size(152, 23);
            this.serverName.TabIndex = 4;
            // 
            // username
            // 
            this.username.Location = new System.Drawing.Point(125, 45);
            this.username.Name = "username";
            this.username.Size = new System.Drawing.Size(152, 23);
            this.username.TabIndex = 5;
            // 
            // password
            // 
            this.password.Location = new System.Drawing.Point(125, 79);
            this.password.Name = "password";
            this.password.Size = new System.Drawing.Size(152, 23);
            this.password.TabIndex = 6;
            this.password.UseSystemPasswordChar = true;
            // 
            // viewFolders
            // 
            this.viewFolders.Location = new System.Drawing.Point(125, 122);
            this.viewFolders.Name = "viewFolders";
            this.viewFolders.Size = new System.Drawing.Size(152, 23);
            this.viewFolders.TabIndex = 7;
            this.viewFolders.Text = "View Folders";
            this.viewFolders.UseVisualStyleBackColor = true;
            this.viewFolders.Click += new System.EventHandler(this.viewFolders_Click);
            // 
            // urlLabel
            // 
            this.urlLabel.AutoSize = true;
            this.urlLabel.Location = new System.Drawing.Point(12, 162);
            this.urlLabel.Name = "urlLabel";
            this.urlLabel.Size = new System.Drawing.Size(0, 15);
            this.urlLabel.TabIndex = 8;
            // 
            // folderLabel
            // 
            this.folderLabel.AutoSize = true;
            this.folderLabel.Location = new System.Drawing.Point(12, 187);
            this.folderLabel.Name = "folderLabel";
            this.folderLabel.Size = new System.Drawing.Size(0, 15);
            this.folderLabel.TabIndex = 9;
            // 
            // Client
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(341, 226);
            this.Controls.Add(this.folderLabel);
            this.Controls.Add(this.urlLabel);
            this.Controls.Add(this.viewFolders);
            this.Controls.Add(this.password);
            this.Controls.Add(this.username);
            this.Controls.Add(this.serverName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "Client";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Client";
            this.Load += new System.EventHandler(this.Client_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Label label1;
        private Label label2;
        private Label label3;
        private TextBox serverName;
        private TextBox username;
        private TextBox password;
        private Button viewFolders;
        private Label urlLabel;
        private Label folderLabel;
    }
}