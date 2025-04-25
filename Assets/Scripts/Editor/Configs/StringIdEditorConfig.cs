using DVG.Core.Ids;
using UnityEngine;

namespace DVG.Editor.Configs
{
    public abstract class StringIdEditorConfig<Id, Type> : SingletonConfig<Type>, IStringIdEditorConfig
        where Type : StringIdEditorConfig<Id, Type>
        where Id : IStringId
    {
        [field: SerializeField]
        public string[] Values { get; private set; }
    }

    public interface IStringIdEditorConfig
    {
        public string[] Values { get; }
    }
}