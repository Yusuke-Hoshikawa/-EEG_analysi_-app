using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ホ号計画.ViewModels;
using ホ号計画.Models;
using ホ号計画.Services;

namespace ホ号計画
{
    public partial class Form1 : Form
    {
        private MainViewModel _viewModel;
        private ModeSelectionForm.AnalysisMode _currentMode;
        private BandSelectionForm.BandSettings _eegBandSettings = new BandSelectionForm.BandSettings();
        private BandSelectionForm.BandSettings _heartBandSettings = new BandSelectionForm.BandSettings();
        private BandPowerAnalysisResult _lastBandPowerResult; // バンドパワー解析結果を保存
        private bool isChickMode = true; // デフォルトはひよこモード

        // 時間区間設定用コントロール
        private Button setIntervalsButton;
        private NumericUpDown restStartNumeric;
        private NumericUpDown restEndNumeric;
        private Label restIntervalLabel;

        // 現在のモード用コントロール参照（後方互換性のため）
        private ComboBox currentWindowFunctionCombo;
        private NumericUpDown currentWindowDurationNumeric;
        private NumericUpDown currentResolutionNumeric;
        private NumericUpDown currentTimeMinNumeric;
        private NumericUpDown currentTimeMaxNumeric;
        private NumericUpDown currentFreqMinNumeric;
        private NumericUpDown currentFreqMaxNumeric;
        private Button currentExecuteButton;
        private Label currentWindowFunctionLabel;
        private Label currentResolutionLabel;
        private Label currentTimeRangeLabel;
        private Label currentFreqRangeLabel;
        private ProgressBar currentProgressBar;

        // UI更新フラグ（無限ループ防止用）
        private bool _isUpdatingFromUI = false;
        private bool _isLoadingSettings = false;

        public Form1() : this(ModeSelectionForm.AnalysisMode.EEG)
        {
        }

        public Form1(ModeSelectionForm.AnalysisMode mode)
        {
            _currentMode = mode;
            InitializeComponent();

            // bandPowerPanelの追加設定（デザイナー再生成に影響されないように）
            bandPowerPanel.AutoScroll = true;
            bandPowerPanel.AutoScrollMinSize = new System.Drawing.Size(0, 400);

            InitializeViewModel();
            InitializeUI();
            UpdateUIForMode();
        }

        private void InitializeViewModel()
        {
            _viewModel = new MainViewModel();
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            System.Diagnostics.Debug.WriteLine($"[EditFlag] ViewModel作成直後のフラグ: MaxTime={_viewModel.AnalysisSettings.IsMaxTimeManuallyEdited}");

            // 初期モードに応じた適切な設定を適用
            bool isEegMode = _currentMode == ModeSelectionForm.AnalysisMode.EEG;
            _viewModel.AnalysisSettings.SetModeDefaults(isEegMode);
            _viewModel.IsEegMode = isEegMode; // ViewModelのモード情報も設定
            System.Diagnostics.Debug.WriteLine($"初期モード設定適用: {(isEegMode ? "脳波" : "心拍")}モード");
            System.Diagnostics.Debug.WriteLine($"[EditFlag] SetModeDefaults後のフラグ: MaxTime={_viewModel.AnalysisSettings.IsMaxTimeManuallyEdited}");

            // 加算平均パラメータコントロールを初期状態で非表示に設定
            InitializeEnsembleAveragingControls();

            // PlotViewにViewModelのPlotModelをバインド
            chartPlotView.Model = _viewModel.SpectrogramPlot;

            // ViewModelの設定をUIに反映
            System.Diagnostics.Debug.WriteLine($"[EditFlag] LoadSettingsFromViewModel開始前のフラグ: MaxTime={_viewModel.AnalysisSettings.IsMaxTimeManuallyEdited}");
            LoadSettingsFromViewModel();
            System.Diagnostics.Debug.WriteLine($"[EditFlag] LoadSettingsFromViewModel完了後のフラグ: MaxTime={_viewModel.AnalysisSettings.IsMaxTimeManuallyEdited}");
        }

        private void LoadSettingsFromViewModel()
        {
            if (_viewModel?.AnalysisSettings != null)
            {
                // 共通コントロールの設定
                LoadSettingsToControls(
                    windowFunctionCombo,
                    timeMinNumeric, timeMaxNumeric, freqMinNumeric, freqMaxNumeric);

                // 心拍モードの場合は周波数範囲を調整
                if (_currentMode == ModeSelectionForm.AnalysisMode.Heart)
                {
                    SetFreqNumericFromHz(freqMaxNumeric, 20.0); // 心拍解析は通常20Hzまで
                }

                // 補間設定をUIに反映
                if (interpolationCheckBox != null)
                    interpolationCheckBox.Checked = _viewModel.AnalysisSettings.EnableInterpolation;

                // DC成分除去設定をUIに反映
                if (dcRemovalCheckBox != null)
                    dcRemovalCheckBox.Checked = _viewModel.AnalysisSettings.EnableDcRemoval;

                // 対数スケール設定をUIに反映
                if (logScaleCheckBox != null)
                    logScaleCheckBox.Checked = _viewModel.IsLogScale;
            }
        }

        private void LoadSettingsToControls(ComboBox windowCombo, // NumericUpDown windowDurationNum, // REMOVED: DFT時間幅廃止
            NumericUpDown timeMinNum, NumericUpDown timeMaxNum, NumericUpDown freqMinNum, NumericUpDown freqMaxNum)
        {
            System.Diagnostics.Debug.WriteLine($"[EditFlag] LoadSettingsToControls開始: {timeMaxNum.Name}");

            // 設定読み込み中フラグを設定
            _isLoadingSettings = true;

            // 編集フラグを一時的に保存
            bool savedMinTimeEdited = _viewModel.AnalysisSettings.IsMinTimeManuallyEdited;
            bool savedMaxTimeEdited = _viewModel.AnalysisSettings.IsMaxTimeManuallyEdited;
            bool savedMinFreqEdited = _viewModel.AnalysisSettings.IsDisplayMinFreqManuallyEdited;
            bool savedMaxFreqEdited = _viewModel.AnalysisSettings.IsDisplayMaxFreqManuallyEdited;

            System.Diagnostics.Debug.WriteLine($"[EditFlag] 編集フラグ保存: MinTime={savedMinTimeEdited}, MaxTime={savedMaxTimeEdited}");

            // イベントハンドラーを一時的に無効化して無限ループを防止
            timeMinNum.ValueChanged -= AxisRange_ValueChanged;
            timeMaxNum.ValueChanged -= AxisRange_ValueChanged;
            freqMinNum.ValueChanged -= AxisRange_ValueChanged;
            freqMaxNum.ValueChanged -= AxisRange_ValueChanged;
            // windowDurationNum.ValueChanged -= OnSettingsChanged; // REMOVED: DFT時間幅廃止

            try
            {
                // 窓関数設定
                string windowFuncName = _viewModel.AnalysisSettings.WindowFunction.ToString();
                for (int i = 0; i < windowCombo.Items.Count; i++)
                {
                    if (windowCombo.Items[i].ToString() == windowFuncName)
                    {
                        windowCombo.SelectedIndex = i;
                        break;
                    }
                }

                // DFT時間幅設定は廃止（BasicTimeUnitを使用）
                // windowDurationNum.Value = (decimal)(_viewModel.AnalysisSettings.WindowDuration * 1000); // 秒をミリ秒に変換してUI表示

                // 時間解像度設定
                //resolutionNum.Value = (decimal)(_viewModel.AnalysisSettings.TimeResolution * 1000); // 秒をミリ秒に変換してUI表示

                // 軸範囲設定
                timeMinNum.Value = (decimal)_viewModel.AnalysisSettings.MinTime;
                timeMaxNum.Value = (decimal)_viewModel.AnalysisSettings.MaxTime;
                // 対数スケール時は指数表示に変換
                if (_viewModel.IsLogScale)
                {
                    double minHz = Math.Max(_viewModel.AnalysisSettings.DisplayMinFreq, 0.001);
                    double maxHz = Math.Max(_viewModel.AnalysisSettings.DisplayMaxFreq, 0.001);
                    freqMinNum.Value = (decimal)Math.Round(Math.Log10(minHz), 1);
                    freqMaxNum.Value = (decimal)Math.Round(Math.Log10(maxHz), 1);
                }
                else
                {
                    freqMinNum.Value = (decimal)_viewModel.AnalysisSettings.DisplayMinFreq;
                    freqMaxNum.Value = (decimal)_viewModel.AnalysisSettings.DisplayMaxFreq;
                }

                // デバッグ出力
                System.Diagnostics.Debug.WriteLine($"UI更新: 時間軸={timeMinNum.Value}-{timeMaxNum.Value}, 周波数軸={freqMinNum.Value}-{freqMaxNum.Value} (LogScale={_viewModel.IsLogScale})");
            }
            finally
            {
                // 編集フラグを復元
                _viewModel.AnalysisSettings.IsMinTimeManuallyEdited = savedMinTimeEdited;
                _viewModel.AnalysisSettings.IsMaxTimeManuallyEdited = savedMaxTimeEdited;
                _viewModel.AnalysisSettings.IsDisplayMinFreqManuallyEdited = savedMinFreqEdited;
                _viewModel.AnalysisSettings.IsDisplayMaxFreqManuallyEdited = savedMaxFreqEdited;

                // イベントハンドラーを再有効化
                timeMinNum.ValueChanged += AxisRange_ValueChanged;
                timeMaxNum.ValueChanged += AxisRange_ValueChanged;
                freqMinNum.ValueChanged += AxisRange_ValueChanged;
                freqMaxNum.ValueChanged += AxisRange_ValueChanged;
                //resolutionNum.ValueChanged += OnSettingsChanged;
                // windowDurationNum.ValueChanged += OnSettingsChanged; // REMOVED: DFT時間幅廃止

                // 設定読み込み完了フラグをリセット
                _isLoadingSettings = false;

                System.Diagnostics.Debug.WriteLine($"[EditFlag] 編集フラグ復元完了: MinTime={savedMinTimeEdited}, MaxTime={savedMaxTimeEdited}, MinFreq={savedMinFreqEdited}, MaxFreq={savedMaxFreqEdited}");
            }
        }

        private void InitializeUI()
        {
            this.AllowDrop = true;
            inputPreviewBox.AllowDrop = true;

            // ComboBoxの初期選択を設定（アイテムが存在する場合のみ）
            if (windowFunctionCombo.Items.Count > 1)
            {
                try
                {
                    windowFunctionCombo.SelectedIndex = Math.Min(1, windowFunctionCombo.Items.Count - 1);
                }
                catch { windowFunctionCombo.SelectedIndex = 0; }
            }

            inputPreviewBox.Paint += InputPreviewBox_Paint;
            outputPreviewBox.Paint += OutputPreviewBox_Paint;
            // PlotViewはPaintイベントが不要

            // 共通コントロールのイベントハンドラ
            spectrogramExecuteButton.Click += ExecuteButton_Click;
            timeMinNumeric.ValueChanged += AxisRange_ValueChanged;
            timeMaxNumeric.ValueChanged += AxisRange_ValueChanged;
            freqMinNumeric.ValueChanged += AxisRange_ValueChanged;
            freqMaxNumeric.ValueChanged += AxisRange_ValueChanged;
            windowFunctionCombo.SelectedIndexChanged += OnSettingsChanged;
            eegBandPowerAnalysisButton.Click += BandPowerAnalysisButton_Click;
            eegBandPowerExportButton.Click += BandPowerExportButton_Click;
            if (interpolationCheckBox != null)
                interpolationCheckBox.CheckedChanged += InterpolationCheckBox_CheckedChanged;

            if (dcRemovalCheckBox != null)
                dcRemovalCheckBox.CheckedChanged += DcRemovalCheckBox_CheckedChanged;

            if (logScaleCheckBox != null)
                logScaleCheckBox.CheckedChanged += LogScaleCheckBox_CheckedChanged;

            // 加算平均コントロールのイベントハンドラー
            divisionCountNumeric.ValueChanged += EnsembleAveragingParameter_ValueChanged;
            totalRangeNumeric.ValueChanged += EnsembleAveragingParameter_ValueChanged;

            // 時間移動幅（TimeResolution）のUIは一時的に非表示
            timeResolutionLabel.Visible = false;
            timeResolutionNumeric.Visible = false;

            // バンドパワーチェックボックスのイベントハンドラ
            eegDeltaCheckBox.CheckedChanged += BandCheckBox_CheckedChanged;
            eegThetaCheckBox.CheckedChanged += BandCheckBox_CheckedChanged;
            eegAlphaCheckBox.CheckedChanged += BandCheckBox_CheckedChanged;
            eegBetaCheckBox.CheckedChanged += BandCheckBox_CheckedChanged;
            eegGammaCheckBox.CheckedChanged += BandCheckBox_CheckedChanged;
            heartLFCheckBox.CheckedChanged += BandCheckBox_CheckedChanged;
            heartHFCheckBox.CheckedChanged += BandCheckBox_CheckedChanged;
            heartLfHfRatioCheckBox.CheckedChanged += BandCheckBox_CheckedChanged;

            // 共通コントロールのイベントハンドラ
            exportButton.Click += ExportButton_Click;
            csvExportButton.Click += CsvExportButton_Click;
            eegModeButton.Click += EegModeButton_Click;
            cbfModeButton.Click += CbfModeButton_Click;

            inputPreviewBox.DragEnter += InputPreviewBox_DragEnter;
            inputPreviewBox.DragDrop += InputPreviewBox_DragDrop;


            // バンド設定ボタンのイベントハンドラ
            eegBandSettingsButton.Click += EegBandSettingsButton_Click;
            heartBandSettingsButton.Click += HeartBandSettingsButton_Click;

            // バンドパワー解析ボタンのイベントハンドラ
            heartBandPowerAnalysisButton.Click += BandPowerAnalysisButton_Click;

            // バンドパワーボタンのイベントハンドラ
            bandPowerSelectButton.Click += BandPowerSelectButton_Click;
            bandPowerBackButton.Click += BandPowerBackButton_Click;
            bandPowerGenerateButton.Click += BandPowerGenerateButton_Click;

            this.Resize += Form1_Resize;
            Form1_Resize(this, EventArgs.Empty);

            // ひよこ/人間モード切り替えの初期化
            UpdateModeToggleButton();
            UpdateUIForUserMode();

            // 初期表示更新
            UpdateSelectedBandsDisplay();
        }

