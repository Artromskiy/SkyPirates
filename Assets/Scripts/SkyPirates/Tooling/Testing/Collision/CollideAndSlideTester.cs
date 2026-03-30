using DVG.Physics;
using UnityEngine;

namespace DVG.SkyPirates.Tooling.Testing
{
    public class CollideAndSlideTester : MonoBehaviour
    {
        [SerializeField]
        private fix _radius;
        [SerializeField]
        private Transform _start;
        [SerializeField]
        private Transform _end;
        [SerializeField]
        private Transform _segments;
        [SerializeField]
        private int _iterations;
        [SerializeField]
        private int _skin = 128;

        private void OnDrawGizmos()
        {
            if (!enabled)
                return;
            Gizmos.color = Color.black;
            for (int i = 0; i < _segments.childCount - 1; i++)
            {
                var startPos = ((fix3)_segments.GetChild(i).position).xz;
                var endPos = ((fix3)_segments.GetChild(i + 1).position).xz;

                Gizmos.DrawLine((float3)startPos.x_y, (float3)endPos.x_y);
            }

            Solvers.Segments = new Segment[_segments.childCount - 1];

            for (int i = 0; i < _segments.childCount - 1; i++)
            {
                var segmentStart = ((fix3)_segments.GetChild(i).position).xz;
                var segmentEnd = ((fix3)_segments.GetChild(i + 1).position).xz;

                Solvers.Segments.Span[i] =
                    new Segment(segmentStart, segmentEnd);
            }

            var start = ((fix3)_start.position).xz;
            var end = ((fix3)_end.position).xz;
            var velocity = end - start;

            var selfDir = end - start;
            var selfOffset = fix2.Normalize(new fix2(-selfDir.y, selfDir.x)) * _radius;

            Gizmos.color = Color.green;
            Gizmos.DrawLine((float3)(start + selfOffset).x_y, (float3)(end + selfOffset).x_y);
            Gizmos.DrawLine((float3)(start - selfOffset).x_y, (float3)(end - selfOffset).x_y);
            Gizmos.DrawWireSphere((float3)start.x_y, (float)_radius);
            Gizmos.DrawWireSphere((float3)end.x_y, (float)_radius);


            //Solvers.MaxIterations = _iterations;
            //Solvers.Skin = new(_skin);
            var finalPos = Solvers.CircleSlide(start, velocity, _radius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere((float3)finalPos.x_y, (float)_radius);
            Gizmos.DrawLine((float3)(finalPos + selfOffset).x_y, (float3)(end + selfOffset).x_y);
            Gizmos.DrawLine((float3)(finalPos - selfOffset).x_y, (float3)(end - selfOffset).x_y);
        }
    }
}