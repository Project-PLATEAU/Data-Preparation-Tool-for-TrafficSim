using PLATEAU.Native;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficSimulationTool.Runtime
{
    public class SimSignalLight
    {
        public static readonly string IDPrefix = "SignalLight";

        public string ID;

        public SimSignalController Controller;

        public SimRoadNetworkLink Link;

        public Vector3 Coord;

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

        public string GetLaneType()
        {
            return "Lane";
        }

        public string GetLanePos()
        {
            return "-1";
        }

        public string GetDistance()
        {
            return "0";
        }

        public GeoJSON.Net.Geometry.Position GetGeometory()
        {
            Vector3 coord = Coord;

            var geoCoord = SimRoadNetworkManager.GeoReference.Unproject(new PlateauVector3d(coord.x, coord.y, coord.z));

            return new GeoJSON.Net.Geometry.Position(geoCoord.Latitude, geoCoord.Longitude);
        }
    }
}