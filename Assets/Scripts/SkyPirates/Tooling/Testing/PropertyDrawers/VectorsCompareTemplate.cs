using System;
using UnityEngine;

namespace DVG.SkyPirates.Tooling.Testing.PropertyDrawers
{
    [Serializable]
    public class VectorsCompareTemplate<Single, Two, Three, Four>
    {
        [SerializeField]
        private CompareTemplate<Single, float> _single;
        [SerializeField]
        private CompareTemplate<Two, Vector2> _two;
        [SerializeField]
        private CompareTemplate<Three, Vector3> _three;
        [SerializeField]
        private CompareTemplate<Four, Vector4> _four;

        public VectorsCompareTemplate(Single single, Two two, Three three, Four four, float singleT, Vector2 twoT, Vector3 threeT, Vector4 fourT)
        {
            _single = new(single, singleT);
            _two = new(two, twoT);
            _three = new(three, threeT);
            _four = new(four, fourT);
        }

        [Serializable]
        private class CompareTemplate<E1, E2>
        {
            [SerializeField]
            private E1 _source;
            [SerializeField]
            private E2 _target;

            [SerializeField]
            private E1[] _sources;
            [SerializeField]
            private E2[] _targets;

            public CompareTemplate(E1 source, E2 target)
            {
                _source = source;
                _target = target;
                _sources = new E1[2]
                {
                    source,
                    source
                };
                _targets = new E2[2]
                {
                    target,
                    target
                };
            }
        }
    }

}
