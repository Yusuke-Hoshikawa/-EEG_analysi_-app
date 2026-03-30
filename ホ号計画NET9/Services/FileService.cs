using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using ホ号計画.Models;

namespace ホ号計画.Services
{
    /// <summary>
    /// データタイプ判定結果
    /// </summary>
    public enum DataType
    {
        EEG,        // 脳波データ
        HeartRate   // 心拍データ（Rピーク時刻）
    }

    /// <summary>
    /// 心拍データのサブタイプ判定結果
    /// </summary>
    public enum HeartRateSubType
    {
        RpeakTimes,      // Rピーク時刻のリスト（従来形式）
        PreSampledBpm    // 時系列心拍数データ（時間, BPM）
    }

    public class FileService
    {
        /// <summary>
        /// ファイルロック対応のリトライ機能付きファイル読み込み
        /// </summary>
        private string[] ReadFileWithRetry(string filePath, int maxRetries = 5)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // 読み取り専用でファイルを開き、他プロセスと共有
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fileStream))
                    {
                        var lines = new List<string>();
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                        return lines.ToArray();
                    }
                }
                catch (IOException ex) when (attempt < maxRetries - 1)
                {
                    // ファイルが使用中の場合、短時間待機してリトライ
                    Thread.Sleep(500 + attempt * 200); // 0.5秒から徐々に延長
                    continue;
                }
                catch (IOException ex) when (attempt == maxRetries - 1)
                {
                    // 最終リトライでも失敗した場合
                    throw new IOException($"ファイルにアクセスできません。ファイルが他のアプリケーション（Excelなど）で開かれている可能性があります。ファイルを閉じてから再試行してください。({ex.Message})", ex);
                }
            }
            
            return new string[0]; // 到達しないが、コンパイラ警告回避
        }
        /// <summary>
        /// CSVファイルを読み込み、データタイプを自動判定して適切に処理
        /// 脳波: そのまま読み込み
        /// 心拍: HRV補間処理を自動実行
        /// </summary>
        public SignalData LoadCsvFile(string filePath, double samplingRate = 1000.0)
        {
            try
            {
                var lines = ReadFileWithRetry(filePath);
                
                // データタイプを自動判定
                var detectedType = DetectDataType(lines, filePath);
                
                if (detectedType == DataType.HeartRate)
                {
                    // 心拍データの場合: Rピーク時刻を読み込み→HRV補間処理
                    return LoadHeartRateData(lines, filePath, samplingRate);
                }
                else
                {
                    // 脳波データの場合: 従来の処理（変更なし）
                    return LoadEegData(lines, filePath, samplingRate);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"CSV読み込みエラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// データタイプを自動判定（ファイル名とヘッダーから推定）
        /// </summary>
        private DataType DetectDataType(string[] lines, string filePath)
        {
            var fileName = Path.GetFileName(filePath).ToLower();
            
            // ファイル名による判定
            if (fileName.Contains("heart") || fileName.Contains("hr") || fileName.Contains("rr") || 
                fileName.Contains("ecg") || fileName.Contains("rpeak") || fileName.Contains("心拍"))
            {
                return DataType.HeartRate;
            }
            
            // ヘッダー行による判定
            foreach (var line in lines.Take(3)) // 最初の3行をチェック
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var lowerLine = line.ToLower();
                if (lowerLine.Contains("rpeak") || lowerLine.Contains("r_peak") || 
                    lowerLine.Contains("heartrate") || lowerLine.Contains("hr") ||
                    lowerLine.Contains("rr_interval"))
                {
                    return DataType.HeartRate;
                }
            }
            
            // デフォルトは脳波
            return DataType.EEG;
        }

        /// <summary>
        /// 心拍データのサブタイプを判定（Rピーク時刻 vs 時系列心拍数）
        /// </summary>
        private HeartRateSubType DetectHeartRateSubType(string[] lines)
        {
            var timeValues = new List<double>();
            var secondColumnValues = new List<double>();
            bool isFirstLine = true;

            foreach (var line in lines.Take(50)) // 最初の50行で判定
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                // ヘッダー行をスキップ
                if (isFirstLine && (line.ToLower().Contains("time") || line.ToLower().Contains("sec") ||
                                    line.ToLower().Contains("rpeak") || line.ToLower().Contains("bpm")))
                {
                    isFirstLine = false;
                    continue;
                }
                isFirstLine = false;

                var values = line.Split(',', ';', '\t');

                // 2列以上のデータがある場合のみ判定
                if (values.Length >= 2)
                {
                    if (double.TryParse(values[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double timeValue))
                    {
                        timeValues.Add(timeValue);
                    }
                    if (double.TryParse(values[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double secondValue))
                    {
                        secondColumnValues.Add(secondValue);
                    }
                }
            }

            // 判定条件:
            // 1. 時間間隔が0.2秒未満（高サンプリング）
            // 2. 第2列の値が30〜250の範囲（生理学的BPM範囲）
            if (timeValues.Count >= 3 && secondColumnValues.Count >= 3)
            {
                // 時間間隔を計算
                var intervals = new List<double>();
                for (int i = 1; i < Math.Min(timeValues.Count, 20); i++)
                {
                    double dt = timeValues[i] - timeValues[i - 1];
                    if (dt > 0) intervals.Add(dt);
                }

                if (intervals.Count >= 2)
                {
                    double avgInterval = intervals.Average();
                    double avgSecondColumn = secondColumnValues.Average();
                    double minSecondColumn = secondColumnValues.Min();
                    double maxSecondColumn = secondColumnValues.Max();

                    System.Diagnostics.Debug.WriteLine($"心拍サブタイプ判定: 平均時間間隔={avgInterval:F4}s, 第2列平均={avgSecondColumn:F1}, 範囲={minSecondColumn:F1}-{maxSecondColumn:F1}");

                    // 時系列心拍数データの条件:
                    // - 時間間隔が0.2秒未満（5Hz以上のサンプリング）
                    // - 第2列の値が30〜250の範囲内（BPMとして妥当）
                    if (avgInterval < 0.2 &&
                        minSecondColumn >= 20 && maxSecondColumn <= 300 &&
                        avgSecondColumn >= 30 && avgSecondColumn <= 200)
                    {
                        System.Diagnostics.Debug.WriteLine("→ 時系列心拍数データ（PreSampledBpm）と判定");
                        return HeartRateSubType.PreSampledBpm;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("→ Rピーク時刻データ（RpeakTimes）と判定");
            return HeartRateSubType.RpeakTimes;
        }

        /// <summary>
        /// CSVの時間軸データからサンプリング周波数を自動推定
        /// </summary>
        private double EstimateSamplingRateFromTimeColumn(string[] lines)
        {
            var timeValues = new List<double>();
            bool isFirstLine = true;
            
            foreach (var line in lines.Take(100)) // 最初の100行で推定
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;
                
                // ヘッダー行をスキップ
                if (isFirstLine && (line.ToLower().Contains("time") || line.ToLower().Contains("sec")))
                {
                    isFirstLine = false;
                    continue;
                }
                isFirstLine = false;
                
                var values = line.Split(',', ';', '\t');
                if (values.Length >= 1 && double.TryParse(values[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double timeValue))
                {
                    timeValues.Add(timeValue);
                }
                
                if (timeValues.Count >= 20) break; // 20サンプルで十分
            }
            
            if (timeValues.Count >= 2)
            {
                // 時間間隔を計算
                var intervals = new List<double>();
                for (int i = 1; i < Math.Min(timeValues.Count, 10); i++)
                {
                    double dt = timeValues[i] - timeValues[i-1];
                    if (dt > 0) intervals.Add(dt);
                }
                
                if (intervals.Count >= 2)
                {
                    double avgInterval = intervals.Average();
                    double estimatedFs = 1.0 / avgInterval;
                    
                    System.Diagnostics.Debug.WriteLine($"時間軸から推定: 平均間隔={avgInterval:F6}s, 推定サンプリング周波数={estimatedFs:F1}Hz");
                    
                    // 妥当性チェック（1Hz～100kHzの範囲）
                    if (estimatedFs >= 1.0 && estimatedFs <= 100000.0)
                    {
                        return Math.Round(estimatedFs); // 整数に丸める
                    }
                }
            }
            
            return -1; // 推定失敗
        }
        
        /// <summary>
        /// 脳波データ読み込み（従来の処理と同じ）
        /// </summary>
        private SignalData LoadEegData(string[] lines, string filePath, double samplingRate)
        {
            // CSVから自動推定を試行
            double estimatedFs = EstimateSamplingRateFromTimeColumn(lines);
            if (estimatedFs > 0)
            {
                samplingRate = estimatedFs;
                System.Diagnostics.Debug.WriteLine($"サンプリング周波数を自動推定: {samplingRate}Hz");
                System.Console.WriteLine($"サンプリング周波数を自動推定: {samplingRate}Hz");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"サンプリング周波数推定失敗、デフォルト値使用: {samplingRate}Hz");
            }
            
            var dataList = new List<double>();

            bool isFirstLine = true;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                // ヘッダー行をスキップ（time,amplitude等）
                if (isFirstLine && (line.ToLower().Contains("time") || line.ToLower().Contains("amplitude")))
                {
                    isFirstLine = false;
                    continue;
                }
                isFirstLine = false;

                var values = line.Split(',', ';', '\t');
                
                // 2列形式（time,amplitude）の場合、amplitude列（values[1]）を読み込み
                if (values.Length >= 2 && double.TryParse(values[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double amplitude))
                {
                    dataList.Add(amplitude);
                }
                // 1列形式（amplitude only）の場合、最初の列を読み込み
                else if (values.Length == 1 && double.TryParse(values[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double singleValue))
                {
                    dataList.Add(singleValue);
                }
            }

            return new SignalData
            {
                TimeData = dataList.ToArray(),
                SamplingRate = samplingRate,
                StartTime = DateTime.Now,
                DataType = "EEG",
                FileName = Path.GetFileName(filePath)
            };
        }

        /// <summary>
        /// 心拍データ読み込み＋HRV補間処理（脳波には影響なし）
        /// サブタイプを自動判定し、適切な処理を実行
        /// </summary>
        private SignalData LoadHeartRateData(string[] lines, string filePath, double samplingRate)
        {
            // 心拍データのサブタイプを判定
            var subType = DetectHeartRateSubType(lines);

            if (subType == HeartRateSubType.PreSampledBpm)
            {
                // 時系列心拍数データの場合: BPM→IHR変換＋10Hzリサンプリング
                return LoadPreSampledHeartRateData(lines, filePath);
            }
            else
            {
                // Rピーク時刻データの場合: 従来処理
                return LoadRpeakHeartRateData(lines, filePath);
            }
        }

        /// <summary>
        /// Rピーク時刻データの読み込み＋HRV補間処理（従来処理）
        /// </summary>
        private SignalData LoadRpeakHeartRateData(string[] lines, string filePath)
        {
            var rpeakTimes = new List<double>();

            bool isFirstLine = true;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                // ヘッダー行をスキップ
                if (isFirstLine && (line.ToLower().Contains("time") || line.ToLower().Contains("rpeak") ||
                                    line.ToLower().Contains("sec") || line.ToLower().Contains("timestamp")))
                {
                    isFirstLine = false;
                    continue;
                }
                isFirstLine = false;

                var values = line.Split(',', ';', '\t');

                // Rピーク時刻を読み込み（通常は第1列）
                if (values.Length >= 1 && double.TryParse(values[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double peakTime))
                {
                    rpeakTimes.Add(peakTime);
                }
                // 2列目がRピーク時刻の場合もサポート
                else if (values.Length >= 2 && double.TryParse(values[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double peakTime2))
                {
                    rpeakTimes.Add(peakTime2);
                }
            }

            if (rpeakTimes.Count < 2)
            {
                throw new InvalidOperationException("有効なRピーク時刻データが不足しています（最低2個必要）");
            }

            // HRV補間処理を自動実行（10Hz等間隔サンプリング）
            double[] ihrData;
            try
            {
                ihrData = IhrInterpolator.BuildIhr10Hz(rpeakTimes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"HRV補間処理エラー: {ex.Message}", ex);
            }

            return new SignalData
            {
                TimeData = ihrData,
                SamplingRate = 10.0, // HRV補間結果は10Hzサンプリング
                StartTime = DateTime.Now,
                DataType = "HeartRate", // 心拍データであることを明示
                FileName = Path.GetFileName(filePath)
            };
        }

        /// <summary>
        /// 時系列心拍数データ（時間, BPM）の読み込み＋10Hzリサンプリング
        /// </summary>
        private SignalData LoadPreSampledHeartRateData(string[] lines, string filePath)
        {
            var timePoints = new List<double>();
            var bpmValues = new List<double>();

            bool isFirstLine = true;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                // ヘッダー行をスキップ
                if (isFirstLine && (line.ToLower().Contains("time") || line.ToLower().Contains("sec") ||
                                    line.ToLower().Contains("bpm") || line.ToLower().Contains("hr")))
                {
                    isFirstLine = false;
                    continue;
                }
                isFirstLine = false;

                var values = line.Split(',', ';', '\t');

                // 第1列: 時間、第2列: 心拍数(BPM)
                if (values.Length >= 2 &&
                    double.TryParse(values[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double time) &&
                    double.TryParse(values[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double bpm))
                {
                    timePoints.Add(time);
                    bpmValues.Add(bpm);
                }
            }

            if (timePoints.Count < 2)
            {
                throw new InvalidOperationException("有効な時系列心拍数データが不足しています（最低2個必要）");
            }

            System.Diagnostics.Debug.WriteLine($"時系列心拍数データ読み込み: {timePoints.Count}サンプル, 時間範囲={timePoints.First():F2}-{timePoints.Last():F2}秒");
            System.Console.WriteLine($"時系列心拍数データ読み込み: {timePoints.Count}サンプル");

            // BPM → IHR[Hz] 変換＋10Hzリサンプリング
            double[] ihrData;
            try
            {
                ihrData = IhrInterpolator.ResampleBpmTo10HzIhr(timePoints, bpmValues);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"心拍数データのリサンプリングエラー: {ex.Message}", ex);
            }

            return new SignalData
            {
                TimeData = ihrData,
                SamplingRate = 10.0, // リサンプリング結果は10Hzサンプリング
                StartTime = DateTime.Now,
                DataType = "HeartRate", // 心拍データであることを明示
                FileName = Path.GetFileName(filePath)
            };
        }

        public void SaveSpectrogramData(SpectrogramData spectrogram, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("Time,Frequency,Power");
                    
                    for (int t = 0; t < spectrogram.TimeSteps; t++)
                    {
                        for (int f = 0; f < spectrogram.FrequencyBins; f++)
                        {
                            writer.WriteLine($"{spectrogram.TimeAxis[t]:F3},{spectrogram.FrequencyAxis[f]:F3},{spectrogram.PowerMatrix[t, f]:E6}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"ファイル保存エラー: {ex.Message}", ex);
            }
        }
    }
}