using System;
using System.Collections.Generic;
using System.Linq;

namespace ホ号計画.Models
{
    /// <summary>
    /// 脳波バンドパワー正規化解析結果を格納するクラス
    /// Excelで示された正規化計算結果と完全に対応
    /// </summary>
    public class BandPowerAnalysisResult
    {
        #region 正規化バンドパワーデータ（各時刻の比率: 0-1, 合計=1.0）
        
        /// <summary>γ波正規化パワー（30-100 Hz）</summary>
        public double[] NormalizedGamma { get; set; }
        
        /// <summary>β波正規化パワー（13-30 Hz）</summary>
        public double[] NormalizedBeta { get; set; }
        
        /// <summary>α波正規化パワー（8-13 Hz）</summary>
        public double[] NormalizedAlpha { get; set; }
        
        /// <summary>θ波正規化パワー（4-8 Hz）</summary>
        public double[] NormalizedTheta { get; set; }
        
        /// <summary>δ波正規化パワー（0.5-4 Hz）</summary>
        public double[] NormalizedDelta { get; set; }
        
        #endregion

        #region パーセント表示用（主要バンドのみ）
        
        /// <summary>γ波パーセント表示（正規化値 × 100）</summary>
        public double[] GammaPercent { get; set; }
        
        /// <summary>β波パーセント表示（正規化値 × 100）</summary>
        public double[] BetaPercent { get; set; }
        
        #endregion

        #region 時間軸・メタデータ
        
        /// <summary>時間軸（秒）</summary>
        public double[] TimeAxis { get; set; }
        
        /// <summary>解析開始時刻</summary>
        public DateTime AnalysisTime { get; set; }
        
        /// <summary>データ長（サンプル数）</summary>
        public int SampleCount => TimeAxis?.Length ?? 0;
        
        #endregion

        #region 時間区間定義
        
        /// <summary>安静時区間</summary>
        public TimeInterval RestInterval { get; set; }
        
        /// <summary>タスク区間リスト（2分間隔等）</summary>
        public List<TimeInterval> TaskIntervals { get; set; }
        
        #endregion

        #region 統計計算結果
        
        /// <summary>安静時ベースライン統計</summary>
        public BandStatistics RestBaseline { get; set; }
        
        /// <summary>各タスク区間の統計</summary>
        public List<BandStatistics> TaskAverages { get; set; }
        
        /// <summary>変化量（Δ値）リスト</summary>
        public List<BandChanges> DeltaValues { get; set; }
        
        #endregion

        #region コンストラクタ
        
        public BandPowerAnalysisResult()
        {
            AnalysisTime = DateTime.Now;
            TaskIntervals = new List<TimeInterval>();
            TaskAverages = new List<BandStatistics>();
            DeltaValues = new List<BandChanges>();
        }

        public BandPowerAnalysisResult(int dataLength) : this()
        {
            InitializeArrays(dataLength);
        }
        
        #endregion

        #region 初期化・検証メソッド
        
        /// <summary>
        /// 配列を指定長で初期化
        /// </summary>
        private void InitializeArrays(int length)
        {
            NormalizedGamma = new double[length];
            NormalizedBeta = new double[length];
            NormalizedAlpha = new double[length];
            NormalizedTheta = new double[length];
            NormalizedDelta = new double[length];
            
            GammaPercent = new double[length];
            BetaPercent = new double[length];
            
            TimeAxis = new double[length];
        }

        /// <summary>
        /// 正規化結果の検証（各時刻で合計=1.0になっているか）
        /// </summary>
        public bool ValidateNormalization(double tolerance = 1e-6)
        {
            if (NormalizedGamma == null || NormalizedBeta == null || 
                NormalizedAlpha == null || NormalizedTheta == null || 
                NormalizedDelta == null)
                return false;

            for (int i = 0; i < SampleCount; i++)
            {
                double sum = NormalizedGamma[i] + NormalizedBeta[i] + 
                           NormalizedAlpha[i] + NormalizedTheta[i] + 
                           NormalizedDelta[i];
                
                if (Math.Abs(sum - 1.0) > tolerance)
                    return false;
            }
            
            return true;
        }

        /// <summary>
        /// パーセント値と正規化値の整合性チェック
        /// </summary>
        public bool ValidatePercentValues(double tolerance = 1e-6)
        {
            if (GammaPercent == null || BetaPercent == null ||
                NormalizedGamma == null || NormalizedBeta == null)
                return false;

            for (int i = 0; i < SampleCount; i++)
            {
                if (Math.Abs(GammaPercent[i] - NormalizedGamma[i] * 100) > tolerance ||
                    Math.Abs(BetaPercent[i] - NormalizedBeta[i] * 100) > tolerance)
                    return false;
            }
            
            return true;
        }

