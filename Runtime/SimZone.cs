using NetTopologySuite.Geometries;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulationTool.Runtime
{
    /// <summary>
    /// シミュレーション用ゾーンを表すクラス
    /// </summary>
    public class SimZone : MonoBehaviour
    {
        public static readonly string IDPrefix = "Zone";

        public string ID;

        /// <summary>
        /// グループのGeometry
        /// </summary>
        public NetTopologySuite.Geometries.Geometry GroupGeometry;

        /// <summary>
        /// ジオメトリ内のノードを除外するかのフラグ
        /// </summary>
        public bool ExcludeNodes = true;

        /// <summary>
        /// ゾーンに属するノード
        /// </summary>
        [field: SerializeField]
        public List<SimRoadNetworkNode> SimRoadNetworkNodes = new List<SimRoadNetworkNode>();

        /// <summary>
        /// ゾーンに属するリンク
        /// </summary>
        [field: SerializeField]
        public List<SimRoadNetworkLink> SimRoadNetworkLinks = new List<SimRoadNetworkLink>();
    }
}