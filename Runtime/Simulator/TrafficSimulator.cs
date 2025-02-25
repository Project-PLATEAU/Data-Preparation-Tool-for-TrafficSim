using System.Linq;
using TrafficSimulationTool.Runtime.SimData;

namespace TrafficSimulationTool.Runtime.Simulator
{
    /// <summary>
    /// TimelineManagerによって管理される
    /// </summary>
    public class TrafficSimulator : SimulatorBase, IPlayable
    {
        private RoadIndicatorDataSet roadData;

        private LinkManager linkManager;

        public void Initialize()
        {
            linkManager = new LinkManager();
        }

        public void Dispose()
        {
            linkManager?.Dispose();
        }

        public void PlayFrame(uint frame)
        {
            for (int i = 0; i < roadData.trafficSlots.Length; i++)
            {
                var slot = roadData.trafficSlots.Slots[i];
                int slotId = slot.ID;
                var data = slot.GetFrame((int)frame);

                if (data.Valid == 1 && enabled)
                {
                    var timline = roadData.GetTimeline(data.Index);
                    var linkId = timline.LinkID;
                    var link = roadNetworkManager.GetLinkById(linkId);

                    if (link != null)
                    {
                        linkManager.SetTimeline(slotId, linkId, link.RuntimeTrans.Select(x => x.gameObject).ToList(), timline);
                    }
                }
                else
                {
                    linkManager.RemoveLink(slotId);
                }
            }
        }

        public void SetData(RoadIndicatorDataSet data)
        {
            roadData = data;
        }
    }
}