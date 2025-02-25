using PLATEAU.CityInfo;
using PLATEAU.Geometries;
using PLATEAU.RoadNetwork.Data;
using PLATEAU.RoadNetwork;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TrafficSimulationTool.Runtime;
using UnityEngine;
using System.Linq;
using PLATEAU.RoadNetwork.Structure;
using NetTopologySuite.Geometries;

namespace TrafficSimulationTool.Editor
{
    public class RoadNetworkExporter
    {
        private const string CRS = "urn:ogc:def:crs:EPSG::6697";

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

        public const string ExportFileNameZone = "output_zone.geojson";
        public const string ExportFileNameLink = "output_link.geojson";
        public const string ExportFileNameLane = "output_lane.geojson";
        public const string ExportFileNameNode = "output_node.geojson";
        public const string ExportFileNameTrack = "output_track.geojson";
        public const string ExportFileNameSignalControler = "output_signalcontroler.geojson";
        public const string ExportFileNameSignalLight = "output_signallight.geojson";
        public const string ExportFileNameSignalStep = "output_signalstep.geojson";

        [SerializeField]
        private string exportPath = Application.persistentDataPath;

        public void ExportSimRoadNetwork(string path)
        {
            exportPath = path;

            ExportNode(SimRoadNetworkManager.SimRoadNetworkNodes);

            ExportLink(SimRoadNetworkManager.SimRoadNetworkLinks);

            ExportLane(SimRoadNetworkManager.SimRoadNetworkLinks);

            ExportTrack(SimRoadNetworkManager.SimRoadNetworkNodes);

            var zones = new List<SimZone>();

            zones.AddRange(SimRoadNetworkManager.TrafficFocusAreas.Cast<SimZone>().ToList());
            zones.AddRange(SimRoadNetworkManager.TrafficFocusGroups.Cast<SimZone>().ToList());

            ExportZone(zones);

#if PLATEAU_TOOLKIT

            List<SimSignalController> signalControllers = new List<SimSignalController>();

            List<SimSignalLight> signalLights = new List<SimSignalLight>();

            List<SimSignalStep> signalSteps = new List<SimSignalStep>();

            GenerateExpSignal(signalControllers, signalLights, signalSteps);

            ExportSignalController(signalControllers);

            ExportSignalLight(signalLights);

            ExportSignalStep(signalSteps);
#endif
            // SDK交通規制を利用する場合

            //ExportSignalController(SimRoadNetworkManager.SignalControllers);

            //ExportSignalLight(SimRoadNetworkManager.SignalLights);

            //ExportSignalStep(SimRoadNetworkManager.SignalSteps);
        }

