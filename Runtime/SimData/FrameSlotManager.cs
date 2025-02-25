using System;
using System.Collections.Generic;
using System.Linq;
using TrafficSimulationTool.Runtime.Util;
using Unity.Collections;

namespace TrafficSimulationTool.Runtime.SimData
{
    /// <summary>
    /// Frame情報保持スロット
    /// 可変で数が増減するデータのFrameリストをNativeArrayで生成
    /// 空きスロットのフレーム配列に追記していき、足りなくなったらスロットを新規生成
    /// 自動車であれば、スロット数 = 同一時間に存在している自動車の数 
    /// layer -> chunk単位のシーケンスのindex
    /// 
    /// [注意] NativeArray:Persistentなので、Dispose必須
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FrameSlotManager<T> : IDisposable where T : struct
    {
        private int totalFrames;

        private List<FrameSlot<T>> slots = new List<FrameSlot<T>>();

        public IReadOnlyList<FrameSlot<T>> Slots => slots;

        public int Length => slots.Count;

        private readonly object lockObj = new object();

        public FrameSlotManager(int frames)
        {
            totalFrames = frames;
        }

        public FrameSlot<T> GetAvailableSlot(int index, int layer)
        {
            lock (lockObj)
            {
                var slot = slots.Find(x => x.IsAvailable(index,layer));
                if (slot == null)
                {
                    slot = CreateSlot();
                }
                return slot;
            }
        }

        private FrameSlot<T> CreateSlot()
        {
            lock (lockObj)
            {
                var slot = new FrameSlot<T>();
                slot.CreateFrames(slots.Count, totalFrames);
                slots.Add(slot);
                return slot;
            }
        }

        public void Dispose()
        {
            foreach (var item in slots)
            {
                item.Dispose();
            }
            slots.Clear();
        }
    }

    //並列スレッド対応版
    public class FrameSlot<T> : IDisposable where T : struct
    {
        private int id;
        private NativeArray<T> frames;

        private List<int> lastFrameIndexes = new(); // Sequence Layerごとの最終Index

        public int ID => id;

        public NativeArray<T> Frames => frames;

        private readonly object lockObj = new object();

        public T GetFrame(int frame)
        {
            return frames[frame];
        }

        public void CreateFrames(int idx, int totalFrames)
        {
            lock (lockObj)
            {
                id = idx;
                frames = new(totalFrames, Allocator.Persistent);
            }
        }

        public void SetFrame(int index, int layer, T frame)
        {
            lock (lockObj)
            {
                frames[index] = frame;

                if (lastFrameIndexes.Count < layer + 1)
                {
                    //ListサイズをLayerに合わせて増加
                    lastFrameIndexes.Resize(layer + 1);
                }
                lastFrameIndexes[layer] = index;
            }
        }

        public bool IsAvailable(int index, int layer)
        {
            lock (lockObj)
            {
                if (layer < lastFrameIndexes.Count)
                    return lastFrameIndexes[layer] < index;

                return true;
            }
        }

        public void Dispose()
        {
            frames.Dispose(); //NativeArray:Persistentなので必須
            lastFrameIndexes?.Clear();

            //Debug.Log($"NativeArray Disposed.");
        }
    }
}
