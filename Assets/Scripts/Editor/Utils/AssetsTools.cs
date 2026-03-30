using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DVG.Editor.Tools
{
    public static class AssetsTools
    {
        private static readonly List<DirectoryInfo> _empty = new();

        [MenuItem("Assets/DVG/Clean Empty Folders")]
        public static void Cleanup()
        {
            _empty.Clear();
            var assetsDir = Application.dataPath + Path.DirectorySeparatorChar;
            GetEmptyDirectories(new DirectoryInfo(assetsDir), _empty);

            foreach (var d in _empty)
            {
                FileUtil.DeleteFileOrDirectory(d.FullName);
                FileUtil.DeleteFileOrDirectory($"{d.FullName}.meta");
            }

            Debug.Log($"Removed {_empty.Count} empty folders");
            if (_empty.Count > 0)
                AssetDatabase.Refresh();
        }

        private static bool GetEmptyDirectories(DirectoryInfo dir, List<DirectoryInfo> results)
        {
            bool isEmpty = true;
            try
            {
                isEmpty = dir.GetDirectories().
                    Count(x => !GetEmptyDirectories(x, results)) == 0 &&
                    dir.GetFiles("*.*").All(x => x.Extension == ".meta");

                if (isEmpty)
                    results.Add(dir);
            }
            catch { }

            return isEmpty;
        }
    }
}