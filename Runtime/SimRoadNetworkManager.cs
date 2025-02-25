using PLATEAU.CityInfo;
using PLATEAU.Geometries;
using PLATEAU.RoadNetwork.Data;
using PLATEAU.RoadNetwork.Structure;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficSimulationTool.Runtime
{
    /// <summary>
    /// シミュレーション用道路ネットワークの管理クラス
    /// </summary>
    public class SimRoadNetworkManager : MonoBehaviour
    {
        [field: SerializeField, SerializeReference, HideInInspector]
        public List<SimRoadNetworkNode> SimRoadNetworkNodes { get; set; } = new List<SimRoadNetworkNode>();

        [field: SerializeField, SerializeReference, HideInInspector]
        public List<SimRoadNetworkLink> SimRoadNetworkLinks { get; set; } = new List<SimRoadNetworkLink>();

        [field: SerializeField, SerializeReference, HideInInspector]
        public List<TrafficFocusArea> TrafficFocusAreas { get; set; } = new List<TrafficFocusArea>();

        [field: SerializeField, SerializeReference, HideInInspector]
        public List<TrafficFocusGroup> TrafficFocusGroups { get; set; } = new List<TrafficFocusGroup>();

        [field: SerializeField, SerializeReference, HideInInspector]
        private PLATEAURnStructureModel rnStructureModel;

        [field: SerializeField, SerializeReference, HideInInspector]
        public List<SimSignalController> SignalControllers { get; set; } = new List<SimSignalController>();

        [field: SerializeField, SerializeReference, HideInInspector]
        public List<SimSignalStep> SignalSteps { get; set; } = new List<SimSignalStep>();

        [field: SerializeField, SerializeReference, HideInInspector]
        public List<SimSignalLight> SignalLights { get; set; } = new List<SimSignalLight>();

        private RoadNetworkDataGetter roadNetworkGetter;

        private RoadNetworkDataSetter roadNetworkSetter;

        public RoadNetworkDataGetter RoadNetworkDataGetter
        {
            get
            {
                if (rnStructureModel == null)
                {
                    rnStructureModel = GameObject.FindObjectOfType<PLATEAURnStructureModel>();
                }

                if (roadNetworkGetter == null)
                {
                    roadNetworkGetter = rnStructureModel.GetRoadNetworkDataGetter();
                }

                return roadNetworkGetter;
            }
        }

        public RoadNetworkDataSetter RoadNetworkDataSetter
        {
            get
            {
                if (rnStructureModel == null)
                {
                    rnStructureModel = GameObject.FindObjectOfType<PLATEAURnStructureModel>();
                }

                if (roadNetworkSetter == null)
                {
                    roadNetworkSetter = rnStructureModel.GetRoadNetworkDataSetter();
                }

                return rnStructureModel.GetRoadNetworkDataSetter();
            }
        }

        [field: SerializeField, SerializeReference, HideInInspector]
        private PLATEAUInstancedCityModel cityModelInstance;

        public GeoReference GeoReference
        {
            get
            {
                if (cityModelInstance == null)
                {
                    cityModelInstance = GameObject.FindObjectOfType<PLATEAUInstancedCityModel>();
                }

                return cityModelInstance.GeoReference;
            }
        }

        public Action<TrafficFocusArea> OnTrafficFocusAreaClickedHandler;

        public Action<TrafficFocusGroup> OnDevelopFocusGroupClickedHandler;

        private void OnDestroy()
        {
            foreach (var focusArea in TrafficFocusAreas)
            {
                focusArea.OnClickedHandler -= OnTrafficAreaClick;
            }

            foreach (var focusGroup in TrafficFocusGroups)
            {
                focusGroup.OnClickedHandler -= OnTrafficGrupClick;
            }
        }

        public void Initialize()
        {
            foreach (var focusArea in TrafficFocusAreas)
            {
                focusArea.OnClickedHandler += OnTrafficAreaClick;
            }

            foreach (var focusGroup in TrafficFocusGroups)
            {
                focusGroup.OnClickedHandler += OnTrafficGrupClick;
            }

            //debug : OriginTran名確認
            //DebugLinks();
            //Debug.Log($"tran {GetLinkIDByName("tran_79a2d162-e6df-41b5-a119-dc6d20b395e2")}");
        }

        public SimRoadNetworkLink GetLinkById(string id)
        {
            return SimRoadNetworkLinks.Find(x => x.ID == id);
        }

        public SimRoadNetworkNode GetNodeById(string id)
        {
            return SimRoadNetworkNodes.Find(x => x.ID == id);
        }

        private void OnTrafficAreaClick(TrafficFocusArea focusArea)
        {
            OnTrafficFocusAreaClickedHandler?.Invoke(focusArea);
        }

        private void OnTrafficGrupClick(TrafficFocusGroup focusGroup)
        {
            OnDevelopFocusGroupClickedHandler?.Invoke(focusGroup);
        }

        //public string GetLinkIDByName(string name)
        //{
        //    var linkid = SimRoadNetworkLinks.Find(x => x.OriginTran?.name == name)?.ID;
        //    return linkid;
        //}

        //public void DebugLinks()
        //{
        //    foreach (var item in SimRoadNetworkLinks)
        //    {
        //        Debug.Log($"link {item.ID} : {item.OriginTran?.name}");
        //    }
        //}
    }
}