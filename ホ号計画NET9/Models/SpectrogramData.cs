using System;
using System.Collections.Generic;

namespace ホ号計画.Models
{
    public class SpectrogramData
    {
        public double[,] PowerMatrix { get; set; }
        public double[] TimeAxis { get; set; }
        public double[] FrequencyAxis { get; set; }
        public double MinPower { get; set; }
        public double MaxPower { get; set; }
        
        public SpectrogramData(int timeSteps, int frequencyBins)
        {
            PowerMatrix = new double[timeSteps, frequencyBins];
            TimeAxis = new double[timeSteps];
            FrequencyAxis = new double[frequencyBins];
            MinPower = double.MaxValue;
            MaxPower = double.MinValue;
        }

        public int TimeSteps => PowerMatrix.GetLength(0);
        public int FrequencyBins => PowerMatrix.GetLength(1);

        public void UpdatePowerRange()
        {
            MinPower = double.MaxValue;
            MaxPower = double.MinValue;
            bool hasValidData = false;

            for (int t = 0; t < TimeSteps; t++)
            {
                for (int f = 0; f < FrequencyBins; f++)
                {
                    double power = PowerMatrix[t, f];
                    
                    // NaN・無限大・異常値をチェック
                    if (double.IsNaN(power) || double.IsInfinity(power))
                    {
                        PowerMatrix[t, f] = -100.0; // 安全なデフォルト値
                        power = -100.0;
                    }
                    else if (power < -300.0 || power > 200.0)
                    {
                        // 異常に大きい・小さい値を制限
                        power = Math.Max(-300.0, Math.Min(200.0, power));
                        PowerMatrix[t, f] = power;
                    }
                    
                    if (power < MinPower) MinPower = power;
                    if (power > MaxPower) MaxPower = power;
                    hasValidData = true;
                }
            }
            
            // 有効なデータがない場合のデフォルト値
            if (!hasValidData || MinPower == double.MaxValue || MaxPower == double.MinValue)
            {
                MinPower = -100.0;
                MaxPower = 0.0;
            }
            
            // 最小・最大値の安全性チェック
            if (double.IsNaN(MinPower) || double.IsInfinity(MinPower)) MinPower = -100.0;
            if (double.IsNaN(MaxPower) || double.IsInfinity(MaxPower)) MaxPower = 0.0;
            
            // フィルタ後の極小パワーデータ用の調整
            if (MaxPower - MinPower < 10.0) // パワー範囲が10dB未満の場合
            {
                // 最小値を-80dBに固定して表示範囲を確保
                MinPower = Math.Min(MinPower, -80.0);
                // 最大値と最小値の差を最低20dBに設定
                if (MaxPower - MinPower < 20.0)
                {
                    MaxPower = MinPower + 20.0;
                }
            }
            
            // 最終的な安全チェック
            MinPower = Math.Max(MinPower, -300.0);
            MaxPower = Math.Min(MaxPower, 200.0);
        }

        /// <summary>
        /// Pythonアルゴリズムと同じIQRベースの外れ値除外自動スケーリング
        /// </summary>
        public void UpdatePowerRangeWithIQR()
        {
            // 全てのパワー値を収集
            var allPowers = new List<double>();
            
            for (int t = 0; t < TimeSteps; t++)
            {
                for (int f = 0; f < FrequencyBins; f++)
                {
                    double power = PowerMatrix[t, f];
                    
                    // NaN・無限大をフィルタ
                    if (!double.IsNaN(power) && !double.IsInfinity(power) && 
                        power >= -300.0 && power <= 200.0)
                    {
                        allPowers.Add(power);
                    }
                }
            }
            
            if (allPowers.Count == 0)
            {
                // デフォルト値
                MinPower = -100.0;
                MaxPower = 0.0;
                return;
            }
            
            // ソートして四分位数を計算
            allPowers.Sort();
            int count = allPowers.Count;
            
            // 25%パーセンタイル (Q1)
            double q25 = GetPercentile(allPowers, 25.0);
            
            // 75%パーセンタイル (Q3)  
            double q75 = GetPercentile(allPowers, 75.0);
            
            // IQR計算
            double iqr = q75 - q25;
            
            // 外れ値除外を無効化し、全データ範囲を使用
            MinPower = allPowers[0];  // 最小値
            MaxPower = allPowers[count - 1];  // 最大値
            
            // コメントアウト: 外れ値除外処理を無効化
            // MinPower = q25 - 1.5 * iqr;
            // MaxPower = q75 + 1.5 * iqr;
            // MaxPower = Math.Min(MaxPower, 5.0);
            
            // 安全性チェック
            if (double.IsNaN(MinPower) || double.IsInfinity(MinPower)) MinPower = -100.0;
            if (double.IsNaN(MaxPower) || double.IsInfinity(MaxPower)) MaxPower = 0.0;
            
            // 最小範囲の確保
            if (MaxPower - MinPower < 1.0)
            {
                MaxPower = MinPower + 10.0;
            }
        }
        
        /// <summary>
        /// パーセンタイル計算（線形補間）
        /// </summary>
        private double GetPercentile(List<double> sortedValues, double percentile)
        {
            if (sortedValues.Count == 0) return 0.0;
            if (sortedValues.Count == 1) return sortedValues[0];
            
            double index = (percentile / 100.0) * (sortedValues.Count - 1);
            int lowerIndex = (int)Math.Floor(index);
            int upperIndex = (int)Math.Ceiling(index);
            
            if (lowerIndex == upperIndex)
            {
                return sortedValues[lowerIndex];
            }
            
            // 線形補間
            double weight = index - lowerIndex;
            return sortedValues[lowerIndex] * (1.0 - weight) + sortedValues[upperIndex] * weight;
        }
    }
}