        /// <summary>
        /// 信号現示情報を生成します
        /// </summary>
        /// <param name="context"></param>
        private void GenerateExpSignal(List<SimSignalController> signalControllers, List<SimSignalLight> signalLights, List<SimSignalStep> signalSteps)
        {
#if PLATEAU_TOOLKIT

            var roadNetworkRoads = SimRoadNetworkManager.RoadNetworkDataGetter.GetRoadBases();

            var trafficIntersections = GameObject.FindObjectsOfType<AWSIM.TrafficSimulation.TrafficIntersection>();

            foreach (var intersection in trafficIntersections)
            {
                var node = intersection.rnTrafficLightController.Parent.ID;

                var signalController = new SimSignalController();

                signalController.ID = SimSignalController.IDPrefix + node.ToString();

                signalController.Node = SimRoadNetworkManager.SimRoadNetworkNodes.Find(x => x.OriginNode == roadNetworkRoads[node]);

                signalController.Coord = intersection.transform.position;

                // オフセットは非対応
                //signalController.OffsetController
                //signalController.OffsetType
                //signalController.OffsetValue

                var linkGroups = new List<List<(SimRoadNetworkLink, SimRoadNetworkLink)>>();

                // 信号灯火器の生成

                foreach (var trafficLightGroup in intersection.TrafficLightGroups.Select((value, index) => new { value, index }))
                {
                    var linkPairs = new List<(SimRoadNetworkLink, SimRoadNetworkLink)>();

                    foreach (var trafficLight in trafficLightGroup.value.TrafficLights.Select((value, index) => new { value, index }))
                    {
                        var signalLight = new SimSignalLight();

                        signalLight.ID = SimSignalLight.IDPrefix + node + "_" + trafficLightGroup.index + "_" + trafficLight.index;

                        signalLight.Controller = signalController;

                        signalLight.Coord = trafficLight.value.transform.position;

                        signalLight.Link = SimRoadNetworkManager.SimRoadNetworkLinks.Find(x => x.OriginLink == roadNetworkRoads[trafficLight.value.rnTrafficLight.RoadId.ID] && x.DownNode == signalController.Node); // OriginLink と DownNode からリンクを特定

                        // トラックからリンクペアを検索
                        foreach (var track in signalController.Node.Tracks)
                        {
                            if (track.UpLink == signalLight.Link)
                            {
                                if (!linkPairs.Contains((track.UpLink, track.DownLink)))
                                {
                                    linkPairs.Add((track.UpLink, track.DownLink));
                                }
                            }
                        }

                        signalLights.Add(signalLight);

                        signalController.SignalLights.Add(signalLight); // 信号制御器にも追加
                    }

                    linkGroups.Add(linkPairs);
                }

                // 信号現示階梯の生成

                foreach (var lightingSequence in intersection.LightingSequences.Select((value, index) => new { value, index }))
                {
                    var signalStep = new SimSignalStep();

                    signalStep.ID = SimSignalStep.IDPrefix + node + "_" + lightingSequence.index;

                    signalStep.Controller = signalController;

                    signalStep.SignalLights = signalController.SignalLights;

                    signalStep.PatternID = "SignalPattern" + node + "_0"; // 時間帯別制御パターンは非対応

                    signalStep.Order = lightingSequence.index;

                    signalStep.Duration = (int)lightingSequence.value.IntervalSec;

                    foreach (var groupLightingOrder in lightingSequence.value.GroupLightingOrders)
                    {
                        var group = groupLightingOrder.Group;

                        var bulb = groupLightingOrder.BulbData;

                        if (bulb == null || bulb.Length == 0)
                        {
                            continue;
                        }

                        if (bulb[0].Color == AWSIM.TrafficLightData.BulbColor.GREEN)
                        {
                            signalStep.LinkPairsGreen = linkGroups[group];
                        }
                        else if (bulb[0].Color == AWSIM.TrafficLightData.BulbColor.YELLOW)
                        {
                            signalStep.LinkPairsYellow = linkGroups[group];
                        }
                        else if (bulb[0].Color == AWSIM.TrafficLightData.BulbColor.RED)
                        {
                            signalStep.LinkPairsRed = linkGroups[group];
                        }
                    }

                    signalSteps.Add(signalStep);

                    // 信号制御器にも追加・時間帯別制御パターンは非対応

                    var startTime = "00/00/00";

                    if (!signalController.SignalPatterns.ContainsKey(startTime))
                    {
                        signalController.SignalPatterns[startTime] = new List<SimSignalStep>();
                    }

                    signalController.SignalPatterns[startTime].Add(signalStep);
                }

                signalControllers.Add(signalController);
            }
#endif
        }

