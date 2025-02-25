using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;

namespace TrafficSimulationTool.Editor
{
    /// <summary>
    /// CSV形式のデータをエクスポートするための機能を提供するクラス
    /// </summary>
    public class CsvExporter
    {
        /// <summary>
        /// CSV形式のデータへ変換する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string CreateCSV<T>(List<T> data)
        {
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);

            // CSVのヘッダー名を取得
            var headerNames = fields
                .Select(field =>
                {
                    var attribute = (CsvHeaderAttribute)Attribute.GetCustomAttribute(field, typeof(CsvHeaderAttribute));
                    return attribute != null ? attribute.HeaderName : field.Name;
                }).ToArray();

            // StringBuilderを使用してCSV文字列を構築
            var csvBuilder = new System.Text.StringBuilder();

            // ヘッダーを追加
            csvBuilder.AppendLine(string.Join(",", headerNames));

            // データをCSV形式に変換
            foreach (var item in data)
            {
                var values = fields
                    .Select(field => field.GetValue(item)?.ToString() ?? string.Empty)
                    .ToArray();
                csvBuilder.AppendLine(string.Join(",", values));
            }

            return csvBuilder.ToString();
        }

        /// <summary>
        /// CSV形式のデータをファイルに書き込む
        /// </summary>
        /// <param name="path"></param>
        /// <param name="csvText"></param>
        /// <returns></returns>
        private static async Task WriteCSVAsync(string path, string csvText)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(path))
                {
                    await writer.WriteAsync(csvText);
                }
            }
            catch (Exception ex)
            {
                // エラーハンドリング
                Console.Error.WriteLine($"Error writing CSV to {path}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// CSV形式のデータを保存する
        /// </summary>
        /// <param name="csvText"></param>
        /// <param name="path"></param>
        /// <param name="onSuccess"></param>
        /// <returns></returns>
        public static async Task ExportCSVAsync(string csvText, string path, Action onSuccess)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
            }

            try
            {
                await WriteCSVAsync(path, csvText);
                onSuccess?.Invoke();
            }
            catch (Exception ex)
            {
                // エラーハンドリング
                Console.Error.WriteLine($"Error exporting CSV: {ex.Message}");
            }
        }
    }
}