using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TrafficSimulationTool.Runtime.Util
{
    public class DebugLogger
    {
        public static readonly int LOG_LEVEL = 100;

        public static void Log(int level, string str, string color= "#a9a9a9")
        {
            if(level <= LOG_LEVEL)
            {
                Debug.Log($"<color={color}>[Log{level}] {str}</color>");
            }    
        }
    }

    public class DebugStopwatch
    {
        private DateTime start;

        public DebugStopwatch()
        {
            start = DateTime.Now;
        }

        public double GetTimeSeconds()
        {
           return (DateTime.Now - start).TotalSeconds;
        }
    }
}
