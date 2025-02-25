using System.Collections;
using System.Collections.Generic;
using TrafficSimulationTool.Editor;
using UnityEngine;

namespace TrafficSimulationTool.Editor
{
    /// <summary>
    /// RoadNetworkのノードのプロパティを保持するクラス
    /// </summary>
    public class GeoJsonFeaturePropertiesNode : GeoJsonFeatureProperties
    {
        public string ID { get; set; }
    }
}