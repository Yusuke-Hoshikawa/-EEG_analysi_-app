using System;
using System.Collections.Generic;
using System.Linq;

namespace ホ号計画
{
    /// <summary>
    /// HRV解析用のIHR（瞬間心拍数）補間処理クラス
    /// C++ Slide_FFT_ver8の補間アルゴリズムをC#で再実装
    /// ver.4コメント: "再サンプリングプログラムで10Hz相当に直線補完する"
    /// </summary>
    public static class IhrInterpolator
    {
    /// <summary>
    /// IHRデータポイント（時刻とIHR値のペア）
    /// </summary>
    public class IhrDataPoint
    {
        public double Time { get; }  // 時刻 [秒]
        public double Ihr { get; }   // IHR値 [Hz]

        public IhrDataPoint(double time, double ihr)
        {
            Time = time;
            Ihr = ihr;
        }
    }
    /// <summary>
    /// Rピーク時刻列から10Hz等間隔サンプリングのIHR波形を生成
    /// C++版と同様の補間アルゴリズムを実装
    /// </summary>
    /// <param name="rpeaksSec">Rピーク時刻リスト [秒]</param>
    /// <returns>10Hzグリッド (0.1秒ステップ) のIHR [Hz] 値</returns>
    public static double[] BuildIhr10Hz(List<double> rpeaksSec)
    {
        if (rpeaksSec == null || rpeaksSec.Count < 2)
        {
            throw new ArgumentException("Rピーク時刻は最低2個必要です");
        }

        // Rピーク時刻をソート（昇順）
        var sortedRpeaks = rpeaksSec.OrderBy(t => t).ToList();

        // RR間隔とIHR（瞬間心拍数）を計算
        var ihrDataPoints = new List<IhrDataPoint>();

        for (int i = 1; i < sortedRpeaks.Count; i++)
        {
            // RR間隔 [秒]
            double rr = sortedRpeaks[i] - sortedRpeaks[i - 1];

            // ゼロ除算防止のみ（異常値除去はC++版と同様に行わない）
            if (rr > 0)
            {
                // IHR = 1 / RR [Hz]  ※C++版コメント通り
                double ihr = 1.0 / rr;

                // 時間スタンプはRピーク時刻（後のRピーク時刻を使用）
                double timeStamp = sortedRpeaks[i];

                ihrDataPoints.Add(new IhrDataPoint(timeStamp, ihr));
            }
        }

        if (ihrDataPoints.Count < 2)
        {
            throw new ArgumentException("有効なIHRデータポイントが不足しています");
        }

        // 10Hzグリッド（0.1秒間隔）でのサンプリング範囲を決定
        double startTime = ihrDataPoints.First().Time;
        double endTime = ihrDataPoints.Last().Time;
        
        // C++版に合わせて0.1秒刻み（10Hz）で生成
        const double samplingInterval = 0.1;
        int numSamples = (int)Math.Floor((endTime - startTime) / samplingInterval) + 1;
        
        var ihrInterpolated = new double[numSamples];

        // 線形補間により10Hzグリッドでサンプリング
        for (int i = 0; i < numSamples; i++)
        {
            double targetTime = startTime + i * samplingInterval;
            ihrInterpolated[i] = LinearInterpolateIhr(ihrDataPoints, targetTime);
        }

        return ihrInterpolated;
    }

    /// <summary>
    /// IHRデータポイントリストから指定時刻の値を線形補間
    /// C++アルゴリズムの補間部分をC#で実装
    /// </summary>
    /// <param name="ihrDataPoints">IHRデータポイントリスト（時刻順でソート済み）</param>
    /// <param name="targetTime">補間したい時刻 [秒]</param>
    /// <returns>補間されたIHR値 [Hz]</returns>
    private static double LinearInterpolateIhr(List<IhrDataPoint> ihrDataPoints, double targetTime)
    {
        if (ihrDataPoints == null || ihrDataPoints.Count == 0)
            return 0.0;

        // 範囲外の場合は端点の値を返す
        if (targetTime <= ihrDataPoints[0].Time)
        {
            return ihrDataPoints[0].Ihr;
        }
        if (targetTime >= ihrDataPoints[ihrDataPoints.Count - 1].Time)
        {
            return ihrDataPoints[ihrDataPoints.Count - 1].Ihr;
        }

        // 補間区間を見つける（バイナリサーチ的アプローチ）
        for (int i = 0; i < ihrDataPoints.Count - 1; i++)
        {
            if (targetTime >= ihrDataPoints[i].Time && targetTime <= ihrDataPoints[i + 1].Time)
            {
                // 線形補間：y = y1 + (y2 - y1) * (x - x1) / (x2 - x1)
                double t1 = ihrDataPoints[i].Time;
                double t2 = ihrDataPoints[i + 1].Time;
                double ihr1 = ihrDataPoints[i].Ihr;
                double ihr2 = ihrDataPoints[i + 1].Ihr;

                // 補間係数
                double alpha = (targetTime - t1) / (t2 - t1);
                return ihr1 + alpha * (ihr2 - ihr1);
            }
        }

        // ここには到達しないはず
        return ihrDataPoints[ihrDataPoints.Count - 1].Ihr;
    }

