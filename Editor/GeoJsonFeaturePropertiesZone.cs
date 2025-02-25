using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficSimulationTool.Editor
{
    /// <summary>
    ///RoadNetworkのゾーンのプロパティを保持するクラス
    /// </summary>
    public class GeoJsonFeaturePropertiesZone : GeoJsonFeatureProperties
    {
        public string ID { get; set; }
        public List<string> NODEID { get; set; }
        public string SIDETYPE { get; set; }
    }
}