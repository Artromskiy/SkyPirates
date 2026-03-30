using UnityEngine;

namespace DVG.SkyPirates.Tooling.Testing.PropertyDrawers
{
    public class FloatPropertyDrawerTest : MonoBehaviour
    {
        [SerializeField]
        private VectorsCompareTemplate<float, float2, float3, float4> _floats = new
        (
            1,
            new(1),
            new(1),
            new(1),
            1,
            new(1, 1),
            new(1, 1, 1),
            new(1, 1, 1, 1)
        );
    }
}
