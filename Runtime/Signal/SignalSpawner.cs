using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrafficSimulationTool.Runtime;
using UnityEngine;

namespace TrafficSimulationTool
{
    public class SignalSpawner : MonoBehaviour
    {
        public static void SpawnSignal(SimRoadNetworkManager simRoadNetworkManager)
        {
            // TODO: for PoC

            var controllers = new List<SimSignalController>();

            var ids = new List<string>() { "Node13", "Node7", "Node5", "Node15", "Node6", "Node12", "Node30", "Node4", "Node24", "Node19", "Node26", "Node33", "Node41", "Node39", "Node32", "Node34", "Node25", "Node16", "Node36", "Node37" };

            foreach (var id in ids)
            {
                var node = simRoadNetworkManager.SimRoadNetworkNodes.Where(x => x.ID == id).First();

                var controller = new SimSignalController()
                {
                    Node = node
                };

                controllers.Add(controller);
            }

            simRoadNetworkManager.SignalControllers.Clear();

            simRoadNetworkManager.SignalControllers = controllers;

            // TODO: for PoC (end)

            var parent = new GameObject("Signals");

            foreach (var signalController in simRoadNetworkManager.SignalControllers)
            {
                var position = signalController.Node.GetPosition();

                GameObject signal = Instantiate(Resources.Load("Prefabs/SignalIcon"), position + new Vector3(0, 5.0f, 0), Quaternion.identity) as GameObject;

                signal.transform.parent = parent.transform;
            }
        }
    }
}