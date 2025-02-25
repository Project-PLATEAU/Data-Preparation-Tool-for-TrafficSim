using PLATEAU.CityInfo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TrafficSimulationTool.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TrafficSimulationTool.Editor
{
    /// <summary>
    /// 交通量推定タブのコントローラ
    /// </summary>
    public class EstimationViewController : ViewController
    {
        public const string TrafficVolumeFileName = "IF205_traffic_volume.csv";

        public const string IF103FileName = "IF103_estgnr.csv"; // .csv は除外（モジュール仕様）

        public const string IF104FileName = "IF104_dev.csv";

        private readonly Dictionary<string, int> dropdownPopulation = new Dictionary<string, int>()
        {
            { "10万人未満", 1 },
            { "10万人以上～40万人未満", 2 },
            { "40万人以上～100万人未満", 3 },
            { "100万人以上", 4 },
        };

        private readonly Dictionary<string, int> dropdownAreaCategory = new Dictionary<string, int>()
        {
            { "商業地区", 1 },
            { "その他地区", 2 },
        };

        private readonly Dictionary<string, int> dropdownPeekTime = new Dictionary<string, int>()
        {
            { "0時", 0 },
            { "1時", 1 },
            { "2時", 2 },
            { "3時", 3 },
            { "4時", 4 },
            { "5時", 5 },
            { "6時", 6 },
            { "7時", 7 },
            { "8時", 8 },
            { "9時", 9 },
            { "10時", 10 },
            { "11時", 11 },
            { "12時", 12 },
            { "13時", 13 },
            { "14時", 14 },
            { "15時", 15 },
            { "16時", 16 },
            { "17時", 17 },
            { "18時", 18 },
            { "19時", 19 },
            { "20時", 20 },
            { "21時", 21 },
            { "22時", 22 },
            { "23時", 23 },
        };

        private class DevelopmentSettingModel
        {
            public float Area { get; set; }

            public int Population { get; set; }

            public float StationDistance { get; set; }

            public int AreaCategory { get; set; }

            public int PeekTime { get; set; }

            public float StandardDeviation { get; set; }

            public float PeekRate { get; set; }

            public float[] Rates { get; set; } = new float[24];

            public bool IsSimpleSetting { get; set; }

            public float StayTime { get; set; }

            public string ZoneSeqDev { get; set; }

            public string ZoneSeqEx { get; set; }

            public string IF103Path { get; set; }

            public string IF104Path { get; set; }

            /// <summary>
            /// ファイル名とCSV文字列のペアを返す
            /// </summary>
            /// <returns></returns>
            public Dictionary<string, string> GetCSVs()
            {
                // ファイル名とCSV文字列のペア
                Dictionary<string, string> ret = new Dictionary<string, string>()
                {
                    { "Area.csv", $"{Area}" }, // 面積
                    { "Pop_Classification.csv", $"{Population}" }, // 人口
                    { "Sta_Dist.csv", $"{StationDistance}" }, // 最寄り駅の距離
                    { "Use_Classification.csv", $"{AreaCategory}" }, // 地域区分

                    { "Ave_stay_time.csv", $"{StayTime}" },
                    { "Distribution.csv", IsSimpleSetting ? $"{PeekTime},{StandardDeviation},{PeekRate}" : $"{string.Join(",", Rates)}" }, // 詳細設定 or 簡易設定

                    { "Zone_Seq_Dev.csv", $"{ZoneSeqDev}" }, // 開発ゾーン
                    { "Zone_Seq_Ex.csv", $"{ZoneSeqEx}" }, // 参照ゾーン

                    { "IF103.csv", $"{IF103Path}" }, // IF207の入力
                    { "IF104.csv", $"{IF104Path}" }, // IF209の出力
                };

                return ret;
            }

            public string ImportPath { get; set; }
        }

        private DevelopmentSettingModel developmentSettingModel;

        private class ExistingSettingModel
        {
            public string RoadNetworkPath { get; set; }

            public string TrafficVolumePath { get; set; }

            public string ExportPath { get; set; }

            public DateTime StartTime { get; set; }

            public DateTime EndTime { get; set; }
        }

        private ExistingSettingModel existingSettingModel;

        private class ConditionsSettingModel
        {
            // シミュレーション日
            public readonly Dictionary<string, int> dropdownPresetDay = new Dictionary<string, int>()
            {
                { "平日", 0 },
                { "休日", 1 },
            };

            // 発生集中原単位（業務施設）
            public readonly Dictionary<string, int> dropdownPresetBusiness = new Dictionary<string, int>()
            {
                { "都心部", 0 },
                { "周辺部", 1 },
            };

            // 発生集中原単位（商業施設）
            public readonly Dictionary<string, int> dropdownPresetCommerce = new Dictionary<string, int>()
            {
                { "三大都市圏中心部", 0 },
                { "三大都市圏郊外部及び地方中枢都市", 1 },
                { "三大都市圏周辺部及び地方都市", 2 },
            };

            // 自動車分担率
            public readonly Dictionary<string, int> dropdownPresetShare = new Dictionary<string, int>()
            {
                { "三大都市圏", 0 },
                { "地方都市圏", 1 },
            };

            // プリセット：シミュレーション日
            public int PresetDay { get; set; }

            // プリセット：発生集中原単位（業務施設）
            public int PresetBusiness { get; set; }

            // プリセット：発生集中原単位（商業施設）
            public int PresetCommerce { get; set; }

            // プリセット：自動車分担率
            public int PresetShare { get; set; }

            // 発生集中原単位（業務施設） [人T.E./ha・日]
            private readonly float[] tableBusinessUnit = new float[] { 3800.0f, 3300.0f }; // 都心部／周辺部

            // 発生集中原単位（商業施設） [人T.E./ha・日]
            private readonly float[,] tableCommerceUnit = new float[,] { { 20600f, 11600f, 10600f }, { 21800f, 18600f, 16100f } }; // 平日／祝日, 三大都市圏中心部／三大都市圏郊外部及び地方中枢都市／三大都市圏周辺部及び地方都市

            // 発生集中原単位（住宅）　[人T.E./ha・日]
            private readonly float tableHousing = 700f; //

            // 自動車分担率 [％]
            private readonly float[,] tableShare = new float[,] { { 32.0f, 61.1f }, { 48.6f, 75.9f } }; // 平日／祝日, 三大都市圏／地方都市圏

            // 台換算係数 [人/台]
            private readonly float[] tableFactor = new float[] { 1.3f, 1.5f, 1.4f }; // 業務施設／商業施設／住宅

            public enum TrafficType
            {
                Business, // 業務施設
                Commerce, // 商業施設
                Housing, // 住宅
                All,
            }

            public enum SettingType
            {
                Unit, // 発生集中原単位
                Share, // 自動車分担率
                Factor, // 台換算係数
                All,
            }

            private float[,] TrafficVolumeSetting = new float[(int)SettingType.All, (int)TrafficType.All];

            public void SetTrafficVolumeSetting(SettingType setting, TrafficType type, float value)
            {
                TrafficVolumeSetting[(int)setting, (int)type] = value;
            }

            public float GetTrafficVolumeSetting(SettingType setting, TrafficType type)
            {
                return TrafficVolumeSetting[(int)setting, (int)type];
            }

            public void ReflectPreset()
            {
                SetTrafficVolumeSetting(SettingType.Unit, TrafficType.Business, tableBusinessUnit[PresetBusiness]);
                SetTrafficVolumeSetting(SettingType.Unit, TrafficType.Commerce, tableCommerceUnit[PresetDay, PresetCommerce]);
                SetTrafficVolumeSetting(SettingType.Unit, TrafficType.Housing, tableHousing);

                SetTrafficVolumeSetting(SettingType.Share, TrafficType.Business, tableShare[PresetDay, PresetShare]);
                SetTrafficVolumeSetting(SettingType.Share, TrafficType.Commerce, tableShare[PresetDay, PresetShare]);
                SetTrafficVolumeSetting(SettingType.Share, TrafficType.Housing, tableShare[PresetDay, PresetShare]);

                SetTrafficVolumeSetting(SettingType.Factor, TrafficType.Business, tableFactor[(int)TrafficType.Business]);
                SetTrafficVolumeSetting(SettingType.Factor, TrafficType.Commerce, tableFactor[(int)TrafficType.Commerce]);
                SetTrafficVolumeSetting(SettingType.Factor, TrafficType.Housing, tableFactor[(int)TrafficType.Housing]);
            }
        }

        private ConditionsSettingModel conditionsSettingModel;

        private TextField[,] conditionsSettings = new TextField[(int)ConditionsSettingModel.SettingType.All, (int)ConditionsSettingModel.TrafficType.All];

        private List<Slider> destinationRateSliders;

        private List<Slider> occurrenceRateSliders;

        private static readonly float FloorHeight = 3.0f; // 1階あたりの階高

        private int timeResolution = 15;

        private SimZone SelectDevZone;

        private SimZone SelectRefZone;

        private static readonly Color colorZone = new Color(0.0f, 0.0f, 1.0f, 0.7f);
        private static readonly Color colorSelectZone = new Color(1.0f, 0.0f, 0.0f, 0.7f);

        private Mesh CustomHandleMesh;

        private const string DateTimeFormat = "yyyyMMddHHmm";

        private Texture2D zoneIconTexture;

        private static readonly Vector2 iconSize = new Vector2(32f, 32f);

        public EstimationViewController(VisualElement element, TrafficSimulationToolWindow parent) : base(element, parent)
        {
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected override void Initialize()
        {
            // 開発OD交通量推定
            developmentSettingModel = new DevelopmentSettingModel();

            // 既存OD交通量推定（シミュレーション条件）
            conditionsSettingModel = new ConditionsSettingModel();

            // 開発ゾーンの指定
            var devZoneName = rootElement.Q<TextField>("DevZoneName");

            devZoneName.SetEnabled(false);

            var buttonDevZoneSelect = rootElement.Q<Button>("ButtonDevZoneSelect");

            buttonDevZoneSelect.clickable.clicked += () =>
            {
                DisableZoneSelect();
                SceneView.duringSceneGui += OnSelectDevZoneGUI;
            };

            // 参照ゾーンの指定
            var refZoneName = rootElement.Q<TextField>("RefZoneName");

            refZoneName.SetEnabled(false);

            var buttonRefZoneSelect = rootElement.Q<Button>("ButtonRefZoneSelect");

            buttonRefZoneSelect.clickable.clicked += () =>
            {
                DisableZoneSelect();
                SceneView.duringSceneGui += OnSelectRefZoneGUI;
            };

            // 既存OD交通量
            var devImport = rootElement.Q<VisualElement>("DevImport");

            var devImportPath = devImport.Children().OfType<TextField>().First();

            devImportPath.SetEnabled(false);

            devImport.Children().OfType<Button>().First().RegisterCallback<ClickEvent>(evt =>
            {
                var path = Application.persistentDataPath;

                path = EditorUtility.OpenFolderPanel("Select Import Folder", path, "");

                devImportPath.value = path;

                developmentSettingModel.ImportPath = path;
            });

            // 面積
            var areaField = rootElement.Q<TextField>("AreaSetting");

            areaField.RegisterValueChangedCallback(evt =>
            {
                if (float.TryParse(evt.newValue, out float result))
                {
                    developmentSettingModel.Area = result;
                }
                else
                {
                    // TODO: エラー時処理
                }
            });

            areaField.value = "0.0"; // 初期値

            // 人口
            var populationDropdown = rootElement.Q<DropdownField>("PopulationSetting");

            populationDropdown.RegisterValueChangedCallback(evt =>
            {
                if (dropdownPopulation.TryGetValue(evt.newValue, out int result))
                {
                    developmentSettingModel.Population = result;
                }
            });

            populationDropdown.choices = dropdownPopulation.Keys.ToList();

            populationDropdown.value = populationDropdown.choices[0]; // 初期値

            // 最寄り駅の距離
            var stationDistanceField = rootElement.Q<TextField>("StationSetting");

            stationDistanceField.RegisterValueChangedCallback(evt =>
            {
                if (float.TryParse(evt.newValue, out float result))
                {
                    developmentSettingModel.StationDistance = result;
                }
                else
                {
                    // TODO: エラー時処理
                }
            });

            stationDistanceField.value = "0.0"; // 初期値

            // 地域区分
            var areaCategoryDropdown = rootElement.Q<DropdownField>("AreaCategorySetting");

            areaCategoryDropdown.RegisterValueChangedCallback(evt =>
            {
                if (dropdownAreaCategory.TryGetValue(evt.newValue, out int result))
                {
                    developmentSettingModel.AreaCategory = result;
                }
            });

            areaCategoryDropdown.choices = dropdownAreaCategory.Keys.ToList();

            areaCategoryDropdown.value = areaCategoryDropdown.choices[0]; // 初期値

            // 滞在時間
            var stayTime = rootElement.Q<TextField>("StayTime");

            stayTime.RegisterValueChangedCallback(evt =>
            {
                if (float.TryParse(evt.newValue, out float result))
                {
                    developmentSettingModel.StayTime = result;
                }
                else
                {
                    // TODO: エラー時処理
                }
            });

            stayTime.value = "0.0"; // 初期値

            // 簡易設定と詳細設定の切り替え
            var tabParent = rootElement.Q<VisualElement>("TimeSettingTab");

            var tabs = tabParent.Children().OfType<RadioButton>().ToList();

            var contents = new List<VisualElement>()
            {
                rootElement.Q<VisualElement>("SimpleSettingPanel"),
                rootElement.Q<VisualElement>("DetailSettingPanel")
            };

            for (int i = 0; i < tabs.Count; i++)
            {
                var index = i;

                tabs[i].RegisterCallback<ClickEvent>(evt =>
                {
                    for (int j = 0; j < tabs.Count; j++)
                    {
                        var isMath = index == j;

                        tabs[j].value = isMath;

                        contents[j].style.display = isMath ? DisplayStyle.Flex : DisplayStyle.None;
                    }

                    developmentSettingModel.IsSimpleSetting = index == 0;
                });

                contents[i].style.display = DisplayStyle.None;
            }

            developmentSettingModel.IsSimpleSetting = true; // 初期値

            tabs[0].value = true;
            contents[0].style.display = DisplayStyle.Flex;

            // 簡易設定

            // ピーク時間
            var peekTimeDropdown = rootElement.Q<DropdownField>("PeakTimeSetting");

            peekTimeDropdown.RegisterValueChangedCallback(evt =>
            {
                if (dropdownPeekTime.TryGetValue(evt.newValue, out int result))
                {
                    developmentSettingModel.PeekTime = result;
                }
            });

            peekTimeDropdown.choices = dropdownPeekTime.Keys.ToList();

            peekTimeDropdown.value = peekTimeDropdown.choices[0]; // 初期値

            // 標準偏差
            var standardDeviationField = rootElement.Q<TextField>("DefaultTimeSetting");

            standardDeviationField.RegisterValueChangedCallback(evt =>
            {
                if (float.TryParse(evt.newValue, out float result))
                {
                    developmentSettingModel.StandardDeviation = result;
                }
                else
                {
                    // TODO: エラー時処理
                }
            });

            standardDeviationField.value = "9"; // 初期値

            // ピーク率
            var peekRateField = rootElement.Q<TextField>("PeakSetting");

            peekRateField.RegisterValueChangedCallback(evt =>
            {
                if (float.TryParse(evt.newValue, out float result))
                {
                    developmentSettingModel.PeekRate = result;
                }
                else
                {
                    // TODO: エラー時処理
                }
            });

            peekRateField.value = "20"; // 初期値

            // 詳細設定

            var rateSliders = rootElement.Q<VisualElement>("DetailSettingPanel").Children().OfType<Slider>().ToList();

            // スライダーの合計が1になるように均等に割り振る
            rateSliders.ForEach(slider =>
            {
                slider.RegisterValueChangedCallback(evt =>
                {
                    // 変更前の合計を取得
                    var previousSum = rateSliders.Sum(s => s.value);

                    // 現在のスライダーの変化量
                    var delta = evt.newValue - evt.previousValue;

                    // スライダーの合計が1を超えた場合
                    if (previousSum + delta > 1.0f)
                    {
                        var excess = previousSum + delta - 1.0f; // 超過分

                        // 他のスライダーに超過分を分配する
                        var slidersToAdjust = rateSliders.Where(s => s != slider).ToList();
                        DistributeDeltaToSliders(slidersToAdjust, -excess);
                    }
                    // スライダーの合計が1に満たない場合
                    else if (previousSum + delta < 1.0f)
                    {
                        var shortage = 1.0f - (previousSum + delta); // 不足分

                        // 他のスライダーに不足分を分配する
                        var slidersToAdjust = rateSliders.Where(s => s != slider).ToList();
                        DistributeDeltaToSliders(slidersToAdjust, shortage);
                    }

                    developmentSettingModel.Rates = rateSliders.Select(s => s.value).ToArray(); // 反映
                });

                slider.value = 1.0f / rateSliders.Count; // 初期値
            });

            // 実行
            rootElement.Query<VisualElement>("EstimateDevelopmenButton")
                .ToList()
                .ForEach(button =>
                {
                    button.RegisterCallback<ClickEvent>(evt =>
                    {
                        EstimateODTrafficDevelopment();
                    });
                });

            // OD交通量推定

            existingSettingModel = new ExistingSettingModel();

            // 道路ネットワーク
            var importNetwork = rootElement.Q<VisualElement>("Import_Network");

            var importNetworkPath = importNetwork.Children().OfType<TextField>().First();

            importNetworkPath.SetEnabled(false);

            importNetwork.Children().OfType<Button>().First().RegisterCallback<ClickEvent>(evt =>
            {
                var path = Application.persistentDataPath;

                path = EditorUtility.OpenFolderPanel("Select Export Folder", path, "");

                importNetworkPath.value = path;

                existingSettingModel.RoadNetworkPath = path;
            });

            // 断面交通量
            var importRoute = rootElement.Q<VisualElement>("Import_TrafficVolume");

            var importRoutePath = importRoute.Children().OfType<TextField>().First();

            importRoutePath.SetEnabled(false);

            importRoute.Children().OfType<Button>().First().RegisterCallback<ClickEvent>(evt =>
            {
                var path = Application.persistentDataPath;

                path = EditorUtility.OpenFolderPanel("Select Export Folder", path, "");

                importRoutePath.value = path;

                existingSettingModel.TrafficVolumePath = path;
            });

            // 出力先
            var exportPath = rootElement.Q<VisualElement>("Export_File");

            var exportPathField = exportPath.Children().OfType<TextField>().First();

            exportPathField.SetEnabled(false);

            exportPath.Children().OfType<Button>().First().RegisterCallback<ClickEvent>(evt =>
            {
                var path = Application.persistentDataPath;

                path = EditorUtility.OpenFolderPanel("Select Export Folder", path, "");

                exportPathField.value = path;

                existingSettingModel.ExportPath = path;
            });

            // シミュレーション条件
            var conditionsPanel = rootElement.Q<VisualElement>("ConditionsPanel");

            var presetConditionDay = conditionsPanel.Q<DropdownField>("PresetConditionDay");

            presetConditionDay.choices = conditionsSettingModel.dropdownPresetDay.Keys.ToList();

            presetConditionDay.value = conditionsSettingModel.dropdownPresetDay.Keys.First(); // 初期値

            presetConditionDay.RegisterValueChangedCallback(evt =>
            {
                if (conditionsSettingModel.dropdownPresetDay.TryGetValue(evt.newValue, out int result))
                {
                    conditionsSettingModel.PresetDay = result;

                    conditionsSettingModel.ReflectPreset();

                    UpdateConditions();
                }
            });

            var presetConditionBusiness = conditionsPanel.Q<DropdownField>("PresetConditionBusiness");

            presetConditionBusiness.choices = conditionsSettingModel.dropdownPresetBusiness.Keys.ToList();

            presetConditionBusiness.value = conditionsSettingModel.dropdownPresetBusiness.Keys.First(); // 初期値

            presetConditionBusiness.RegisterValueChangedCallback(evt =>
            {
                if (conditionsSettingModel.dropdownPresetBusiness.TryGetValue(evt.newValue, out int result))
                {
                    conditionsSettingModel.PresetBusiness = result;

                    conditionsSettingModel.ReflectPreset();

                    UpdateConditions();
                }
            });

            var presetConditionCommerce = conditionsPanel.Q<DropdownField>("PresetConditionCommerce");

            presetConditionCommerce.choices = conditionsSettingModel.dropdownPresetCommerce.Keys.ToList();

            presetConditionCommerce.value = conditionsSettingModel.dropdownPresetCommerce.Keys.First(); // 初期値

            presetConditionCommerce.RegisterValueChangedCallback(evt =>
            {
                if (conditionsSettingModel.dropdownPresetCommerce.TryGetValue(evt.newValue, out int result))
                {
                    conditionsSettingModel.PresetCommerce = result;

                    conditionsSettingModel.ReflectPreset();

                    UpdateConditions();
                }
            });

            var presetConditionShare = conditionsPanel.Q<DropdownField>("PresetConditionShare");

            presetConditionShare.choices = conditionsSettingModel.dropdownPresetShare.Keys.ToList();

            presetConditionShare.value = conditionsSettingModel.dropdownPresetShare.Keys.First(); // 初期値

            presetConditionShare.RegisterValueChangedCallback(evt =>
            {
                if (conditionsSettingModel.dropdownPresetShare.TryGetValue(evt.newValue, out int result))
                {
                    conditionsSettingModel.PresetShare = result;

                    conditionsSettingModel.ReflectPreset();

                    UpdateConditions();
                }
            });

            conditionsSettingModel.ReflectPreset();

            conditionsSettings[(int)ConditionsSettingModel.SettingType.Unit, (int)ConditionsSettingModel.TrafficType.Business] = conditionsPanel.Q<TextField>("ConditionBusinessUnit");
            conditionsSettings[(int)ConditionsSettingModel.SettingType.Unit, (int)ConditionsSettingModel.TrafficType.Commerce] = conditionsPanel.Q<TextField>("ConditionCommerceUnit");
            conditionsSettings[(int)ConditionsSettingModel.SettingType.Unit, (int)ConditionsSettingModel.TrafficType.Housing] = conditionsPanel.Q<TextField>("ConditionShareUnit");

            conditionsSettings[(int)ConditionsSettingModel.SettingType.Share, (int)ConditionsSettingModel.TrafficType.Business] = conditionsPanel.Q<TextField>("ConditionBusinessRate");
            conditionsSettings[(int)ConditionsSettingModel.SettingType.Share, (int)ConditionsSettingModel.TrafficType.Commerce] = conditionsPanel.Q<TextField>("ConditionCommerceRate");
            conditionsSettings[(int)ConditionsSettingModel.SettingType.Share, (int)ConditionsSettingModel.TrafficType.Housing] = conditionsPanel.Q<TextField>("ConditionShareRate");

            conditionsSettings[(int)ConditionsSettingModel.SettingType.Factor, (int)ConditionsSettingModel.TrafficType.Business] = conditionsPanel.Q<TextField>("ConditionBusinessFactor");
            conditionsSettings[(int)ConditionsSettingModel.SettingType.Factor, (int)ConditionsSettingModel.TrafficType.Commerce] = conditionsPanel.Q<TextField>("ConditionCommerceFactor");
            conditionsSettings[(int)ConditionsSettingModel.SettingType.Factor, (int)ConditionsSettingModel.TrafficType.Housing] = conditionsPanel.Q<TextField>("ConditionShareFactor");

            UpdateConditions(); // 初期値

            destinationRateSliders = rootElement.Q<VisualElement>("DestinationRateSliders").Children().OfType<Slider>().ToList();

            float[] initDestinationRate = { 0.025f, 0.025f, 0.025f, 0.025f, 0.025f, 0.025f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.025f, 0.025f, 0.025f };

            int initDestinationIndex = 0;

            // スライダーの合計が1になるように均等に割り振る
            destinationRateSliders.ForEach(slider =>
            {
                slider.value = initDestinationRate[initDestinationIndex++]; // 初期値

                // 各スライダーの値が非常に小さくなり操作しづらいため、最大値と最小値を設定
                slider.highValue = 1.0f; // 最大値
                slider.lowValue = 0.0f; // 最小値

                slider.RegisterValueChangedCallback(evt =>
                {
                    // 変更前の合計を取得
                    var previousSum = destinationRateSliders.Sum(s => s.value);

                    // 現在のスライダーの変化量
                    var delta = evt.newValue - evt.previousValue;

                    // スライダーの合計が1を超えた場合
                    if (previousSum + delta > 1.0f)
                    {
                        var excess = previousSum + delta - 1.0f; // 超過分

                        // 他のスライダーに超過分を分配する
                        var slidersToAdjust = destinationRateSliders.Where(s => s != slider).ToList();
                        DistributeDeltaToSliders(slidersToAdjust, -excess);
                    }
                    // スライダーの合計が1に満たない場合
                    else if (previousSum + delta < 1.0f)
                    {
                        var shortage = 1.0f - (previousSum + delta); // 不足分

                        // 他のスライダーに不足分を分配する
                        var slidersToAdjust = destinationRateSliders.Where(s => s != slider).ToList();
                        DistributeDeltaToSliders(slidersToAdjust, shortage);
                    }
                });
            });

            occurrenceRateSliders = rootElement.Q<VisualElement>("OccurrenceRateSliders").Children().OfType<Slider>().ToList();

            float[] initOccurrenceRate = { 0.025f, 0.025f, 0.025f, 0.025f, 0.025f, 0.025f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.025f, 0.025f, 0.025f };

            int initOccurrenceIndex = 0;

            // スライダーの合計が1になるように均等に割り振る
            occurrenceRateSliders.ForEach(slider =>
            {
                slider.value = initOccurrenceRate[initOccurrenceIndex++]; // 初期値

                // 各スライダーの値が非常に小さくなり操作しづらいため、最大値と最小値を設定
                slider.highValue = 1.0f; // 最大値
                slider.lowValue = 0.0f; // 最小値

                slider.RegisterValueChangedCallback(evt =>
                {
                    // 変更前の合計を取得
                    var previousSum = occurrenceRateSliders.Sum(s => s.value);

                    // 現在のスライダーの変化量
                    var delta = evt.newValue - evt.previousValue;

                    // スライダーの合計が1を超えた場合
                    if (previousSum + delta > 1.0f)
                    {
                        var excess = previousSum + delta - 1.0f; // 超過分

                        // 他のスライダーに超過分を分配する
                        var slidersToAdjust = occurrenceRateSliders.Where(s => s != slider).ToList();
                        DistributeDeltaToSliders(slidersToAdjust, -excess);
                    }
                    // スライダーの合計が1に満たない場合
                    else if (previousSum + delta < 1.0f)
                    {
                        var shortage = 1.0f - (previousSum + delta); // 不足分

                        // 他のスライダーに不足分を分配する
                        var slidersToAdjust = occurrenceRateSliders.Where(s => s != slider).ToList();
                        DistributeDeltaToSliders(slidersToAdjust, shortage);
                    }
                });
            });

            // 時間解像度
            var TimeResolutionField = rootElement.Q<TextField>("TimeResolution");

            TimeResolutionField.value = "15"; // 初期値

            TimeResolutionField.RegisterValueChangedCallback(evt =>
            {
                if (int.TryParse(evt.newValue, out int result))
                {
                    if (result > 60)
                    {
                        TimeResolutionField.value = "60";　// 60分を超える場合は60分に設定
                    }
                    else if (result < 1)
                    {
                        TimeResolutionField.value = "1";　// 1分未満の場合は1分に設定
                    }

                    timeResolution = result;
                }
                else
                {
                    // TODO: エラー時処理
                }
            });

            // 開始日時
            var startTimePanel = rootElement.Q<VisualElement>("StartTimePanel");

            var startTimeFields = startTimePanel.Query<TextField>().ToList();

            DateTime startTime = new DateTime(2024, 9, 1, 0, 0, 0); // 初期値

            existingSettingModel.StartTime = startTime;

            // 対応するDateTimeの要素を配列にまとめる
            var startTimeParts = new string[]
            {
                startTime.Year.ToString("0000"),  // 年
                startTime.Month.ToString("00"),   // 月
                startTime.Day.ToString("00"),     // 日
                startTime.Hour.ToString("00"),    // 時
                startTime.Minute.ToString("00")   // 分
            };

            startTimeFields.ForEach(startTimeField =>
            {
                int index = startTimeFields.IndexOf(startTimeField);

                startTimeField.value = startTimeParts[index]; // 初期値

                startTimeField.RegisterCallback<FocusOutEvent>(evt =>
                {
                    var concatenatedString = string.Concat(startTimeFields.Select(f => f.value));

                    if (DateTime.TryParseExact(concatenatedString, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                    {
                        existingSettingModel.StartTime = result;
                    }
                    else
                    {
                        // TODO: エラー時処理
                    }
                });
            });

            // 終了日時
            var endTimePanel = rootElement.Q<VisualElement>("EndTimePanel");

            var endTimeFields = endTimePanel.Query<TextField>().ToList();

            DateTime endTime = new DateTime(2024, 9, 2, 0, 0, 0); // 初期値

            existingSettingModel.EndTime = endTime;

            // 対応するDateTimeの要素を配列にまとめる
            var endTimeParts = new string[]
            {
                endTime.Year.ToString("0000"),  // 年
                endTime.Month.ToString("00"),   // 月
                endTime.Day.ToString("00"),     // 日
                endTime.Hour.ToString("00"),    // 時
                endTime.Minute.ToString("00")   // 分
            };

            endTimeFields.ForEach(endTimeField =>
            {
                int index = endTimeFields.IndexOf(endTimeField);

                endTimeField.value = endTimeParts[index]; // 初期値

                endTimeField.RegisterCallback<FocusOutEvent>(evt =>
                {
                    var concatenatedString = string.Concat(endTimeFields.Select(f => f.value));

                    if (DateTime.TryParseExact(concatenatedString, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                    {
                        existingSettingModel.EndTime = result;
                    }
                    else
                    {
                        // TODO: エラー時処理
                    }
                });
            });

            // 実行
            rootElement.Q<VisualElement>("EstimateExistingButton").RegisterCallback<ClickEvent>(evt =>
            {
                EstimateODTrafficExisting();
            });

            if (zoneIconTexture == null)
            {
                zoneIconTexture = TrafficSimulationToolUtility.GetAssetRelativePath<Texture2D>("Images/Icon/handle_zone.png");
            }
        }

        public override void OnDisable()
        {
            DisableZoneSelect();
        }

        private void DisableZoneSelect()
        {
            SceneView.duringSceneGui -= OnSelectDevZoneGUI;
            SceneView.duringSceneGui -= OnSelectRefZoneGUI;
        }

        private void OnSelectDevZoneGUI(SceneView sceneView)
        {
            var zones = GameObject.FindObjectsOfType<TrafficFocusGroup>();

            foreach (var zone in zones)
            {
                MeshFilter meshFilter = zone.gameObject.GetComponent<MeshFilter>();
                Mesh mesh = meshFilter.sharedMesh;

                var center = zone.transform.position;
                var quaternion = zone.transform.rotation;
                var size = 1.0f;

                var isSelected = SelectDevZone == zone;

                Handles.color = colorZone;// isSelected ? colorSelectZone : colorZone;

                CustomHandleMesh = mesh;

                var iconCenter = mesh.bounds.center;

                // ハンドルの描画＆クリック判定
                if (Handles.Button(iconCenter, Quaternion.identity, iconSize.x, iconSize.y, (id, pos, rot, s, e) => CustomIconHandleCap(id, pos, rot, s, e, zoneIconTexture, mesh, center, quaternion, size)))
                {
                    SelectDevZone = zone;

                    // 開発ゾーンの設定
                    var roadNetworkManager = GameObject.FindObjectOfType<SimRoadNetworkManager>();

                    if (roadNetworkManager != null)
                    {
                        foreach (var trafficFocusGroup in roadNetworkManager.TrafficFocusGroups)
                        {
                            trafficFocusGroup.IsDevelopArea = false;
                        }
                    }

                    zone.IsDevelopArea = true;

                    // エリア選択モードを終了
                    SceneView.duringSceneGui -= OnSelectDevZoneGUI;

                    var devZoneName = rootElement.Q<TextField>("DevZoneName");

                    if (SelectDevZone != null)
                    {
                        devZoneName.value = SelectDevZone.GetComponent<SimZone>().ID;
                    }
                }
            }

            Event e = Event.current;

            if (e.type == EventType.MouseDown)
            {
                switch (e.button)
                {
                    case 1:
                        // エリア選択モードを終了
                        SceneView.duringSceneGui -= OnSelectDevZoneGUI;
                        break;
                }
            }
        }

        private void OnSelectRefZoneGUI(SceneView sceneView)
        {
            var zones = GameObject.FindObjectsOfType<TrafficFocusGroup>();

            foreach (var zone in zones)
            {
                MeshFilter meshFilter = zone.gameObject.GetComponent<MeshFilter>();
                Mesh mesh = meshFilter.sharedMesh;

                var center = zone.transform.position;
                var quaternion = zone.transform.rotation;
                var size = 1.0f;

                var isSelected = SelectRefZone == zone;

                Handles.color = colorZone;// isSelected ? colorSelectZone : colorZone;

                CustomHandleMesh = mesh;

                var iconCenter = mesh.bounds.center;

                // ハンドルの描画＆クリック判定
                if (Handles.Button(iconCenter, Quaternion.identity, iconSize.x, iconSize.y, (id, pos, rot, s, e) => CustomIconHandleCap(id, pos, rot, s, e, zoneIconTexture, mesh, center, quaternion, size)))
                {
                    SelectRefZone = zone;

                    // エリア選択モードを終了
                    SceneView.duringSceneGui -= OnSelectRefZoneGUI;

                    var devZoneName = rootElement.Q<TextField>("RefZoneName");

                    if (SelectRefZone != null)
                    {
                        devZoneName.value = SelectRefZone.GetComponent<SimZone>().ID;
                    }
                }
            }

            Event e = Event.current;

            if (e.type == EventType.MouseDown)
            {
                switch (e.button)
                {
                    case 1:
                        // エリア選択モードを終了
                        SceneView.duringSceneGui -= OnSelectRefZoneGUI;
                        break;
                }
            }
        }

        /// <summary>
        /// カスタムアイコンハンドルの描画
        /// </summary>
        /// <param name="controlID"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="size"></param>
        /// <param name="eventType"></param>
        /// <param name="icon"></param>
        private void CustomIconHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType, Texture2D icon, Mesh mesh, Vector3 positionMesh, Quaternion rotationMesh, float sizeMesh)
        {
            switch (eventType)
            {
                case EventType.MouseMove:
                case EventType.Layout:
                    UnityEditor.HandleUtility.AddControl(controlID, UnityEditor.HandleUtility.DistanceToCircle(position, size * 0.5f));
                    break;

                case EventType.Repaint:

                    // マウスオーバー時のみ描画する場合
                    //if (HandleUtility.nearestControl == controlID)
                    //{
                    //    Graphics.DrawMeshNow(mesh, StartCapDraw(position2, rotation2, size2));
                    //}

                    Graphics.DrawMeshNow(mesh, StartCapDraw(positionMesh, rotationMesh, sizeMesh));

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

        /// <summary>
        ///
        /// </summary>
        /// <param name="sceneView"></param>
        private void OnSelectAreaGUI(SceneView sceneView)
        {
            Event e = Event.current;

            UnityEditor.HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            var zones = GameObject.FindObjectsOfType<SimZone>();

            foreach (var zone in zones)
            {
                MeshFilter meshFilter = zone.gameObject.GetComponent<MeshFilter>();
                Mesh mesh = meshFilter.sharedMesh;

                var center = zone.transform.position;
                var quaternion = zone.transform.rotation;
                var size = 1.0f;

                if (zone as TrafficFocusGroup == false)
                {
                    continue;
                }

                var isSelected = SelectDevZone == zone;

                Handles.color = isSelected ? colorSelectZone : colorZone;

                CustomHandleMesh = mesh;

                // ハンドルの描画＆クリック判定
                if (Handles.Button(center, quaternion, size, size, CustomMeshHandleCap))
                {
                    SelectDevZone = zone;

                    // エリア選択モードを終了
                    SceneView.duringSceneGui -= OnSelectAreaGUI;

                    var devZoneName = rootElement.Q<TextField>("DevZoneName");

                    if (SelectDevZone != null)
                    {
                        devZoneName.value = SelectDevZone.GetComponent<SimZone>().ID;
                    }
                }
            }

            if (e.type == EventType.MouseDown)
            {
                switch (e.button)
                {
                    case 1:
                        // エリア選択モードを終了
                        SceneView.duringSceneGui -= OnSelectAreaGUI;
                        break;
                }
            }
        }

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
                    UnityEditor.HandleUtility.AddControl(controlID, PointViewController.DistanceToMesh(CustomHandleMesh, position, rotation, size));
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
        /// スライダーにdeltaを均等に分配する
        /// </summary>
        /// <param name="sliders"></param>
        /// <param name="delta"></param>
        private void DistributeDeltaToSliders(List<Slider> sliders, float delta)
        {
            if (sliders.Count == 0) return;

            // 非常に小さなdeltaの場合は無視する (浮動小数点の誤差対策)
            if (Mathf.Abs(delta) < 0.001f) return;

            // スライダーの現在の値と最小値を考慮してdeltaを分配
            var availableSliders = sliders.Where(s => (delta < 0 && s.value > 0) || (delta > 0 && s.value < 1)).ToList();

            if (availableSliders.Count == 0) return;

            // 分配する量を各スライダーに均等に割り振る
            float distributePerSlider = delta / availableSliders.Count;

            // スライダーの値を変更
            foreach (var slider in availableSliders)
            {
                var value = Mathf.Clamp(slider.value + distributePerSlider, 0, 1); // 0～1の範囲に制約

                // 通知を抑制して値を設定
                slider.SetValueWithoutNotify(value);
            }
        }

        /// <summary>
        /// 条件設定をプリセット値で更新する
        /// </summary>
        private void UpdateConditions()
        {
            for (int i = 0; i < conditionsSettings.GetLength(0); i++)
            {
                for (int j = 0; j < conditionsSettings.GetLength(1); j++)
                {
                    conditionsSettings[i, j].value = conditionsSettingModel.GetTrafficVolumeSetting((ConditionsSettingModel.SettingType)i, (ConditionsSettingModel.TrafficType)j).ToString();
                }
            }
        }

        /// <summary>
        /// 交通量を推定する
        /// </summary>
        /// <returns></returns>
        private List<CsvPropertiesTraffic> CalculateEstimation()
        {
            var UsageMap = new Dictionary<string, ConditionsSettingModel.TrafficType>
            {
                { "業務施設", ConditionsSettingModel.TrafficType.Business },
                { "商業施設", ConditionsSettingModel.TrafficType.Commerce },
                { "住宅", ConditionsSettingModel.TrafficType.Housing }
            };

            var ret = new List<CsvPropertiesTraffic>();

            var roadNetworkManager = GameObject.FindObjectOfType<SimRoadNetworkManager>();

            if (roadNetworkManager == null)
            {
                return ret;
            }

            foreach (var zone in roadNetworkManager.TrafficFocusGroups)
            {
                // 1 日あたりのゾーンの出入り台数

                var totalTraffics = new double[UsageMap.Count]; // 交通量

                var totalFloorAreas = new double[UsageMap.Count]; // 総延床面積

                var unkonwnUsageArea = 0.0d; // 未知用途面積

                foreach (var building in zone.BuildingPairs)
                {
                    var lod0footprintGeo = SimZoneUtility.CreateFootprintGeometryFromMesh(building.FootprintBuilding.GetComponent<MeshFilter>().sharedMesh);

                    var area = lod0footprintGeo.Area; // 床面積

                    var cityObjectGroup = building.Building.GetComponent<PLATEAUCityObjectGroup>();

                    var cityObjects = cityObjectGroup.CityObjects;

                    foreach (var cityObject in cityObjects.rootCityObjects)
                    {
                        var height = 1.0d; // 高さ

                        var usageKey = string.Empty; // 用途

                        var attributes = cityObject.AttributesMap;

                        if (attributes.TryGetValue("bldg:measuredheight", out var measuredheight))
                        {
                            height = measuredheight.DoubleValue;
                        }

                        if (attributes.TryGetValue("bldg:usage", out var usage))
                        {
                            usageKey = usage.StringValue;
                        }

                        // TODO: 延床面積の算出方法は要確認？
                        var floorArea = area * (int)(height / FloorHeight); // 𝑠:延床面積

                        if (UsageMap.ContainsKey(usageKey) == false)
                        {
                            unkonwnUsageArea += floorArea;

                            break;
                        }

                        var index = UsageMap.Keys.ToList().IndexOf(usageKey);

                        totalFloorAreas[index] += floorArea; // 未知用途面積比率算出用

                        var traffic = CalculateTrafficVolume(floorArea, UsageMap[usageKey]);

                        totalTraffics[index] += traffic;

                        break;
                    }
                }

                // 未知用途面積を比率から振り分け
                for (var i = 0; i < UsageMap.Keys.Count; i++)
                {
                    var ratio = 1.0d / UsageMap.Keys.Count; // 初期化

                    if (totalFloorAreas.Sum() != 0) ratio = totalFloorAreas[i] / totalFloorAreas.Sum();

                    var traffic = CalculateTrafficVolume(ratio * unkonwnUsageArea, UsageMap.Values.ElementAt(i));

                    totalTraffics[i] += traffic;
                }

                var v = totalTraffics.Sum(); // v:1日あたりのゾーンの出入り台数

                // OD 設定時間あたり交通量へ変換

                double[] pIn = new double[24]; // 時間帯 𝑡 の集中時間帯係数

                // NOTE: 簡易な方式へ変更
                //double[,] pOut = new double[24, 24];　// 時間帯 𝑡 の集中した車両が 𝑇 時間帯に発生する確率
                double[] pOut = new double[24];

                double[] vOut = new double[24]; // 時間帯 𝑡 のゾーンの発生台数

                double[] vIn = new double[24]; // 時間帯 𝑡 のゾーンの集中台数

                // pInを設定する (時間帯ごとの集中時間帯係数)
                pIn = destinationRateSliders.Select(s => (double)s.value).ToArray();

                // NOTE: 簡易な方式へ変更
                // pOutを設定する (x時間以内に分散する、分布は均一)
                //for (int t = 0; t < 24; t++)
                //{
                //    // 発生する確率の分布を設定
                //
                //    // 3時間以内に分散する
                //    pOut[t, t] = 0.6f;           // 即時発生の確率
                //    pOut[t, (t + 1) % 24] = 0.3f; // 1時間後に発生
                //    pOut[t, (t + 2) % 24] = 0.1f; // 2時間後に発生
                //
                //    // 一様分布
                //    for (int T = 0; T < 24; T++)
                //    {
                //        pOut[t, T] = 1.0f / 24.0f;
                //    }
                //}
                pOut = occurrenceRateSliders.Select(s => (double)s.value).ToArray();

                // vInを計算する
                for (int t = 0; t < 24; t++)
                {
                    vIn[t] = v * pIn[t];
                }

                // NOTE: 簡易な方式へ変更
                // vOutを計算する
                //for (int T = 0; T < 24; T++)
                //{
                //    vOut[T] = 0; // 初期化
                //
                //    for (int t = 0; t < 24; t++)
                //    {
                //        vOut[T] += vIn[t] * pOut[t, T];
                //    }
                //}
                for (int t = 0; t < 24; t++)
                {
                    vOut[t] = v * pOut[t];
                }

                string startDaytime = existingSettingModel.StartTime.ToString(DateTimeFormat);
                string endDaytime = existingSettingModel.EndTime.ToString(DateTimeFormat);

                DateTime startTime = DateTime.ParseExact(startDaytime, DateTimeFormat, CultureInfo.InvariantCulture);
                DateTime endTime = DateTime.ParseExact(endDaytime, DateTimeFormat, CultureInfo.InvariantCulture);

                while (startTime < endTime)
                {
                    var trafficIn = 0.0d;
                    var trafficOut = 0.0d;

                    // 交通量は時間帯は１時間ごとなので、時間帯に応じた1分あたりの交通量を積算
                    for (int i = 0; i < timeResolution; i++)
                    {
                        var currentHour = startTime.AddMinutes(i).Hour;

                        trafficIn += vIn[currentHour] / 60.0;
                        trafficOut += vOut[currentHour] / 60.0;
                    }

                    DateTime nextTime = startTime.AddMinutes(timeResolution);

                    string startTimeString = startTime.ToString(DateTimeFormat);
                    string endTimeString = nextTime.ToString(DateTimeFormat);

                    ret.Add(new CsvPropertiesTraffic()
                    {
                        ZoneID = zone.ID,
                        StartTIme = startTimeString,
                        EndTime = endTimeString,
                        DepartureCount = Math.Round(trafficOut, 0, MidpointRounding.AwayFromZero).ToString(), // 四捨五入
                        DestinationCount = Math.Round(trafficIn, 0, MidpointRounding.AwayFromZero).ToString(), // 四捨五入
                    });

                    // 次の時間範囲へ
                    startTime = nextTime;
                }
            }

            return ret;
        }

        /// <summary>
        /// 交通量を計算する
        /// </summary>
        /// <param name="floorArea"></param>
        /// <param name="trafficType"></param>
        /// <returns></returns>
        private double CalculateTrafficVolume(double floorArea, ConditionsSettingModel.TrafficType trafficType)
        {
            var unit = conditionsSettingModel.GetTrafficVolumeSetting(ConditionsSettingModel.SettingType.Unit, trafficType); // 発生集中原単位

            var share = conditionsSettingModel.GetTrafficVolumeSetting(ConditionsSettingModel.SettingType.Share, trafficType); // 自動車分担率

            var factor = conditionsSettingModel.GetTrafficVolumeSetting(ConditionsSettingModel.SettingType.Factor, trafficType); // 台換算係数

            double floorAreaInHa = floorArea / 10000; // 床面積をヘクタールに変換

            var shareRate = share / 100.0f; // %を割合に変換

            var traffic = floorAreaInHa * unit * shareRate * factor;

            return traffic;
        }

        private async void EstimateODTrafficExisting()
        {
            var exportCSVs = CalculateEstimation();

            var csv = CsvExporter.CreateCSV(exportCSVs);

            string path = Path.Combine(existingSettingModel.RoadNetworkPath, TrafficVolumeFileName);

            await CsvExporter.ExportCSVAsync(csv, path, () => Debug.Log("CSV file saved successfully."));

            var args = new ModuleProcess.ArgumentSettings
            {
                InputFn008Dir = existingSettingModel.RoadNetworkPath + "/",
                OutputDir = existingSettingModel.ExportPath + "/",
                InputFn009Dir = existingSettingModel.TrafficVolumePath + "/",
                OutputFn009Dir = existingSettingModel.ExportPath + "/",
                StartTime = existingSettingModel.StartTime.ToString(DateTimeFormat),
                EndTime = existingSettingModel.EndTime.ToString(DateTimeFormat),
                TimeResolution = timeResolution,
            };

            // fn008_TravelRouteDeterminationFn.exe を実行
            await ModuleProcess.RunModuleFN008009(args);
        }

        private async void EstimateODTrafficDevelopment()
        {
            // 既存OD交通量のパスを指定
            developmentSettingModel.IF103Path = developmentSettingModel.ImportPath + "/" + IF103FileName; // .csv は除外（モジュール仕様）

            // 既存OD交通量と同じ場所に出力
            developmentSettingModel.IF104Path = developmentSettingModel.ImportPath + "/" + IF104FileName;

            // 開発ゾーンの指定
            developmentSettingModel.ZoneSeqDev = SelectDevZone.ID;

            // 参照ゾーンの指定
            developmentSettingModel.ZoneSeqEx = SelectRefZone.ID;

            // ヘッダなしのため CsvExporter を使用せず直接作成
            var csvs = developmentSettingModel.GetCSVs();

            // 開発OD交通量推定の設定は一旦CSVとしてエクスポートしてからモジュールを実行する
            foreach (var csv in csvs)
            {
                var path = Path.Combine(ModuleProcess.FN010011InputPath, csv.Key);

                await CsvExporter.ExportCSVAsync(csv.Value, path, () => Debug.Log("CSV file saved successfully."));
            }

            // Development_Traffic_Volume.exe を実行
            await ModuleProcess.RunModuleFN010011();
        }
    }
}