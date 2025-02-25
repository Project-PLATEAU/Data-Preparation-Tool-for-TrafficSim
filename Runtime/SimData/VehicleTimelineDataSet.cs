using PLATEAU.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using TrafficSimulationTool.Runtime.Util;
using System.Threading;
using System.Collections;
using System.Threading.Tasks;
using static TrafficSimulationTool.Runtime.SimData.SequenceChunkManager;

namespace TrafficSimulationTool.Runtime.SimData
{
    /// <summary>
    /// Vehicle用メインデータ
    /// </summary>
    public class VehicleTimelineDataSet : IDataSet
    {
        public static readonly bool ENABLE_CHUNK_LOAD = true;

        public string name;
        public List<VehicleTimeline> vehicleTimelines;
        public FrameSlotManager<VehicleFrame> vehicleSlots;
        private SequenceDuration totalDuration;
        private SequenceChunkManager chunkManager;

        public void Initialize(string name, List<object> dataList)
        {
            this.name = name;
            vehicleTimelines = dataList.OfType<VehicleTimeline>().OrderBy(v => v.TimeSeconds).ToList();

            //Index付与
            uint index = 0;
            foreach (VehicleTimeline tl in vehicleTimelines)
                tl.Index = index++;
        }

        public void InitializeReference(GeoReference geoReference)
        {
            foreach (var item in vehicleTimelines)
            {
                item.Initialize(geoReference);
            }
        }

        public void Dispose()
        {
            vehicleSlots?.Dispose(); //NativeArray:Persistentなので必須
            DebugLogger.Log(10, $"NativeArray Disposed.(VehicleTimelineDataSet)");

            vehicleTimelines?.Clear();
            chunkManager?.Dispose();
        }

        public (VehicleTimeline, VehicleTimeline) GetStartEndTimelines(VehicleFrame frame) 
        {
            return ( vehicleTimelines[(int)frame.StartIndex] , vehicleTimelines[(int)frame.EndIndex] );
        }

        public DateTime GetStartTime()
        {
            return TimelineUtil.GetDateTime(vehicleTimelines.First().TimeStamp);
        }

        public DateTime GetEndTime()
        {
            return TimelineUtil.GetDateTime(vehicleTimelines.Last().TimeStamp);
        }

        public void ChangePriority(DateTime currentTime)
        {
            if (totalDuration == null) return;
            var timeSec = TimelineUtil.GetTimeSeconds(currentTime) - totalDuration.StartTimeSeconds;
            chunkManager?.ChangePriority(timeSec);
        }

        public async Task InitializeFrames(SequenceDuration duration, CancellationToken? token = null)
        {
            totalDuration = duration; 
            vehicleSlots = new((int)totalDuration.TotalFrames);

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
                var currentTimelines = vehicleTimelines;
                await Task.Run(() =>{ CreateFrames(0, currentTimelines, token);});
            }

            DebugLogger.Log(10, $"First Task Finish : vehicleSlots Length :{vehicleSlots.Length}", "red");
        }

        public void CreateFrames(ChunkData data, CancellationToken? token = null)
        {
            var chunkIndex = data.Index;

            var start = totalDuration.StartTimeSeconds;
            var currentTimelines = vehicleTimelines.FindAll(v => v.TimeSeconds >= start + data.StartTime && v.TimeSeconds < start + data.EndTime).ToList();

            DebugLogger.Log(10, $"CreateFrames :{TimelineUtil.GetDateTime(start + data.StartTime)} -> {TimelineUtil.GetDateTime(start + data.EndTime)} , timelines : {currentTimelines.Count} / {vehicleTimelines.Count}");

            CreateFrames(chunkIndex, currentTimelines, token);

            DebugLogger.Log(10, $"vehicleSlots Length :{vehicleSlots.Length}", "red");
        }

