using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DVG.Misc
{
    public class ShareLog : MonoBehaviour
    {
        [SerializeField]
        private Button _button;

        private void Awake()
        {
            Logger.Init();
            _button.onClick.AddListener(Share);
        }

        private void Share()
        {
            var writeTo = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(writeTo, Logger.GetLogs());
            new NativeShare().AddFile(writeTo).Share();
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