        private void ExportLink(List<SimRoadNetworkLink> simRoadNetworkLinks)
        {
            var simLinks = new List<GeoJsonFeature>();

            foreach (var simLink in simRoadNetworkLinks)
            {
                // レーンが存在しない場合はリンクを出力しない
                if (simLink.OriginTran != null && simLink.GetOriginLanes().Count == 0)
                {
                    Debug.Log("No valid lane is linked to this link. " + simLink.ID);

                    continue;
                }

                var geom = simLink.GetGeometory();

                var lineString = new GeoJSON.Net.Geometry.LineString(geom);

                var propety = new GeoJsonFeaturePropertiesLink
                {
                    ID = simLink.ID,
                    UPNODE = simLink.UpNode.ID,
                    DOWNNODE = simLink.DownNode.ID,
                    LENGTH = simLink.GetLaneLength(),
                    LANENUM = simLink.GetLaneNum(),

                    // SDK非対応
                    RLANENUM = 0,
                    RLANELENGTH = 0,
                    LLANENUM = 0,
                    LLANELENGTH = 0,
                    PROHIBIT = GeoJsonFeatureProperties.ProhiBit.UTurn,
                    TURNCONFIG = (int)(GeoJsonFeatureProperties.TurnConfig.Left | GeoJsonFeatureProperties.TurnConfig.Straight | GeoJsonFeatureProperties.TurnConfig.Right),
                    TYPECONFIG = (int)(GeoJsonFeatureProperties.TypeConfig.Small | GeoJsonFeatureProperties.TypeConfig.Large | GeoJsonFeatureProperties.TypeConfig.Bus),
                };

                simLinks.Add(new GeoJsonFeature(lineString, propety));
            }

            ExportGeoJson(simLinks, ExportFileNameLink);
        }

        private void ExportLane(List<SimRoadNetworkLink> simRoadNetworkLinks)
        {
            var simLanes = new List<GeoJsonFeature>();

            foreach (var simLink in simRoadNetworkLinks)
            {
                foreach (var simLane in simLink.Lanes)
                {
                    var geom = simLane.GetGeometory();

                    var lineString = new GeoJSON.Net.Geometry.LineString(geom);

                    var propety = new GeoJsonFeaturePropertiesLane
                    {
                        ID = simLane.ID,
                        LINKID = simLink.ID,
                        LANEPOS = simLane.Order,
                        LENGTH = simLane.GetLength(),
                        WIDTH = simLane.GetLaneWidth(),
                    };

                    simLanes.Add(new GeoJsonFeature(lineString, propety));
                }
            }

            ExportGeoJson(simLanes, ExportFileNameLane);
        }

        private void ExportNode(List<SimRoadNetworkNode> simRoadNetworkNodes)
        {
            var simNodes = new List<GeoJsonFeature>();

            foreach (var simNode in simRoadNetworkNodes)
            {
                var geom = simNode.GetGeometory();

                var point = new GeoJSON.Net.Geometry.Point(geom);

                var propety = new GeoJsonFeaturePropertiesNode
                {
                    ID = simNode.ID
                };

                simNodes.Add(new GeoJsonFeature(point, propety));
            }

            ExportGeoJson(simNodes, ExportFileNameNode);
        }

        private void ExportZone(List<SimZone> simRoadNetworkZones)
        {
            var simNodes = new List<GeoJsonFeature>();

            foreach (var simZone in simRoadNetworkZones)
            {
                var geom = GetGeometry(simZone);

                var polygon = ConvertToGeoJsonPolygon(geom);

                var propety = new GeoJsonFeaturePropertiesZone
                {
                    ID = simZone.ID,
                    NODEID = simZone.SimRoadNetworkNodes.Select(x => x.ID).ToList(),
                    SIDETYPE = simZone as TrafficFocusArea && (simZone as TrafficFocusArea).AreaType == TrafficFocusArea.AreaTypes.Intrude ? "Out" : "In",
                };

                simNodes.Add(new GeoJsonFeature(polygon, propety));
            }

            ExportGeoJson(simNodes, ExportFileNameZone);
        }

