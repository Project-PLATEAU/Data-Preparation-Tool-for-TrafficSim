using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TrafficSimulationTool.Runtime;

namespace TrafficSimulationTool.Editor
{
    // TODO: Runtimeでも実装しているので、共通化する
    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class CsvHeaderAttribute : System.Attribute
    {
        public string HeaderName { get; }

        public CsvHeaderAttribute(string headerName)
        {
            HeaderName = headerName;
        }
    }

    /// <summary>
    /// 交通量のプロパティを保持するクラス
    /// </summary>
    public class CsvPropertiesTraffic
    {
        [CsvHeader("ゾーンID")]
        public string ZoneID;

        [CsvHeader("集計開始時刻")]
        public string StartTIme;

        [CsvHeader("集計終了時刻")]
        public string EndTime;

        [CsvHeader("発生台数")]
        public string DepartureCount;

        [CsvHeader("集中台数")]
        public string DestinationCount;
    }
}