using System;
using System.Buffers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DVG.Misc
{
    public class FPSCounter : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _avgTotalFpsText;
        [SerializeField]
        private TMP_Text _minTotalFpsText;

        [SerializeField]
        private TMP_Text _avgCpuFpsText;
        [SerializeField]
        private TMP_Text _minCpuFpsText;

        [SerializeField]
        private float _timingDuration = 0.3f;

        private readonly FrameTiming[] _frameTimings = new FrameTiming[1];
        private readonly Queue<(FrameTiming data, float time)> _timings = new();

        private void Update()
        {
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _frameTimings) > 0)
            {
                while (_timings.TryPeek(out var t) && t.time < Time.unscaledTime - _timingDuration)
                    _timings.Dequeue();
                _timings.Enqueue((_frameTimings[0], Time.unscaledTime));
            }
            double avgMaxTotal = 0;
            double avgMaxCpu = 0;
            double avgTotal = 0;
            double avgCpu = 0;

            (FrameTiming data, float time)[] timings = ArrayPool<(FrameTiming, float)>.Shared.Rent(_timings.Count);
            _timings.CopyTo(timings, 0);
            for (int i = 0; i < _timings.Count; i++)
            {
                var (data, time) = timings[i];
                avgTotal += data.cpuFrameTime;
                avgCpu += data.cpuMainThreadFrameTime;
            }

            int tenPercCount = Maths.Min(_timings.Count, Maths.Max(1, _timings.Count / 10));

            Array.Sort(timings, (t1, t2) => t2.data.cpuFrameTime.CompareTo(t1.data.cpuFrameTime));
            for (int i = 0; i < tenPercCount; i++)
                avgMaxTotal += timings[i].data.cpuFrameTime;

            Array.Sort(timings, (t1, t2) => t2.data.cpuMainThreadFrameTime.CompareTo(t1.data.cpuMainThreadFrameTime));
            for (int i = 0; i < tenPercCount; i++)
                avgMaxCpu += timings[i].data.cpuMainThreadFrameTime;

            ArrayPool<(FrameTiming, float)>.Shared.Return(timings);

            avgTotal /= _timings.Count;
            avgCpu /= _timings.Count;

            avgMaxTotal /= tenPercCount;
            avgMaxCpu /= tenPercCount;

            if (_avgTotalFpsText) _avgTotalFpsText.text = $"All Avg: {ToFps(avgTotal)}";
            if (_minTotalFpsText) _minTotalFpsText.text = $"All Min: {ToFps(avgMaxTotal)}";
            if (_avgCpuFpsText) _avgCpuFpsText.text = $"Cpu Avg: {ToFps(avgCpu)}";
            if (_minCpuFpsText) _minCpuFpsText.text = $"Cpu Min: {ToFps(avgMaxCpu)}";
        }

        private int ToFps(double timeMs) => (int)(1000.0 / timeMs);
    }
}