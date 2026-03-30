using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MathNet.Numerics;
using ホ号計画.Models;
using ホ号計画.Utils;

namespace ホ号計画.Services
{
    public class AnalysisService
    {
        public SpectrogramData ComputeSpectrogram(SignalData signalData, AnalysisSettings settings, IProgress<int> progress = null)
        {
            int dataLength = signalData.TimeData.Length;
            
            // 入力データの妥当性チェック（DFTは2サンプル以上あれば動作する）
            if (dataLength < 2)
            {
                throw new ArgumentException($"データが短すぎます。最小2サンプル必要ですが、{dataLength}サンプルでした。");
            }

            // 設定可能なウィンドウサイズ（時間幅から計算）
            int nperseg = settings.GetWindowSize(signalData.SamplingRate);

            // データ長に合わせてウィンドウサイズを調整（最小2サンプル）
            int originalNperseg = nperseg;
            nperseg = Math.Max(2, Math.Min(nperseg, dataLength));
            
            // 時間軸解像度（ずらし幅）から直接ホップサイズを計算
            // 時間軸解像度 = hop / samplingRate
            // → hop = 時間軸解像度 × samplingRate
            int hop = settings.GetHopSize(signalData.SamplingRate);
            
            // ホップサイズがウィンドウサイズを超えないよう制限
            hop = Math.Min(hop, nperseg);
            
            // オーバーラップサイズを逆算
            int noverlap = Math.Max(0, nperseg - hop);
            
            // ホップサイズの妥当性チェック（ゼロ除算防止）
            if (hop <= 0)
            {
                hop = Math.Max(1, nperseg / 4); // 最低1、推奨nperseg/4
                System.Diagnostics.Debug.WriteLine($"警告: ホップサイズを{hop}に調整しました");
            }
            
            // 最優先デバッグ: スペクトログラムパラメータをログ出力
            double actualDataDuration = dataLength / signalData.SamplingRate;
            double actualWindowDuration = nperseg / signalData.SamplingRate;
            double actualTimeResolution = hop / signalData.SamplingRate;
            
            // フレーム数計算の安全性チェック
            if (dataLength < nperseg)
            {
                throw new ArgumentException($"データ長({dataLength})がウィンドウサイズ({nperseg})より短いです。");
            }
            
            // フレーム数計算（Pythonと同じ）
            int numFrames = Math.Max(1, (dataLength - nperseg) / hop + 1);
            int numFreqBins = nperseg / 2 + 1; // rfftと同じ
            
            // 計算パラメータの詳細ログ
            double dataDurationSec = dataLength / signalData.SamplingRate;
            double windowDurationSec = nperseg / signalData.SamplingRate;  
            double hopDurationSec = hop / signalData.SamplingRate;
            double expectedMaxTime = (numFrames - 1) * hopDurationSec;
            
            var spectrogram = new SpectrogramData(numFrames, numFreqBins);
            
            // 設定された窓関数係数を正しく生成
            double[] window = WindowFunctions.CreateWindow(nperseg, settings.WindowFunction);
            
            // 窓関数のパワー合計を計算（Pythonと同じ正規化）
            double winPower = 0;
            for (int i = 0; i < nperseg; i++)
            {
                winPower += window[i] * window[i];
            }
            
            // 各フレームを処理
            for (int frameIndex = 0; frameIndex < numFrames; frameIndex++)
            {
                int startIndex = frameIndex * hop;
                if (startIndex + nperseg > dataLength)
                    break;
                
                // セグメント抽出
                double[] segment = new double[nperseg];
                Array.Copy(signalData.TimeData, startIndex, segment, 0, nperseg);
                System.Diagnostics.Debug.WriteLine($"[SEG] segment.Length={segment.Length}, nperseg={nperseg}");

                // フレーム単位DC成分除去（窓関数適用前に平均値を減算）
                if (settings.EnableDcRemoval)
                {
                    double mean = 0.0;
                    for (int i = 0; i < nperseg; i++)
                        mean += segment[i];
                    mean /= nperseg;
                    for (int i = 0; i < nperseg; i++)
                        segment[i] -= mean;
                }

                // 窓関数適用
                for (int i = 0; i < nperseg; i++)
                {
                    segment[i] *= window[i];
                }
                
                // DFT計算
                Complex[] dftData = ComputeDFT(segment);
                System.Diagnostics.Debug.WriteLine($"[DFT-OUT] dftData.Length={dftData.Length}");
                // パワースペクトル計算（Pythonと同じ正規化）
                for (int f = 0; f < numFreqBins; f++)
                {
                    double magnitude = dftData[f].Magnitude;

// ★ pure DFT → 片側スペクトル補正（rFFT 相当） ★
// f = 0 と f = nperseg/2（Nyquist）は 2倍しない。
// その他は必ず 2倍。
if (f != 0 && f != nperseg / 2)
    magnitude *= 2.0;

double power = (magnitude * magnitude) / (signalData.SamplingRate * winPower);
                    
                    // 数値安定化（Pythonと同じ）
                    power = Math.Max(power, 1e-12);
                    
                    // dB変換
                    spectrogram.PowerMatrix[frameIndex, f] = 10.0 * Math.Log10(power);
                }

                // 時間軸設定（区間の中央時刻）
                spectrogram.TimeAxis[frameIndex] = (frameIndex * hop + nperseg / 2.0) / signalData.SamplingRate;
                
                // 進捗報告
                if (progress != null && frameIndex % 10 == 0)
                {
                    int progressPercent = (int)((double)frameIndex / numFrames * 100);
                    progress.Report(progressPercent);
                }
            }
            
            // 時間軸の最終検証
            double actualMinTime = spectrogram.TimeAxis[0];
            double actualMaxTime = spectrogram.TimeAxis[numFrames - 1];
            System.Diagnostics.Debug.WriteLine($"時間軸検証: 計算範囲={actualMinTime:F3}s～{actualMaxTime:F1}s, データ長={dataDurationSec:F1}s");
            
            if (actualMaxTime < dataDurationSec * 0.5)
            {
                System.Diagnostics.Debug.WriteLine($"警告: 時間軸が短すぎます！ 期待値:{dataDurationSec:F1}s, 実際:{actualMaxTime:F1}s");
                System.Console.WriteLine($"警告: 時間軸異常 - 期待値:{dataDurationSec:F1}s, 実際:{actualMaxTime:F1}s");
            }
            
            // 周波数軸設定（rfftfreqと同じ）
            double freqResolution = signalData.SamplingRate / nperseg;
            
            for (int f = 0; f < numFreqBins; f++)
            {
                spectrogram.FrequencyAxis[f] = f * freqResolution;
            }
            
            // IQRベースの自動スケーリング適用
            spectrogram.UpdatePowerRangeWithIQR();
            
            // デバッグ: 最終的な時間軸範囲を確認
            if (spectrogram.TimeAxis != null && spectrogram.TimeAxis.Length > 0)
            {
                double timeStart = spectrogram.TimeAxis[0];
                double timeEnd = spectrogram.TimeAxis[spectrogram.TimeAxis.Length - 1];
            }
            
            return spectrogram;
        }

