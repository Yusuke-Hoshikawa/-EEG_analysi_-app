using System;
using ホ号計画.Models;

namespace ホ号計画.Utils
{
    public static class WindowFunctions
    {
        /// <summary>
        /// データに窓関数を適用する（既存メソッド）
        /// </summary>
        public static double[] ApplyWindow(double[] data, WindowFunction windowType)
        {
            int N = data.Length;
            double[] windowed = new double[N];
            
            for (int i = 0; i < N; i++)
            {
                double windowValue = GetWindowValue(i, N, windowType);
                windowed[i] = data[i] * windowValue;
            }
            
            return windowed;
        }

        /// <summary>
        /// 窓関数係数配列を生成する（スペクトログラム用）
        /// </summary>
        /// <param name="length">窓長</param>
        /// <param name="windowType">窓関数の種類</param>
        /// <returns>窓関数係数配列</returns>
        public static double[] CreateWindow(int length, WindowFunction windowType)
        {
            double[] window = new double[length];
            for (int i = 0; i < length; i++)
            {
                window[i] = GetWindowValue(i, length, windowType);
            }
            return window;
        }

        private static double GetWindowValue(int n, int N, WindowFunction windowType)
        {
            switch (windowType)
            {
                case WindowFunction.Rectangular:
                    return 1.0;

                case WindowFunction.Hanning:
                    return 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * n / (N - 1)));

                case WindowFunction.Hamming:
                    return 0.54 - 0.46 * Math.Cos(2.0 * Math.PI * n / (N - 1));

                case WindowFunction.Blackman:
                    return 0.42 - 0.5 * Math.Cos(2.0 * Math.PI * n / (N - 1)) + 0.08 * Math.Cos(4.0 * Math.PI * n / (N - 1));

                default:
                    return 1.0;
            }
        }
    }
}