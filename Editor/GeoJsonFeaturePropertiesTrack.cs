﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficSimulationTool.Editor
{
    /// <summary>
    ///RoadNetworkのゾーンのプロパティを保持するクラス
    /// </summary>
    public class GeoJsonFeaturePropertiesTrack : GeoJsonFeatureProperties
    {
        public string ID { get; set; }
        public int ORDER { get; set; }
        public string UPLINKID { get; set; }
        public int UPLANEPOS { get; set; }
        public double UPDISTANCE { get; set; }
        public string DOWNLINKID { get; set; }
        public int DOWNLANEPOS { get; set; }
        public double DOWNDISTANCE { get; set; }
        public double LENGTH { get; set; }
        public int TURNCONFIG { get; set; }
        public int TYPECONFIG { get; set; }
    }
}