    /// <summary>
    /// 線形補間関数（汎用版 - デバッグ用）
    /// </summary>
    /// <param name="timePoints">時間点のリスト</param>
    /// <param name="values">対応する値のリスト</param>
    /// <param name="targetTime">補間したい時間点</param>
    /// <returns>補間された値</returns>
    private static double LinearInterpolate(List<double> timePoints, List<double> values, double targetTime)
    {
        // 範囲外の場合は端点の値を返す
        if (targetTime <= timePoints[0])
        {
            return values[0];
        }
        if (targetTime >= timePoints[timePoints.Count - 1])
        {
            return values[values.Count - 1];
        }

        // 補間区間を見つける
        for (int i = 0; i < timePoints.Count - 1; i++)
        {
            if (targetTime >= timePoints[i] && targetTime <= timePoints[i + 1])
            {
                // 線形補間
                double t1 = timePoints[i];
                double t2 = timePoints[i + 1];
                double v1 = values[i];
                double v2 = values[i + 1];

                // 補間係数
                double alpha = (targetTime - t1) / (t2 - t1);
                return v1 + alpha * (v2 - v1);
            }
        }

        // ここには到達しないはず
        return values[values.Count - 1];
    }

    /// <summary>
    /// 時系列心拍数データ（時間, BPM）を10Hz IHRデータに変換
    /// BPM → IHR[Hz] 変換＋線形補間による10Hzリサンプリング
    /// </summary>
    /// <param name="timePoints">時間軸リスト [秒]</param>
    /// <param name="bpmValues">心拍数リスト [BPM]</param>
    /// <returns>10Hzグリッド (0.1秒ステップ) のIHR [Hz] 値</returns>
    public static double[] ResampleBpmTo10HzIhr(List<double> timePoints, List<double> bpmValues)
    {
        if (timePoints == null || bpmValues == null || timePoints.Count < 2 || bpmValues.Count < 2)
        {
            throw new ArgumentException("時系列心拍数データは最低2サンプル必要です");
        }

        if (timePoints.Count != bpmValues.Count)
        {
            throw new ArgumentException("時間軸とBPM値のサンプル数が一致しません");
        }

        // BPM → IHR[Hz] 変換: IHR = BPM / 60
        var ihrValues = bpmValues.Select(bpm => bpm / 60.0).ToList();

        // 時間範囲を取得
        double startTime = timePoints.First();
        double endTime = timePoints.Last();

        // 10Hzグリッド（0.1秒間隔）でリサンプリング
        const double samplingInterval = 0.1;
        int numSamples = (int)Math.Floor((endTime - startTime) / samplingInterval) + 1;

        if (numSamples < 1)
        {
            throw new ArgumentException("データの時間範囲が短すぎます");
        }

        var ihrResampled = new double[numSamples];

        // 線形補間により10Hzグリッドでサンプリング
        for (int i = 0; i < numSamples; i++)
        {
            double targetTime = startTime + i * samplingInterval;
            ihrResampled[i] = LinearInterpolate(timePoints, ihrValues, targetTime);
        }

        System.Diagnostics.Debug.WriteLine($"BPM→IHRリサンプリング完了: 入力{timePoints.Count}サンプル → 出力{numSamples}サンプル (10Hz)");
        System.Diagnostics.Debug.WriteLine($"  時間範囲: {startTime:F2}s - {endTime:F2}s ({endTime - startTime:F1}秒間)");
        System.Diagnostics.Debug.WriteLine($"  IHR範囲: {ihrResampled.Min():F3} - {ihrResampled.Max():F3} Hz ({ihrResampled.Min() * 60:F1} - {ihrResampled.Max() * 60:F1} BPM相当)");

        return ihrResampled;
    }

    /// <summary>
    /// デバッグ用：RR間隔とIHR値のペアを取得
    /// </summary>
    /// <param name="rpeaksSec">Rピーク時刻リスト [秒]</param>
    /// <returns>時間点, RR間隔, IHR値のタプル</returns>
    public static List<(double Time, double RR, double IHR)> GetRRAndIHR(List<double> rpeaksSec)
    {
        if (rpeaksSec == null || rpeaksSec.Count < 2)
        {
            return new List<(double, double, double)>();
        }

        var sortedRpeaks = rpeaksSec.OrderBy(t => t).ToList();
        var result = new List<(double, double, double)>();

        for (int i = 1; i < sortedRpeaks.Count; i++)
        {
            double rr = sortedRpeaks[i] - sortedRpeaks[i - 1];

            // ゼロ除算防止のみ（異常値除去はC++版と同様に行わない）
            if (rr > 0)
            {
                double ihr = 1.0 / rr;
                double timeStamp = (sortedRpeaks[i - 1] + sortedRpeaks[i]) / 2.0;
                result.Add((timeStamp, rr, ihr));
            }
        }

        return result;
    }
    }
}