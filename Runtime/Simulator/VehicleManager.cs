using PLATEAU.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrafficSimulationTool.Runtime.Simulator
{
    /// <summary>
    /// Simulate時の Vehicle管理
    /// </summary>
    public class VehicleManager : IDisposable
    {
        private readonly Dictionary<string, string> VEHICLE_ASSET_PATH = new Dictionary<string, string>() {
            { "small", "HDRP Sample Assets/Vehicles/Prefabs/Vehicle_Sedan_01_White" },
            { "large", "HDRP Sample Assets/Vehicles/Prefabs/Vehicle_Truck" },
            { "small_dev", "HDRP Sample Assets/Vehicles/Prefabs/Vehicle_Minivan_01_White" },
        };

        private Dictionary<int, Vehicle> _vehicles = new Dictionary<int, Vehicle>();
        private List<Vehicle> _unusedVehecles = new List<Vehicle>();
        private GameObject vehiclesContainer;

        public VehicleManager()
        {
            vehiclesContainer = new GameObject("Vehicles");
        }

        public void ClearVehicles()
        {
            foreach (var kv in _vehicles)
                kv.Value?.Dispose();

            _vehicles.Clear();
        }

        public Vehicle GetVehicle(int id, string vid, string type)
        {
            if (_vehicles.TryGetValue(id, out var vehicle))
            {
                if (vehicle.VehicleType == type && vehicle.VehicleID == vid)
                {
                    return vehicle;
                }

                ClearVehicle(id);
            }
            return CreateVehicle(id, vid, type);
        }

        /// <summary>
        /// VehecleをDictionary・退避リストから検索
        /// </summary>
        public Vehicle GetVehicleByVid(string vid)
        {
            var unusedVehicle = _unusedVehecles.Find(x => x.VehicleID == vid);
            if (unusedVehicle != null)
            {
                return unusedVehicle;
            }

            return _vehicles.Values.ToList().Find(x => x.VehicleID == vid);
        }

        public Vehicle CreateVehicle(int id, string vid, string type)
        {
            //Debug.Log($"CreateVehicle {vid}");
            var asset = VEHICLE_ASSET_PATH[type];
            var vehiclePrefab = Resources.Load<GameObject>(asset);
            var instance = GameObject.Instantiate(vehiclePrefab);
            var vehicle = instance.AddComponent<Vehicle>();
            vehicle.transform.SetParent(vehiclesContainer.transform);
            vehicle.SetVehicleID(vid, type);
            SetOriginalTransform(vehicle, vid);

            _vehicles.Add(id, vehicle);
            return vehicle;
        }

        /// <summary>
        /// 一旦、退避リスト（_unusedVehecles）に退避
        /// </summary>
        public bool ClearVehicle(int id)
        {
            if (_vehicles.ContainsKey(id))
            {
                if(_vehicles[id] != null)
                {
                    _unusedVehecles.Add(_vehicles[id]);
                } 
                _vehicles.Remove(id);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 退避リスト（_unusedVehecles）内のアイテムを全て削除
        /// </summary>
        public void DestroyUnusedVehicles()
        {
            if (_unusedVehecles.Count <= 0)
                return;

            foreach (var item in _unusedVehecles)
            {
                item?.Dispose();
            }
            _unusedVehecles.Clear();
        }

        public List<Vehicle> GetAllVehieclesByNodes(List<string> nodes)
        {
            return _vehicles.Values.ToList().FindAll(v => nodes.Contains(v.StartNodeID));
        }

        public List<Vehicle> GetAllVehieclesByEndNodes(List<string> nodes)
        {
            return _vehicles.Values.ToList().FindAll(v => nodes.Contains(v.EndNodeID));
        }

        public void Dispose()
        {
            DestroyUnusedVehicles();
        }

        /// <summary>
        /// 元のVehicleからtransformを取得して設定
        /// </summary>
        private void SetOriginalTransform(Vehicle vehicle, string vid)
        {
            var original = GetVehicleByVid(vid);
            if (original != null)
            {
                vehicle.transform.forward = original.transform.forward;
                vehicle.transform.position = original.transform.position;
            }
        }
    }
}