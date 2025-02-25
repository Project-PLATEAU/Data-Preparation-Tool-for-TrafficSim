using GeoJSON.Net.Geometry;
using PLATEAU.CityInfo;
using PLATEAU.Geometries;
using PLATEAU.Native;
using PLATEAU.RoadNetwork;
using PLATEAU.RoadNetwork.Data;
using PLATEAU.RoadNetwork.Structure;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using System.Windows.Forms;

namespace TrafficSimulationTool.Runtime
{
    /// <summary>
    /// シミュレーション用道路ネットワークのレーンを表すクラス
    /// </summary>
    [System.Serializable]
    public class SimRoadNetworkLane
    {
        public static readonly string IDPrefix = "Lane";

        private const double EarthRadiusKm = 6371.0; // 地球の半径 (km)

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
        public RnDataLane OriginLane { get; private set; } = null;

        [field: SerializeField]
        public int Order { get; private set; } = 0;

        public SimRoadNetworkLane(string id, RnDataLane lane, int order)
        {
            ID = IDPrefix + id + "_" + order;

            OriginLane = lane;

            Order = order;
        }

        public List<GeoJSON.Net.Geometry.Position> GetGeometory(bool isCenter = true)
        {
            var coods = new List<GeoJSON.Net.Geometry.Position>();

            var ways = SimRoadNetworkManager.RoadNetworkDataGetter.GetWays();

            var linestrings = SimRoadNetworkManager.RoadNetworkDataGetter.GetLineStrings();

            var points = SimRoadNetworkManager.RoadNetworkDataGetter.GetPoints();

            var isReverse = OriginLane.IsReverse;

            var way = isCenter ? OriginLane.CenterWay : OriginLane.RightWay;

            if (way.IsValid)
            {
                var linestring = ways[way.ID].LineString;

                var line = linestrings[linestring.ID].Points;

                if (isReverse)
                {
                    linestrings[linestring.ID].Points.Reverse();
                }

                foreach (var point in linestrings[linestring.ID].Points)
                {
                    if (!point.IsValid) continue;

                    var cood = points[point.ID].Vertex;

                    var geoCood = SimRoadNetworkManager.GeoReference.Unproject(new PlateauVector3d(cood.x, cood.y, cood.z));

                    coods.Add(new GeoJSON.Net.Geometry.Position(geoCood.Latitude, geoCood.Longitude));
                }
            }

            if (coods.Count < 2)
            {
                // フォールバック

                Debug.LogWarning("Link geometry is invalid. Fallback to straight line." + ID);

                var geoCood = SimRoadNetworkManager.GeoReference.Unproject(new PlateauVector3d(0, 0, 0));

                if (coods.Count == 0)
                {
                    coods.Add(new GeoJSON.Net.Geometry.Position(geoCood.Latitude, geoCood.Longitude));
                }

                coods.Add(new GeoJSON.Net.Geometry.Position(geoCood.Latitude, geoCood.Longitude));
            }

            return coods;
        }

        public float GetLength()
        {
            return (float)CalculateTotalDistance(GetGeometory());
        }

        private double CalculateTotalDistance(List<GeoJSON.Net.Geometry.Position> positions)
        {
            double totalDistance = 0.0;

            for (int i = 0; i < positions.Count - 1; i++)
            {
                var start = positions[i];
                var end = positions[i + 1];
                totalDistance += HaversineDistance(start, end);
            }

            return totalDistance;
        }

        private double HaversineDistance(GeoJSON.Net.Geometry.Position pos1, GeoJSON.Net.Geometry.Position pos2)
        {
            double lat1 = pos1.Latitude * Math.PI / 180.0;
            double lon1 = pos1.Longitude * Math.PI / 180.0;
            double lat2 = pos2.Latitude * Math.PI / 180.0;
            double lon2 = pos2.Longitude * Math.PI / 180.0;

            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;

            double a = Math.Pow(Math.Sin(dLat / 2), 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusKm * c * 1000;
        }

        /// <summary>
        /// レーンの幅員を取得する
        /// </summary>
        public float GetLaneWidth()
        {
            var ways = SimRoadNetworkManager.RoadNetworkDataGetter.GetWays();

            var linestrings = SimRoadNetworkManager.RoadNetworkDataGetter.GetLineStrings();

            var points = SimRoadNetworkManager.RoadNetworkDataGetter.GetPoints();

            var wayleft = OriginLane.LeftWay;

            var wayRight = OriginLane.RightWay;

            if (wayleft.IsValid == false || wayRight.IsValid == false)
            {
                return 0;
            }

            var linestringleft = ways[wayleft.ID].LineString;

            var linestringright = ways[wayRight.ID].LineString;

            var pointsleft = linestrings[linestringleft.ID].Points;

            var pointsright = linestrings[linestringright.ID].Points;

            var left = new List<Vector3>();

            var right = new List<Vector3>();

            foreach (var point in pointsleft)
            {
                if (!point.IsValid) continue;

                var cood = points[point.ID].Vertex;

                left.Add(cood);
            }

            foreach (var point in pointsright)
            {
                if (!point.IsValid) continue;

                var cood = points[point.ID].Vertex;

                right.Add(cood);
            }

            return GetMinimumDistanceBetweenPointSets(left, right);
        }

        /// <summary>
        /// 2つの点集合間の最短距離を計算する
        /// </summary>
        /// <param name="pointSetA"></param>
        /// <param name="pointSetB"></param>
        /// <returns></returns>
        private float GetMinimumDistanceBetweenPointSets(List<Vector3> pointSetA, List<Vector3> pointSetB)
        {
            float minDistance = float.MaxValue;

            // 点集合Aのすべての点
            foreach (Vector3 pointA in pointSetA)
            {
                // 点集合Bのすべての点
                foreach (Vector3 pointB in pointSetB)
                {
                    // 点Aと点B間の距離
                    float distance = Vector3.Distance(pointA, pointB);

                    // 最短距離を更新
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }
            }

            return minDistance;
        }
    }
}