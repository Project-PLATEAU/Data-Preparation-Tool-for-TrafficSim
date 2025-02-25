using PLATEAU.Geometries;

namespace TrafficSimulationTool.Runtime.Simulator
{
    public class SimulatorBase
    {
        protected SimRoadNetworkManager roadNetworkManager;
        protected GeoReference geoReference;

        protected bool enabled = true;

        public virtual void SetEnabled(bool bEnabled)
        {
            enabled = bEnabled;
        }

        public virtual void InitializeReferences(GeoReference geo, SimRoadNetworkManager roadManager)
        {
            roadNetworkManager = roadManager;
            geoReference = geo;
        }
    }
}
