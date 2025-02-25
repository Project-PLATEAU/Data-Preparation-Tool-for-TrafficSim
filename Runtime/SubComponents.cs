using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using PLATEAU.CityInfo;
using System;

using TrafficSimulationTool.Runtime.Simulator;
using TrafficSimulationTool.Runtime.SimData;
using TrafficSimulationTool.Runtime.UICommon;
using TrafficSimulationTool.Runtime.FX;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using TrafficSimulationTool.Runtime.Util;
using SFB;

namespace TrafficSimulationTool.Runtime
{
    /// <summary>
    /// Mainの処理です
    /// </summary>

    public class SubComponents : MonoBehaviour
    {
        // FPS x PLAYBACK_SPEED = Actual Playback FrameRate (ex:FPS:3 x PLAYBACK_SPEED:8 = 24fps)
        public static readonly uint FPS = 3;

        //public static readonly uint FPS = 1; //Debug (frames = timeline)
        public static readonly float PLAYBACK_SPEED = 8f;

        private CameraManager cameraController;
        private VehicleSimulator vehicleSimulator;
        private TrafficSimulator trafficSimulator;

        private DataManager dataManager;
        private TimelineManager timelineManager;

        private MainMenuController mainMenucontroller;

        private HighlightFX outlineFX;

        private PLATEAUInstancedCityModel cityModel;
        private SimRoadNetworkManager roadNetworkManager;

        private ExtensionFilter[] extensions = new[] {
            new ExtensionFilter("CSV files (*.csv)", "csv" ),
            new ExtensionFilter("All files (*.*)", "*" ),
        };

        private void Awake()
        {
            // 必要な機能をここに追加します

            cityModel = GameObject.FindObjectOfType<PLATEAUInstancedCityModel>();
            if (cityModel == null)
            {
                Debug.LogError("cityModelInstance is Null!");
                return;
            }

            roadNetworkManager = GameObject.FindObjectOfType<SimRoadNetworkManager>();
            if (roadNetworkManager == null)
            {
                Debug.LogError("SimRoadNetworkManager is Null!");
                return;
            }

            cameraController = gameObject.AddComponent<CameraManager>();
            dataManager = gameObject.AddComponent<DataManager>();
            timelineManager = gameObject.AddComponent<TimelineManager>();
            vehicleSimulator = new();
            trafficSimulator = new();
            outlineFX = new();

            cameraController.Initialize(Camera.main);

            roadNetworkManager.Initialize();

            vehicleSimulator.Initialize();
            trafficSimulator.Initialize();
            vehicleSimulator.InitializeReferences(cityModel.GeoReference, roadNetworkManager);
            trafficSimulator.InitializeReferences(cityModel.GeoReference, roadNetworkManager);

            timelineManager.AddSequence(vehicleSimulator);
            timelineManager.AddSequence(trafficSimulator);

            timelineManager.OnTimelineUpdated += OnTimelineUpdated;

            // UI
            mainMenucontroller = UIDocumentFactory.CreateWithUxmlName<MainMenuController>("MainMenu");
            mainMenucontroller.TopMenu.ButtonPressed += OnTopMenuButtonPressed;
            mainMenucontroller.TopMenu.DatalistSelected += OnDataSelected;
            mainMenucontroller.SequenceControl.ButtonPressed += OnSequenceControlButtonPressed;
            mainMenucontroller.SequenceControl.SliderStateChanged += OnSliderStateChange;
            mainMenucontroller.LoadDataDialog.ButtonPressed += OnLoadDataDialogButtonPressed;
            mainMenucontroller.SequenceControl.SetEnabled(false);

            SignalSpawner.SpawnSignal(roadNetworkManager);
        }

        private void OnDestroy()
        {
            if (mainMenucontroller != null)
            {
                mainMenucontroller.TopMenu.ButtonPressed -= OnTopMenuButtonPressed;
                mainMenucontroller.TopMenu.DatalistSelected -= OnDataSelected;
                mainMenucontroller.SequenceControl.ButtonPressed -= OnSequenceControlButtonPressed;
                mainMenucontroller.SequenceControl.SliderStateChanged -= OnSliderStateChange;
                mainMenucontroller.LoadDataDialog.ButtonPressed -= OnLoadDataDialogButtonPressed;
            }

            cameraController?.Dispose();
            timelineManager?.Dispose();
            vehicleSimulator?.Dispose();
            trafficSimulator?.Dispose();
            dataManager?.Dispose();
            mainMenucontroller?.Dispose();
            outlineFX?.Dispose();
        }

        //Data初期化処理
        private async void InitializeData()
        {
            var sw = new DebugStopwatch();//Debug
            DebugLogger.Log(10, "InitializeData start ", "green");

            var success = await dataManager.CurrentDataSets?.Initialize(FPS, cityModel?.GeoReference);

            if (success)
            {
                vehicleSimulator?.SetData(dataManager.CurrentDataSets.vehicleTimelineDataSet);
                trafficSimulator?.SetData(dataManager.CurrentDataSets.roadIndicatorDataSet);
                timelineManager?.SetDurationData(dataManager.CurrentDataSets.duration);
                timelineManager?.SetSpeed(PLAYBACK_SPEED);

                mainMenucontroller.SequenceControl.SetEnabled(true);
            }
            else
            {
                //TODO : エラー表示
                Debug.LogError($"Data Initialization failed.");

                dataManager.RemoveDataSet(dataManager.CurrentDataSets);
                mainMenucontroller.TopMenu.SetDataSet(dataManager.GetDataSetNameList());
            }

            DebugLogger.Log(10, $"InitializeData Finish {sw.GetTimeSeconds()}", "green");
        }

