using NetTopologySuite.Algorithm;
using PLATEAU.Native;
using PLATEAU.RoadNetwork.Data;
using PLATEAU.RoadNetwork.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using TrafficSimulationTool;
using TrafficSimulationTool.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TrafficSimulationTool.Editor
{
    /// <summary>
    /// 交通集中発生点設定タブのコントローラ
    /// </summary>
    public class PointViewController : ViewController
    {
        private SimRoadNetworkManager roadNetworkManager;

        private Texture2D zoneIconTexture;

        private Texture2D handleTextureNode;

        private Texture2D handleTextureAssigned;

        private TrafficFocusArea.AreaTypes areaType;

        private int tabSelected = 0;

        // ゾーンの配色
        private static readonly Color colorZone = new Color(0.0f, 0.0f, 1.0f, 0.7f);

        private static readonly Color colorZoneSelect = new Color(1.0f, 1.0f, 0.4f, 0.7f);

        private static readonly Color colorTrafficArea = new Color(1.0f, 0.0f, 0.5f, 0.7f);

        // アウトラインの配色
        private static readonly Color colorOutLineAssigned = new Color(1.0f, 0.8f, 1.0f, 1.0f);

        private static readonly Color colorOutLineUnassigned = new Color(0.8f, 1.0f, 1.0f, 1.0f);

        // 建物の配色
        private static readonly Color colorBuildingAssigned = new Color(0.0f, 0.6f, 0.3f, 0.4f);

        private static readonly Color colorBuildingUnassigned = new Color(0.4f, 0.8f, 0.4f, 0.4f);

        private static readonly Vector2 iconSize = new Vector2(32f, 32f);

        public PointViewController(VisualElement element, TrafficSimulationToolWindow parent) : base(element, parent)
        {
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected override void Initialize()
        {
            // ゾーン自動生成

            rootElement.Q<VisualElement>("ButtonAutoGenerate").RegisterCallback<ClickEvent>(evt =>
            {
                GenerateSimRoadNetwork();

                roadNetworkManager = GameObject.FindFirstObjectByType<SimRoadNetworkManager>();

                var a = roadNetworkManager as SimRoadNetworkManager;

                SimZoneUtility.AutoGenerateSimZone(roadNetworkManager.SimRoadNetworkNodes, roadNetworkManager.SimRoadNetworkLinks);
            });

            // ゾーン設定

            rootElement.Q<VisualElement>("ButtonZoneEditCombine").RegisterCallback<ClickEvent>(evt =>
            {
                var zone = SimZoneUtility.Merge(GetSelectedObjectsByComponent<TrafficFocusGroup>().Select(go => go.GetComponent<TrafficFocusGroup>()).ToList());
            });

            rootElement.Q<VisualElement>("ButtonZoneEditAdd").RegisterCallback<ClickEvent>(evt =>
            {
                SceneView.duringSceneGui -= OnSelectZoneGUI;

                SceneView.duringSceneGui -= OnAddZoneGUI;
                SceneView.duringSceneGui += OnAddZoneGUI;
            });

            rootElement.Q<VisualElement>("ButtonZoneEditRemove").RegisterCallback<ClickEvent>(evt =>
            {
                if (GetSelectedObjectsByComponent<TrafficFocusGroup>().Count == 1)
                {
                    var group = GetSelectedObjectsByComponent<TrafficFocusGroup>().First();

                    roadNetworkManager.TrafficFocusGroups.Remove(group.GetComponent<TrafficFocusGroup>());

                    GameObject.DestroyImmediate(group.gameObject);
                }
            });

            //　アイコンをロード
            if (zoneIconTexture == null)
            {
                zoneIconTexture = TrafficSimulationToolUtility.GetAssetRelativePath<Texture2D>("Images/Icon/handle_zone.png");
            }
            if (handleTextureNode == null)
            {
                handleTextureNode = TrafficSimulationToolUtility.GetAssetRelativePath<Texture2D>("Images/Icon/handle_node.png");
            }
            if (handleTextureAssigned == null)
            {
                handleTextureAssigned = TrafficSimulationToolUtility.GetAssetRelativePath<Texture2D>("Images/Icon/handle_node_assigned.png");
            }
        }

        public override void OnEnable()
        {
            if (roadNetworkManager == null)
            {
                roadNetworkManager = GameObject.FindObjectOfType<SimRoadNetworkManager>();
            }

            SceneView.duringSceneGui -= OnSelectZoneGUI;
            SceneView.duringSceneGui += OnSelectZoneGUI;
        }

        public override void OnDisable()
        {
            SceneView.duringSceneGui -= OnSelectZoneGUI;
        }

        private void GenerateSimRoadNetwork()
        {
            if (roadNetworkManager != null)
            {
                GameObject.DestroyImmediate(roadNetworkManager.gameObject);
            }

            var roadNetworkManagerGameObject = new GameObject("SimRoadNetwork");

            roadNetworkManager = roadNetworkManagerGameObject.AddComponent<SimRoadNetworkManager>();

            roadNetworkManagerGameObject.AddComponent<SimRoadNetworkDebugDrawer>();

            var roadNetwork = GameObject.FindObjectOfType<PLATEAURnStructureModel>();

            var roadNetworkGetter = roadNetwork.GetRoadNetworkDataGetter();

            var roadNetworkRoads = roadNetworkGetter.GetRoadBases();

            var roadNetworkNodes = roadNetworkRoads.OfType<RnDataIntersection>().ToList();

            var roadNetworkLinks = roadNetworkRoads.OfType<RnDataRoad>().ToList();

            // ノードの生成

            var simRoadNetworkNodes = new List<SimRoadNetworkNode>();

            foreach (var road in roadNetworkRoads.Select((value, index) => new { value, index }))
            {
                var node = road.value as RnDataIntersection;

                if (node == null)
                {
                    continue;
                }

                simRoadNetworkNodes.Add(new SimRoadNetworkNode(simRoadNetworkNodes.Count.ToString(), road.index));
            }

            roadNetworkManager.SimRoadNetworkNodes = simRoadNetworkNodes;

            // リンクの生成

            var simRoadNetworkLinks = new List<SimRoadNetworkLink>();

            Dictionary<int, SimRoadNetworkNode> vNodes = new Dictionary<int, SimRoadNetworkNode>();

            foreach (var road in roadNetworkRoads.Select((value, index) => new { value, index }))
            {
                var link = road.value as RnDataRoad;

                if (link == null)
                {
                    continue;
                }

                // リンクが接続されていない場合はスキップ
                if (!link.Next.IsValid && !link.Prev.IsValid)
                {
                    Debug.LogWarning("Link is not connected to any node.");

                    continue;
                }

                var next = link.Next.IsValid ? roadNetworkRoads[link.Next.ID] as RnDataIntersection : null;
                var prev = link.Prev.IsValid ? roadNetworkRoads[link.Prev.ID] as RnDataIntersection : null;

                //　仮想ノードが割り当てられている場合はそのノードを取得
                SimRoadNetworkNode vNext = vNodes.ContainsKey(link.Next.ID) ? vNodes[link.Next.ID] : null;
                SimRoadNetworkNode vPrev = vNodes.ContainsKey(link.Prev.ID) ? vNodes[link.Prev.ID] : null;

                // FIXME: for PoC
                //if (link.index == 85)
                //{
                //    //prev = roadNetworkRoads[45] as RnDataIntersection;
                //    vPrev = simRoadNetworkNodes.Find(x => x.ID == SimRoadNetworkNode.IDPrefix + 45);
                //}
                //if (link.index == 72)
                //{
                //    //next = roadNetworkRoads[45] as RnDataIntersection;
                //    vNext = simRoadNetworkNodes.Find(x => x.ID == SimRoadNetworkNode.IDPrefix + 45);
                //}

                // 接続先がリンクかつ仮想ノードが割り当てられていない場合は仮想ノードを生成
                // 接続点ノードが割り当てられていない場合への対処
                if (next == null && link.Next.IsValid && roadNetworkRoads[link.Next.ID] as RnDataRoad != null && vNext == null)
                {
                    vNext = new SimRoadNetworkNode(simRoadNetworkNodes.Count.ToString(), -1);

                    simRoadNetworkNodes.Add(vNext);

                    vNodes.Add(road.index, vNext);
                }
                if (prev == null && link.Prev.IsValid && roadNetworkRoads[link.Prev.ID] as RnDataRoad != null && vPrev == null)
                {
                    vPrev = new SimRoadNetworkNode(simRoadNetworkNodes.Count.ToString(), -1);

                    simRoadNetworkNodes.Add(vPrev);

                    vNodes.Add(road.index, vPrev);
                }

                // 終端の仮想ノードを生成
                if (next == null && vNext == null)
                {
                    vNext = new SimRoadNetworkNode(simRoadNetworkNodes.Count.ToString(), -1);

                    simRoadNetworkNodes.Add(vNext);
                }
                if (prev == null && vPrev == null)
                {
                    vPrev = new SimRoadNetworkNode(simRoadNetworkNodes.Count.ToString(), -1);

                    simRoadNetworkNodes.Add(vPrev);
                }

                var simNodeNext = next != null ? simRoadNetworkNodes.Find(x => x.OriginNode == next) : vNext;
                var simNodePrev = prev != null ? simRoadNetworkNodes.Find(x => x.OriginNode == prev) : vPrev;

                var simNodeNextID = simNodeNext.ID.Replace(SimRoadNetworkNode.IDPrefix, "");
                var simNodePrevID = simNodePrev.ID.Replace(SimRoadNetworkNode.IDPrefix, "");

                // リンクの生成
                var simLinkL = new SimRoadNetworkLink(road.index + "_" + simNodePrevID + "_" + simNodeNextID, road.index);
                var simLinkR = new SimRoadNetworkLink(road.index + "_" + simNodeNextID + "_" + simNodePrevID, road.index);

                simLinkL.IsReverse = false;
                simLinkR.IsReverse = true;

                simLinkL.UpNode = simNodePrev;
                simLinkL.DownNode = simNodeNext;

                simLinkR.UpNode = simNodeNext;
                simLinkR.DownNode = simNodePrev;

                simLinkL.Pair = simLinkR;
                simLinkR.Pair = simLinkL;

                simRoadNetworkLinks.Add(simLinkL);
                simRoadNetworkLinks.Add(simLinkR);
            }

            roadNetworkManager.SimRoadNetworkLinks = simRoadNetworkLinks;

            foreach (var link in roadNetworkManager.SimRoadNetworkLinks)
            {
                link.GenerateLane();
            }

            // トラックの生成（マネージャにリンクが登録されている必要あり）
            foreach (var node in roadNetworkManager.SimRoadNetworkNodes)
            {
                node.GenerateTrack();
            }

            // 接続点ノード・仮想ノードの座標を設定
            foreach (var node in roadNetworkManager.SimRoadNetworkNodes)
            {
                if (node.OriginTran == null)
                {
                    // 接続されているリンクの隣接情報から座標を取得する
                    // OriginTranがない時点で交差点ではなく接続点なので上流・下流ともに１つでいい
                    var uplink = roadNetworkManager.SimRoadNetworkLinks.Where(x => x.DownNode == node && x.GetOriginLanes().Count > 0);
                    var downlink = roadNetworkManager.SimRoadNetworkLinks.Where(x => x.UpNode == node && x.GetOriginLanes().Count > 0);

                    // uplinksの下流ボーダーとdownlinksの上流ボーダーの中点を取得
                    List<Vector3> points = new List<Vector3>();

                    if (uplink.Count() > 0)
                    {
                        points.Add(SimZoneUtility.CalculateCenter(roadNetworkManager, uplink.First().GetOriginLanes(), true));
                    }
                    if (downlink.Count() > 0)
                    {
                        points.Add(SimZoneUtility.CalculateCenter(roadNetworkManager, downlink.First().GetOriginLanes(), false));
                    }

                    node.Coord = SimZoneUtility.CalculateCenter(points);
                }
            }

            // FIXME: for PoC
            //var vDestination = new SimRoadNetworkNode(simRoadNetworkNodes.Count.ToString(), -1);
            //var vDestination = new SimRoadNetworkNode(99.ToString(), -1);

            //var vDestinationGeo = roadNetworkManager.GeoReference.Project(new GeoCoordinate(35.62604921041615569, 139.78095943169893189, 0));

            //vDestination.Coord = new Vector3((float)vDestinationGeo.X, (float)vDestinationGeo.Y, (float)vDestinationGeo.Z);

            //simRoadNetworkNodes.Add(vDestination);
        }

        private List<GameObject> GetSelectedObjectsByComponent<T>() where T : Component
        {
            return Selection.objects
                .OfType<GameObject>()
                .Where(go => go.GetComponent<T>() != null)
                .ToList();
        }

        /// <summary>
        /// ハンドルのGUI描画
        /// </summary>
        /// <param name="sceneView"></param>
        private void OnSelectZoneGUI(SceneView sceneView)
        {
            if (roadNetworkManager == null)
            {
                return;
            }

            // 操作が競合するため、ProBuilderウィンドウが開いている場合は警告を表示
            if (IsOpenProBuilderWindow())
            {
                Handles.BeginGUI();

                Rect rect = sceneView.camera.pixelRect;

                // 左下の位置に表示
                float windowWidth = 200;
                float windowHeight = 40;
                float xPos = 10;
                float yPos = rect.height - windowHeight - 10;

                // 警告ウィンドウを左下に表示
                GUILayout.Window(0, new Rect(xPos, yPos, windowWidth, windowHeight), (id) =>
                {
                    GUILayout.Label("ゾーン編集に戻るにはProBuilderを終了してください");
                }, "TrafficSimulationTool");

                Handles.EndGUI();

                return;
            }

            Event e = Event.current;

            UnityEditor.HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            //var zones = roadNetworkManager.TrafficFocusGroups;

            // ゾーンの描画
            // 描画順の関係で２回にわけて描画する
            // １回目：ゾーンのメッシュを描画
            foreach (var zone in roadNetworkManager.TrafficFocusGroups)
            {
                if (zone == null)
                {
                    continue;
                }

                if (zone.gameObject == null)
                {
                    continue;
                }

                MeshFilter meshFilter = zone.gameObject.GetComponent<MeshFilter>();
                Mesh mesh = meshFilter.sharedMesh;

                var center = zone.transform.position;
                var quaternion = zone.transform.rotation;
                var size = 1.0f;

                var isSelect = Selection.gameObjects.Contains(zone.gameObject);

                Handles.color = isSelect ? colorZoneSelect : colorZone;

                CustomHandleMesh = mesh;

                switch (e.type)
                {
                    case EventType.Repaint:
                        Graphics.DrawMeshNow(CustomHandleMesh, StartCapDraw(center, quaternion, size));
                        break;
                }
            }

            // エリア外交通集中発生点の描画
            // 編集しないのでインタラクション不要

            var areas = roadNetworkManager.TrafficFocusAreas;

            foreach (var area in areas)
            {
                if (area == null)
                {
                    continue;
                }

                if (area.gameObject == null)
                {
                    continue;
                }

                MeshFilter meshFilter = area.gameObject.GetComponent<MeshFilter>();
                Mesh mesh = meshFilter.sharedMesh;

                var center = area.transform.position;
                var quaternion = area.transform.rotation;
                var size = 1.0f;

                Handles.color = colorTrafficArea;

                CustomHandleMesh = mesh;

                switch (e.type)
                {
                    case EventType.Repaint:
                        Graphics.DrawMeshNow(CustomHandleMesh, StartCapDraw(center, quaternion, size));
                        break;
                }
            }

            // ゾーンが一つだけ選択されている場合
            if (GetSelectedObjectsByComponent<TrafficFocusGroup>().Count == 1)
            {
                // ゾーンに紐づけるノードを描画
                foreach (var node in roadNetworkManager.SimRoadNetworkNodes)
                {
                    if (node.IsVirtual)
                    {
                        continue;
                    }

                    var mesh = node.Mesh;

                    if (mesh == null)
                    {
                        continue;
                    }

                    var zone = Selection.activeGameObject.GetComponent<TrafficFocusGroup>();
                    var isAssinged = zone.SimRoadNetworkNodes.Any(x => x.RoadNetworkIndex == node.RoadNetworkIndex);

                    Handles.color = isAssinged ? colorOutLineAssigned : colorOutLineUnassigned;

                    CustomHandleMesh = mesh;

                    var iconCenter = mesh.bounds.center;

                    if (Handles.Button(iconCenter, Quaternion.identity, iconSize.x, iconSize.y, (id, pos, rot, s, e) => CustomIconHandleCap(id, pos, rot, s, e, isAssinged ? handleTextureAssigned : handleTextureNode)))
                    {
                        if (isAssinged)
                        {
                            zone.SimRoadNetworkNodes.RemoveAll(n => n.RoadNetworkIndex == node.RoadNetworkIndex);
                        }
                        else
                        {
                            // ゾーンにノードを追加
                            zone.SimRoadNetworkNodes.Add(node);
                        }
                    }

                    // アウトラインを描画
                    if (Event.current.type == EventType.Repaint)
                    {
                        Handles.DrawOutline(new GameObject[] { node.OriginTran.gameObject }, Handles.color);
                    }
                }

                // ゾーンに紐づける建物を描画
                var cityObjects = SimZoneUtility.GetAllBuildingPairs();

                foreach (var cityObject in cityObjects)
                {
                    var gameObject = cityObject.footprint;
                    var meshFilter = gameObject.GetComponent<MeshFilter>();
                    var mesh = meshFilter.sharedMesh;

                    var center = cityObject.building.transform.position;
                    var quaternion = cityObject.building.transform.rotation;
                    var size = 1.0f;

                    var zone = Selection.activeGameObject.GetComponent<TrafficFocusGroup>();
                    var isAssinged = zone.BuildingPairs.Any(pair => pair.Building == cityObject.building);

                    Handles.color = isAssinged ? colorBuildingAssigned : colorBuildingUnassigned;
                    CustomHandleMesh = mesh;
                    if (Handles.Button(Vector3.zero, quaternion, size, size, CustomMeshHandleCap))
                    {
                        if (isAssinged)
                        {
                            SimZoneUtility.RemoveFromZone(zone, new List<GameObject>() { cityObject.building });
                        }
                        else
                        {
                            // 先に他のゾーンから選択された建物を削除
                            var groups = GameObject.FindObjectsOfType<TrafficFocusGroup>();

                            foreach (var group in groups)
                            {
                                SimZoneUtility.RemoveFromZone(group, new List<GameObject>() { cityObject.building });
                            }

                            // ゾーンに建物を追加
                            SimZoneUtility.AddToZone(zone, new List<GameObject>() { cityObject.building });
                        }
                    }
                }
            }

            // ゾーンの描画
            // 描画順の関係で２回にわけて描画する
            // ２回目：ゾーンのアイコンを描画
            foreach (var zone in roadNetworkManager.TrafficFocusGroups)
            {
                if (zone == null)
                {
                    continue;
                }

                if (zone.gameObject == null)
                {
                    continue;
                }

                MeshFilter meshFilter = zone.gameObject.GetComponent<MeshFilter>();
                Mesh mesh = meshFilter.sharedMesh;

                var isSelect = Selection.gameObjects.Contains(zone.gameObject);

                Handles.color = isSelect ? colorZoneSelect : colorZone;

                CustomHandleMesh = mesh;

                var iconCenter = mesh.bounds.center;

                // ハンドルの描画＆クリック判定
                if (Handles.Button(iconCenter, Quaternion.identity, iconSize.x, iconSize.y, (id, pos, rot, s, e) => CustomIconHandleCap(id, pos, rot, s, e, zoneIconTexture)))
                {
                    if (e.control || e.command)
                    {
                        var objects = Selection.gameObjects.ToList();

                        if (objects.Contains(zone.gameObject))
                        {
                            objects.Remove(zone.gameObject);

                            Selection.objects = objects.ToArray();
                        }
                        else
                        {
                            objects.Add(zone.gameObject);
                        }
                        Selection.objects = objects.ToArray();
                    }
                    else
                    {
                        Selection.activeGameObject = zone.gameObject;
                    }
                }
            }
        }

        private void OnAddZoneGUI(SceneView sceneView)
        {
            Event e = Event.current;

            UnityEditor.HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            var ignores = new List<SimRoadNetworkLink>();

            // 建物が選択された場合に建物の LOD0 でゾーンを生成
            var cityObjects = SimZoneUtility.GetAllBuildingPairs();

            foreach (var cityObject in cityObjects)
            {
                var gameObject = cityObject.footprint;
                var meshFilter = gameObject.GetComponent<MeshFilter>();
                var mesh = meshFilter.sharedMesh;

                var center = cityObject.building.transform.position;
                var quaternion = cityObject.building.transform.rotation;
                var size = 1.0f;

                Handles.color = colorBuildingUnassigned;

                CustomHandleMesh = mesh;

                if (Handles.Button(Vector3.zero, quaternion, size, size, CustomMeshHandleCap))
                {
                    // 先に他のゾーンから選択された建物を削除
                    var groups = GameObject.FindObjectsOfType<TrafficFocusGroup>();

                    foreach (var group in groups)
                    {
                        SimZoneUtility.RemoveFromZone(group, new List<GameObject>() { cityObject.building });
                    }

                    SimZoneUtility.ManualGenerateSimZone(cityObject.building);

                    // ゾーン追加モードを終了
                    SceneView.duringSceneGui -= OnAddZoneGUI;
                    SceneView.duringSceneGui += OnSelectZoneGUI;
                }
            }

            if (e.type == EventType.MouseDown)
            {
                switch (e.button)
                {
                    case 1:
                        // ゾーン追加モードを終了
                        SceneView.duringSceneGui -= OnAddZoneGUI;
                        SceneView.duringSceneGui += OnSelectZoneGUI;
                        break;
                }
            }
        }

        private static bool IsOpenProBuilderWindow()
        {
            return IsWindowOpen("ProBuilder");
        }

        private static bool IsWindowOpen(string windowName)
        {
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window.titleContent.text == windowName)
                    return true;
            }
            return false;
        }

        //private void CustomIconHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType, Texture2D icon, Mesh mesh, Vector3 positionMesh, Quaternion rotationMesh, float sizeMesh)
        //{
        //    switch (eventType)
        //    {
        //        case EventType.MouseMove:
        //        case EventType.Layout:
        //            UnityEditor.HandleUtility.AddControl(controlID, UnityEditor.HandleUtility.DistanceToCircle(position, size * 0.5f));
        //            break;

        //        case EventType.Repaint:

        //            Graphics.DrawMeshNow(mesh, StartCapDraw(positionMesh, rotationMesh, sizeMesh));

        //            DrawIcon(position, icon, size);

        //            break;
        //    }
        //}

        /// <summary>
        /// カスタムアイコンハンドルの描画
        /// </summary>
        /// <param name="controlID"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="size"></param>
        /// <param name="eventType"></param>
        /// <param name="icon"></param>
        private void CustomIconHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType, Texture2D icon)
        {
            switch (eventType)
            {
                case EventType.MouseMove:
                case EventType.Layout:
                    UnityEditor.HandleUtility.AddControl(controlID, UnityEditor.HandleUtility.DistanceToCircle(position, size * 0.5f));
                    break;

                case EventType.Repaint:
                    DrawIcon(position, icon, size);
                    break;
            }
        }

        /// <summary>
        /// シーンビューに2Dアイコンを描画する
        /// </summary>
        /// <param name="position">アイコンの位置</param>
        /// <param name="icon">アイコンのテクスチャ</param>
        /// <param name="size">アイコンのサイズ</param>
        private void DrawIcon(Vector3 position, Texture2D icon, float size)
        {
            // ワールド座標をスクリーン座標に変換
            Vector2 guiPoint = UnityEditor.HandleUtility.WorldToGUIPoint(position);

            // アイコンの描画位置とサイズを設定
            Rect iconRect = new Rect(guiPoint.x - size * 0.5f, guiPoint.y - size * 0.5f, size, size);

            // アイコンを描画
            Handles.BeginGUI();
            GUI.DrawTexture(iconRect, icon);
            Handles.EndGUI();
        }

        private Mesh CustomHandleMesh;

        /// <summary>
        /// カスタムメッシュハンドルの描画
        /// </summary>
        /// <param name="controlID"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="size"></param>
        /// <param name="eventType"></param>
        private void CustomMeshHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            switch (eventType)
            {
                case EventType.MouseMove:
                case EventType.Layout:
                    UnityEditor.HandleUtility.AddControl(controlID, DistanceToMesh(CustomHandleMesh, position, rotation, size));
                    break;

                case EventType.Repaint:
                    Graphics.DrawMeshNow(CustomHandleMesh, StartCapDraw(position, rotation, size));
                    break;
            }
        }

        /// <summary>
        /// ハンドルの描画設定をしてメッシュの変換行列を返す
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private Matrix4x4 StartCapDraw(Vector3 position, Quaternion rotation, float size)
        {
            Shader.SetGlobalColor("_HandleColor", Handles.color);
            Shader.SetGlobalFloat("_HandleSize", size);
            Matrix4x4 matrix4x = Handles.matrix * Matrix4x4.TRS(position, rotation, Vector3.one);
            Shader.SetGlobalMatrix("_ObjectToWorld", matrix4x);
            UnityEditor.HandleUtility.handleMaterial.SetFloat("_HandleZTest", (float)Handles.zTest);
            UnityEditor.HandleUtility.handleMaterial.SetPass(0);
            return matrix4x;
        }

        /// <summary>
        /// マウス位置とメッシュまでの距離を計算
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static float DistanceToMesh(Mesh mesh, Vector3 position, Quaternion rotation, float size)
        {
            // カメラに表示されている場合のみ距離を計算
            if (IsAnyVertexInView(mesh, position, rotation, size))
            {
                return DistanceToPointCloudConvexHull(mesh.vertices);
            }
            return float.PositiveInfinity;
        }

        /// <summary>
        /// メッシュの頂点がカメラに表示されているか
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static bool IsAnyVertexInView(Mesh mesh, Vector3 position, Quaternion rotation, float size)
        {
            Vector3[] vertices = mesh.vertices;
            Camera currentCamera = SceneView.currentDrawingSceneView.camera;

            foreach (var vertex in vertices)
            {
                Vector3 worldVertex = position + rotation * (vertex * size);
                Vector3 screenPoint = currentCamera.WorldToScreenPoint(worldVertex);

                if (screenPoint.z > 0 && screenPoint.x >= 0 && screenPoint.x <= UnityEngine.Screen.width && screenPoint.y >= 0 && screenPoint.y <= UnityEngine.Screen.height)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 点群の凸包までの距離を計算
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private static float DistanceToPointCloudConvexHull(params Vector3[] points)
        {
            if (points == null || points.Length == 0 || Camera.current == null)
            {
                return float.PositiveInfinity;
            }

            List<Vector2> pointCloudConvexHull = new List<Vector2>();
            Vector2 mousePosition = Event.current.mousePosition;
            CalcPointCloudConvexHull(points, pointCloudConvexHull);
            return DistancePointToConvexHull(mousePosition, pointCloudConvexHull);
        }

        /// <summary>
        /// 3Dの凸包を2Dに変換
        /// </summary>
        /// <param name="points"></param>
        /// <param name="outHull"></param>
        private static void CalcPointCloudConvexHull(Vector3[] points, List<Vector2> outHull)
        {
            outHull.Clear();
            if (points != null && points.Length != 0)
            {
                Matrix4x4 matrix = Handles.matrix;
                CameraProjectionCache cameraProjectionCache = new CameraProjectionCache(SceneView.currentDrawingSceneView.camera);
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = cameraProjectionCache.WorldToGUIPoint(matrix.MultiplyPoint3x4(points[i]));
                }

                CalcConvexHull2D(points, outHull);
            }
        }

        /// <summary>
        /// 2Dの凸包を計算
        /// </summary>
        /// <param name="points"></param>
        /// <param name="outHull"></param>
        private static void CalcConvexHull2D(Vector3[] points, List<Vector2> outHull)
        {
            outHull.Clear();
            if (points == null || points.Length == 0)
            {
                return;
            }

            int num = points.Length + 1;
            if (outHull.Capacity < num)
            {
                outHull.Capacity = num;
            }

            if (points.Length == 1)
            {
                outHull.Add(points[0]);
                return;
            }

            Array.Sort(points, delegate (Vector3 a, Vector3 b)
            {
                int num3 = a.x.CompareTo(b.x);
                return (num3 != 0) ? num3 : a.y.CompareTo(b.y);
            });
            foreach (Vector2 vector in points)
            {
                RemoveInsidePoints(2, vector, outHull);
                outHull.Add(vector);
            }

            int num2 = points.Length - 2;
            int countLimit = outHull.Count + 1;
            while (num2 >= 0)
            {
                Vector2 vector2 = points[num2];
                RemoveInsidePoints(countLimit, vector2, outHull);
                outHull.Add(vector2);
                num2--;
            }

            outHull.RemoveAt(outHull.Count - 1);
        }

        /// <summary>
        /// 内部の点を削除
        /// </summary>
        /// <param name="countLimit"></param>
        /// <param name="pt"></param>
        /// <param name="hull"></param>
        private static void RemoveInsidePoints(int countLimit, Vector2 pt, List<Vector2> hull)
        {
            while (hull.Count >= countLimit && CalcPointSide(hull[hull.Count - 2], hull[hull.Count - 1], pt) <= 0f)
            {
                hull.RemoveAt(hull.Count - 1);
            }
        }

        /// <summary>
        /// 点と凸包の距離を計算
        /// </summary>
        /// <param name="p"></param>
        /// <param name="hull"></param>
        /// <returns></returns>
        private static float DistancePointToConvexHull(Vector2 p, List<Vector2> hull)
        {
            float num = float.PositiveInfinity;
            if (hull == null || hull.Count == 0)
            {
                return num;
            }

            bool flag = hull.Count > 1;
            int num2 = 0;
            for (int i = 0; i < hull.Count; i++)
            {
                int index = ((i == 0) ? (hull.Count - 1) : (i - 1));
                Vector2 vector = hull[i];
                Vector2 vector2 = hull[index];
                float num3 = CalcPointSide(vector, vector2, p);
                int num4 = ((num3 >= 0f) ? 1 : (-1));
                if (num2 == 0)
                {
                    num2 = num4;
                }
                else if (num4 != num2)
                {
                    flag = false;
                }

                float b = DistancePointToLineSegment(p, vector, vector2);
                num = Mathf.Min(num, b);
            }

            if (flag)
            {
                num = 0f;
            }

            return num;
        }

        /// <summary>
        /// 線分に対する点の位置を計算
        /// </summary>
        /// <param name="l0"></param>
        /// <param name="l1"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private static float CalcPointSide(Vector2 l0, Vector2 l1, Vector2 point)
        {
            return (l1.y - l0.y) * (point.x - l0.x) - (l1.x - l0.x) * (point.y - l0.y);
        }

        /// <summary>
        /// 点と線分の距離を計算
        /// </summary>
        /// <param name="p"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float DistancePointToLineSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            float sqrMagnitude = (b - a).sqrMagnitude;
            if ((double)sqrMagnitude == 0.0)
            {
                return (p - a).magnitude;
            }

            float num = Vector2.Dot(p - a, b - a) / sqrMagnitude;
            if ((double)num < 0.0)
            {
                return (p - a).magnitude;
            }

            if ((double)num > 1.0)
            {
                return (p - b).magnitude;
            }

            Vector2 vector = a + num * (b - a);
            return (p - vector).magnitude;
        }
    }
}