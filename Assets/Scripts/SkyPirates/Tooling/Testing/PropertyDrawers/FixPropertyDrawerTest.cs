using UnityEngine;

namespace DVG.SkyPirates.Tooling.Testing.PropertyDrawers
{
    public class FixPropertyDrawerTest : MonoBehaviour
    {
        [SerializeField]
        private VectorsCompareTemplate<fix, fix2, fix3, fix4> _fixes = new
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