using System.IO;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using TrafficSimulationTool.Runtime;
using PLATEAU.RoadNetwork.Structure;
using PLATEAU.RoadNetwork.Data;
using PLATEAU.Native;
using UnityEngine.Splines;

namespace TrafficSimulationTool.Editor
{
    /// <summary>
    /// 信号現示設定タブのコントローラ
    /// </summary>
    public class SignalViewController : ViewController
    {
        private CsvPropertiesSignalDefine[] SignalDefines;
        private CsvPropertiesSignalControl[] SignalControls;

        private Dictionary<string, List<CsvPropertiesSignalDefine>> SignalDefineMap = new Dictionary<string, List<CsvPropertiesSignalDefine>>();
        private Dictionary<string, List<CsvPropertiesSignalControl>> SignalControlMap = new Dictionary<string, List<CsvPropertiesSignalControl>>();

        private string SelectedIntersectionNumber;
        private string SelectedDefineDate;
        private string SelectedControlDate;
        private string SelectedControlTime;

        private CsvPropertiesSignalDefine SelectedSignalDefine;
        private CsvPropertiesSignalControl SelectedSignalControl;

        private SimRoadNetworkLink[] SelectedInflowLinks = new Runtime.SimRoadNetworkLink[8];
        private SimRoadNetworkLink[] SelectedOutflowLinks = new Runtime.SimRoadNetworkLink[8];
        private SimRoadNetworkNode SelectedNode;

        public SignalViewController(VisualElement element, TrafficSimulationToolWindow parent) : base(element, parent)
        {
        }

        protected override void Initialize()
        {
            var buttonImportDefine = rootElement.Q<Button>("ButtonImportDefine");

            var pathImportDefine = rootElement.Q<TextField>("PathImportDefine");

            pathImportDefine.SetEnabled(false);

            buttonImportDefine.clickable.clicked += () =>
            {
                var folderPath = Application.persistentDataPath;

                var filePath = EditorUtility.OpenFilePanel("Select Import Signal Define File", folderPath, "csv");

                pathImportDefine.value = filePath;

                ImportSignalDefine(filePath);

                UpdateElements();
            };

            var buttonImportControl = rootElement.Q<Button>("ButtonImportControl");

            var pathImportControl = rootElement.Q<TextField>("PathImportControl");

            pathImportControl.SetEnabled(false);

            buttonImportControl.clickable.clicked += () =>
            {
                var folderPath = Application.persistentDataPath;

                var filePath = EditorUtility.OpenFilePanel("Select Import Signal Control File", folderPath, "csv");

                pathImportControl.value = filePath;

                ImportSignalControl(filePath);

                UpdateElements();
            };

            var applyNode = rootElement.Q<DropdownField>("ApplyNode");

            var roadNetworkManager = GameObject.FindObjectOfType<SimRoadNetworkManager>();

            if (roadNetworkManager != null)
            {
                roadNetworkManager.SimRoadNetworkNodes.ForEach(node => applyNode.choices.Add(node.ID));

                applyNode.RegisterValueChangedCallback(evt =>
                {
                    SelectedNode = roadNetworkManager.SimRoadNetworkNodes.Find(x => x.ID == evt.newValue);
                });
            }

            var buttonApply = rootElement.Q<Button>("ButtonApply");

            buttonApply.clickable.clicked += () =>
            {
#if PLATEAU_TOOLKIT

                ApplySignal();

#endif
                // SDK交通規制を利用する場合
                //GenerateSignal();
            };
        }

        public override void OnEnable()
        {
            SceneView.duringSceneGui -= OnDrawRoadNetworkGUI;

            SceneView.duringSceneGui += OnDrawRoadNetworkGUI;
        }

        public override void OnDisable()
        {
            SceneView.duringSceneGui -= OnDrawRoadNetworkGUI;
        }

