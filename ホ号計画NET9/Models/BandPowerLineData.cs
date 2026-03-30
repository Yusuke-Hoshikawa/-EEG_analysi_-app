using System;

namespace ホ号計画.Models
{
    /// <summary>
    /// Pythonアルゴリズム互換のバンドパワー線グラフデータ
    /// </summary>
    public class BandPowerLineData
    {
        #region 時間軸・メタデータ
        
        /// <summary>時間軸（秒）</summary>
        public double[] TimeAxis { get; set; }
        
        /// <summary>解析開始時刻</summary>
        public DateTime AnalysisTime { get; set; }
        
        /// <summary>データ長（サンプル数）</summary>
        public int SampleCount => TimeAxis?.Length ?? 0;
        
        #endregion

        #region 各バンドのdBスケールパワー（平滑化済み）
        
        /// <summary>δ波パワー（0-4 Hz）[dB]</summary>
        public double[] DeltaBandDb { get; set; }
        
        /// <summary>θ波パワー（4-8 Hz）[dB]</summary>
        public double[] ThetaBandDb { get; set; }
        
        /// <summary>α波パワー（8-13 Hz）[dB]</summary>
        public double[] AlphaBandDb { get; set; }
        
        /// <summary>β波パワー（13-36 Hz）[dB]</summary>
        public double[] BetaBandDb { get; set; }
        
        /// <summary>γ波パワー（≥36 Hz）[dB]</summary>
        public double[] GammaBandDb { get; set; }
        
        #endregion

        #region コンストラクタ
        
        public BandPowerLineData()
        {
            AnalysisTime = DateTime.Now;
        }
        
        #endregion

        #region ユーティリティメソッド
        
        /// <summary>
        /// データが有効かチェック
        /// </summary>
        public bool IsValid()
        {
            return TimeAxis != null && TimeAxis.Length > 0 &&
                   DeltaBandDb != null && ThetaBandDb != null &&
                   AlphaBandDb != null && BetaBandDb != null &&
                   GammaBandDb != null &&
                   DeltaBandDb.Length == TimeAxis.Length &&
                   ThetaBandDb.Length == TimeAxis.Length &&
                   AlphaBandDb.Length == TimeAxis.Length &&
                   BetaBandDb.Length == TimeAxis.Length &&
                   GammaBandDb.Length == TimeAxis.Length;
        }
        
        /// <summary>
        /// 解析結果の要約を取得
        /// </summary>
        public string GetSummary()
        {
            if (!IsValid())
                return "データが無効です";
                
            var summary = $"=== バンドパワー線グラフデータ ===\n";
            summary += $"解析時刻: {AnalysisTime:yyyy/MM/dd HH:mm:ss}\n";
            summary += $"データ長: {SampleCount}サンプル\n";
            summary += $"時間範囲: {TimeAxis[0]:F3} - {TimeAxis[TimeAxis.Length - 1]:F3} 秒\n";
            
            // 各バンドの統計
            summary += $"δ波範囲: {GetRange(DeltaBandDb)}\n";
            summary += $"θ波範囲: {GetRange(ThetaBandDb)}\n";
            summary += $"α波範囲: {GetRange(AlphaBandDb)}\n";
            summary += $"β波範囲: {GetRange(BetaBandDb)}\n";
            summary += $"γ波範囲: {GetRange(GammaBandDb)}\n";
            
            return summary;
        }
        
        private string GetRange(double[] data)
        {
            if (data == null || data.Length == 0)
                return "N/A";
                
            double min = double.MaxValue;
            double max = double.MinValue;
            
            foreach (double value in data)
            {
                if (!double.IsNaN(value) && !double.IsInfinity(value))
                {
                    min = Math.Min(min, value);
                    max = Math.Max(max, value);
                }
            }
            
            return $"{min:F2} - {max:F2} dB";
        }
        
        #endregion
    }
}