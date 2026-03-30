using UnityEngine;

namespace DVG.SkyPirates.Tooling.Testing.PropertyDrawers
{
    public class DoublePropertyDrawerTest : MonoBehaviour
    {
        [SerializeField]
        private VectorsCompareTemplate<double, double2, double3, double4> _doubles = new
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