        /// <summary>
        /// HRVデータからIHRを10Hz等間隔でサンプリング（HRV解析用）
        /// </summary>
        /// <param name="rpeaksSec">Rピーク時刻 [秒]</param>
        /// <returns>10Hz等間隔サンプリングのIHRデータ</returns>
        public double[] ProcessHRVToIHR(List<double> rpeaksSec)
        {
            return IhrInterpolator.BuildIhr10Hz(rpeaksSec);
        }

        public double[] ComputePowerSpectrum(double[] timeData, double samplingRate, AnalysisSettings settings)
        {
            double[] windowed = WindowFunctions.ApplyWindow(timeData, settings.WindowFunction);
            
            // 純粋なDFT実装
            Complex[] dftData = ComputeDFT(windowed);
            
            int numFreqBins = dftData.Length / 2 + 1;
            double[] powerSpectrum = new double[numFreqBins];
            
            // 物理的ノイズフロア
            double quantizationNoise = Math.Pow(2, -16);
            double thermalNoise = 1e-9;
            double noiseFloor = Math.Max(quantizationNoise * quantizationNoise, thermalNoise);
            
            for (int i = 0; i < numFreqBins; i++)
            {
                double power = Math.Max(dftData[i].Magnitude, noiseFloor);
                powerSpectrum[i] = 10.0 * Math.Log10(power);
            }
            
            return powerSpectrum;
        }

        public double[] ApplyBandpassFilter(double[] inputData, double lowCutoff, double highCutoff, double samplingRate)
        {
            if (inputData == null || inputData.Length == 0)
                return new double[0];

            // 全てのフィルタに対して適切なDFT処理を実行
            double[] processedData = RemoveDCOffset(inputData);

            // 純粋なDFT実装でフィルタリング
            Complex[] dftData = ComputeDFT(processedData);

            int length = dftData.Length;
            double freqResolution = samplingRate / length;

            for (int i = 0; i < length; i++)
            {
                double freq;
                if (i <= length / 2)
                {
                    freq = i * freqResolution;
                }
                else
                {
                    freq = (i - length) * freqResolution;
                }

                double absFreq = Math.Abs(freq);
                double filterGain = 1.0;
                
                // 理論的に妥当な遷移帯域設計
                // Kaiser窓設計法に基づく遷移帯域幅の計算
                // 60dB阻止域減衰を目標とした設計
                double transitionWidth = 2.0; // Hz - 固定遷移帯域幅（脳波解析に適切）
                
                // バターワース型の遷移特性（単調減少、リップルなし）
                if (absFreq < lowCutoff - transitionWidth)
                {
                    filterGain = 0.0; // 完全阻止域
                }
                else if (absFreq < lowCutoff)
                {
                    // 下側遷移帯域（-60dBまでの減衰）
                    double ratio = (absFreq - (lowCutoff - transitionWidth)) / transitionWidth;
                    filterGain = Math.Pow(ratio, 4); // 4次バターワース特性
                }
                else if (absFreq <= highCutoff)
                {
                    filterGain = 1.0; // 通過域
                }
                else if (absFreq < highCutoff + transitionWidth)
                {
                    // 上側遷移帯域（-60dBまでの減衰）
                    double ratio = (highCutoff + transitionWidth - absFreq) / transitionWidth;
                    filterGain = Math.Pow(ratio, 4); // 4次バターワース特性
                }
                else
                {
                    filterGain = 0.0; // 完全阻止域
                }
                
                dftData[i] *= filterGain;
            }

            // 逆DFT実装
            Complex[] idftData = ComputeIDFT(dftData);
            
            double[] result = idftData.Select(x => x.Real).ToArray();
            
            // DCオフセット除去（理論的に妥当な処理）
            result = RemoveDCOffset(result);
            
            return result;
        }

        private double[] RemoveDCOffset(double[] data)
        {
            if (data == null || data.Length == 0)
                return data;

            // 平均値（DCオフセット）を計算
            double mean = data.Sum() / data.Length;
            
            // 平均値を減算してDCオフセットを除去
            return data.Select(x => x - mean).ToArray();
        }

        public double[] ApplyLowPassFilter(double[] inputData, double cutoffFreq, double samplingRate)
        {
            return ApplyBandpassFilter(inputData, 0, cutoffFreq, samplingRate);
        }

        public double[] ApplyHighPassFilter(double[] inputData, double cutoffFreq, double samplingRate)
        {
            return ApplyBandpassFilter(inputData, cutoffFreq, samplingRate / 2, samplingRate);
        }

