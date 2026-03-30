using System;

namespace ホ号計画.Models
{
    public class FilterAnalysisResult
    {
        public double[] OriginalData { get; set; }
        public double[] LFFilteredData { get; set; }
        public double[] HFFilteredData { get; set; }
        public double[] LFPowerSpectrum { get; set; }
        public double[] HFPowerSpectrum { get; set; }
        public double LFPowerRatio { get; set; }
        public double HFPowerRatio { get; set; }
        public double HFLFRatio { get; set; }
        public FilterType AppliedFilter { get; set; }
        public DateTime AnalysisTime { get; set; }
        public string FilterDescription { get; set; }

        public FilterAnalysisResult()
        {
            AnalysisTime = DateTime.Now;
            AppliedFilter = FilterType.None;
        }

        public void CalculatePowerRatios()
        {
            if (LFPowerSpectrum != null && LFPowerSpectrum.Length > 0)
            {
                double lfSum = 0;
                foreach (var power in LFPowerSpectrum)
                {
                    lfSum += Math.Pow(10, power / 10.0); // dBからリニアに変換
                }
                LFPowerRatio = lfSum / LFPowerSpectrum.Length;
            }

            if (HFPowerSpectrum != null && HFPowerSpectrum.Length > 0)
            {
                double hfSum = 0;
                foreach (var power in HFPowerSpectrum)
                {
                    hfSum += Math.Pow(10, power / 10.0); // dBからリニアに変換
                }
                HFPowerRatio = hfSum / HFPowerSpectrum.Length;
            }

            if (LFPowerRatio > 0 && HFPowerRatio > 0)
            {
                HFLFRatio = HFPowerRatio / LFPowerRatio;
            }
        }
    }
}