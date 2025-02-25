using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using TrafficSimulationTool.Runtime.Util;
using PLATEAU.Geometries;
using System.Threading;
using System.Threading.Tasks;
using static TrafficSimulationTool.Runtime.SimData.SequenceChunkManager;

namespace TrafficSimulationTool.Runtime.SimData
{
    /// <summary>
    /// Traffic用メインデータ
    /// </summary>
    public class RoadIndicatorDataSet : IDataSet
    {
        public static readonly bool ENABLE_CHUNK_LOAD = true;

        public string name;
        public List<RoadIndicator> roadIndicators;
        public List<string> linkList;

        private SequenceDuration totalDuration;
        private SequenceChunkManager chunkManager;
        public FrameSlotManager<TrafficFrame> trafficSlots;

        public void Initialize(string name, List<object> dataList)
        {
            this.name = name;
            roadIndicators = dataList.OfType<RoadIndicator>().OrderBy(r => r.StartTime).ToList();

            //Index付与
            uint index = 0;
            foreach (RoadIndicator tl in roadIndicators)
                tl.Index = index++;
        }

        public void Dispose()
        {
            trafficSlots?.Dispose(); //NativeArray:Persistentなので必須
            DebugLogger.Log(10, $"NativeArray Disposed.(RoadIndicatorDataSet)");

            chunkManager?.Dispose();
            roadIndicators?.Clear();
        }

        public DateTime GetStartTime()
        {
            return TimelineUtil.GetDateTime(roadIndicators.First().StartTime + "00"); // yyyyMMddHHmm + ss
        }

        public DateTime GetEndTime()
        {
            var sortByEndTime = roadIndicators.OrderBy(r => r.EndTime).ToList();
            return TimelineUtil.GetDateTime(sortByEndTime.Last().EndTime + "00"); // yyyyMMddHHmm + ss
        }

        public void ChangePriority(DateTime currentTime)
        {
            if (totalDuration == null) return;
            var timeSec = TimelineUtil.GetTimeSeconds(currentTime) - totalDuration.StartTimeSeconds;
            chunkManager?.ChangePriority(timeSec);
        }

        public RoadIndicator GetTimeline(int timelineIndex)
        {
            return roadIndicators[timelineIndex];
        }

        public async Task InitializeFrames(SequenceDuration duration, CancellationToken? token = null)
        {
            totalDuration = duration;

            trafficSlots = new((int)totalDuration.TotalFrames);

            if (ENABLE_CHUNK_LOAD)
            {
                chunkManager = new SequenceChunkManager();
                chunkManager.InitializeAndRun(totalDuration, CreateFrames);
                var task = chunkManager.GetTask(0); //1つ目を待機
                if (task != null)
                    await task;
            }
            else
            {
                //chunk不使用
                var currentTimelines = roadIndicators;
                await Task.Run(() => { CreateFrames(0, currentTimelines, token); });
            }

            DebugLogger.Log(10, $"First Task Finish : trafficSlots Length :{trafficSlots.Length} ", "red");
        }

        public void CreateFrames(ChunkData data, CancellationToken? token = null)
        {
            var chunkIndex = data.Index;
            var start = totalDuration.StartTimeSeconds;
            var currentTimelines = roadIndicators.FindAll(v => v.StartTimeSeconds >= start + data.StartTime && v.StartTimeSeconds < start + data.EndTime).ToList();
            CreateFrames(chunkIndex, currentTimelines, token);
        }

        public void CreateFrames(int layer, List<RoadIndicator> currentTimelines, CancellationToken? token = null)
        {
            linkList = currentTimelines.Select(l => l.LinkID).Distinct().ToList();

            DebugLogger.Log(10, $"linkList Count : {linkList.Count} layer: {layer}");

            foreach (var linkId in linkList)
            {
                token?.ThrowIfCancellationRequested();

                var timestamps = currentTimelines.FindAll(l => l.LinkID == linkId).OrderBy(v => v.StartTime).ToList();

                var frames = new NativeArray<TrafficFrame>((int)totalDuration.TotalFrames, Allocator.Persistent);

                RoadIndicator startTimeline = timestamps.FirstOrDefault();
                int startIndex = (int)(startTimeline.StartTimeSeconds - totalDuration.StartTimeSeconds) * (int)totalDuration.FramesPerSecond;
                var slot = trafficSlots.GetAvailableSlot(startIndex, layer);

                for (int i = 0; i < timestamps.Count; i++)
                {
                    var timestamp = timestamps[i];
                    int startFrameIndex = (int)(timestamp.StartTimeSeconds - totalDuration.StartTimeSeconds) * (int)totalDuration.FramesPerSecond;
                    int endFrameIndex = (int)(timestamp.EndTimeSeconds - totalDuration.StartTimeSeconds) * (int)totalDuration.FramesPerSecond;

                    //中間フレーム生成
                    for (int j = startFrameIndex; j < endFrameIndex; j++)
                    {
                        TrafficFrame frame = new();
                        frame.Valid = 1;
                        frame.Index = (int)timestamp.Index;
                        slot.SetFrame(j, layer, frame);
                    }
                }
            }
        }
    }

    public struct TrafficFrame
    {
        public int Valid;
        public int Index;
    }
}