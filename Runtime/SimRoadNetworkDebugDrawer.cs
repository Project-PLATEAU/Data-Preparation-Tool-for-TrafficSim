using PLATEAU.Native;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace TrafficSimulationTool.Runtime
{
    /// <summary>
    /// シミュレーション用道路ネットワークのデバッグ描画を行うクラス
    /// </summary>
    public class SimRoadNetworkDebugDrawer : MonoBehaviour
    {
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

        [System.Serializable]
        public enum DrawMode
        {
            Both,
            Foward,
            Reverse,
        }

        [field: SerializeField]
        public bool EnableNodeDrawing { get; private set; } = false;

        [field: SerializeField]
        public bool EnableLinkDrawing { get; private set; } = false;

        [field: SerializeField]
        public DrawMode LinkDrawingMode { get; private set; } = DrawMode.Both;

        [field: SerializeField]
        public bool EnableLaneDrawing { get; private set; } = false;

        [field: SerializeField]
        public DrawMode LaneDrawingMode { get; private set; } = DrawMode.Both;

        // Start is called before the first frame update
        private void Start()
        {
        }

        // Update is called once per frame
        private void Update()
        {
        }

        private void OnDrawGizmos()
        {
            if (EnableNodeDrawing)
            {
                DrawNodes();
            }

            if (EnableLinkDrawing)
            {
                DrawLinks();
            }

            if (EnableLaneDrawing)
            {
                DrawLanes();
            }
        }

        private void DrawNodes()
        {
#if UNITY_EDITOR
            var size = 3f;
            var color = Color.green;

            // ノード情報の描画
            foreach (var node in SimRoadNetworkManager.SimRoadNetworkNodes)
            {
                var geo = node.GetGeometory();

                var coord = SimRoadNetworkManager.GeoReference.Project(new GeoCoordinate(geo.Latitude, geo.Longitude, 0));

                var pos = new Vector3((float)coord.X, (float)coord.Y, (float)coord.Z);

                var label = node.ID;

                UnityEditor.Handles.color = color;
                UnityEditor.Handles.DrawWireCube(pos, Vector3.one * size);

                // yellow: 仮想ノード, red: 接続点ノード
                GUIStyle style = new GUIStyle();
                style.normal.textColor = node.IsVirtual ? Color.yellow : node.OriginTran == null ? Color.red : Color.white;
                UnityEditor.Handles.Label(pos, label, style);
            }
#endif
        }

        private void DrawLinks()
        {
#if UNITY_EDITOR
            var size = 3f;
            var color = Color.green;

            // リンク情報の描画
            foreach (var link in SimRoadNetworkManager.SimRoadNetworkLinks)
            {
                if (LinkDrawingMode == DrawMode.Foward && link.IsReverse) continue;

                if (LinkDrawingMode == DrawMode.Reverse && !link.IsReverse) continue;

                if (link.OriginTran == null)
                {
                    continue;
                }

                if (link.Lanes.Count == 0)
                {
                    continue;
                }

                Spline spline = new Spline();

                foreach (var geoCoord in link.Lanes[link.IsReverse ? link.Lanes.Count - 1 : 0].GetGeometory(false))
                {
                    var geo = SimRoadNetworkManager.GeoReference.Project(new GeoCoordinate(geoCoord.Latitude, geoCoord.Longitude, 0));

                    spline.Add(new BezierKnot(new Vector3((float)geo.X, (float)geo.Y, (float)geo.Z)));
                }

                spline.Closed = false;

                var pos = spline.EvaluatePosition(0.5f);
                var label = link.ID;

                UnityEditor.Handles.color = color;
                UnityEditor.Handles.DrawWireCube(pos, Vector3.one * size);

                // yellow: 仮想リンク, red: 接続リンク
                GUIStyle style = new GUIStyle();
                style.normal.textColor = link.OriginLink == null ? Color.yellow : link.OriginTran == null ? Color.red : Color.white;
                UnityEditor.Handles.Label(pos, label, style);
            }
#endif
        }

        private void DrawLanes()
        {
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;

            // リンク情報の描画
            foreach (var link in SimRoadNetworkManager.SimRoadNetworkLinks)
            {
                if (LaneDrawingMode == DrawMode.Foward && link.IsReverse) continue;

                if (LaneDrawingMode == DrawMode.Reverse && !link.IsReverse) continue;

                if (link.OriginTran != null && link.GetOriginLanes().Count == 0) continue;

                var height = link.OriginTran != null ? link.OriginTran.GetComponent<MeshRenderer>().bounds.center.y : 0.0f;

                var coords = link.GetGeometory();

                var points = new List<Vector3>();

                foreach (var coord in coords)
                {
                    var point = SimRoadNetworkManager.GeoReference.Project(new GeoCoordinate(coord.Latitude, coord.Longitude, height));

                    points.Add(new Vector3((float)point.X, (float)point.Y, (float)point.Z));
                }

                var direction = points[points.Count - 1] - points[points.Count - 2];
                var normalizedDirection = direction.normalized;
                var arrowLength = 3.0f;
                var arrowBase = points[points.Count - 1] - normalizedDirection * arrowLength;
                var perpendicularDirection = new Vector3(-normalizedDirection.z, 0, normalizedDirection.x); //垂直ベクトル
                var arrowWidth = 1.0f;
                var arrowWingL = arrowBase + perpendicularDirection * arrowWidth;
                var arrowWingR = arrowBase - perpendicularDirection * arrowWidth;
                var arrow = new Vector3[] { arrowWingL, points[points.Count - 1], arrowWingR };

                UnityEditor.Handles.DrawPolyLine(points.ToArray());
                UnityEditor.Handles.DrawPolyLine(arrow);
            }
#endif
        }
    }
}