        private void ExportTrack(List<SimRoadNetworkNode> simRoadNetworkNodes)
        {
            var simNodes = new List<GeoJsonFeature>();

            foreach (var simNode in simRoadNetworkNodes)
            {
                foreach (var simTrack in simNode.Tracks)
                {
                    var geom = simTrack.GetGeometory();

                    var point = new GeoJSON.Net.Geometry.LineString(geom);

                    var propety = new GeoJsonFeaturePropertiesTrack
                    {
                        ID = simTrack.ID,
                        ORDER = simTrack.Order,
                        UPLINKID = simTrack.UpLink?.ID,
                        UPLANEPOS = simTrack.UpLane,
                        UPDISTANCE = simTrack.UpDistance,
                        DOWNLINKID = simTrack.DownLink?.ID,
                        DOWNLANEPOS = simTrack.DownLane,
                        DOWNDISTANCE = simTrack.DownDistance,
                        LENGTH = simTrack.GetLength(),
                        TURNCONFIG = (int)(GeoJsonFeatureProperties.TurnConfig.Left | GeoJsonFeatureProperties.TurnConfig.Straight | GeoJsonFeatureProperties.TurnConfig.Right),
                        TYPECONFIG = (int)(GeoJsonFeatureProperties.TypeConfig.Small | GeoJsonFeatureProperties.TypeConfig.Large | GeoJsonFeatureProperties.TypeConfig.Bus),
                    };

                    simNodes.Add(new GeoJsonFeature(point, propety));
                }
            }

            ExportGeoJson(simNodes, ExportFileNameTrack);
        }

        private void ExportSignalController(List<SimSignalController> simSignalControllers)
        {
            var geoJsonFeature = new List<GeoJsonFeature>();

            foreach (var simSignalController in simSignalControllers)
            {
                var geom = simSignalController.GetGeometory();

                var point = new GeoJSON.Net.Geometry.Point(geom);

                var propety = new GeoJsonFeaturePropertiesSignalController
                {
                    ID = simSignalController.ID,
                    ALLOCNODE = simSignalController.GetNode(),
                    SIGLIGHT = simSignalController.GetSignalLights(),
                    OFFSETBASESIGID = simSignalController.OffsetController != null ? simSignalController.OffsetController.ID : string.Empty,
                    NUMOFPATTERN = simSignalController.GetPatternNum(),
                    PATTERNID = simSignalController.GetPatternID(),
                    INITCYCLE = simSignalController.GetCycleLen(),
                    PHASENUM = simSignalController.GetPhaseNum(),
                    OFFSETTYPE = simSignalController.OffsetType,
                    OFFSET = simSignalController.OffsetValue,
                    STARTTIME = simSignalController.GetStartTime(),
                };

                geoJsonFeature.Add(new GeoJsonFeature(point, propety));
            }

            ExportGeoJson(geoJsonFeature, ExportFileNameSignalControler);
        }

        private void ExportSignalLight(List<SimSignalLight> simSignalLights)
        {
            var geoJsonFeature = new List<GeoJsonFeature>();

            foreach (var simSignalLight in simSignalLights)
            {
                var geom = simSignalLight.GetGeometory();

                var point = new GeoJSON.Net.Geometry.Point(geom);

                var propety = new GeoJsonFeaturePropertiesSignalLight
                {
                    ID = simSignalLight.ID,
                    SIGNALID = simSignalLight.Controller.GetID(),
                    LINKID = simSignalLight.Link?.ID,
                    LANETYPE = simSignalLight.GetLaneType(),
                    LANEPOS = simSignalLight.GetLanePos(),
                    DISTANCE = simSignalLight.GetDistance(),
                };

                geoJsonFeature.Add(new GeoJsonFeature(point, propety));
            }

            ExportGeoJson(geoJsonFeature, ExportFileNameSignalLight);
        }

