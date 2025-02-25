using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficSimulationTool
{
    public class SignalViewer : MonoBehaviour
    {
        private void Update()
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
        }
    }
}