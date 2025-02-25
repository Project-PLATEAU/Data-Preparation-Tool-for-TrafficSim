using System.Collections.Generic;
using System.IO;
using TrafficSimulationTool.Runtime.SimData;
using UnityEngine;

public class CsvParser
{
    public static T[] ParseCsv<T>(string csvFilePath) where T : class, new()
    {
        List<T> dataList = new List<T>();

        try
        {
            // CSVファイルを読み込み
            string[] lines = File.ReadAllLines(csvFilePath);

            // CSVのヘッダーを取得
            string[] headers = lines[0].Split(',');

            // フィールドとヘッダー名の対応を作成
            var fields = typeof(T).GetFields();

            Dictionary<string, System.Reflection.FieldInfo> fieldMap = new Dictionary<string, System.Reflection.FieldInfo>();

            foreach (var field in fields)
            {
                var attribute = (CsvHeaderAttribute)System.Attribute.GetCustomAttribute(field, typeof(CsvHeaderAttribute));

                if (attribute != null)
                {
                    fieldMap[attribute.HeaderName] = field;
                }
            }

            // データ行をパース
            for (int i = 1; i < lines.Length; i++)
            {
                string[] csvFields = lines[i].Split(',');

                T data = new T();

                for (int j = 0; j < headers.Length; j++)
                {
                    if (fieldMap.TryGetValue(headers[j], out var field))
                    {
                        object value = null;
                        if (field.FieldType == typeof(int))
                            value = int.Parse(csvFields[j]);
                        else if (field.FieldType == typeof(float))
                            value = float.Parse(csvFields[j]);
                        else if (field.FieldType == typeof(string))
                            value = csvFields[j];
                        else if (field.FieldType == typeof(bool))
                            value = bool.Parse(csvFields[j]);

                        field.SetValue(data, value);
                    }
                }
                dataList.Add(data);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("CSVパース中にエラーが発生しました: " + e.Message);
        }

        return dataList.ToArray();
    }
}