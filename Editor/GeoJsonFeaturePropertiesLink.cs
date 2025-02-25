using System.Collections;
using System.Collections.Generic;
using TrafficSimulationTool.Editor;
using UnityEngine;

namespace TrafficSimulationTool.Editor
{
    /// <summary>
    /// RoadNetworkのリンクのプロパティを保持するクラス
    /// </summary>
    public class GeoJsonFeaturePropertiesLink : GeoJsonFeatureProperties
    {
        public string ID { get; set; }
        public string UPNODE { get; set; }
        public string DOWNNODE { get; set; }
        public double LENGTH { get; set; }
        public int LANENUM { get; set; }
        public int RLANENUM { get; set; }
        public int RLANELENGTH { get; set; }
        public int LLANENUM { get; set; }
        public int LLANELENGTH { get; set; }
        public string PROHIBIT { get; set; }
        public int TURNCONFIG { get; set; }
        public int TYPECONFIG { get; set; }
    }
}