        private void CreateFrames(int layer, List<VehicleTimeline> currentTimelines, CancellationToken? token = null)
        {
            var vehicleIdList = currentTimelines.Select(v => v.VehicleID).Distinct().ToList();
            DebugLogger.Log(10, $"vehicleIdList : {vehicleIdList.Count}");
            DebugLogger.Log(10, $"currentTimelines Count :{currentTimelines.Count}");

            foreach (var vehicleId in vehicleIdList)
            {
                token?.ThrowIfCancellationRequested();

                var timestamps = currentTimelines.FindAll(v => v.VehicleID == vehicleId).OrderBy(v => v.TimeSeconds).ToList();

                for (int i = 0; i < timestamps.Count - 1; i++) //最後のindexは除去
                {
                    //Debug.Log($"timestamp : {timestamps[i].TimeSeconds - duration.StartTimeSeconds} / {duration.TotalDuration}");
                    int startTimelineIndex = i;
                    int endTimelineIndex = i + 1;
                    CreateIntermediateFrames(layer, timestamps, startTimelineIndex, endTimelineIndex);
                }
            }

            DebugLogger.Log(10, $"vehicleSlots Length :{vehicleSlots.Length}");
        }

        //中間フレーム生成
        private void CreateIntermediateFrames(int layer, List<VehicleTimeline> timlines, int startTimelineIndex, int endTimelineIndex)
        {
            VehicleTimeline startTimeline = timlines[startTimelineIndex];
            VehicleTimeline endTimeline = timlines[endTimelineIndex];

            int startFrameIndex = (int)(startTimeline.TimeSeconds - totalDuration.StartTimeSeconds) * (int)totalDuration.FramesPerSecond;
            int endFrameIndex = (int)(endTimeline.TimeSeconds - totalDuration.StartTimeSeconds) * (int)totalDuration.FramesPerSecond;

            //Debug.Log($"timestamp : {startTimelineIndex} frame start: {startFrameIndex} end:{endFrameIndex}");

            var slot = vehicleSlots.GetAvailableSlot(startFrameIndex, layer);

            for (int j = startFrameIndex; j < endFrameIndex; j++)
            {
                VehicleFrame currentFrame = new();
                currentFrame.Valid = 1;
                currentFrame.StartIndex = timlines[startTimelineIndex].Index;
                currentFrame.EndIndex = timlines[endTimelineIndex].Index;
                currentFrame.Percentage = GetFramePercent(j, startFrameIndex, endFrameIndex);
                currentFrame.Position = Vector3.Lerp(startTimeline.Position, endTimeline.Position, currentFrame.Percentage);

                //Forward Vector設定
                const float MIN_DISTANCE = 3f; //Vector計算に用いる最低移動距離
                //前方のTimeline検索
                for (int i = startTimelineIndex; i > 0; i--)
                {
                    if (Mathf.Abs(Vector3.Distance(endTimeline.Position, timlines[i].Position)) > MIN_DISTANCE)
                    {
                        currentFrame.Vector = (endTimeline.Position - timlines[i].Position).normalized;
                        if (currentFrame.Vector != Vector3.zero)
                            break;
                    }
                }
                //後方のTimeline検索
                if (currentFrame.Vector == Vector3.zero)
                {
                    for (int i = endTimelineIndex; i < timlines.Count; i++)
                    {
                        if (Mathf.Abs(Vector3.Distance(endTimeline.Position, timlines[i].Position)) > MIN_DISTANCE)
                        {
                            currentFrame.Vector = (timlines[i].Position - endTimeline.Position).normalized;
                            if (currentFrame.Vector != Vector3.zero)
                                break;
                        }
                    }
                }
                //Chunk内のTimeline上で移動しない場合ここで、currentFrame.Vector == Vector3.zeroとなる

                if (slot.Frames.Length <= j)
                    Debug.LogError($"Frame Index too short {j} / {slot.Frames.Length}");
                else
                    slot.SetFrame(j, layer, currentFrame);
            }
        }

        private float GetFramePercent(int current, int start, int end)
        {
            float clamped = Math.Clamp(current, start, end);
            float total = Math.Abs(end - start);
            float position = Math.Abs(clamped - start);
            float percent = (float)(position / total);
            return percent;
        }
    }

    public struct VehicleFrame
    {
        public int Valid;
        public uint StartIndex;
        public uint EndIndex;
        public float Percentage;
        public Vector3 Position;
        public Vector3 Vector;
    }
}
