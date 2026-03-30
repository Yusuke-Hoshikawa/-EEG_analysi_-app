using System;
using System.IO;
using System.Globalization;
using System.Text;
using ホ号計画.Models;

namespace ホ号計画.Services
{
    /// <summary>
    /// HRV解析結果のC++互換出力クラス
    /// C++ Slide_FFT_ver8と同じ形式でファイル出力
    /// </summary>
    public static class HrvResultExporter
    {
        /// <summary>
        /// C++版と同じ形式でCSVファイルに出力
        /// </summary>
        /// <param name="result">HRV解析結果</param>
        /// <param name="outputPath">出力ファイルパス</param>
        public static void SaveToFile(HrvAnalysisResult result, string outputPath)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            
            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("出力パスが指定されていません", nameof(outputPath));
            
            // 出力ディレクトリが存在しない場合は作成
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var sb = new StringBuilder();
            
            // ヘッダー情報（C++版準拠）
            sb.AppendLine("# HRV Analysis Results (Sliding FFT ver.8 C# Implementation)");
            sb.AppendLine($"# Analysis Date: {result.AnalysisTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"# Window Duration: {result.WindowDuration} sec");
            sb.AppendLine($"# Sampling Rate: {result.SamplingRate} Hz");
            sb.AppendLine($"# Data Length: {result.SampleCount} frames");
            sb.AppendLine("#");
            sb.AppendLine("# LF Band: 0.04-0.15 Hz");
            sb.AppendLine("# HF Band: 0.15-0.40 Hz");
            sb.AppendLine("#");
            sb.AppendLine("# Columns:");
            sb.AppendLine("# 1. Time [sec]");
            sb.AppendLine("# 2. LF Power [ms²]");
            sb.AppendLine("# 3. HF Power [ms²]");
            sb.AppendLine("# 4. LF/HF Ratio");
            sb.AppendLine("#");
            
            // 統計情報（C++版準拠）
            sb.AppendLine("# Statistical Summary:");
            sb.AppendLine($"# LF Power  - Mean: {result.LfMean:F6}, Std: {result.LfStd:F6}");
            sb.AppendLine($"# HF Power  - Mean: {result.HfMean:F6}, Std: {result.HfStd:F6}");
            sb.AppendLine($"# LF/HF Ratio - Mean: {result.LfHfRatioMean:F6}, Std: {result.LfHfRatioStd:F6}");
            sb.AppendLine("#");
            
            // カラムヘッダー
            sb.AppendLine("Time,LF_Power,HF_Power,LF_HF_Ratio");
            
            // データ行（C++版の精度に合わせて6桁出力）
            for (int i = 0; i < result.SampleCount; i++)
            {
                string line = string.Format(CultureInfo.InvariantCulture,
                    "{0:F3},{1:F6},{2:F6},{3:F6}",
                    result.TimeAxis[i],
                    result.LfPower[i],
                    result.HfPower[i],
                    result.LfHfRatio[i]);
                    
                sb.AppendLine(line);
            }
            
            // ファイル書き込み（UTF-8、BOMなし）
            File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(false));
        }
        
        /// <summary>
        /// 簡易サマリーファイルを出力（C++版追加形式）
        /// </summary>
        /// <param name="result">HRV解析結果</param>
        /// <param name="summaryPath">サマリーファイルパス</param>
        public static void SaveSummary(HrvAnalysisResult result, string summaryPath)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            
            var sb = new StringBuilder();
            
            // サマリーヘッダー
            sb.AppendLine("=== HRV Analysis Summary ===");
            sb.AppendLine($"Analysis Date: {result.AnalysisTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Data Duration: {result.SampleCount * (result.WindowDuration / 2.0) / result.SamplingRate:F1} sec");
            sb.AppendLine($"Number of Frames: {result.SampleCount}");
            sb.AppendLine();
            
            // 統計値
            sb.AppendLine("=== Statistical Results ===");
            sb.AppendLine("LF Component (0.04-0.15 Hz):");
            sb.AppendLine($"  Mean: {result.LfMean:F6} ms²");
            sb.AppendLine($"  Std:  {result.LfStd:F6} ms²");
            sb.AppendLine();
            
            sb.AppendLine("HF Component (0.15-0.40 Hz):");
            sb.AppendLine($"  Mean: {result.HfMean:F6} ms²");
            sb.AppendLine($"  Std:  {result.HfStd:F6} ms²");
            sb.AppendLine();
            
            sb.AppendLine("LF/HF Ratio:");
            sb.AppendLine($"  Mean: {result.LfHfRatioMean:F6}");
            sb.AppendLine($"  Std:  {result.LfHfRatioStd:F6}");
            sb.AppendLine();
            
            // パラメータ情報
            sb.AppendLine("=== Analysis Parameters ===");
            sb.AppendLine($"Window Duration: {result.WindowDuration} sec");
            sb.AppendLine($"Sampling Rate: {result.SamplingRate} Hz");
            sb.AppendLine($"Window Overlap: 50% (Half-shift)");
            sb.AppendLine($"Integration Method: Simpson's Rule");
            sb.AppendLine($"Window Function: Hamming");
            
            // ファイル書き込み
            File.WriteAllText(summaryPath, sb.ToString(), new UTF8Encoding(false));
        }
        
        /// <summary>
        /// 複数形式での一括出力（C++版互換セット）
        /// </summary>
        /// <param name="result">HRV解析結果</param>
        /// <param name="baseOutputPath">ベース出力パス（拡張子なし）</param>
        public static void SaveAllFormats(HrvAnalysisResult result, string baseOutputPath)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            
            // メインデータファイル
            string mainPath = baseOutputPath + "_hrv_results.csv";
            SaveToFile(result, mainPath);
            
            // サマリーファイル
            string summaryPath = baseOutputPath + "_hrv_summary.txt";
            SaveSummary(result, summaryPath);
            
            // LF成分のみ
            string lfPath = baseOutputPath + "_lf_component.csv";
            SaveSingleComponent(result.TimeAxis, result.LfPower, "LF Power [ms²]", lfPath);
            
            // HF成分のみ
            string hfPath = baseOutputPath + "_hf_component.csv";
            SaveSingleComponent(result.TimeAxis, result.HfPower, "HF Power [ms²]", hfPath);
            
            // LF/HF比のみ
            string ratioPath = baseOutputPath + "_lfhf_ratio.csv";
            SaveSingleComponent(result.TimeAxis, result.LfHfRatio, "LF/HF Ratio", ratioPath);
        }
        
        /// <summary>
        /// 単一成分データの出力
        /// </summary>
        private static void SaveSingleComponent(double[] timeAxis, double[] data, string dataLabel, string outputPath)
        {
            var sb = new StringBuilder();
            
            // ヘッダー
            sb.AppendLine($"# {dataLabel} Data");
            sb.AppendLine($"# Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("#");
            sb.AppendLine($"Time,{dataLabel.Replace(" ", "_").Replace("[", "").Replace("]", "")}");
            
            // データ
            for (int i = 0; i < timeAxis.Length; i++)
            {
                sb.AppendLine($"{timeAxis[i]:F3},{data[i]:F6}");
            }
            
            File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(false));
        }
    }
}