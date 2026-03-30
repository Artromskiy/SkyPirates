using System.Diagnostics;

namespace DVG.UniTools.Tracing
{
    internal class UnityTraceFailListener : TraceListener
    {
        public override void Fail(string message)
        {
            UnityEngine.Debug.LogAssertion(message);
            UnityEngine.Debug.LogError(message);
            UnityEngine.Debug.Break();
        }
        public override void Write(string message) { }
        public override void WriteLine(string message) { }
    }
}