#nullable enable
using DVG.Ids;
using UnityEngine;

namespace DVG.Editor.Configs
{
    public abstract class StringIdEditorConfig<Id, Type> : SingletonConfig<Type>, IStringIdEditorConfig
        where Type : StringIdEditorConfig<Id, Type>
        where Id : IId
    {
        [field: SerializeField]
        public string[] Values { get; private set; } = null!;
    }

    public interface IStringIdEditorConfig
    {
        string[] Values { get; }
    }
}