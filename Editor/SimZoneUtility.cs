using Codice.Client.BaseCommands;
using PLATEAU.CityInfo;
using PLATEAU.RoadNetwork;
using PLATEAU.RoadNetwork.Data;
using System.Collections.Generic;
using System.Linq;
using TrafficSimulationTool.Runtime;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulationTool.Editor
{
    public class SimZoneUtility
    {
        /// <summary>
        /// ノードを割り当てる距離
        /// </summary>
        private const float AssignDistance = 50.0f;

        /// <summary>
        /// ゾーンを自動生成
        /// </summary>
        /// <returns></returns>
        public static List<GameObject> AutoGenerateSimZone(List<SimRoadNetworkNode> nodes, List<SimRoadNetworkLink> links)
        {
            // FIXME: 生成時間短縮のため中間生成オブジェクトのうちジオメトリ化しなくてもいいものがあれば除外する
            // ゾーンの中間生成物を格納するオブジェクトを作成
            var zoneChache = GetObjectFromRoot("ZoneCache");

            zoneChache.SetActive(false);

            // FIXME: 本来はロードネットワークから取得すべきだがとりあえずtranオブジェクトを参照
            // TODO: RoadNetworkManagerからGetterを取得できるようになったらTargetTranから取得するように変更
            var all = GameObject.FindObjectsOfType<PLATEAUCityObjectGroup>();

            List<GameObject> trans = new List<GameObject>();

            trans = all.Where(obj => obj.name.StartsWith("tran")).Select(obj => obj.gameObject).ToList();

            var meshes = new List<Mesh>();

            foreach (var tran in trans)
            {
                var mesh = tran.GetComponent<MeshFilter>().sharedMesh;

                meshes.Add(mesh);
            }

            // NetTopologySuiteを使用してmeshesを結合してポリゴンを作成
            var combineGeo = MergeMeshGeometries(meshes);
            var combineMesh = GeometryToMesh(combineGeo);
            var combineMeshObject = CreateMeshObject(combineMesh, "CombinedMesh", Color.white);
            combineMeshObject.transform.SetParent(zoneChache.transform);

            // バウンディングボックスのポリゴンを作成
            var boundsGeo = CreateBoundingBoxGeometry(combineGeo);
            var boundsMesh = GeometryToMesh(boundsGeo);
            var boundsMeshObject = CreateMeshObject(boundsMesh, "BoundsMesh", Color.yellow);
            boundsMeshObject.transform.SetParent(zoneChache.transform);

            // バウンディングボックスと結合ポリゴンの差分を取得
            var diffGeo = boundsGeo.Difference(combineGeo);
            var diffMesh = GeometryToMesh(diffGeo);
            var diffMeshObject = CreateMeshObject(diffMesh, "DiffMesh", Color.red);
            diffMeshObject.transform.SetParent(zoneChache.transform);

            // 差分ポリゴンをグループ化して表示
            var groupGeos = new List<NetTopologySuite.Geometries.Geometry>();
            if (diffGeo is NetTopologySuite.Geometries.Polygon polygon)
            {
                groupGeos.Add(polygon);
            }
            else if (diffGeo is NetTopologySuite.Geometries.MultiPolygon multiPolygon)
            {
                for (int i = 0; i < multiPolygon.NumGeometries; i++)
                {
                    groupGeos.Add((NetTopologySuite.Geometries.Polygon)multiPolygon.GetGeometryN(i));
                }
            }
            var groupMeshParentObject = new GameObject("GroupMesh");
            foreach (var geo in groupGeos.Select((value, index) => new { value, index }))
            {
                var groupMesh = GeometryToMesh(geo.value);
                var groupMeshObject = CreateMeshObject(groupMesh, "GroupMesh" + geo.index, Color.green);
                groupMeshObject.transform.SetParent(groupMeshParentObject.transform);
            }
            groupMeshParentObject.transform.SetParent(zoneChache.transform);

            // 表示オブジェクトと底面取得オブジェクトのペアを作成
            var bldgPairs = GetBuildingPairs();

            // 底面ジオメトリを作成、エリア判定してグループと紐づけ
            var blocks = GetBlockDictionary(bldgPairs, groupGeos);

            // グループごとに表示オブジェクトをまとめる
            foreach (var groupGeometryPair in blocks.Select((Value, Index) => new { Value, Index }))
            {
                var groupObject = new GameObject("Group" + groupGeometryPair.Index);
                foreach (var pair in groupGeometryPair.Value.Value)
                {
                    var footprintGeo = CreateFootprintGeometryFromMesh(pair.footprint.GetComponent<MeshFilter>().sharedMesh);
                    var groupMesh = GeometryToMesh(footprintGeo);
                    var groupMeshObject = CreateMeshObject(groupMesh, "GroupMesh", Color.cyan);
                    groupMeshObject.transform.SetParent(groupObject.transform);
                }
                groupObject.transform.SetParent(zoneChache.transform);
            }

            var zoneRoot = GameObject.FindObjectOfType<SimRoadNetworkManager>().gameObject;

            var convexHullMeshObjects = new List<GameObject>();

            // グループごとにジオメトリを囲ってゾーンのジオメトリを作成
            foreach (var block in blocks)
            {
                var footprintGeos = block.Value.Select(pair => CreateFootprintGeometryFromMesh(pair.footprint.GetComponent<MeshFilter>().sharedMesh));
                var convexHullGeo = CreateConvexHullGeometry(footprintGeos.ToList());
                var convexHullMesh = GeometryToMesh(convexHullGeo);
                var convexHullMeshObject = CreateMeshObject(convexHullMesh, "TrafficFocusGroup", Color.blue);

                convexHullMeshObjects.Add(convexHullMeshObject);

                convexHullMeshObject.transform.SetParent(zoneRoot.transform);

                // ゾーングループを作成
                var simzonegroup = convexHullMeshObject.AddComponent<TrafficFocusGroup>();

                foreach (var pair in block.Value)
                {
                    simzonegroup.AddBuilding(pair.building, pair.footprint);
                }

                // ゾーンに属するノードを登録
                AssignSimNode(simzonegroup);

                var manager = GameObject.FindObjectOfType<SimRoadNetworkManager>();

                // 重複しないようにIDを生成
                var indexes = manager.TrafficFocusGroups.Select(x => int.Parse(x.ID.Substring("ZoneGroup".Length))).ToList();

                var newID = indexes.Count > 0 ? indexes.Max() + 1 : 0;

                simzonegroup.ID = "ZoneGroup" + newID;

                manager.TrafficFocusGroups.Add(simzonegroup);
            }

            // プロビルダーメッシュに変換
            foreach (var convexHullMeshObject in convexHullMeshObjects)
            {
                ConvertMeshToPBMesh(convexHullMeshObject);
            }

            AutoGenerateTrafficFocusAreaIntrude();// AutoGenerateTrafficFocusAreaIntrude(links);

            // ゾーンの中間生成物を削除
            GameObject.DestroyImmediate(zoneChache);

            return convexHullMeshObjects;
        }

        public static void ManualGenerateSimZone(GameObject building)
        {
            // 表示オブジェクトと底面取得オブジェクトのペアを作成
            var bldgPairs = GetBuildingPairs();

            var zoneRoot = GameObject.FindObjectOfType<SimRoadNetworkManager>().gameObject;

            var convexHullMeshObjects = new List<GameObject>();

            // 引数の建物のペアからフットプリントを取得
            var pair = bldgPairs.First(pair => pair.building == building);

            var mesh = pair.footprint.GetComponent<MeshFilter>().sharedMesh;

            var convexHullMeshObject = CreateMeshObject(mesh, "TrafficFocusGroup", Color.blue);

            convexHullMeshObjects.Add(convexHullMeshObject);

            convexHullMeshObject.transform.SetParent(zoneRoot.transform);

            // ゾーングループを作成
            var simzonegroup = convexHullMeshObject.AddComponent<TrafficFocusGroup>();

            // ゾーンに属するノードを登録
            AssignSimNode(simzonegroup);

            var manager = GameObject.FindObjectOfType<SimRoadNetworkManager>();

            // TODO:IDの生成は要検討
            simzonegroup.ID = "ZoneGroup" + manager.TrafficFocusGroups.Count;

            manager.TrafficFocusGroups.Add(simzonegroup);

            // プロビルダーメッシュに変換
            ConvertMeshToPBMesh(convexHullMeshObject);
        }

        private static GameObject GetObjectFromRoot(string name)
        {
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (var rootObject in rootObjects)
            {
                if (rootObject.name == name)
                {
                    return rootObject;
                }
            }

            return new GameObject(name);
        }

        /// <summary>
        /// メッシュのジオメトリを結合する
        /// </summary>
        /// <param name="meshes"></param>
        /// <returns></returns>
        private static NetTopologySuite.Geometries.Geometry MergeMeshGeometries(List<Mesh> meshes)
        {
            var geometries = new List<NetTopologySuite.Geometries.Geometry>();
            var factory = new NetTopologySuite.Geometries.GeometryFactory();

            // 各メッシュを NetTopologySuite の Geometry に変換
            foreach (var mesh in meshes)
            {
                NetTopologySuite.Geometries.Geometry geometry = MeshToGeometry(mesh, factory);
                if (geometry != null)
                {
                    geometries.Add(geometry);
                }
            }

            // メッシュを結合
            return factory.BuildGeometry(geometries).Union();
        }

        /// <summary>
        /// バウンディングボックスのジオメトリを作成
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        private static NetTopologySuite.Geometries.Geometry CreateBoundingBoxGeometry(NetTopologySuite.Geometries.Geometry geometry)
        {
            var envelope = geometry.EnvelopeInternal;
            var coordinates = new[]
            {
                new NetTopologySuite.Geometries.Coordinate(envelope.MinX, envelope.MinY),
                new NetTopologySuite.Geometries.Coordinate(envelope.MinX, envelope.MaxY),
                new NetTopologySuite.Geometries.Coordinate(envelope.MaxX, envelope.MaxY),
                new NetTopologySuite.Geometries.Coordinate(envelope.MaxX, envelope.MinY),
                new NetTopologySuite.Geometries.Coordinate(envelope.MinX, envelope.MinY)
            };
            var factory = new NetTopologySuite.Geometries.GeometryFactory();
            var linearRing = factory.CreateLinearRing(coordinates);
            return factory.CreatePolygon(linearRing);
        }

        /// <summary>
        /// Mesh オブジェクトを作成する
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="name"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        private static GameObject CreateMeshObject(Mesh mesh, string name, Color color)
        {
            var gameObject = new GameObject(name);
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            var meshRenderer = gameObject.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh = mesh;

            meshRenderer.sharedMaterial = new Material(Shader.Find("HDRP/Lit"));
            meshRenderer.sharedMaterial.SetColor("_BaseColor", color);

            // Mesh非表示化
            // レンダリングモードを透明に設定
            meshRenderer.sharedMaterial.SetFloat("_SurfaceType", 1); // Transparent
            // マテリアルのBlending Modeを設定
            meshRenderer.sharedMaterial.SetFloat("_BlendMode", 0); // Alpha
            // アルファクリッピングを無効化
            meshRenderer.sharedMaterial.SetFloat("_AlphaClip", 0);
            // アルファ値を設定
            var fixColor = color;
            fixColor.a = 0.0f;
            meshRenderer.sharedMaterial.SetColor("_BaseColor", fixColor);

            return gameObject;
        }

        /// <summary>
        /// Mesh を Geometry に変換する
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        private static NetTopologySuite.Geometries.Geometry MeshToGeometry(Mesh mesh, NetTopologySuite.Geometries.GeometryFactory factory)
        {
            var vertices = mesh.vertices;
            var indices = mesh.triangles;
            var polygonList = new List<NetTopologySuite.Geometries.Polygon>();

            for (int i = 0; i < indices.Length; i += 3)
            {
                var p1 = new NetTopologySuite.Geometries.Coordinate(vertices[indices[i]].x, vertices[indices[i]].z);
                var p2 = new NetTopologySuite.Geometries.Coordinate(vertices[indices[i + 1]].x, vertices[indices[i + 1]].z);
                var p3 = new NetTopologySuite.Geometries.Coordinate(vertices[indices[i + 2]].x, vertices[indices[i + 2]].z);

                var coordinates = new[] { p1, p2, p3, p1 }; // 最後の p1 はポリゴンを閉じるため
                var linearRing = factory.CreateLinearRing(coordinates);
                var polygon = factory.CreatePolygon(linearRing);

                polygonList.Add(polygon);
            }

            if (polygonList.Count > 0)
            {
                var geo = factory.CreateMultiPolygon(polygonList.ToArray());

                // バッファ操作を使用してポリゴンを修正
                return geo.Buffer(0);
            }

            return null;
        }

        /// <summary>
        /// Geometry を Mesh に変換する
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        private static Mesh GeometryToMesh(NetTopologySuite.Geometries.Geometry geometry)
        {
            var mesh = new Mesh();
            var coordinates = new List<Vector3>();
            var triangles = new List<int>();
            var normals = new List<Vector3>();

            if (geometry is NetTopologySuite.Geometries.Polygon polygon)
            {
                AddPolygonVerticesAndTriangles(polygon, coordinates, triangles);
            }
            else if (geometry is NetTopologySuite.Geometries.MultiPolygon multiPolygon)
            {
                for (int i = 0; i < multiPolygon.NumGeometries; i++)
                {
                    AddPolygonVerticesAndTriangles((NetTopologySuite.Geometries.Polygon)multiPolygon.GetGeometryN(i), coordinates, triangles);
                }
            }

            for (int i = 0; i < coordinates.Count; i++)
            {
                normals.Add(Vector3.up);
            }

            mesh.vertices = coordinates.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();

            // メッシュの法線とUVを再計算
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();

            return mesh;
        }

        /// <summary>
        /// Polygon の頂点と三角形を追加
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="vertices"></param>
        /// <param name="triangles"></param>
        private static void AddPolygonVerticesAndTriangles(NetTopologySuite.Geometries.Polygon polygon, List<Vector3> vertices, List<int> triangles)
        {
            var tess = new LibTessDotNet.Double.Tess();

            AddRingToTess(polygon.ExteriorRing, tess);

            foreach (var interiorRing in polygon.InteriorRings)
            {
                AddRingToTess(interiorRing, tess);
            }

            tess.Tessellate(LibTessDotNet.Double.WindingRule.EvenOdd, LibTessDotNet.Double.ElementType.Polygons, 3);

            // Tessellate結果から頂点とインデックスを追加
            Dictionary<int, int> indexMap = new Dictionary<int, int>();
            for (int i = 0; i < tess.VertexCount; i++)
            {
                var pos = tess.Vertices[i].Position;
                Vector3 vertex = new Vector3((float)pos.X, 0, (float)pos.Y);

                indexMap[i] = vertices.Count;
                vertices.Add(vertex);
            }

            for (int i = 0; i < tess.ElementCount; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int index = tess.Elements[i * 3 + j];
                    triangles.Add(indexMap[index]);
                }
            }
        }

        /// <summary>
        /// Ring を Tess に追加
        /// </summary>
        /// <param name="ring"></param>
        /// <param name="tess"></param>
        private static void AddRingToTess(NetTopologySuite.Geometries.LineString ring, LibTessDotNet.Double.Tess tess)
        {
            var contour = new LibTessDotNet.Double.ContourVertex[ring.NumPoints];
            for (int i = 0; i < ring.NumPoints; i++)
            {
                var coord = ring.GetCoordinateN(i);
                contour[i].Position = new LibTessDotNet.Double.Vec3(coord.X, coord.Y, 0);
            }
            tess.AddContour(contour, LibTessDotNet.Double.ContourOrientation.Original);
        }

        /// <summary>
        /// メッシュから底面ジオメトリを作成
        /// </summary>
        /// <param name="lod0Mesh"></param>
        /// <returns></returns>
        public static NetTopologySuite.Geometries.Geometry CreateFootprintGeometryFromMesh(Mesh lod0Mesh)
        {
            var factory = new NetTopologySuite.Geometries.GeometryFactory();
            var coordinates = new List<NetTopologySuite.Geometries.Coordinate>();

            float minHeight = lod0Mesh.vertices.Min(v => v.y);
            var bottomVertices = lod0Mesh.vertices.Where(v => v.y == minHeight).ToArray();
            foreach (var vertex in bottomVertices)
            {
                coordinates.Add(new NetTopologySuite.Geometries.Coordinate(vertex.x, vertex.z));
            }
            coordinates.Add(coordinates[0]);// 閉じるために最初の頂点を再度追加
            var linearRing = factory.CreateLinearRing(coordinates.AsEnumerable().Reverse().ToArray());//法線が反転してしまうので逆向きにする
            return factory.CreatePolygon(linearRing);
        }

        /// <summary>
        /// 頂点の座標を反転
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        private static NetTopologySuite.Geometries.Coordinate[] ReverseCoordinates(NetTopologySuite.Geometries.Coordinate[] coordinates)
        {
            var reversed = new NetTopologySuite.Geometries.Coordinate[coordinates.Length];
            for (int i = 0; i < coordinates.Length; i++)
            {
                reversed[i] = coordinates[coordinates.Length - 1 - i];
            }
            return reversed;
        }

        /// <summary>
        /// ポリゴンの法線を反転
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        private static NetTopologySuite.Geometries.Geometry ReversePolygon(NetTopologySuite.Geometries.Polygon polygon)
        {
            var geometryFactory = polygon.Factory;

            var exteriorCoordinates = ReverseCoordinates(polygon.ExteriorRing.Coordinates);
            var exteriorRing = geometryFactory.CreateLinearRing(exteriorCoordinates);

            var interiorRings = new List<NetTopologySuite.Geometries.LinearRing>();
            for (int i = 0; i < polygon.NumInteriorRings; i++)
            {
                var interiorCoordinates = ReverseCoordinates(polygon.GetInteriorRingN(i).Coordinates);
                var interiorRing = geometryFactory.CreateLinearRing(interiorCoordinates);
                interiorRings.Add(interiorRing);
            }

            return geometryFactory.CreatePolygon(exteriorRing, interiorRings.ToArray());
        }

        /// <summary>
        /// 凸包のジオメトリを作成
        /// </summary>
        /// <param name="geometries"></param>
        /// <returns></returns>
        private static NetTopologySuite.Geometries.Geometry CreateConvexHullGeometry(List<NetTopologySuite.Geometries.Geometry> geometries)
        {
            var geometryCollection = new NetTopologySuite.Geometries.GeometryCollection(geometries.ToArray(), new NetTopologySuite.Geometries.GeometryFactory());
            var convexHull = new NetTopologySuite.Algorithm.ConvexHull(geometryCollection);
            var convexHullGeo = convexHull.GetConvexHull();
            //法線が反転してしまうので逆向きにする
            if (convexHullGeo is NetTopologySuite.Geometries.Polygon polygon)
            {
                return ReversePolygon(polygon);
            }
            return convexHullGeo;
        }

        /// <summary>
        /// 建物とfootprint取得先のペアを取得
        /// </summary>
        /// <returns></returns>
        private static List<(GameObject building, GameObject footprint)> GetBuildingPairs()
        {
            var allCityObjects = GameObject.FindObjectsOfType<PLATEAUCityObjectGroup>();
            var bldgs = allCityObjects.Select(obj => obj.gameObject).Where(obj => obj.name.StartsWith("bldg")).ToList();
            var pairs = new List<(GameObject building, GameObject footprint)>();

            foreach (var bldg in bldgs)
            {
                var footprintObject = bldg.transform.parent.name == "LOD0" ? bldg : bldg.transform.parent?.parent?.Find("LOD0")?.Find(bldg.name)?.gameObject;

                //var footprintGeo = CreateFootprintGeometryFromMesh(footprintObject.GetComponent<MeshFilter>().sharedMesh);

                pairs.Add(new(bldg, footprintObject));
            }

            return pairs;
        }

        /// <summary>
        /// 街区ごとの建物とfootprint取得先のペアを取得
        /// </summary>
        /// <param name="bldgPairs"></param>
        /// <param name="groupGeos"></param>
        /// <returns></returns>
        private static Dictionary<NetTopologySuite.Geometries.Geometry, List<(GameObject building, GameObject footprint)>> GetBlockDictionary(List<(GameObject building, GameObject footprint)> bldgPairs, List<NetTopologySuite.Geometries.Geometry> groupGeos)
        {
            Dictionary<NetTopologySuite.Geometries.Geometry, List<(GameObject building, GameObject footprint)>> blocks = new Dictionary<NetTopologySuite.Geometries.Geometry, List<(GameObject building, GameObject footprint)>>();

            foreach (var bldg in bldgPairs)
            {
                foreach (var groupGeo in groupGeos)
                {
                    var footprintGeo = CreateFootprintGeometryFromMesh(bldg.footprint.GetComponent<MeshFilter>().sharedMesh);

                    if (footprintGeo.Within(groupGeo))
                    {
                        if (!blocks.ContainsKey(groupGeo))
                        {
                            blocks[groupGeo] = new List<(GameObject building, GameObject footprint)>();
                        }
                        blocks[groupGeo].Add(bldg);
                    }
                }
            }

            return blocks;
        }

        /// <summary>
        /// MeshをProBuilderMeshに変換
        /// </summary>
        /// <param name="gameObject"></param>
        private static void ConvertMeshToPBMesh(GameObject gameObject)
        {
            var mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;

            if (mesh == null)
            {
                return;
            }

            UnityEngine.ProBuilder.ProBuilderMesh pbMesh = gameObject.GetComponent<UnityEngine.ProBuilder.ProBuilderMesh>();

            if (pbMesh == null)
            {
                pbMesh = gameObject.AddComponent<UnityEngine.ProBuilder.ProBuilderMesh>();
            }

            pbMesh.RebuildWithPositionsAndFaces(mesh.vertices, new UnityEngine.ProBuilder.Face[] { new UnityEngine.ProBuilder.Face(mesh.triangles) });
        }

        /// <summary>
        /// SimZoneの再構築
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="mesh"></param>
        private static void ReBuildSimZone(TrafficFocusGroup zone)
        {
            if (zone.BuildingPairs.Count == 0)
            {
                return;
            }

            // フットプリントのジオメトリを取得
            List<NetTopologySuite.Geometries.Geometry> footprintGeos = new List<NetTopologySuite.Geometries.Geometry>();

            foreach (var pair in zone.BuildingPairs)
            {
                var footprintGeo = CreateFootprintGeometryFromMesh(pair.FootprintBuilding.GetComponent<MeshFilter>().sharedMesh);

                footprintGeos.Add(footprintGeo);
            }

            // ゾーンジオメトリを再計算
            var convexHullGeo = CreateConvexHullGeometry(footprintGeos);
            var convexHullMesh = GeometryToMesh(convexHullGeo);

            zone.gameObject.GetComponent<MeshFilter>().sharedMesh = convexHullMesh;

            // プロビルダーメッシュに変換
            ConvertMeshToPBMesh(zone.gameObject);
        }

        /// <summary>
        /// SimZoneGroup同士をマージ
        /// </summary>
        /// <param name="groups"></param>
        /// <returns></returns>
        public static TrafficFocusGroup Merge(List<TrafficFocusGroup> groups)
        {
            if (groups.Count == 0) return null;

            var parent = groups[0].transform.parent;

            List<NetTopologySuite.Geometries.Geometry> footprintGeos = new List<NetTopologySuite.Geometries.Geometry>();

            foreach (var group in groups)
            {
                foreach (var buildingPair in group.BuildingPairs)
                {
                    var footprintGeo = CreateFootprintGeometryFromMesh(buildingPair.FootprintBuilding.GetComponent<MeshFilter>().sharedMesh);

                    footprintGeos.Add(footprintGeo);
                }
            }

            // ジオメトリを再計算
            var convexHullGeo = CreateConvexHullGeometry(footprintGeos);
            var convexHullMesh = GeometryToMesh(convexHullGeo);
            var convexHullMeshObject = CreateMeshObject(convexHullMesh, "ConvexHullMesh m", Color.blue);

            // プロビルダーメッシュに変換
            ConvertMeshToPBMesh(convexHullMeshObject);

            convexHullMeshObject.transform.SetParent(parent);

            var simZoneGroup = convexHullMeshObject.AddComponent<TrafficFocusGroup>();

            var manager = GameObject.FindObjectOfType<SimRoadNetworkManager>();

            foreach (var group in groups)
            {
                foreach (var buildingPair in group.BuildingPairs)
                {
                    simZoneGroup.AddBuilding(buildingPair.Building, buildingPair.FootprintBuilding);
                }

                manager.TrafficFocusGroups.Remove(group);

                GameObject.DestroyImmediate(group.gameObject);
            }

            manager.TrafficFocusGroups.Add(simZoneGroup);

            return simZoneGroup;
        }

        /// <summary>
        /// ゾーンから建物を削除
        /// </summary>
        /// <param name="zones"></param>
        public static void RemoveFromZone(TrafficFocusGroup zone, List<GameObject> objects)
        {
            foreach (var obj in objects)
            {
                zone.BuildingPairs.RemoveAll(pair => pair.Building == obj);
            }

            ReBuildSimZone(zone);
        }

        /// <summary>
        /// ゾーンに建物を追加
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="objects"></param>
        public static void AddToZone(TrafficFocusGroup zone, List<GameObject> objects)
        {
            foreach (var obj in objects)
            {
                var footprint = obj.transform.parent.name == "LOD0" ? obj : obj.transform.parent?.parent?.Find("LOD0")?.Find(obj.name)?.gameObject;

                if (footprint == null)
                {
                    continue;
                }

                zone.AddBuilding(obj, footprint);
            }

            ReBuildSimZone(zone);
        }

        /// <summary>
        /// ゾーンを手動追加（交通集中発生点）
        /// </summary>
        /// <param name="center"></param>
        public static SimZone ManualGenerateSimZone(in Vector3 center)
        {
            var rootObject = GameObject.FindObjectOfType<SimRoadNetworkManager>().gameObject;

            var newCenter = new Vector3(center.x, 0, center.z);
            var circleGeo = CreateCircleGeometry(newCenter);
            var circleMesh = GeometryToMesh(circleGeo);
            var circleMeshObject = CreateMeshObject(circleMesh, "TrafficFocusArea", Color.red);
            circleMeshObject.transform.SetParent(rootObject.transform);

            // ゾーンを作成
            var simZoneGroup = circleMeshObject.AddComponent<TrafficFocusArea>();

            // ゾーン内のノードを除外しない
            simZoneGroup.ExcludeNodes = false;

            AssignSimNode(simZoneGroup);

            // プロビルダーメッシュに変換
            ConvertMeshToPBMesh(circleMeshObject);

            return simZoneGroup;
        }

        /// <summary>
        /// 円のジオメトリを作成
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="numPoints"></param>
        /// <returns></returns>
        private static NetTopologySuite.Geometries.Geometry CreateCircleGeometry(in Vector3 center, double radius = 10, int numPoints = 16)
        {
            // 円の頂点を格納するリスト
            List<NetTopologySuite.Geometries.Coordinate> coordinates = new List<NetTopologySuite.Geometries.Coordinate>();

            // 円を16分割して各点の座標を計算
            for (int i = 0; i < numPoints; i++)
            {
                double angle = 2 * Mathf.PI * i / numPoints;
                double x = center.x + radius * Mathf.Cos((float)angle);
                double y = center.z + radius * Mathf.Sin((float)angle);
                coordinates.Add(new NetTopologySuite.Geometries.Coordinate(x, y));
            }

            // 最初の点を最後にもう一度追加して閉じる
            coordinates.Add(coordinates[0]);

            // リニアリングを作成し、それを使ってポリゴンを作成
            NetTopologySuite.Geometries.LinearRing ring = new NetTopologySuite.Geometries.LinearRing(coordinates.AsEnumerable().Reverse().ToArray());//法線が反転してしまうので逆向きにする
            NetTopologySuite.Geometries.Polygon circlePolygon = new NetTopologySuite.Geometries.Polygon(ring);

            return circlePolygon;
        }

        /// <summary>
        /// ゾーンに紐づくノードを割り当て
        /// </summary>
        /// <param name="simZoneGroup"></param>
        private static void AssignSimNode(SimZone simZone)
        {
            var manager = GameObject.FindObjectOfType<SimRoadNetworkManager>();

            var nodes = manager.SimRoadNetworkNodes;

            var zoneMesh = simZone.GetComponent<MeshFilter>().sharedMesh;
            var zoneGeo = MeshToGeometry(zoneMesh, new NetTopologySuite.Geometries.GeometryFactory());

            foreach (var node in nodes)
            {
                if (node.IsVirtual)
                {
                    continue;
                }

                var nodeMesh = node.Mesh;

                if (nodeMesh == null)
                {
                    continue;
                }

                var center = nodeMesh.bounds.center;

                var point = new NetTopologySuite.Geometries.Point(center.x, center.z);

                var distance = zoneGeo.Distance(point);

                if (distance < AssignDistance)
                {
                    if (!simZone.SimRoadNetworkNodes.Contains(node))
                    {
                        simZone.SimRoadNetworkNodes.Add(node);
                    }
                }
            }
        }

        /// <summary>
        /// エリア外交通集中発生点を自動生成
        /// </summary>
        private static void AutoGenerateTrafficFocusAreaIntrude()
        {
            var roadNetworkManager = GameObject.FindObjectOfType<SimRoadNetworkManager>();

            roadNetworkManager.SimRoadNetworkNodes.Where(node => node.IsVirtual).ToList().ForEach(node =>
            {
                var center = node.Coord;

                var zone = ManualGenerateSimZone(center);

                zone.SimRoadNetworkNodes.Add(node);

                var focusArea = zone as TrafficFocusArea;

                focusArea.AreaType = TrafficFocusArea.AreaTypes.Intrude;

                focusArea.ID = "ZonePoint" + roadNetworkManager.TrafficFocusAreas.Count;

                roadNetworkManager.TrafficFocusAreas.Add(focusArea);
            });
        }

        /// <summary>
        /// メッシュの中心座標を計算
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        private static Vector3 CalculateMeshCenter(Mesh mesh)
        {
            var bounds = mesh.bounds;
            return new Vector3(bounds.center.x, 0, bounds.center.z);
        }

        public static Vector3 CalculateCenter(SimRoadNetworkManager roadNetworkManager, List<RnID<RnDataLane>> lanes, bool isNext)
        {
            var allLanes = roadNetworkManager.RoadNetworkDataGetter.GetLanes();

            var allWays = roadNetworkManager.RoadNetworkDataGetter.GetWays();

            var allLineStrings = roadNetworkManager.RoadNetworkDataGetter.GetLineStrings();

            var allPoints = roadNetworkManager.RoadNetworkDataGetter.GetPoints();

            List<Vector3> borderVertices = new List<Vector3>();

            foreach (var lane in lanes)
            {
                var border = isNext ? allLanes[lane.ID].NextBorder : allLanes[lane.ID].PrevBorder;

                var way = allWays[border.ID];

                var lineString = allLineStrings[way.LineString.ID];

                foreach (var point in lineString.Points)
                {
                    borderVertices.Add(allPoints[point.ID].Vertex);
                }
            }

            return CalculateCenter(borderVertices);
        }

        public static Vector3 CalculateCenter(List<Vector3> positions)
        {
            if (positions == null || positions.Count == 0)
                return Vector3.zero; // リストが空の場合、ゼロベクトルを返す

            Vector3 sum = Vector3.zero;

            // 全てのベクトルを合計
            foreach (Vector3 pos in positions)
            {
                sum += pos;
            }

            // 平均値を求める
            return sum / positions.Count;
        }

        public static List<(GameObject building, GameObject footprint)> GetAllBuildingPairs()
        {
            return GetBuildingPairs();
        }
    }
}