        private void OnTopMenuButtonPressed(GlobalNavController.MenuButtonType type)
        {
            if (type == GlobalNavController.MenuButtonType.LOAD_DATA)
            {
                mainMenucontroller.LoadDataDialog?.SetVisibility(true);
            }
            else if (type == GlobalNavController.MenuButtonType.CAMERA_TYPE_2D)
            {
                cameraController?.SetCameraViewType(CameraManager.CameraViewType.TYPE_2D);
            }
            else if (type == GlobalNavController.MenuButtonType.CAMERA_TYPE_3D)
            {
                cameraController?.SetCameraViewType(CameraManager.CameraViewType.TYPE_3D);
            }
            else if (type == GlobalNavController.MenuButtonType.TOGGLE_VEHICLE_ON)
            {
                vehicleSimulator.SetEnabled(true);
                vehicleSimulator.PlayFrame(timelineManager.GetCurrentFrame());
            }
            else if (type == GlobalNavController.MenuButtonType.TOGGLE_TRAFFIC_ON)
            {
                trafficSimulator.SetEnabled(true);
                trafficSimulator.PlayFrame(timelineManager.GetCurrentFrame());
            }
            else if (type == GlobalNavController.MenuButtonType.TOGGLE_VEHICLE_OFF)
            {
                vehicleSimulator.SetEnabled(false);
                vehicleSimulator.PlayFrame(timelineManager.GetCurrentFrame());
            }
            else if (type == GlobalNavController.MenuButtonType.TOGGLE_TRAFFIC_OFF)
            {
                trafficSimulator.SetEnabled(false);
                trafficSimulator.PlayFrame(timelineManager.GetCurrentFrame());
            }
        }

        private void OnDataSelected(int index)
        {
            dataManager.SetCurrentDataIndex(index);
            InitializeData();
        }

        private void OnLoadDataDialogButtonPressed(LoadDataMenuController.MenuButtonType type)
        {
            if (type == LoadDataMenuController.MenuButtonType.LOAD_VEHICLE_TIMELINE)
                LoadVehicleTimeLine();
            else if (type == LoadDataMenuController.MenuButtonType.LOAD_ROAD_INDICATOR)
                LoadRoadIndicator();
            else if (type == LoadDataMenuController.MenuButtonType.LOAD_DATA_DIALOG_CLOSE)
            {
                mainMenucontroller.LoadDataDialog?.SetVisibility(false);
                mainMenucontroller.TopMenu.SetDataSet(dataManager.GetDataSetNameList());

                InitializeData();
            }
        }

        private void OnSequenceControlButtonPressed(SequenceControlMenuController.MenuButtonType type)
        {
            if (type == SequenceControlMenuController.MenuButtonType.PLAY)
            {
                timelineManager?.Play();
            }
            else if (type == SequenceControlMenuController.MenuButtonType.PAUSE)
            {
                timelineManager?.Pause();
            }
        }

        private void OnSliderStateChange(SliderStateEvent evt)
        {
            if (evt.State == SliderStateEvent.StateType.POINTER_DOWN)
                cameraController?.SetEnable(false);
            else if (evt.State == SliderStateEvent.StateType.POINTER_LEAVE)
            {
                cameraController?.SetEnable(true);

                //Load中の場合は現在のフレーム位置の優先順位を上げる
                dataManager.CurrentDataSets?.vehicleTimelineDataSet?.ChangePriority(timelineManager.GetCurrentTime());
                dataManager.CurrentDataSets?.roadIndicatorDataSet?.ChangePriority(timelineManager.GetCurrentTime());
            }
            else if (evt.State == SliderStateEvent.StateType.VALUE_CHANGE)
            {
                var percent = evt.Value / 100f; //Slider (0 - 100 )の場合
                timelineManager.Move(percent);
                mainMenucontroller.SequenceControl.SetDateTime(timelineManager.GetCurrentTime());
            }
        }

        private void OnTimelineUpdated(float percent, DateTime time)
        {
            mainMenucontroller.SequenceControl.SetSliderValue(percent * 100); //0 -100の場合
            mainMenucontroller.SequenceControl.SetDateTime(time);
        }

        private async void LoadVehicleTimeLine()
        {
            mainMenucontroller.LoadDataDialog.SetEnabled(false);
            var browser = new FileBrowser();

            var result = StandaloneFileBrowser.OpenFilePanel("Select a CSV file", "", extensions, false);

            if (result.Length != 0)
            {
                var data = await LoadCSV<VehicleTimeline, VehicleTimelineDataSet>(result[0]);
                dataManager.AddDataSet(data);
                mainMenucontroller.LoadDataDialog.SetVehicleText(data.name);
            }
            mainMenucontroller.LoadDataDialog.SetEnabled(true);
        }

        private async void LoadRoadIndicator()
        {
            mainMenucontroller.LoadDataDialog.SetEnabled(false);
            var browser = new FileBrowser();

            var result = StandaloneFileBrowser.OpenFilePanel("Select a CSV file", "", extensions, false);

            if (result.Length != 0)
            {
                var data = await LoadCSV<RoadIndicator, RoadIndicatorDataSet>(result[0]);
                dataManager.AddDataSet(data);
                mainMenucontroller.LoadDataDialog.SetRoadIndicatorText(data.name);
            }
            mainMenucontroller.LoadDataDialog.SetEnabled(true);
        }

        private async Task<U> LoadCSV<T, U>(string filePath) where T : class, new() where U : IDataSet, new()
        {
            var csvData = await Task.Run(() => CsvParser.ParseCsv<T>(filePath));
            U data = new();
            await Task.Run(() => data.Initialize(filePath, csvData.OfType<object>().ToList()));
            DebugLogger.Log(10, $"Data loaded : {filePath}");

            return data;
        }
    }
}