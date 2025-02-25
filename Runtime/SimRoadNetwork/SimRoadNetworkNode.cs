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

namespace TrafficSimulationTool.Runtime
{
    /// <summary>
    /// シミュレーション用道路ネットワークのノードを表すクラス
    /// </summary>
    [System.Serializable]
    public class SimRoadNetworkNode
    {
        public static readonly string IDPrefix = "Node";

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
        public int RoadNetworkIndex { get; private set; } = -1;

        public RnDataIntersection OriginNode
        {
            get
            {
                if (RoadNetworkIndex < 0)
                {
                    return null;
                }

                return SimRoadNetworkManager.RoadNetworkDataGetter.GetRoadBases()[RoadNetworkIndex] as RnDataIntersection;
            }
        }

        public PLATEAUCityObjectGroup OriginTran
        {
            get
            {
                return OriginNode?.TargetTran;
            }
        }

        public Mesh Mesh
        {
            get
            {
                return OriginTran?.GetComponent<MeshFilter>().sharedMesh;
            }
        }

        public bool IsVirtual
        {
            get
            {
                return RoadNetworkIndex < 0;
            }
        }

        [field: SerializeField]
        public Vector3 Coord;

        [field: SerializeField]
        public List<SimRoadNetworkTrack> Tracks = new List<SimRoadNetworkTrack>();

        public SimRoadNetworkNode(string id, int index)
        {
            ID = IDPrefix + id;

            RoadNetworkIndex = index;
        }

        public void GenerateTrack()
        {
            Tracks = new List<SimRoadNetworkTrack>();

            if (OriginNode != null)
            {
                for (int i = 0; i < OriginNode.Tracks.Count; i++)
                {
                    Tracks.Add(new SimRoadNetworkTrack(ID.Replace(SimRoadNetworkNode.IDPrefix, ""), OriginNode.Tracks[i], i));
                }
            }
        }

        public GeoJSON.Net.Geometry.Position GetGeometory()
        {
            Vector3 coord = Coord;

            if (!IsVirtual && OriginNode != null)
            {
                coord = GetPosition();
            }

            var geoCoord = SimRoadNetworkManager.GeoReference.Unproject(new PlateauVector3d(coord.x, coord.y, coord.z));

            return new GeoJSON.Net.Geometry.Position(geoCoord.Latitude, geoCoord.Longitude);
        }

        public Vector3 GetPosition()
        {
            var coods = new List<Vector3>();

            var all_ways = SimRoadNetworkManager.RoadNetworkDataGetter.GetWays();

            var all_linestrings = SimRoadNetworkManager.RoadNetworkDataGetter.GetLineStrings();

            var all_points = SimRoadNetworkManager.RoadNetworkDataGetter.GetPoints();

            var neighbors = OriginNode.Neighbors;

            var ways = neighbors.Select(n => n.Border).ToList();

            foreach (var way in ways)
            {
                if (!way.IsValid) continue;

                var linestring = all_ways[way.ID].LineString;

                var line = all_linestrings[linestring.ID].Points;

                foreach (var point in line)
                {
                    if (!point.IsValid) continue;

                    var vertex = all_points[point.ID].Vertex;

                    coods.Add(new Vector3(vertex.x, vertex.y, vertex.z));
                }
            }

            // 重心を求める
            var coord = new Vector3(coods.Average(c => c.x), coods.Average(c => c.y), coods.Average(c => c.z));

            return coord;
        }
    }
}