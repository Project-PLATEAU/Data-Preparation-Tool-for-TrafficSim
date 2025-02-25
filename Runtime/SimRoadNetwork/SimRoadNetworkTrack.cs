using GeoJSON.Net.Geometry;
using PLATEAU.CityInfo;
using PLATEAU.Geometries;
using PLATEAU.Native;
using PLATEAU.RoadNetwork;
using PLATEAU.RoadNetwork.Data;
using PLATEAU.RoadNetwork.Structure;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

namespace TrafficSimulationTool.Runtime
{
    /// <summary>
    /// シミュレーション用道路ネットワークのトラックを表すクラス
    /// </summary>
    [System.Serializable]
    public class SimRoadNetworkTrack
    {
        public static readonly string IDPrefix = "Track";

        [field: SerializeField]
        public string ID { get; private set; }

        private SimRoadNetworkManager simRoadNetworkManager;

        public SimRoadNetworkManager SimRoadNetworkManager
        {
            get
            {
                if (simRoadNetworkManager == null)
                {
                    simRoadNetworkManager = GameObject.FindObjectOfType<SimRoadNetworkManager>();
                }

                return simRoadNetworkManager;
            }
        }

        [field: SerializeField]
        public RnDataTrack OriginTrack { get; private set; } = null;

        private const int TrackResolution = 10; // Trackの解像度

        [field: SerializeField]
        public int Order { get; private set; } = 0;

        [field: SerializeField]
        public float UpDistance = 0.0f;

        [field: SerializeField]
        public float DownDistance = 0.0f;

        [field: SerializeField]
        public SimRoadNetworkLink UpLink = null;

        [field: SerializeField]
        public SimRoadNetworkLink DownLink = null;

        [field: SerializeField]
        public int UpLane = 0;

        [field: SerializeField]
        public int DownLane = 0;

        public SimRoadNetworkTrack(string id, RnDataTrack track, int order)
        {
            ID = IDPrefix + id;

            OriginTrack = track;

            Order = order;

            var allLanes = SimRoadNetworkManager.RoadNetworkDataGetter.GetLanes();

            var allWays = SimRoadNetworkManager.RoadNetworkDataGetter.GetWays();

            foreach (var link in SimRoadNetworkManager.SimRoadNetworkLinks)
            {
                var lanes = link.GetOriginLanes();

                for (int i = 0; i < lanes.Count; i++)
                {
                    var lane = lanes[i];

                    if (allWays[OriginTrack.FromBorder.ID].LineString.ID == allWays[allLanes[lanes[i].ID].NextBorder.ID].LineString.ID ||
                        allWays[OriginTrack.FromBorder.ID].LineString.ID == allWays[allLanes[lanes[i].ID].PrevBorder.ID].LineString.ID)
                    {
                        UpLink = link;

                        UpLane = i;
                    }

                    if (allWays[OriginTrack.ToBorder.ID].LineString.ID == allWays[allLanes[lanes[i].ID].NextBorder.ID].LineString.ID ||
                        allWays[OriginTrack.ToBorder.ID].LineString.ID == allWays[allLanes[lanes[i].ID].PrevBorder.ID].LineString.ID)
                    {
                        DownLink = link;

                        DownLane = i;
                    }
                }
            }

            if (UpLink == null || DownLink == null)
            {
                Debug.LogError("Link not found " + ID);

                return;
            }

            var linkid = UpLink.ID.Replace(SimRoadNetworkLink.IDPrefix, "").Split('_');

            ID += "_" + linkid[0] + "_" + order;
        }

        public List<GeoJSON.Net.Geometry.Position> GetGeometory()
        {
            var coods = new List<GeoJSON.Net.Geometry.Position>();

            for (int i = 0; i < TrackResolution; i++)
            {
                var point = UnityEngine.Splines.SplineUtility.EvaluatePosition(OriginTrack.Spline, (float)i / TrackResolution);

                var cood = point;

                var geoCood = SimRoadNetworkManager.GeoReference.Unproject(new PlateauVector3d(cood.x, cood.y, cood.z));

                coods.Add(new GeoJSON.Net.Geometry.Position(geoCood.Latitude, geoCood.Longitude));
            }

            return coods;
        }

        public float GetLength()
        {
            return UnityEngine.Splines.SplineUtility.CalculateLength(OriginTrack.Spline, Matrix4x4.identity);
        }
    }
}