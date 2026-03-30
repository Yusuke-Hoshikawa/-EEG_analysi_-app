using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ホ号計画.Models;

namespace ホ号計画.Services
{
    /// <summary>
    /// スライディングFFT解析クラス（C++ ver.8互換）
    /// 既存のC# DFT関数を使用してC++のスライディングFFTアルゴリズムを再実装
    /// </summary>
    public class SlidingFftAnalyzer
    {
        private readonly AnalysisService _analysisService;

        // C++版の定数定義
        private const double SAMPLING_RATE = 10.0;  // 10Hz固定
        private const double DEFAULT_WINDOW_DURATION = 256.0; // 256秒ウィンドウ（標準）
        private const double MIN_WINDOW_DURATION = 5.0;       // 最小5秒ウィンドウ（短いデータ用）
        private const int DEFAULT_WINDOW_SIZE = (int)(DEFAULT_WINDOW_DURATION * SAMPLING_RATE); // 2560サンプル
        private const int MIN_WINDOW_SIZE = (int)(MIN_WINDOW_DURATION * SAMPLING_RATE);         // 50サンプル

        // 実際に使用するウィンドウサイズ（動的に決定）
        private double _actualWindowDuration;
        private int _actualWindowSize;

        // HRV周波数帯域定義（C++版準拠）
        private const double LF_BAND_LOW = 0.04;   // 0.04 Hz
        private const double LF_BAND_HIGH = 0.15;  // 0.15 Hz
        private const double HF_BAND_LOW = 0.15;   // 0.15 Hz
        private const double HF_BAND_HIGH = 0.40;  // 0.40 Hz

        public SlidingFftAnalyzer(AnalysisService analysisService)
        {
            _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
        }
        
        /// <summary>
        /// メインのHRV解析エントリーポイント
        /// C++のSlide_FFT_ver8アルゴリズムを忠実に再現
        /// 短いデータの場合は自動的にウィンドウサイズを調整
        /// </summary>
        /// <param name="ihrData10Hz">10Hz等間隔のIHRデータ [Hz]</param>
        /// <returns>HRV解析結果</returns>
        public HrvAnalysisResult AnalyzeHrv(double[] ihrData10Hz)
        {
            if (ihrData10Hz == null || ihrData10Hz.Length < MIN_WINDOW_SIZE)
            {
                throw new ArgumentException($"データ長が不足しています。最低{MIN_WINDOW_SIZE}サンプル（{MIN_WINDOW_DURATION}秒）必要です。現在: {ihrData10Hz?.Length ?? 0}サンプル");
            }

            // データ長に応じてウィンドウサイズを動的に決定
            DetermineWindowSize(ihrData10Hz.Length);

            // Step 1: DC成分除去
            double[] processedData = RemoveDcComponent(ihrData10Hz);

            // Step 2: スライディングFFT実行（C++の z=0/1 パターン）
            var slidingResults = PerformSlidingFft(processedData);

            // Step 3: LF/HF成分をSimpson則で積分
            var lfPower = ComputeLfPowerSimpson(slidingResults);
            var hfPower = ComputeHfPowerSimpson(slidingResults);

            // Step 4: 時間軸を生成
            double[] timeAxis = GenerateTimeAxis(slidingResults.Count);

            // Step 5: LF/HF比を計算
            double[] lfHfRatio = CalculateLfHfRatio(lfPower, hfPower);

            // Step 6: 結果オブジェクトを作成
            var result = new HrvAnalysisResult
            {
                TimeAxis = timeAxis,
                LfPower = lfPower,
                HfPower = hfPower,
                LfHfRatio = lfHfRatio,
                WindowDuration = _actualWindowDuration,
                SamplingRate = SAMPLING_RATE
            };

            // 統計値計算
            result.CalculateStatistics();

            return result;
        }

