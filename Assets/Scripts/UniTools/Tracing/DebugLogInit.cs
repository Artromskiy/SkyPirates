using System;
using Delta.Diagnostics;
using UnityEngine;

namespace DVG.SkyPirates.Client.Init
{
    public class DebugLogInit
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Delta.Diagnostics.Logger.Instance = new UnityLogger();
        }
    }

    internal sealed class UnityLogger : Delta.Diagnostics.ILogger
    {
        [HideInCallstack]
        public void Info(string message, string? method, object? context)
            => Log(UnityEngine.Debug.Log, message, method, context);

        [HideInCallstack]
        public void Warn(string message, string? method, object? context)
            => Log(UnityEngine.Debug.LogWarning, message, method, context);

        [HideInCallstack]
        public void Assert(bool condition, string? method, object? context)
        {
            if (condition)
            {
                return;
            }

            UnityEngine.Debug.LogAssertion(FormatMessage("assertion failed", method, context), UnityContext(context));
            UnityEngine.Debug.Break();
        }

        [HideInCallstack]
        public void Error(Exception exception, string? method, object? context)
        {
            UnityEngine.Debug.LogError(FormatPrefix(method, context), UnityContext(context));
            UnityEngine.Debug.LogException(exception, UnityContext(context));
            UnityEngine.Debug.Break();
        }

        [HideInCallstack]
        public void Throw(Exception exception, string? method, object? context)
        {
            UnityEngine.Debug.LogError(FormatMessage("throwing exception", method, context), UnityContext(context));
            UnityEngine.Debug.LogException(exception, UnityContext(context));
            UnityEngine.Debug.Break();
        }

        [HideInCallstack]
        private static void Log(Action<string, UnityEngine.Object?> log, string message, string? method, object? context)
        {
            log(FormatMessage(message, method, context), UnityContext(context));
        }

        private static string FormatPrefix(string? method, object? context)
        {
            var prefix = string.Empty;
            if (context is string text)
                prefix = $"[{text}]";
            else if (context is not null && context is not UnityEngine.Object)
                prefix = $"[{context.GetType().FullName}]";

            if (!string.IsNullOrEmpty(method))
                prefix = string.IsNullOrEmpty(prefix) ? $"[{method}]" : $"{prefix} [{method}]";

            return prefix;
        }

        private static string FormatMessage(string message, string? method, object? context)
        {
            var prefix = FormatPrefix(method, context);
            return string.IsNullOrEmpty(prefix)
                ? message
                : string.IsNullOrEmpty(message) ? prefix : $"{prefix} {message}";
        }

        private static UnityEngine.Object? UnityContext(object? context) => context as UnityEngine.Object;
    }
}