        public FilterAnalysisResult AnalyzeFrequencyBands(SignalData signalData, AnalysisSettings settings)
        {
            var result = new FilterAnalysisResult
            {
                OriginalData = signalData.TimeData,
                AppliedFilter = FilterType.None
            };

            try
            {
                if (settings.EnableLFFilter)
                {
                    result.LFFilteredData = ApplyBandpassFilter(
                        signalData.TimeData, 
                        settings.LFCutoffLow, 
                        settings.LFCutoffHigh, 
                        signalData.SamplingRate
                    );
                    
                    result.LFPowerSpectrum = ComputePowerSpectrum(
                        result.LFFilteredData, 
                        signalData.SamplingRate, 
                        settings
                    );
                    
                    result.AppliedFilter = FilterType.LowFrequency;
                    result.FilterDescription = $"LF Filter: {settings.LFCutoffLow:F1}-{settings.LFCutoffHigh:F1} Hz";
                }

                if (settings.EnableHFFilter)
                {
                    result.HFFilteredData = ApplyBandpassFilter(
                        signalData.TimeData, 
                        settings.HFCutoffLow, 
                        settings.HFCutoffHigh, 
                        signalData.SamplingRate
                    );
                    
                    result.HFPowerSpectrum = ComputePowerSpectrum(
                        result.HFFilteredData, 
                        signalData.SamplingRate, 
                        settings
                    );
                    
                    if (result.AppliedFilter == FilterType.LowFrequency)
                    {
                        // 両方のフィルタが適用された場合
                        result.FilterDescription += $" + HF Filter: {settings.HFCutoffLow:F1}-{settings.HFCutoffHigh:F1} Hz";
                    }
                    else
                    {
                        result.AppliedFilter = FilterType.HighFrequency;
                        result.FilterDescription = $"HF Filter: {settings.HFCutoffLow:F1}-{settings.HFCutoffHigh:F1} Hz";
                    }
                }

                // パワー比の計算
                result.CalculatePowerRatios();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"周波数フィルタ解析エラー: {ex.Message}", ex);
            }

            return result;
        }

        public SpectrogramData ComputeFilteredSpectrogram(double[] filteredData, double samplingRate, AnalysisSettings settings)
        {
            if (filteredData == null || filteredData.Length == 0)
                throw new ArgumentException("フィルタ後データが無効です。");

            // フィルタ後データをそのまま使用（理論的に正当な処理）
            // 人工的な閾値処理は信号処理理論に反するため除去

            // フィルタ後データから一時的なSignalDataを作成
            var tempSignalData = new SignalData
            {
                TimeData = filteredData, // 人工的処理なしの生データを使用
                SamplingRate = samplingRate,
                FileName = "フィルタ後データ"
            };

            // 既存のComputeSpectrogramメソッドを使用
            return ComputeSpectrogram(tempSignalData, settings);
        }

        /// <summary>
        /// 純粋なDFT実装（数学的定義通り）
        /// X[k] = Σ(n=0 to N-1) x[n] * exp(-j*2π*k*n/N)
        /// </summary>
        private Complex[] ComputeDFT(double[] timeData)
        {
            int N = timeData.Length;
            System.Diagnostics.Debug.WriteLine($"[DFT] N={N}");
            Complex[] result = new Complex[N];

            for (int k = 0; k < N; k++)
            {
                Complex sum = Complex.Zero;
                for (int n = 0; n < N; n++)
                {
                    double angle = -2.0 * Math.PI * k * n / N;
                    sum += timeData[n] * new Complex(Math.Cos(angle), Math.Sin(angle));
                }
                result[k] = sum;
            }

            return result;
        }

        /// <summary>
        /// 純粋な逆DFT実装（数学的定義通り）
        /// x[n] = (1/N) * Σ(k=0 to N-1) X[k] * exp(j*2π*k*n/N)
        /// </summary>
        private Complex[] ComputeIDFT(Complex[] freqData)
        {
            int N = freqData.Length;
            Complex[] result = new Complex[N];
            
            for (int n = 0; n < N; n++)
            {
                Complex sum = Complex.Zero;
                for (int k = 0; k < N; k++)
                {
                    // 逆DFTの数学的定義：exp(j*2π*k*n/N)
                    double angle = 2.0 * Math.PI * k * n / N;
                    Complex twiddle = new Complex(Math.Cos(angle), Math.Sin(angle));
                    sum += freqData[k] * twiddle;
                }
                // 正規化（1/N）
                result[n] = sum / N;
            }
            
            return result;
        }

        #region 脳波バンドパワー正規化解析

