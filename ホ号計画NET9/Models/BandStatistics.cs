using System;

namespace ホ号計画.Models
{
    /// <summary>
    /// 脳波バンド統計値を格納するクラス
    /// Excelでの平均値計算結果と対応
    /// </summary>
    public class BandStatistics
    {
        #region 基本情報
        
        /// <summary>区間ラベル（"安静時"、"タスク1"等）</summary>
        public string IntervalLabel { get; set; }
        
        /// <summary>区間開始時刻（秒）</summary>
        public double StartTime { get; set; }
        
        /// <summary>区間終了時刻（秒）</summary>
        public double EndTime { get; set; }
        
        /// <summary>区間継続時間（秒）</summary>
        public double Duration => EndTime - StartTime;
        
        /// <summary>統計計算に使用されたサンプル数</summary>
        public int SampleCount { get; set; }
        
        #endregion

        #region バンド別平均値（パーセント表示）
        
        /// <summary>γ波平均パーセント（30-100 Hz）</summary>
        public double GammaPercent { get; set; }
        
        /// <summary>β波平均パーセント（13-30 Hz）</summary>
        public double BetaPercent { get; set; }
        
        /// <summary>α波平均パーセント（8-13 Hz）</summary>
        public double AlphaPercent { get; set; }
        
        /// <summary>θ波平均パーセント（4-8 Hz）</summary>
        public double ThetaPercent { get; set; }
        
        /// <summary>δ波平均パーセント（0.5-4 Hz）</summary>
        public double DeltaPercent { get; set; }
        
        #endregion

        #region 正規化値（0-1の比率）
        
        /// <summary>γ波正規化平均（パーセント値 ÷ 100）</summary>
        public double GammaNormalized => GammaPercent / 100.0;
        
        /// <summary>β波正規化平均</summary>
        public double BetaNormalized => BetaPercent / 100.0;
        
        /// <summary>α波正規化平均</summary>
        public double AlphaNormalized => AlphaPercent / 100.0;
        
        /// <summary>θ波正規化平均</summary>
        public double ThetaNormalized => ThetaPercent / 100.0;
        
        /// <summary>δ波正規化平均</summary>
        public double DeltaNormalized => DeltaPercent / 100.0;
        
        #endregion

        #region 計算時刻
        
        /// <summary>統計計算実行時刻</summary>
        public DateTime CalculationTime { get; set; }
        
        #endregion

        #region コンストラクタ
        
        public BandStatistics()
        {
            CalculationTime = DateTime.Now;
        }
        
        public BandStatistics(string intervalLabel) : this()
        {
            IntervalLabel = intervalLabel;
        }
        
        #endregion

        #region 検証メソッド
        
        /// <summary>
        /// 正規化値の合計が1.0になっているかチェック
        /// </summary>
        /// <param name="tolerance">許容誤差</param>
        /// <returns>合計=1.0の場合true</returns>
        public bool ValidateNormalization(double tolerance = 1e-6)
        {
            double sum = GammaNormalized + BetaNormalized + AlphaNormalized + 
                        ThetaNormalized + DeltaNormalized;
            return Math.Abs(sum - 1.0) <= tolerance;
        }
        
        /// <summary>
        /// 統計値の妥当性チェック
        /// </summary>
        public bool IsValid()
        {
            // 負の値チェック
            if (GammaPercent < 0 || BetaPercent < 0 || AlphaPercent < 0 || 
                ThetaPercent < 0 || DeltaPercent < 0)
                return false;
            
            // パーセント値の上限チェック（100%を大幅に超えることはない）
            if (GammaPercent > 100 || BetaPercent > 100 || AlphaPercent > 100 || 
                ThetaPercent > 100 || DeltaPercent > 100)
                return false;
            
            // サンプル数チェック
            if (SampleCount <= 0)
                return false;
            
            // 時間範囲チェック
            if (StartTime < 0 || EndTime <= StartTime)
                return false;
            
            return true;
        }
        
        #endregion

        #region 表示用メソッド
        
