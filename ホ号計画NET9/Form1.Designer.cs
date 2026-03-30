namespace ホ号計画
{
    partial class Form1
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            topPanel = new Panel();
            topTableLayoutPanel = new TableLayoutPanel();
            outputPanel = new Panel();
            outputFileLabel = new Label();
            outputPreviewBox = new PictureBox();
            csvExportButton = new Button();
            settingsPanel = new Panel();
            modeToggleButton = new Button();
            commonSettingsPanel = new Panel();
            settingsTableLayoutPanel = new TableLayoutPanel();
            divisionCountNumeric = new NumericUpDown();
            spectrogramExecuteButton = new Button();
            spectrogramProgressBar = new ProgressBar();
            windowFunctionLabel = new Label();
            windowFunctionCombo = new ComboBox();
            timeRangeLabel = new Label();
            totalRangeNumeric = new NumericUpDown();
            totalRangeLabel = new Label();
            timeMinNumeric = new NumericUpDown();
            timeMaxNumeric = new NumericUpDown();
            freqRangeLabel = new Label();
            freqMinNumeric = new NumericUpDown();
            freqMaxNumeric = new NumericUpDown();
            interpolationCheckBox = new CheckBox();
            dcRemovalCheckBox = new CheckBox();
            logScaleCheckBox = new CheckBox();
            timeResolutionLabel = new Label();
            timeResolutionNumeric = new NumericUpDown();
            divisionCountLabel = new Label();
            bandPowerSelectButton = new Button();
            bandPowerPanel = new Panel();
            bandSelectionLabel = new Label();
            eegDeltaCheckBox = new CheckBox();
            eegThetaCheckBox = new CheckBox();
            eegAlphaCheckBox = new CheckBox();
            eegBetaCheckBox = new CheckBox();
            eegGammaCheckBox = new CheckBox();
            heartLFCheckBox = new CheckBox();
            heartHFCheckBox = new CheckBox();
            heartLfHfRatioCheckBox = new CheckBox();
            bandPowerBackButton = new Button();
            bandPowerGenerateButton = new Button();
            inputPanel = new Panel();
            inputFileLabel = new Label();
            inputPreviewBox = new PictureBox();
            eegModeButton = new Button();
            cbfModeButton = new Button();
            heartHfButton = new Button();
            heartLfButton = new Button();
            bandPowerAnalysisButton = new Button();
            bandPowerExportButton = new Button();
            eegBandPowerAnalysisButton = new Button();
            eegBandPowerExportButton = new Button();
            eegSelectedBandsLabel = new Label();
            eegBandSettingsButton = new Button();
            eegColorMapLabel = new Label();
            eegColorMapCombo = new ComboBox();
            eegDbRangeLabel = new Label();
            eegDbMinNumeric = new NumericUpDown();
            eegDbMaxNumeric = new NumericUpDown();
            heartSelectedBandsLabel = new Label();
            heartBandSettingsButton = new Button();
            heartBandPowerAnalysisButton = new Button();
            heartColorMapLabel = new Label();
            heartColorMapCombo = new ComboBox();
            heartDbRangeLabel = new Label();
            heartDbMinNumeric = new NumericUpDown();
            heartDbMaxNumeric = new NumericUpDown();
            chartPanel = new Panel();
            exportButton = new Button();
            chartPlotView = new OxyPlot.WindowsForms.PlotView();
            topPanel.SuspendLayout();
            topTableLayoutPanel.SuspendLayout();
            outputPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)outputPreviewBox).BeginInit();
            settingsPanel.SuspendLayout();
            commonSettingsPanel.SuspendLayout();
            settingsTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)divisionCountNumeric).BeginInit();
            ((System.ComponentModel.ISupportInitialize)totalRangeNumeric).BeginInit();
            ((System.ComponentModel.ISupportInitialize)timeMinNumeric).BeginInit();
            ((System.ComponentModel.ISupportInitialize)timeMaxNumeric).BeginInit();
            ((System.ComponentModel.ISupportInitialize)freqMinNumeric).BeginInit();
            ((System.ComponentModel.ISupportInitialize)freqMaxNumeric).BeginInit();
            ((System.ComponentModel.ISupportInitialize)timeResolutionNumeric).BeginInit();
            bandPowerPanel.SuspendLayout();
            inputPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)inputPreviewBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)eegDbMinNumeric).BeginInit();
            ((System.ComponentModel.ISupportInitialize)eegDbMaxNumeric).BeginInit();
            ((System.ComponentModel.ISupportInitialize)heartDbMinNumeric).BeginInit();
            ((System.ComponentModel.ISupportInitialize)heartDbMaxNumeric).BeginInit();
            chartPanel.SuspendLayout();
            SuspendLayout();
            //
            // topTableLayoutPanel
            //
            topTableLayoutPanel.ColumnCount = 3;
            topTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            topTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            topTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            topTableLayoutPanel.RowCount = 1;
            topTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            topTableLayoutPanel.Dock = DockStyle.Fill;
            topTableLayoutPanel.Margin = new Padding(0);
            topTableLayoutPanel.Name = "topTableLayoutPanel";
            topTableLayoutPanel.Controls.Add(inputPanel, 0, 0);
            topTableLayoutPanel.Controls.Add(settingsPanel, 1, 0);
            topTableLayoutPanel.Controls.Add(bandPowerPanel, 1, 0);
            topTableLayoutPanel.Controls.Add(outputPanel, 2, 0);
            //
            // topPanel
            //
            topPanel.Controls.Add(topTableLayoutPanel);
            topPanel.Dock = DockStyle.Top;
            topPanel.Location = new Point(0, 0);
            topPanel.Margin = new Padding(3, 4, 3, 4);
            topPanel.Name = "topPanel";
            topPanel.Size = new Size(1260, 486);
            topPanel.TabIndex = 0;
            // 
            // outputPanel
            // 
            outputPanel.AutoScroll = true;
            outputPanel.BorderStyle = BorderStyle.FixedSingle;
            outputPanel.Controls.Add(outputFileLabel);
            outputPanel.Controls.Add(outputPreviewBox);
            outputPanel.Controls.Add(csvExportButton);
            outputPanel.Dock = DockStyle.Fill;
            outputPanel.Margin = new Padding(0);
            outputPanel.Name = "outputPanel";
            outputPanel.TabIndex = 2;
            // 
            // outputFileLabel
            // 
            outputFileLabel.AutoSize = true;
            outputFileLabel.Location = new Point(10, 14);
            outputFileLabel.Name = "outputFileLabel";
            outputFileLabel.Size = new Size(110, 25);
            outputFileLabel.TabIndex = 1;
            outputFileLabel.Text = "出力プレビュー";
            // 
            // outputPreviewBox
            // 
            outputPreviewBox.BackColor = Color.LightGray;
            outputPreviewBox.BorderStyle = BorderStyle.FixedSingle;
            outputPreviewBox.Location = new Point(10, 56);
            outputPreviewBox.Margin = new Padding(3, 4, 3, 4);
            outputPreviewBox.Name = "outputPreviewBox";
            outputPreviewBox.Size = new Size(440, 300);
            outputPreviewBox.SizeMode = PictureBoxSizeMode.AutoSize;
            outputPreviewBox.TabIndex = 0;
            outputPreviewBox.TabStop = false;
            // 
            // csvExportButton
            // 
            csvExportButton.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 128);
            csvExportButton.Location = new Point(340, 14);
            csvExportButton.Margin = new Padding(3, 4, 3, 4);
            csvExportButton.Name = "csvExportButton";
            csvExportButton.Size = new Size(110, 35);
            csvExportButton.TabIndex = 2;
            csvExportButton.Text = "CSVデータ出力";
            csvExportButton.UseVisualStyleBackColor = true;
            // 
            // settingsPanel
            // 
            settingsPanel.AutoScroll = true;
            settingsPanel.AutoScrollMinSize = new Size(0, 400);
            settingsPanel.BorderStyle = BorderStyle.FixedSingle;
            settingsPanel.Controls.Add(modeToggleButton);
            settingsPanel.Controls.Add(commonSettingsPanel);
            settingsPanel.Controls.Add(bandPowerSelectButton);
            settingsPanel.Dock = DockStyle.Fill;
            settingsPanel.Margin = new Padding(0);
            settingsPanel.Name = "settingsPanel";
            settingsPanel.TabIndex = 1;
            settingsPanel.Paint += settingsPanel_Paint_1;
            // 
            // modeToggleButton
            // 
            modeToggleButton.BackColor = Color.LightBlue;
            modeToggleButton.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold);
            modeToggleButton.Location = new Point(210, -2);
            modeToggleButton.Margin = new Padding(3, 4, 3, 4);
            modeToggleButton.Name = "modeToggleButton";
            modeToggleButton.Size = new Size(100, 42);
            modeToggleButton.TabIndex = 12;
            modeToggleButton.Text = "ひよこモード";
            modeToggleButton.UseVisualStyleBackColor = false;
            modeToggleButton.Click += ModeToggleButton_Click;
            // 
            // commonSettingsPanel
            // 
            commonSettingsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            commonSettingsPanel.Controls.Add(settingsTableLayoutPanel);
            commonSettingsPanel.Location = new Point(10, 64);
            commonSettingsPanel.Margin = new Padding(3, 4, 3, 4);
            commonSettingsPanel.Name = "commonSettingsPanel";
            commonSettingsPanel.Size = new Size(297, 413);
            commonSettingsPanel.TabIndex = 20;
            // 
            // settingsTableLayoutPanel
            // 
            settingsTableLayoutPanel.ColumnCount = 3;
            settingsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            settingsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            settingsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            settingsTableLayoutPanel.Controls.Add(divisionCountNumeric, 1, 8);
            settingsTableLayoutPanel.Controls.Add(spectrogramExecuteButton, 0, 9);
            settingsTableLayoutPanel.Controls.Add(spectrogramProgressBar, 0, 10);
            settingsTableLayoutPanel.Controls.Add(windowFunctionLabel, 0, 0);
            settingsTableLayoutPanel.Controls.Add(windowFunctionCombo, 1, 0);
            settingsTableLayoutPanel.Controls.Add(timeRangeLabel, 0, 1);
            settingsTableLayoutPanel.Controls.Add(totalRangeNumeric, 1, 7);
            settingsTableLayoutPanel.Controls.Add(totalRangeLabel, 0, 7);
            settingsTableLayoutPanel.Controls.Add(timeMinNumeric, 1, 1);
            settingsTableLayoutPanel.Controls.Add(timeMaxNumeric, 2, 1);
            settingsTableLayoutPanel.Controls.Add(freqRangeLabel, 0, 2);
            settingsTableLayoutPanel.Controls.Add(freqMinNumeric, 1, 2);
            settingsTableLayoutPanel.Controls.Add(freqMaxNumeric, 2, 2);
            settingsTableLayoutPanel.Controls.Add(dcRemovalCheckBox, 0, 3);
            settingsTableLayoutPanel.Controls.Add(interpolationCheckBox, 0, 4);
            settingsTableLayoutPanel.Controls.Add(logScaleCheckBox, 0, 5);
            settingsTableLayoutPanel.Controls.Add(timeResolutionLabel, 0, 6);
            settingsTableLayoutPanel.Controls.Add(timeResolutionNumeric, 1, 6);
            settingsTableLayoutPanel.Controls.Add(divisionCountLabel, 0, 8);
            settingsTableLayoutPanel.Location = new Point(0, -21);
            settingsTableLayoutPanel.Margin = new Padding(0);
            settingsTableLayoutPanel.Name = "settingsTableLayoutPanel";
            settingsTableLayoutPanel.Padding = new Padding(5, 7, 5, 7);
            settingsTableLayoutPanel.RowCount = 11;
            settingsTableLayoutPanel.RowStyles.Add(new RowStyle());
            settingsTableLayoutPanel.RowStyles.Add(new RowStyle());
            settingsTableLayoutPanel.RowStyles.Add(new RowStyle());
            settingsTableLayoutPanel.RowStyles.Add(new RowStyle());
            settingsTableLayoutPanel.RowStyles.Add(new RowStyle());
            settingsTableLayoutPanel.RowStyles.Add(new RowStyle());
            settingsTableLayoutPanel.RowStyles.Add(new RowStyle());
            settingsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            settingsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            settingsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 43F));
            settingsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 19F));
            settingsTableLayoutPanel.Size = new Size(372, 400);
            settingsTableLayoutPanel.TabIndex = 0;
            settingsTableLayoutPanel.Paint += settingsTableLayoutPanel_Paint;
            // 
            // divisionCountNumeric
            // 
            divisionCountNumeric.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            divisionCountNumeric.Location = new Point(131, 277);
            divisionCountNumeric.Margin = new Padding(3, 4, 3, 4);
            divisionCountNumeric.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            divisionCountNumeric.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            divisionCountNumeric.Name = "divisionCountNumeric";
            divisionCountNumeric.Size = new Size(113, 31);
            divisionCountNumeric.TabIndex = 17;
            divisionCountNumeric.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // spectrogramExecuteButton
            // 
            spectrogramExecuteButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            settingsTableLayoutPanel.SetColumnSpan(spectrogramExecuteButton, 3);
            spectrogramExecuteButton.Location = new Point(8, 317);
            spectrogramExecuteButton.Margin = new Padding(3, 4, 3, 4);
            spectrogramExecuteButton.Name = "spectrogramExecuteButton";
            spectrogramExecuteButton.Size = new Size(356, 35);
            spectrogramExecuteButton.TabIndex = 7;
            spectrogramExecuteButton.Text = "スペクトログラム表示";
            spectrogramExecuteButton.UseVisualStyleBackColor = true;
            // 
            // spectrogramProgressBar
            // 
            spectrogramProgressBar.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            settingsTableLayoutPanel.SetColumnSpan(spectrogramProgressBar, 3);
            spectrogramProgressBar.Location = new Point(8, 368);
            spectrogramProgressBar.Margin = new Padding(3, 4, 3, 4);
            spectrogramProgressBar.Name = "spectrogramProgressBar";
            spectrogramProgressBar.Size = new Size(356, 12);
            spectrogramProgressBar.TabIndex = 9;
            spectrogramProgressBar.Visible = false;
            // 
            // windowFunctionLabel
            // 
            windowFunctionLabel.Anchor = AnchorStyles.Left;
            windowFunctionLabel.AutoSize = true;
            windowFunctionLabel.Location = new Point(8, 15);
            windowFunctionLabel.Name = "windowFunctionLabel";
            windowFunctionLabel.Size = new Size(70, 25);
            windowFunctionLabel.TabIndex = 1;
            windowFunctionLabel.Text = "窓関数:";
            // 
            // windowFunctionCombo
            // 
            windowFunctionCombo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            settingsTableLayoutPanel.SetColumnSpan(windowFunctionCombo, 2);
            windowFunctionCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            windowFunctionCombo.FormattingEnabled = true;
            windowFunctionCombo.Items.AddRange(new object[] { "Rectangular", "Hanning", "Hamming", "Blackman" });
            windowFunctionCombo.Location = new Point(131, 11);
            windowFunctionCombo.Margin = new Padding(3, 4, 3, 4);
            windowFunctionCombo.Name = "windowFunctionCombo";
            windowFunctionCombo.Size = new Size(233, 33);
            windowFunctionCombo.TabIndex = 0;
            // 
            // timeRangeLabel
            // 
            timeRangeLabel.Anchor = AnchorStyles.Left;
            timeRangeLabel.AutoSize = true;
            timeRangeLabel.Location = new Point(8, 48);
            timeRangeLabel.Name = "timeRangeLabel";
            timeRangeLabel.Size = new Size(107, 50);
            timeRangeLabel.TabIndex = 6;
            timeRangeLabel.Text = "時間軸範囲(秒)";
            // 
            // totalRangeNumeric
            // 
            totalRangeNumeric.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            totalRangeNumeric.DecimalPlaces = 2;
            totalRangeNumeric.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            totalRangeNumeric.Location = new Point(131, 239);
            totalRangeNumeric.Margin = new Padding(3, 4, 3, 4);
            totalRangeNumeric.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            totalRangeNumeric.Minimum = new decimal(new int[] { 1, 0, 0, 131072 });
            totalRangeNumeric.Name = "totalRangeNumeric";
            totalRangeNumeric.Size = new Size(113, 31);
            totalRangeNumeric.TabIndex = 19;
            totalRangeNumeric.Value = new decimal(new int[] { 100, 0, 0, 131072 });
            // 
            // totalRangeLabel
            // 
            totalRangeLabel.AutoSize = true;
            totalRangeLabel.Location = new Point(8, 235);
            totalRangeLabel.Name = "totalRangeLabel";
            totalRangeLabel.Size = new Size(98, 25);
            totalRangeLabel.TabIndex = 18;
            totalRangeLabel.Text = "時間幅(秒):";
            // 
            // timeMinNumeric
            // 
            timeMinNumeric.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            timeMinNumeric.DecimalPlaces = 2;
            timeMinNumeric.Location = new Point(131, 57);
            timeMinNumeric.Margin = new Padding(3, 4, 3, 4);
            timeMinNumeric.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            timeMinNumeric.Name = "timeMinNumeric";
            timeMinNumeric.Size = new Size(113, 31);
            timeMinNumeric.TabIndex = 7;
            // 
            // timeMaxNumeric
            // 
            timeMaxNumeric.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            timeMaxNumeric.DecimalPlaces = 2;
            timeMaxNumeric.Location = new Point(250, 57);
            timeMaxNumeric.Margin = new Padding(3, 4, 3, 4);
            timeMaxNumeric.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            timeMaxNumeric.Name = "timeMaxNumeric";
            timeMaxNumeric.Size = new Size(114, 31);
            timeMaxNumeric.TabIndex = 8;
            // 
            // freqRangeLabel
            // 
            freqRangeLabel.Anchor = AnchorStyles.Left;
            freqRangeLabel.AutoSize = true;
            freqRangeLabel.Location = new Point(8, 98);
            freqRangeLabel.Name = "freqRangeLabel";
            freqRangeLabel.Size = new Size(102, 50);
            freqRangeLabel.TabIndex = 9;
            freqRangeLabel.Text = "周波数範囲(Hz)";
            // 
            // freqMinNumeric
            // 
            freqMinNumeric.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            freqMinNumeric.DecimalPlaces = 1;
            freqMinNumeric.Location = new Point(131, 107);
            freqMinNumeric.Margin = new Padding(3, 4, 3, 4);
            freqMinNumeric.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            freqMinNumeric.Name = "freqMinNumeric";
            freqMinNumeric.Size = new Size(113, 31);
            freqMinNumeric.TabIndex = 10;
            // 
            // freqMaxNumeric
            // 
            freqMaxNumeric.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            freqMaxNumeric.DecimalPlaces = 1;
            freqMaxNumeric.Location = new Point(250, 107);
            freqMaxNumeric.Margin = new Padding(3, 4, 3, 4);
            freqMaxNumeric.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            freqMaxNumeric.Name = "freqMaxNumeric";
            freqMaxNumeric.Size = new Size(114, 31);
            freqMaxNumeric.TabIndex = 11;
            freqMaxNumeric.Value = new decimal(new int[] { 100, 0, 0, 0 });
            //
            // dcRemovalCheckBox
            //
            dcRemovalCheckBox.Anchor = AnchorStyles.Left;
            dcRemovalCheckBox.AutoSize = true;
            settingsTableLayoutPanel.SetColumnSpan(dcRemovalCheckBox, 3);
            dcRemovalCheckBox.Margin = new Padding(3, 4, 3, 4);
            dcRemovalCheckBox.Name = "dcRemovalCheckBox";
            dcRemovalCheckBox.Size = new Size(181, 29);
            dcRemovalCheckBox.TabIndex = 13;
            dcRemovalCheckBox.Text = "DC成分除去";
            dcRemovalCheckBox.Checked = true;
            dcRemovalCheckBox.UseVisualStyleBackColor = true;
            //
            // interpolationCheckBox
            //
            interpolationCheckBox.Anchor = AnchorStyles.Left;
            interpolationCheckBox.AutoSize = true;
            settingsTableLayoutPanel.SetColumnSpan(interpolationCheckBox, 3);
            interpolationCheckBox.Location = new Point(8, 152);
            interpolationCheckBox.Margin = new Padding(3, 4, 3, 4);
            interpolationCheckBox.Name = "interpolationCheckBox";
            interpolationCheckBox.Size = new Size(181, 29);
            interpolationCheckBox.TabIndex = 12;
            interpolationCheckBox.Text = "スペクトログラム補間";
            interpolationCheckBox.UseVisualStyleBackColor = true;
            //
            // logScaleCheckBox
            //
            logScaleCheckBox.Anchor = AnchorStyles.Left;
            logScaleCheckBox.AutoSize = true;
            settingsTableLayoutPanel.SetColumnSpan(logScaleCheckBox, 3);
            logScaleCheckBox.Margin = new Padding(3, 4, 3, 4);
            logScaleCheckBox.Name = "logScaleCheckBox";
            logScaleCheckBox.Size = new Size(181, 29);
            logScaleCheckBox.TabIndex = 22;
            logScaleCheckBox.Text = "周波数軸：対数スケール";
            logScaleCheckBox.UseVisualStyleBackColor = true;
            //
            // timeResolutionLabel
            // 
            timeResolutionLabel.AutoSize = true;
            timeResolutionLabel.Location = new Point(8, 185);
            timeResolutionLabel.Name = "timeResolutionLabel";
            timeResolutionLabel.Size = new Size(107, 50);
            timeResolutionLabel.TabIndex = 21;
            timeResolutionLabel.Text = "時間移動幅(秒):";
            // 
            // timeResolutionNumeric
            // 
            timeResolutionNumeric.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            timeResolutionNumeric.DecimalPlaces = 3;
            timeResolutionNumeric.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            timeResolutionNumeric.Location = new Point(131, 194);
            timeResolutionNumeric.Margin = new Padding(3, 4, 3, 4);
            timeResolutionNumeric.Name = "timeResolutionNumeric";
            timeResolutionNumeric.Size = new Size(113, 31);
            timeResolutionNumeric.TabIndex = 20;
            // 
            // divisionCountLabel
            // 
            divisionCountLabel.AutoSize = true;
            divisionCountLabel.Location = new Point(8, 273);
            divisionCountLabel.Name = "divisionCountLabel";
            divisionCountLabel.Size = new Size(70, 25);
            divisionCountLabel.TabIndex = 16;
            divisionCountLabel.Text = "分割数:";
            // 
            // bandPowerSelectButton
            // 
            bandPowerSelectButton.Location = new Point(-8, -4);
            bandPowerSelectButton.Margin = new Padding(3, 4, 3, 4);
            bandPowerSelectButton.Name = "bandPowerSelectButton";
            bandPowerSelectButton.Size = new Size(170, 42);
            bandPowerSelectButton.TabIndex = 20;
            bandPowerSelectButton.Text = "バンドパワー解析";
            bandPowerSelectButton.UseVisualStyleBackColor = true;
            // 
            // bandPowerPanel
            // 
            bandPowerPanel.BorderStyle = BorderStyle.FixedSingle;
            bandPowerPanel.Controls.Add(bandSelectionLabel);
            bandPowerPanel.Controls.Add(eegDeltaCheckBox);
            bandPowerPanel.Controls.Add(eegThetaCheckBox);
            bandPowerPanel.Controls.Add(eegAlphaCheckBox);
            bandPowerPanel.Controls.Add(eegBetaCheckBox);
            bandPowerPanel.Controls.Add(eegGammaCheckBox);
            bandPowerPanel.Controls.Add(heartLFCheckBox);
            bandPowerPanel.Controls.Add(heartHFCheckBox);
            bandPowerPanel.Controls.Add(heartLfHfRatioCheckBox);
            bandPowerPanel.Controls.Add(bandPowerBackButton);
            bandPowerPanel.Controls.Add(bandPowerGenerateButton);
            bandPowerPanel.Dock = DockStyle.Fill;
            bandPowerPanel.Margin = new Padding(0);
            bandPowerPanel.Name = "bandPowerPanel";
            bandPowerPanel.TabIndex = 2;
            bandPowerPanel.Visible = false;
            // 
            // bandSelectionLabel
            // 
            bandSelectionLabel.AutoSize = true;
            bandSelectionLabel.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold);
            bandSelectionLabel.Location = new Point(15, 83);
            bandSelectionLabel.Name = "bandSelectionLabel";
            bandSelectionLabel.Size = new Size(161, 22);
            bandSelectionLabel.TabIndex = 8;
            bandSelectionLabel.Text = "解析する波を選択：";
            // 
            // eegDeltaCheckBox
            // 
            eegDeltaCheckBox.AutoSize = true;
            eegDeltaCheckBox.Checked = true;
            eegDeltaCheckBox.CheckState = CheckState.Checked;
            eegDeltaCheckBox.Location = new Point(15, 125);
            eegDeltaCheckBox.Margin = new Padding(3, 4, 3, 4);
            eegDeltaCheckBox.Name = "eegDeltaCheckBox";
            eegDeltaCheckBox.Size = new Size(67, 29);
            eegDeltaCheckBox.TabIndex = 9;
            eegDeltaCheckBox.Text = "δ波";
            eegDeltaCheckBox.UseVisualStyleBackColor = true;
            // 
            // eegThetaCheckBox
            // 
            eegThetaCheckBox.AutoSize = true;
            eegThetaCheckBox.Checked = true;
            eegThetaCheckBox.CheckState = CheckState.Checked;
            eegThetaCheckBox.Location = new Point(15, 153);
            eegThetaCheckBox.Margin = new Padding(3, 4, 3, 4);
            eegThetaCheckBox.Name = "eegThetaCheckBox";
            eegThetaCheckBox.Size = new Size(67, 29);
            eegThetaCheckBox.TabIndex = 10;
            eegThetaCheckBox.Text = "θ波";
            eegThetaCheckBox.UseVisualStyleBackColor = true;
            // 
            // eegAlphaCheckBox
            // 
            eegAlphaCheckBox.AutoSize = true;
            eegAlphaCheckBox.Checked = true;
            eegAlphaCheckBox.CheckState = CheckState.Checked;
            eegAlphaCheckBox.Location = new Point(15, 181);
            eegAlphaCheckBox.Margin = new Padding(3, 4, 3, 4);
            eegAlphaCheckBox.Name = "eegAlphaCheckBox";
            eegAlphaCheckBox.Size = new Size(67, 29);
            eegAlphaCheckBox.TabIndex = 11;
            eegAlphaCheckBox.Text = "α波";
            eegAlphaCheckBox.UseVisualStyleBackColor = true;
            // 
            // eegBetaCheckBox
            // 
            eegBetaCheckBox.AutoSize = true;
            eegBetaCheckBox.Checked = true;
            eegBetaCheckBox.CheckState = CheckState.Checked;
            eegBetaCheckBox.Location = new Point(15, 208);
            eegBetaCheckBox.Margin = new Padding(3, 4, 3, 4);
            eegBetaCheckBox.Name = "eegBetaCheckBox";
            eegBetaCheckBox.Size = new Size(66, 29);
            eegBetaCheckBox.TabIndex = 12;
            eegBetaCheckBox.Text = "β波";
            eegBetaCheckBox.UseVisualStyleBackColor = true;
            // 
            // eegGammaCheckBox
            // 
            eegGammaCheckBox.AutoSize = true;
            eegGammaCheckBox.Checked = true;
            eegGammaCheckBox.CheckState = CheckState.Checked;
            eegGammaCheckBox.Location = new Point(15, 236);
            eegGammaCheckBox.Margin = new Padding(3, 4, 3, 4);
            eegGammaCheckBox.Name = "eegGammaCheckBox";
            eegGammaCheckBox.Size = new Size(65, 29);
            eegGammaCheckBox.TabIndex = 13;
            eegGammaCheckBox.Text = "γ波";
            eegGammaCheckBox.UseVisualStyleBackColor = true;
            // 
            // heartLFCheckBox
            // 
            heartLFCheckBox.AutoSize = true;
            heartLFCheckBox.Checked = true;
            heartLFCheckBox.CheckState = CheckState.Checked;
            heartLFCheckBox.Location = new Point(15, 125);
            heartLFCheckBox.Margin = new Padding(3, 4, 3, 4);
            heartLFCheckBox.Name = "heartLFCheckBox";
            heartLFCheckBox.Size = new Size(187, 29);
            heartLFCheckBox.TabIndex = 14;
            heartLFCheckBox.Text = "LF (Low Frequency)";
            heartLFCheckBox.UseVisualStyleBackColor = true;
            heartLFCheckBox.Visible = false;
            // 
            // heartHFCheckBox
            // 
            heartHFCheckBox.AutoSize = true;
            heartHFCheckBox.Checked = true;
            heartHFCheckBox.CheckState = CheckState.Checked;
            heartHFCheckBox.Location = new Point(15, 153);
            heartHFCheckBox.Margin = new Padding(3, 4, 3, 4);
            heartHFCheckBox.Name = "heartHFCheckBox";
            heartHFCheckBox.Size = new Size(198, 29);
            heartHFCheckBox.TabIndex = 15;
            heartHFCheckBox.Text = "HF (High Frequency)";
            heartHFCheckBox.UseVisualStyleBackColor = true;
            heartHFCheckBox.Visible = false;
            // 
            // heartLfHfRatioCheckBox
            // 
            heartLfHfRatioCheckBox.AutoSize = true;
            heartLfHfRatioCheckBox.Checked = true;
            heartLfHfRatioCheckBox.CheckState = CheckState.Checked;
            heartLfHfRatioCheckBox.Location = new Point(15, 181);
            heartLfHfRatioCheckBox.Margin = new Padding(3, 4, 3, 4);
            heartLfHfRatioCheckBox.Name = "heartLfHfRatioCheckBox";
            heartLfHfRatioCheckBox.Size = new Size(102, 29);
            heartLfHfRatioCheckBox.TabIndex = 16;
            heartLfHfRatioCheckBox.Text = "LF/HF比";
            heartLfHfRatioCheckBox.UseVisualStyleBackColor = true;
            heartLfHfRatioCheckBox.Visible = false;
            // 
            // bandPowerBackButton
            // 
            bandPowerBackButton.Location = new Point(10, 14);
            bandPowerBackButton.Margin = new Padding(3, 4, 3, 4);
            bandPowerBackButton.Name = "bandPowerBackButton";
            bandPowerBackButton.Size = new Size(80, 42);
            bandPowerBackButton.TabIndex = 0;
            bandPowerBackButton.Text = "← 戻る";
            bandPowerBackButton.UseVisualStyleBackColor = true;
            // 
            // bandPowerGenerateButton
            // 
            bandPowerGenerateButton.Anchor = AnchorStyles.Bottom;
            bandPowerGenerateButton.Location = new Point(121, 309);
            bandPowerGenerateButton.Margin = new Padding(3, 4, 3, 4);
            bandPowerGenerateButton.Name = "bandPowerGenerateButton";
            bandPowerGenerateButton.Size = new Size(157, 56);
            bandPowerGenerateButton.TabIndex = 2;
            bandPowerGenerateButton.Text = "グラフ生成";
            bandPowerGenerateButton.UseVisualStyleBackColor = true;
            // 
            // inputPanel
            // 
            inputPanel.AutoScroll = true;
            inputPanel.BorderStyle = BorderStyle.FixedSingle;
            inputPanel.Controls.Add(inputFileLabel);
            inputPanel.Controls.Add(inputPreviewBox);
            inputPanel.Dock = DockStyle.Fill;
            inputPanel.Margin = new Padding(0);
            inputPanel.Name = "inputPanel";
            inputPanel.TabIndex = 0;
            // 
            // inputFileLabel
            // 
            inputFileLabel.AutoSize = true;
            inputFileLabel.Location = new Point(10, 14);
            inputFileLabel.Name = "inputFileLabel";
            inputFileLabel.Size = new Size(129, 25);
            inputFileLabel.TabIndex = 1;
            inputFileLabel.Text = "ファイルをドロップ";
            // 
            // inputPreviewBox
            // 
            inputPreviewBox.BackColor = Color.LightGray;
            inputPreviewBox.BorderStyle = BorderStyle.FixedSingle;
            inputPreviewBox.Location = new Point(10, 56);
            inputPreviewBox.Margin = new Padding(3, 4, 3, 4);
            inputPreviewBox.Name = "inputPreviewBox";
            inputPreviewBox.Size = new Size(380, 300);
            inputPreviewBox.SizeMode = PictureBoxSizeMode.AutoSize;
            inputPreviewBox.TabIndex = 0;
            inputPreviewBox.TabStop = false;
            // 
            // eegModeButton
            // 
            eegModeButton.BackColor = SystemColors.Control;
            eegModeButton.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold);
            eegModeButton.Location = new Point(20, 10);
            eegModeButton.Name = "eegModeButton";
            eegModeButton.Size = new Size(80, 30);
            eegModeButton.TabIndex = 10;
            eegModeButton.Text = "脳波モード";
            eegModeButton.UseVisualStyleBackColor = false;
            // 
            // cbfModeButton
            // 
            cbfModeButton.BackColor = Color.LightGreen;
            cbfModeButton.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold);
            cbfModeButton.Location = new Point(110, 10);
            cbfModeButton.Name = "cbfModeButton";
            cbfModeButton.Size = new Size(80, 30);
            cbfModeButton.TabIndex = 11;
            cbfModeButton.Text = "心拍モード";
            cbfModeButton.UseVisualStyleBackColor = false;
            // 
            // heartHfButton
            // 
            heartHfButton.Location = new Point(50, 180);
            heartHfButton.Name = "heartHfButton";
            heartHfButton.Size = new Size(75, 40);
            heartHfButton.TabIndex = 6;
            heartHfButton.Text = "HPF";
            heartHfButton.UseVisualStyleBackColor = true;
            // 
            // heartLfButton
            // 
            heartLfButton.Location = new Point(270, 180);
            heartLfButton.Name = "heartLfButton";
            heartLfButton.Size = new Size(75, 40);
            heartLfButton.TabIndex = 8;
            heartLfButton.Text = "LPF";
            heartLfButton.UseVisualStyleBackColor = true;
            // 
            // bandPowerAnalysisButton
            // 
            bandPowerAnalysisButton.BackColor = Color.LightCyan;
            bandPowerAnalysisButton.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold);
            bandPowerAnalysisButton.Location = new Point(220, 10);
            bandPowerAnalysisButton.Name = "bandPowerAnalysisButton";
            bandPowerAnalysisButton.Size = new Size(130, 30);
            bandPowerAnalysisButton.TabIndex = 12;
            bandPowerAnalysisButton.Text = "バンドパワー解析";
            bandPowerAnalysisButton.UseVisualStyleBackColor = false;
            // 
            // bandPowerExportButton
            // 
            bandPowerExportButton.Font = new Font("Microsoft Sans Serif", 8F);
            bandPowerExportButton.Location = new Point(320, 10);
            bandPowerExportButton.Name = "bandPowerExportButton";
            bandPowerExportButton.Size = new Size(70, 30);
            bandPowerExportButton.TabIndex = 13;
            bandPowerExportButton.Text = "情報";
            bandPowerExportButton.UseVisualStyleBackColor = true;
            // 
            // eegBandPowerAnalysisButton
            // 
            eegBandPowerAnalysisButton.BackColor = Color.LightCyan;
            eegBandPowerAnalysisButton.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold);
            eegBandPowerAnalysisButton.Location = new Point(220, 10);
            eegBandPowerAnalysisButton.Name = "eegBandPowerAnalysisButton";
            eegBandPowerAnalysisButton.Size = new Size(130, 30);
            eegBandPowerAnalysisButton.TabIndex = 12;
            eegBandPowerAnalysisButton.Text = "バンドパワー解析";
            eegBandPowerAnalysisButton.UseVisualStyleBackColor = false;
            // 
            // eegBandPowerExportButton
            // 
            eegBandPowerExportButton.Font = new Font("Microsoft Sans Serif", 8F);
            eegBandPowerExportButton.Location = new Point(320, 10);
            eegBandPowerExportButton.Name = "eegBandPowerExportButton";
            eegBandPowerExportButton.Size = new Size(70, 30);
            eegBandPowerExportButton.TabIndex = 13;
            eegBandPowerExportButton.Text = "情報";
            eegBandPowerExportButton.UseVisualStyleBackColor = true;
            // 
            // eegSelectedBandsLabel
            // 
            eegSelectedBandsLabel.AutoSize = true;
            eegSelectedBandsLabel.Location = new Point(10, 20);
            eegSelectedBandsLabel.Name = "eegSelectedBandsLabel";
            eegSelectedBandsLabel.Size = new Size(100, 18);
            eegSelectedBandsLabel.TabIndex = 0;
            eegSelectedBandsLabel.Text = "解析波：なし";
            // 
            // eegBandSettingsButton
            // 
            eegBandSettingsButton.Location = new Point(10, 50);
            eegBandSettingsButton.Name = "eegBandSettingsButton";
            eegBandSettingsButton.Size = new Size(100, 30);
            eegBandSettingsButton.TabIndex = 1;
            eegBandSettingsButton.Text = "波種設定";
            eegBandSettingsButton.UseVisualStyleBackColor = true;
            // 
            // eegColorMapLabel
            // 
            eegColorMapLabel.AutoSize = true;
            eegColorMapLabel.Location = new Point(10, 10);
            eegColorMapLabel.Name = "eegColorMapLabel";
            eegColorMapLabel.Size = new Size(80, 18);
            eegColorMapLabel.TabIndex = 0;
            eegColorMapLabel.Text = "カラーマップ:";
            // 
            // eegColorMapCombo
            // 
            eegColorMapCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            eegColorMapCombo.FormattingEnabled = true;
            eegColorMapCombo.Items.AddRange(new object[] { "Viridis", "Jet", "Hot", "Cool", "Gray" });
            eegColorMapCombo.Location = new Point(100, 8);
            eegColorMapCombo.Name = "eegColorMapCombo";
            eegColorMapCombo.Size = new Size(100, 33);
            eegColorMapCombo.TabIndex = 1;
            // 
            // eegDbRangeLabel
            // 
            eegDbRangeLabel.AutoSize = true;
            eegDbRangeLabel.Location = new Point(210, 10);
            eegDbRangeLabel.Name = "eegDbRangeLabel";
            eegDbRangeLabel.Size = new Size(90, 18);
            eegDbRangeLabel.TabIndex = 2;
            eegDbRangeLabel.Text = "dB範囲:";
            // 
            // eegDbMinNumeric
            // 
            eegDbMinNumeric.Location = new Point(300, 8);
            eegDbMinNumeric.Maximum = new decimal(new int[] { 0, 0, 0, 0 });
            eegDbMinNumeric.Minimum = new decimal(new int[] { 200, 0, 0, int.MinValue });
            eegDbMinNumeric.Name = "eegDbMinNumeric";
            eegDbMinNumeric.Size = new Size(60, 31);
            eegDbMinNumeric.TabIndex = 3;
            eegDbMinNumeric.Value = new decimal(new int[] { 80, 0, 0, int.MinValue });
            // 
            // eegDbMaxNumeric
            // 
            eegDbMaxNumeric.Location = new Point(300, 35);
            eegDbMaxNumeric.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            eegDbMaxNumeric.Minimum = new decimal(new int[] { 100, 0, 0, int.MinValue });
            eegDbMaxNumeric.Name = "eegDbMaxNumeric";
            eegDbMaxNumeric.Size = new Size(60, 31);
            eegDbMaxNumeric.TabIndex = 4;
            eegDbMaxNumeric.Value = new decimal(new int[] { 20, 0, 0, int.MinValue });
            // 
            // heartSelectedBandsLabel
            // 
            heartSelectedBandsLabel.AutoSize = true;
            heartSelectedBandsLabel.Location = new Point(10, 20);
            heartSelectedBandsLabel.Name = "heartSelectedBandsLabel";
            heartSelectedBandsLabel.Size = new Size(120, 18);
            heartSelectedBandsLabel.TabIndex = 0;
            heartSelectedBandsLabel.Text = "解析成分：なし";
            // 
            // heartBandSettingsButton
            // 
            heartBandSettingsButton.Location = new Point(10, 50);
            heartBandSettingsButton.Name = "heartBandSettingsButton";
            heartBandSettingsButton.Size = new Size(100, 30);
            heartBandSettingsButton.TabIndex = 1;
            heartBandSettingsButton.Text = "成分設定";
            heartBandSettingsButton.UseVisualStyleBackColor = true;
            // 
            // heartBandPowerAnalysisButton
            // 
            heartBandPowerAnalysisButton.BackColor = Color.LightCyan;
            heartBandPowerAnalysisButton.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold);
            heartBandPowerAnalysisButton.Location = new Point(220, 50);
            heartBandPowerAnalysisButton.Name = "heartBandPowerAnalysisButton";
            heartBandPowerAnalysisButton.Size = new Size(130, 30);
            heartBandPowerAnalysisButton.TabIndex = 2;
            heartBandPowerAnalysisButton.Text = "バンドパワー解析";
            heartBandPowerAnalysisButton.UseVisualStyleBackColor = false;
            // 
            // heartColorMapLabel
            // 
            heartColorMapLabel.AutoSize = true;
            heartColorMapLabel.Location = new Point(10, 10);
            heartColorMapLabel.Name = "heartColorMapLabel";
            heartColorMapLabel.Size = new Size(80, 18);
            heartColorMapLabel.TabIndex = 0;
            heartColorMapLabel.Text = "カラーマップ:";
            // 
            // heartColorMapCombo
            // 
            heartColorMapCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            heartColorMapCombo.FormattingEnabled = true;
            heartColorMapCombo.Items.AddRange(new object[] { "Viridis", "Jet", "Hot", "Cool", "Gray" });
            heartColorMapCombo.Location = new Point(100, 8);
            heartColorMapCombo.Name = "heartColorMapCombo";
            heartColorMapCombo.Size = new Size(100, 33);
            heartColorMapCombo.TabIndex = 1;
            // 
            // heartDbRangeLabel
            // 
            heartDbRangeLabel.AutoSize = true;
            heartDbRangeLabel.Location = new Point(210, 10);
            heartDbRangeLabel.Name = "heartDbRangeLabel";
            heartDbRangeLabel.Size = new Size(90, 18);
            heartDbRangeLabel.TabIndex = 2;
            heartDbRangeLabel.Text = "dB範囲:";
            // 
            // heartDbMinNumeric
            // 
            heartDbMinNumeric.Location = new Point(300, 8);
            heartDbMinNumeric.Maximum = new decimal(new int[] { 0, 0, 0, 0 });
            heartDbMinNumeric.Minimum = new decimal(new int[] { 200, 0, 0, int.MinValue });
            heartDbMinNumeric.Name = "heartDbMinNumeric";
            heartDbMinNumeric.Size = new Size(60, 31);
            heartDbMinNumeric.TabIndex = 3;
            heartDbMinNumeric.Value = new decimal(new int[] { 80, 0, 0, int.MinValue });
            // 
            // heartDbMaxNumeric
            // 
            heartDbMaxNumeric.Location = new Point(300, 35);
            heartDbMaxNumeric.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            heartDbMaxNumeric.Minimum = new decimal(new int[] { 100, 0, 0, int.MinValue });
            heartDbMaxNumeric.Name = "heartDbMaxNumeric";
            heartDbMaxNumeric.Size = new Size(60, 31);
            heartDbMaxNumeric.TabIndex = 4;
            heartDbMaxNumeric.Value = new decimal(new int[] { 20, 0, 0, int.MinValue });
            // 
            // chartPanel
            // 
            chartPanel.BorderStyle = BorderStyle.FixedSingle;
            chartPanel.Controls.Add(exportButton);
            chartPanel.Controls.Add(chartPlotView);
            chartPanel.Dock = DockStyle.Fill;
            chartPanel.Location = new Point(0, 486);
            chartPanel.Margin = new Padding(3, 4, 3, 4);
            chartPanel.Name = "chartPanel";
            chartPanel.Size = new Size(1260, 883);
            chartPanel.TabIndex = 1;
            // 
            // exportButton
            // 
            exportButton.Anchor = AnchorStyles.Bottom;
            exportButton.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 128);
            exportButton.Location = new Point(550, 819);
            exportButton.Margin = new Padding(3, 4, 3, 4);
            exportButton.Name = "exportButton";
            exportButton.Size = new Size(160, 49);
            exportButton.TabIndex = 5;
            exportButton.Text = "グラフを画像出力";
            exportButton.UseVisualStyleBackColor = true;
            // 
            // chartPlotView
            // 
            chartPlotView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            chartPlotView.BackColor = Color.White;
            chartPlotView.Location = new Point(50, 14);
            chartPlotView.Margin = new Padding(3, 4, 3, 4);
            chartPlotView.Name = "chartPlotView";
            chartPlotView.PanCursor = Cursors.Hand;
            chartPlotView.Size = new Size(1193, 806);
            chartPlotView.TabIndex = 4;
            chartPlotView.ZoomHorizontalCursor = Cursors.SizeWE;
            chartPlotView.ZoomRectangleCursor = Cursors.SizeNWSE;
            chartPlotView.ZoomVerticalCursor = Cursors.SizeNS;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1260, 1369);
            Controls.Add(chartPanel);
            Controls.Add(topPanel);
            Margin = new Padding(3, 4, 3, 4);
            Name = "Form1";
            Text = "脳波・心拍解析アプリケーション - ホ号計画";
            topTableLayoutPanel.ResumeLayout(false);
            topPanel.ResumeLayout(false);
            outputPanel.ResumeLayout(false);
            outputPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)outputPreviewBox).EndInit();
            settingsPanel.ResumeLayout(false);
            commonSettingsPanel.ResumeLayout(false);
            settingsTableLayoutPanel.ResumeLayout(false);
            settingsTableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)divisionCountNumeric).EndInit();
            ((System.ComponentModel.ISupportInitialize)totalRangeNumeric).EndInit();
            ((System.ComponentModel.ISupportInitialize)timeMinNumeric).EndInit();
            ((System.ComponentModel.ISupportInitialize)timeMaxNumeric).EndInit();
            ((System.ComponentModel.ISupportInitialize)freqMinNumeric).EndInit();
            ((System.ComponentModel.ISupportInitialize)freqMaxNumeric).EndInit();
            ((System.ComponentModel.ISupportInitialize)timeResolutionNumeric).EndInit();
            bandPowerPanel.ResumeLayout(false);
            bandPowerPanel.PerformLayout();
            inputPanel.ResumeLayout(false);
            inputPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)inputPreviewBox).EndInit();
            ((System.ComponentModel.ISupportInitialize)eegDbMinNumeric).EndInit();
            ((System.ComponentModel.ISupportInitialize)eegDbMaxNumeric).EndInit();
            ((System.ComponentModel.ISupportInitialize)heartDbMinNumeric).EndInit();
            ((System.ComponentModel.ISupportInitialize)heartDbMaxNumeric).EndInit();
            chartPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.TableLayoutPanel topTableLayoutPanel;
        private System.Windows.Forms.Panel inputPanel;
        private System.Windows.Forms.Label inputFileLabel;
        private System.Windows.Forms.PictureBox inputPreviewBox;
        private System.Windows.Forms.Panel settingsPanel;
        private System.Windows.Forms.Button spectrogramExecuteButton;
        private System.Windows.Forms.Button eegBandPowerAnalysisButton;
        private System.Windows.Forms.Button eegBandPowerExportButton;

        // バンドパワー設定コントロール
        private System.Windows.Forms.Label eegSelectedBandsLabel;
        private System.Windows.Forms.Button eegBandSettingsButton;
        private System.Windows.Forms.Label heartSelectedBandsLabel;
        private System.Windows.Forms.Button heartBandSettingsButton;
        private System.Windows.Forms.Button heartBandPowerAnalysisButton;

        // スペクトログラム表示設定コントロール
        private System.Windows.Forms.Label eegColorMapLabel;
        private System.Windows.Forms.ComboBox eegColorMapCombo;
        private System.Windows.Forms.Label eegDbRangeLabel;
        private System.Windows.Forms.NumericUpDown eegDbMinNumeric;
        private System.Windows.Forms.NumericUpDown eegDbMaxNumeric;
        private System.Windows.Forms.Label heartColorMapLabel;
        private System.Windows.Forms.ComboBox heartColorMapCombo;
        private System.Windows.Forms.Label heartDbRangeLabel;
        private System.Windows.Forms.NumericUpDown heartDbMinNumeric;
        private System.Windows.Forms.NumericUpDown heartDbMaxNumeric;
        private System.Windows.Forms.Panel commonSettingsPanel;

        private System.Windows.Forms.Panel outputPanel;
        private System.Windows.Forms.Label outputFileLabel;
        private System.Windows.Forms.PictureBox outputPreviewBox;
        private System.Windows.Forms.Panel chartPanel;
        private OxyPlot.WindowsForms.PlotView chartPlotView;
        private System.Windows.Forms.Button exportButton;
        private System.Windows.Forms.Button csvExportButton;
        private System.Windows.Forms.Button eegModeButton;
        private System.Windows.Forms.Button cbfModeButton;
        private System.Windows.Forms.Button bandPowerAnalysisButton;
        private System.Windows.Forms.Button bandPowerExportButton;

        // バンドパワー専用パネル
        private System.Windows.Forms.Panel bandPowerPanel;
        private System.Windows.Forms.Button bandPowerBackButton;
        private System.Windows.Forms.Button bandPowerGenerateButton;
        private System.Windows.Forms.CheckBox eegDeltaCheckBox;
        private System.Windows.Forms.CheckBox eegThetaCheckBox;
        private System.Windows.Forms.CheckBox eegAlphaCheckBox;
        private System.Windows.Forms.CheckBox eegBetaCheckBox;
        private System.Windows.Forms.CheckBox eegGammaCheckBox;
        private System.Windows.Forms.CheckBox heartLFCheckBox;
        private System.Windows.Forms.CheckBox heartHFCheckBox;
        private System.Windows.Forms.CheckBox heartLfHfRatioCheckBox;
        private System.Windows.Forms.Label bandSelectionLabel;
        private System.Windows.Forms.Button bandPowerSelectButton;

        // 共通設定コントロール
        private System.Windows.Forms.ComboBox windowFunctionCombo;
        private System.Windows.Forms.CheckBox interpolationCheckBox;
        private System.Windows.Forms.CheckBox dcRemovalCheckBox;
        private System.Windows.Forms.CheckBox logScaleCheckBox;
        private System.Windows.Forms.Label divisionCountLabel;
        private System.Windows.Forms.NumericUpDown divisionCountNumeric;
        private System.Windows.Forms.Label totalRangeLabel;
        private System.Windows.Forms.NumericUpDown totalRangeNumeric;
        private System.Windows.Forms.Label timeResolutionLabel;
        private System.Windows.Forms.NumericUpDown timeResolutionNumeric;
        private System.Windows.Forms.Label windowFunctionLabel;
        private System.Windows.Forms.Label timeRangeLabel;
        private System.Windows.Forms.NumericUpDown timeMinNumeric;
        private System.Windows.Forms.NumericUpDown timeMaxNumeric;
        private System.Windows.Forms.Label freqRangeLabel;
        private System.Windows.Forms.NumericUpDown freqMinNumeric;
        private System.Windows.Forms.NumericUpDown freqMaxNumeric;
        private System.Windows.Forms.Button modeToggleButton;
        private TableLayoutPanel settingsTableLayoutPanel;
        private ProgressBar spectrogramProgressBar;

        // 心拍用ボタン（後方互換）
        private System.Windows.Forms.Button heartHfButton;
        private System.Windows.Forms.Button heartLfButton;
    }
}