        /// <summary>
        /// データ長に応じてウィンドウサイズを動的に決定
        /// </summary>
        private void DetermineWindowSize(int dataLength)
        {
            if (dataLength >= DEFAULT_WINDOW_SIZE)
            {
                // 十分なデータがある場合は標準ウィンドウサイズを使用
                _actualWindowSize = DEFAULT_WINDOW_SIZE;
                _actualWindowDuration = DEFAULT_WINDOW_DURATION;
                System.Diagnostics.Debug.WriteLine($"HRV解析: 標準ウィンドウサイズ使用 ({_actualWindowDuration}秒, {_actualWindowSize}サンプル)");
            }
            else
            {
                // データが短い場合はデータ長に合わせてウィンドウサイズを調整
                _actualWindowSize = dataLength;
                _actualWindowDuration = dataLength / SAMPLING_RATE;

                // 周波数分解能を計算して警告
                double freqResolution = SAMPLING_RATE / _actualWindowSize;
                System.Diagnostics.Debug.WriteLine($"HRV解析: 短いデータのため動的ウィンドウサイズ使用 ({_actualWindowDuration:F1}秒, {_actualWindowSize}サンプル)");
                System.Diagnostics.Debug.WriteLine($"  周波数分解能: {freqResolution:F4}Hz（LF下限0.04Hzの検出精度が低下する可能性があります）");
                System.Console.WriteLine($"警告: データが短いため解析精度が低下します（{_actualWindowDuration:F1}秒, 周波数分解能{freqResolution:F4}Hz）");
            }
        }
        
        /// <summary>
        /// DC成分除去（C++版準拠）
        /// </summary>
        private double[] RemoveDcComponent(double[] data)
        {
            // 全体の平均値を計算
            double mean = data.Sum() / data.Length;
            
            // 平均値を減算
            double[] result = new double[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = data[i] - mean;
            }
            
            return result;
        }
        
        /// <summary>
        /// スライディングFFT実行（C++のz=0/1パターンを再現）
        /// 短いデータの場合は単一ウィンドウで解析
        /// </summary>
        private List<SlidingFftFrame> PerformSlidingFft(double[] data)
        {
            var results = new List<SlidingFftFrame>();

            // C++版のスライディングパターン: z=0からz=1への半分シフト
            int halfShift = Math.Max(1, _actualWindowSize / 2);
            int maxFrames = Math.Max(1, (data.Length - _actualWindowSize) / halfShift + 1);

            for (int frameIndex = 0; frameIndex < maxFrames; frameIndex++)
            {
                int startIndex = frameIndex * halfShift;

                // データ長チェック
                if (startIndex + _actualWindowSize > data.Length)
                    break;

                // ウィンドウデータ抽出
                double[] windowData = new double[_actualWindowSize];
                Array.Copy(data, startIndex, windowData, 0, _actualWindowSize);

                // Hamming窓適用（C++版準拠）
                ApplyHammingWindow(windowData);

                // DFT計算（既存のC# DFT関数を使用）
                Complex[] dftResult = CallExistingDft(windowData);

                // パワースペクトル計算
                double[] powerSpectrum = ComputePowerSpectrum(dftResult);

                // 周波数軸生成
                double[] frequencies = GenerateFrequencyAxis(_actualWindowSize);

                // フレーム結果を保存
                var frame = new SlidingFftFrame
                {
                    FrameIndex = frameIndex,
                    StartTime = startIndex / SAMPLING_RATE,
                    Frequencies = frequencies,
                    PowerSpectrum = powerSpectrum
                };

                results.Add(frame);
            }

            return results;
        }
        
        /// <summary>
        /// Hamming窓を適用（C++版と同じ実装）
        /// </summary>
        private void ApplyHammingWindow(double[] data)
        {
            int N = data.Length;
            for (int n = 0; n < N; n++)
            {
                // Hamming窓: w(n) = 0.54 - 0.46 * cos(2π*n/(N-1))
                double window = 0.54 - 0.46 * Math.Cos(2.0 * Math.PI * n / (N - 1));
                data[n] *= window;
            }
        }
        
        /// <summary>
        /// 既存のC# DFT関数を呼び出し
        /// AnalysisService.ComputeDFTPublic()を使用
        /// </summary>
        private Complex[] CallExistingDft(double[] timeData)
        {
            return _analysisService.ComputeDFTPublic(timeData);
        }
        
        /// <summary>
        /// パワースペクトル計算（C++版準拠）
        /// </summary>
        private double[] ComputePowerSpectrum(Complex[] dftResult)
        {
            int N = dftResult.Length;
            double[] powerSpectrum = new double[N / 2 + 1]; // 片側スペクトル

            for (int k = 0; k < powerSpectrum.Length; k++)
            {
                double magnitude = dftResult[k].Magnitude;

                // 片側スペクトル補正（DC成分とNyquist成分以外は2倍）
                if (k != 0 && k != N / 2)
                {
                    magnitude *= 2.0;
                }

                // パワー計算（ms²単位）- 動的ウィンドウサイズを使用
                powerSpectrum[k] = (magnitude * magnitude) / (SAMPLING_RATE * _actualWindowSize);
            }

            return powerSpectrum;
        }
        
