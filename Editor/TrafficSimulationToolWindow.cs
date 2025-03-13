using PLATEAU.CityInfo;
using PLATEAU.Geometries;
using PLATEAU.RoadNetwork.Data;
using PLATEAU.RoadNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using TrafficSimulationTool.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PLATEAU.RoadNetwork.Structure;

namespace TrafficSimulationTool.Editor
{
    public class TrafficSimulationToolWindow : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        private RoadNetworkDataGetter RoadNetworkGetter;

        private GeoReference GeoReferencer;

        private const string CRS = "urn:ogc:def:crs:EPSG::6697";

        private int selectedTab = 0;

        private ViewController[] controllers;

        [MenuItem("PLATEAU/交通シミュレーション支援ツール")]
        public static void Open()
        {
            var window = GetWindow<TrafficSimulationToolWindow>("交通シミュレーション支援ツール");

            window.Show();
        }

        public void CreateGUI()
        {
            Layers.CreateLayer(Runtime.FX.HighlightFX.FX_LAYER_CAR);
            Layers.CreateLayer(Runtime.FX.HighlightFX.FX_LAYER_TRAFFIC);

            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Instantiate UXML
            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();

            // Add to the root
            root.Add(labelFromUXML);

            // Menu と Content の要素を取得
            var menu = root.Q<VisualElement>("Menu");
            var content = root.Q<VisualElement>("ScrollView");

            // Menu の子要素を取得 (タブ要素)
            List<RadioButton> tabs = menu.Children().OfType<RadioButton>().ToList();

            // Content の子要素を取得
            List<VisualElement> contents = content.Children().ToList();

            // タブの切り替えイベントを登録
            for (int i = 0; i < tabs.Count; i++)
            {
                int index = i; // クリックイベントで使用するためのローカル変数

                tabs[i].RegisterCallback<ClickEvent>(evt =>
                {
                    contents[selectedTab].style.display = DisplayStyle.None;

                    controllers[selectedTab].OnDisable();

                    // インデックスに対応するコンテンツ要素を表示する
                    contents[index].style.display = DisplayStyle.Flex;

                    tabs[index].value = true;

                    controllers[index].OnEnable();

                    selectedTab = index;
                });

                // 一旦コンテンツをすべて非表示にする
                contents[i].style.display = DisplayStyle.None;
            }

            // 最初のタブを選択状態にする
            contents[selectedTab].style.display = DisplayStyle.Flex;
            tabs[selectedTab].value = true;

            var viewPoint = labelFromUXML.Q<VisualElement>("PointView");
            var viewSignal = labelFromUXML.Q<VisualElement>("SignalView");
            var viewEstimation = labelFromUXML.Q<VisualElement>("EstimationView");
            var viewExport = labelFromUXML.Q<VisualElement>("ExportView");

            var viewPointController = new PointViewController(viewPoint, this);
            var viewSignalController = new SignalViewController(viewSignal, this);
            var viewEstimationController = new EstimationViewController(viewEstimation, this);
            var viewExportController = new ExportViewController(viewExport, this);

            controllers = new ViewController[] { viewPointController, viewSignalController, viewEstimationController, viewExportController };

            controllers[selectedTab].OnEnable();
        }

        private void OnDisable()
        {
            if (controllers == null)
            {
                return;
            }

            controllers[selectedTab].OnDisable();
        }
    }
}