        /// <summary>
        /// 統計値の要約文字列を取得
        /// </summary>
        public string GetSummary()
        {
            return $"{IntervalLabel}: γ={GammaPercent:F4}%, β={BetaPercent:F4}%, " +
                   $"α={AlphaPercent:F4}%, θ={ThetaPercent:F4}%, δ={DeltaPercent:F4}% " +
                   $"({StartTime:F1}-{EndTime:F1}s, n={SampleCount})";
        }
        
        /// <summary>
        /// Excelフォーマットでの文字列表現
        /// </summary>
        public string ToExcelFormat()
        {
            return $"{IntervalLabel},{GammaPercent:F6},{BetaPercent:F6}," +
                   $"{AlphaPercent:F6},{ThetaPercent:F6},{DeltaPercent:F6}";
        }
        
        #endregion

        #region 演算メソッド
        
        /// <summary>
        /// 他の統計値との差分を計算（主にベースラインとの比較用）
        /// </summary>
        public BandChanges CalculateDifference(BandStatistics baseline, string taskLabel = null)
        {
            if (baseline == null)
                throw new ArgumentNullException(nameof(baseline));
            
            return new BandChanges
            {
                TaskLabel = taskLabel ?? IntervalLabel,
                DeltaGamma = GammaPercent - baseline.GammaPercent,
                DeltaBeta = BetaPercent - baseline.BetaPercent,
                DeltaAlpha = AlphaPercent - baseline.AlphaPercent,
                DeltaTheta = ThetaPercent - baseline.ThetaPercent,
                DeltaDelta = DeltaPercent - baseline.DeltaPercent,
                BaselineLabel = baseline.IntervalLabel
            };
        }
        
        #endregion
    }

    /// <summary>
    /// 脳波バンド変化量（Δ値）を格納するクラス
    /// </summary>
    public class BandChanges
    {
        #region 基本情報
        
        /// <summary>タスクラベル</summary>
        public string TaskLabel { get; set; }
        
        /// <summary>ベースライン（比較基準）のラベル</summary>
        public string BaselineLabel { get; set; }
        
        #endregion

        #region 変化量（パーセントポイント差）
        
        /// <summary>γ波変化量（%ポイント）</summary>
        public double DeltaGamma { get; set; }
        
        /// <summary>β波変化量（%ポイント）</summary>
        public double DeltaBeta { get; set; }
        
        /// <summary>α波変化量（%ポイント）</summary>
        public double DeltaAlpha { get; set; }
        
        /// <summary>θ波変化量（%ポイント）</summary>
        public double DeltaTheta { get; set; }
        
        /// <summary>δ波変化量（%ポイント）</summary>
        public double DeltaDelta { get; set; }
        
        #endregion

        #region 計算時刻
        
        /// <summary>変化量計算時刻</summary>
        public DateTime CalculationTime { get; set; }
        
        #endregion

        #region コンストラクタ
        
        public BandChanges()
        {
            CalculationTime = DateTime.Now;
        }
        
        #endregion

        #region 表示用メソッド
        
        /// <summary>
        /// 変化量の要約文字列を取得
        /// </summary>
        public string GetSummary()
        {
            return $"{TaskLabel} vs {BaselineLabel}: " +
                   $"Δγ={DeltaGamma:F4}, Δβ={DeltaBeta:F4}, " +
                   $"Δα={DeltaAlpha:F4}, Δθ={DeltaTheta:F4}, Δδ={DeltaDelta:F4}";
        }
        
        /// <summary>
        /// Excelフォーマットでの文字列表現
        /// </summary>
        public string ToExcelFormat()
        {
            return $"{TaskLabel},{DeltaGamma:F6},{DeltaBeta:F6}," +
                   $"{DeltaAlpha:F6},{DeltaTheta:F6},{DeltaDelta:F6}";
        }
        
        /// <summary>
        /// 主要バンド（γ・β）のみの要約
        /// </summary>
        public string GetMainBandsSummary()
        {
            return $"{TaskLabel}: Δγ={DeltaGamma:F4}, Δβ={DeltaBeta:F4}";
        }
        