        /// <summary>
        /// 周波数軸生成
        /// </summary>
        private double[] GenerateFrequencyAxis(int windowSize)
        {
            int numFreqs = windowSize / 2 + 1;
            double[] frequencies = new double[numFreqs];
            
            double freqResolution = SAMPLING_RATE / windowSize;
            
            for (int k = 0; k < numFreqs; k++)
            {
                frequencies[k] = k * freqResolution;
            }
            
            return frequencies;
        }
        
        /// <summary>
        /// LF成分パワーをSimpson則で積分（C++版準拠）
        /// </summary>
        private double[] ComputeLfPowerSimpson(List<SlidingFftFrame> frames)
        {
            double[] lfPower = new double[frames.Count];
            
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                lfPower[i] = IntegratePowerSimpson(frame.Frequencies, frame.PowerSpectrum, 
                    LF_BAND_LOW, LF_BAND_HIGH);
            }
            
            return lfPower;
        }
        
        /// <summary>
        /// HF成分パワーをSimpson則で積分（C++版準拠）
        /// </summary>
        private double[] ComputeHfPowerSimpson(List<SlidingFftFrame> frames)
        {
            double[] hfPower = new double[frames.Count];
            
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                hfPower[i] = IntegratePowerSimpson(frame.Frequencies, frame.PowerSpectrum, 
                    HF_BAND_LOW, HF_BAND_HIGH);
            }
            
            return hfPower;
        }
        
        /// <summary>
        /// Simpson則による数値積分（C++版と同じアルゴリズム）
        /// </summary>
        private double IntegratePowerSimpson(double[] frequencies, double[] powerSpectrum, 
            double lowFreq, double highFreq)
        {
            // 積分範囲のインデックスを見つける
            int startIndex = -1, endIndex = -1;
            
            for (int i = 0; i < frequencies.Length; i++)
            {
                if (startIndex == -1 && frequencies[i] >= lowFreq)
                    startIndex = i;
                if (frequencies[i] <= highFreq)
                    endIndex = i;
            }
            
            if (startIndex == -1 || endIndex == -1 || startIndex >= endIndex)
                return 0.0;
            
            // Simpson則による積分
            double integral = 0.0;
            int n = endIndex - startIndex;
            
            if (n < 2)
                return 0.0;
            
            // 偶数個にするため調整
            if (n % 2 == 1)
                n--;
            
            double h = (frequencies[startIndex + n] - frequencies[startIndex]) / n;
            
            // Simpson則の公式: ∫f(x)dx ≈ h/3 * (f₀ + 4f₁ + 2f₂ + 4f₃ + ... + fₙ)
            integral += powerSpectrum[startIndex]; // f₀
            
            for (int i = 1; i < n; i++)
            {
                double coefficient = (i % 2 == 1) ? 4.0 : 2.0;
                integral += coefficient * powerSpectrum[startIndex + i];
            }
            
            integral += powerSpectrum[startIndex + n]; // fₙ
            integral *= h / 3.0;
            
            return integral;
        }
        
        /// <summary>
        /// 時間軸生成
        /// </summary>
        private double[] GenerateTimeAxis(int numFrames)
        {
            double[] timeAxis = new double[numFrames];
            double halfShift = Math.Max(1, _actualWindowSize / 2) / SAMPLING_RATE;

            for (int i = 0; i < numFrames; i++)
            {
                timeAxis[i] = i * halfShift + _actualWindowDuration / 2.0; // ウィンドウ中心時刻
            }

            return timeAxis;
        }
        
        /// <summary>
        /// LF/HF比を計算
        /// </summary>
        private double[] CalculateLfHfRatio(double[] lfPower, double[] hfPower)
        {
            double[] ratio = new double[lfPower.Length];
            
            for (int i = 0; i < ratio.Length; i++)
            {
                // ゼロ割り防止
                ratio[i] = (hfPower[i] > 0) ? lfPower[i] / hfPower[i] : 0.0;
            }
            
            return ratio;
        }
    }
    
    /// <summary>
    /// スライディングFFTの1フレーム分の結果
    /// </summary>
    internal class SlidingFftFrame
    {
        public int FrameIndex { get; set; }
        public double StartTime { get; set; }
        public double[] Frequencies { get; set; }
        public double[] PowerSpectrum { get; set; }
    }
}