        private void ExportSignalStep(List<SimSignalStep> simSignalSteps)
        {
            var geoJsonFeature = new List<GeoJsonFeature>();

            foreach (var simSignalStep in simSignalSteps)
            {
                var geom = simSignalStep.GetGeometory();

                var point = new GeoJSON.Net.Geometry.Point(geom);

                var propety = new GeoJsonFeaturePropertiesSignalStep
                {
                    ID = simSignalStep.ID,
                    PATTERNID = simSignalStep.PatternID,
                    ORDER = simSignalStep.Order,
                    DURATION = simSignalStep.Duration,
                    SIGLIGHT = simSignalStep.GetSignalLights(),
                    TYPEMASK = simSignalStep.GetTypeMask(),
                    GREEN = simSignalStep.GetColor(simSignalStep.LinkPairsGreen),
                    YELLOW = simSignalStep.GetColor(simSignalStep.LinkPairsYellow),
                    RED = simSignalStep.GetColor(simSignalStep.LinkPairsRed),
                };

                geoJsonFeature.Add(new GeoJsonFeature(point, propety));
            }

            ExportGeoJson(geoJsonFeature, ExportFileNameSignalStep);
        }

        private Geometry GetGeometry(SimZone simZone)
        {
            var pbMesh = simZone.GetComponent<UnityEngine.ProBuilder.ProBuilderMesh>();

            var mesh = new Mesh();
            mesh.SetVertices(pbMesh.GetVertices().Select(o => new Vector3()
            {
                x = o.position.x,
                y = o.position.y,
                z = o.position.z,
            }).ToList());
            var indices = new List<int>();
            pbMesh.faces.ToList().ForEach(o => { indices.AddRange(o.indexes.ToList()); });
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return ConvertMeshToGeometry(mesh);
        }

        private static Geometry ConvertMeshToGeometry(Mesh mesh)
        {
            // Meshから頂点を取得
            Vector3[] vertices = mesh.vertices;

            // Vector3の配列をNetTopologySuiteのCoordinate配列に変換
            List<Coordinate> coordinates = vertices.Select(v => new Coordinate(v.x, v.z)).ToList(); // z軸をy軸として扱う

            // 凸包を計算し、外形を求める
            GeometryFactory geometryFactory = new GeometryFactory();
            var convexHull = geometryFactory.CreateMultiPointFromCoords(coordinates.ToArray()).ConvexHull();

            return convexHull;
        }

        private GeoJSON.Net.Geometry.Polygon ConvertToGeoJsonPolygon(Geometry geometry)
        {
            // GeometryがPolygonかどうか確認
            if (!(geometry is Polygon polygon))
            {
                return null;
            }

            // ExteriorRing（外側のリング）の座標を取得
            var coordinates = polygon.ExteriorRing.Coordinates;

            // NetTopologySuiteのCoordinateをGeoJSON.Net.Geometry.Positionに変換
            List<GeoJSON.Net.Geometry.Position> exteriorPositions = coordinates.Select(c => ConvertToGeometryToGeoJson(c)).ToList();

            // GeoJSONのLineStringとして外形を定義
            GeoJSON.Net.Geometry.LineString exteriorLineString = new GeoJSON.Net.Geometry.LineString(exteriorPositions);

            // GeoJSONのPolygonを作成
            return new GeoJSON.Net.Geometry.Polygon(new List<GeoJSON.Net.Geometry.LineString> { exteriorLineString });
        }

        private GeoJSON.Net.Geometry.Position ConvertToGeometryToGeoJson(NetTopologySuite.Geometries.Coordinate coordinate)
        {
            var geoCood = SimRoadNetworkManager.GeoReference.Unproject(new PLATEAU.Native.PlateauVector3d(coordinate.X, 0, coordinate.Y));

            return new GeoJSON.Net.Geometry.Position(geoCood.Latitude, geoCood.Longitude);
        }

        private async void ExportGeoJson(List<GeoJsonFeature> features, string fileName)
        {
            var geoJson = GeoJsonExporter.CreateGeoJson(features, CRS);

            Debug.Log(geoJson);

            string path = Path.Combine(exportPath, fileName);

            await GeoJsonExporter.ExportGeoJsonAsync(geoJson, path, () => Debug.Log("Json file saved successfully."));
        }
    }
}