        private void UpdateUIForMode()
        {
            // ウィンドウタイトルを更新
            string modeText = _currentMode == ModeSelectionForm.AnalysisMode.EEG ? "脳波" : "心拍";
            this.Text = $"{modeText}解析アプリケーション - ホ号計画";

            // パネルの表示切替
            // 共通設定パネルは常に表示
            settingsPanel.Visible = true;

            if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
            {
                // モードボタンの表示を更新
                eegModeButton.BackColor = Color.LightGreen;
                eegModeButton.Text = "脳波モード";
                cbfModeButton.BackColor = SystemColors.Control;
                cbfModeButton.Text = "心拍モード";
            }
            else
            {
                // モードボタンの表示を更新
                eegModeButton.BackColor = SystemColors.Control;
                eegModeButton.Text = "脳波モード";
                cbfModeButton.BackColor = Color.LightCoral;
                cbfModeButton.Text = "心拍モード";
            }

            // 後方互換性のため、現在使用中のコントロール参照を更新
            currentWindowFunctionCombo = windowFunctionCombo;
            currentExecuteButton = spectrogramExecuteButton;
            currentWindowFunctionLabel = windowFunctionLabel;
            currentTimeRangeLabel = timeRangeLabel;
            currentTimeMinNumeric = timeMinNumeric;
            currentTimeMaxNumeric = timeMaxNumeric;
            currentFreqRangeLabel = freqRangeLabel;
            currentFreqMinNumeric = freqMinNumeric;
            currentFreqMaxNumeric = freqMaxNumeric;
            currentProgressBar = spectrogramProgressBar;

            // 後方互換性のComboBoxに初期選択を設定
            if (windowFunctionCombo != null && windowFunctionCombo.Items.Count > 1 && windowFunctionCombo.SelectedIndex == -1)
            {
                try
                {
                    windowFunctionCombo.SelectedIndex = Math.Min(1, windowFunctionCombo.Items.Count - 1);
                }
                catch { windowFunctionCombo.SelectedIndex = 0; }
            }

            // ViewModelのIsBusy変更を監視
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // ひよこ/人間モード切り替えボタンの表示制御（心拍モードのみ）
            modeToggleButton.Visible = (_currentMode == ModeSelectionForm.AnalysisMode.Heart);

            // 心拍モード時は脳波/心拍切り替えボタンを非表示
            bool showModeButtons = (_currentMode == ModeSelectionForm.AnalysisMode.EEG);
            eegModeButton.Visible = showModeButtons;
            cbfModeButton.Visible = showModeButtons;

            // バンドパワー解析関連のイベント追加
            bandPowerAnalysisButton.Click += BandPowerAnalysisButton_Click;
            bandPowerExportButton.Click += BandPowerExportButton_Click;
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            // UI変更時に即座にViewModelに反映（無限ループ防止フラグ設定）
            _isUpdatingFromUI = true;
            UpdateAnalysisSettingsFromUI();
            _isUpdatingFromUI = false;
        }

        /// <summary>
        /// 加算平均パラメータの値変更イベントハンドラー
        /// </summary>
        private void EnsembleAveragingParameter_ValueChanged(object sender, EventArgs e)
        {
            if (_viewModel != null && !_isLoadingSettings)
            {
                int divisionCount = (int)divisionCountNumeric.Value;          // 分割数
                double totalRange = (double)totalRangeNumeric.Value;    // 加算平均の総時間幅

                if (divisionCount <= 0) return;

                double dftTimeWidth = totalRange / divisionCount;

                // 既存パラメータに流し込むだけ
                _viewModel.AnalysisSettings.DivisionCount = divisionCount;
                _viewModel.AnalysisSettings.BasicTimeUnit = dftTimeWidth;

                // TimeResolution UIは非表示のため、ここでの制約更新は不要

                // PreTrigger/PostTriggerは加算平均パラメータとは別の概念なので、ここでは設定しない
                // （デフォルト値: PreTrigger=0.1秒、PostTrigger=0.5秒が使われる）

                System.Diagnostics.Debug.WriteLine(
                    $"加算平均更新: 総範囲={totalRange:F3}s, 分割数={divisionCount}, DFT幅={dftTimeWidth:F3}s"
                );
            }
        }

        /// <summary>
        /// 時間移動幅（TimeResolution）の値変更イベントハンドラー
        /// </summary>
        private void TimeResolution_ValueChanged(object sender, EventArgs e)
        {
            if (_viewModel != null && !_isLoadingSettings)
            {
                double timeResolution = (double)timeResolutionNumeric.Value;

                // 0以下は許可しない（ゼロ除算防止）
                if (timeResolution <= 0) return;

                _viewModel.AnalysisSettings.TimeResolution = timeResolution;

                System.Diagnostics.Debug.WriteLine(
                    $"時間移動幅更新: TimeResolution={timeResolution:F3}s"
                );
            }
        }

        /// <summary>
        /// 加算平均コントロールの初期状態を設定
        /// </summary>
        private void InitializeEnsembleAveragingControls()
        {
            // 新仕様：DFT時間幅と分割数を直接設定
            if (_viewModel?.AnalysisSettings != null)
            {
                divisionCountNumeric.Value = (decimal)_viewModel.AnalysisSettings.DivisionCount;     // 分割数
                totalRangeNumeric.Value = (decimal)_viewModel.AnalysisSettings.BasicTimeUnit;   // DFT時間幅

                // 初期化時にもBasicTimeUnitを計算して設定（イベントハンドラーと同じ計算）
                double totalRange = (double)totalRangeNumeric.Value;
                int divisionCount = (int)divisionCountNumeric.Value;
                double basicTimeUnit = totalRange / divisionCount;
                _viewModel.AnalysisSettings.BasicTimeUnit = basicTimeUnit;

                // TimeResolution UIは非表示のため、初期化不要
            }
        }


