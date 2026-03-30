using DVG.UniTools.Tracing;
using System;
using System.Diagnostics;

namespace DVG.SkyPirates.Client.Init
{
    public class DebugLogInit
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        private static void Init()
        {
            UnityEngine.Debug.Log($"{nameof(DebugLogInit)}");

            Console.SetOut(new UnityDebugLogWriter());
            Trace.Listeners.Add(new DelegateTraceListener(UnityEngine.Debug.LogError, TraceEventType.Critical | TraceEventType.Error));
            Trace.Listeners.Add(new DelegateTraceListener(UnityEngine.Debug.LogWarning, TraceEventType.Warning));
            Trace.Listeners.Add(new DelegateTraceListener(UnityEngine.Debug.Log, TraceEventType.Information | TraceEventType.Verbose));
            Trace.Listeners.Add(new UnityTraceFailListener());
        }
    }
}