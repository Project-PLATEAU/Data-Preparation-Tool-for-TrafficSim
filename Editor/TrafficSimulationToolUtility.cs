using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TrafficSimulationTool.Editor
{
    public class TrafficSimulationToolUtility
    {
        public static string RootPath = "Packages/com.synesthesias.plateau-trafficsimulationtool/Editor/";

        public static T GetAssetRelativePath<T>(string path) where T : UnityEngine.Object
        {
            var fullPath = RootPath + path;
            return AssetDatabase.LoadAssetAtPath<T>(fullPath);
        }
    }
}