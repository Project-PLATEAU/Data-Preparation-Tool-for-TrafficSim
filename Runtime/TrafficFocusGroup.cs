using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace TrafficSimulationTool.Runtime
{
    public class TrafficFocusGroup : SimZone
    {
        [System.Serializable]
        public class BuildingPair
        {
            public GameObject Building;

            public GameObject FootprintBuilding;

            public BuildingPair(GameObject building, GameObject footprint)
            {
                this.Building = building;
                this.FootprintBuilding = footprint;
            }
        }

        /// <summary>
        /// 建物とそのfootprintのペア
        /// </summary>
        public List<BuildingPair> BuildingPairs = new List<BuildingPair>();

        public bool IsDevelopArea = false;

        private bool isSelected = false;

        /// <summary>
        /// footprintと建物のペアを追加
        /// </summary>
        /// <param name="building"></param>
        /// <param name="footprint"></param>
        public void AddBuilding(GameObject building, GameObject footprint)
        {
            BuildingPairs.Add(new BuildingPair(building, footprint));
        }

        public Action<TrafficFocusGroup> OnClickedHandler;

        public void Start()
        {
            if (!IsDevelopArea) return;

            //Color
            var basecolor = gameObject.GetComponent<Renderer>().material.color;
            gameObject.GetComponent<Renderer>().material.color = new Color32((byte)(basecolor.r * 255), (byte)(basecolor.g * 255), (byte)(basecolor.b * 255), 50);
            gameObject.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var ymargin = 40f; // TODO:仮オフセット
            transform.position = new Vector3(transform.position.x, -ymargin, transform.position.z);
            // TODO: 上からレイを飛ばして地面に合わせる
            Ray ray = new Ray(new Vector3(transform.position.x, 100f, transform.position.z), Vector3.down);
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity))
            {
                transform.position = new Vector3(hit.point.x, hit.point.y - ymargin, hit.point.z);
            }

            gameObject.AddComponent<MeshCollider>();
        }

        public void OnClicked()
        {
            Debug.Log($"Object {name} clicked.");

            isSelected = !isSelected;
            var basecolor = gameObject.GetComponent<Renderer>().material.color;

            if (isSelected) {
                gameObject.GetComponent<Renderer>().material.color = new Color32((byte)(basecolor.r * 255), (byte)(basecolor.g * 255), (byte)(basecolor.b * 255), 190);
            } else {
                gameObject.GetComponent<Renderer>().material.color = new Color32((byte)(basecolor.r * 255), (byte)(basecolor.g * 255), (byte)(basecolor.b * 255), 50);
            }

            OnClickedHandler?.Invoke(this);
        }
    }
}