        /// <summary>
        /// 脳波5バンドの正規化パワー解析を実行
        /// Excelで示された正規化計算を完全実装
        /// </summary>
        /// <param name="signalData">入力信号データ</param>
        /// <param name="settings">解析設定</param>
        /// <param name="intervalManager">時間区間管理</param>
        /// <param name="progress">進捗報告</param>
        /// <returns>正規化解析結果</returns>
        public BandPowerAnalysisResult AnalyzeFiveBandPowers(
            SignalData signalData, 
            AnalysisSettings settings,
            TimeIntervalManager intervalManager,
            IProgress<int> progress = null)
        {
            if (signalData?.TimeData == null || signalData.TimeData.Length == 0)
                throw new ArgumentException("信号データが無効です");

            if (intervalManager == null || !intervalManager.IsConfigured)
                throw new ArgumentException("時間区間が設定されていません");

            progress?.Report(0);

            // スペクトログラム計算
            var spectrogram = ComputeSpectrogram(signalData, settings, 
                new Progress<int>(p => progress?.Report(p * 60 / 100))); // 60%まで
            
            progress?.Report(60);

            // 各バンドのパワーを抽出（Python定義に合わせて）
            var gammaPower = ExtractBandPower(spectrogram, BandDefinitions.GammaLow, BandDefinitions.GammaHigh);    // γ波 (36 Hz以上)
            var betaPower = ExtractBandPower(spectrogram, BandDefinitions.BetaLow, BandDefinitions.BetaHigh);        // β波 (13-36 Hz)
            var alphaPower = ExtractBandPower(spectrogram, BandDefinitions.AlphaLow, BandDefinitions.AlphaHigh);    // α波 (8-13 Hz)
            var thetaPower = ExtractBandPower(spectrogram, BandDefinitions.ThetaLow, BandDefinitions.ThetaHigh);    // θ波 (4-8 Hz)
            var deltaPower = ExtractBandPower(spectrogram, BandDefinitions.DeltaLow, BandDefinitions.DeltaHigh);    // δ波 (0-4 Hz)

            progress?.Report(75);

            // 正規化計算（各時刻で合計=1.0）
            NormalizeBandPowers(
                gammaPower, betaPower, alphaPower, thetaPower, deltaPower,
                out double[] normGamma, out double[] normBeta, out double[] normAlpha,
                out double[] normTheta, out double[] normDelta);

            progress?.Report(85);

            // 結果オブジェクト作成
            var result = new BandPowerAnalysisResult(spectrogram.TimeSteps)
            {
                // 正規化結果
                NormalizedGamma = normGamma,
                NormalizedBeta = normBeta,
                NormalizedAlpha = normAlpha,
                NormalizedTheta = normTheta,
                NormalizedDelta = normDelta,

                // パーセント表示（主要バンドのみ）
                GammaPercent = normGamma.Select(x => x * 100).ToArray(),
                BetaPercent = normBeta.Select(x => x * 100).ToArray(),

                // 時間軸
                TimeAxis = spectrogram.TimeAxis,

                // 区間設定
                RestInterval = intervalManager.RestInterval,
                TaskIntervals = new List<TimeInterval>(intervalManager.TaskIntervals),

                AnalysisTime = DateTime.Now
            };

            progress?.Report(90);

            // 統計計算
            result.RecalculateStatistics();

            progress?.Report(100);

            return result;
        }

        /// <summary>
        /// スペクトログラムから指定周波数帯域のパワーを抽出（シンプソン法による高精度積分）
        /// </summary>
        /// <param name="spectrogram">スペクトログラムデータ</param>
        /// <param name="lowFreq">下限周波数 (Hz)</param>
        /// <param name="highFreq">上限周波数 (Hz) - double.PositiveInfinityで上限なし</param>
        /// <returns>時系列バンドパワー配列</returns>
        private double[] ExtractBandPower(
            SpectrogramData spectrogram, 
            double lowFreq, 
            double highFreq)
        {
            double[] bandPower = new double[spectrogram.TimeSteps];
            
            for (int t = 0; t < spectrogram.TimeSteps; t++)
            {
                // 時刻tでのパワー値を線形値に変換
                double[] powerValues = new double[spectrogram.FrequencyBins];
                
                for (int f = 0; f < spectrogram.FrequencyBins; f++)
                {
                    // dBから線形パワーに変換
                    powerValues[f] = Math.Pow(10, spectrogram.PowerMatrix[t, f] / 10.0);
                }
                
                // 上限無限の場合の処理
                double effectiveHighFreq = double.IsPositiveInfinity(highFreq) ? 
                    spectrogram.FrequencyAxis[spectrogram.FrequencyAxis.Length - 1] : highFreq;
                
                // シンプソン法で積分計算
                bandPower[t] = IntegrateBandPowerSimpson(spectrogram.FrequencyAxis, powerValues, 
                                                       lowFreq, effectiveHighFreq);
            }
            
            return bandPower;
        }

        /// <summary>
        /// 5バンドパワーの正規化（各時刻で合計=1.0）
        /// Excelの正規化式を完全実装
        /// </summary>
        /// <param name="gamma">γ波パワー配列</param>
        /// <param name="beta">β波パワー配列</param>
        /// <param name="alpha">α波パワー配列</param>
        /// <param name="theta">θ波パワー配列</param>
        /// <param name="delta">δ波パワー配列</param>
        /// <param name="normGamma">正規化γ波（出力）</param>
        /// <param name="normBeta">正規化β波（出力）</param>
        /// <param name="normAlpha">正規化α波（出力）</param>
        /// <param name="normTheta">正規化θ波（出力）</param>
        /// <param name="normDelta">正規化δ波（出力）</param>
        private void NormalizeBandPowers(
            double[] gamma, double[] beta, double[] alpha, 
            double[] theta, double[] delta,
            out double[] normGamma, out double[] normBeta, 
            out double[] normAlpha, out double[] normTheta, out double[] normDelta)
        {
            int length = gamma.Length;
            normGamma = new double[length];
            normBeta = new double[length];
            normAlpha = new double[length];
            normTheta = new double[length];
            normDelta = new double[length];
            
            for (int i = 0; i < length; i++)
            {
                // 各時刻での全バンド合計パワー
                double totalPower = gamma[i] + beta[i] + alpha[i] + theta[i] + delta[i];
                
                // ゼロ割り防止
                if (totalPower > 0)
                {
                    // Excel式: = Gamma_i / (Gamma_i + Beta_i + Alpha_i + Theta_i + Delta_i)
                    normGamma[i] = gamma[i] / totalPower;
                    normBeta[i] = beta[i] / totalPower;
                    normAlpha[i] = alpha[i] / totalPower;
                    normTheta[i] = theta[i] / totalPower;
                    normDelta[i] = delta[i] / totalPower;
                }
                else
                {
                    // 全バンドがゼロの場合は均等分割（理論的には発生しないが安全のため）
                    normGamma[i] = normBeta[i] = normAlpha[i] = normTheta[i] = normDelta[i] = 0.2;
                }
                
                // 検証: 合計が1.0になることを確認（デバッグ用）
                double sum = normGamma[i] + normBeta[i] + normAlpha[i] + normTheta[i] + normDelta[i];
                if (Math.Abs(sum - 1.0) > 1e-10)
                {
                    throw new InvalidOperationException($"正規化エラー: 時刻{i}で合計={sum} (期待値=1.0)");
                }
            }
        }

