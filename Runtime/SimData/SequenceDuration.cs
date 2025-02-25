using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using TrafficSimulationTool.Runtime.Util;
using System.Runtime.Serialization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace TrafficSimulationTool.Runtime.SimData
{
    /// <summary>
    /// 時間情報保持データ
    /// </summary>
    public class SequenceDuration
    {
        public uint TotalFrames { get; private set; }
        public uint FramesPerSecond { get; private set; }

        public DateTime StartTime { get; private set; } = DateTime.MinValue;
        public DateTime EndTime { get; private set; } = DateTime.MinValue;

        public double TotalDuration { get => (EndTime - StartTime).TotalSeconds; }

        public double StartTimeSeconds { get => TimelineUtil.GetTimeSeconds(StartTime); }
        public double EndTimeSeconds { get => TimelineUtil.GetTimeSeconds(EndTime); }

        public SequenceDuration(uint fps)
        {
            FramesPerSecond = fps;
        }

        public void SetStartEnd(IDataSet data)
        {
            var start = data.GetStartTime();
            var end = data.GetEndTime();

            //Debug.Log($"SetStartEnd : {start.ToString()} -> {end.ToString()}");

            if (StartTime == DateTime.MinValue)
                StartTime = start;
            else
            {
                CheckTimeDiff(StartTime, start);
                StartTime = DateTime.Compare(start, StartTime) < 0 ? start : StartTime;
            }               

            if (EndTime == DateTime.MinValue)
                EndTime = end;
            else
            {
                CheckTimeDiff(EndTime, end);
                EndTime = DateTime.Compare(end, EndTime) < 0 ? EndTime : end;
            } 
        }

        public void CalculateTotalFrames()
        {
            TotalFrames = (uint)(TotalDuration * FramesPerSecond);

            DebugLogger.Log(10, $"start:{StartTime.ToString()} end:{EndTime.ToString()}");
            DebugLogger.Log(10, $"totalDuration : {TotalDuration} totalFrames : {TotalFrames}");
        }

        //時間差が大きい場合はエラー(30分以上）
        private void CheckTimeDiff(DateTime a, DateTime b)
        {
            var span = a - b;
            if(Math.Abs(span.TotalMinutes) > 30) 
            {
                throw new TimeNotMatchException($"Start/End time diff too long  {span.TotalMinutes} minutes.");
            }
        }
    }

    [Serializable()]
    public class TimeNotMatchException : Exception
    {
        public TimeNotMatchException() : base() { }
        public TimeNotMatchException(string message) : base(message) { }
        public TimeNotMatchException(string message, Exception innerException) : base(message, innerException) { }
        protected TimeNotMatchException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