        #endregion

        #region 統計計算サポートメソッド
        
        /// <summary>
        /// 指定時間区間の統計を計算
        /// </summary>
        public BandStatistics CalculateIntervalStatistics(TimeInterval interval)
        {
            if (interval == null || TimeAxis == null)
                throw new ArgumentException("区間または時間軸が無効です");

            var indices = GetTimeIndicesForInterval(interval);
            if (indices.Count == 0)
                throw new ArgumentException($"区間 '{interval.Label}' に該当する時刻データが見つかりません");

            return new BandStatistics
            {
                IntervalLabel = interval.Label,
                GammaPercent = indices.Average(i => GammaPercent[i]),
                BetaPercent = indices.Average(i => BetaPercent[i]),
                AlphaPercent = indices.Average(i => NormalizedAlpha[i] * 100),
                ThetaPercent = indices.Average(i => NormalizedTheta[i] * 100),
                DeltaPercent = indices.Average(i => NormalizedDelta[i] * 100),
                StartTime = interval.StartTime,
                EndTime = interval.EndTime,
                SampleCount = indices.Count
            };
        }

        /// <summary>
        /// 時間区間に含まれるインデックスを取得
        /// </summary>
        public List<int> GetTimeIndicesForInterval(TimeInterval interval)
        {
            var indices = new List<int>();
            
            for (int i = 0; i < TimeAxis.Length; i++)
            {
                if (TimeAxis[i] >= interval.StartTime && TimeAxis[i] <= interval.EndTime)
                {
                    indices.Add(i);
                }
            }
            
            return indices;
        }

        /// <summary>
        /// 全統計値を再計算（区間設定変更時などに使用）
        /// </summary>
        public void RecalculateStatistics()
        {
            // 安静時ベースライン計算
            if (RestInterval != null)
            {
                RestBaseline = CalculateIntervalStatistics(RestInterval);
            }

            // タスク区間統計計算
            TaskAverages.Clear();
            DeltaValues.Clear();

            foreach (var taskInterval in TaskIntervals)
            {
                var taskStats = CalculateIntervalStatistics(taskInterval);
                TaskAverages.Add(taskStats);

                // 変化量計算（Δ値）
                if (RestBaseline != null)
                {
                    var deltaChanges = new BandChanges
                    {
                        TaskLabel = taskInterval.Label,
                        DeltaGamma = taskStats.GammaPercent - RestBaseline.GammaPercent,
                        DeltaBeta = taskStats.BetaPercent - RestBaseline.BetaPercent,
                        DeltaAlpha = taskStats.AlphaPercent - RestBaseline.AlphaPercent,
                        DeltaTheta = taskStats.ThetaPercent - RestBaseline.ThetaPercent,
                        DeltaDelta = taskStats.DeltaPercent - RestBaseline.DeltaPercent
                    };
                    DeltaValues.Add(deltaChanges);
                }
            }
        }

        #endregion

        #region デバッグ・ログ用メソッド
        
        /// <summary>
        /// 解析結果の要約を取得
        /// </summary>
        public string GetSummary()
        {
            var summary = $"=== 脳波バンドパワー正規化解析結果 ===\n";
            summary += $"解析時刻: {AnalysisTime:yyyy/MM/dd HH:mm:ss}\n";
            summary += $"データ長: {SampleCount}サンプル\n";
            summary += $"時間範囲: {TimeAxis?.FirstOrDefault():F3} - {TimeAxis?.LastOrDefault():F3} 秒\n";
            summary += $"正規化検証: {(ValidateNormalization() ? "OK" : "NG")}\n";
            summary += $"パーセント値検証: {(ValidatePercentValues() ? "OK" : "NG")}\n\n";

            if (RestBaseline != null)
            {
                summary += $"安静時ベースライン:\n";
                summary += $"  γ波: {RestBaseline.GammaPercent:F4}%\n";
                summary += $"  β波: {RestBaseline.BetaPercent:F4}%\n\n";
            }

            if (DeltaValues.Count > 0)
            {
                summary += $"変化量（Δ値）:\n";
                foreach (var delta in DeltaValues)
                {
                    summary += $"  {delta.TaskLabel}: Δγ={delta.DeltaGamma:F4}, Δβ={delta.DeltaBeta:F4}\n";
                }
            }

            return summary;
        }

        #endregion
    }
}