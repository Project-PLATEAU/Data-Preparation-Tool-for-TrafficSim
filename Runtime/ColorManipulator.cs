using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrafficSimulationTool.Runtime
{
    public class ColorManipulator
    {
        private class GradientThreshold
        {
            public Color Color { get; set; }         // グラデーションの色
            public float Threshold { get; set; }    // 閾値
            public string Label { get; set; }       // 関連する文言

            public GradientThreshold(Color color, float threshold, string label)
            {
                Color = color;
                Threshold = threshold;
                Label = label;
            }
        }

        private static readonly List<GradientThreshold> gradation = new List<GradientThreshold>
        {
            new GradientThreshold(new Color(1.0f, 0.0f, 0.0f), 0, "0km/h"),
            new GradientThreshold(new Color(1.0f, 0.53f, 0.0f), 10, "10km/h"),
            new GradientThreshold(new Color(1.0f, 1.0f, 0.0f), 20, "20km/h"),
            new GradientThreshold(new Color(0.03f, 1.0f, 0.0f), 30, "30km/h"),
            new GradientThreshold(new Color(0.0f, 0.53f, 1.0f), 40, "40km/h"),
        };

        /// <summary>
        /// グラデーションテクスチャを生成
        /// </summary>
        /// <param name="width">横方向ピクセル数</param>
        /// <param name="height">縦方向ピクセル数</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Texture2D GenerateGradientTexture(int width = 16, int height = 320)
        {
            var texture = new Texture2D(width, height);

            // グラデーション区間の数
            int sectionCount = gradation.Count - 1;
            if (sectionCount <= 0) throw new ArgumentException("The gradation list must have at least two colors.");

            for (int y = 0; y < height; y++)
            {
                // 高さに基づいて全体の補間位置 t を計算
                float t = 1.0f - (y / (float)(height - 1));

                // 現在の t が属する区間を計算
                int currentSection = Mathf.FloorToInt(t * sectionCount);
                float localT = (t * sectionCount) - currentSection;

                // 両端の色を取得
                Color startColor = gradation[currentSection].Color;
                Color endColor = gradation[Mathf.Min(currentSection + 1, gradation.Count - 1)].Color;

                // 現在の区間内での色補間
                Color color = Color.Lerp(startColor, endColor, localT);

                // 各横方向ピクセルに色を設定
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();

            return texture;
        }

        public static Color GetColorFromSpeed(float speed)
        {
            // 速度が最小値未満の場合、最初の色を返す
            if (speed <= gradation.First().Threshold)
                return gradation.First().Color;

            // 速度が最大値を超える場合、最後の色を返す
            if (speed >= gradation.Last().Threshold)
                return gradation.Last().Color;

            // 閾値に基づいて、属する区間を特定
            for (int i = 0; i < gradation.Count - 1; i++)
            {
                GradientThreshold start = gradation[i];
                GradientThreshold end = gradation[i + 1];

                if (speed >= start.Threshold && speed <= end.Threshold)
                {
                    // 線形補間
                    float t = (speed - start.Threshold) / (end.Threshold - start.Threshold);
                    return Color.Lerp(start.Color, end.Color, t);
                }
            }

            // フォールバック
            return gradation.First().Color;
        }
    }
}