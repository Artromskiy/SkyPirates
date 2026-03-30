using DVG.Physics;
using DVG.SkyPirates.Shared.Systems;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DVG.SkyPirates.Tooling.Testing
{
    public class CollideAndSlideRealTester : MonoBehaviour
    {
        [SerializeField]
        private int _solveIndex;
        [SerializeField]
        private List<UnsolvedData> _unsolved = new();

        private void Start()
        {
            HexMapCollisionSystem.OnFailedToSolve += OnFailedToSolve;
        }

        private void OnFailedToSolve(Segment[] segments, fix2 from, fix2 to, fix radius)
        {
            _unsolved.Add(new()
            {
                segments = segments,
                from = from,
                to = to,
                radius = radius,
            });
        }

        private void OnDrawGizmos()
        {
            if (!enabled)
                return;
            if (_solveIndex == -1)
                return;

            Gizmos.color = Color.black;
            var data = _unsolved[_solveIndex];
            var start = data.from;
            var end = data.to;
            var radius = data.radius;
            var velocity = end - start;

            var selfDir = end - start;

            Gizmos.color = Color.green;
            if (fix2.SqrLength(selfDir) > 0)
            {
                var selfOffset = fix2.Normalize(new fix2(-selfDir.y, selfDir.x)) * radius;
                Gizmos.DrawLine((float3)(start + selfOffset).x_y, (float3)(end + selfOffset).x_y);
                Gizmos.DrawLine((float3)(start - selfOffset).x_y, (float3)(end - selfOffset).x_y);
            }

            Gizmos.DrawWireSphere((float3)start.x_y, (float)radius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere((float3)end.x_y, (float)radius);

            Solvers.Segments = data.segments;
            var finalPos = Solvers.CircleSlide(start, velocity, radius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere((float3)finalPos.x_y, (float)radius);

            if (fix2.SqrLength(selfDir) > 0)
            {
                var selfOffset = fix2.Normalize(new fix2(-selfDir.y, selfDir.x)) * radius;
                Gizmos.DrawLine((float3)(finalPos + selfOffset).x_y, (float3)(end + selfOffset).x_y);
                Gizmos.DrawLine((float3)(finalPos - selfOffset).x_y, (float3)(end - selfOffset).x_y);
            }
        }

        [Serializable]
        private struct UnsolvedData
        {
            [HideInInspector]
            public Segment[] segments;
            public fix2 from;
            public fix2 to;
            public fix radius;
        }
    }
}