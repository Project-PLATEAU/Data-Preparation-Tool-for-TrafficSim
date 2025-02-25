using PLATEAU.Geometries;
using System.Collections.Generic;
using System.Linq;
using TrafficSimulationTool.Runtime.SimData;
using UnityEngine;

namespace TrafficSimulationTool.Runtime.Simulator
{
    /// <summary>
    /// TimelineManagerによって管理される
    /// </summary>
    public class VehicleSimulator : SimulatorBase, IPlayable
    {
        private VehicleManager vehicleManager;

        private VehicleTimelineDataSet vehicleData;

        public string SelectedFocusAreaID { get; private set; }

        public void Initialize()
        {
            vehicleManager = new VehicleManager();
        }

        public override void InitializeReferences(GeoReference geo, SimRoadNetworkManager roadManager)
        {
            base.InitializeReferences(geo, roadManager);
            roadManager.OnTrafficFocusAreaClickedHandler += OnFocusAreaClick;
            roadManager.OnDevelopFocusGroupClickedHandler += OnFocusAreaClick;
        }

        public void Dispose()
        {
            vehicleManager?.Dispose();

            if (roadNetworkManager != null)
            {
                roadNetworkManager.OnTrafficFocusAreaClickedHandler -= OnFocusAreaClick;
                roadNetworkManager.OnDevelopFocusGroupClickedHandler -= OnFocusAreaClick;
            }
        }

        public void SetData(VehicleTimelineDataSet data)
        {
            vehicleData = data;
            vehicleManager.ClearVehicles();
        }

        public void PlayFrame(uint frame)
        {
            for (int i = 0; i < vehicleData.vehicleSlots.Length; i++) //読込中再生するのでEnumeratorは使用不可
            {
                var slot = vehicleData.vehicleSlots.Slots[i];
                int slotId = slot.ID;
                var frameData = slot.GetFrame((int)frame);

                if (frameData.Valid == 1 && enabled)
                {
                    var (start, end) = vehicleData.GetStartEndTimelines(frameData);
                    var vehicle = vehicleManager.GetVehicle(slotId, start.VehicleID, start.VehicleType);
                    if (start.Departure == SelectedFocusAreaID || start.Destination == SelectedFocusAreaID) {
                        vehicle.HighlightMaterial();
                    }
                    var link = roadNetworkManager.GetLinkById(start.LinkID);
                    vehicle.SetTimelines(start, end, frameData, link?.RuntimeTrans?.Select(x => x.gameObject).ToList());
                }
                else
                {
                    vehicleManager.ClearVehicle(slotId);
                }
            }

            vehicleManager.DestroyUnusedVehicles();
        }

        private void OnFocusAreaClick(SimZone focusArea)
        {
            var nodeIds = new List<string>() { focusArea.ID };
            var vehicles = vehicleManager.GetAllVehieclesByNodes(nodeIds);
            if (SelectedFocusAreaID == focusArea.ID) {
                foreach (var vehicle in vehicles)
                {
                    vehicle.DisableHighlightMaterial();
                }
                SelectedFocusAreaID = null;
            } else {
                foreach (var vehicle in vehicles)
                {
                    vehicle.HighlightMaterial();
                }
                SelectedFocusAreaID = focusArea.ID;
            }
        }
    }
}