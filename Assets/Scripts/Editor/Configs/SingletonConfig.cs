#nullable enable
using UnityEditor;
using UnityEngine;

namespace DVG.Editor.Configs
{
    public abstract class SingletonConfig<Type> : ScriptableObject
        where Type : SingletonConfig<Type>
    {
        private const string Folder = "Assets/Editor/Configs/";
        private static readonly string Path = $"{Folder}{typeof(Type).Name}.asset";

        private static Type? _instance;
        public static Type Instance => _instance = _instance == null ? AssetDatabase.LoadAssetAtPath<Type>(Path) : _instance;
    }
}