        /// <summary>
        /// 脳波バンド定義の定数（Pythonアルゴリズムと同じ）
        /// </summary>
        public static class BandDefinitions
        {
            public const double GammaLow = 36.0;    // γ波下限 (Hz) - Python準拠
            public const double GammaHigh = double.PositiveInfinity;  // γ波上限 (Hz) - 上限なし
            
            public const double BetaLow = 13.0;     // β波下限 (Hz)
            public const double BetaHigh = 36.0;    // β波上限 (Hz) - Python準拠
            
            public const double AlphaLow = 8.0;     // α波下限 (Hz)
            public const double AlphaHigh = 13.0;   // α波上限 (Hz)
            
            public const double ThetaLow = 4.0;     // θ波下限 (Hz)
            public const double ThetaHigh = 8.0;    // θ波上限 (Hz)
            
            public const double DeltaLow = 0.0;     // δ波下限 (Hz) - Python準拠
            public const double DeltaHigh = 4.0;    // δ波上限 (Hz)
        }

        /// <summary>
        /// バンドパワー解析の検証（テスト用）
        /// </summary>
        /// <param name="result">解析結果</param>
        /// <returns>検証レポート</returns>
        public string ValidateBandPowerAnalysis(BandPowerAnalysisResult result)
        {
            var report = "=== バンドパワー解析検証レポート ===\n";
            
            // 正規化検証
            bool normalizationOK = result.ValidateNormalization();
            report += $"正規化検証: {(normalizationOK ? "OK" : "NG")}\n";
            
            // パーセント値検証
            bool percentOK = result.ValidatePercentValues();
            report += $"パーセント値検証: {(percentOK ? "OK" : "NG")}\n";
            
            // データサイズ検証
            report += $"データ長: {result.SampleCount}サンプル\n";
            report += $"時間範囲: {result.TimeAxis?.FirstOrDefault():F3} - {result.TimeAxis?.LastOrDefault():F3}秒\n";
            
            // 統計値検証
            if (result.RestBaseline != null)
            {
                bool baselineOK = result.RestBaseline.ValidateNormalization();
                report += $"ベースライン正規化: {(baselineOK ? "OK" : "NG")}\n";
                report += $"ベースライン値: γ={result.RestBaseline.GammaPercent:F4}%, β={result.RestBaseline.BetaPercent:F4}%\n";
            }
            
            if (result.DeltaValues.Count > 0)
            {
                report += $"変化量計算: {result.DeltaValues.Count}個のタスク区間\n";
                foreach (var delta in result.DeltaValues)
                {
                    report += $"  {delta.TaskLabel}: Δγ={delta.DeltaGamma:F4}, Δβ={delta.DeltaBeta:F4}\n";
                }
            }
            
            return report;
        }

        #endregion

        #region Python互換バンドパワー線グラフ生成

        /// <summary>
        /// Pythonアルゴリズムと同じバンドパワー線グラフデータを生成
        /// </summary>
        /// <param name="signalData">入力信号データ</param>
        /// <param name="settings">解析設定</param>
        /// <param name="progress">進捗報告</param>
        /// <returns>バンドパワー線グラフデータ</returns>
        public BandPowerLineData ComputeBandPowerLines(
            SignalData signalData,
            AnalysisSettings settings,
            IProgress<int> progress = null)
        {
            if (signalData?.TimeData == null || signalData.TimeData.Length == 0)
                throw new ArgumentException("信号データが無効です");

            progress?.Report(0);

            // スペクトログラム計算（動的パラメータ使用）
            var spectrogram = ComputeSpectrogram(signalData, settings,
                new Progress<int>(p => progress?.Report(p * 70 / 100))); // 70%まで

            progress?.Report(70);

            // 既存のスペクトログラムを使ってバンドパワーを計算
            return ComputeBandPowerLinesFromSpectrogram(spectrogram, progress, 70);
        }

