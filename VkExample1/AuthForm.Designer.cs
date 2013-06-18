namespace VkApiNet
{
    partial class AuthForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AuthForm));
            this.browserPlugin = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // browserPlugin
            // 
            this.browserPlugin.Dock = System.Windows.Forms.DockStyle.Fill;
            this.browserPlugin.Location = new System.Drawing.Point(0, 0);
            this.browserPlugin.MinimumSize = new System.Drawing.Size(20, 20);
            this.browserPlugin.Name = "browserPlugin";
            this.browserPlugin.ScriptErrorsSuppressed = true;
            this.browserPlugin.ScrollBarsEnabled = false;
            this.browserPlugin.Size = new System.Drawing.Size(532, 506);
            this.browserPlugin.TabIndex = 0;
            // 
            // AuthForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(532, 506);
            this.Controls.Add(this.browserPlugin);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AuthForm";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Авторизация";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.WebBrowser browserPlugin;
    }
}