        private void OnDrawRoadNetworkGUI(SceneView sceneView)
        {
            var roadNetworkManager = GameObject.FindObjectOfType<SimRoadNetworkManager>();

            // ノード情報の描画
            foreach (var node in roadNetworkManager.SimRoadNetworkNodes)
            {
                var geo = node.GetGeometory();

                var coord = roadNetworkManager.GeoReference.Project(new GeoCoordinate(geo.Latitude, geo.Longitude, 0));

                var pos = new Vector3((float)coord.X, (float)coord.Y, (float)coord.Z);

                var label = node.ID;

                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                UnityEditor.Handles.Label(pos, label, style);
            }

            // リンク情報の描画
            foreach (var link in roadNetworkManager.SimRoadNetworkLinks)
            {
                if (link.Lanes.Count == 0)
                {
                    continue;
                }

                var spline = new Spline();

                foreach (var geoCoord in link.Lanes[link.IsReverse ? link.Lanes.Count - 1 : 0].GetGeometory(false))
                {
                    var geo = roadNetworkManager.GeoReference.Project(new GeoCoordinate(geoCoord.Latitude, geoCoord.Longitude, 0));

                    spline.Add(new BezierKnot(new Vector3((float)geo.X, (float)geo.Y, (float)geo.Z)));
                }

                spline.Closed = false;

                var pos = spline.EvaluatePosition(0.5f);
                var label = link.ID;

                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                UnityEditor.Handles.Label(pos, label, style);
            }
        }

#if PLATEAU_TOOLKIT