        /// <summary>
        /// 既存のスペクトログラムからバンドパワー線グラフデータを生成
        /// </summary>
        /// <param name="spectrogram">計算済みのスペクトログラムデータ</param>
        /// <param name="progress">進捗報告</param>
        /// <param name="progressOffset">進捗のオフセット（0-100）</param>
        /// <returns>バンドパワー線グラフデータ</returns>
        public BandPowerLineData ComputeBandPowerLinesFromSpectrogram(
            SpectrogramData spectrogram,
            IProgress<int> progress = null,
            int progressOffset = 0)
        {
            if (spectrogram?.PowerMatrix == null || spectrogram.TimeAxis == null)
                throw new ArgumentException("スペクトログラムデータが無効です");

            // 進捗計算用のスケール（progressOffset から 100 までを使用）
            int remainingProgress = 100 - progressOffset;

            // 各バンドのパワーを抽出（Python定義に合わせて）
            var deltaPower = ExtractBandPower(spectrogram, BandDefinitions.DeltaLow, BandDefinitions.DeltaHigh);    // δ波 (0-4 Hz)
            var thetaPower = ExtractBandPower(spectrogram, BandDefinitions.ThetaLow, BandDefinitions.ThetaHigh);    // θ波 (4-8 Hz)
            var alphaPower = ExtractBandPower(spectrogram, BandDefinitions.AlphaLow, BandDefinitions.AlphaHigh);    // α波 (8-13 Hz)
            var betaPower = ExtractBandPower(spectrogram, BandDefinitions.BetaLow, BandDefinitions.BetaHigh);        // β波 (13-36 Hz)
            var gammaPower = ExtractBandPower(spectrogram, BandDefinitions.GammaLow, BandDefinitions.GammaHigh);    // γ波 (36 Hz以上)

            progress?.Report(progressOffset + (int)(remainingProgress * 0.5));

            // dBスケーリング（Python同様）
            var deltaDb = ConvertToDbScale(deltaPower);
            var thetaDb = ConvertToDbScale(thetaPower);
            var alphaDb = ConvertToDbScale(alphaPower);
            var betaDb = ConvertToDbScale(betaPower);
            var gammaDb = ConvertToDbScale(gammaPower);

            progress?.Report(progressOffset + (int)(remainingProgress * 0.7));

            // 5点移動平均による平滑化（Python同様）
            var deltaSmoothed = ApplyMovingAverage(deltaDb, 5);
            var thetaSmoothed = ApplyMovingAverage(thetaDb, 5);
            var alphaSmoothed = ApplyMovingAverage(alphaDb, 5);
            var betaSmoothed = ApplyMovingAverage(betaDb, 5);
            var gammaSmoothed = ApplyMovingAverage(gammaDb, 5);

            progress?.Report(progressOffset + (int)(remainingProgress * 0.9));

            // 時間軸を0始まりに調整（スペクトログラムの時間軸はウィンドウ中央時刻のため）
            double[] adjustedTimeAxis = new double[spectrogram.TimeAxis.Length];
            double timeOffset = spectrogram.TimeAxis.Length > 0 ? spectrogram.TimeAxis[0] : 0;
            for (int i = 0; i < spectrogram.TimeAxis.Length; i++)
            {
                adjustedTimeAxis[i] = spectrogram.TimeAxis[i] - timeOffset;
            }
            System.Diagnostics.Debug.WriteLine($"バンドパワー時間軸調整: 元の開始={timeOffset:F3}秒 → 0秒始まりに調整");

            // 結果オブジェクト作成
            var result = new BandPowerLineData
            {
                TimeAxis = adjustedTimeAxis,
                DeltaBandDb = deltaSmoothed,
                ThetaBandDb = thetaSmoothed,
                AlphaBandDb = alphaSmoothed,
                BetaBandDb = betaSmoothed,
                GammaBandDb = gammaSmoothed,
                AnalysisTime = DateTime.Now
            };

            progress?.Report(100);

            return result;
        }

        /// <summary>
        /// 線形パワーをdBスケールに変換（Pythonと同じ）
        /// </summary>
        private double[] ConvertToDbScale(double[] linearPower)
        {
            double[] dbScale = new double[linearPower.Length];
            for (int i = 0; i < linearPower.Length; i++)
            {
                // Python同様の数値安定化
                dbScale[i] = 10.0 * Math.Log10(linearPower[i] + 1e-12);
            }
            return dbScale;
        }

        /// <summary>
        /// 移動平均による平滑化（Pythonのnp.convolveと同等）
        /// </summary>
        private double[] ApplyMovingAverage(double[] data, int windowLen)
        {
            if (data == null || data.Length == 0 || windowLen <= 0)
                return data;

            double[] smoothed = new double[data.Length];
            double[] kernel = new double[windowLen];
            
            // 均等な重みを設定
            for (int i = 0; i < windowLen; i++)
            {
                kernel[i] = 1.0 / windowLen;
            }

            // 畳み込み処理（mode='same'）
            for (int i = 0; i < data.Length; i++)
            {
                double sum = 0;
                double weightSum = 0;
                
                int halfWindow = windowLen / 2;
                for (int j = -halfWindow; j <= halfWindow; j++)
                {
                    int index = i + j;
                    if (index >= 0 && index < data.Length)
                    {
                        sum += data[index] * kernel[j + halfWindow];
                        weightSum += kernel[j + halfWindow];
                    }
                }
                
                smoothed[i] = weightSum > 0 ? sum / weightSum : data[i];
            }

            return smoothed;
        }

        #endregion
        
        /// <summary>
        /// 目標とする時間軸解像度を取得
        /// </summary>
        /// <param name="signalData">信号データ</param>
        /// <param name="settings">解析設定</param>
        /// <returns>時間軸解像度（秒）</returns>
        private double GetTargetTimeResolution(SignalData signalData, AnalysisSettings settings)
        {
            double dataTimeDuration = signalData.TimeData.Length / signalData.SamplingRate;
            
            // 直接指定された時間解像度を使用
            double targetResolution = settings.TimeResolution;
            
            // 妥当な範囲に制限
            double minResolution = 0.001; // 1ms
            double maxResolution = dataTimeDuration / 10.0; // データ長の1/10
            
            double clampedResolution = Math.Max(minResolution, Math.Min(targetResolution, maxResolution));
            
            System.Diagnostics.Debug.WriteLine($"時間解像度: 要求{targetResolution:F6}s → 実際{clampedResolution:F6}s");
            
            return clampedResolution;
        }
        
        /// <summary>
        /// 信号データからトリガー位置を検出する
        /// </summary>
        /// <param name="data">信号データ</param>
        /// <param name="threshold">トリガー閾値</param>
        /// <param name="samplingRate">サンプリング周波数</param>
        /// <returns>トリガーのサンプルインデックスリスト</returns>
        public List<int> DetectTriggers(double[] data, double threshold, double samplingRate)
        {
            var triggers = new List<int>();
            bool wasAboveThreshold = false;
            int minIntervalSamples = (int)(samplingRate * 0.1); // 最小100msインターバル
            int lastTrigger = -minIntervalSamples;
            
            for (int i = 1; i < data.Length; i++)
            {
                bool isAboveThreshold = data[i] > threshold;
                
                // 閾値を上向きに超えた瞬間をトリガーとして検出
                if (isAboveThreshold && !wasAboveThreshold && (i - lastTrigger) >= minIntervalSamples)
                {
                    triggers.Add(i);
                    lastTrigger = i;
                }
                
                wasAboveThreshold = isAboveThreshold;
            }
            
            return triggers;
        }
        
