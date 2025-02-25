using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficSimulationTool.Runtime
{
    [System.Serializable]
    public class SimSignalStep
    {
        public static readonly string IDPrefix = "SignalStep";

        public string ID;

        public SimSignalController Controller;

        public List<SimSignalLight> SignalLights;

        public string PatternID;

        public int Order;

        public int Duration;

        public List<(SimRoadNetworkLink In, SimRoadNetworkLink Out)> LinkPairsGreen = new List<(SimRoadNetworkLink In, SimRoadNetworkLink Out)>();

        public List<(SimRoadNetworkLink In, SimRoadNetworkLink Out)> LinkPairsYellow = new List<(SimRoadNetworkLink In, SimRoadNetworkLink Out)>();

        public List<(SimRoadNetworkLink In, SimRoadNetworkLink Out)> LinkPairsRed = new List<(SimRoadNetworkLink In, SimRoadNetworkLink Out)>();

        public string GetColor(List<(SimRoadNetworkLink In, SimRoadNetworkLink Out)> pair)
        {
            var ret = "";

            foreach (var p in pair)
            {
                if (ret != "")
                {
                    ret += ":";
                }

                if (p.In == null || p.Out == null)
                {
                    continue;
                }

                ret += p.In.ID + "->" + p.Out.ID;
            }

            return ret;
        }

        public string GetSignalLights()
        {
            var ret = SignalLights[0].ID;

            for (int i = 1; i < SignalLights.Count; i++)
            {
                ret += ":" + SignalLights[i].ID;
            }

            return ret;
        }

        public int GetTypeMask()
        {
            return -1;
        }

        public GeoJSON.Net.Geometry.Position GetGeometory()
        {
            return Controller.GetGeometory();
        }
    }
}