        private void ApplySignal()
        {
            var roadNetworkManager = GameObject.FindObjectOfType<SimRoadNetworkManager>();

            var roadNetworkRoads = roadNetworkManager.RoadNetworkDataGetter.GetRoadBases();

            // 信号制御器を取得

            var trafficIntersections = GameObject.FindObjectsOfType<AWSIM.TrafficSimulation.TrafficIntersection>();

            var trafficIntersection = trafficIntersections.ToList().Find(x => roadNetworkRoads[x.rnTrafficLightController.Parent.ID] == SelectedNode.OriginNode);

            if (trafficIntersection == null)
            {
                return;
            }

            // スプリットから各青現示の時間を取得

            var cycle = int.Parse(SelectedSignalControl.CycleLength);

            var cycles = new List<int>()
            {
                SelectedSignalControl.Split1 != null ? cycle * (int.Parse(SelectedSignalControl.Split1)) / 100 : 0,
                SelectedSignalControl.Split2 != null ? cycle * (int.Parse(SelectedSignalControl.Split2)) / 100 : 0,
                SelectedSignalControl.Split3 != null ? cycle * (int.Parse(SelectedSignalControl.Split3)) / 100 : 0,
                SelectedSignalControl.Split4 != null ? cycle * (int.Parse(SelectedSignalControl.Split4)) / 100 : 0,
                SelectedSignalControl.Split5 != null ? cycle * (int.Parse(SelectedSignalControl.Split5)) / 100 : 0,
                SelectedSignalControl.Split6 != null ? cycle * (int.Parse(SelectedSignalControl.Split6)) / 100 : 0,
            };

            var inflowTable = new int[6, 8]
            {
                {
                    SelectedSignalDefine.Split1IncomingLink1,
                    SelectedSignalDefine.Split1IncomingLink2,
                    SelectedSignalDefine.Split1IncomingLink3,
                    SelectedSignalDefine.Split1IncomingLink4,
                    SelectedSignalDefine.Split1IncomingLink5,
                    SelectedSignalDefine.Split1IncomingLink6,
                    SelectedSignalDefine.Split1IncomingLink7,
                    SelectedSignalDefine.Split1IncomingLink8,
                },
                {
                    SelectedSignalDefine.Split2IncomingLink1,
                    SelectedSignalDefine.Split2IncomingLink2,
                    SelectedSignalDefine.Split2IncomingLink3,
                    SelectedSignalDefine.Split2IncomingLink4,
                    SelectedSignalDefine.Split2IncomingLink5,
                    SelectedSignalDefine.Split2IncomingLink6,
                    SelectedSignalDefine.Split2IncomingLink7,
                    SelectedSignalDefine.Split2IncomingLink8,
                },
                {
                    SelectedSignalDefine.Split3IncomingLink1,
                    SelectedSignalDefine.Split3IncomingLink2,
                    SelectedSignalDefine.Split3IncomingLink3,
                    SelectedSignalDefine.Split3IncomingLink4,
                    SelectedSignalDefine.Split3IncomingLink5,
                    SelectedSignalDefine.Split3IncomingLink6,
                    SelectedSignalDefine.Split3IncomingLink7,
                    SelectedSignalDefine.Split3IncomingLink8,
                },
                {
                    SelectedSignalDefine.Split4IncomingLink1,
                    SelectedSignalDefine.Split4IncomingLink2,
                    SelectedSignalDefine.Split4IncomingLink3,
                    SelectedSignalDefine.Split4IncomingLink4,
                    SelectedSignalDefine.Split4IncomingLink5,
                    SelectedSignalDefine.Split4IncomingLink6,
                    SelectedSignalDefine.Split4IncomingLink7,
                    SelectedSignalDefine.Split4IncomingLink8,
                },
                {
                    SelectedSignalDefine.Split5IncomingLink1,
                    SelectedSignalDefine.Split5IncomingLink2,
                    SelectedSignalDefine.Split5IncomingLink3,
                    SelectedSignalDefine.Split5IncomingLink4,
                    SelectedSignalDefine.Split5IncomingLink5,
                    SelectedSignalDefine.Split5IncomingLink6,
                    SelectedSignalDefine.Split5IncomingLink7,
                    SelectedSignalDefine.Split5IncomingLink8,
                },
                {
                    SelectedSignalDefine.Split6IncomingLink1,
                    SelectedSignalDefine.Split6IncomingLink2,
                    SelectedSignalDefine.Split6IncomingLink3,
                    SelectedSignalDefine.Split6IncomingLink4,
                    SelectedSignalDefine.Split6IncomingLink5,
                    SelectedSignalDefine.Split6IncomingLink6,
                    SelectedSignalDefine.Split6IncomingLink7,
                    SelectedSignalDefine.Split6IncomingLink8,
                },
            };

            for (int j = 0; j < cycles.Count; j++)
            {
                for (int m = 0; m < SelectedInflowLinks.Length; m++)
                {
                    if (SelectedInflowLinks[m] == null) continue;

                    if (inflowTable[j, m] != 1) continue;

                    foreach (var group in trafficIntersection.TrafficLightGroups.Select((value, index) => new { value, index }))
                    {
                        var light = group.value.TrafficLights.ToList().Find(x => roadNetworkRoads[x.rnTrafficLight.RoadId.ID] == SelectedInflowLinks[m]?.OriginLink);

                        if (light == null) continue;

                        foreach (var sequence in trafficIntersection.LightingSequences)
                        {
                            foreach (var order in sequence.GroupLightingOrders)
                            {
                                if (order.Group == group.index)
                                {
                                    if (order.BulbData[0].Color == AWSIM.TrafficLightData.BulbColor.GREEN)
                                    {
                                        sequence.SetIntervalSec(cycles[j]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

#endif

        public void GenerateSignal()
        {
            var roadNetworkManager = GameObject.FindObjectOfType<SimRoadNetworkManager>();

            var controller = new SimSignalController();

            controller.ID = "Signal" + SelectedNode.ID;

            controller.Node = SelectedNode;

            for (int i = 0; i < SelectedInflowLinks.Length; i++)
            {
                if (SelectedInflowLinks[i] == null) continue;

                var light = new SimSignalLight();

                light.ID = "Light" + SelectedNode.ID.Replace("Node", "") + "_" + i;

                light.Controller = controller;

                light.Link = SelectedInflowLinks[i];

                controller.SignalLights.Add(light);

                // Manager に信号灯器を追加
                roadNetworkManager.SignalLights.Add(light);
            }

            var cycle = int.Parse(SelectedSignalControl.CycleLength);

            var cycles = new List<int>()
            {
                SelectedSignalControl.Split1 != null ? cycle * (int.Parse(SelectedSignalControl.Split1)) / 100 : 0,
                SelectedSignalControl.Split2 != null ? cycle * (int.Parse(SelectedSignalControl.Split2)) / 100 : 0,
                SelectedSignalControl.Split3 != null ? cycle * (int.Parse(SelectedSignalControl.Split3)) / 100 : 0,
                SelectedSignalControl.Split4 != null ? cycle * (int.Parse(SelectedSignalControl.Split4)) / 100 : 0,
                SelectedSignalControl.Split5 != null ? cycle * (int.Parse(SelectedSignalControl.Split5)) / 100 : 0,
                SelectedSignalControl.Split6 != null ? cycle * (int.Parse(SelectedSignalControl.Split6)) / 100 : 0,
            };

            var inflowTable = new int[6, 8]
            {
                {
                    SelectedSignalDefine.Split1IncomingLink1,
                    SelectedSignalDefine.Split1IncomingLink2,
                    SelectedSignalDefine.Split1IncomingLink3,
                    SelectedSignalDefine.Split1IncomingLink4,
                    SelectedSignalDefine.Split1IncomingLink5,
                    SelectedSignalDefine.Split1IncomingLink6,
                    SelectedSignalDefine.Split1IncomingLink7,
                    SelectedSignalDefine.Split1IncomingLink8,
                },
                {
                    SelectedSignalDefine.Split2IncomingLink1,
                    SelectedSignalDefine.Split2IncomingLink2,
                    SelectedSignalDefine.Split2IncomingLink3,
                    SelectedSignalDefine.Split2IncomingLink4,
                    SelectedSignalDefine.Split2IncomingLink5,
                    SelectedSignalDefine.Split2IncomingLink6,
                    SelectedSignalDefine.Split2IncomingLink7,
                    SelectedSignalDefine.Split2IncomingLink8,
                },
                {
                    SelectedSignalDefine.Split3IncomingLink1,
                    SelectedSignalDefine.Split3IncomingLink2,
                    SelectedSignalDefine.Split3IncomingLink3,
                    SelectedSignalDefine.Split3IncomingLink4,
                    SelectedSignalDefine.Split3IncomingLink5,
                    SelectedSignalDefine.Split3IncomingLink6,
                    SelectedSignalDefine.Split3IncomingLink7,
                    SelectedSignalDefine.Split3IncomingLink8,
                },
                {
                    SelectedSignalDefine.Split4IncomingLink1,
                    SelectedSignalDefine.Split4IncomingLink2,
                    SelectedSignalDefine.Split4IncomingLink3,
                    SelectedSignalDefine.Split4IncomingLink4,
                    SelectedSignalDefine.Split4IncomingLink5,
                    SelectedSignalDefine.Split4IncomingLink6,
                    SelectedSignalDefine.Split4IncomingLink7,
                    SelectedSignalDefine.Split4IncomingLink8,
                },
                {
                    SelectedSignalDefine.Split5IncomingLink1,
                    SelectedSignalDefine.Split5IncomingLink2,
                    SelectedSignalDefine.Split5IncomingLink3,
                    SelectedSignalDefine.Split5IncomingLink4,
                    SelectedSignalDefine.Split5IncomingLink5,
                    SelectedSignalDefine.Split5IncomingLink6,
                    SelectedSignalDefine.Split5IncomingLink7,
                    SelectedSignalDefine.Split5IncomingLink8,
                },
                {
                    SelectedSignalDefine.Split6IncomingLink1,
                    SelectedSignalDefine.Split6IncomingLink2,
                    SelectedSignalDefine.Split6IncomingLink3,
                    SelectedSignalDefine.Split6IncomingLink4,
                    SelectedSignalDefine.Split6IncomingLink5,
                    SelectedSignalDefine.Split6IncomingLink6,
                    SelectedSignalDefine.Split6IncomingLink7,
                    SelectedSignalDefine.Split6IncomingLink8,
                },
            };

            var outflowTable = new int[6, 8]
            {
                {
                    SelectedSignalDefine.Split1OutgoingLink1,
                    SelectedSignalDefine.Split1OutgoingLink2,
                    SelectedSignalDefine.Split1OutgoingLink3,
                    SelectedSignalDefine.Split1OutgoingLink4,
                    SelectedSignalDefine.Split1OutgoingLink5,
                    SelectedSignalDefine.Split1OutgoingLink6,
                    SelectedSignalDefine.Split1OutgoingLink7,
                    SelectedSignalDefine.Split1OutgoingLink8,
                },
                {
                    SelectedSignalDefine.Split2OutgoingLink1,
                    SelectedSignalDefine.Split2OutgoingLink2,
                    SelectedSignalDefine.Split2OutgoingLink3,
                    SelectedSignalDefine.Split2OutgoingLink4,
                    SelectedSignalDefine.Split2OutgoingLink5,
                    SelectedSignalDefine.Split2OutgoingLink6,
                    SelectedSignalDefine.Split2OutgoingLink7,
                    SelectedSignalDefine.Split2OutgoingLink8,
                },
                {
                    SelectedSignalDefine.Split3OutgoingLink1,
                    SelectedSignalDefine.Split3OutgoingLink2,
                    SelectedSignalDefine.Split3OutgoingLink3,
                    SelectedSignalDefine.Split3OutgoingLink4,
                    SelectedSignalDefine.Split3OutgoingLink5,
                    SelectedSignalDefine.Split3OutgoingLink6,
                    SelectedSignalDefine.Split3OutgoingLink7,
                    SelectedSignalDefine.Split3OutgoingLink8,
                },
                {
                    SelectedSignalDefine.Split4OutgoingLink1,
                    SelectedSignalDefine.Split4OutgoingLink2,
                    SelectedSignalDefine.Split4OutgoingLink3,
                    SelectedSignalDefine.Split4OutgoingLink4,
                    SelectedSignalDefine.Split4OutgoingLink5,
                    SelectedSignalDefine.Split4OutgoingLink6,
                    SelectedSignalDefine.Split4OutgoingLink7,
                    SelectedSignalDefine.Split4OutgoingLink8,
                },
                {
                    SelectedSignalDefine.Split5OutgoingLink1,
                    SelectedSignalDefine.Split5OutgoingLink2,
                    SelectedSignalDefine.Split5OutgoingLink3,
                    SelectedSignalDefine.Split5OutgoingLink4,
                    SelectedSignalDefine.Split5OutgoingLink5,
                    SelectedSignalDefine.Split5OutgoingLink6,
                    SelectedSignalDefine.Split5OutgoingLink7,
                    SelectedSignalDefine.Split5OutgoingLink8,
                },
                {
                    SelectedSignalDefine.Split6OutgoingLink1,
                    SelectedSignalDefine.Split6OutgoingLink2,
                    SelectedSignalDefine.Split6OutgoingLink3,
                    SelectedSignalDefine.Split6OutgoingLink4,
                    SelectedSignalDefine.Split6OutgoingLink5,
                    SelectedSignalDefine.Split6OutgoingLink6,
                    SelectedSignalDefine.Split6OutgoingLink7,
                    SelectedSignalDefine.Split6OutgoingLink8,
                },
            };

            // pattern
            for (int i = 0; i < 1; i++)
            {
                var time = SelectedControlTime.Replace(":", "/") + "/00";

                var steps = new List<SimSignalStep>();

                controller.SignalPatterns.Add(time, steps);

                // step
                for (int j = 0; j < cycles.Count; j++)
                {
                    if (cycles[j] == 0) continue;

                    var step = new SimSignalStep();

                    step.ID = "Step" + SelectedNode.ID + "_" + i + "_" + j;

                    step.Controller = controller;

                    step.SignalLights = controller.SignalLights;

                    step.PatternID = "Pattern" + 0;

                    step.Order = j;

                    step.Duration = cycles[j];

                    // 流入リンク分回す
                    for (int m = 0; m < SelectedInflowLinks.Length; m++)
                    {
                        if (SelectedInflowLinks[m] == null) continue;

                        if (inflowTable[j, m] == 1)
                        {
                            for (int n = 0; n < SelectedOutflowLinks.Length; n++)
                            {
                                if (SelectedOutflowLinks[n] == null) continue;

                                if (outflowTable[j, n] == 1)
                                {
                                    step.LinkPairsGreen.Add((SelectedInflowLinks[m], SelectedOutflowLinks[n]));
                                }
                            }
                        }
                        else
                        {
                            for (int n = 0; n < SelectedOutflowLinks.Length; n++)
                            {
                                if (SelectedOutflowLinks[n] == null) continue;

                                //if (outflowTable[j, n] == 0)
                                {
                                    step.LinkPairsRed.Add((SelectedInflowLinks[m], SelectedOutflowLinks[n]));
                                }
                            }
                        }
                    }

                    steps.Add(step);

                    // Manager に信号現示階梯を追加
                    roadNetworkManager.SignalSteps.Add(step);
                }
            }

            // Manager に信号制御機を追加
            roadNetworkManager.SignalControllers.Add(controller);

            // SDK に信号制御機を追加

            // ID取得
            var mapRoadBaseRnID = roadNetworkManager.RoadNetworkDataGetter.GenerateIdTable<RnDataRoadBase>(roadNetworkManager.RoadNetworkDataGetter.GetRoadBases()); // TODO: RoadNetworkManager に持たせる？

            // ID生成に必要な機能を作成
            var roadIdGenrator = roadNetworkManager.RoadNetworkDataSetter.CreateIdGenerator<RnDataRoadBase>();
            var controllerIdGenrator = roadNetworkManager.RoadNetworkDataSetter.CreateIdGenerator<RnDataTrafficLightController>();
            var lightIdGenrator = roadNetworkManager.RoadNetworkDataSetter.CreateIdGenerator<RnDataTrafficLight>();
            var patternIdGenrator = roadNetworkManager.RoadNetworkDataSetter.CreateIdGenerator<RnDataTrafficSignalPattern>();
            var phaseIdGenrator = roadNetworkManager.RoadNetworkDataSetter.CreateIdGenerator<RnDataTrafficSignalPhase>();

            // 信号制御器
            var trafficLightControllers = roadNetworkManager.RoadNetworkDataGetter.GetTrafficLightController();

            var rnDataTrafficLightControllers = new List<RnDataTrafficLightController>();

            rnDataTrafficLightControllers.AddRange(trafficLightControllers);

            var importController = new RnDataTrafficLightController()
            {
                Parent = mapRoadBaseRnID[controller.Node.OriginNode],
                TrafficLights = new List<RnID<RnDataTrafficLight>>(),
                SignalPatterns = new List<RnID<RnDataTrafficSignalPattern>>(),
            };

            rnDataTrafficLightControllers.Add(importController);

            var controllerId = new RnID<RnDataTrafficLightController>(rnDataTrafficLightControllers.Count - 1, roadIdGenrator);

            // 信号灯火器
            var trafficLisghts = roadNetworkManager.RoadNetworkDataGetter.GetTrafficLights();

            var rnDataTrafficLights = new List<RnDataTrafficLight>();

            rnDataTrafficLights.AddRange(trafficLisghts);

            var signalLightIDs = new List<RnID<RnDataTrafficLight>>();

            foreach (var item in controller.SignalLights)
            {
                rnDataTrafficLights.Add(new RnDataTrafficLight()
                {
                    Parent = controllerId,
                    RoadId = mapRoadBaseRnID[item.Link.OriginLink],
                    LaneType = item.GetLaneType(),
                    Distance = float.Parse(item.GetDistance()),
                });

                signalLightIDs.Add(new RnID<RnDataTrafficLight>(rnDataTrafficLights.Count - 1, lightIdGenrator));
            }

            importController.TrafficLights.AddRange(signalLightIDs);

            // パターン
            var trafficSignalPatterns = roadNetworkManager.RoadNetworkDataGetter.GetTrafficSignalPattern();

            var rnDataTrafficSignalPatterns = new List<RnDataTrafficSignalPattern>();

            rnDataTrafficSignalPatterns.AddRange(trafficSignalPatterns);

            var signalPatternIDs = new List<RnID<RnDataTrafficSignalPattern>>();

            foreach (var item in controller.SignalPatterns)
            {
                rnDataTrafficSignalPatterns.Add(new RnDataTrafficSignalPattern()
                {
                    Parent = controllerId,
                    Phases = new List<RnID<RnDataTrafficSignalPhase>>(),
                    OffsetSeconds = controller.OffsetValue,
                    OffsetTrafficLight = RnID<RnDataTrafficLight>.Undefined,
                    OffsetType = (OffsetRelationType)controller.OffsetType,
                    //StartOffsets = DateTime.Parse(item.StartTime),
                });

                signalPatternIDs.Add(new RnID<RnDataTrafficSignalPattern>(rnDataTrafficSignalPatterns.Count - 1, patternIdGenrator));
            }

            importController.SignalPatterns.AddRange(signalPatternIDs);

            // フェイズ
            var trafficSignalPhases = roadNetworkManager.RoadNetworkDataGetter.GetTrafficSignalPhase();

            var rnDataTrafficSignalPhases = new List<RnDataTrafficSignalPhase>();

            rnDataTrafficSignalPhases.AddRange(trafficSignalPhases);

            var signalPhaseIDs = new List<RnID<RnDataTrafficSignalPhase>>();

            //foreach (var item in controller.SignalPatterns)
            for (var i = 0; i < controller.SignalPatterns.Count; i++)
            {
                var item = controller.SignalPatterns.Values.ElementAt(i);

                for (int j = 0; j < item.Count; j++)
                {
                    var step = item[j];

                    rnDataTrafficSignalPhases.Add(new RnDataTrafficSignalPhase()
                    {
                        Parent = signalPatternIDs[i],
                        Order = step.Order,
                        Split = step.Duration,
                        EnterableVehicleTypeMask = step.GetTypeMask(),
                        //BlueRoadPairs = step.LinkPairsGreen.Select(x => new RnID<RnDataRoadBase>(mapRoadBaseRnID[x.In.OriginLink].ID, roadIdGenrator)).ToList(),
                        //YellowRoadPairs = step.LinkPairsYellow.Select(x => new RnID<RnDataRoadBase>(mapRoadBaseRnID[x.In.OriginLink].ID, roadIdGenrator)).ToList(),
                        //RedRoadPairs = step.LinkPairsRed.Select(x => new RnID<RnDataRoadBase>(mapRoadBaseRnID[x.In.OriginLink].ID, roadIdGenrator)).ToList(),
                    });

                    signalPhaseIDs.Add(new RnID<RnDataTrafficSignalPhase>(rnDataTrafficSignalPhases.Count - 1, phaseIdGenrator));
                }
            }

            foreach (var item in rnDataTrafficSignalPatterns)
            {
                item.Phases.AddRange(signalPhaseIDs);
            }

            // SDK に書き戻す
            var trafficLightDataSet = new RnTrafficLightDataSet(rnDataTrafficLightControllers, rnDataTrafficLights, rnDataTrafficSignalPatterns, rnDataTrafficSignalPhases);

            roadNetworkManager.RoadNetworkDataSetter.SetTrafficSignalLightController(trafficLightDataSet);
        }

        private void ImportSignalDefine(string importPath)
        {
            try
            {
                SignalDefines = CsvImporter.ParseCsv<CsvPropertiesSignalDefine>(importPath, true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void ImportSignalControl(string importPath)
        {
            try
            {
                SignalControls = CsvImporter.ParseCsv<CsvPropertiesSignalControl>(importPath, true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void UpdateElements()
        {
            if (SignalDefines == null || SignalControls == null)
            {
                return;
            }

            SignalDefineMap = SignalDefines.GroupBy(x => x.IntersectionNumber).ToDictionary(x => x.Key, x => x.ToList());

            SignalControlMap = SignalControls.GroupBy(x => x.IntersectionNumber).ToDictionary(x => x.Key, x => x.ToList());

            var intersectionNumberDropdown = rootElement.Q<DropdownField>("IntersectionNumber");

            intersectionNumberDropdown.choices.Clear();

            intersectionNumberDropdown.choices = SignalDefineMap.Keys.ToList();

            var deineDateDropdown = rootElement.Q<DropdownField>("SelectDefineDate");

            var controlDateDropdown = rootElement.Q<DropdownField>("SelectControlDate");

            var controlTimeDropdown = rootElement.Q<DropdownField>("SelectControlTime");

            // 交差点番号の選択
            intersectionNumberDropdown.RegisterValueChangedCallback(evt =>
            {
                SelectedIntersectionNumber = evt.newValue;

                deineDateDropdown.choices.Clear();

                controlTimeDropdown.choices.Clear();

                if (SignalDefineMap.TryGetValue(evt.newValue, out List<CsvPropertiesSignalDefine> defines))
                {
                    foreach (var item in defines)
                    {
                        deineDateDropdown.choices.Add(item.Date);
                    }

                    // 定義日の選択
                    deineDateDropdown.RegisterValueChangedCallback(evt =>
                    {
                        SelectedDefineDate = evt.newValue;

                        SelectedSignalDefine = defines.Find(x => x.Date == SelectedDefineDate);

                        UpdateReplaceElement();
                    });
                }

                if (SignalControlMap.TryGetValue(evt.newValue, out List<CsvPropertiesSignalControl> controlls))
                {
                    foreach (var item in controlls)
                    {
                        var day = item.Time.Split(' ')[0];

                        if (controlDateDropdown.choices.Contains(day))
                        {
                            continue;
                        }

                        controlDateDropdown.choices.Add(day);
                    }

                    // 制御日の選択
                    controlDateDropdown.RegisterValueChangedCallback(evt =>
                    {
                        SelectedControlDate = evt.newValue;

                        controlTimeDropdown.choices.Clear();

                        foreach (var item in controlls)
                        {
                            var day = item.Time.Split(' ')[0];

                            var time = item.Time.Split(' ')[1];

                            if (!day.Equals(SelectedControlDate))
                            {
                                continue;
                            }

                            if (controlTimeDropdown.choices.Contains(time))
                            {
                                continue;
                            }

                            controlTimeDropdown.choices.Add(time);

                            controlTimeDropdown.RegisterValueChangedCallback(evt =>
                            {
                                SelectedControlTime = evt.newValue;

                                SelectedSignalControl = controlls.Find(x => x.Time.Split(' ')[1] == SelectedControlTime);
                            });
                        }
                    });
                }
            });
        }

        private void UpdateReplaceElement()
        {
            const string DefaultValue = "blank";

            var roadNetworkManager = GameObject.FindObjectOfType<SimRoadNetworkManager>();

            var inlinks = new List<string>()
            {
                SelectedSignalDefine.IncomingLink1Number,
                SelectedSignalDefine.IncomingLink2Number,
                SelectedSignalDefine.IncomingLink3Number,
                SelectedSignalDefine.IncomingLink4Number,
                SelectedSignalDefine.IncomingLink5Number,
                SelectedSignalDefine.IncomingLink6Number,
                SelectedSignalDefine.IncomingLink7Number,
                SelectedSignalDefine.IncomingLink8Number,
            };

            foreach (var item in inlinks)
            {
                var inlink = rootElement.Q<TextField>($"OriginInflowLink{inlinks.IndexOf(item) + 1}");

                inlink.value = DefaultValue;

                var replaceInlink = rootElement.Q<DropdownField>($"ReplaceInflowLink{inlinks.IndexOf(item) + 1}");

                replaceInlink.choices.Clear();

                if (item != null)
                {
                    inlink.value = item;

                    roadNetworkManager.SimRoadNetworkLinks.ForEach(link => replaceInlink.choices.Add(link.ID));
                }

                replaceInlink.RegisterValueChangedCallback(evt =>
                {
                    var index = inlinks.IndexOf(item);

                    SelectedInflowLinks[index] = roadNetworkManager.SimRoadNetworkLinks.Find(x => x.ID == evt.newValue);
                });
            }

            var outlinks = new List<string>()
            {
                SelectedSignalDefine.OutgoingLink1Number,
                SelectedSignalDefine.OutgoingLink2Number,
                SelectedSignalDefine.OutgoingLink3Number,
                SelectedSignalDefine.OutgoingLink4Number,
                SelectedSignalDefine.OutgoingLink5Number,
                SelectedSignalDefine.OutgoingLink6Number,
                SelectedSignalDefine.OutgoingLink7Number,
                SelectedSignalDefine.OutgoingLink8Number,
            };

            foreach (var item in outlinks)
            {
                var outlink = rootElement.Q<TextField>($"OriginOutflowLink{outlinks.IndexOf(item) + 1}");

                outlink.value = DefaultValue;

                var replaceOutlink = rootElement.Q<DropdownField>($"ReplaceOutflowLink{outlinks.IndexOf(item) + 1}");

                replaceOutlink.choices.Clear();

                if (item != null)
                {
                    outlink.value = item;

                    roadNetworkManager.SimRoadNetworkLinks.ForEach(link => replaceOutlink.choices.Add(link.ID));
                }

                replaceOutlink.RegisterValueChangedCallback(evt =>
                {
                    var index = outlinks.IndexOf(item);

                    SelectedOutflowLinks[index] = roadNetworkManager.SimRoadNetworkLinks.Find(x => x.ID == evt.newValue);
                });
            }
        }
    }
}