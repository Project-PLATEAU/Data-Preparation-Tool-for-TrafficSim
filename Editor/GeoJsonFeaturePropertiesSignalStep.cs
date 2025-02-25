using System.Collections;
using System.Collections.Generic;
using TrafficSimulationTool.Editor;
using UnityEngine;

namespace TrafficSimulationTool.Editor
{
    /// <summary>
    /// RoadNetworkの信号現示階梯のプロパティを保持するクラス
    /// </summary>
    public class GeoJsonFeaturePropertiesSignalStep : GeoJsonFeatureProperties
    {
        public string ID { get; set; }

        public string PATTERNID { get; set; }

        public int ORDER { get; set; }

        public int DURATION { get; set; }

        public string SIGLIGHT { get; set; }

        public int TYPEMASK { get; set; }

        public string GREEN { get; set; }

        public string YELLOW { get; set; }

        public string RED { get; set; }
    }
}