        /// <summary>
        /// トリガー位置を中心にエポックを抽出する
        /// </summary>
        /// <param name="data">信号データ</param>
        /// <param name="triggers">トリガー位置リスト</param>
        /// <param name="preSamples">トリガー前のサンプル数</param>
        /// <param name="postSamples">トリガー後のサンプル数</param>
        /// <returns>抽出されたエポック配列</returns>
        public double[][] ExtractEpochs(double[] data, List<int> triggers, int preSamples, int postSamples)
        {
            var epochs = new List<double[]>();
            int epochLength = preSamples + postSamples;
            
            foreach (int trigger in triggers)
            {
                int startIndex = trigger - preSamples;
                int endIndex = trigger + postSamples;
                
                // 範囲チェック
                if (startIndex >= 0 && endIndex <= data.Length)
                {
                    var epoch = new double[epochLength];
                    Array.Copy(data, startIndex, epoch, 0, epochLength);
                    epochs.Add(epoch);
                }
            }
            
            return epochs.ToArray();
        }
        
        /// <summary>
        /// 複数エポックの加算平均を計算する
        /// </summary>
        /// <param name="epochs">エポック配列</param>
        /// <returns>加算平均された1本の波形</returns>
        public double[] ComputeEnsembleAverage(double[][] epochs)
        {
            if (epochs == null || epochs.Length == 0)
                return null;
            
            int epochLength = epochs[0].Length;
            var averaged = new double[epochLength];
            
            // 各サンプル点での平均を計算
            for (int sample = 0; sample < epochLength; sample++)
            {
                double sum = 0;
                for (int epoch = 0; epoch < epochs.Length; epoch++)
                {
                    sum += epochs[epoch][sample];
                }
                averaged[sample] = sum / epochs.Length;
            }
            
            return averaged;
        }
        
        /// <summary>
        /// 信号データに加算平均処理を適用する
        /// </summary>
        /// <param name="original">元の信号データ</param>
        /// <param name="settings">解析設定</param>
        /// <returns>加算平均処理後の信号データ</returns>
        public SignalData ApplyEnsembleAveraging(SignalData original, AnalysisSettings settings)
        {
            if (original?.TimeData == null || !settings.EnableEnsembleAveraging)
                return original;
            
            // サンプル数に変換
            int preSamples = (int)(settings.PreTrigger * original.SamplingRate);
            int postSamples = (int)(settings.PostTrigger * original.SamplingRate);
            
            // トリガー検出
            var triggers = DetectTriggers(original.TimeData, settings.TriggerThreshold, original.SamplingRate);
            
            if (triggers.Count < 2)
            {
                System.Diagnostics.Debug.WriteLine($"加算平均: トリガーが少なすぎます ({triggers.Count}個)");
                return original; // トリガーが少ない場合は元データを返す
            }
            
            // エポック抽出
            var epochs = ExtractEpochs(original.TimeData, triggers, preSamples, postSamples);
            
            if (epochs.Length < 2)
            {
                System.Diagnostics.Debug.WriteLine($"加算平均: 有効なエポックが少なすぎます ({epochs.Length}個)");
                return original;
            }
            
            // 加算平均計算
            var averagedData = ComputeEnsembleAverage(epochs);
            
            // 新しい時間軸を作成（エポック長に合わせる）
            double epochDurationSec = (preSamples + postSamples) / original.SamplingRate;
            
            // 結果をSignalDataとして返す
            var result = new SignalData
            {
                TimeData = averagedData,
                SamplingRate = original.SamplingRate,
                StartTime = original.StartTime,
                DataType = "Ensemble Averaged",
                FileName = original.FileName + " (Averaged)"
            };
            
            System.Diagnostics.Debug.WriteLine($"加算平均完了: {triggers.Count}個のトリガー, {epochs.Length}個のエポック, 平均波形長={averagedData.Length}サンプル ({epochDurationSec:F3}秒)");
            
            return result;
        }
        
        #region HRV解析（C++スライディングFFT ver.8互換）
        
        /// <summary>
        /// Rピーク時刻列からC++版スライディングFFT互換のHRV解析を実行
        /// </summary>
        /// <param name="rpeaks">Rピーク時刻リスト [秒]</param>
        /// <returns>HRV解析結果</returns>
        public HrvAnalysisResult AnalyzeHrvWithSlidingFft(List<double> rpeaks)
        {
            if (rpeaks == null || rpeaks.Count < 10)
                throw new ArgumentException("HRV解析には最低10個のRピークが必要です");
            
            // Step 1: IHR10Hz変換（既存機能を活用）
            double[] ihrData = IhrInterpolator.BuildIhr10Hz(rpeaks);
            
            // Step 2: スライディングFFT解析
            var slidingAnalyzer = new SlidingFftAnalyzer(this);
            var result = slidingAnalyzer.AnalyzeHrv(ihrData);
            
            return result;
        }
        
        /// <summary>
        /// HRV解析結果をC++互換形式で保存
        /// </summary>
        /// <param name="result">HRV解析結果</param>
        /// <param name="outputPath">出力ファイルパス</param>
        public void SaveHrvResults(HrvAnalysisResult result, string outputPath)
        {
            HrvResultExporter.SaveToFile(result, outputPath);
        }
        
        /// <summary>
        /// HRV解析結果を複数形式で一括保存
        /// </summary>
        /// <param name="result">HRV解析結果</param>
        /// <param name="baseOutputPath">ベース出力パス（拡張子なし）</param>
        public void SaveHrvResultsAllFormats(HrvAnalysisResult result, string baseOutputPath)
        {
            HrvResultExporter.SaveAllFormats(result, baseOutputPath);
        }
        
        /// <summary>
        /// 既存のComputeDFTメソッドをpublicアクセス用に公開
        /// スライディングFFT解析で使用
        /// </summary>
        /// <param name="timeData">時間領域データ</param>
        /// <returns>DFT結果（複素数配列）</returns>
        public Complex[] ComputeDFTPublic(double[] timeData)
        {
            return ComputeDFT(timeData);
        }
        
        #endregion
        
        #region シンプソン法積分
        
