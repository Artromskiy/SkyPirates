using DVG.Physics;
using UnityEngine;

namespace DVG.SkyPirates.Tooling.Testing
{
    public class CircleCastTester : MonoBehaviour
    {
        [SerializeField]
        private fix _radius;
        [SerializeField]
        private Transform _rayStart;
        [SerializeField]
        private Transform _rayEnd;

        [SerializeField]
        private Transform _segments;


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

            Gizmos.color = Color.green;
            var start = ((fix3)_rayStart.position).xz;
            var end = ((fix3)_rayEnd.position).xz;
            var selfDir = end - start;
            var selfOffset = fix2.Normalize(new fix2(-selfDir.y, selfDir.x)) * _radius;

            Gizmos.DrawLine((float3)(start + selfOffset).x_y, (float3)(end + selfOffset).x_y);
            Gizmos.DrawLine((float3)(start - selfOffset).x_y, (float3)(end - selfOffset).x_y);
            Gizmos.DrawWireSphere((float3)start.x_y, (float)_radius);

            Solvers.Segments = new Segment[_segments.childCount - 1];

            for (int i = 0; i < _segments.childCount - 1; i++)
            {
                var segmentStart = ((fix3)_segments.GetChild(i).position).xz;
                var segmentEnd = ((fix3)_segments.GetChild(i + 1).position).xz;
                Solvers.Segments.Span[i] = new Segment(segmentStart, segmentEnd);
            }

            if (Solvers.CircleCast(start, end, _radius, out var collision))
            {
                Gizmos.color = Color.red;
                var contact = collision.Contact;
                var normal = collision.Normal;

                Gizmos.DrawWireSphere((float3)contact.x_y, (float)_radius);
                Gizmos.DrawLine((float3)(contact + selfOffset).x_y, (float3)(end + selfOffset).x_y);
                Gizmos.DrawLine((float3)(contact - selfOffset).x_y, (float3)(end - selfOffset).x_y);

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere((float3)(contact + normal).x_y, (float)_radius);
            }
            else
            {
                Gizmos.DrawWireSphere(((float2)end).x_y, (float)_radius);
            }
        }
    }
}