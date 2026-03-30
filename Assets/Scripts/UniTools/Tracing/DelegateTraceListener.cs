using System;
using System.Diagnostics;

namespace DVG.UniTools.Tracing
{
    internal class DelegateTraceListener : TraceListener
    {
        private readonly Action<string> _listener;
        private bool _tracing;

        public DelegateTraceListener(Action<string> listener, TraceEventType traceMask)
        {
            _listener = listener;
            Filter = new SpecificFilter(traceMask);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            _tracing = true;
            base.TraceEvent(eventCache, source, eventType, id, format, args);
            _tracing = false;
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            _tracing = true;
            base.TraceEvent(eventCache, source, eventType, id, message);
            _tracing = false;
        }

        public override void WriteLine(string message)
        {
            if (_tracing)
                _listener?.Invoke(message);
        }
        public override void Write(string message) { }

        private class SpecificFilter : TraceFilter
        {
            private readonly TraceEventType _traceMask;

            public SpecificFilter(TraceEventType traceMask)
            {
                _traceMask = traceMask;
            }

            public override bool ShouldTrace(TraceEventCache cache, string source, TraceEventType eventType, int id, string formatOrMessage, object[] args, object data1, object[] data)
            {
                return _traceMask.HasFlag(eventType);
            }
        }
    }
}