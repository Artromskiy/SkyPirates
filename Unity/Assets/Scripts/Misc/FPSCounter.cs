using System;
using System.Buffers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DVG.Misc
{
    public class FPSCounter : MonoBehaviour
    {
        private static readonly Comparer<double> ValueComparer = Comparer<double>.Default;

        [SerializeField] private TMP_Text _textPrefab;
        [SerializeField] private Transform _textParent;
        [SerializeField] private float _timingDuration = 0.3f;
        [SerializeField] private float _textUpdateSeconds = 0.1f;
        [SerializeField] private MetricSelection _all = new() { Average = true, MinTen = true };
        [SerializeField] private MetricSelection _cpu = new() { Average = true, MinTen = true };
        [SerializeField] private MetricSelection _gpu = new();
        [SerializeField] private MetricSelection _render = new();

        private readonly FrameTiming[] _frameTimings = new FrameTiming[1];
        private readonly Queue<(FrameTiming Data, float Time)> _timings = new();
        private readonly List<StatisticSource> _sources = new();
        private readonly List<StatisticView> _views = new();
        private StatisticValues[] _statistics = Array.Empty<StatisticValues>();
        private float _textUpdateTimer;

        private void Start()
        {
            RegisterStatistics();
            _statistics = new StatisticValues[_sources.Count];
            CreateStatisticViews();
        }

        private void Update()
        {
            _textUpdateTimer -= Time.unscaledDeltaTime;

            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _frameTimings) > 0)
            {
                while (_timings.TryPeek(out var timing) && timing.Time < Time.unscaledTime - _timingDuration)
                {
                    _timings.Dequeue();
                }

                _timings.Enqueue((_frameTimings[0], Time.unscaledTime));
            }

            if (_timings.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _sources.Count; i++)
            {
                StatisticSource source = _sources[i];
                if (source.Selection.HasAny)
                {
                    _statistics[i] = CalculateStatistics(source.Getter, source.Selection);
                }
            }

            if (_textUpdateTimer > 0f)
            {
                return;
            }

            _textUpdateTimer = Math.Max(0f, _textUpdateSeconds);
            for (int i = 0; i < _views.Count; i++)
            {
                StatisticView view = _views[i];
                StatisticSource source = _sources[view.SourceIndex];
                view.Update(source.Name, _statistics[view.SourceIndex], source.Formatter);
            }
        }

        private void RegisterStatistics()
        {
            _sources.Clear();
            _sources.Add(new StatisticSource("All", static data => data.cpuFrameTime, ToFps, _all));
            _sources.Add(new StatisticSource("Cpu", static data => data.cpuMainThreadFrameTime, ToFps, _cpu));
            _sources.Add(new StatisticSource("Gpu", static data => data.gpuFrameTime, ToFps, _gpu));
            _sources.Add(new StatisticSource("Ren", static data => data.cpuRenderThreadFrameTime, ToFps, _render));
        }

        private void CreateStatisticViews()
        {
            Transform parent = _textParent;
            var existingChildren = new Transform[parent.childCount];
            for (int i = 0; i < existingChildren.Length; i++)
            {
                existingChildren[i] = parent.GetChild(i);
            }

            _views.Clear();
            for (int sourceIndex = 0; sourceIndex < _sources.Count; sourceIndex++)
            {
                StatisticSource source = _sources[sourceIndex];
                var view = new StatisticView(sourceIndex);
                if (source.Selection.Median)
                {
                    view.Add(StatisticMetric.Median, CreateText(parent, $"{source.Name} Median"));
                }
                if (source.Selection.Average)
                {
                    view.Add(StatisticMetric.Average, CreateText(parent, $"{source.Name} Average"));
                }
                if (source.Selection.MinTen)
                {
                    view.Add(StatisticMetric.MinTen, CreateText(parent, $"{source.Name} Min 10%"));
                }
                if (source.Selection.MaxTen)
                {
                    view.Add(StatisticMetric.MaxTen, CreateText(parent, $"{source.Name} Max 10%"));
                }

                if (view.HasMetrics)
                {
                    _views.Add(view);
                }
            }

            for (int i = 0; i < existingChildren.Length; i++)
            {
                Destroy(existingChildren[i].gameObject);
            }
        }

        private TMP_Text CreateText(Transform parent, string objectName)
        {
            TMP_Text text = Instantiate(_textPrefab, parent, false);
            text.name = objectName;
            text.text = string.Empty;
            return text;
        }

        private StatisticValues CalculateStatistics(Func<FrameTiming, double> getter, MetricSelection selection)
        {
            int count = _timings.Count;
            bool needsSortedValues = selection.Median || selection.MinTen || selection.MaxTen;
            double[] values = needsSortedValues ? ArrayPool<double>.Shared.Rent(count) : Array.Empty<double>();
            double sum = 0d;
            int index = 0;

            foreach (var timing in _timings)
            {
                double value = getter(timing.Data);
                if (needsSortedValues)
                {
                    values[index++] = value;
                }
                if (selection.Average)
                {
                    sum += value;
                }
            }

            if (needsSortedValues)
            {
                Array.Sort(values, 0, count, ValueComparer);
            }

            int tenPercentCount = selection.MinTen || selection.MaxTen ? Math.Max(1, count / 10) : 0;
            double minTen = 0d;
            double maxTen = 0d;
            for (int i = 0; i < tenPercentCount; i++)
            {
                if (selection.MinTen)
                {
                    minTen += values[i];
                }
                if (selection.MaxTen)
                {
                    maxTen += values[count - i - 1];
                }
            }

            double median = selection.Median
                ? count % 2 == 0
                    ? (values[count / 2 - 1] + values[count / 2]) * 0.5
                    : values[count / 2]
                : 0d;

            if (needsSortedValues)
            {
                ArrayPool<double>.Shared.Return(values);
            }

            return new StatisticValues(
                median,
                selection.Average ? sum / count : 0d,
                selection.MinTen ? minTen / tenPercentCount : 0d,
                selection.MaxTen ? maxTen / tenPercentCount : 0d);
        }

        private static string ToFps(double timeMs) => timeMs > 0 ? $"{1000.0 / timeMs:F0} fps" : "N/A";

        private readonly struct StatisticSource
        {
            public readonly string Name;
            public readonly Func<FrameTiming, double> Getter;
            public readonly Func<double, string> Formatter;
            public readonly MetricSelection Selection;

            public StatisticSource(string name, Func<FrameTiming, double> getter, Func<double, string> formatter,
                MetricSelection selection)
            {
                Name = name;
                Getter = getter;
                Formatter = formatter;
                Selection = selection;
            }
        }

        [Serializable]
        private sealed class MetricSelection
        {
            public bool Median;
            public bool Average;
            public bool MinTen;
            public bool MaxTen;

            public bool HasAny => Median || Average || MinTen || MaxTen;
        }

        private readonly struct StatisticValues
        {
            public readonly double Median;
            public readonly double Average;
            public readonly double MinTen;
            public readonly double MaxTen;

            public StatisticValues(double median, double average, double minTen, double maxTen)
            {
                Median = median;
                Average = average;
                MinTen = minTen;
                MaxTen = maxTen;
            }
        }

        private enum StatisticMetric : byte
        {
            Median,
            Average,
            MinTen,
            MaxTen,
        }

        private sealed class StatisticView
        {
            private readonly List<(StatisticMetric Metric, TMP_Text Text)> _metrics = new();

            public int SourceIndex { get; }
            public bool HasMetrics => _metrics.Count != 0;

            public StatisticView(int sourceIndex) => SourceIndex = sourceIndex;

            public void Add(StatisticMetric metric, TMP_Text text) => _metrics.Add((metric, text));

            public void Update(string name, StatisticValues values, Func<double, string> formatter)
            {
                foreach (var metric in _metrics)
                {
                    metric.Text.text = metric.Metric switch
                    {
                        StatisticMetric.Median => $"{name} Median: {formatter(values.Median)}",
                        StatisticMetric.Average => $"{name} Average: {formatter(values.Average)}",
                        StatisticMetric.MinTen => $"{name} Min 10%: {formatter(values.MinTen)}",
                        StatisticMetric.MaxTen => $"{name} Max 10%: {formatter(values.MaxTen)}",
                        _ => throw new ArgumentOutOfRangeException(),
                    };
                }
            }
        }
    }
}
