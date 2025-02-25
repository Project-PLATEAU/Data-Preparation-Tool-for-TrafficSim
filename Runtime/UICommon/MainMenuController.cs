using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace TrafficSimulationTool.Runtime.UICommon
{
    public class MainMenuController : MonoBehaviour, IUIDocumentInitializable
    {
        private UIDocument _uiDocument;
        public GlobalNavController TopMenu { get; private set; } = new GlobalNavController();
        public SequenceControlMenuController SequenceControl { get; private set; } = new SequenceControlMenuController();
        public LoadDataMenuController LoadDataDialog { get; private set; } = new LoadDataMenuController();

        public void Initialize(UIDocument uiDocument)
        {
            _uiDocument = uiDocument;
            _uiDocument.sortingOrder = 1;

            // Top Menu
            var topMenuElement = _uiDocument.rootVisualElement.Q<VisualElement>("GlobalNav");
            TopMenu.Initialize(topMenuElement);

            // Controller
            var sequenceControlElement = _uiDocument.rootVisualElement.Q<VisualElement>("Panel_TimeSlider");
            SequenceControl.Initialize(sequenceControlElement);

            // Load Data Dialog
            var dialogElement = _uiDocument.rootVisualElement.Q<VisualElement>("Panel_Import");
            LoadDataDialog.Initialize(dialogElement);

            // Example
            var exampleElement = _uiDocument.rootVisualElement.Q<VisualElement>("ExampleContainer");

            var bar = exampleElement.Q<VisualElement>("ExampleColorBar");

            bar.style.backgroundImage = new StyleBackground(ColorManipulator.GenerateGradientTexture());

            //var gradation = new List<GradientThreshold>
            //{
            //    new GradientThreshold(new Color(1.0f, 0.0f, 0.0f), 0, "0km/h"),
            //    new GradientThreshold(new Color(1.0f, 0.53f, 0.0f), 10, "10km/h"),
            //    new GradientThreshold(new Color(1.0f, 1.0f, 0.0f), 20, "20km/h"),
            //    new GradientThreshold(new Color(0.03f, 1.0f, 0.0f), 30, "30km/h"),
            //    new GradientThreshold(new Color(0.0f, 0.53f, 1.0f), 40, "40km/h"),
            //};

            //var colors = gradation.Select(g => g.Color).ToList();

            //bar.style.backgroundImage = new StyleBackground(GenerateGradientTexture(colors));
        }

        //private class GradientThreshold
        //{
        //    public Color Color { get; set; }         // グラデーションの色
        //    public float Threshold { get; set; }    // 閾値
        //    public string Label { get; set; }       // 関連する文言

        //    public GradientThreshold(Color color, float threshold, string label)
        //    {
        //        Color = color;
        //        Threshold = threshold;
        //        Label = label;
        //    }
        //}

        //private Texture2D GenerateGradientTexture(List<Color> gradation)
        //{
        //    int width = 16; // 横幅は固定
        //    int height = 320; // 縦方向のピクセル数
        //    var texture = new Texture2D(width, height);

        //    // グラデーション区間の数
        //    int sectionCount = gradation.Count - 1;
        //    if (sectionCount <= 0) throw new ArgumentException("The gradation list must have at least two colors.");

        //    for (int y = 0; y < height; y++)
        //    {
        //        // 高さに基づいて全体の補間位置 t を計算
        //        float t = 1.0f - (y / (float)(height - 1));

        //        // 現在の t が属する区間を計算
        //        int currentSection = Mathf.FloorToInt(t * sectionCount);
        //        float localT = (t * sectionCount) - currentSection;

        //        // 両端の色を取得
        //        Color startColor = gradation[currentSection];
        //        Color endColor = gradation[Mathf.Min(currentSection + 1, gradation.Count - 1)];

        //        // 現在の区間内での色補間
        //        Color color = Color.Lerp(startColor, endColor, localT);

        //        // 各横方向ピクセルに色を設定
        //        for (int x = 0; x < width; x++)
        //        {
        //            texture.SetPixel(x, y, color);
        //        }
        //    }

        //    texture.Apply();
        //    return texture;
        //}

        public void Dispose()
        {
            TopMenu?.Dispose();
            SequenceControl?.Dispose();
            LoadDataDialog?.Dispose();
        }

        public void SetVisibility(bool visible)
        {
            _uiDocument.rootVisualElement.visible = visible;
        }
    }
}