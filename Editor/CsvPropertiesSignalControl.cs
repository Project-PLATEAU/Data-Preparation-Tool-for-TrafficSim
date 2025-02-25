using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TrafficSimulationTool.Runtime;

namespace TrafficSimulationTool.Editor
{
    /// <summary>
    /// 信号制御のプロパティを保持するクラス
    /// </summary>
    [System.Serializable]
    public class CsvPropertiesSignalControl
    {
        [CsvHeader("時刻")]
        public string Time;

        [CsvHeader("情報源コード")]
        public string SourceCode;

        [CsvHeader("交差点番号")]
        public string IntersectionNumber;

        [CsvHeader("サイクル長")]
        public string CycleLength;

        [CsvHeader("スプリット＃１")]
        public string Split1;

        [CsvHeader("スプリット＃２")]
        public string Split2;

        [CsvHeader("スプリット＃３")]
        public string Split3;

        [CsvHeader("スプリット＃４")]
        public string Split4;

        [CsvHeader("スプリット＃５")]
        public string Split5;

        [CsvHeader("スプリット＃６")]
        public string Split6;

        [CsvHeader("リンクバージョン")]
        public string LinkVersion;
    }
}