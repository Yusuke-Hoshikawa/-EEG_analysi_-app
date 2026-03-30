using System;

namespace ホ号計画.Models
{
    /// <summary>
    /// HRV解析結果データモデル（C++スライディングFFT ver.8互換）
    /// </summary>
    public class HrvAnalysisResult
    {
        /// <summary>
        /// 時間軸データ [秒]
        /// </summary>
        public double[] TimeAxis { get; set; }
        
        /// <summary>
        /// LF成分パワー [ms²]
        /// </summary>
        public double[] LfPower { get; set; }
        
        /// <summary>
        /// HF成分パワー [ms²]
        /// </summary>
        public double[] HfPower { get; set; }
        
        /// <summary>
        /// LF/HF比
        /// </summary>
        public double[] LfHfRatio { get; set; }
        
        /// <summary>
        /// 解析時刻
        /// </summary>
        public DateTime AnalysisTime { get; set; }
        
        /// <summary>
        /// データ長（サンプル数）
        /// </summary>
        public int SampleCount => TimeAxis?.Length ?? 0;
        
        /// <summary>
        /// 統計データ - LF成分
        /// </summary>
        public double LfMean { get; set; }
        public double LfStd { get; set; }
        
        /// <summary>
        /// 統計データ - HF成分
        /// </summary>
        public double HfMean { get; set; }
        public double HfStd { get; set; }
        
        /// <summary>
        /// 統計データ - LF/HF比
        /// </summary>
        public double LfHfRatioMean { get; set; }
        public double LfHfRatioStd { get; set; }
        
        /// <summary>
        /// 使用されたウィンドウ長 [秒]
        /// </summary>
        public double WindowDuration { get; set; }
        
        /// <summary>
        /// サンプリング周波数 [Hz]
        /// </summary>
        public double SamplingRate { get; set; }
        
        public HrvAnalysisResult()
        {
            AnalysisTime = DateTime.Now;
        }
        
        /// <summary>
        /// 統計値を計算
        /// </summary>
        public void CalculateStatistics()
        {
            if (LfPower != null && LfPower.Length > 0)
            {
                LfMean = CalculateMean(LfPower);
                LfStd = CalculateStandardDeviation(LfPower, LfMean);
            }
            
            if (HfPower != null && HfPower.Length > 0)
            {
                HfMean = CalculateMean(HfPower);
                HfStd = CalculateStandardDeviation(HfPower, HfMean);
            }
            
            if (LfHfRatio != null && LfHfRatio.Length > 0)
            {
                LfHfRatioMean = CalculateMean(LfHfRatio);
                LfHfRatioStd = CalculateStandardDeviation(LfHfRatio, LfHfRatioMean);
            }
        }
        
        private static double CalculateMean(double[] data)
        {
            double sum = 0;
            for (int i = 0; i < data.Length; i++)
            {
                sum += data[i];
            }
            return sum / data.Length;
        }
        
        private static double CalculateStandardDeviation(double[] data, double mean)
        {
            double sumSquaredDiff = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double diff = data[i] - mean;
                sumSquaredDiff += diff * diff;
            }
            return Math.Sqrt(sumSquaredDiff / data.Length);
        }
    }
}