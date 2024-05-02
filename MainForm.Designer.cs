namespace TigerBotV2
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
            this.aimStatusLabel = new System.Windows.Forms.Label();
            this.triggerStatusLabel = new System.Windows.Forms.Label();
            this.holderPanel = new System.Windows.Forms.Panel();
            this.holderPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // aimStatusLabel
            // 
            this.aimStatusLabel.AutoSize = true;
            this.aimStatusLabel.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.aimStatusLabel.ForeColor = System.Drawing.Color.Black;
            this.aimStatusLabel.Location = new System.Drawing.Point(10, 10);
            this.aimStatusLabel.Name = "aimStatusLabel";
            this.aimStatusLabel.Size = new System.Drawing.Size(125, 16);
            this.aimStatusLabel.TabIndex = 0;
            this.aimStatusLabel.Text = "Aim Assist : Inactive";
            // 
            // triggerStatusLabel
            // 
            this.triggerStatusLabel.AutoSize = true;
            this.triggerStatusLabel.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.triggerStatusLabel.ForeColor = System.Drawing.Color.Black;
            this.triggerStatusLabel.Location = new System.Drawing.Point(10, 30);
            this.triggerStatusLabel.Name = "triggerStatusLabel";
            this.triggerStatusLabel.Size = new System.Drawing.Size(125, 16);
            this.triggerStatusLabel.TabIndex = 1;
            this.triggerStatusLabel.Text = "Trigger Bot : Inactive";
            // 
            // holderPanel
            // 
            this.holderPanel.BackColor = System.Drawing.Color.White;
            this.holderPanel.Controls.Add(this.aimStatusLabel);
            this.holderPanel.Controls.Add(this.triggerStatusLabel);
            this.holderPanel.Location = new System.Drawing.Point(10, 10);
            this.holderPanel.Name = "holderPanel";
            this.holderPanel.Size = new System.Drawing.Size(144, 56);
            this.holderPanel.TabIndex = 2;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1920, 1080);
            this.Controls.Add(this.holderPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(1920, 1080);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(1918, 1030);
            this.Name = "MainForm";
            this.Opacity = 0.5D;
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "TigerBot";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.MainForm_Paint);
            this.holderPanel.ResumeLayout(false);
            this.holderPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label aimStatusLabel;
        private System.Windows.Forms.Label triggerStatusLabel;
        private System.Windows.Forms.Panel holderPanel;
    }
}

