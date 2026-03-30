using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.Input;
using ホ号計画.Models;
using ホ号計画.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using OxyPlot.Legends;

namespace ホ号計画.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly FileService _fileService;
        private readonly AnalysisService _analysisService;

        private SignalData _currentSignal;
        private AnalysisSettings _analysisSettings;
        private SpectrogramData _spectrogramData;
        private FilterAnalysisResult _filterAnalysisResult;
        private BandPowerAnalysisResult _bandPowerResult;
        private BandPowerLineData _bandPowerLineData;
        private TimeIntervalManager _intervalManager;
        private PlotModel _spectrogramPlot;
        private PlotModel _signalPlot;
        private PlotModel _filterPlot;
        private PlotModel _bandPowerPlot;
        private bool _isEegMode = true; // デフォルトはEEGモード
        private bool _isLogScale = false;
        private double _preLogMinFreq = 0.0;   // ログ切替前の DisplayMinFreq
        private double _logAutoMinFreq = 0.0;  // ログ時に自動設定した下限値

        public SignalData CurrentSignal
        {
            get => _currentSignal;
            set => SetProperty(ref _currentSignal, value);
        }

        public AnalysisSettings AnalysisSettings
        {
            get => _analysisSettings;
            set => SetProperty(ref _analysisSettings, value);
        }

        public bool IsEegMode
        {
            get => _isEegMode;
            set => SetProperty(ref _isEegMode, value);
        }

        // バンド選択状態
        public bool IsDeltaSelected { get; set; } = true;
        public bool IsThetaSelected { get; set; } = true;
        public bool IsAlphaSelected { get; set; } = true;
        public bool IsBetaSelected { get; set; } = true;
        public bool IsGammaSelected { get; set; } = true;
        public bool IsLFSelected { get; set; } = true;
        public bool IsHFSelected { get; set; } = true;
        public bool IsLfHfRatioSelected { get; set; } = true;

        public SpectrogramData SpectrogramData
        {
            get => _spectrogramData;
            set => SetProperty(ref _spectrogramData, value);
        }

        public PlotModel SpectrogramPlot
        {
            get => _spectrogramPlot;
            set => SetProperty(ref _spectrogramPlot, value);
        }

        public PlotModel SignalPlot
        {
            get => _signalPlot;
            set => SetProperty(ref _signalPlot, value);
        }

        public FilterAnalysisResult FilterAnalysisResult
        {
            get => _filterAnalysisResult;
            set => SetProperty(ref _filterAnalysisResult, value);
        }

        public PlotModel FilterPlot
        {
            get => _filterPlot;
            set => SetProperty(ref _filterPlot, value);
        }

        public BandPowerAnalysisResult BandPowerResult
        {
            get => _bandPowerResult;
            set => SetProperty(ref _bandPowerResult, value);
        }

        public BandPowerLineData BandPowerLineData
        {
            get => _bandPowerLineData;
            set => SetProperty(ref _bandPowerLineData, value);
        }

        public TimeIntervalManager IntervalManager
        {
            get => _intervalManager;
            set => SetProperty(ref _intervalManager, value);
        }

        public PlotModel BandPowerPlot
        {
            get => _bandPowerPlot;
            set => SetProperty(ref _bandPowerPlot, value);
        }

        public bool EnableInterpolation
        {
            get => _analysisSettings.EnableInterpolation;
            set 
            {
                if (_analysisSettings.EnableInterpolation != value)
                {
                    _analysisSettings.EnableInterpolation = value;
                    OnPropertyChanged();
                    
                    // スペクトログラムが既に表示されている場合は更新
                    if (SpectrogramData != null)
                    {
                        UpdateSpectrogramPlot();
                    }
                }
            }
        }

        public bool IsLogScale
        {
            get => _isLogScale;
            set
            {
                if (_isLogScale == value) return;
                _isLogScale = value;
                OnPropertyChanged();
                if (value)
                    SwitchToLogScale();
                else
                    SwitchToLinearScale();
            }
        }

        public MainViewModel()
        {
            _fileService = new FileService();
            _analysisService = new AnalysisService();
            _analysisSettings = new AnalysisSettings();
            _intervalManager = new TimeIntervalManager();
            
            InitializePlots();
        }

        private void InitializePlots()
        {
            SignalPlot = new PlotModel { Title = "時系列信号" };
            SignalPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "時間 (秒)" });
            SignalPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "振幅" });

            SpectrogramPlot = new PlotModel { Title = "" };
            SpectrogramPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "時間 (秒)", Key = "TimeAxis" });
            SpectrogramPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "周波数 (Hz)", Key = "FrequencyAxis" });
            
            // ColorAxis for HeatMap
            var colorAxis = new LinearColorAxis
            {
                Key = "HeatmapColors",
                Position = AxisPosition.Right,
                Title = "パワー (dB)",
                Palette = OxyPalettes.Jet(256)
            };
            SpectrogramPlot.Axes.Add(colorAxis);

            FilterPlot = new PlotModel { Title = "フィルタ結果" };
            FilterPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "時間 (秒)" });
            FilterPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "振幅" });

            BandPowerPlot = new PlotModel 
            { 
                Title = "脳波バンドパワー正規化結果"
            };
            BandPowerPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "時間 (秒)" });
            BandPowerPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "正規化パワー (%)" });
            
            // OxyPlot 2.2.0 対応: 手動で凡例を追加
            BandPowerPlot.Legends.Add(new Legend()
            {
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Inside,
                LegendOrientation = LegendOrientation.Vertical,
                LegendFontSize = 12
            });
            
            // 起動時にテストグラフを表示
            CreateInitialTestPlot();
        }

        private void CreateInitialTestPlot()
        {
            try
            {
                // 起動時に表示される基本的なテストスペクトラム
                var testSeries = new LineSeries 
                { 
                    Title = "サンプルスペクトラム", 
                    Color = OxyColors.Blue,
                    StrokeThickness = 2
                };
                
                // 脳波の典型的な周波数成分をシミュレート
                for (int f = 1; f <= 50; f++)
                {
                    double freq = f;
                    double power = -60; // ベースライン
                    
                    // α波 (8-13Hz) にピーク
                    if (freq >= 8 && freq <= 13)
                        power += 20 * Math.Exp(-Math.Pow((freq - 10) / 2, 2));
                    
                    // θ波 (4-8Hz) に小さなピーク
                    if (freq >= 4 && freq <= 8)
                        power += 10 * Math.Exp(-Math.Pow((freq - 6) / 1.5, 2));
                    
                    // β波 (13-30Hz) に小さなピーク
                    if (freq >= 13 && freq <= 30)
                        power += 8 * Math.Exp(-Math.Pow((freq - 20) / 5, 2));
                    
                    testSeries.Points.Add(new DataPoint(freq, power));
                }
                
                SpectrogramPlot.Series.Add(testSeries);
                
                // 軸の範囲を設定
                SpectrogramPlot.Axes[0].Minimum = 0;
                SpectrogramPlot.Axes[0].Maximum = 50;
                SpectrogramPlot.Axes[1].Minimum = -70;
                SpectrogramPlot.Axes[1].Maximum = -30;
                
                StatusMessage = "サンプルスペクトラムを表示中 - CSVファイルを読み込むか脳波解析を実行してください";
            }
            catch (Exception ex)
            {
                StatusMessage = $"初期化エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        private void LoadFile()
        {
            try
            {
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                    openFileDialog.Title = "CSVファイルを選択";

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        IsBusy = true;
                        StatusMessage = "ファイル読み込み中...";

                        CurrentSignal = _fileService.LoadCsvFile(openFileDialog.FileName, 1000.0); // 1kHzサンプリング周波数（固定）
                        
                        // ファイル読み込み後、未編集の解析変数のみ自動設定
                        SetInitialSettingsIfNotEdited(CurrentSignal);
                        
                        // UIにも設定を反映
                        OnPropertyChanged(nameof(AnalysisSettings));
                        OnPropertyChanged(nameof(EnableInterpolation));
                        
                        UpdateSignalPlot();
                        StatusMessage = $"読み込み完了: {CurrentSignal.FileName} ({CurrentSignal.SampleCount} サンプル, 解析範囲: 0-{AnalysisSettings.MaxTime:F1}秒, 時間解像度: {AnalysisSettings.TimeResolution:F4}秒)";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"エラー: {ex.Message}";
                MessageBox.Show($"ファイル読み込みエラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async void AnalyzeSignal()
        {
            if (CurrentSignal?.TimeData == null || CurrentSignal.TimeData.Length == 0)
            {
                MessageBox.Show("先にCSVファイルを読み込んでください。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "スペクトログラム計算中...";

                // 重い処理を別スレッドで実行
                var signalData = CurrentSignal;
                var settings = AnalysisSettings;
                
                // データの妥当性を再チェック
                if (signalData?.TimeData == null || signalData.TimeData.Length == 0)
                {
                    throw new InvalidOperationException("信号データが無効です。データを再度読み込んでください。");
                }
                
                System.Diagnostics.Debug.WriteLine($"解析開始: データ長={signalData.TimeData.Length}, サンプリング周波数={signalData.SamplingRate}");
                
                var progress = new Progress<int>(percent => ProgressValue = percent);
                // 時間範囲でデータを抽出（必要に応じて）
                signalData = ExtractTimeRange(signalData, settings.MinTime, settings.MaxTime);

                // 加算平均スペクトログラム生成（統合処理）
                if (settings.EnableEnsembleAveraging)
                {
                    StatusMessage = "加算平均スペクトログラム計算中...";
                }
                else
                {
                    StatusMessage = "スペクトログラム計算中...";
                }
                
                SpectrogramData = await Task.Run(() => _analysisService.GenerateEnsembleAveragedSpectrogram(signalData, settings, progress));
                
                UpdateSpectrogramPlot();
                
                StatusMessage = $"解析完了: {SpectrogramData.TimeSteps}×{SpectrogramData.FrequencyBins} データポイント";
            }
            catch (Exception ex)
            {
                StatusMessage = $"エラー: {ex.Message}";
                MessageBox.Show($"解析エラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateSignalPlot()
        {
            if (CurrentSignal?.TimeData == null)
                return;

            SignalPlot.Series.Clear();
            var series = new LineSeries { Title = "信号", Color = OxyColors.Blue };

            for (int i = 0; i < Math.Min(CurrentSignal.TimeData.Length, 10000); i++)
            {
                double time = i / CurrentSignal.SamplingRate;
                double amplitude = SafeDouble(CurrentSignal.TimeData[i], 0.0);
                
                // 異常に大きな値を制限
                amplitude = Math.Max(-1e6, Math.Min(1e6, amplitude));
                
                series.Points.Add(new DataPoint(time, amplitude));
            }

            SignalPlot.Series.Add(series);
            SignalPlot.InvalidatePlot(true);
        }

        private void UpdateSpectrogramPlot()
        {
            try
            {
                SpectrogramPlot.Series.Clear();

                // Form1 が SpectrogramPlot.Axes.Clear() した後に Key=null の Bottom/Left 軸を
                // 残したまま更新することがある。その場合 HeatMapSeries が誤った軸を
                // DefaultXAxis として使ってしまうため、スペクトログラム描画前に軸を正常状態に戻す。
                RestoreSpectrogramAxes();
                
                if (SpectrogramData?.PowerMatrix == null)
                {
                    // データがない場合はテスト線を表示
                    CreateTestPlot();
                    StatusMessage = "データなし - テスト表示";
                    return;
                }
                
                // データのサイズ検証
                if (SpectrogramData.TimeSteps <= 0 || SpectrogramData.FrequencyBins <= 0)
                {
                    CreateTestPlot();
                    StatusMessage = "データサイズ不正 - テスト表示";
                    return;
                }
                
                // スペクトログラムを平均スペクトラムとして表示
                CreateScatterSpectrogramPlot();
                
                SpectrogramPlot.InvalidatePlot(true);
            }
            catch (Exception ex)
            {
                StatusMessage = $"グラフ更新エラー: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"UpdateSpectrogramPlot Error: {ex}");
                
                // エラー時は確実に表示されるテスト表示
                CreateTestPlot();
            }
        }

        /// <summary>
        /// スペクトログラム描画に必要な軸を正常な状態に復元する。
        /// Form1 が SpectrogramPlot.Axes.Clear() を呼び出した後に
        /// Key を持たない余分な Bottom/Left 軸が残ることがあるため、
        /// それらを除去し TimeAxis / FrequencyAxis / HeatmapColors を確保する。
        /// </summary>
        private void RestoreSpectrogramAxes()
        {
            // Key=null の Bottom 軸を全て削除（EEG/バンドパワー表示の残骸）
            foreach (var ax in SpectrogramPlot.Axes.Where(a => a.Position == AxisPosition.Bottom && string.IsNullOrEmpty(a.Key)).ToList())
                SpectrogramPlot.Axes.Remove(ax);

            // Key=null の Left 軸を全て削除（同上）
            foreach (var ax in SpectrogramPlot.Axes.Where(a => a.Position == AxisPosition.Left && string.IsNullOrEmpty(a.Key)).ToList())
                SpectrogramPlot.Axes.Remove(ax);

            // TimeAxis がなければ追加
            if (!SpectrogramPlot.Axes.Any(a => a.Key == "TimeAxis"))
                SpectrogramPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "時間 (秒)", Key = "TimeAxis" });

            // FrequencyAxis がなければ追加（対数スケール設定を考慮）
            if (!SpectrogramPlot.Axes.Any(a => a.Key == "FrequencyAxis"))
                ReplaceFrequencyAxis(isLog: _isLogScale);

            // ColorAxis がなければ追加
            if (!SpectrogramPlot.Axes.Any(a => a.Key == "HeatmapColors"))
                SpectrogramPlot.Axes.Add(new LinearColorAxis { Key = "HeatmapColors", Position = AxisPosition.Right, Title = "パワー (dB)", Palette = OxyPalettes.Jet(256) });
        }

        private void CreateScatterSpectrogramPlot()
        {
            // 最優先デバッグ: PowerMatrix の min/max をログ出力
            var flatPowerMatrix = SpectrogramData.PowerMatrix.Cast<double>().ToArray();
            var actualMin = flatPowerMatrix.Min();
            var actualMax = flatPowerMatrix.Max();
            System.Diagnostics.Debug.WriteLine($"PowerMatrix actual: min={actualMin:F6}, max={actualMax:F6}");
            System.Console.WriteLine($"PowerMatrix actual: min={actualMin:F6}, max={actualMax:F6}");

            // [AxesDiag] 関数先頭: 現在の軸一覧 (VS Output ウィンドウで確認可能)
            System.Diagnostics.Debug.WriteLine($"[AxesDiag] ===== CreateScatterSpectrogramPlot 開始 =====");
            System.Diagnostics.Debug.WriteLine($"[AxesDiag] 軸数={SpectrogramPlot.Axes.Count}");
            foreach (var ax in SpectrogramPlot.Axes)
                System.Diagnostics.Debug.WriteLine($"[AxesDiag]   Key={(ax.Key ?? "(null)")}, Pos={ax.Position}, Type={ax.GetType().Name}");
            
            // デバッグ情報を出力
            StatusMessage = $"データサイズ: {SpectrogramData.TimeSteps}x{SpectrogramData.FrequencyBins}, パワー範囲: {SpectrogramData.MinPower:F2} - {SpectrogramData.MaxPower:F2}, 実際値: {actualMin:F2} - {actualMax:F2}";
            
            // ColorAxisが存在しない場合は追加
            var colorAxis = SpectrogramPlot.Axes.FirstOrDefault(a => a.Key == "HeatmapColors");
            if (colorAxis == null)
            {
                colorAxis = new LinearColorAxis
                {
                    Key = "HeatmapColors",
                    Position = AxisPosition.Right,
                    Title = "パワー (dB)",
                    Palette = OxyPalettes.Jet(256)
                };
                SpectrogramPlot.Axes.Add(colorAxis);
            }
            
            try 
            {
                // デバッグ：配列サイズ確認
                var (heatMapData, clippedTimeStart, clippedTimeEnd) = ConvertPowerMatrixToHeatMapData();

                // TimeSteps=1の場合、OxyPlot HeatMapSeriesのゼロ除算を回避するため行を複製して[2, freqBins]にする
                if (heatMapData.GetLength(0) == 1)
                {
                    int freqBins = heatMapData.GetLength(1);
                    var padded = new double[2, freqBins];
                    for (int f = 0; f < freqBins; f++)
                    {
                        padded[0, f] = heatMapData[0, f];
                        padded[1, f] = heatMapData[0, f];
                    }
                    heatMapData = padded;
                }

                var debugMsg = $"HeatMapData: {heatMapData.GetLength(0)}x{heatMapData.GetLength(1)}, TimeSteps: {SpectrogramData.TimeSteps}, FreqBins: {SpectrogramData.FrequencyBins}";
                System.Diagnostics.Debug.WriteLine(debugMsg);
                System.Console.WriteLine(debugMsg);
                StatusMessage += $" | {debugMsg}";
                
                // Step 3: Python互換IQRカラースケール適用
                var (vmin, vmax) = CalculateIQRScale(heatMapData);
                colorAxis.Minimum = vmin;
                colorAxis.Maximum = vmax;
                System.Diagnostics.Debug.WriteLine($"IQR Scale: vmin={vmin:F1} dB, vmax={vmax:F1} dB");
                System.Console.WriteLine($"IQR Scale: vmin={vmin:F1} dB, vmax={vmax:F1} dB");
                StatusMessage += $" | IQR: {vmin:F1}-{vmax:F1}dB";
                
                // 2Dスペクトログラム（ヒートマップ）として表示
                // UI設定された周波数範囲を使用（ナイキスト周波数制限を考慮）
                double nyquistFreq = CurrentSignal.SamplingRate / 2.0;
                double minDisplayFreq = AnalysisSettings.DisplayMinFreq >= 0 ? AnalysisSettings.DisplayMinFreq : SpectrogramData.FrequencyAxis[0];
                double maxDisplayFreq = AnalysisSettings.DisplayMaxFreq > 0 ? AnalysisSettings.DisplayMaxFreq : 40.0;
                
                // ナイキスト周波数制限の適用と警告
                if (maxDisplayFreq > nyquistFreq)
                {
                    System.Diagnostics.Debug.WriteLine($"警告: UI設定周波数{maxDisplayFreq}Hzがナイキスト周波数{nyquistFreq:F1}Hzを超過。制限します。");
                    System.Console.WriteLine($"ナイキスト制限: UI設定{maxDisplayFreq}Hz → 実際{nyquistFreq:F1}Hz (サンプリング{CurrentSignal.SamplingRate}Hz)");
                    maxDisplayFreq = nyquistFreq;
                }
                
                // 最小周波数のbin計算（安全性チェック付き）
                int minFreqBin = Array.FindIndex(SpectrogramData.FrequencyAxis, f => f >= minDisplayFreq);
                if (minFreqBin == -1) minFreqBin = 0;
                minFreqBin = Math.Max(0, Math.Min(minFreqBin, SpectrogramData.FrequencyAxis.Length - 1));
                double actualMinFreq = SpectrogramData.FrequencyAxis[minFreqBin];
                
                // 最大周波数のbin計算（安全性チェック付き）
                int maxFreqBin = Array.FindIndex(SpectrogramData.FrequencyAxis, f => f > maxDisplayFreq);
                if (maxFreqBin == -1) maxFreqBin = SpectrogramData.FrequencyBins;
                maxFreqBin = Math.Max(minFreqBin + 1, Math.Min(maxFreqBin, SpectrogramData.FrequencyAxis.Length));
                double actualMaxFreq = maxFreqBin > 0 ? SpectrogramData.FrequencyAxis[maxFreqBin - 1] : SpectrogramData.FrequencyAxis[SpectrogramData.FrequencyBins - 1];
                
                // 周波数範囲の妥当性チェック（min >= max の場合のみ修正）
                if (actualMinFreq >= actualMaxFreq)
                {
                    System.Diagnostics.Debug.WriteLine($"警告: 無効な周波数範囲 - min={actualMinFreq}, max={actualMaxFreq}");

                    // 最小限の範囲を確保（周波数分解能の1ビン分）
                    double freqResolution = SpectrogramData.FrequencyAxis.Length > 1
                        ? SpectrogramData.FrequencyAxis[1] - SpectrogramData.FrequencyAxis[0]
                        : 0.1;
                    actualMaxFreq = actualMinFreq + freqResolution;

                    System.Diagnostics.Debug.WriteLine($"修正後周波数範囲: {actualMinFreq:F2} - {actualMaxFreq:F2} Hz");
                }
                
                // デバッグ出力：HeatMapSeries範囲確認
                System.Diagnostics.Debug.WriteLine($"HeatMapSeries周波数範囲: DisplayMin/Max={AnalysisSettings.DisplayMinFreq}/{AnalysisSettings.DisplayMaxFreq}, actual={actualMinFreq:F2}-{actualMaxFreq:F2}");
                
                // 座標値の検証（時間軸の安全性チェック）
                if (SpectrogramData.TimeAxis == null || SpectrogramData.TimeAxis.Length < SpectrogramData.TimeSteps)
                {
                    System.Diagnostics.Debug.WriteLine("警告: 時間軸データが不正です");
                    return;
                }
                
                double x0 = clippedTimeStart;
                double x1 = clippedTimeEnd;

                // TimeSteps=1の場合、0〜totalRangeの幅でHeatMap描画
                if (heatMapData.GetLength(0) <= 1)
                {
                    double totalRange = AnalysisSettings.BasicTimeUnit * AnalysisSettings.DivisionCount;
                    x0 = AnalysisSettings.MinTime;
                    x1 = Math.Min(AnalysisSettings.MaxTime, totalRange);
                }

                // 周波数ビン幅を計算（各セルの下端がビンのスタート位置と一致するように半ビン幅ずらす）
                double freqBinWidth = SpectrogramData.FrequencyAxis.Length > 1
                    ? SpectrogramData.FrequencyAxis[1] - SpectrogramData.FrequencyAxis[0]
                    : 1.0;
                double y0 = actualMinFreq + freqBinWidth / 2.0;
                double y1 = actualMaxFreq + freqBinWidth / 2.0;

                // 対数スケール時は LinearAxis + log10 座標変換方式のため Y0/Y1 を log10 変換
                if (_isLogScale)
                {
                    double safeY0 = Math.Max(y0, GetFrequencyResolution());
                    double safeY1 = Math.Max(y1, safeY0 * 2);
                    y0 = Math.Log10(safeY0);
                    y1 = Math.Log10(safeY1);
                }
                
                // 描画安全性の包括的チェック
                if (!IsValidHeatMapCoordinates(x0, x1, y0, y1))
                {
                    System.Diagnostics.Debug.WriteLine($"警告: 無効な描画座標検出 - X0={x0}, X1={x1}, Y0={y0}, Y1={y1}");
                    System.Diagnostics.Debug.WriteLine($"  Width={Math.Abs(x1-x0)}, Height={Math.Abs(y1-y0)}");
                    
                    // フォールバック: 平均スペクトラムで表示
                    CreateAverageSpectrumPlot();
                    return;
                }
                
                // データ配列の検証
                if (heatMapData == null || !ValidateHeatMapData(heatMapData))
                {
                    System.Diagnostics.Debug.WriteLine("警告: 無効なHeatMapData検出");
                    return; // 描画をスキップ
                }

                // Series.Add()の前にTimeAxisが確実に存在するよう更新・再生成する
                // （OxyPlotはXAxisKey非nullの場合にGetAxis()でTimeAxisを厳格検索するため）
                UpdateAxisRanges();

                // [AxesDiag] UpdateAxisRanges 直後
                System.Diagnostics.Debug.WriteLine($"[AxesDiag] UpdateAxisRanges後 軸数={SpectrogramPlot.Axes.Count}");
                foreach (var ax in SpectrogramPlot.Axes)
                    System.Diagnostics.Debug.WriteLine($"[AxesDiag]   Key={(ax.Key ?? "(null)")}, Pos={ax.Position}");

                // HeatMapSeries座標を軸範囲にクリップ（OverflowException防止）
                var clipFreqAxis = SpectrogramPlot.Axes.FirstOrDefault(a => a.Key == "FrequencyAxis");
                var clipTimeAxis = SpectrogramPlot.Axes.FirstOrDefault(a => a.Key == "TimeAxis");
                if (clipFreqAxis != null)
                {
                    double axisYMin = clipFreqAxis.Minimum;
                    double axisYMax = clipFreqAxis.Maximum;

                    // HeatMapが軸範囲と完全に重ならない場合はフォールバック
                    if (y1 <= axisYMin || y0 >= axisYMax)
                    {
                        System.Diagnostics.Debug.WriteLine($"警告: HeatMap Y範囲({y0:F4}~{y1:F4})が軸範囲({axisYMin:F4}~{axisYMax:F4})と重ならない");
                        System.Diagnostics.Debug.WriteLine("→ 周波数分解能が表示範囲に対して粗すぎます。DFT時間幅を増やしてください。");
                        StatusMessage = "警告: 周波数分解能が表示範囲に対して粗すぎます。DFT時間幅(秒)を増やしてください。";
                        CreateAverageSpectrumPlot();
                        return;
                    }

                    // 部分的に軸範囲外の場合はクリップ
                    if (y0 < axisYMin) y0 = axisYMin;
                    if (y1 > axisYMax) y1 = axisYMax;
                }
                if (clipTimeAxis != null)
                {
                    double axisXMin = clipTimeAxis.Minimum;
                    double axisXMax = clipTimeAxis.Maximum;
                    if (x0 < axisXMin) x0 = axisXMin;
                    if (x1 > axisXMax) x1 = axisXMax;
                }

                // クリップ後の座標妥当性チェック
                if (y1 - y0 < 1e-10 || x1 - x0 < 1e-10)
                {
                    System.Diagnostics.Debug.WriteLine($"警告: クリップ後の描画範囲が小さすぎます (dx={x1-x0:E2}, dy={y1-y0:E2})");
                    StatusMessage = "警告: 表示範囲が小さすぎます。周波数範囲またはDFT時間幅を調整してください。";
                    CreateAverageSpectrumPlot();
                    return;
                }

                try
                {
                    // デバッグ: HeatMapSeries 座標値と軸範囲を出力
                    var dbgFreqAxis = SpectrogramPlot.Axes.FirstOrDefault(a => a.Key == "FrequencyAxis");
                    var dbgTimeAxis = SpectrogramPlot.Axes.FirstOrDefault(a => a.Key == "TimeAxis");
                    System.Diagnostics.Debug.WriteLine($"[HeatMap] 座標: X0={x0:F4}, X1={x1:F4}, Y0={y0:F4}, Y1={y1:F4}");
                    System.Diagnostics.Debug.WriteLine($"[HeatMap] データ: {heatMapData.GetLength(0)}x{heatMapData.GetLength(1)}");
                    System.Diagnostics.Debug.WriteLine($"[HeatMap] 時間軸: Min={dbgTimeAxis?.Minimum:F4}, Max={dbgTimeAxis?.Maximum:F4}");
                    System.Diagnostics.Debug.WriteLine($"[HeatMap] 周波数軸: Min={dbgFreqAxis?.Minimum:F4}, Max={dbgFreqAxis?.Maximum:F4}, Type={dbgFreqAxis?.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"[HeatMap] IsLogScale={_isLogScale}");
                    System.Console.WriteLine($"[HeatMap] X0={x0:F4}, X1={x1:F4}, Y0={y0:F4}, Y1={y1:F4}, Data={heatMapData.GetLength(0)}x{heatMapData.GetLength(1)}, FreqAxisMin={dbgFreqAxis?.Minimum:F4}, FreqAxisMax={dbgFreqAxis?.Maximum:F4}");

                    var heatMapSeries = new HeatMapSeries
                    {
                        X0 = x0,
                        X1 = x1,
                        Y0 = y0,
                        Y1 = y1,
                        Data = heatMapData,
                        Interpolate = false,
                        ColorAxisKey = "HeatmapColors"
                    };

                    SpectrogramPlot.Series.Add(heatMapSeries);
                    System.Diagnostics.Debug.WriteLine("HeatMapSeries正常に追加されました");

                    // [AxesDiag] Series.Add 直後
                    System.Diagnostics.Debug.WriteLine($"[AxesDiag] Series.Add後 軸数={SpectrogramPlot.Axes.Count}");
                    foreach (var ax in SpectrogramPlot.Axes)
                        System.Diagnostics.Debug.WriteLine($"[AxesDiag]   Key={(ax.Key ?? "(null)")}, Pos={ax.Position}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"HeatMapSeries作成エラー: {ex.Message}");
                    // フォールバック: 平均スペクトラムで表示
                    CreateAverageSpectrumPlot();
                    return;
                }
                
                // デバッグ: HeatMapSeries座標設定確認
                System.Diagnostics.Debug.WriteLine($"HeatMapSeries座標: X0={x0:F3}s, X1={x1:F3}s, Y0={y0:F1}Hz, Y1={y1:F1}Hz");
                System.Console.WriteLine($"HeatMapSeries座標: X0={x0:F3}s, X1={x1:F3}s, Y0={y0:F1}Hz, Y1={y1:F1}Hz");
                System.Diagnostics.Debug.WriteLine($"軸範囲（Series.Add前に設定済み）: {AnalysisSettings.MinTime:F1}-{AnalysisSettings.MaxTime:F1}秒");
                
                // 時間軸のタイトルと位置のみ設定（範囲はユーザー設定を保持）
                var timeAxis = SpectrogramPlot.Axes.FirstOrDefault(a => a.Key == "TimeAxis") as LinearAxis;
                if (timeAxis != null)
                {
                    timeAxis.Title = "時間 (秒)"; // タイトルを確実に設定
                    timeAxis.Position = AxisPosition.Bottom; // 位置も確実に設定
                }
                
                var freqAxis = SpectrogramPlot.Axes.FirstOrDefault(a => a.Key == "FrequencyAxis");
                if (freqAxis != null)
                {
                    // 軸範囲はUpdateAxisRanges()のユーザー設定値を尊重（上書きしない）
                    freqAxis.Title = "周波数 (Hz)"; // タイトルを確実に設定
                    freqAxis.Position = AxisPosition.Left; // 位置も確実に設定
                }
                
                // カラー軸も確実に設定
                var existingColorAxis = SpectrogramPlot.Axes.FirstOrDefault(a => a.Key == "HeatmapColors") as LinearColorAxis;
                if (existingColorAxis != null)
                {
                    existingColorAxis.Title = "パワー (dB)";
                    existingColorAxis.Position = AxisPosition.Right;
                }
                
                // プロットタイトルをクリア
                SpectrogramPlot.Title = "";
                
                // 全ての軸を強制的に正しいタイトルに設定（UI表示修正）
                foreach (var axis in SpectrogramPlot.Axes)
                {
                    if (axis.Position == AxisPosition.Bottom)
                    {
                        axis.Title = "時間 (秒)";
                    }
                    else if (axis.Position == AxisPosition.Left)
                    {
                        axis.Title = "周波数 (Hz)";
                    }
                    else if (axis.Position == AxisPosition.Right && axis is LinearColorAxis)
                    {
                        axis.Title = "パワー (dB)";
                    }
                }
                
                // プロットの再描画を強制
                SpectrogramPlot.InvalidatePlot(true);
                
                StatusMessage += " | スペクトログラム表示完了";
            }
            catch (Exception ex)
            {
                // エラー時は平均スペクトラムで表示
                CreateAverageSpectrumPlot();
                StatusMessage += $" | ヒートマップエラー、平均スペクトラムで表示: {ex.Message}";
            }
        }

        /// <summary>
        /// PowerMatrixをHeatMapSeries用のデータに変換
        /// </summary>
        private (double[,] data, double timeStart, double timeEnd) ConvertPowerMatrixToHeatMapData()
        {
            // 1. UI設定に基づく時間範囲制限
            double minTime = AnalysisSettings.MinTime;
            double maxTime = AnalysisSettings.MaxTime > 0 ? AnalysisSettings.MaxTime : 300.0;
            var (timeClipped, minTimeBin, maxTimeBinExcl) = LimitTimeRange(SpectrogramData.PowerMatrix, SpectrogramData.TimeAxis, minTime, maxTime);
            double actualTimeStart = SpectrogramData.TimeAxis[minTimeBin];
            double actualTimeEnd = SpectrogramData.TimeAxis[Math.Min(maxTimeBinExcl - 1, SpectrogramData.TimeAxis.Length - 1)];

            // 2. UI設定に基づく周波数範囲制限
            double maxFreq = AnalysisSettings.DisplayMaxFreq > 0 ? AnalysisSettings.DisplayMaxFreq : 40.0;
            double minFreq = AnalysisSettings.DisplayMinFreq >= 0 ? AnalysisSettings.DisplayMinFreq : 0.0;
            var limitedMatrix = LimitFrequencyRange(timeClipped, SpectrogramData.FrequencyAxis, minFreq, maxFreq);

            // 3. ダウンサンプリング制限を撤廃（元のサイズをそのまま使用）
            int maxTimeSteps = limitedMatrix.GetLength(0);  // 制限なし
            int maxFreqSteps = limitedMatrix.GetLength(1);  // 制限なし
            var downsampledMatrix = DownsampleForDisplay(limitedMatrix, maxTimeSteps, maxFreqSteps);

            System.Diagnostics.Debug.WriteLine($"データサイズ: {downsampledMatrix.GetLength(0)}x{downsampledMatrix.GetLength(1)}");

            // 4. HeatMapSeries用に転置 - OxyPlot座標系: [X軸インデックス, Y軸インデックス]
            int timeSteps = downsampledMatrix.GetLength(0);  // 時間方向
            int freqBins = downsampledMatrix.GetLength(1);   // 周波数方向

            // OxyPlot HeatMapSeries: Data[x軸, y軸] = [時間, 周波数] (転置しない)
            var heatMapData = new double[timeSteps, freqBins];

            for (int t = 0; t < timeSteps; t++)
            {
                for (int f = 0; f < freqBins; f++)
                {
                    heatMapData[t, f] = downsampledMatrix[t, f];  // そのままコピー
                }
            }

            return (heatMapData, actualTimeStart, actualTimeEnd);
        }
        
        /// <summary>
        /// 座標値の妥当性をチェック（描画可能範囲に制限）
        /// </summary>
        private static bool IsValidCoordinate(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && 
                   value > -1e6 && value < 1e6 && Math.Abs(value) < 1000000;
        }
        
        /// <summary>
        /// HeatMapSeries座標の描画安全性をチェック
        /// </summary>
        private static bool IsValidHeatMapCoordinates(double x0, double x1, double y0, double y1)
        {
            // 座標値の基本チェック
            if (!IsValidCoordinate(x0) || !IsValidCoordinate(x1) || !IsValidCoordinate(y0) || !IsValidCoordinate(y1))
                return false;
            
            // 幅と高さの計算
            double width = Math.Abs(x1 - x0);
            double height = Math.Abs(y1 - y0);
            
            // 描画サイズの妥当性チェック（System.Drawing.Graphics の制限を考慮）
            if (width <= 0 || height <= 0 || width > 32767 || height > 32767)
                return false;
            
            // アスペクト比の異常値チェック
            if (width / height > 1e6 || height / width > 1e6)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// HeatMapDataの妥当性をチェック
        /// </summary>
        private static bool ValidateHeatMapData(double[,] data)
        {
            if (data == null) return false;
            
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);
            
            if (rows == 0 || cols == 0) return false;
            
            // サイズ制限（メモリとレンダリング負荷を考慮）
            if (rows > 10000 || cols > 10000 || (long)rows * cols > 50000000)
            {
                System.Diagnostics.Debug.WriteLine($"警告: データサイズが大きすぎます - {rows}x{cols}");
                return false;
            }
            
            // サンプルチェック（全要素チェックは重いため）
            int sampleCount = Math.Min(100, rows * cols);
            for (int i = 0; i < sampleCount; i++)
            {
                int row = (i * rows) / sampleCount;
                int col = (i * cols) / sampleCount;
                double value = data[row, col];
                
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    System.Diagnostics.Debug.WriteLine($"警告: 無効なデータ値検出 [{row},{col}] = {value}");
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 周波数範囲制限 (minFreq-maxFreq Hz)
        /// </summary>
        private static double[,] LimitFrequencyRange(double[,] powerMatrix, double[] freqAxis, double minFreq, double maxFreq)
        {
            if (powerMatrix == null || freqAxis == null) 
                throw new ArgumentNullException("powerMatrix or freqAxis is null");
            
            int timeSteps = powerMatrix.GetLength(0);
            int freqSteps = powerMatrix.GetLength(1);
            
            if (timeSteps == 0 || freqSteps == 0 || freqAxis.Length == 0)
                throw new ArgumentException("Invalid matrix or frequency axis dimensions");
            
            // 最小周波数のbinを見つける（安全性チェック付き）
            int minFreqBin = Array.FindIndex(freqAxis, f => f >= minFreq);
            if (minFreqBin == -1) minFreqBin = 0;
            minFreqBin = Math.Max(0, Math.Min(minFreqBin, freqAxis.Length - 1));
            
            // 最大周波数のbinを見つける（安全性チェック付き）
            int maxFreqBin = Array.FindIndex(freqAxis, f => f > maxFreq);
            if (maxFreqBin == -1) maxFreqBin = freqAxis.Length;
            maxFreqBin = Math.Max(minFreqBin + 1, Math.Min(maxFreqBin, freqAxis.Length));
            
            int freqBinCount = maxFreqBin - minFreqBin;
            if (freqBinCount <= 0) freqBinCount = 1; // 最低1binは確保
            
            var limited = new double[timeSteps, freqBinCount];
            for (int t = 0; t < timeSteps; t++)
            {
                for (int f = 0; f < freqBinCount; f++)
                {
                    int srcFreqBin = minFreqBin + f;
                    if (srcFreqBin < powerMatrix.GetLength(1))
                    {
                        limited[t, f] = powerMatrix[t, srcFreqBin];
                    }
                }
            }
            
            return limited;
        }

        /// <summary>
        /// 時間範囲制限 (minTime-maxTime 秒)
        /// </summary>
        private static (double[,] limited, int minTimeBin, int maxTimeBinExclusive) LimitTimeRange(double[,] powerMatrix, double[] timeAxis, double minTime, double maxTime)
        {
            if (powerMatrix == null || timeAxis == null)
                throw new ArgumentNullException("powerMatrix or timeAxis is null");

            int timeSteps = powerMatrix.GetLength(0);
            int freqSteps = powerMatrix.GetLength(1);

            if (timeSteps == 0 || freqSteps == 0 || timeAxis.Length == 0)
                throw new ArgumentException("Invalid matrix or time axis dimensions");

            // 最小時間のbinを見つける（安全性チェック付き）
            int minTimeBin = Array.FindIndex(timeAxis, t => t >= minTime);
            if (minTimeBin == -1) minTimeBin = 0;
            minTimeBin = Math.Max(0, Math.Min(minTimeBin, timeAxis.Length - 1));

            // 最大時間のbinを見つける（安全性チェック付き）
            int maxTimeBin = Array.FindIndex(timeAxis, t => t > maxTime);
            if (maxTimeBin == -1) maxTimeBin = timeAxis.Length;
            maxTimeBin = Math.Max(minTimeBin + 1, Math.Min(maxTimeBin, timeAxis.Length));

            int timeBinCount = maxTimeBin - minTimeBin;
            if (timeBinCount <= 0) timeBinCount = 1; // 最低1binは確保

            var limited = new double[timeBinCount, freqSteps];
            for (int t = 0; t < timeBinCount; t++)
            {
                for (int f = 0; f < freqSteps; f++)
                {
                    int srcTimeBin = minTimeBin + t;
                    if (srcTimeBin < powerMatrix.GetLength(0))
                    {
                        limited[t, f] = powerMatrix[srcTimeBin, f];
                    }
                }
            }

            return (limited, minTimeBin, maxTimeBin);
        }

        /// <summary>
        /// 描画用ダウンサンプリング (平均プール) - アンチエイリアシング対応
        /// </summary>
        private static double[,] DownsampleForDisplay(double[,] matrix, int maxT, int maxF)
        {
            int sourceT = matrix.GetLength(0);
            int sourceF = matrix.GetLength(1);
            
            int targetT = Math.Min(maxT, sourceT);
            int targetF = Math.Min(maxF, sourceF);
            
            // ダウンサンプリングが不要な場合は元の配列を返す
            if (targetT >= sourceT && targetF >= sourceF)
            {
                return matrix;
            }
            
            var downsampled = new double[targetT, targetF];
            
            double tRatio = (double)sourceT / targetT;
            double fRatio = (double)sourceF / targetF;
            
            // デバッグ情報
            System.Diagnostics.Debug.WriteLine($"DownsampleForDisplay: {sourceT}x{sourceF} → {targetT}x{targetF}, 比率: t={tRatio:F2}, f={fRatio:F2}");
            
            for (int t = 0; t < targetT; t++)
            {
                for (int f = 0; f < targetF; f++)
                {
                    // より精密な範囲計算（境界値の精度向上）
                    double startTReal = t * tRatio;
                    double endTReal = (t + 1) * tRatio;
                    double startFReal = f * fRatio;
                    double endFReal = (f + 1) * fRatio;
                    
                    int startT = (int)Math.Floor(startTReal);
                    int endT = (int)Math.Ceiling(endTReal);
                    int startF = (int)Math.Floor(startFReal);
                    int endF = (int)Math.Ceiling(endFReal);
                    
                    // 境界制限
                    startT = Math.Max(0, startT);
                    endT = Math.Min(sourceT, endT);
                    startF = Math.Max(0, startF);
                    endF = Math.Min(sourceF, endF);
                    
                    double sum = 0;
                    double weightSum = 0;
                    
                    // 重み付き平均（アンチエイリアシング効果）
                    for (int st = startT; st < endT; st++)
                    {
                        for (int sf = startF; sf < endF; sf++)
                        {
                            // 重み計算（ピクセル重複度に基づく）
                            double tWeight = Math.Min(st + 1, endTReal) - Math.Max(st, startTReal);
                            double fWeight = Math.Min(sf + 1, endFReal) - Math.Max(sf, startFReal);
                            double weight = tWeight * fWeight;
                            
                            if (weight > 0)
                            {
                                sum += matrix[st, sf] * weight;
                                weightSum += weight;
                            }
                        }
                    }
                    
                    downsampled[t, f] = weightSum > 0 ? sum / weightSum : 0;
                }
            }
            
            return downsampled;
        }
        
        /// <summary>
        /// Python互換IQRベースカラースケール計算
        /// </summary>
        private static (double vmin, double vmax) CalculateIQRScale(double[,] matrix)
        {
            var allValues = matrix.Cast<double>()
                .Where(x => !double.IsNaN(x) && !double.IsInfinity(x))
                .ToList();
            
            if (allValues.Count == 0)
                return (-60.0, 0.0);  // フォールバック
                
            allValues.Sort();
            
            // Python同様のパーセンタイル計算
            double q25 = GetPercentile(allValues, 25.0);
            double q75 = GetPercentile(allValues, 75.0);
            double iqr = q75 - q25;
            
            // 外れ値除外を無効化し、全データ範囲を使用
            double vmin = allValues[0];  // 最小値
            double vmax = allValues[allValues.Count - 1];  // 最大値
            
            // コメントアウト: IQRスケーリングを無効化
            // double vmin = q25 - 1.5 * iqr;
            // double vmax = q75 + 1.5 * iqr;
            // vmax = Math.Min(vmax, 5.0);  // 極端なスパイクを防ぐ
            
            return (vmin, vmax);
        }
        
        /// <summary>
        /// パーセンタイル計算 (既存のSpectrogramData.csと同じロジック)
        /// </summary>
        private static double GetPercentile(List<double> sortedValues, double percentile)
        {
            if (sortedValues.Count == 0) return 0.0;
            if (sortedValues.Count == 1) return sortedValues[0];
            
            double index = (percentile / 100.0) * (sortedValues.Count - 1);
            int lowerIndex = (int)Math.Floor(index);
            int upperIndex = (int)Math.Ceiling(index);
            
            if (lowerIndex == upperIndex)
            {
                return sortedValues[lowerIndex];
            }
            
            // 線形補間
            double weight = index - lowerIndex;
            return sortedValues[lowerIndex] * (1.0 - weight) + sortedValues[upperIndex] * weight;
        }

        /// <summary>
        /// フォールバック用の平均スペクトラム表示
        /// </summary>
        private void CreateAverageSpectrumPlot()
        {
            var avgSeries = new LineSeries 
            { 
                Title = "平均スペクトラム", 
                Color = OxyColors.Blue,
                StrokeThickness = 2
            };
            
            // 各周波数の時間平均を計算
            for (int f = 0; f < SpectrogramData.FrequencyBins; f++)
            {
                double avgPower = 0;
                int validCount = 0;
                
                for (int t = 0; t < SpectrogramData.TimeSteps; t++)
                {
                    double power = SpectrogramData.PowerMatrix[t, f];
                    if (!double.IsNaN(power) && !double.IsInfinity(power))
                    {
                        avgPower += power;
                        validCount++;
                    }
                }
                
                if (validCount > 0)
                {
                    avgPower /= validCount;
                    double freq = SafeDouble(SpectrogramData.FrequencyAxis[f], f);
                    avgSeries.Points.Add(new DataPoint(freq, avgPower));
                }
            }
            
            if (avgSeries.Points.Count > 0)
            {
                SpectrogramPlot.Series.Add(avgSeries);
            }
        }

        private void CreateTestPlot()
        {
            try
            {
                SpectrogramPlot.Series.Clear();
                
                // 確実に表示される基本的なライン
                var testSeries = new LineSeries 
                { 
                    Title = "テストスペクトラム", 
                    Color = OxyColors.Red,
                    StrokeThickness = 2
                };
                
                // 10Hz付近にピークを持つテストスペクトラム
                for (int f = 0; f < 100; f++)
                {
                    double freq = f;
                    double power = -50 + 30 * Math.Exp(-Math.Pow((freq - 10) / 5, 2)); // 10Hz付近にガウシアン
                    testSeries.Points.Add(new DataPoint(freq, power));
                }
                
                SpectrogramPlot.Series.Add(testSeries);
                
                // 軸の範囲を明示的に設定
                if (SpectrogramPlot.Axes.Count >= 2)
                {
                    SpectrogramPlot.Axes[0].Minimum = 0;
                    SpectrogramPlot.Axes[0].Maximum = 50;
                    SpectrogramPlot.Axes[1].Minimum = -60;
                    SpectrogramPlot.Axes[1].Maximum = -10;
                }
                
                SpectrogramPlot.InvalidatePlot(true);
                StatusMessage = "テストスペクトラムを表示";
            }
            catch (Exception ex)
            {
                StatusMessage = $"テスト表示エラー: {ex.Message}";
            }
        }

        private void CreateSimpleTextPlot()
        {
            try
            {
                SpectrogramPlot.Series.Clear();
                SpectrogramPlot.Annotations.Clear();
                
                // 簡単なテキスト注釈を追加
                var textAnnotation = new OxyPlot.Annotations.TextAnnotation
                {
                    Text = "スペクトログラム表示中にエラーが発生しました\n\n代替表示モードを使用しています",
                    TextPosition = new DataPoint(0.5, 0.5),
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                    TextVerticalAlignment = OxyPlot.VerticalAlignment.Middle,
                    FontSize = 14,
                    TextColor = OxyColors.Red
                };
                
                SpectrogramPlot.Annotations.Add(textAnnotation);
                SpectrogramPlot.InvalidatePlot(true);
            }
            catch
            {
                // 最後の手段：何もしない
            }
        }

        private void SwitchToLogScale()
        {
            // 現在の下限値を保存
            _preLogMinFreq = AnalysisSettings.DisplayMinFreq;

            // 対数スケール時の下限をデータの周波数分解能から決定
            double logDefaultMin = GetFrequencyResolution();
            _logAutoMinFreq = logDefaultMin;

            // デフォルト（0.0以下）のままならデータ由来の下限を適用
            if (AnalysisSettings.DisplayMinFreq <= 0.0)
                AnalysisSettings.DisplayMinFreq = logDefaultMin;

            ReplaceFrequencyAxis(isLog: true);
        }

        /// <summary>
        /// 対数スケール用の下限値を返す。
        /// SpectrogramData があれば周波数分解能（最初の非ゼロビン）を使用し、
        /// なければ DisplayMaxFreq の 1/100 をフォールバックとする。
        /// </summary>
        private double GetFrequencyResolution()
        {
            // データがあれば周波数分解能を使用
            if (SpectrogramData?.FrequencyAxis != null && SpectrogramData.FrequencyAxis.Length > 1)
            {
                double res = SpectrogramData.FrequencyAxis[1] - SpectrogramData.FrequencyAxis[0];
                if (res > 0) return res;
            }

            // データがない場合は表示上限の 1/100 をフォールバック
            double maxFreq = AnalysisSettings.DisplayMaxFreq > 0 ? AnalysisSettings.DisplayMaxFreq : 40.0;
            return maxFreq / 100.0;
        }

        /// <summary>
        /// 整数を Unicode 上付き文字に変換（例: -2 → "⁻²"）
        /// </summary>
        private static string ToSuperscript(int n)
        {
            const string superDigits = "⁰¹²³⁴⁵⁶⁷⁸⁹";
            if (n == 0) return "⁰";
            string result = "";
            int abs = Math.Abs(n);
            if (n < 0) result = "⁻";
            foreach (char c in abs.ToString())
                result += superDigits[c - '0'];
            return result;
        }

        private void SwitchToLinearScale()
        {
            // ユーザーが変更していなければ元の値（通常0.0）に戻す
            if (AnalysisSettings.DisplayMinFreq == _logAutoMinFreq)
                AnalysisSettings.DisplayMinFreq = _preLogMinFreq;

            ReplaceFrequencyAxis(isLog: false);
        }

        private void ReplaceFrequencyAxis(bool isLog)
        {
            var oldAxis = SpectrogramPlot.Axes.FirstOrDefault(a => a.Key == "FrequencyAxis");
            if (oldAxis != null)
                SpectrogramPlot.Axes.Remove(oldAxis);

            Axis newAxis;
            if (isLog)
            {
                // LogarithmicAxis は HeatMapSeries と互換性がない（GDI+ OverflowException）ため、
                // LinearAxis + log10 座標変換で対数表示を実現する
                double safeMin = AnalysisSettings.DisplayMinFreq > 0
                    ? AnalysisSettings.DisplayMinFreq
                    : GetFrequencyResolution();
                double safeMax = Math.Max(AnalysisSettings.DisplayMaxFreq, safeMin * 2);

                double logMin = Math.Log10(safeMin);
                double logMax = Math.Log10(safeMax);

                newAxis = new Log10LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "周波数 (Hz)",
                    Key = "FrequencyAxis",
                    Minimum = logMin,
                    Maximum = logMax,
                    LabelFormatter = logValue =>
                    {
                        int exp = (int)Math.Round(logValue);
                        if (Math.Abs(logValue - exp) < 0.01)
                            return $"10{ToSuperscript(exp)}";
                        return "";
                    }
                };
            }
            else
            {
                newAxis = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "周波数 (Hz)",
                    Key = "FrequencyAxis",
                    Minimum = AnalysisSettings.DisplayMinFreq,
                    Maximum = AnalysisSettings.DisplayMaxFreq
                };
            }

            SpectrogramPlot.Axes.Add(newAxis);
            SpectrogramPlot.InvalidatePlot(true);
        }

        private void UpdateAxisRanges()
        {
            if (SpectrogramData?.TimeAxis == null || SpectrogramData?.FrequencyAxis == null)
                return;

            if (SpectrogramData.TimeSteps <= 0 || SpectrogramData.TimeAxis.Length == 0 ||
                SpectrogramData.FrequencyBins <= 0 || SpectrogramData.FrequencyAxis.Length == 0)
                return;

            if (SpectrogramPlot?.Axes == null || SpectrogramPlot.Axes.Count < 2)
                return;

            // 時間軸：既存オブジェクトを直接更新（Remove→AddするとOxyPlotがデフォルト軸を自動生成して2本目が出現するため）
            // Series.Clear()等でOxyPlotがTimeAxisを取り除いた場合は再生成する
            double timeMin = AnalysisSettings.MinTime;
            double timeMax = AnalysisSettings.MaxTime;
            var timeAxis = SpectrogramPlot.Axes.FirstOrDefault(a => a.Key == "TimeAxis") as LinearAxis;
            if (timeAxis != null)
            {
                timeAxis.Minimum = timeMin;
                timeAxis.Maximum = timeMax;
                timeAxis.Reset();
            }
            else
            {
                SpectrogramPlot.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "時間 (秒)",
                    Key = "TimeAxis",
                    Minimum = timeMin,
                    Maximum = timeMax
                });
            }

            // 周波数軸範囲の設定
            double freqMin = AnalysisSettings.DisplayMinFreq;
            double freqMax = AnalysisSettings.DisplayMaxFreq;
            var freqAxis = SpectrogramPlot.Axes.FirstOrDefault(a => a.Key == "FrequencyAxis");
            if (freqAxis != null)
            {
                if (_isLogScale)
                {
                    // 対数スケール: LinearAxis + log10 座標変換方式
                    double safeMin = (freqMin > 0) ? freqMin : GetFrequencyResolution();
                    double safeMax = Math.Max(freqMax, safeMin * 2);
                    double logMin = Math.Log10(safeMin);
                    double logMax = Math.Log10(safeMax);
                    freqAxis.Minimum = logMin;
                    freqAxis.Maximum = logMax;
                    freqAxis.Zoom(logMin, logMax);
                }
                else
                {
                    freqAxis.Minimum = freqMin;
                    freqAxis.Maximum = freqMax;
                    freqAxis.Zoom(freqMin, freqMax);
                }
            }
        }

        public void UpdateAxisRangesFromUI(double timeMin, double timeMax, double freqMin, double freqMax)
        {
            AnalysisSettings.MinTime = timeMin;
            AnalysisSettings.MaxTime = timeMax;
            AnalysisSettings.DisplayMinFreq = freqMin;
            AnalysisSettings.DisplayMaxFreq = freqMax;

            UpdateAxisRanges();
            SpectrogramPlot.InvalidatePlot(true);
        }

        [RelayCommand]
        private void SaveResults()
        {
            if (SpectrogramData?.PowerMatrix == null)
            {
                MessageBox.Show("保存するデータがありません。先に解析を実行してください。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
                    saveFileDialog.Title = "結果を保存";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        _fileService.SaveSpectrogramData(SpectrogramData, saveFileDialog.FileName);
                        StatusMessage = "結果を保存しました";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"エラー: {ex.Message}";
                MessageBox.Show($"保存エラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private double SafeDouble(double value, double defaultValue)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return defaultValue;
            return value;
        }

        [RelayCommand]
        private void ApplyLFFilter()
        {
            if (CurrentSignal?.TimeData == null || CurrentSignal.TimeData.Length == 0)
            {
                MessageBox.Show("先にCSVファイルを読み込んでください。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "LFフィルタ処理中...";

                AnalysisSettings.EnableLFFilter = true;
                AnalysisSettings.EnableHFFilter = false;
                
                FilterAnalysisResult = _analysisService.AnalyzeFrequencyBands(CurrentSignal, AnalysisSettings);
                
                // フィルタ後データのスペクトログラムを計算してメインチャートに表示
                if (FilterAnalysisResult.LFFilteredData != null && FilterAnalysisResult.LFFilteredData.Length > 0)
                {
                    SpectrogramData = _analysisService.ComputeFilteredSpectrogram(
                        FilterAnalysisResult.LFFilteredData, 
                        CurrentSignal.SamplingRate, 
                        AnalysisSettings
                    );
                    UpdateSpectrogramPlot();
                }
                
                // 1次元波形はプレビュー用に保持
                UpdateFilterPlot(FilterAnalysisResult.LFFilteredData, "LF フィルタ結果");
                
                StatusMessage = $"LFフィルタ完了: {AnalysisSettings.LFCutoffLow:F1}-{AnalysisSettings.LFCutoffHigh:F1} Hz";
            }
            catch (Exception ex)
            {
                StatusMessage = $"エラー: {ex.Message}";
                MessageBox.Show($"LFフィルタエラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void ApplyHFFilter()
        {
            if (CurrentSignal?.TimeData == null || CurrentSignal.TimeData.Length == 0)
            {
                MessageBox.Show("先にCSVファイルを読み込んでください。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "HFフィルタ処理中...";

                AnalysisSettings.EnableLFFilter = false;
                AnalysisSettings.EnableHFFilter = true;
                
                FilterAnalysisResult = _analysisService.AnalyzeFrequencyBands(CurrentSignal, AnalysisSettings);
                
                // フィルタ後データのスペクトログラムを計算してメインチャートに表示
                if (FilterAnalysisResult.HFFilteredData != null && FilterAnalysisResult.HFFilteredData.Length > 0)
                {
                    SpectrogramData = _analysisService.ComputeFilteredSpectrogram(
                        FilterAnalysisResult.HFFilteredData, 
                        CurrentSignal.SamplingRate, 
                        AnalysisSettings
                    );
                    UpdateSpectrogramPlot();
                }
                
                // 1次元波形はプレビュー用に保持
                UpdateFilterPlot(FilterAnalysisResult.HFFilteredData, "HF フィルタ結果");
                
                StatusMessage = $"HFフィルタ完了: {AnalysisSettings.HFCutoffLow:F1}-{AnalysisSettings.HFCutoffHigh:F1} Hz";
            }
            catch (Exception ex)
            {
                StatusMessage = $"エラー: {ex.Message}";
                MessageBox.Show($"HFフィルタエラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateFilterPlot(double[] filteredData, string title)
        {
            if (filteredData == null || CurrentSignal == null)
                return;

            FilterPlot.Series.Clear();
            FilterPlot.Title = title;
            
            var series = new LineSeries { Title = "フィルタ後信号", Color = OxyColors.Red };

            for (int i = 0; i < Math.Min(filteredData.Length, 10000); i++)
            {
                double time = i / CurrentSignal.SamplingRate;
                double amplitude = SafeDouble(filteredData[i], 0.0);
                
                // 異常に大きな値を制限
                amplitude = Math.Max(-1e6, Math.Min(1e6, amplitude));
                
                series.Points.Add(new DataPoint(time, amplitude));
            }

            FilterPlot.Series.Add(series);
            FilterPlot.InvalidatePlot(true);
            
            // FilterPlotの変更をUIに通知
            OnPropertyChanged(nameof(FilterPlot));
        }

        #region 脳波バンドパワー正規化解析

        /// <summary>
        /// 脳波バンドパワー正規化解析を実行
        /// </summary>
        [RelayCommand]
        private async void AnalyzeBandPowers()
        {
            if (CurrentSignal?.TimeData == null || CurrentSignal.TimeData.Length == 0)
            {
                MessageBox.Show("先にCSVファイルを読み込んでください。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 区間設定チェック
            if (IntervalManager == null || !IntervalManager.IsConfigured)
            {
                // デフォルト区間設定を作成
                var totalDuration = CurrentSignal.TimeData.Length / CurrentSignal.SamplingRate;
                IntervalManager.SetDefaultIntervals(totalDuration);
                StatusMessage = $"デフォルト区間設定を適用しました（総時間: {totalDuration:F1}秒）";
            }

            try
            {
                IsBusy = true;
                StatusMessage = "脳波バンドパワー正規化解析を開始しています...";

                var progress = new Progress<int>(percent => ProgressValue = percent);
                BandPowerResult = await Task.Run(() =>
                    _analysisService.AnalyzeFiveBandPowers(CurrentSignal, AnalysisSettings, IntervalManager, progress));

                UpdateBandPowerPlot();

                var validationReport = _analysisService.ValidateBandPowerAnalysis(BandPowerResult);
                StatusMessage = $"バンドパワー解析完了\n{validationReport}";

                // 結果サマリーの表示
                var summary = BandPowerResult.GetSummary();
                Console.WriteLine(summary); // デバッグ用
            }
            catch (Exception ex)
            {
                StatusMessage = $"バンドパワー解析エラー: {ex.Message}";
                MessageBox.Show($"バンドパワー解析エラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// バンドパワー解析結果をCSVエクスポート
        /// </summary>
        [RelayCommand]
        private void ExportBandPowerResults()
        {
            if (BandPowerResult == null)
            {
                MessageBox.Show("エクスポートするバンドパワー解析結果がありません。先に解析を実行してください。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (var saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
                    saveFileDialog.Title = "正規化バンドパワー結果を保存";
                    saveFileDialog.FileName = $"正規化バンドパワー_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        ExportBandPowerToCsv(saveFileDialog.FileName);
                        StatusMessage = $"バンドパワー結果を保存しました: {saveFileDialog.FileName}";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"エクスポートエラー: {ex.Message}";
                MessageBox.Show($"エクスポートエラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Python互換バンドパワー線グラフ解析実行
        /// </summary>
        public async Task ExecuteBandPowerLineAnalysisAsync()
        {
            if (CurrentSignal?.TimeData == null)
            {
                StatusMessage = "信号データがロードされていません";
                return;
            }

            IsBusy = true;
            try
            {
                StatusMessage = "バンドパワー線グラフ解析を実行中...";

                var progress = new Progress<int>(percent => ProgressValue = percent);

                // 既存のスペクトログラムがある場合はそれを使用（時間軸の一貫性を保証）
                if (SpectrogramData?.PowerMatrix != null && SpectrogramData.TimeAxis != null)
                {
                    BandPowerLineData = await Task.Run(() =>
                        _analysisService.ComputeBandPowerLinesFromSpectrogram(SpectrogramData, progress));
                    System.Diagnostics.Debug.WriteLine($"バンドパワー: 既存スペクトログラムを使用 (TimeSteps={SpectrogramData.TimeSteps})");
                }
                else
                {
                    // スペクトログラムがない場合は従来通り計算
                    BandPowerLineData = await Task.Run(() =>
                        _analysisService.ComputeBandPowerLines(CurrentSignal, AnalysisSettings, progress));
                    System.Diagnostics.Debug.WriteLine("バンドパワー: スペクトログラムを新規計算");
                }

                UpdateBandPowerLinePlot();

                StatusMessage = $"バンドパワー線グラフ解析完了\n{BandPowerLineData.GetSummary()}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"バンドパワー線グラフ解析エラー: {ex.Message}";
                MessageBox.Show($"バンドパワー線グラフ解析エラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// HRV解析実行（心拍モード用）
        /// </summary>
        public async Task ExecuteHrvAnalysisAsync()
        {
            if (CurrentSignal?.TimeData == null)
            {
                StatusMessage = "信号データがロードされていません";
                return;
            }

            IsBusy = true;
            try
            {
                StatusMessage = "心拍変動（HRV）解析を実行中...";

                var progress = new Progress<int>(percent => ProgressValue = percent);
                var hrvResult = await Task.Run(() =>
                {
                    var analyzer = new Services.SlidingFftAnalyzer(_analysisService);
                    return analyzer.AnalyzeHrv(CurrentSignal.TimeData);
                });

                // HRV結果をBandPowerPlotに描画
                UpdateHrvPlot(hrvResult);

                StatusMessage = $"HRV解析完了";
            }
            catch (Exception ex)
            {
                StatusMessage = $"HRV解析エラー: {ex.Message}";
                MessageBox.Show($"HRV解析エラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// HRV解析結果をグラフ表示
        /// </summary>
        private void UpdateHrvPlot(Models.HrvAnalysisResult hrvResult)
        {
            if (hrvResult == null)
                return;

            try
            {
                BandPowerPlot.Series.Clear();
                BandPowerPlot.Annotations.Clear();
                BandPowerPlot.Axes.Clear();

                // タイトル設定
                BandPowerPlot.Title = "心拍変動（HRV）解析結果";

                // 時間軸
                var xAxis = new OxyPlot.Axes.LinearAxis
                {
                    Position = OxyPlot.Axes.AxisPosition.Bottom,
                    Title = "時間 (秒)"
                };

                // パワー軸
                var yAxis = new OxyPlot.Axes.LinearAxis
                {
                    Position = OxyPlot.Axes.AxisPosition.Left,
                    Title = "パワー (ms²)"
                };

                BandPowerPlot.Axes.Add(xAxis);
                BandPowerPlot.Axes.Add(yAxis);

                // LF系列（チェックボックスがONの場合のみ表示）
                if (IsLFSelected)
                {
                    var lfSeries = new LineSeries
                    {
                        Title = "LF (0.04-0.15 Hz)",
                        Color = OxyColors.Blue,
                        StrokeThickness = 2
                    };
                    for (int i = 0; i < hrvResult.TimeAxis.Length; i++)
                    {
                        lfSeries.Points.Add(new DataPoint(hrvResult.TimeAxis[i], hrvResult.LfPower[i]));
                    }
                    BandPowerPlot.Series.Add(lfSeries);
                }

                // HF系列（チェックボックスがONの場合のみ表示）
                if (IsHFSelected)
                {
                    var hfSeries = new LineSeries
                    {
                        Title = "HF (0.15-0.40 Hz)",
                        Color = OxyColors.Red,
                        StrokeThickness = 2
                    };
                    for (int i = 0; i < hrvResult.TimeAxis.Length; i++)
                    {
                        hfSeries.Points.Add(new DataPoint(hrvResult.TimeAxis[i], hrvResult.HfPower[i]));
                    }
                    BandPowerPlot.Series.Add(hfSeries);
                }

                // LF/HF比系列（チェックボックスがONの場合のみ表示）
                if (IsLfHfRatioSelected)
                {
                    var ratioSeries = new LineSeries
                    {
                        Title = "LF/HF比",
                        Color = OxyColors.Green,
                        StrokeThickness = 2,
                        YAxisKey = "RatioAxis"
                    };
                    for (int i = 0; i < hrvResult.TimeAxis.Length; i++)
                    {
                        ratioSeries.Points.Add(new DataPoint(hrvResult.TimeAxis[i], hrvResult.LfHfRatio[i]));
                    }

                    // LF/HF比用の右側Y軸を追加
                    var ratioAxis = new OxyPlot.Axes.LinearAxis
                    {
                        Position = OxyPlot.Axes.AxisPosition.Right,
                        Title = "LF/HF比",
                        Key = "RatioAxis"
                    };
                    BandPowerPlot.Axes.Add(ratioAxis);
                    BandPowerPlot.Series.Add(ratioSeries);
                }

                // 凡例設定
                BandPowerPlot.Legends.Add(new Legend
                {
                    LegendPosition = LegendPosition.TopRight
                });

                BandPowerPlot.InvalidatePlot(true);
            }
            catch (Exception ex)
            {
                StatusMessage = $"HRVグラフ表示エラー: {ex.Message}";
            }
        }

        /// <summary>
        /// バンドパワー線グラフプロット更新（Python互換）
        /// </summary>
        private void UpdateBandPowerLinePlot()
        {
            if (BandPowerLineData == null || !BandPowerLineData.IsValid())
                return;

            try
            {
                BandPowerPlot.Series.Clear();
                BandPowerPlot.Annotations.Clear();
                
                // 軸ラベルを更新
                BandPowerPlot.Title = "";
                BandPowerPlot.Axes[1].Title = "Power [dB]";

                // チェックボックスの選択状態に応じてバンド系列を追加
                if (IsEegMode)
                {
                    // EEGモード: 選択されたバンドのみ表示
                    if (IsDeltaSelected)
                    {
                        var deltaSeries = new LineSeries
                        {
                            Title = "δ (0–4 Hz)",
                            Color = OxyColors.Purple,
                            StrokeThickness = 2
                        };
                        for (int i = 0; i < BandPowerLineData.TimeAxis.Length; i++)
                        {
                            double time = BandPowerLineData.TimeAxis[i];
                            deltaSeries.Points.Add(new DataPoint(time, BandPowerLineData.DeltaBandDb[i]));
                        }
                        BandPowerPlot.Series.Add(deltaSeries);
                    }

                    if (IsThetaSelected)
                    {
                        var thetaSeries = new LineSeries
                        {
                            Title = "θ (4–8 Hz)",
                            Color = OxyColors.Blue,
                            StrokeThickness = 2
                        };
                        for (int i = 0; i < BandPowerLineData.TimeAxis.Length; i++)
                        {
                            double time = BandPowerLineData.TimeAxis[i];
                            thetaSeries.Points.Add(new DataPoint(time, BandPowerLineData.ThetaBandDb[i]));
                        }
                        BandPowerPlot.Series.Add(thetaSeries);
                    }

                    if (IsAlphaSelected)
                    {
                        var alphaSeries = new LineSeries
                        {
                            Title = "α (8–13 Hz)",
                            Color = OxyColors.Green,
                            StrokeThickness = 2
                        };
                        for (int i = 0; i < BandPowerLineData.TimeAxis.Length; i++)
                        {
                            double time = BandPowerLineData.TimeAxis[i];
                            alphaSeries.Points.Add(new DataPoint(time, BandPowerLineData.AlphaBandDb[i]));
                        }
                        BandPowerPlot.Series.Add(alphaSeries);
                    }

                    if (IsBetaSelected)
                    {
                        var betaSeries = new LineSeries
                        {
                            Title = "β (13–36 Hz)",
                            Color = OxyColors.Orange,
                            StrokeThickness = 2
                        };
                        for (int i = 0; i < BandPowerLineData.TimeAxis.Length; i++)
                        {
                            double time = BandPowerLineData.TimeAxis[i];
                            betaSeries.Points.Add(new DataPoint(time, BandPowerLineData.BetaBandDb[i]));
                        }
                        BandPowerPlot.Series.Add(betaSeries);
                    }

                    if (IsGammaSelected)
                    {
                        var gammaSeries = new LineSeries
                        {
                            Title = "γ (≥36 Hz)",
                            Color = OxyColors.Red,
                            StrokeThickness = 2
                        };
                        for (int i = 0; i < BandPowerLineData.TimeAxis.Length; i++)
                        {
                            double time = BandPowerLineData.TimeAxis[i];
                            gammaSeries.Points.Add(new DataPoint(time, BandPowerLineData.GammaBandDb[i]));
                        }
                        BandPowerPlot.Series.Add(gammaSeries);
                    }
                }
                else
                {
                    // 心拍モード: LF/HF成分（今後実装予定）
                    // TODO: 心拍モード用のLF/HF表示を実装
                }

                // 軸範囲設定
                BandPowerPlot.Axes[0].Minimum = BandPowerLineData.TimeAxis.First();
                BandPowerPlot.Axes[0].Maximum = BandPowerLineData.TimeAxis.Last();
                
                // Y軸範囲を自動設定（dBスケール）
                BandPowerPlot.Axes[1].Minimum = double.NaN;
                BandPowerPlot.Axes[1].Maximum = double.NaN;

                BandPowerPlot.InvalidatePlot(true);
                StatusMessage = "バンドパワー線グラフプロットを更新しました";
            }
            catch (Exception ex)
            {
                StatusMessage = $"線グラフプロット更新エラー: {ex.Message}";
            }
        }

        /// <summary>
        /// バンドパワープロット更新
        /// </summary>
        private void UpdateBandPowerPlot()
        {
            if (BandPowerResult == null || BandPowerResult.TimeAxis == null)
                return;

            try
            {
                BandPowerPlot.Series.Clear();
                BandPowerPlot.Annotations.Clear();

                // γ波とβ波の時系列プロット
                var gammaSeries = new LineSeries
                {
                    Title = "γ波 (30-100Hz)",
                    Color = OxyColors.Red,
                    StrokeThickness = 2
                };

                var betaSeries = new LineSeries
                {
                    Title = "β波 (13-30Hz)",
                    Color = OxyColors.Blue,
                    StrokeThickness = 2
                };

                var alphaSeries = new LineSeries
                {
                    Title = "α波 (8-13Hz)",
                    Color = OxyColors.Green,
                    StrokeThickness = 2
                };

                // データポイント追加
                for (int i = 0; i < BandPowerResult.TimeAxis.Length; i++)
                {
                    double time = BandPowerResult.TimeAxis[i];
                    gammaSeries.Points.Add(new DataPoint(time, BandPowerResult.GammaPercent[i]));
                    betaSeries.Points.Add(new DataPoint(time, BandPowerResult.BetaPercent[i]));
                    alphaSeries.Points.Add(new DataPoint(time, BandPowerResult.NormalizedAlpha[i] * 100));
                }

                BandPowerPlot.Series.Add(gammaSeries);
                BandPowerPlot.Series.Add(betaSeries);
                BandPowerPlot.Series.Add(alphaSeries);

                // 時間区間のマーカーを追加
                AddIntervalMarkers();

                // 軸範囲設定
                BandPowerPlot.Axes[0].Minimum = BandPowerResult.TimeAxis.First();
                BandPowerPlot.Axes[0].Maximum = BandPowerResult.TimeAxis.Last();
                BandPowerPlot.Axes[1].Minimum = 0;
                BandPowerPlot.Axes[1].Maximum = Math.Max(BandPowerResult.GammaPercent.Max(), BandPowerResult.BetaPercent.Max()) * 1.1;

                BandPowerPlot.InvalidatePlot(true);
                StatusMessage = "バンドパワープロットを更新しました";
            }
            catch (Exception ex)
            {
                StatusMessage = $"プロット更新エラー: {ex.Message}";
            }
        }

        /// <summary>
        /// 時間区間マーカーをプロットに追加
        /// </summary>
        private void AddIntervalMarkers()
        {
            if (BandPowerResult?.RestInterval != null)
            {
                // 安静時区間のマーカー
                var restMarker = new RectangleAnnotation
                {
                    MinimumX = BandPowerResult.RestInterval.StartTime,
                    MaximumX = BandPowerResult.RestInterval.EndTime,
                    Fill = OxyColor.FromArgb(50, 0, 255, 0), // 半透明の緑
                    Text = "安静時"
                };
                BandPowerPlot.Annotations.Add(restMarker);
            }

            // タスク区間のマーカー
            if (BandPowerResult?.TaskIntervals != null)
            {
                foreach (var task in BandPowerResult.TaskIntervals)
                {
                    var taskMarker = new RectangleAnnotation
                    {
                        MinimumX = task.StartTime,
                        MaximumX = task.EndTime,
                        Fill = OxyColor.FromArgb(50, 255, 0, 0), // 半透明の赤
                        Text = task.Label
                    };
                    BandPowerPlot.Annotations.Add(taskMarker);
                }
            }
        }

        /// <summary>
        /// CSVファイルへの出力処理
        /// </summary>
        private void ExportBandPowerToCsv(string filePath)
        {
            using (var writer = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                var result = BandPowerResult;

                // ヘッダー情報
                writer.WriteLine("# 脳波バンドパワー正規化解析結果");
                writer.WriteLine($"# 解析時刻: {result.AnalysisTime:yyyy/MM/dd HH:mm:ss}");
                writer.WriteLine($"# データ長: {result.SampleCount}サンプル");
                writer.WriteLine($"# 時間範囲: {result.TimeAxis.First():F3} - {result.TimeAxis.Last():F3} 秒");
                writer.WriteLine();

                // 正規化検証結果
                writer.WriteLine($"# 正規化検証: {(result.ValidateNormalization() ? "OK" : "NG")}");
                writer.WriteLine($"# パーセント値検証: {(result.ValidatePercentValues() ? "OK" : "NG")}");
                writer.WriteLine();

                // 時系列データ
                writer.WriteLine("Time(sec),Gamma_Normalized,Beta_Normalized,Alpha_Normalized,Theta_Normalized,Delta_Normalized,Gamma_Percent,Beta_Percent");
                for (int i = 0; i < result.TimeAxis.Length; i++)
                {
                    writer.WriteLine($"{result.TimeAxis[i]:F6}," +
                                   $"{result.NormalizedGamma[i]:F6}," +
                                   $"{result.NormalizedBeta[i]:F6}," +
                                   $"{result.NormalizedAlpha[i]:F6}," +
                                   $"{result.NormalizedTheta[i]:F6}," +
                                   $"{result.NormalizedDelta[i]:F6}," +
                                   $"{result.GammaPercent[i]:F6}," +
                                   $"{result.BetaPercent[i]:F6}");
                }

                writer.WriteLine();
                writer.WriteLine("# 統計値（Excel形式）");
                writer.WriteLine("Interval,Gamma_Avg_Percent,Beta_Avg_Percent,Alpha_Avg_Percent,Theta_Avg_Percent,Delta_Avg_Percent");

                // 安静時ベースライン
                if (result.RestBaseline != null)
                {
                    writer.WriteLine($"{result.RestBaseline.IntervalLabel}," +
                                   $"{result.RestBaseline.GammaPercent:F6}," +
                                   $"{result.RestBaseline.BetaPercent:F6}," +
                                   $"{result.RestBaseline.AlphaPercent:F6}," +
                                   $"{result.RestBaseline.ThetaPercent:F6}," +
                                   $"{result.RestBaseline.DeltaPercent:F6}");
                }

                // タスク時平均
                foreach (var taskAvg in result.TaskAverages)
                {
                    writer.WriteLine($"{taskAvg.IntervalLabel}," +
                                   $"{taskAvg.GammaPercent:F6}," +
                                   $"{taskAvg.BetaPercent:F6}," +
                                   $"{taskAvg.AlphaPercent:F6}," +
                                   $"{taskAvg.ThetaPercent:F6}," +
                                   $"{taskAvg.DeltaPercent:F6}");
                }

                writer.WriteLine();
                writer.WriteLine("# 変化量（Δ値）");
                writer.WriteLine("Task,Delta_Gamma,Delta_Beta,Delta_Alpha,Delta_Theta,Delta_Delta");

                foreach (var delta in result.DeltaValues)
                {
                    writer.WriteLine($"{delta.TaskLabel}," +
                                   $"{delta.DeltaGamma:F6}," +
                                   $"{delta.DeltaBeta:F6}," +
                                   $"{delta.DeltaAlpha:F6}," +
                                   $"{delta.DeltaTheta:F6}," +
                                   $"{delta.DeltaDelta:F6}");
                }
            }
        }

        #endregion
        
        /// <summary>
        /// 時間範囲で信号を切り出し
        /// </summary>
        private static SignalData ExtractTimeRange(SignalData full, double tMin, double tMax)
        {
            // 入力データの妥当性チェック
            if (full?.TimeData == null || full.TimeData.Length == 0)
            {
                throw new ArgumentException("元データが無効です");
            }
            
            double totalDuration = full.TimeData.Length / full.SamplingRate;
            
            // 時間範囲が無効な場合は元データをそのまま返す
            if (tMin < 0 || tMax <= tMin || tMin >= totalDuration)
            {
                System.Diagnostics.Debug.WriteLine($"無効な時間範囲: {tMin}-{tMax}秒、元データをそのまま使用");
                return full;
            }
            
            int startSample = (int)(tMin * full.SamplingRate);
            int endSample   = (int)(tMax * full.SamplingRate);

            startSample = Math.Max(0, startSample);
            endSample   = Math.Min(full.TimeData.Length, endSample);

            int length = Math.Max(0, endSample - startSample);
            
            // 抽出されるデータが短すぎる場合は元データを返す
            if (length < 64)
            {
                System.Diagnostics.Debug.WriteLine($"抽出データが短すぎます: {length}サンプル、元データを使用");
                return full;
            }
            
            var sliced = new double[length];
            Array.Copy(full.TimeData, startSample, sliced, 0, length);

            System.Diagnostics.Debug.WriteLine($"時間範囲抽出: {tMin:F1}-{tMax:F1}秒、{length}サンプル");
            
            // 他の情報はそのまま持つ（例：freq軸はComputeSpectrogram側で再生成される）
            return new SignalData
            {
                TimeData = sliced,
                SamplingRate = full.SamplingRate,
                DataType = full.DataType,
                FileName = full.FileName + $"_t{tMin:F1}-{tMax:F1}s"
            };
        }
        
        /// <summary>
        /// 未編集の解析変数のみを適切な初期値に自動設定する
        /// </summary>
        /// <param name="signalData">読み込まれた信号データ</param>
        private void SetInitialSettingsIfNotEdited(SignalData signalData)
        {
            if (signalData?.TimeData == null || signalData.SamplingRate <= 0)
                return;
            
            // データの実際の時間長を計算
            double dataTimeDuration = signalData.TimeData.Length / signalData.SamplingRate;
            double nyquistFreq = signalData.SamplingRate / 2.0;
            
            System.Diagnostics.Debug.WriteLine($"=== 未編集変数の自動設定開始 ===");
            System.Diagnostics.Debug.WriteLine($"データ時間長: {dataTimeDuration:F3}秒, ナイキスト周波数: {nyquistFreq:F1}Hz");
            System.Diagnostics.Debug.WriteLine($"[EditFlag] 編集フラグ確認: IsMinTimeManuallyEdited={AnalysisSettings.IsMinTimeManuallyEdited}, IsMaxTimeManuallyEdited={AnalysisSettings.IsMaxTimeManuallyEdited}");
            
            // 時間軸最小値（未編集の場合のみ）
            if (!AnalysisSettings.IsMinTimeManuallyEdited)
            {
                AnalysisSettings.MinTime = 0.0;
                System.Diagnostics.Debug.WriteLine($"[EditFlag] MinTime自動設定: {AnalysisSettings.MinTime:F1}秒");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[EditFlag] MinTime自動設定スキップ（編集済み）: 現在値={AnalysisSettings.MinTime:F1}秒");
            }
            
            // 時間軸最大値（未編集の場合のみ）
            if (!AnalysisSettings.IsMaxTimeManuallyEdited)
            {
                AnalysisSettings.MaxTime = dataTimeDuration;
                System.Diagnostics.Debug.WriteLine($"[EditFlag] MaxTime自動設定: {AnalysisSettings.MaxTime:F1}秒");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[EditFlag] MaxTime自動設定スキップ（編集済み）: 現在値={AnalysisSettings.MaxTime:F1}秒");
            }
            
            // 周波数軸最小値（未編集の場合のみ）
            if (!AnalysisSettings.IsDisplayMinFreqManuallyEdited)
            {
                AnalysisSettings.DisplayMinFreq = 0.0;
                System.Diagnostics.Debug.WriteLine($"DisplayMinFreq自動設定: {AnalysisSettings.DisplayMinFreq:F1}Hz");
            }
            
            // 周波数軸最大値（未編集の場合のみ）
            if (!AnalysisSettings.IsDisplayMaxFreqManuallyEdited)
            {
                // モードに応じた適切な最大周波数を設定
                double appropriateMaxFreq = Math.Min(nyquistFreq, 100.0); // ナイキスト周波数と100Hzの小さい方
                AnalysisSettings.DisplayMaxFreq = appropriateMaxFreq;
                System.Diagnostics.Debug.WriteLine($"DisplayMaxFreq自動設定: {AnalysisSettings.DisplayMaxFreq:F1}Hz");
            }
            
            // 時間解像度の最適化（未編集の場合のみ）
            double targetTimeResolution = dataTimeDuration / 1000.0; // 約1000フレーム
            int targetHopSize = (int)(targetTimeResolution * signalData.SamplingRate);
            targetHopSize = Math.Max(64, Math.Min(10000, targetHopSize)); // 64-10000に制限
            AnalysisSettings.TimeResolution = targetHopSize / signalData.SamplingRate;
            
            System.Diagnostics.Debug.WriteLine($"時間解像度最適化: {AnalysisSettings.TimeResolution:F6}秒 (ホップサイズ: {targetHopSize})");
            System.Diagnostics.Debug.WriteLine($"=== 自動設定完了 ===");
        }
    }

    /// <summary>
    /// log10 座標空間上で対数的な目盛りを生成する LinearAxis サブクラス。
    /// 大目盛り: 10 の整数乗（10⁰, 10¹, 10², ...）
    /// 小目盛り: 各 decade 内の 2,3,4,5,6,7,8,9 の位置（間隔が徐々に狭まる）
    /// </summary>
    public class Log10LinearAxis : LinearAxis
    {
        public override void GetTickValues(
            out IList<double> majorLabelValues,
            out IList<double> majorTickValues,
            out IList<double> minorTickValues)
        {
            majorLabelValues = new List<double>();
            majorTickValues = new List<double>();
            minorTickValues = new List<double>();

            int startDecade = (int)Math.Floor(ActualMinimum);
            int endDecade = (int)Math.Ceiling(ActualMaximum);

            for (int decade = startDecade; decade <= endDecade; decade++)
            {
                if (decade >= ActualMinimum && decade <= ActualMaximum)
                {
                    majorLabelValues.Add(decade);
                    majorTickValues.Add(decade);
                }

                for (int k = 2; k <= 9; k++)
                {
                    double minorPos = decade + Math.Log10(k);
                    if (minorPos >= ActualMinimum && minorPos <= ActualMaximum)
                        minorTickValues.Add(minorPos);
                }
            }
        }
    }
}