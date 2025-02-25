using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TrafficSimulationTool.Editor
{
    /// <summary>
    /// データ出力タブのコントローラ
    /// </summary>
    public class ExportViewController : ViewController
    {
        [SerializeField]
        private string exportPath = Application.persistentDataPath;

        public ExportViewController(VisualElement element, TrafficSimulationToolWindow parent) : base(element, parent)
        {
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected override void Initialize()
        {
            var pathField = rootElement.Q<TextField>("TextFieldExportPath");

            pathField.SetEnabled(false);

            var pathSelector = rootElement.Q<Button>("ButtonBrowse");

            pathSelector.clickable.clicked += () =>
            {
                exportPath = EditorUtility.OpenFolderPanel("Select Export Folder", exportPath, "");

                pathField.value = exportPath;
            };

            var exportButton = rootElement.Q<Button>("ButtonExport");

            exportButton.clickable.clicked += () =>
            {
                var roadNetworkExporter = new RoadNetworkExporter();

                roadNetworkExporter.ExportSimRoadNetwork(exportPath);
            };
        }
    }
}