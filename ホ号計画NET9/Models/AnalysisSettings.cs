using System;

namespace ホ号計画.Models
{
    public enum WindowFunction
    {
        Rectangular,
        Hanning,
        Hamming,
        Blackman
    }

    public enum FilterType
    {
        None,
        LowFrequency,
        HighFrequency
    }

    public class AnalysisSettings
    {
        public WindowFunction WindowFunction { get; set; }

        [Obsolete("WindowDuration is deprecated. Use BasicTimeUnit instead.", false)]
        public double WindowDuration { get; set; }    // ウィンドウ時間幅（秒）- 廃止予定、BasicTimeUnitを使用してください

        public double TimeResolution { get; set; }     // 時間軸解像度（秒）- スペクトログラムの時間移動幅
        public double MinFrequency { get; set; }
        public double MaxFrequency { get; set; }
        
        // 表示範囲設定の追加
        public double MinTime { get; set; }        // 時間軸表示下限
        public double MaxTime { get; set; }        // 時間軸表示上限
        public double DisplayMinFreq { get; set; } // 周波数軸表示下限
        public double DisplayMaxFreq { get; set; } // 周波数軸表示上限
        
        // フィルタ設定の追加
        public bool EnableHFFilter { get; set; }
        public bool EnableLFFilter { get; set; }
        public double LFCutoffLow { get; set; }    // LF帯域下限
        public double LFCutoffHigh { get; set; }   // LF帯域上限
        public double HFCutoffLow { get; set; }    // HF帯域下限
        public double HFCutoffHigh { get; set; }   // HF帯域上限
        
        // スペクトログラム表示設定の追加
        public bool EnableInterpolation { get; set; } // スペクトログラム補間有効/無効

        // DC成分除去設定
        public bool EnableDcRemoval { get; set; } = true; // デフォルトで有効
        
        // 加算平均設定の追加
        public bool EnableEnsembleAveraging { get; set; } = false;
        public double TriggerThreshold { get; set; } = 0.1;
        public double PreTrigger { get; set; } = 0.1;   // 0.1秒前
        public double PostTrigger { get; set; } = 0.5;  // 0.5秒後
        
        // 加算平均スペクトログラム設定の追加
        public double BasicTimeUnit { get; set; } = 1.0;    // DFT時間幅（秒）
        public int DivisionCount { get; set; } = 5;         // 分割数
        
        // 編集状態追跡フィールド
        public bool IsMinTimeManuallyEdited { get; set; } = false;
        public bool IsMaxTimeManuallyEdited { get; set; } = false;
        public bool IsDisplayMinFreqManuallyEdited { get; set; } = false;
        public bool IsDisplayMaxFreqManuallyEdited { get; set; } = false;

        public AnalysisSettings()
        {
            WindowFunction = WindowFunction.Hanning;
            #pragma warning disable CS0618 // 型またはメンバーが旧型式です
            WindowDuration = 1.0; // 非推奨: BasicTimeUnitを使用してください（後方互換性のため残存）
            #pragma warning restore CS0618
            TimeResolution = 1.0; // デフォルトはBasicTimeUnitと同じ（オーバーラップなし）
            MinFrequency = 0.0;
            MaxFrequency = 5.0;
            
            // 表示範囲のデフォルト値（汎用設定）
            MinTime = 0.0;           // 開始時間
            MaxTime = 300.0;         // 5分デフォルト
            DisplayMinFreq = 0.0;    // 表示最小周波数
            DisplayMaxFreq = 40.0;   // 脳波解析用デフォルト（40Hz）
            
            // フィルタ設定のデフォルト値
            EnableHFFilter = false;
            EnableLFFilter = false;
            LFCutoffLow = 0.5;    // 0.5 Hz
            LFCutoffHigh = 30.0;  // 30 Hz
            HFCutoffLow = 30.0;   // 30 Hz
            HFCutoffHigh = 100.0; // 100 Hz
            
            // スペクトログラム表示設定のデフォルト値
            EnableInterpolation = true; // デフォルトで補間を有効にする
        }

        /// <summary>
        /// ウィンドウサイズ（サンプル数）をサンプリング周波数から計算
        /// </summary>
        /// <param name="samplingRate">サンプリング周波数</param>
        /// <returns>ウィンドウサイズ（サンプル数）</returns>
        public int GetWindowSize(double samplingRate)
        {
            // WindowDuration は廃止され、常に BasicTimeUnit を使用
            int windowSize = Math.Max(2, (int)(BasicTimeUnit * samplingRate));

            // デバッグ: ウィンドウサイズ計算をログ出力
            System.Diagnostics.Debug.WriteLine($"=== ウィンドウサイズ計算 ===");
            System.Diagnostics.Debug.WriteLine($"BasicTimeUnit設定値: {BasicTimeUnit:F6}秒");
            System.Diagnostics.Debug.WriteLine($"SamplingRate: {samplingRate:F1}Hz");
            System.Diagnostics.Debug.WriteLine($"理論ウィンドウサイズ: {BasicTimeUnit * samplingRate:F1}サンプル");
            System.Diagnostics.Debug.WriteLine($"実際のウィンドウサイズ: {windowSize}サンプル");
            System.Diagnostics.Debug.WriteLine($"実際の周波数分解能: {samplingRate / windowSize:F6}Hz");

            return windowSize;
        }
        
        /// <summary>
        /// ホップサイズ（ステップサイズ）をサンプリング周波数から計算。
        /// ずらし幅は TimeResolution で指定する。
        /// </summary>
        /// <param name="samplingRate">サンプリング周波数</param>
        /// <returns>ホップサイズ（サンプル数）</returns>
        public int GetHopSize(double samplingRate) => Math.Max(1, (int)(TimeResolution * samplingRate));

        /// <summary>
        /// 解析モードに応じた適切なデフォルト値を設定
        /// </summary>
        /// <param name="isEegMode">脳波モードの場合true、心拍モードの場合false</param>
        public void SetModeDefaults(bool isEegMode)
        {
            if (isEegMode)
            {
                // 脳波解析用設定
                DisplayMinFreq = 0.0;
                DisplayMaxFreq = 40.0;    // 脳波の主要帯域
                MinFrequency = 0.0;
                MaxFrequency = 40.0;
                System.Diagnostics.Debug.WriteLine("脳波モード用デフォルト設定を適用: 0-40Hz");
            }
            else
            {
                // 心拍変動解析用設定
                DisplayMinFreq = 0.0;
                DisplayMaxFreq = 0.5;     // HRV主要帯域
                MinFrequency = 0.0;
                MaxFrequency = 0.5;
                System.Diagnostics.Debug.WriteLine("心拍モード用デフォルト設定を適用: 0-0.5Hz");
            }
        }
    }
}