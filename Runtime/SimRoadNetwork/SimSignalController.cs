using PLATEAU.Native;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrafficSimulationTool.Runtime
{
    [System.Serializable]
    public class SimSignalController
    {
        public static readonly string IDPrefix = "SignalController";

        public string ID;

        public SimRoadNetworkNode Node;

        public Vector3 Coord;

        public List<SimSignalLight> SignalLights = new List<SimSignalLight>();

        public SimSignalController OffsetController;

        public int OffsetType;

        public int OffsetValue;

        public Dictionary<string, List<SimSignalStep>> SignalPatterns = new Dictionary<string, List<SimSignalStep>>();

        public int PatternIndex;

        //public List<string> StartTime;

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

        public string GetID()
        {
            return ID;
        }

        public string GetNode()
        {
            return Node.ID;
        }

        public string GetSignalLights()
        {
            if (SignalLights.Count == 0)
            {
                return "";
            }

            var ret = SignalLights[0].ID;

            for (int i = 1; i < SignalLights.Count; i++)
            {
                ret += ":" + SignalLights[i].ID;
            }

            return ret;
        }

        public int GetPatternNum()
        {
            return SignalPatterns.Count;
        }

        public string GetPatternID()
        {
            string ret = "";

            foreach (var pattern in SignalPatterns.Select((kvp, index) => new { kvp, index }))
            {
                if (pattern.kvp.Value.Count == 0) continue;

                ret += pattern.index == 0 ? pattern.kvp.Value[0].PatternID : ":" + pattern.kvp.Value[0].PatternID;
            }

            return ret;
        }

        public string GetCycleLen()
        {
            string ret = "";

            foreach (var pattern in SignalPatterns.Select((kvp, index) => new { kvp, index }))
            {
                int cycle = 0;

                pattern.kvp.Value.ForEach(x => cycle += x.Duration);

                ret += pattern.index == 0 ? cycle.ToString() : ":" + cycle.ToString();
            }
            return ret;
        }

        public string GetPhaseNum()
        {
            string ret = "";

            foreach (var pattern in SignalPatterns.Select((kvp, index) => new { kvp, index }))
            {
                int phase = pattern.kvp.Value.Count;

                ret += pattern.index == 0 ? phase.ToString() : ":" + phase.ToString();
            }

            return ret;
        }

        public string GetStartTime()
        {
            string ret = "";

            foreach (var pattern in SignalPatterns.Select((kvp, index) => new { kvp, index }))
            {
                ret += pattern.index == 0 ? pattern.kvp.Key : ":" + pattern.kvp.Key;
            }

            return ret;
        }

        public GeoJSON.Net.Geometry.Position GetGeometory()
        {
            Vector3 coord = Coord;

            var geoCoord = SimRoadNetworkManager.GeoReference.Unproject(new PlateauVector3d(coord.x, coord.y, coord.z));

            return new GeoJSON.Net.Geometry.Position(geoCoord.Latitude, geoCoord.Longitude);
        }
    }
}