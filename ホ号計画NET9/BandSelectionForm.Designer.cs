namespace ホ号計画
{
    partial class BandSelectionForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.eegBandPanel = new System.Windows.Forms.Panel();
            this.deltaBandCheckBox = new System.Windows.Forms.CheckBox();
            this.thetaBandCheckBox = new System.Windows.Forms.CheckBox();
            this.alphaBandCheckBox = new System.Windows.Forms.CheckBox();
            this.betaBandCheckBox = new System.Windows.Forms.CheckBox();
            this.gammaBandCheckBox = new System.Windows.Forms.CheckBox();
            this.heartBandPanel = new System.Windows.Forms.Panel();
            this.lfBandCheckBox = new System.Windows.Forms.CheckBox();
            this.hfBandCheckBox = new System.Windows.Forms.CheckBox();
            this.selectAllButton = new System.Windows.Forms.Button();
            this.deselectAllButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.eegBandPanel.SuspendLayout();
            this.heartBandPanel.SuspendLayout();
            this.SuspendLayout();
            
            // 
            // eegBandPanel
            // 
            this.eegBandPanel.Controls.Add(this.deltaBandCheckBox);
            this.eegBandPanel.Controls.Add(this.thetaBandCheckBox);
            this.eegBandPanel.Controls.Add(this.alphaBandCheckBox);
            this.eegBandPanel.Controls.Add(this.betaBandCheckBox);
            this.eegBandPanel.Controls.Add(this.gammaBandCheckBox);
            this.eegBandPanel.Location = new System.Drawing.Point(20, 20);
            this.eegBandPanel.Name = "eegBandPanel";
            this.eegBandPanel.Size = new System.Drawing.Size(340, 120);
            this.eegBandPanel.TabIndex = 0;
            
            // 
            // deltaBandCheckBox
            // 
            this.deltaBandCheckBox.AutoSize = true;
            this.deltaBandCheckBox.Location = new System.Drawing.Point(10, 10);
            this.deltaBandCheckBox.Name = "deltaBandCheckBox";
            this.deltaBandCheckBox.Size = new System.Drawing.Size(120, 22);
            this.deltaBandCheckBox.TabIndex = 0;
            this.deltaBandCheckBox.Text = "δ波";
            this.deltaBandCheckBox.UseVisualStyleBackColor = true;
            
            // 
            // thetaBandCheckBox
            // 
            this.thetaBandCheckBox.AutoSize = true;
            this.thetaBandCheckBox.Location = new System.Drawing.Point(180, 10);
            this.thetaBandCheckBox.Name = "thetaBandCheckBox";
            this.thetaBandCheckBox.Size = new System.Drawing.Size(110, 22);
            this.thetaBandCheckBox.TabIndex = 1;
            this.thetaBandCheckBox.Text = "θ波";
            this.thetaBandCheckBox.UseVisualStyleBackColor = true;
            
            // 
            // alphaBandCheckBox
            // 
            this.alphaBandCheckBox.AutoSize = true;
            this.alphaBandCheckBox.Location = new System.Drawing.Point(10, 40);
            this.alphaBandCheckBox.Name = "alphaBandCheckBox";
            this.alphaBandCheckBox.Size = new System.Drawing.Size(120, 22);
            this.alphaBandCheckBox.TabIndex = 2;
            this.alphaBandCheckBox.Text = "α波";
            this.alphaBandCheckBox.UseVisualStyleBackColor = true;
            
            // 
            // betaBandCheckBox
            // 
            this.betaBandCheckBox.AutoSize = true;
            this.betaBandCheckBox.Location = new System.Drawing.Point(180, 40);
            this.betaBandCheckBox.Name = "betaBandCheckBox";
            this.betaBandCheckBox.Size = new System.Drawing.Size(130, 22);
            this.betaBandCheckBox.TabIndex = 3;
            this.betaBandCheckBox.Text = "β波";
            this.betaBandCheckBox.UseVisualStyleBackColor = true;
            
            // 
            // gammaBandCheckBox
            // 
            this.gammaBandCheckBox.AutoSize = true;
            this.gammaBandCheckBox.Location = new System.Drawing.Point(10, 70);
            this.gammaBandCheckBox.Name = "gammaBandCheckBox";
            this.gammaBandCheckBox.Size = new System.Drawing.Size(140, 22);
            this.gammaBandCheckBox.TabIndex = 4;
            this.gammaBandCheckBox.Text = "γ波";
            this.gammaBandCheckBox.UseVisualStyleBackColor = true;
            
            // 
            // heartBandPanel
            // 
            this.heartBandPanel.Controls.Add(this.lfBandCheckBox);
            this.heartBandPanel.Controls.Add(this.hfBandCheckBox);
            this.heartBandPanel.Location = new System.Drawing.Point(20, 20);
            this.heartBandPanel.Name = "heartBandPanel";
            this.heartBandPanel.Size = new System.Drawing.Size(340, 120);
            this.heartBandPanel.TabIndex = 1;
            this.heartBandPanel.Visible = false;
            
            // 
            // lfBandCheckBox
            // 
            this.lfBandCheckBox.AutoSize = true;
            this.lfBandCheckBox.Location = new System.Drawing.Point(10, 30);
            this.lfBandCheckBox.Name = "lfBandCheckBox";
            this.lfBandCheckBox.Size = new System.Drawing.Size(180, 22);
            this.lfBandCheckBox.TabIndex = 0;
            this.lfBandCheckBox.Text = "低周波成分 (LF: 0.04-0.15Hz)";
            this.lfBandCheckBox.UseVisualStyleBackColor = true;
            
            // 
            // hfBandCheckBox
            // 
            this.hfBandCheckBox.AutoSize = true;
            this.hfBandCheckBox.Location = new System.Drawing.Point(10, 60);
            this.hfBandCheckBox.Name = "hfBandCheckBox";
            this.hfBandCheckBox.Size = new System.Drawing.Size(175, 22);
            this.hfBandCheckBox.TabIndex = 1;
            this.hfBandCheckBox.Text = "高周波成分 (HF: 0.15-0.4Hz)";
            this.hfBandCheckBox.UseVisualStyleBackColor = true;
            
            // 
            // selectAllButton
            // 
            this.selectAllButton.Location = new System.Drawing.Point(20, 160);
            this.selectAllButton.Name = "selectAllButton";
            this.selectAllButton.Size = new System.Drawing.Size(80, 30);
            this.selectAllButton.TabIndex = 2;
            this.selectAllButton.Text = "全選択";
            this.selectAllButton.UseVisualStyleBackColor = true;
            this.selectAllButton.Click += new System.EventHandler(this.selectAllButton_Click);
            
            // 
            // deselectAllButton
            // 
            this.deselectAllButton.Location = new System.Drawing.Point(110, 160);
            this.deselectAllButton.Name = "deselectAllButton";
            this.deselectAllButton.Size = new System.Drawing.Size(80, 30);
            this.deselectAllButton.TabIndex = 3;
            this.deselectAllButton.Text = "全解除";
            this.deselectAllButton.UseVisualStyleBackColor = true;
            this.deselectAllButton.Click += new System.EventHandler(this.deselectAllButton_Click);
            
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(200, 160);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(80, 30);
            this.okButton.TabIndex = 4;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(290, 160);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(80, 30);
            this.cancelButton.TabIndex = 5;
            this.cancelButton.Text = "キャンセル";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            
            // 
            // BandSelectionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(390, 210);
            this.Controls.Add(this.eegBandPanel);
            this.Controls.Add(this.heartBandPanel);
            this.Controls.Add(this.selectAllButton);
            this.Controls.Add(this.deselectAllButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BandSelectionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "バンド選択";
            this.eegBandPanel.ResumeLayout(false);
            this.eegBandPanel.PerformLayout();
            this.heartBandPanel.ResumeLayout(false);
            this.heartBandPanel.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel eegBandPanel;
        private System.Windows.Forms.CheckBox deltaBandCheckBox;
        private System.Windows.Forms.CheckBox thetaBandCheckBox;
        private System.Windows.Forms.CheckBox alphaBandCheckBox;
        private System.Windows.Forms.CheckBox betaBandCheckBox;
        private System.Windows.Forms.CheckBox gammaBandCheckBox;
        private System.Windows.Forms.Panel heartBandPanel;
        private System.Windows.Forms.CheckBox lfBandCheckBox;
        private System.Windows.Forms.CheckBox hfBandCheckBox;
        private System.Windows.Forms.Button selectAllButton;
        private System.Windows.Forms.Button deselectAllButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}