        #endregion

        #region 解析用メソッド
        
        /// <summary>
        /// 有意な変化があるかどうかを判定
        /// </summary>
        /// <param name="threshold">変化量の閾値（%ポイント）</param>
        /// <returns>閾値を超える変化がある場合true</returns>
        public bool HasSignificantChange(double threshold = 1.0)
        {
            return Math.Abs(DeltaGamma) > threshold || 
                   Math.Abs(DeltaBeta) > threshold ||
                   Math.Abs(DeltaAlpha) > threshold ||
                   Math.Abs(DeltaTheta) > threshold ||
                   Math.Abs(DeltaDelta) > threshold;
        }
        
        /// <summary>
        /// 最大変化量を取得
        /// </summary>
        public double GetMaxAbsoluteChange()
        {
            return Math.Max(Math.Max(Math.Max(Math.Max(
                Math.Abs(DeltaGamma), Math.Abs(DeltaBeta)), 
                Math.Abs(DeltaAlpha)), Math.Abs(DeltaTheta)), 
                Math.Abs(DeltaDelta));
        }
        
        #endregion
    }

    /// <summary>
    /// 時間区間情報を格納するクラス
    /// </summary>
    public class TimeInterval
    {
        #region 基本プロパティ
        
        /// <summary>区間開始時刻（秒）</summary>
        public double StartTime { get; set; }
        
        /// <summary>区間終了時刻（秒）</summary>
        public double EndTime { get; set; }
        
        /// <summary>区間ラベル</summary>
        public string Label { get; set; }
        
        /// <summary>区間タイプ</summary>
        public IntervalType IntervalType { get; set; }
        
        #endregion

        #region 計算プロパティ
        
        /// <summary>区間継続時間（秒）</summary>
        public double Duration => EndTime - StartTime;
        
        /// <summary>区間中央時刻（秒）</summary>
        public double CenterTime => (StartTime + EndTime) / 2.0;
        
        #endregion

        #region コンストラクタ
        
        public TimeInterval()
        {
            IntervalType = IntervalType.Other;
        }
        
        public TimeInterval(double startTime, double endTime, string label, IntervalType type = IntervalType.Other)
        {
            StartTime = startTime;
            EndTime = endTime;
            Label = label;
            IntervalType = type;
        }
        
        #endregion

        #region 検証・判定メソッド
        
        /// <summary>
        /// 指定時刻がこの区間に含まれるかチェック
        /// </summary>
        /// <param name="time">チェックする時刻</param>
        /// <returns>区間に含まれる場合true</returns>
        public bool Contains(double time)
        {
            return time >= StartTime && time <= EndTime;
        }
        
        /// <summary>
        /// 他の区間と重複するかチェック
        /// </summary>
        /// <param name="other">比較対象の区間</param>
        /// <returns>重複する場合true</returns>
        public bool OverlapsWith(TimeInterval other)
        {
            if (other == null) return false;
            return !(EndTime <= other.StartTime || StartTime >= other.EndTime);
        }
        
        /// <summary>
        /// 区間の妥当性チェック
        /// </summary>
        public bool IsValid()
        {
            return StartTime >= 0 && EndTime > StartTime && !string.IsNullOrEmpty(Label);
        }
        
        #endregion

        #region 表示用メソッド
        
        /// <summary>
        /// 区間情報の文字列表現
        /// </summary>
        public override string ToString()
        {
            return $"{Label}: {StartTime:F1} - {EndTime:F1}s ({Duration:F1}s)";
        }
        
        /// <summary>
        /// 詳細情報を含む文字列表現
        /// </summary>
        public string GetDetailedString()
        {
            return $"{Label} ({IntervalType}): {StartTime:F3} - {EndTime:F3}s " +
                   $"(継続時間: {Duration:F3}s, 中央: {CenterTime:F3}s)";
        }
        
        #endregion
    }
}