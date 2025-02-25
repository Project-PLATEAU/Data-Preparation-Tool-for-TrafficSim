using PLATEAU.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using TrafficSimulationTool.Runtime.Util;

namespace TrafficSimulationTool.Runtime.SimData
{
    //時間情報を分割して保持し、時間単位ごとのTaskを順番に実行
    public class SequenceChunkManager : IDisposable
    {
        public const double DEFAULT_SEQUENCE_DURATION_SECONDS = 120;//2分

        private double sequenceDurationSeconds;
        private List<ChunkData> chunks;

        private Action<ChunkData, CancellationToken?> dataHandler;
        private List<Task> queue;
        private Dictionary<int, Task> queueCache;

        public Task CurrentTask {  get; private set; }

        public CancellationTokenSource cancellationTokenSource;

        public SequenceChunkManager(double sequenceSizeSec = DEFAULT_SEQUENCE_DURATION_SECONDS)
        {
            sequenceDurationSeconds = sequenceSizeSec;
        }

        public void InitializeAndRun(SequenceDuration duration, Action<ChunkData, CancellationToken?> action)
        {
            Initialize(duration);
            SetAction(action);
            CreateTasks();
            Run();
        }

        public void Initialize(SequenceDuration duration)
        {
            int numDiv = (int)Math.Floor(duration.TotalDuration / sequenceDurationSeconds) + 1;
            chunks = new();
            queue = new();
            queueCache = new();

            //Debug.Log($"Chunks : {numDiv}");

            for (int i = 0; i < numDiv; i++)
            {
                var current = i * sequenceDurationSeconds;
                var chunk = new ChunkData(i, current, current + sequenceDurationSeconds);
                chunks.Add(chunk);

                //Debug.Log($"Chunk : {i} => {chunk.StartTime} : {chunk.EndTime}");
            }
            cancellationTokenSource = new();
        }

        public Task GetTask(int index)
        {
            if (queue.Count > 0　&& index < queue.Count)
                return queue.ElementAt(index);
            return null;
        }

        public void Dispose()
        {
            CurrentTask = null;
            dataHandler = null;
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            chunks.Clear();
            queue.Clear();
            queueCache.Clear();

            DebugLogger.Log(10, "Chunk Tasks Disposed.");
        }

        public void SetAction(Action<ChunkData, CancellationToken?> action)
        {
            dataHandler = action;
        }

        public void CreateTasks()
        {
            foreach (var chunk in chunks)
            {
                var task = new Task(() => CreateTaskFunc(chunk));
                queue.Add(task);
                queueCache.Add(chunk.Index, task);
            }
        }
        private void CreateTaskFunc(ChunkData chunk)
        {
            if (chunk.Status == ChunkData.LoadStatus.Pending)
            {
                chunk.Status = ChunkData.LoadStatus.Loading;
                dataHandler?.Invoke(chunk, cancellationTokenSource.Token);
                chunk.Status = ChunkData.LoadStatus.Completed;
            }
        }

        public async void Run()
        {
            var sw = new DebugStopwatch();
            DebugLogger.Log(10, "Chunk Tasks Started." , "cyan");

            try
            {
                await Task.Run(async () =>
                { 
                    while (queue.Count > 0)
                    {
                        CurrentTask = queue.Pop();
                        CurrentTask.RunSynchronously(TaskScheduler.Default);
                        await CurrentTask; //不要？

                        DebugLogger.Log(10, $"Chunk Task : {chunks.Count - queue.Count} / {chunks.Count} finished.", "yellow");
                    }
                });
            }
            catch (OperationCanceledException)
            {
                DebugLogger.Log(10, "Chunk Tasks Canceled.");
            }
            finally
            {
                Dispose();
            }

            DebugLogger.Log(10, $"All Chunk Tasks finished. {sw.GetTimeSeconds()} ", "cyan");
        }
        

        public void ChangePriority(double currentTime)
        {
            if (queue?.Count <= 0) return;

            var chunk = chunks.Find(x => x.StartTime <= currentTime && x.EndTime > currentTime);
            if(chunk.Status == ChunkData.LoadStatus.Pending)
            {
                if (queueCache.TryGetValue(chunk.Index, out var task))
                {
                    int index = queue.FindIndex(t => t == task);
                    if (queue.TrySwap(index, 0, out var error)) //UnityEngine.Rendering.SwapCollectionExtensionsの機能
                    {
                        DebugLogger.Log(10, $"ChangePriority: {chunk.StartTime} => {chunk.EndTime}  index {chunk.Index}", "purple");
                    }
                    else
                    {
                        DebugLogger.Log(10, $"ChangePriority: error {chunk.Index} : {error.Message} ", "blue");
                    }  
                }
            }
        }

        /// <summary>
        /// Sequenceの塊ごとのStartTime/EndTime を保持
        /// </summary>
        public class ChunkData
        {
            public enum LoadStatus
            {
                Pending,
                Loading,
                Completed
            }

            public int Index;
            public double StartTime;
            public double EndTime;
            public LoadStatus Status;

            public ChunkData(int index, double start, double end)
            {
                Index = index;
                StartTime = start;
                EndTime = end;
                Status = LoadStatus.Pending;
            }
        }
    }
}