        /// <summary>
        /// シンプソン法による数値積分
        /// バンドパワー計算に使用する高精度積分手法
        /// </summary>
        /// <param name="frequencies">周波数軸配列</param>
        /// <param name="powerValues">パワー値配列（線形値）</param>
        /// <param name="lowFreq">積分範囲下限（Hz）</param>
        /// <param name="highFreq">積分範囲上限（Hz）</param>
        /// <returns>積分結果（パワー値）</returns>
        private double IntegrateBandPowerSimpson(double[] frequencies, double[] powerValues, 
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
            
            // 積分範囲が無効な場合
            if (startIndex == -1 || endIndex == -1 || startIndex >= endIndex)
                return 0.0;
            
            int n = endIndex - startIndex;
            
            // 点が少ない場合の処理
            if (n < 2)
            {
                // 台形則で近似
                if (n == 1)
                    return 0.5 * (powerValues[startIndex] + powerValues[endIndex]) * 
                           (frequencies[endIndex] - frequencies[startIndex]);
                return 0.0;
            }
            
            // シンプソン法は偶数個の区間が必要
            if (n % 2 == 1) n--;
            
            double h = (frequencies[startIndex + n] - frequencies[startIndex]) / n;
            
            // Simpson則の公式: ∫f(x)dx ≈ h/3 * (f₀ + 4f₁ + 2f₂ + 4f₃ + ... + fₙ)
            double integral = powerValues[startIndex]; // f₀
            
            for (int i = 1; i < n; i++)
            {
                double coefficient = (i % 2 == 1) ? 4.0 : 2.0;
                integral += coefficient * powerValues[startIndex + i];
            }
            
            integral += powerValues[startIndex + n]; // fₙ
            integral *= h / 3.0;
            
            return integral;
        }
        
        #endregion
        
        #region 加算平均スペクトログラム

        /// <summary>
        /// 加算平均スペクトログラムを生成
        /// 信号をtotalRange長のブロックに分割し、各ブロック内で
        /// DFT窓(=totalRange/分割数)を50%オーバーラップでずらしながらDFTを計算、
        /// 全DFTの線形パワーを加算平均して1スペクトルを得る。
        /// ブロックごとのスペクトルを並べてスペクトログラムを構成する。
        /// </summary>
        public SpectrogramData GenerateEnsembleAveragedSpectrogram(SignalData signalData, AnalysisSettings settings, IProgress<int> progress = null)
        {
            double basicTimeUnit = settings.BasicTimeUnit;  // DFT窓 = totalRange / divisionCount
            int divisionCount = settings.DivisionCount;
            if (divisionCount <= 0) divisionCount = 1;

            double totalRange = basicTimeUnit * divisionCount;
            double samplingRate = signalData.SamplingRate;
            int totalRangeSamples = Math.Max(1, (int)(totalRange * samplingRate));
            int dataLength = signalData.TimeData.Length;

            // ブロック数（totalRangeに満たない端数は切り捨て）
            int numBlocks = Math.Max(1, dataLength / totalRangeSamples);

            // DFTパラメータ（50%オーバーラップ）
            var blockSettings = new AnalysisSettings
            {
                WindowFunction = settings.WindowFunction,
#pragma warning disable CS0618
                WindowDuration = basicTimeUnit,
#pragma warning restore CS0618
                BasicTimeUnit = basicTimeUnit,
                TimeResolution = basicTimeUnit / 2.0,  // 50%オーバーラップ
                MinFrequency = settings.MinFrequency,
                MaxFrequency = settings.MaxFrequency,
                DisplayMinFreq = settings.DisplayMinFreq,
                DisplayMaxFreq = settings.DisplayMaxFreq,
                EnableInterpolation = settings.EnableInterpolation,
                EnableDcRemoval = settings.EnableDcRemoval
            };

            // 周波数軸情報を事前計算
            int nperseg = blockSettings.GetWindowSize(samplingRate);
            nperseg = Math.Max(2, Math.Min(nperseg, totalRangeSamples));
            int numFreqBins = nperseg / 2 + 1;
            double freqResolution = samplingRate / nperseg;

            var result = new SpectrogramData(numBlocks, numFreqBins);
            for (int f = 0; f < numFreqBins; f++)
                result.FrequencyAxis[f] = f * freqResolution;

            // 各ブロックを処理
            for (int blockIdx = 0; blockIdx < numBlocks; blockIdx++)
            {
                int blockStart = blockIdx * totalRangeSamples;
                int blockLength = Math.Min(totalRangeSamples, dataLength - blockStart);

                // ブロックの信号を抽出
                var blockData = new double[blockLength];
                Array.Copy(signalData.TimeData, blockStart, blockData, 0, blockLength);

                var blockSignal = new SignalData
                {
                    TimeData = blockData,
                    SamplingRate = samplingRate
                };

                // ブロック内のスペクトログラムを計算（50%オーバーラップ）
                var blockSpectrogram = ComputeSpectrogram(blockSignal, blockSettings);

                // 時間軸：ブロックの中央時刻
                result.TimeAxis[blockIdx] = (blockStart + totalRangeSamples / 2.0) / samplingRate;

                // ブロック内の全フレームを加算平均 → 1スペクトル
                int frameCount = blockSpectrogram.TimeSteps;
                for (int f = 0; f < numFreqBins; f++)
                {
                    double powerSum = 0.0;
                    for (int t = 0; t < frameCount; t++)
                    {
                        double linearPower = Math.Pow(10, blockSpectrogram.PowerMatrix[t, f] / 10.0);
                        powerSum += linearPower;
                    }
                    double avgPower = powerSum / frameCount;
                    result.PowerMatrix[blockIdx, f] = 10.0 * Math.Log10(Math.Max(avgPower, 1e-12));
                }

                progress?.Report((int)((double)(blockIdx + 1) / numBlocks * 100));
            }

            result.UpdatePowerRange();
            return result;
        }

        #endregion
    }
}