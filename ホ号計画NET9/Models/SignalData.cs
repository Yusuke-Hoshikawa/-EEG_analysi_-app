using System;

namespace ホ号計画.Models
{
    public class SignalData
    {
        public double[] TimeData { get; set; }
        public double SamplingRate { get; set; }
        public DateTime StartTime { get; set; }
        public string DataType { get; set; }
        public string FileName { get; set; }

        public SignalData()
        {
            TimeData = new double[0];
            SamplingRate = 1000.0;
            StartTime = DateTime.Now;
            DataType = "EEG";
            FileName = "";
        }

        public double Duration => TimeData.Length / SamplingRate;
        public int SampleCount => TimeData.Length;
    }
}