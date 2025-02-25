using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TrafficSimulationTool.Runtime.SimData;
using TrafficSimulationTool.Runtime.Util;

namespace TrafficSimulationTool.Runtime.Simulator
{
    /// <summary>
    /// 各Simulatorの時間制御管理
    /// </summary>
    public class TimelineManager : MonoBehaviour, IDisposable
    {
        private List<IPlayable> sequences;

        private SequenceDuration duration;

        private uint currentFrame;

        private bool isPlaying = false;

        private float playbackSpeed = 1f;

        private Coroutine sequenceCoroutine;

        private YieldInstruction WaitTiming;

        public event Action<float, DateTime> OnTimelineUpdated;

        private bool IsValid {  get { return duration != null; } }

        public void SetDurationData(SequenceDuration dur)
        {
            duration = dur;
            currentFrame = 0;
            InvokeTimelineUpdate();
        }

        public void SetSpeed(float speed)
        {
            playbackSpeed = speed;
            WaitTiming = GetWaitTiming();
        }

        public uint GetCurrentFrame()
        {
            return currentFrame;
        }

        public DateTime GetCurrentTime()
        {
            if (!IsValid) return DateTime.Now;

            var percent = GetPercentFromFrame(currentFrame, duration.TotalFrames);
            var span = TimelineUtil.GetTimeSpanByPercent(percent, duration.StartTime, duration.EndTime);
            DateTime currentTime = duration.StartTime + span;
            return currentTime;
        }

        public void Dispose(){}
        public void Play()
        {
            if (!IsValid) return;

            isPlaying = true;
            sequenceCoroutine = StartCoroutine(SequenceEnumerator());
        }

        public void Pause()
        {
            if (!IsValid) return;

            isPlaying = false;
            if (sequenceCoroutine != null)
                StopCoroutine(sequenceCoroutine);
        }

        public void Move(float percent)
        {
            if (!IsValid) return;

            currentFrame = GetFrameFromPercent(percent);
            foreach (var sequence in sequences)
            {
                sequence.PlayFrame(currentFrame);
            }
        }

        private IEnumerator SequenceEnumerator()
        {
            //WaitTiming = new WaitForEndOfFrame();
            WaitTiming = GetWaitTiming();
            while (isPlaying)
            {
                if (currentFrame > duration.TotalFrames - 1)
                    currentFrame = 0;

                foreach (var sequence in sequences)
                {
                    sequence.PlayFrame(currentFrame);
                }

                //Debug.Log($"Enumerate {currentFrame}");
                currentFrame++; 
                InvokeTimelineUpdate();
                yield return WaitTiming;
            }
        }

        private uint GetFrameFromPercent(float percent)
        {
            var len = duration.TotalFrames - 1;
            var frameFloatValue = len * percent;
            uint frame = (uint)Math.Clamp((float)Mathf.Ceil(frameFloatValue), 0f, (float)len);
            return frame;
        }

        private float GetPercentFromFrame(uint position, uint total)
        {
            return (float)position / (float)total;
        }

        private YieldInstruction GetWaitTiming()
        {
            return new WaitForSeconds(1f / duration.FramesPerSecond / playbackSpeed);
        }

        public void AddSequence(IPlayable seq)
        {
            if(sequences  == null)
                sequences = new List<IPlayable>();

            sequences.Add(seq);
        }

        private void InvokeTimelineUpdate()
        {
            var percent = GetPercentFromFrame(currentFrame, duration.TotalFrames);
            OnTimelineUpdated?.Invoke(percent, GetCurrentTime());
        }

    }
    public interface IPlayable: IDisposable
    {
        void Initialize();
        void PlayFrame(uint frame);

    }

}
