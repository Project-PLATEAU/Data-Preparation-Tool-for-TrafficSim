using System;
using TrafficSimulationTool.Runtime.Util;

namespace TrafficSimulationTool.Runtime.SimData
{
    [System.Serializable]
    public class RoadIndicator
    {
        [CsvHeader("集計開始時刻")]
        public string StartTime;

        [CsvHeader("集計終了時刻")]
        public string EndTime;

        [CsvHeader("リンクID")]
        public string LinkID;

        [CsvHeader("交通量")]
        public float TrafficVolume;

        [CsvHeader("平均速度")]
        public float TrafficSpeed;

        public uint Index { get; set; }

        private Nullable<double> _startTimeSeconds;

        public double StartTimeSeconds
        {
            get
            {
                if (_startTimeSeconds == null)
                    _startTimeSeconds = TimelineUtil.GetTimeSeconds(StartTime + "00"); // yyyyMMddHHmm + ss
                return _startTimeSeconds ?? 0;
            }
        }

        private Nullable<double> _endTimeSeconds;

        public double EndTimeSeconds
        {
            get
            {
                if (_endTimeSeconds == null)
                    _endTimeSeconds = TimelineUtil.GetTimeSeconds(EndTime + "00"); // yyyyMMddHHmm + ss
                return _endTimeSeconds ?? 0;
            }
        }
    }
}