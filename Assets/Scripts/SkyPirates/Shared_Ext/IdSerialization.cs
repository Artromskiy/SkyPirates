using UnityEngine;

namespace DVG.SkyPirates.Shared.Ids
{
    public partial struct GoodsId : ISerializationCallbackReceiver
    {
        public void OnAfterDeserialize() => _value = Registry.GetOrCreate(Value);
        public readonly void OnBeforeSerialize() { }
    }

    public partial struct StateId : ISerializationCallbackReceiver
    {
        public void OnAfterDeserialize() => _value = Registry.GetOrCreate(Value);
        public readonly void OnBeforeSerialize() { }
    }
}
