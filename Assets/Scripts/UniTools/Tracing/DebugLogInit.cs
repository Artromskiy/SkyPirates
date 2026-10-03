using System;
using DVG;
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
            Logger.Instance = new UnityLogger();
        }
    }

    internal sealed class UnityLogger : ILogger
    {
        [HideInCallstack]
        public void Info(string message, string? method, string? filePath, object? context)
            => Log(UnityEngine.Debug.Log, message, method, filePath, context);

        [HideInCallstack]
        public void Warn(string message, string? method, string? filePath, object? context)
            => Log(UnityEngine.Debug.LogWarning, message, method, filePath, context);

        [HideInCallstack]
        public void Assert(bool condition, string? method, string? filePath, object? context)
        {
            if (condition)
            {
                return;
            }

            UnityEngine.Debug.LogAssertion($"{Location(method, filePath)}: assertion failed", UnityContext(context));
            UnityEngine.Debug.Break();
        }

        [HideInCallstack]
        public void Error(Exception exception, string? method, string? filePath, object? context)
        {
            UnityEngine.Debug.LogError(Location(method, filePath), UnityContext(context));
            UnityEngine.Debug.LogException(exception, UnityContext(context));
            UnityEngine.Debug.Break();
        }

        [HideInCallstack]
        public void Throw(Exception exception, string? method, string? filePath, object? context)
        {
            UnityEngine.Debug.LogError($"{Location(method, filePath)}: throwing exception", UnityContext(context));
            UnityEngine.Debug.LogException(exception, UnityContext(context));
            UnityEngine.Debug.Break();
        }

        [HideInCallstack]
        private static void Log(Action<string, UnityEngine.Object?> log, string message, string? method, string? filePath, object? context)
        {
            string contextText = context is UnityEngine.Object ? string.Empty : context is null ? string.Empty : $" | Context: {context}";
            log($"{Location(method, filePath)}: {message}{contextText}", UnityContext(context));
        }

        private static string Location(string? method, string? filePath)
        {
            string source = string.IsNullOrEmpty(filePath) ? string.Empty : $" ({filePath})";
            return string.IsNullOrEmpty(method) ? $"Unknown caller{source}" : $"{method}{source}";
        }

        private static UnityEngine.Object? UnityContext(object? context) => context as UnityEngine.Object;
    }
}
