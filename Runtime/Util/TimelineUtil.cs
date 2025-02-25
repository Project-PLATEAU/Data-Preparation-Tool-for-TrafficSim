using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficSimulationTool.Runtime.Util
{
    public class TimelineUtil
    {
        public static double GetTimeSeconds(string timeString)
        {
            DateTime parsedDateTime = GetDateTime(timeString);
            return GetTimeSeconds(parsedDateTime);
        }

        public static double GetTimeSeconds(DateTime time)
        {
            return (time - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static DateTime GetDateTime(double seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            return new DateTime(1970, 1, 1) + span;
        }

        public static DateTime GetDateTime(string timeString)
        {
            try
            {
                return DateTime.ParseExact(timeString, "yyyyMMddHHmmss", null);
                //return DateTime.ParseExact(timeString, "HH:mm:ss", CultureInfo.InvariantCulture);
                //return DateTime.ParseExact(timeString, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
            catch(Exception)
            {
                Debug.LogError($" Parse Error {timeString}");
            }

            return DateTime.Now;
        }

        public static TimeSpan GetTimeSpanByPercent(float percent , DateTime start, DateTime end)
        {
            var total = end - start;
            return total.Multiply(percent);
        }
    }
}
