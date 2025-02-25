using System.Collections;
using System.Collections.Generic;
using TrafficSimulationTool.Editor;
using UnityEngine;

namespace TrafficSimulationTool.Editor
{
    /// <summary>
    /// RoadNetworkのレーンのプロパティを保持するクラス
    /// </summary>
    public class GeoJsonFeaturePropertiesLane : GeoJsonFeatureProperties
    {
        public string ID { get; set; }
        public string LINKID { get; set; }
        public int LANEPOS { get; set; }
        public double LENGTH { get; set; }
        public double WIDTH { get; set; }
    }
}