        private void AxisRange_ValueChanged(object sender, EventArgs e)
        {
            if (_viewModel != null)
            {
                // プログラムによる更新の場合は編集フラグを設定しない
                if (!_isUpdatingFromUI && !_isLoadingSettings)
                {
                    // ユーザーによる手動編集の場合、編集フラグを設定
                    NumericUpDown changedControl = sender as NumericUpDown;
                    if (changedControl != null)
                    {
                        if (changedControl == timeMinNumeric)
                        {
                            _viewModel.AnalysisSettings.IsMinTimeManuallyEdited = true;
                        }
                        else if (changedControl == timeMaxNumeric)
                        {
                            _viewModel.AnalysisSettings.IsMaxTimeManuallyEdited = true;
                        }
                        else if (changedControl == freqMinNumeric)
                        {
                            _viewModel.AnalysisSettings.IsDisplayMinFreqManuallyEdited = true;
                        }
                        else if (changedControl == freqMaxNumeric)
                        {
                            _viewModel.AnalysisSettings.IsDisplayMaxFreqManuallyEdited = true;
                        }
                    }
                }

                // UI変更時の無限ループ防止フラグ設定
                _isUpdatingFromUI = true;

                // 共通コントロールから値を取得
                double timeMin = (double)timeMinNumeric.Value;
                double timeMax = (double)timeMaxNumeric.Value;
                double freqMin, freqMax;
                if (_viewModel.IsLogScale)
                {
                    // 指数モード: 10^n → Hz に変換
                    freqMin = Math.Pow(10, (double)freqMinNumeric.Value);
                    freqMax = Math.Pow(10, (double)freqMaxNumeric.Value);
                }
                else
                {
                    freqMin = (double)freqMinNumeric.Value;
                    freqMax = (double)freqMaxNumeric.Value;
                }

                _viewModel.UpdateAxisRangesFromUI(timeMin, timeMax, freqMin, freqMax);

                _isUpdatingFromUI = false;

                // chartPlotViewを安全に更新
                if (chartPlotView != null && chartPlotView.Model != null)
                {
                    try
                    {
                        chartPlotView.Model = _viewModel.SpectrogramPlot;
                        chartPlotView.InvalidatePlot(true);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"チャート更新エラー: {ex.Message}");
                        // エラー時はグラフ更新をスキップ
                    }
                }
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.StatusMessage):
                    UpdateStatusDisplay();
                    break;
                case nameof(MainViewModel.IsBusy):
                    UpdateBusyState();
                    break;
                case nameof(MainViewModel.ProgressValue):
                    UpdateProgressValue();
                    break;
                case nameof(MainViewModel.CurrentSignal):
                    UpdateSignalDisplay();
                    break;
                case nameof(MainViewModel.SpectrogramData):
                    UpdateSpectrogramDisplay();
                    break;
                case nameof(MainViewModel.FilterAnalysisResult):
                    UpdateOutputPreview();
                    UpdateSpectrogramDisplay(); // フィルタ結果変更時にチャート更新
                    break;
                case nameof(MainViewModel.FilterPlot):
                    UpdateSpectrogramDisplay(); // FilterPlot変更時にチャート更新
                    break;
                case nameof(MainViewModel.AnalysisSettings):
                    // UI変更による設定更新の場合は反映しない（無限ループ防止）
                    if (!_isUpdatingFromUI)
                    {
                        // データ読み込み時の自動設定値をUIに反映
                        LoadSettingsFromViewModel();
                    }
                    break;
            }
        }

        private void UpdateStatusDisplay()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateStatusDisplay));
                return;
            }

            outputFileLabel.Text = _viewModel.StatusMessage ?? "出力プレビュー";
        }

        private void UpdateBusyState()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateBusyState));
                return;
            }

            currentExecuteButton.Enabled = !_viewModel.IsBusy;
            //hfButton.Enabled = !_viewModel.IsBusy;
            //lfButton.Enabled = !_viewModel.IsBusy;

            // 進捗バーの制御
            if (_viewModel.IsBusy)
            {
                currentProgressBar.Visible = true;
                currentProgressBar.Style = ProgressBarStyle.Continuous;
                currentProgressBar.Minimum = 0;
                currentProgressBar.Maximum = 100;
            }
            else
            {
                currentProgressBar.Visible = false;
                currentProgressBar.Value = 0;
            }
        }

        private void UpdateSignalDisplay()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateSignalDisplay));
                return;
            }

            if (_viewModel.CurrentSignal != null)
            {
                inputFileLabel.Text = $"読み込み: {_viewModel.CurrentSignal.FileName}";
                // 入力プレビューボックスを再描画して波形を表示
                inputPreviewBox.Invalidate();
            }
            else
            {
                inputFileLabel.Text = "ファイルをドロップ";
                inputPreviewBox.Invalidate();
            }
        }

        private void UpdateSpectrogramDisplay()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateSpectrogramDisplay));
                return;
            }

            // メインチャートは常にSpectrogramPlot（ヒートマップ）を表示
            // フィルタ処理後もスペクトログラムが更新されるため、常にSpectrogramPlotを使用
            try
            {
                chartPlotView.Model = _viewModel.SpectrogramPlot;
                chartPlotView.InvalidatePlot(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"スペクトログラム表示エラー: {ex.Message}");
                // エラー時は表示更新をスキップ
            }
        }

        private void UpdateOutputPreview()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateOutputPreview));
                return;
            }

            outputPreviewBox.Invalidate();
        }

        private void UpdateAnalysisSettingsFromUI()
        {
            if (_viewModel?.AnalysisSettings != null)
            {
                // 窓関数を文字列からenumに変換
                if (Enum.TryParse<Models.WindowFunction>(currentWindowFunctionCombo.Text, out var windowFunc))
                {
                    _viewModel.AnalysisSettings.WindowFunction = windowFunc;
                }

                // currentResolutionNumericが存在する場合のみ設定（心拍モードでは存在しない）
                if (currentResolutionNumeric != null)
                {
                    double timeResolution = (double)currentResolutionNumeric.Value;
                    _viewModel.AnalysisSettings.TimeResolution = timeResolution;

                    System.Diagnostics.Debug.WriteLine($"UI更新: 時間解像度={_viewModel.AnalysisSettings.TimeResolution:F6}s");
                }
            }
        }

        private void InputPreviewBox_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = inputPreviewBox.ClientRectangle;

            if (_viewModel?.CurrentSignal?.TimeData != null && _viewModel.CurrentSignal.TimeData.Length > 0)
            {
                // 波形描画
                DrawWaveform(g, rect, _viewModel.CurrentSignal.TimeData, Color.Blue);
            }
            else
            {
                // プレースホルダー表示
                using (var brush = new SolidBrush(Color.Gray))
                {
                    using (var font = new Font("Arial", 24))
                    {
                        string text = "Preview\n\nDrop CSV file here";
                        var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(text, font, brush, rect, format);
                    }
                }
            }
        }

        private void DrawWaveform(Graphics g, Rectangle rect, double[] data, Color color)
        {
            if (data == null || data.Length < 2)
                return;

            using (var pen = new Pen(color, 1))
            {
                int margin = 10;
                int plotWidth = rect.Width - 2 * margin;
                int plotHeight = rect.Height - 2 * margin;

                double globalMin = data.Min();
                double globalMax = data.Max();
                double range = globalMax - globalMin;
                if (range == 0) range = 1;

                // 1pxあたりに何サンプル入るか
                double samplesPerPixel = (double)data.Length / plotWidth;

                for (int px = 0; px < plotWidth; px++)
                {
                    int startIndex = (int)(px * samplesPerPixel);
                    int endIndex = (int)((px + 1) * samplesPerPixel);
                    if (endIndex >= data.Length) endIndex = data.Length - 1;

                    double localMin = double.MaxValue;
                    double localMax = double.MinValue;

                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        double val = data[i];
                        if (val < localMin) localMin = val;
                        if (val > localMax) localMax = val;
                    }

                    float x = margin + px;

                    // 上側（max）
                    float normMax = (float)((localMax - globalMin) / range);
                    float yMax = margin + plotHeight * (1f - normMax);

                    // 下側（min）
                    float normMin = (float)((localMin - globalMin) / range);
                    float yMin = margin + plotHeight * (1f - normMin);

                    g.DrawLine(pen, x, yMin, x, yMax);
                }

                // 情報表示
                var brush = new SolidBrush(Color.Black);
                var font = new Font("Arial", 8);
                string info = $"Samples: {data.Length}\nMax: {globalMax:F3}\nMin: {globalMin:F3}";
                g.DrawString(info, font, brush, new PointF(5, 5));
            }
        }


        private void OutputPreviewBox_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = outputPreviewBox.ClientRectangle;

            if (_viewModel?.SpectrogramData != null)
            {
                // スペクトログラム結果表示
                DrawSpectrogramResults(g, rect, _viewModel.SpectrogramData);
            }
        }

        private void DrawFilterResults(Graphics g, Rectangle rect, FilterAnalysisResult result)
        {
            using (var brush = new SolidBrush(Color.Black))
            {
                using (var font = new Font("Arial", 10))
                {
                    int y = 10;
                    int lineHeight = 15;

                    g.DrawString("フィルタ解析結果", new Font("Arial", 12, FontStyle.Bold), brush, new PointF(10, y));
                    y += lineHeight * 2;

                    if (!string.IsNullOrEmpty(result.FilterDescription))
                    {
                        g.DrawString($"フィルタ: {result.FilterDescription}", font, brush, new PointF(10, y));
                        y += lineHeight;
                    }

                    g.DrawString($"解析時刻: {result.AnalysisTime:yyyy/MM/dd HH:mm:ss}", font, brush, new PointF(10, y));
                    y += lineHeight * 2;

                    if (result.LFPowerRatio > 0)
                    {
                        g.DrawString($"LF パワー比: {result.LFPowerRatio:F6}", font, brush, new PointF(10, y));
                        y += lineHeight;
                    }

                    if (result.HFPowerRatio > 0)
                    {
                        g.DrawString($"HF パワー比: {result.HFPowerRatio:F6}", font, brush, new PointF(10, y));
                        y += lineHeight;
                    }

                    if (result.HFLFRatio > 0)
                    {
                        g.DrawString($"HF/LF 比: {result.HFLFRatio:F3}", font, brush, new PointF(10, y));
                        y += lineHeight * 2;
                    }

                    // フィルタ後データの波形プレビュー
                    if (result.LFFilteredData != null && result.LFFilteredData.Length > 0)
                    {
                        var previewRect = new Rectangle(10, y, rect.Width - 20, Math.Min(80, rect.Bottom - y - 10));
                        DrawMiniWaveform(g, previewRect, result.LFFilteredData, Color.Green, "LF フィルタ結果");
                    }
                    else if (result.HFFilteredData != null && result.HFFilteredData.Length > 0)
                    {
                        var previewRect = new Rectangle(10, y, rect.Width - 20, Math.Min(80, rect.Bottom - y - 10));
                        DrawMiniWaveform(g, previewRect, result.HFFilteredData, Color.Red, "HF フィルタ結果");
                    }
                }
            }
        }

        private void DrawSpectrogramResults(Graphics g, Rectangle rect, SpectrogramData spectrogram)
        {
            using (var brush = new SolidBrush(Color.Black))
            {
                using (var font = new Font("Arial", 10))
                {
                    int y = 10;
                    int lineHeight = 15;

                    g.DrawString("📈 スペクトログラム解析結果", new Font("Arial", 12, FontStyle.Bold), brush, new PointF(10, y));
                    y += lineHeight * 2;

                    double timeRes = spectrogram.TimeAxis.Length > 1
                        ? spectrogram.TimeAxis[1] - spectrogram.TimeAxis[0] : 0;
                    g.DrawString($"時間分解能: {timeRes:F3} 秒", font, brush, new PointF(10, y));
                    y += lineHeight;

                    double freqRes = spectrogram.FrequencyAxis.Length > 1
                        ? spectrogram.FrequencyAxis[1] - spectrogram.FrequencyAxis[0] : 0;
                    g.DrawString($"周波数分解能: {freqRes:F1} Hz", font, brush, new PointF(10, y));
                    y += lineHeight;

                    if (spectrogram.TimeAxis != null && spectrogram.TimeAxis.Length > 0)
                    {
                        g.DrawString($"時間範囲: {spectrogram.TimeAxis[0]:F3} - {spectrogram.TimeAxis[spectrogram.TimeSteps - 1]:F3} 秒", font, brush, new PointF(10, y));
                        y += lineHeight;
                    }

                    if (spectrogram.FrequencyAxis != null && spectrogram.FrequencyAxis.Length > 0)
                    {
                        g.DrawString($"周波数範囲: {spectrogram.FrequencyAxis[0]:F1} - {spectrogram.FrequencyAxis[spectrogram.FrequencyBins - 1]:F1} Hz", font, brush, new PointF(10, y));
                        y += lineHeight;
                    }

                    g.DrawString($"パワー範囲: {spectrogram.MinPower:F1} - {spectrogram.MaxPower:F1} dB", font, brush, new PointF(10, y));
                }
            }
        }

        private void DrawMiniWaveform(Graphics g, Rectangle rect, double[] data, Color color, string title)
        {
            if (data == null || data.Length < 2 || rect.Height < 20)
                return;

            // タイトル描画
            using (var titleBrush = new SolidBrush(Color.Black))
            {
                using (var titleFont = new Font("Arial", 8))
                {
                    g.DrawString(title, titleFont, titleBrush, new PointF(rect.X, rect.Y));
                }
            }

            // 波形描画エリア
            var waveRect = new Rectangle(rect.X, rect.Y + 15, rect.Width, rect.Height - 15);

            using (var pen = new Pen(color, 1))
            {
                // データのサンプリング
                int maxPoints = Math.Min(waveRect.Width / 2, 200);
                int step = Math.Max(1, data.Length / maxPoints);

                double minValue = data.Min();
                double maxValue = data.Max();
                double range = maxValue - minValue;
                if (range == 0) range = 1.0;

                var points = new List<PointF>();
                for (int i = 0; i < data.Length; i += step)
                {
                    if (points.Count >= maxPoints) break;

                    float x = waveRect.X + (float)(points.Count * waveRect.Width / (double)maxPoints);
                    float y = waveRect.Y + (float)(waveRect.Height - (data[i] - minValue) / range * waveRect.Height * 0.8 - waveRect.Height * 0.1);
                    points.Add(new PointF(x, y));
                }

                if (points.Count > 1)
                {
                    g.DrawLines(pen, points.ToArray());
                }
            }
        }

        // ChartPictureBox_Paint は PlotView 使用により不要

        private void InputPreviewBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void InputPreviewBox_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    string filePath = files[0];
                    if (Path.GetExtension(filePath).ToLower() == ".csv")
                    {
                        // ViewModelのLoadFileメソッドを直接呼び出し
                        LoadFileFromPath(filePath);
                    }
                    else
                    {
                        MessageBox.Show("CSVファイルのみ対応しています。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイル読み込みエラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadFileFromPath(string filePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[EditFlag] LoadFileFromPath開始: {filePath}");

                // ViewModelのFileServiceを使用してファイル読み込み
                var fileServiceField = typeof(MainViewModel).GetField("_fileService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fileServiceField != null)
                {
                    var fileService = fileServiceField.GetValue(_viewModel);
                    if (fileService != null)
                    {
                        var loadMethod = fileService.GetType().GetMethod("LoadCsvFile");
                        if (loadMethod != null)
                        {
                            double samplingRate = 1000.0; // デフォルト値
                            var signalData = loadMethod.Invoke(fileService, new object[] { filePath, samplingRate });

                            // ViewModelのCurrentSignalに設定
                            _viewModel.CurrentSignal = (Models.SignalData)signalData;

                            System.Diagnostics.Debug.WriteLine($"[EditFlag] ファイル読み込み完了: {(_viewModel.CurrentSignal?.FileName ?? "null")}");

                            // 重要: MainViewModelの完全な処理を実行
                            // SetInitialSettingsIfNotEditedを呼び出し
                            var setInitialMethod = typeof(MainViewModel).GetMethod("SetInitialSettingsIfNotEdited",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (setInitialMethod != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[EditFlag] SetInitialSettingsIfNotEdited呼び出し開始");
                                setInitialMethod.Invoke(_viewModel, new object[] { _viewModel.CurrentSignal });
                                System.Diagnostics.Debug.WriteLine($"[EditFlag] SetInitialSettingsIfNotEdited呼び出し完了");
                            }

                            // UI更新を実行 - 現在のモードに応じてコントロールを更新
                            if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
                            {
                                LoadSettingsToControls(windowFunctionCombo, // eegWindowDurationNumeric, // REMOVED: DFT時間幅廃止 
                                     timeMinNumeric, timeMaxNumeric,
                                    freqMinNumeric, freqMaxNumeric);
                            }
                            else
                            {
                                LoadSettingsToControls(windowFunctionCombo, // heartWindowDurationNumeric, // REMOVED: DFT時間幅廃止
                                     timeMinNumeric, timeMaxNumeric,
                                    freqMinNumeric, freqMaxNumeric);
                            }

                            // 信号プロット更新
                            var updateMethod = typeof(MainViewModel).GetMethod("UpdateSignalPlot",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (updateMethod != null)
                            {
                                updateMethod.Invoke(_viewModel, null);
                            }

                            System.Diagnostics.Debug.WriteLine($"[EditFlag] LoadFileFromPath処理完了");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EditFlag] LoadFileFromPathエラー: {ex.Message}");
                MessageBox.Show($"ファイル読み込みエラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExecuteButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
                {
                    PerformEEGAnalysis();
                }
                else
                {
                    UpdateAnalysisSettingsFromUI();

                    // 一時的に直接メソッド呼び出し（Source Generator問題回避）
                    if (_viewModel.CurrentSignal?.TimeData != null && _viewModel.CurrentSignal.TimeData.Length > 0)
                    {
                        // ViewModelの解析メソッドを直接呼び出し
                        var analyzeMethod = typeof(MainViewModel).GetMethod("AnalyzeSignal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (analyzeMethod != null)
                        {
                            analyzeMethod.Invoke(_viewModel, null);
                        }
                    }
                    else
                    {
                        MessageBox.Show("解析を実行できません。CSVファイルを読み込んでください。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HfButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
                {
                    // β波解析
                    AnalyzeSingleBand("β波", 13, 19);
                }
                else
                {
                    // HFフィルタ処理をViewModelに委譲
                    var hfFilterMethod = typeof(MainViewModel).GetMethod("ApplyHFFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (hfFilterMethod != null)
                    {
                        hfFilterMethod.Invoke(_viewModel, null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{(_currentMode == ModeSelectionForm.AnalysisMode.EEG ? "β波解析" : "HFフィルタ")}エラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LfButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
                {
                    // α波解析
                    AnalyzeSingleBand("α波", 8, 13);
                }
                else
                {
                    // LFフィルタ処理をViewModelに委譲
                    var lfFilterMethod = typeof(MainViewModel).GetMethod("ApplyLFFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (lfFilterMethod != null)
                    {
                        lfFilterMethod.Invoke(_viewModel, null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{(_currentMode == ModeSelectionForm.AnalysisMode.EEG ? "α波解析" : "LFフィルタ")}エラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            int totalWidth = this.ClientSize.Width;
            int topHeight = (int)(this.ClientSize.Height * 0.35);

            topPanel.Height = topHeight;

            // パネルの位置・サイズは topTableLayoutPanel が自動管理するため手動設定不要
            int panelWidth = inputPanel.Width;

            // 設定パネル内のコントロールのスケール調整
            ResizeSettingsPanel(panelWidth, topHeight);

            if (inputPreviewBox != null)
            {
                inputPreviewBox.Width = Math.Max(50, inputPanel.ClientSize.Width - 20);
                inputPreviewBox.Height = Math.Max(50, topHeight - 70);
            }

            if (outputPreviewBox != null)
            {
                outputPreviewBox.Width = Math.Max(50, outputPanel.ClientSize.Width - 20);
                outputPreviewBox.Height = Math.Max(50, topHeight - 70);
            }

            // エクスポートボタンの位置を画面中央下端に調整
            if (exportButton != null)
            {
                int chartHeight = this.ClientSize.Height - topHeight;
                exportButton.Left = (totalWidth - exportButton.Width) / 2; // 中央揃え
                exportButton.Top = chartHeight - exportButton.Height - 10; // 下端から10pxの余白
            }
        }

        private void ResizeSettingsPanel(int panelWidth, int panelHeight)
        {
            // 現在アクティブなパネルを取得
            var activePanel = _currentMode == ModeSelectionForm.AnalysisMode.EEG ? settingsPanel : settingsPanel;
            if (activePanel == null) return;

            // 基準サイズ（400x350）に対する比率
            float widthRatio = (float)panelWidth / 400f;
            float heightRatio = (float)panelHeight / 350f;
            float scale = Math.Min(widthRatio, heightRatio); // 均等スケール

            // 最小・最大スケール制限
            scale = Math.Max(0.5f, Math.Min(2.0f, scale));

            // 各コントロールの位置とサイズを調整
            int baseMargin = (int)(20 * scale);
            int labelWidth = (int)(130 * scale);
            int controlWidth = (int)(200 * scale);
            int controlHeight = (int)(25 * scale);
            int lineHeight = (int)(40 * scale);
            int buttonWidth = (int)(75 * scale);
            int executeButtonWidth = (int)(100 * scale);
            int buttonHeight = (int)(40 * scale);

            int currentY = baseMargin;

            // 窓関数
            if (currentWindowFunctionLabel != null)
            {
                currentWindowFunctionLabel.Location = new System.Drawing.Point(baseMargin, currentY);
                currentWindowFunctionLabel.Size = new System.Drawing.Size(labelWidth, (int)(18 * scale));
            }
            if (currentWindowFunctionCombo != null)
            {
                currentWindowFunctionCombo.Location = new System.Drawing.Point(baseMargin + labelWidth, currentY);
                currentWindowFunctionCombo.Size = new System.Drawing.Size(controlWidth, controlHeight);
            }
            currentY += lineHeight;

            // DFT時間幅 (WindowDuration control layout is handled in Designer.cs)

            // 分解能
            if (currentResolutionLabel != null)
            {
                currentResolutionLabel.Location = new System.Drawing.Point(baseMargin, currentY);
                currentResolutionLabel.Size = new System.Drawing.Size(labelWidth, (int)(18 * scale));
            }
            if (currentResolutionNumeric != null)
            {
                currentResolutionNumeric.Location = new System.Drawing.Point(baseMargin + labelWidth, currentY);
                currentResolutionNumeric.Size = new System.Drawing.Size(controlWidth, controlHeight);
            }
            currentY += lineHeight;

            // 時間軸範囲
            if (currentTimeRangeLabel != null)
            {
                currentTimeRangeLabel.Location = new System.Drawing.Point(baseMargin, currentY);
                currentTimeRangeLabel.Size = new System.Drawing.Size(labelWidth, (int)(18 * scale));
            }
            int rangeControlWidth = (int)(90 * scale);
            if (currentTimeMinNumeric != null)
            {
                currentTimeMinNumeric.Location = new System.Drawing.Point(baseMargin + labelWidth, currentY);
                currentTimeMinNumeric.Size = new System.Drawing.Size(rangeControlWidth, controlHeight);
            }
            if (currentTimeMaxNumeric != null)
            {
                currentTimeMaxNumeric.Location = new System.Drawing.Point(baseMargin + labelWidth + rangeControlWidth + 10, currentY);
                currentTimeMaxNumeric.Size = new System.Drawing.Size(rangeControlWidth, controlHeight);
            }
            currentY += lineHeight;

            // 周波数軸範囲
            if (currentFreqRangeLabel != null)
            {
                currentFreqRangeLabel.Location = new System.Drawing.Point(baseMargin, currentY);
                currentFreqRangeLabel.Size = new System.Drawing.Size(labelWidth, (int)(18 * scale));
            }
            if (currentFreqMinNumeric != null)
            {
                currentFreqMinNumeric.Location = new System.Drawing.Point(baseMargin + labelWidth, currentY);
                currentFreqMinNumeric.Size = new System.Drawing.Size(rangeControlWidth, controlHeight);
            }
            if (currentFreqMaxNumeric != null)
            {
                currentFreqMaxNumeric.Location = new System.Drawing.Point(baseMargin + labelWidth + rangeControlWidth + 10, currentY);
                currentFreqMaxNumeric.Size = new System.Drawing.Size(rangeControlWidth, controlHeight);
            }
            currentY += lineHeight;

            // ボタン行
            int buttonY = currentY;
            int buttonSpacing = (int)(25 * scale);
            int totalButtonWidth = buttonWidth * 2 + executeButtonWidth + buttonSpacing * 2;
            int buttonStartX = (panelWidth - totalButtonWidth) / 2;

            /*if (hfButton != null)
            {
                hfButton.Location = new System.Drawing.Point(buttonStartX, buttonY);
                hfButton.Size = new System.Drawing.Size(buttonWidth, buttonHeight);
            }*/
            if (currentExecuteButton != null)
            {
                currentExecuteButton.Location = new System.Drawing.Point(buttonStartX + buttonWidth + buttonSpacing, buttonY);
                currentExecuteButton.Size = new System.Drawing.Size(executeButtonWidth, buttonHeight);
            }
            /*if (lfButton != null)
            {
                lfButton.Location = new System.Drawing.Point(buttonStartX + buttonWidth + executeButtonWidth + buttonSpacing * 2, buttonY);
                lfButton.Size = new System.Drawing.Size(buttonWidth, buttonHeight);
            }*/
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (chartPlotView?.Model == null)
                {
                    MessageBox.Show("エクスポートするグラフがありません。先にデータを読み込んで解析を実行してください。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 現在表示中のグラフタイプを判定
                bool isBandPowerGraph = chartPlotView.Model.Title != null &&
                    (chartPlotView.Model.Title.Contains("バンドパワー") ||
                     chartPlotView.Model.Title.Contains("Band Power") ||
                     chartPlotView.Model.Title.Contains("HRV"));

                string defaultFileName = isBandPowerGraph
                    ? $"バンドパワー_{DateTime.Now:yyyyMMdd_HHmmss}"
                    : $"スペクトログラム_{DateTime.Now:yyyyMMdd_HHmmss}";

                using (var saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "PNG画像 (*.png)|*.png|JPEG画像 (*.jpg)|*.jpg|BMP画像 (*.bmp)|*.bmp";
                    saveFileDialog.Title = "グラフを画像として保存";
                    saveFileDialog.DefaultExt = "png";
                    saveFileDialog.FileName = defaultFileName;

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // OxyPlotの画像エクスポート機能を使用
                        var exporter = new OxyPlot.WindowsForms.PngExporter();

                        // 画像サイズを設定（chartPlotViewのサイズに合わせる）
                        exporter.Width = chartPlotView.Width;
                        exporter.Height = chartPlotView.Height;

                        // ファイル拡張子に応じて適切なエクスポーターを選択
                        string extension = Path.GetExtension(saveFileDialog.FileName).ToLower();

                        switch (extension)
                        {
                            case ".png":
                                var pngExporter = new OxyPlot.WindowsForms.PngExporter { Width = chartPlotView.Width, Height = chartPlotView.Height };
                                using (var stream = File.Create(saveFileDialog.FileName))
                                {
                                    pngExporter.Export(chartPlotView.Model, stream);
                                }
                                break;

                            case ".jpg":
                            case ".jpeg":
                                // JPEGの場合、一度PNGで作成してからJPEGに変換
                                string tempPngFile = Path.GetTempFileName() + ".png";
                                var tempPngExporter = new OxyPlot.WindowsForms.PngExporter { Width = chartPlotView.Width, Height = chartPlotView.Height };
                                using (var tempStream = File.Create(tempPngFile))
                                {
                                    tempPngExporter.Export(chartPlotView.Model, tempStream);
                                }

                                // PNGからJPEGに変換
                                using (var image = System.Drawing.Image.FromFile(tempPngFile))
                                {
                                    image.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                                }
                                File.Delete(tempPngFile);
                                break;

                            case ".bmp":
                                // BMPの場合、一度PNGで作成してからBMPに変換
                                string tempPngFileBmp = Path.GetTempFileName() + ".png";
                                var tempPngExporterBmp = new OxyPlot.WindowsForms.PngExporter { Width = chartPlotView.Width, Height = chartPlotView.Height };
                                using (var tempStreamBmp = File.Create(tempPngFileBmp))
                                {
                                    tempPngExporterBmp.Export(chartPlotView.Model, tempStreamBmp);
                                }

                                // PNGからBMPに変換
                                using (var image = System.Drawing.Image.FromFile(tempPngFileBmp))
                                {
                                    image.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Bmp);
                                }
                                File.Delete(tempPngFileBmp);
                                break;

                            default:
                                // デフォルトはPNG
                                var defaultExporter = new OxyPlot.WindowsForms.PngExporter { Width = chartPlotView.Width, Height = chartPlotView.Height };
                                using (var defaultStream = File.Create(saveFileDialog.FileName))
                                {
                                    defaultExporter.Export(chartPlotView.Model, defaultStream);
                                }
                                break;
                        }

                        MessageBox.Show($"グラフを画像として保存しました:\n{saveFileDialog.FileName}", "保存完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"画像の保存中にエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CsvExportButton_Click(object sender, EventArgs e)
        {
            try
            {
                // 現在表示されているデータに応じてCSV出力
                // バンドパワーグラフが表示されている場合を優先
                if (chartPlotView.Model == _viewModel?.BandPowerPlot &&
                    _viewModel?.BandPowerLineData != null)
                {
                    ExportBandPowerLineDataToCsv();
                }
                else if (_viewModel?.FilterAnalysisResult != null)
                {
                    ExportFilterResultToCsv();
                }
                else if (_viewModel?.SpectrogramData != null)
                {
                    ExportSpectrogramToCsv();
                }
                else
                {
                    MessageBox.Show("エクスポートするデータがありません。先にデータを読み込んで解析を実行してください。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"CSVエクスポート中にエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportFilterResultToCsv()
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
                saveFileDialog.Title = "フィルタ結果をCSVで保存";
                saveFileDialog.DefaultExt = "csv";
                saveFileDialog.FileName = $"フィルタ結果_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var result = _viewModel.FilterAnalysisResult;
                    using (var writer = new System.IO.StreamWriter(saveFileDialog.FileName, false, System.Text.Encoding.UTF8))
                    {
                        // ヘッダー情報
                        writer.WriteLine($"# フィルタ解析結果");
                        writer.WriteLine($"# 解析時刻: {result.AnalysisTime:yyyy/MM/dd HH:mm:ss}");
                        writer.WriteLine($"# フィルタ: {result.FilterDescription}");

                        if (result.LFPowerRatio > 0)
                            writer.WriteLine($"# LF パワー比: {result.LFPowerRatio:F6}");
                        if (result.HFPowerRatio > 0)
                            writer.WriteLine($"# HF パワー比: {result.HFPowerRatio:F6}");
                        if (result.HFLFRatio > 0)
                            writer.WriteLine($"# HF/LF 比: {result.HFLFRatio:F3}");

                        writer.WriteLine();

                        // データ出力
                        if (result.LFFilteredData != null && result.LFFilteredData.Length > 0)
                        {
                            writer.WriteLine("Time(sec),LF_Filtered_Data");
                            double samplingRate = _viewModel.CurrentSignal?.SamplingRate ?? 1000.0;
                            for (int i = 0; i < result.LFFilteredData.Length; i++)
                            {
                                double time = i / samplingRate;
                                writer.WriteLine($"{time:F6},{result.LFFilteredData[i]:F6}");
                            }
                        }
                        else if (result.HFFilteredData != null && result.HFFilteredData.Length > 0)
                        {
                            writer.WriteLine("Time(sec),HF_Filtered_Data");
                            double samplingRate = _viewModel.CurrentSignal?.SamplingRate ?? 1000.0;
                            for (int i = 0; i < result.HFFilteredData.Length; i++)
                            {
                                double time = i / samplingRate;
                                writer.WriteLine($"{time:F6},{result.HFFilteredData[i]:F6}");
                            }
                        }
                    }

                    MessageBox.Show($"フィルタ結果をCSVで保存しました:\n{saveFileDialog.FileName}", "保存完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ExportSpectrogramToCsv()
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
                saveFileDialog.Title = "スペクトログラムデータをCSVで保存";
                saveFileDialog.DefaultExt = "csv";
                saveFileDialog.FileName = $"スペクトログラム_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var spectrogram = _viewModel.SpectrogramData;
                    using (var writer = new System.IO.StreamWriter(saveFileDialog.FileName, false, System.Text.Encoding.UTF8))
                    {
                        // ヘッダー情報
                        writer.WriteLine($"# スペクトログラムデータ");
                        double timeRes = spectrogram.TimeAxis.Length > 1
                            ? spectrogram.TimeAxis[1] - spectrogram.TimeAxis[0] : 0;
                        writer.WriteLine($"# 時間分解能: {timeRes:F3} 秒");
                        double freqRes = spectrogram.FrequencyAxis.Length > 1
                            ? spectrogram.FrequencyAxis[1] - spectrogram.FrequencyAxis[0] : 0;
                        writer.WriteLine($"# 周波数分解能: {freqRes:F1} Hz");
                        if (spectrogram.TimeAxis != null && spectrogram.TimeAxis.Length > 0)
                            writer.WriteLine($"# 時間範囲: {spectrogram.TimeAxis[0]:F3} - {spectrogram.TimeAxis[spectrogram.TimeSteps - 1]:F3} 秒");
                        if (spectrogram.FrequencyAxis != null && spectrogram.FrequencyAxis.Length > 0)
                            writer.WriteLine($"# 周波数範囲: {spectrogram.FrequencyAxis[0]:F1} - {spectrogram.FrequencyAxis[spectrogram.FrequencyBins - 1]:F1} Hz");
                        writer.WriteLine($"# パワー範囲: {spectrogram.MinPower:F1} - {spectrogram.MaxPower:F1} dB");
                        writer.WriteLine();

                        // 周波数軸ヘッダー
                        writer.Write("Time(sec)");
                        for (int f = 0; f < spectrogram.FrequencyBins; f++)
                        {
                            writer.Write($",{spectrogram.FrequencyAxis[f]:F1}");
                        }
                        writer.WriteLine();

                        // データ出力（時間×周波数のマトリックス）
                        for (int t = 0; t < spectrogram.TimeSteps; t++)
                        {
                            writer.Write($"{spectrogram.TimeAxis[t]:F6}");
                            for (int f = 0; f < spectrogram.FrequencyBins; f++)
                            {
                                writer.Write($",{spectrogram.PowerMatrix[t, f]:F3}");
                            }
                            writer.WriteLine();
                        }
                    }

                    MessageBox.Show($"スペクトログラムデータをCSVで保存しました:\n{saveFileDialog.FileName}", "保存完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ExportBandPowerLineDataToCsv()
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
                saveFileDialog.Title = "バンドパワーデータをCSVで保存";
                saveFileDialog.DefaultExt = "csv";
                saveFileDialog.FileName = $"バンドパワー_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var data = _viewModel.BandPowerLineData;
                    using (var writer = new System.IO.StreamWriter(saveFileDialog.FileName, false, System.Text.Encoding.UTF8))
                    {
                        // ヘッダー情報
                        writer.WriteLine("# バンドパワー線グラフデータ（dBスケール）");
                        writer.WriteLine($"# 解析時刻: {data.AnalysisTime:yyyy/MM/dd HH:mm:ss}");
                        writer.WriteLine($"# データ長: {data.SampleCount}サンプル");
                        if (data.TimeAxis != null && data.TimeAxis.Length > 0)
                            writer.WriteLine($"# 時間範囲: {data.TimeAxis[0]:F3} - {data.TimeAxis[data.TimeAxis.Length - 1]:F3} 秒");
                        writer.WriteLine();

                        // カラムヘッダー
                        writer.WriteLine("Time(sec),Delta_dB,Theta_dB,Alpha_dB,Beta_dB,Gamma_dB");

                        // データ出力
                        for (int i = 0; i < data.TimeAxis.Length; i++)
                        {
                            writer.WriteLine($"{data.TimeAxis[i]:F6}," +
                                $"{data.DeltaBandDb[i]:F6}," +
                                $"{data.ThetaBandDb[i]:F6}," +
                                $"{data.AlphaBandDb[i]:F6}," +
                                $"{data.BetaBandDb[i]:F6}," +
                                $"{data.GammaBandDb[i]:F6}");
                        }
                    }

                    MessageBox.Show($"バンドパワーデータをCSVで保存しました:\n{saveFileDialog.FileName}", "保存完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void resolutionNumeric_ValueChanged(object sender, EventArgs e)
        {
            OnSettingsChanged(sender, e);
        }


        private void UpdateProgressValue()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateProgressValue));
                return;
            }

            currentProgressBar.Value = Math.Min(_viewModel.ProgressValue, 100);
        }

        private void EegModeButton_Click(object sender, EventArgs e)
        {
            _currentMode = ModeSelectionForm.AnalysisMode.EEG;

            // 脳波モード用のデフォルト設定を適用
            _viewModel.AnalysisSettings.SetModeDefaults(true);
            _viewModel.IsEegMode = true; // ViewModelのモード情報を更新

            UpdateUIForMode();
            UpdateUIFromAnalysisSettings(); // UI に新しい設定を反映
        }

        private void CbfModeButton_Click(object sender, EventArgs e)
        {
            _currentMode = ModeSelectionForm.AnalysisMode.Heart;

            // 心拍モード用のデフォルト設定を適用  
            _viewModel.AnalysisSettings.SetModeDefaults(false);
            _viewModel.IsEegMode = false; // ViewModelのモード情報を更新

            UpdateUIForMode();
            UpdateUIFromAnalysisSettings(); // UI に新しい設定を反映
        }

        /// <summary>
        /// ひよこ/人間モード切り替えボタンクリック
        /// </summary>
        private void ModeToggleButton_Click(object sender, EventArgs e)
        {
            isChickMode = !isChickMode;
            UpdateModeToggleButton();
            UpdateUIForUserMode();
        }

        private void UpdateButtonLabels()
        {
            if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
            {
                //hfButton.Text = "β波";
                currentExecuteButton.Text = "脳波解析";
                //lfButton.Text = "α波";
            }
            else
            {
                //hfButton.Text = "HPF";
                currentExecuteButton.Text = "心拍解析";
                //lfButton.Text = "LPF";
            }
        }

        private void InterpolationCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is CheckBox checkBox && _viewModel != null)
            {
                _viewModel.EnableInterpolation = checkBox.Checked;
            }
        }

        private void DcRemovalCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is CheckBox checkBox && _viewModel != null)
            {
                _viewModel.AnalysisSettings.EnableDcRemoval = checkBox.Checked;
            }
        }

        private void LogScaleCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is CheckBox checkBox && _viewModel != null)
            {
                bool isLog = checkBox.Checked;

                // まずViewModelの対数スケール設定を更新（軸の再構築）
                _viewModel.IsLogScale = isLog;

                // NumericUpDownの表示モードを切替（Hz ↔ 10^n 指数）
                SwitchFreqNumericMode(isLog);
            }
        }

        /// <summary>
        /// 周波数範囲NumericUpDownの表示モードをHz/指数で切り替える
        /// </summary>
        private void SwitchFreqNumericMode(bool isLog)
        {
            freqMinNumeric.ValueChanged -= AxisRange_ValueChanged;
            freqMaxNumeric.ValueChanged -= AxisRange_ValueChanged;

            try
            {
                double minHz = _viewModel.AnalysisSettings.DisplayMinFreq;
                double maxHz = _viewModel.AnalysisSettings.DisplayMaxFreq;

                if (isLog)
                {
                    // 指数モード: Minimum を先に広げてから Value を設定
                    freqMinNumeric.Minimum = -3m;
                    freqMinNumeric.Maximum = 5m;
                    freqMaxNumeric.Minimum = -3m;
                    freqMaxNumeric.Maximum = 5m;

                    freqMinNumeric.DecimalPlaces = 1;
                    freqMinNumeric.Increment = 0.5m;
                    freqMaxNumeric.DecimalPlaces = 1;
                    freqMaxNumeric.Increment = 0.5m;

                    decimal minExp = (decimal)Math.Round(Math.Log10(Math.Max(minHz, 0.001)), 1);
                    decimal maxExp = (decimal)Math.Round(Math.Log10(Math.Max(maxHz, 0.001)), 1);
                    freqMinNumeric.Value = Math.Max(freqMinNumeric.Minimum, Math.Min(freqMinNumeric.Maximum, minExp));
                    freqMaxNumeric.Value = Math.Max(freqMaxNumeric.Minimum, Math.Min(freqMaxNumeric.Maximum, maxExp));

                    freqRangeLabel.Text = "周波数範囲(10^n)";
                }
                else
                {
                    // Hzモード: Maximum を先に広げてから Value → Minimum の順
                    freqMaxNumeric.Maximum = 1000m;
                    freqMinNumeric.Maximum = 1000m;

                    decimal minVal = (decimal)Math.Max(0, Math.Min(1000, Math.Round(minHz, 1)));
                    decimal maxVal = (decimal)Math.Max(0, Math.Min(1000, Math.Round(maxHz, 1)));
                    freqMinNumeric.Value = minVal;
                    freqMaxNumeric.Value = maxVal;

                    freqMinNumeric.Minimum = 0m;
                    freqMaxNumeric.Minimum = 0m;
                    freqMinNumeric.DecimalPlaces = 1;
                    freqMinNumeric.Increment = 0.1m;
                    freqMaxNumeric.DecimalPlaces = 1;
                    freqMaxNumeric.Increment = 1m;

                    freqRangeLabel.Text = "周波数範囲(Hz)";
                }
            }
            finally
            {
                freqMinNumeric.ValueChanged += AxisRange_ValueChanged;
                freqMaxNumeric.ValueChanged += AxisRange_ValueChanged;
            }
        }

        /// <summary>
        /// Hz値から周波数NumericUpDownを設定（対数モード時は自動で指数に変換）
        /// </summary>
        private void SetFreqNumericFromHz(NumericUpDown control, double hzValue)
        {
            if (_viewModel?.IsLogScale == true)
            {
                decimal exponent = (decimal)Math.Round(Math.Log10(Math.Max(hzValue, 0.001)), 1);
                exponent = Math.Max(control.Minimum, Math.Min(control.Maximum, exponent));
                SetNumericValueProgrammatically(control, exponent);
            }
            else
            {
                decimal val = (decimal)Math.Max((double)control.Minimum, Math.Min((double)control.Maximum, hzValue));
                SetNumericValueProgrammatically(control, val);
            }
        }

        private void BandCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_viewModel != null)
            {
                // ViewModelのバンド選択状態を更新
                _viewModel.IsDeltaSelected = eegDeltaCheckBox.Checked;
                _viewModel.IsThetaSelected = eegThetaCheckBox.Checked;
                _viewModel.IsAlphaSelected = eegAlphaCheckBox.Checked;
                _viewModel.IsBetaSelected = eegBetaCheckBox.Checked;
                _viewModel.IsGammaSelected = eegGammaCheckBox.Checked;
                _viewModel.IsLFSelected = heartLFCheckBox.Checked;
                _viewModel.IsHFSelected = heartHFCheckBox.Checked;
                _viewModel.IsLfHfRatioSelected = heartLfHfRatioCheckBox.Checked;

                // バンドパワーグラフを再描画
                UpdateBandPowerPlotFromViewModel();
            }
        }

        private void EnsembleAveragingButton_Click(object sender, EventArgs e)
        {
            if (_viewModel != null)
            {
                // 現在の状態をトグル
                bool currentlyEnabled = _viewModel.AnalysisSettings.EnableEnsembleAveraging;
                bool newState = !currentlyEnabled;

                _viewModel.AnalysisSettings.EnableEnsembleAveraging = newState;

                // オーバーレイ表示の切り替え
                if (newState)
                {
                    // 加算平均設定パネルをオーバーレイ表示
                    commonSettingsPanel.Visible = false;

                    // グラフ生成ボタンと他のボタンも最前面に表示（パネルに隠れないようにする）
                    spectrogramExecuteButton.BringToFront();
                    bandPowerSelectButton.BringToFront();
                    spectrogramProgressBar.BringToFront();
                }

                // 加算平均パラメータをAnalysisSettingsに設定
                if (newState)
                {
                    // TriggerThreshold、PreTrigger、PostTriggerは加算平均パラメータとは別の概念
                    // （デフォルト値が使われる: TriggerThreshold=0.1, PreTrigger=0.1秒, PostTrigger=0.5秒）
                }
            }
        }

        /// <summary>
        /// 加算平均パラメータパネルの戻るボタンクリック
        /// </summary>
        private void EnsembleAveragingBackButton_Click(object sender, EventArgs e)
        {
            // オーバーレイを閉じて元の設定画面に戻る
            commonSettingsPanel.Visible = true;
        }

        private void UpdateBandPowerPlotFromViewModel()
        {
            if (_viewModel?.BandPowerLineData != null && _viewModel.BandPowerLineData.IsValid())
            {
                // ViewModelのUpdateBandPowerLinePlotメソッドを呼び出し
                var updateMethod = typeof(MainViewModel).GetMethod("UpdateBandPowerLinePlot",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (updateMethod != null)
                {
                    updateMethod.Invoke(_viewModel, null);
                }
            }
        }

        private void UpdateUIFromAnalysisSettings()
        {
            if (_viewModel?.AnalysisSettings == null)
                return;

            // 設定読み込み中フラグを設定（編集フラグ保護）
            _isLoadingSettings = true;

            try
            {
                var settings = _viewModel.AnalysisSettings;

                // 脳波モードのコントロールを更新
                if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
                {
                    timeMinNumeric.Value = (decimal)settings.MinTime;
                    timeMaxNumeric.Value = (decimal)settings.MaxTime;
                    SetFreqNumericFromHz(freqMinNumeric, settings.DisplayMinFreq);
                    SetFreqNumericFromHz(freqMaxNumeric, settings.DisplayMaxFreq);

                    interpolationCheckBox.Checked = settings.EnableInterpolation;
                    dcRemovalCheckBox.Checked = settings.EnableDcRemoval;
                }

                // 心拍モードのコントロールを更新
                if (_currentMode == ModeSelectionForm.AnalysisMode.Heart)
                {
                    timeMinNumeric.Value = (decimal)settings.MinTime;
                    timeMaxNumeric.Value = (decimal)settings.MaxTime;
                    SetFreqNumericFromHz(freqMinNumeric, settings.DisplayMinFreq);
                    SetFreqNumericFromHz(freqMaxNumeric, Math.Min(settings.DisplayMaxFreq, 20.0));

                    interpolationCheckBox.Checked = settings.EnableInterpolation;
                    dcRemovalCheckBox.Checked = settings.EnableDcRemoval;
                }

                System.Diagnostics.Debug.WriteLine($"UI updated from AnalysisSettings: Time={settings.MinTime:F1}-{settings.MaxTime:F1}s, Freq=0-{settings.DisplayMaxFreq:F1}Hz, TimeResolution={settings.TimeResolution:F6}s");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating UI from AnalysisSettings: {ex.Message}");
            }
            finally
            {
                // 設定読み込み完了フラグをリセット
                _isLoadingSettings = false;
                System.Diagnostics.Debug.WriteLine("UpdateUIFromAnalysisSettings: 編集フラグ保護完了");
            }
        }

        /// <summary>
        /// プログラムによるNumericUpDown値設定を安全に行う（編集フラグを設定しない）
        /// </summary>
        /// <param name="control">対象のNumericUpDownコントロール</param>
        /// <param name="value">設定する値</param>
        private void SetNumericValueProgrammatically(NumericUpDown control, decimal value)
        {
            if (control == null) return;

            bool wasLoadingSettings = _isLoadingSettings;
            _isLoadingSettings = true;

            try
            {
                control.Value = value;
                System.Diagnostics.Debug.WriteLine($"プログラム設定: {control.Name} = {value}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NumericUpDown設定エラー: {control.Name}, {ex.Message}");
            }
            finally
            {
                // 元の状態を復元（ネストした呼び出しに対応）
                _isLoadingSettings = wasLoadingSettings;
            }
        }

        private void PerformEEGAnalysis()
        {
            if (_viewModel?.CurrentSignal?.TimeData == null)
            {
                // 脳波モードでテストデータを生成
                GenerateEEGTestData();
                if (_viewModel?.CurrentSignal?.TimeData == null)
                {
                    MessageBox.Show("解析を実行できません。CSVファイルを読み込んでください。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                _viewModel.StatusMessage = "脳波解析を開始しています...";

                var timeData = _viewModel.CurrentSignal.TimeData;
                var samplingRate = _viewModel.CurrentSignal.SamplingRate;

                _viewModel.StatusMessage = $"データサイズ: {timeData.Length}サンプル, サンプリング周波数: {samplingRate}Hz";

                // 各周波数帯域の1秒積分値を計算
                var deltaIntegrals = CalculateBandIntegrals(timeData, samplingRate, 0.5, 4);    // δ波 (0.5-4Hz)
                var thetaIntegrals = CalculateBandIntegrals(timeData, samplingRate, 4, 8);      // θ波 (4-8Hz)
                var alphaIntegrals = CalculateBandIntegrals(timeData, samplingRate, 8, 13);     // α波 (8-13Hz)
                var betaIntegrals = CalculateBandIntegrals(timeData, samplingRate, 13, 19);     // β波 (13-19Hz)

                // スペクトログラム解析も実行してグラフに表示
                if (_viewModel.AnalysisSettings != null)
                {
                    _viewModel.StatusMessage = "スペクトログラム計算中...";

                    // ViewModelのAnalyzeSignalメソッドを呼び出し
                    var analyzeMethod = typeof(MainViewModel).GetMethod("AnalyzeSignal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (analyzeMethod != null)
                    {
                        analyzeMethod.Invoke(_viewModel, null);
                    }
                }

                // 結果を表示
                DisplayEEGResults(deltaIntegrals, thetaIntegrals, alphaIntegrals, betaIntegrals);

                // 脳波スペクトラムをグラフに直接表示
                CreateEEGSpectrumPlot(deltaIntegrals, thetaIntegrals, alphaIntegrals, betaIntegrals);

                // スペクトログラム解析完了後、確実にSpectrogramPlotを表示
                chartPlotView.Model = _viewModel.SpectrogramPlot;

                // 軸タイトルを強制修正
                ForceFixSpectrogramAxisTitles();

                chartPlotView.InvalidatePlot(true);
            }
            catch (Exception ex)
            {
                _viewModel.StatusMessage = $"脳波解析エラー: {ex.Message}";
                MessageBox.Show($"脳波解析エラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private double[] CalculateBandIntegrals(double[] timeData, double samplingRate, double lowFreq, double highFreq)
        {
            // データの前処理：異常値をチェック
            var cleanedData = CleanSignalData(timeData);

            // バンドパスフィルタを適用
            var filteredData = ApplyBandPassFilter(cleanedData, samplingRate, lowFreq, highFreq);

            int windowSize = (int)samplingRate; // 1秒のウィンドウサイズ
            var integrals = new List<double>();

            for (int i = 0; i <= filteredData.Length - windowSize; i += windowSize)
            {
                var segment = new double[windowSize];
                Array.Copy(filteredData, i, segment, 0, windowSize);

                // パワーを計算（RMS値の二乗）
                double power = 0;
                for (int j = 0; j < segment.Length; j++)
                {
                    if (!double.IsNaN(segment[j]) && !double.IsInfinity(segment[j]))
                    {
                        power += segment[j] * segment[j];
                    }
                }
                power /= segment.Length; // 平均パワー

                // 異常値をチェックして安全な範囲に制限
                if (!double.IsNaN(power) && !double.IsInfinity(power) && power >= 0 && power < 1e10)
                {
                    integrals.Add(Math.Log10(Math.Max(power, 1e-10))); // 対数スケールで保存
                }
                else
                {
                    integrals.Add(-10.0); // デフォルト値
                }
            }

            return integrals.ToArray();
        }

        private double[] ApplyHanningWindow(double[] data)
        {
            var windowed = new double[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                double window = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (data.Length - 1)));
                windowed[i] = data[i] * window;
            }
            return windowed;
        }

        private double[] CalculatePowerSpectrum(double[] data, double samplingRate)
        {
            // 簡易FFT実装（実際のプロジェクトでは既存のFFTライブラリを使用）
            int n = data.Length;
            var powerSpectrum = new double[n / 2];

            for (int k = 0; k < n / 2; k++)
            {
                double real = 0, imag = 0;
                for (int t = 0; t < n; t++)
                {
                    double angle = -2 * Math.PI * k * t / n;
                    real += data[t] * Math.Cos(angle);
                    imag += data[t] * Math.Sin(angle);
                }
                powerSpectrum[k] = (real * real + imag * imag) / n;
            }

            return powerSpectrum;
        }

        private double IntegrateBandPower(double[] powerSpectrum, double samplingRate, double lowFreq, double highFreq)
        {
            int lowBin = (int)(lowFreq * powerSpectrum.Length * 2 / samplingRate);
            int highBin = (int)(highFreq * powerSpectrum.Length * 2 / samplingRate);

            lowBin = Math.Max(0, lowBin);
            highBin = Math.Min(powerSpectrum.Length - 1, highBin);

            double sum = 0;
            for (int i = lowBin; i <= highBin; i++)
            {
                sum += powerSpectrum[i];
            }
            return sum;
        }

        private void DisplayEEGResults(double[] delta, double[] theta, double[] alpha, double[] beta)
        {
            var result = new StringBuilder();
            result.AppendLine("脳波解析結果 (1秒積分値, 対数スケール)");
            result.AppendLine($"時間ステップ数: {Math.Max(Math.Max(delta.Length, theta.Length), Math.Max(alpha.Length, beta.Length))}");
            result.AppendLine();

            // 統計情報（対数スケールなのでそのまま表示）
            result.AppendLine($"δ波 (0.5-4Hz): 平均={delta.Average():F2} dB, 最大={delta.Max():F2} dB");
            result.AppendLine($"θ波 (4-8Hz): 平均={theta.Average():F2} dB, 最大={theta.Max():F2} dB");
            result.AppendLine($"α波 (8-13Hz): 平均={alpha.Average():F2} dB, 最大={alpha.Max():F2} dB");
            result.AppendLine($"β波 (13-19Hz): 平均={beta.Average():F2} dB, 最大={beta.Max():F2} dB");

            _viewModel.StatusMessage = result.ToString();
        }

        private void AnalyzeSingleBand(string bandName, double lowFreq, double highFreq)
        {
            if (_viewModel?.CurrentSignal?.TimeData == null)
            {
                MessageBox.Show("解析を実行できません。CSVファイルを読み込んでください。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var timeData = _viewModel.CurrentSignal.TimeData;
            var samplingRate = _viewModel.CurrentSignal.SamplingRate;

            var integrals = CalculateBandIntegrals(timeData, samplingRate, lowFreq, highFreq);

            var result = new StringBuilder();
            result.AppendLine($"{bandName} 解析結果 ({lowFreq}-{highFreq}Hz)");
            result.AppendLine($"時間ステップ数: {integrals.Length}");
            result.AppendLine($"平均パワー: {integrals.Average():F2} dB");
            result.AppendLine($"最大パワー: {integrals.Max():F2} dB");
            result.AppendLine($"最小パワー: {integrals.Min():F2} dB");
            result.AppendLine();
            result.AppendLine("1秒積分値 (対数スケール):");
            for (int i = 0; i < Math.Min(integrals.Length, 10); i++)
            {
                result.AppendLine($"  {i + 1}秒目: {integrals[i]:F2} dB");
            }
            if (integrals.Length > 10)
            {
                result.AppendLine($"  ... （{integrals.Length - 10}個の値を省略）");
            }

            _viewModel.StatusMessage = result.ToString();
        }

        private double[] ApplyLowPassFilter(double[] data, double samplingRate, double cutoffFreq)
        {
            double dt = 1.0 / samplingRate;
            double rc = 1.0 / (2.0 * Math.PI * cutoffFreq);
            double alpha = dt / (rc + dt);

            // alphaの値を安全な範囲に制限
            alpha = Math.Max(0.0, Math.Min(1.0, alpha));

            var filtered = new double[data.Length];
            filtered[0] = (!double.IsNaN(data[0]) && !double.IsInfinity(data[0])) ? data[0] : 0.0;

            for (int i = 1; i < data.Length; i++)
            {
                double input = (!double.IsNaN(data[i]) && !double.IsInfinity(data[i])) ? data[i] : 0.0;
                double prev = (!double.IsNaN(filtered[i - 1]) && !double.IsInfinity(filtered[i - 1])) ? filtered[i - 1] : 0.0;

                filtered[i] = alpha * input + (1 - alpha) * prev;

                // 結果の安全性チェック
                if (double.IsNaN(filtered[i]) || double.IsInfinity(filtered[i]))
                {
                    filtered[i] = prev;
                }
            }

            return filtered;
        }

        private double[] ApplyHighPassFilter(double[] data, double samplingRate, double cutoffFreq)
        {
            double dt = 1.0 / samplingRate;
            double rc = 1.0 / (2.0 * Math.PI * cutoffFreq);
            double alpha = rc / (rc + dt);

            // alphaの値を安全な範囲に制限
            alpha = Math.Max(0.0, Math.Min(1.0, alpha));

            var filtered = new double[data.Length];
            filtered[0] = 0;

            for (int i = 1; i < data.Length; i++)
            {
                double current = (!double.IsNaN(data[i]) && !double.IsInfinity(data[i])) ? data[i] : 0.0;
                double prev = (!double.IsNaN(data[i - 1]) && !double.IsInfinity(data[i - 1])) ? data[i - 1] : 0.0;
                double filteredPrev = (!double.IsNaN(filtered[i - 1]) && !double.IsInfinity(filtered[i - 1])) ? filtered[i - 1] : 0.0;

                filtered[i] = alpha * (filteredPrev + current - prev);

                // 結果の安全性チェック
                if (double.IsNaN(filtered[i]) || double.IsInfinity(filtered[i]))
                {
                    filtered[i] = 0.0;
                }
            }

            return filtered;
        }

        private double[] ApplyBandPassFilter(double[] data, double samplingRate, double lowFreq, double highFreq)
        {
            var lowPassed = ApplyLowPassFilter(data, samplingRate, highFreq);
            var bandPassed = ApplyHighPassFilter(lowPassed, samplingRate, lowFreq);
            return bandPassed;
        }

        private double[] CleanSignalData(double[] data)
        {
            var cleaned = new double[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                if (double.IsNaN(data[i]) || double.IsInfinity(data[i]))
                {
                    // NaNや無限大の場合は前の値または0で置換
                    cleaned[i] = i > 0 ? cleaned[i - 1] : 0.0;
                }
                else if (Math.Abs(data[i]) > 1e6)
                {
                    // 異常に大きな値は制限
                    cleaned[i] = Math.Sign(data[i]) * 1e6;
                }
                else
                {
                    cleaned[i] = data[i];
                }
            }

            return cleaned;
        }

        private void GenerateEEGTestData()
        {
            try
            {
                double samplingRate = 250.0; // 250Hz
                int duration = 10; // 10秒
                int dataLength = (int)(samplingRate * duration);

                var testData = new double[dataLength];
                var random = new Random();

                // 脳波風の複合信号を生成
                for (int i = 0; i < dataLength; i++)
                {
                    double t = i / samplingRate;

                    // α波 (10Hz) 
                    testData[i] += 50.0 * Math.Sin(2 * Math.PI * 10 * t);

                    // θ波 (6Hz)
                    testData[i] += 30.0 * Math.Sin(2 * Math.PI * 6 * t);

                    // β波 (15Hz)
                    testData[i] += 20.0 * Math.Sin(2 * Math.PI * 15 * t);

                    // ノイズ
                    testData[i] += 10.0 * (random.NextDouble() - 0.5);
                }

                // SignalDataオブジェクトを作成
                var signalData = new SignalData
                {
                    TimeData = testData,
                    SamplingRate = samplingRate,
                    FileName = "EEGテストデータ"
                };

                _viewModel.CurrentSignal = signalData;
                _viewModel.StatusMessage = "脳波テストデータを生成しました";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"テストデータ生成エラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateEEGSpectrumPlot(double[] delta, double[] theta, double[] alpha, double[] beta)
        {
            try
            {
                // ViewModelのSpectrogramPlotを直接操作してEEG結果を表示
                _viewModel.SpectrogramPlot.Series.Clear();
                _viewModel.SpectrogramPlot.Title = "脳波解析結果 (各帯域の平均パワー)";

                var eegSeries = new OxyPlot.Series.LineSeries
                {
                    Title = "脳波帯域パワー",
                    Color = OxyPlot.OxyColors.Blue,
                    StrokeThickness = 3,
                    MarkerType = OxyPlot.MarkerType.Circle,
                    MarkerSize = 8,
                    MarkerFill = OxyPlot.OxyColors.LightBlue
                };

                // 各帯域の平均値を計算して表示
                double deltaAvg = delta.Length > 0 ? delta.Average() : -100;
                double thetaAvg = theta.Length > 0 ? theta.Average() : -100;
                double alphaAvg = alpha.Length > 0 ? alpha.Average() : -100;
                double betaAvg = beta.Length > 0 ? beta.Average() : -100;

                eegSeries.Points.Add(new OxyPlot.DataPoint(0, deltaAvg));
                eegSeries.Points.Add(new OxyPlot.DataPoint(1, thetaAvg));
                eegSeries.Points.Add(new OxyPlot.DataPoint(2, alphaAvg));
                eegSeries.Points.Add(new OxyPlot.DataPoint(3, betaAvg));

                _viewModel.SpectrogramPlot.Series.Add(eegSeries);

                // 軸設定
                _viewModel.SpectrogramPlot.Axes.Clear();
                var xAxis = new OxyPlot.Axes.LinearAxis
                {
                    Position = OxyPlot.Axes.AxisPosition.Bottom,
                    Title = "脳波帯域 (0:δ, 1:θ, 2:α, 3:β)",
                    Minimum = -0.5,
                    Maximum = 3.5
                };

                var valueAxis = new OxyPlot.Axes.LinearAxis
                {
                    Position = OxyPlot.Axes.AxisPosition.Left,
                    Title = "パワー (dB)"
                };

                _viewModel.SpectrogramPlot.Axes.Add(xAxis);
                _viewModel.SpectrogramPlot.Axes.Add(valueAxis);

                _viewModel.SpectrogramPlot.InvalidatePlot(true);
                _viewModel.StatusMessage = "脳波解析結果をグラフに表示しました";
            }
            catch (Exception ex)
            {
                _viewModel.StatusMessage = $"グラフ表示エラー: {ex.Message}";
            }
        }

        #region バンドパワー解析イベントハンドラ

        private void BandPowerAnalysisButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_viewModel?.CurrentSignal?.TimeData == null)
                {
                    MessageBox.Show("解析を実行できません。CSVファイルを読み込んでください。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // モードに応じて処理を分岐
                if (_currentMode == ModeSelectionForm.AnalysisMode.Heart)
                {
                    // 心拍モード: HRV解析を実行
                    _viewModel.StatusMessage = "心拍変動（HRV）解析を開始...";

                    Task.Run(async () =>
                    {
                        await _viewModel.ExecuteHrvAnalysisAsync();

                        // UI更新をメインスレッドで実行
                        this.Invoke(new Action(() =>
                        {
                            // HRV解析結果をグラフ表示
                            chartPlotView.Model = _viewModel.BandPowerPlot;
                            chartPlotView.InvalidatePlot(true);

                            string modeText = isChickMode ? "ひよこモード" : "人間モード";
                            MessageBox.Show($"心拍変動（HRV）解析が完了しました。\n\nモード: {modeText}\nLF/HF成分のグラフが表示されています。", "解析完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }));
                    });
                }
                else
                {
                    // 脳波モード: Python互換バンドパワー線グラフを生成
                    _viewModel.StatusMessage = "Python互換バンドパワー線グラフ解析を開始...";

                    // 非同期でバンドパワー線グラフ解析を実行
                    Task.Run(async () =>
                    {
                        await _viewModel.ExecuteBandPowerLineAnalysisAsync();

                        // UI更新をメインスレッドで実行
                        this.Invoke(new Action(() =>
                        {
                            // グラフビューをバンドパワープロットに切り替え
                            chartPlotView.Model = _viewModel.BandPowerPlot;
                            chartPlotView.InvalidatePlot(true);

                            MessageBox.Show("Python互換バンドパワー線グラフ解析が完了しました。\n\n全5バンド（δ, θ, α, β, γ）のdBスケール線グラフが表示されています。", "解析完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }));
                    });
                }
            }
            catch (Exception ex)
            {
                currentProgressBar.Visible = false;
                bandPowerAnalysisButton.Enabled = true;
                _viewModel.StatusMessage = $"バンドパワー解析エラー: {ex.Message}";
                MessageBox.Show($"バンドパワー解析エラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BandPowerExportButton_Click(object sender, EventArgs e)
        {
            try
            {
                // バンドパワー解析結果がある場合はグラフ表示、ない場合は情報表示
                if (_lastBandPowerResult != null)
                {
                    ShowBandPowerGraphs(_lastBandPowerResult);
                }
                else
                {
                    MessageBox.Show("バンドパワー解析を先に実行してください。\n\n1. CSVファイルを読み込み\n2. 'バンドパワー解析'ボタンをクリック\n3. 解析完了後、この'情報'ボタンでグラフ表示", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"グラフ表示エラー: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// バンドパワー解析結果をグラフ表示（簡略化）
        /// </summary>
        private void ShowBandPowerGraphs(BandPowerAnalysisResult result)
        {
            try
            {
                // 一時的にバンドパワーグラフを表示
                _viewModel.SpectrogramPlot.Series.Clear();
                _viewModel.SpectrogramPlot.Axes.Clear();
                _viewModel.SpectrogramPlot.Title = "脳波バンドパワー正規化解析結果";

                // 軸設定
                _viewModel.SpectrogramPlot.Axes.Add(new OxyPlot.Axes.LinearAxis
                {
                    Position = OxyPlot.Axes.AxisPosition.Bottom,
                    Title = "時間 (秒)"
                });
                _viewModel.SpectrogramPlot.Axes.Add(new OxyPlot.Axes.LinearAxis
                {
                    Position = OxyPlot.Axes.AxisPosition.Left,
                    Title = "正規化パワー (%)",
                    Minimum = 0,
                    Maximum = 100
                });

                // 主要バンドのみ表示（γ波、β波）
                var gammaSeries = new OxyPlot.Series.LineSeries
                {
                    Title = "γ波 (30-100Hz)",
                    Color = OxyPlot.OxyColors.Red,
                    StrokeThickness = 2
                };
                var betaSeries = new OxyPlot.Series.LineSeries
                {
                    Title = "β波 (13-30Hz)",
                    Color = OxyPlot.OxyColors.Blue,
                    StrokeThickness = 2
                };

                for (int i = 0; i < result.TimeAxis.Length; i++)
                {
                    double time = result.TimeAxis[i];
                    gammaSeries.Points.Add(new OxyPlot.DataPoint(time, result.GammaPercent[i]));
                    betaSeries.Points.Add(new OxyPlot.DataPoint(time, result.BetaPercent[i]));
                }

                _viewModel.SpectrogramPlot.Series.Add(gammaSeries);
                _viewModel.SpectrogramPlot.Series.Add(betaSeries);

                _viewModel.SpectrogramPlot.InvalidatePlot(true);
                chartPlotView.InvalidatePlot(true);

                _viewModel.StatusMessage = "バンドパワーグラフを表示中";
            }
            catch (Exception ex)
            {
                _viewModel.StatusMessage = $"グラフ表示エラー: {ex.Message}";
            }
        }

        #endregion

        #region バンド設定イベントハンドラ

        private void EegBandSettingsButton_Click(object sender, EventArgs e)
        {
            using (var bandSelectionForm = new BandSelectionForm(ModeSelectionForm.AnalysisMode.EEG))
            {
                // 現在の設定を渡す
                bandSelectionForm.EegBandSettings = _eegBandSettings;

                if (bandSelectionForm.ShowDialog(this) == DialogResult.OK)
                {
                    // 設定を更新
                    _eegBandSettings = bandSelectionForm.EegBandSettings;
                    UpdateSelectedBandsDisplay();
                }
            }
        }

        private void HeartBandSettingsButton_Click(object sender, EventArgs e)
        {
            using (var bandSelectionForm = new BandSelectionForm(ModeSelectionForm.AnalysisMode.Heart))
            {
                // 現在の設定を渡す
                bandSelectionForm.HeartBandSettings = _heartBandSettings;

                if (bandSelectionForm.ShowDialog(this) == DialogResult.OK)
                {
                    // 設定を更新
                    _heartBandSettings = bandSelectionForm.HeartBandSettings;
                    UpdateSelectedBandsDisplay();
                }
            }
        }

        private void UpdateSelectedBandsDisplay()
        {
            if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
            {
                var selectedBands = new List<string>();
                if (_eegBandSettings.Delta) selectedBands.Add("δ");
                if (_eegBandSettings.Theta) selectedBands.Add("θ");
                if (_eegBandSettings.Alpha) selectedBands.Add("α");
                if (_eegBandSettings.Beta) selectedBands.Add("β");
                if (_eegBandSettings.Gamma) selectedBands.Add("γ");

                eegSelectedBandsLabel.Text = selectedBands.Count > 0 ?
                    $"解析波：{string.Join(", ", selectedBands)}" : "解析波：なし";
            }
            else
            {
                var selectedBands = new List<string>();
                if (_heartBandSettings.LF) selectedBands.Add("LF");
                if (_heartBandSettings.HF) selectedBands.Add("HF");

                heartSelectedBandsLabel.Text = selectedBands.Count > 0 ?
                    $"解析成分：{string.Join(", ", selectedBands)}" : "解析成分：なし";
            }
        }

        #endregion

        #region 新しいバンドパワーパネル用イベントハンドラ

        /// <summary>
        /// メイン設定パネルのバンドパワーボタンクリック
        /// </summary>
        private void BandPowerSelectButton_Click(object sender, EventArgs e)
        {
            // メイン設定パネルを隠してバンドパワーパネルを表示
            settingsPanel.Visible = false;
            settingsPanel.Visible = false;
            bandPowerPanel.Visible = true;

            // チェックボックス表示を更新
            UpdateBandPowerCheckBoxes();
        }

        /// <summary>
        /// バンドパワーパネルの戻るボタンクリック
        /// </summary>
        private void BandPowerBackButton_Click(object sender, EventArgs e)
        {
            // バンドパワーパネルを隠して元のモード設定パネルを表示
            bandPowerPanel.Visible = false;
            if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
            {
                settingsPanel.Visible = true;
            }
            else
            {
                settingsPanel.Visible = true;
            }
        }

        /// <summary>
        /// バンドパワーパネルのチェックボックス表示を現在のモードに応じて更新
        /// </summary>
        private void UpdateBandPowerCheckBoxes()
        {
            if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
            {
                // 脳波モード: EEGチェックボックスを表示、心拍チェックボックスを非表示
                eegDeltaCheckBox.Visible = true;
                eegThetaCheckBox.Visible = true;
                eegAlphaCheckBox.Visible = true;
                eegBetaCheckBox.Visible = true;
                eegGammaCheckBox.Visible = true;

                // チェック状態をViewModelと同期
                eegDeltaCheckBox.Checked = _viewModel?.IsDeltaSelected ?? true;
                eegThetaCheckBox.Checked = _viewModel?.IsThetaSelected ?? true;
                eegAlphaCheckBox.Checked = _viewModel?.IsAlphaSelected ?? true;
                eegBetaCheckBox.Checked = _viewModel?.IsBetaSelected ?? true;
                eegGammaCheckBox.Checked = _viewModel?.IsGammaSelected ?? true;

                heartLFCheckBox.Visible = false;
                heartHFCheckBox.Visible = false;
                heartLfHfRatioCheckBox.Visible = false;

                bandSelectionLabel.Text = "解析する脳波を選択：";
            }
            else
            {
                // 心拍モード: 心拍チェックボックスを表示、EEGチェックボックスを非表示
                eegDeltaCheckBox.Visible = false;
                eegThetaCheckBox.Visible = false;
                eegAlphaCheckBox.Visible = false;
                eegBetaCheckBox.Visible = false;
                eegGammaCheckBox.Visible = false;

                heartLFCheckBox.Visible = true;
                heartHFCheckBox.Visible = true;
                heartLfHfRatioCheckBox.Visible = true;

                // チェック状態をViewModelと同期
                heartLFCheckBox.Checked = _viewModel?.IsLFSelected ?? true;
                heartHFCheckBox.Checked = _viewModel?.IsHFSelected ?? true;
                heartLfHfRatioCheckBox.Checked = _viewModel?.IsLfHfRatioSelected ?? true;

                bandSelectionLabel.Text = "解析する成分を選択：";

                // ひよこ/人間モードに応じてLF/HF周波数表示を更新
                UpdateHeartRateCheckBoxLabels();
            }
        }

        /// <summary>
        /// 心拍モードのチェックボックスラベルをひよこ/人間モードに応じて更新
        /// </summary>
        private void UpdateHeartRateCheckBoxLabels()
        {
            if (isChickMode)
            {
                // ひよこモード: HF=1.2-0.4Hz, LF=0.1-0.04Hz
                heartHFCheckBox.Text = "HF (1.2-0.4Hz)";
                heartLFCheckBox.Text = "LF (0.1-0.04Hz)";
            }
            else
            {
                // 人間モード: HF=0.4-0.15Hz, LF=0.15-0.04Hz
                heartHFCheckBox.Text = "HF (0.4-0.15Hz)";
                heartLFCheckBox.Text = "LF (0.15-0.04Hz)";
            }
        }


        /// <summary>
        /// バンドパワーパネルのグラフ生成ボタンクリック
        /// </summary>
        private void BandPowerGenerateButton_Click(object sender, EventArgs e)
        {
            // 現在のモードに応じて適切なバンドパワー解析を実行
            BandPowerAnalysisButton_Click(sender, e);
        }

        #endregion

        #region ひよこ/人間モード切り替え

        /// <summary>
        /// モード切り替えボタンの表示を更新
        /// </summary>
        private void UpdateModeToggleButton()
        {
            if (isChickMode)
            {
                modeToggleButton.Text = "🐣ひよこモード";
                modeToggleButton.BackColor = Color.LightBlue;
            }
            else
            {
                modeToggleButton.Text = "👨人間モード";
                modeToggleButton.BackColor = Color.LightGreen;
            }
        }

        /// <summary>
        /// ユーザーモードに応じたUI表示の更新
        /// </summary>
        private void UpdateUIForUserMode()
        {
            if (_currentMode == ModeSelectionForm.AnalysisMode.Heart)
            {
                if (isChickMode)
                {
                    // ひよこモード: 簡単な表示 + HF/LF設定
                    ApplyChickModeUI();
                    ApplyChickHeartRateSettings();
                    UpdateHeartRateCheckBoxLabels();
                }
                else
                {
                    // 人間モード: 詳細な表示 + HF/LF設定
                    ApplyHumanModeUI();
                    ApplyHumanHeartRateSettings();
                    UpdateHeartRateCheckBoxLabels();
                }
            }
            else
            {
                // 脳波モードは従来通り
                if (isChickMode)
                {
                    ApplyChickModeUI();
                }
                else
                {
                    ApplyHumanModeUI();
                }
            }
        }

        /// <summary>
        /// ひよこモード用のUI設定（簡単表示）
        /// </summary>
        private void ApplyChickModeUI()
        {
            // フォントサイズを大きく
            var chickFont = new Font(this.Font.FontFamily, this.Font.Size + 2, FontStyle.Bold);

            // ボタンテキストを統一
            if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
            {
                spectrogramExecuteButton.Text = "スペクトログラム表示";
                spectrogramExecuteButton.Font = chickFont;
                bandPowerSelectButton.Text = "バンドパワー";
            }
            else
            {
                spectrogramExecuteButton.Text = "スペクトログラム表示";
                spectrogramExecuteButton.Font = chickFont;
                bandPowerSelectButton.Text = "バンドパワー";
            }

            bandPowerGenerateButton.Text = "✨グラフ作成";
            bandPowerBackButton.Text = "⬅戻る";

            // プログレスバーを見やすく
            if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
            {
                spectrogramProgressBar.Style = ProgressBarStyle.Continuous;
            }
            else
            {
                spectrogramProgressBar.Style = ProgressBarStyle.Continuous;
            }
        }

        /// <summary>
        /// 人間モード用のUI設定（詳細表示）
        /// </summary>
        private void ApplyHumanModeUI()
        {
            // フォントを通常に戻す
            var normalFont = new Font(this.Font.FontFamily, this.Font.Size, FontStyle.Regular);

            // ボタンテキストを統一
            if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
            {
                spectrogramExecuteButton.Text = "スペクトログラム表示";
                spectrogramExecuteButton.Font = normalFont;
                bandPowerSelectButton.Text = "バンドパワー";
            }
            else
            {
                spectrogramExecuteButton.Text = "スペクトログラム表示";
                spectrogramExecuteButton.Font = normalFont;
                bandPowerSelectButton.Text = "バンドパワー";
            }

            bandPowerGenerateButton.Text = "グラフ生成";
            bandPowerBackButton.Text = "戻る";

            // プログレスバーを標準に
            if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
            {
                spectrogramProgressBar.Style = ProgressBarStyle.Continuous;
            }
            else
            {
                spectrogramProgressBar.Style = ProgressBarStyle.Continuous;
            }
        }

        /// <summary>
        /// ひよこモード用心拍HF/LF設定（C++コード準拠）
        /// chick: HF=1.2-0.4Hz, LF=0.1-0.04Hz
        /// </summary>
        private void ApplyChickHeartRateSettings()
        {
            // HF設定 (1.2-0.4Hz)
            SetFreqNumericFromHz(freqMinNumeric, 0.4);
            SetFreqNumericFromHz(freqMaxNumeric, 1.2);

            System.Diagnostics.Debug.WriteLine("ひよこモード心拍設定適用: HF=0.4-1.2Hz");

            // LF設定用の追加パラメータ（後で実装予定）
            // LF: 0.1-0.04Hz
        }

        /// <summary>
        /// 人間モード用心拍HF/LF設定（C++コード準拠）
        /// human: HF=0.4-0.15Hz, LF=0.15-0.04Hz  
        /// </summary>
        private void ApplyHumanHeartRateSettings()
        {
            // HF設定 (0.4-0.15Hz)
            SetFreqNumericFromHz(freqMinNumeric, 0.15);
            SetFreqNumericFromHz(freqMaxNumeric, 0.4);

            System.Diagnostics.Debug.WriteLine("人間モード心拍設定適用: HF=0.15-0.4Hz");

            // LF設定用の追加パラメータ（後で実装予定）
            // LF: 0.15-0.04Hz
        }

        #endregion

        /// <summary>
        /// スペクトログラムの軸タイトルを強制的に正しく設定
        /// </summary>
        private void ForceFixSpectrogramAxisTitles()
        {
            try
            {
                if (chartPlotView?.Model != null && chartPlotView.Model.Axes != null)
                {
                    // 各軸のタイトルを強制的に設定
                    foreach (var axis in chartPlotView.Model.Axes)
                    {
                        switch (axis.Position)
                        {
                            case OxyPlot.Axes.AxisPosition.Bottom:
                                axis.Title = "時間 (秒)";
                                break;
                            case OxyPlot.Axes.AxisPosition.Left:
                                axis.Title = "周波数 (Hz)";
                                break;
                            case OxyPlot.Axes.AxisPosition.Right:
                                if (axis is OxyPlot.Axes.LinearColorAxis)
                                {
                                    axis.Title = "パワー (dB)";
                                }
                                break;
                        }
                    }

                    // プロットの再描画を強制
                    chartPlotView.Model.InvalidatePlot(true);

                    System.Diagnostics.Debug.WriteLine("スペクトログラム軸タイトルを強制修正しました");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"軸タイトル修正エラー: {ex.Message}");
            }
        }

        private void settingsTableLayoutPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void settingsPanel_Paint_1(object sender, PaintEventArgs e)
        {

        }
    }
}
