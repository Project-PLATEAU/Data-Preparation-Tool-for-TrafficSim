using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficSimulationTool.Runtime
{
    public class TrafficFocusArea : SimZone
    {
        public enum AreaTypes
        {
            Inside,
            InterChange,
            Intrude
        }

        public AreaTypes AreaType { get; set; }

        public Action<TrafficFocusArea> OnClickedHandler;

        private bool IsSelected = false;

        public void Start()
        {

            //Color
            var basecolor = gameObject.GetComponent<Renderer>().material.color;
            gameObject.GetComponent<Renderer>().material.color = new Color32((byte)(basecolor.r * 255), (byte)(basecolor.g * 255), (byte)(basecolor.b * 255), 50);
            gameObject.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var ymargin = 40f; //高さがある？
            transform.position = new Vector3(transform.position.x, -ymargin, transform.position.z);
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
            IsSelected = !IsSelected;
            var basecolor = gameObject.GetComponent<Renderer>().material.color;

            if (IsSelected) {
                Debug.Log("a");
                gameObject.GetComponent<Renderer>().material.color = new Color32((byte)(basecolor.r * 255), (byte)(basecolor.g * 255), (byte)(basecolor.b * 255), 190);
            } else {
                gameObject.GetComponent<Renderer>().material.color = new Color32((byte)(basecolor.r * 255), (byte)(basecolor.g * 255), (byte)(basecolor.b * 255), 50);
            }

            OnClickedHandler?.Invoke( this );
        }
    }
}