using System.IO;
using System.Text;

namespace DVG.UniTools.Tracing
{
    internal class UnityDebugLogWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
        public override void Write(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            UnityEngine.Debug.Log(value);
        }
    }
}