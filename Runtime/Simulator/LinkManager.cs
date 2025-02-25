using System;
using System.Collections.Generic;
using TrafficSimulationTool.Runtime.SimData;
using UnityEngine;
using TrafficSimulationTool.Runtime.FX;
using System.Linq;

namespace TrafficSimulationTool.Runtime.Simulator
{
    /// <summary>
    /// Simulate時の Traffic Link管理
    /// </summary>
    public class LinkManager : IDisposable
    {
        private Dictionary<int, LinkStatus> _links = new Dictionary<int, LinkStatus>();

        public void SetTimeline(int index, string linkId, List<GameObject> obj, RoadIndicator timeline)
        {
            bool requireUpdate = false;
            LinkStatus stat = null;
            if (!_links.ContainsKey(index))
            {
                stat = new LinkStatus(linkId, obj, timeline);
                _links.Add(index, stat);
                requireUpdate = true;
            }
            else
            {
                stat = _links[index];
                if (stat.IsIdChanged(timeline))
                {
                    stat = new LinkStatus(linkId, obj, timeline);
                    RemoveLink(index);
                    _links.Add(index, stat);
                    requireUpdate = true;
                }
                else
                    requireUpdate = stat.IsChanged(timeline);
            }

            if (requireUpdate)
            {
                stat.UpdateData(timeline);
                SetTrafficColor(stat.linkObject, timeline.TrafficSpeed, timeline.TrafficVolume);
                stat.SetEanbled(true);
            }
        }

        public void RemoveLink(int index)
        {
            if (_links.ContainsKey(index))
            {
                LinkStatus stat = _links[index];
                _links.Remove(index);

                if (!_links.Values.Any(x => x.linkId == stat.linkId))
                    stat.SetEanbled(false);
            }
        }

        public void Dispose()
        {
            _links.Clear();
        }

        private void SetTrafficColor(List<GameObject> cityobjs, float speed, float volume)
        {
            foreach (var cityobj in cityobjs)
            {
                //Debug.Log($"Selected Link Name : {cityobj.name}");

                var comp = cityobj.GetComponent<SelectionColor>();
                if (comp == null)
                    comp = cityobj.AddComponent<SelectionColor>();

                var kms = speed * 3.6f; // m/s -> km/h

                // speed, volumeに応じて色を変更
                comp.selectionColor = ColorManipulator.GetColorFromSpeed(kms);
                comp.SendMessage("OnValidate", null, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    public class LinkStatus
    {
        public string linkId { get; private set; }
        public List<GameObject> linkObject { get; private set; }
        public RoadIndicator timestamp { get; private set; }

        public static Material material { get; private set; }

        public LinkStatus(string id, List<GameObject> obj, RoadIndicator data)
        {
            linkId = id;
            linkObject = obj;
            timestamp = data;

            material = material ?? new Material(Shader.Find("HDRP/Unlit"));
            material.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        }

        public void UpdateData(RoadIndicator data)
        {
            timestamp = data;
        }

        public bool IsIdChanged(RoadIndicator data)
        {
            return !(data?.LinkID == timestamp?.LinkID);
        }

        public bool IsChanged(RoadIndicator data)
        {
            return !(data?.TrafficSpeed == timestamp?.TrafficSpeed && data?.TrafficVolume == timestamp?.TrafficVolume);
        }

        public void SetEanbled(bool bEanbled)
        {
            if (linkObject != null)
            {
                foreach (var obj in linkObject)
                {
                    obj.layer = bEanbled ? LayerMask.NameToLayer(HighlightFX.FX_LAYER_TRAFFIC) : LayerMask.NameToLayer(HighlightFX.DEFAULT_LAYER);

                    obj.GetComponent<Renderer>().sharedMaterial = material;
                }
            }
        }
    }
}