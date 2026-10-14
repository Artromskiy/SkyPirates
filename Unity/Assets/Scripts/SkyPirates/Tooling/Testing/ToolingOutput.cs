using System.IO;
using UnityEngine;

namespace SkyPirates.Tooling.Testing
{
    internal static class ToolingOutput
    {
        public static string GetJsonPath(string category, string fileName)
        {
            string directory = Path.Combine(Application.persistentDataPath, "Sky Pirates", "Tooling", category);
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, Path.ChangeExtension(fileName, "json"));
        }
    }
}
