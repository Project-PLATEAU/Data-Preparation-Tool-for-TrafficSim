using PLATEAU.CityInfo;
using PLATEAU.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using TrafficSimulationTool.Runtime.FX;
using TrafficSimulationTool.Runtime.SimData;
using UnityEngine;

namespace TrafficSimulationTool.Runtime.Simulator
{
    public class Vehicle : MonoBehaviour, IDisposable
    {
        public string VehicleID;

        public string VehicleType;

        public string StartNodeID { get; private set; }

        public string EndNodeID { get; private set; }

        public void SetVehicleID(string vid, string type) // ReInitialize without changing the asset
        {
            VehicleID = vid;
            VehicleType = type;
            gameObject.name = vid;
            StartNodeID = null;
        }

        public void SetTimelines(VehicleTimeline start, VehicleTimeline end, VehicleFrame frame, List<GameObject> road)
        {
            StartNodeID = start.Departure;
            EndNodeID = start.Destination;

            //Position, Vector の計算は　VehicleTimelineDataSetで行う
            Vector3 position = frame.Position;
            Vector3 vector = frame.Vector;
            GetComponentInChildren<Collider>().enabled = false;

            Ray ray = new Ray(new Vector3((float)position.x, 100f, (float)position.z), Vector3.down);
            RaycastHit[] hits = new RaycastHit[16]; //最大16個まで

            //Debug : Raycast Results 最大値調査
            //var testItems = Physics.RaycastAll(ray);
            //Debug.Log($"Raycastall results {testItems.Length}");

            if (Physics.RaycastNonAlloc(ray, hits, Mathf.Infinity) > 0)
            {
                bool found = false;

                if (road != null)
                {
                    foreach (var hit in hits)
                    {
                        if (road.Any(x => x == hit.transform?.gameObject))
                        {
                            position = hit.point;
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    //Linkのgameobjectが取得できない場合PLATEAUCityObjectGroupの最上部
                    var hitObjs = Array.FindAll(hits, x => x.transform?.TryGetComponent<PLATEAUCityObjectGroup>(out var comp) == true && x.transform.name.StartsWith("tran"));
                    if (hitObjs.TryFindMinElement(x => x.distance, out var o))
                    {
                        position = o.point;
                    }
                }
            }

            //渋滞すると衝突判定が走ってしまう
            //GetComponentInChildren<Collider>().enabled = true;

            //set data
            transform.position = position;

            if (vector != Vector3.zero)
                transform.forward = vector;

            //Debug.Log($"car {VehicleID} : {position.x} {position.y} {position.z} {percent}");
        }

        private void ChangeLayerRecursive(GameObject go, bool toggle)
        {
            go.layer = LayerMask.NameToLayer(toggle ? HighlightFX.FX_LAYER_CAR : "Default");
            for (int i = 0; i < go.transform.childCount; i++)
            {
                ChangeLayerRecursive(go.transform.GetChild(i).gameObject, toggle);
            }
        }

        public void DisableHighlightMaterial()
        {
            SetEmission(gameObject, false);
            ChangeLayerRecursive(gameObject, false);
        }

        public void HighlightMaterial()
        {
            Debug.Log("HighlightMaterial");

            SetEmission(gameObject, true);
            ChangeLayerRecursive(gameObject, true);
        }

        public void Dispose()
        {
            if (gameObject != null)
                DestroyImmediate(gameObject);
        }

        public static void SetEmission(GameObject obj, bool toggle)
        {
            if (obj == null)
            {
                Debug.LogError("対象のGameObjectがnullです。");
                return;
            }

            Color color = toggle ? new Color(1f, 0.6f, 0.6f, 1f) : new Color(1f, 1f, 1f, 1f);

            // オブジェクトおよびその子オブジェクトのRendererコンポーネントをすべて取得
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                // 各レンダラーのマテリアルをループ
                foreach (Material mat in renderer.materials)
                {
                    bool propertySet = false;

                    // Shader Graphの_BaseColorプロパティを確認
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", color);
                        propertySet = true;
                    }

                    // 互換性のために_Colorプロパティも確認
                    if (mat.HasProperty("_Color"))
                    {
                        mat.SetColor("_Color", color);
                        propertySet = true;
                    }

                    if (!propertySet)
                    {
                        Debug.LogWarning($"マテリアル '{mat.name}' に '_BaseColor' または '_Color' プロパティがありません。");
                    }
                }
            }
        }
    }
}