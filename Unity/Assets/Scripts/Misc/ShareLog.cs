using Common.UI;
using System.Text;
using UnityEngine;

namespace DVG.Misc
{
    public class ShareLog : MonoBehaviour
    {
        [SerializeField]
        private EventButton _button;

        private void Awake()
        {
            Logger.Init();
            _button.OnClick += Copy;
        }

        private void Copy()
        {
            GUIUtility.systemCopyBuffer = Logger.GetLogs();
            Debug.Log("Logs copied to clipboard.");
        }


        private static class Logger
        {
            private static readonly StringBuilder _logs = new();

            public static void Init()
            { }

            static Logger()
            {
                Application.logMessageReceived += CatchLogs;
            }

            public static string GetLogs() => _logs.ToString();

            private static void CatchLogs(string condition, string stackTrace, LogType type)
            {
                _logs.AppendLine($"{type}: {condition} {stackTrace}");
                _logs.AppendLine();
            }
        }
    }
}
