using System;
using System.Collections.Generic;
using ホ号計画.Models;

namespace ホ号計画.Services
{
    /// <summary>
    /// HRV解析機能のテスト実行クラス
    /// C++版との互換性検証用
    /// </summary>
    public static class HrvTestRunner
    {
        /// <summary>
        /// 基本的なHRV解析テストを実行
        /// </summary>
        public static void RunBasicTest()
        {
            Console.WriteLine("=== HRV解析テスト開始 ===");
            
            try
            {
                // テスト用Rピークデータ生成（60bpm、10分間）
                var testRpeaks = GenerateTestRpeaks(60.0, 600.0);
                Console.WriteLine($"テストRピークデータ: {testRpeaks.Count}個, 10分間, 60bpm");
                
                // AnalysisService初期化
                var analysisService = new AnalysisService();
                
                // HRV解析実行
                Console.WriteLine("HRV解析実行中...");
                var result = analysisService.AnalyzeHrvWithSlidingFft(testRpeaks);
                
                // 結果検証
                Console.WriteLine($"解析完了: {result.SampleCount}フレーム");
                Console.WriteLine($"時間範囲: {result.TimeAxis[0]:F1} - {result.TimeAxis[result.SampleCount-1]:F1}秒");
                Console.WriteLine($"LF平均: {result.LfMean:F6} ms²");
                Console.WriteLine($"HF平均: {result.HfMean:F6} ms²");
                Console.WriteLine($"LF/HF比平均: {result.LfHfRatioMean:F6}");
                
                // ファイル出力テスト
                string outputPath = @"C:\temp\hrv_test_results.csv";
                analysisService.SaveHrvResults(result, outputPath);
                Console.WriteLine($"結果保存: {outputPath}");
                
                Console.WriteLine("=== テスト成功 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== テスト失敗 ===");
                Console.WriteLine($"エラー: {ex.Message}");
                Console.WriteLine($"スタックトレース: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// テスト用Rピークデータ生成
        /// </summary>
        /// <param name="bpm">心拍数 [bpm]</param>
        /// <param name="durationSec">データ長 [秒]</param>
        /// <returns>Rピーク時刻リスト [秒]</returns>
        private static List<double> GenerateTestRpeaks(double bpm, double durationSec)
        {
            var rpeaks = new List<double>();
            
            // 基本RR間隔
            double baseRR = 60.0 / bpm; // 秒
            
            // わずかな変動を加える（HRV模擬）
            var random = new Random(42); // 再現性のため固定シード
            
            double currentTime = 0.0;
            while (currentTime < durationSec)
            {
                rpeaks.Add(currentTime);
                
                // ±5%の変動を加える
                double variation = 1.0 + (random.NextDouble() - 0.5) * 0.1;
                currentTime += baseRR * variation;
            }
            
            return rpeaks;
        }
        
        /// <summary>
        /// C++版データとの比較テスト（将来実装）
        /// </summary>
        public static void RunCompatibilityTest(string cppResultPath, List<double> testRpeaks)
        {
            // TODO: C++版の出力ファイルと比較
            Console.WriteLine("C++互換性テストは未実装です");
        }
    }
}