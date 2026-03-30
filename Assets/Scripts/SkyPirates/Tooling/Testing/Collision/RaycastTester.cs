using DVG.Physics;
using UnityEngine;

namespace DVG.SkyPirates.Tooling.Testing
{
    public class RaycastTester : MonoBehaviour
    {
        [SerializeField]
        private Transform _rayStart;
        [SerializeField]
        private Transform _rayEnd;

        [SerializeField]
        private Transform _segments;


        private void OnDrawGizmos()
        {
            Gizmos.color = Color.black;
            for (int i = 0; i < _segments.childCount - 1; i++)
                Gizmos.DrawLine(_segments.GetChild(i).position, _segments.GetChild(i + 1).position);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(_rayStart.position, _rayEnd.position);
            var start = (fix2)((float3)_rayStart.position).xz;
            var end = (fix2)((float3)_rayEnd.position).xz;

            Solvers.Segments = new Segment[_segments.childCount - 1];

            for (int i = 0; i < _segments.childCount - 1; i++)
            {
                var segmentStart = (fix2)((float3)_segments.GetChild(i).position).xz;
                var segmentEnd = (fix2)((float3)_segments.GetChild(i + 1).position).xz;
                Solvers.Segments.Span[i] = new Segment(segmentStart, segmentEnd);
            }

            if (Solvers.RayCast(start, end, out var collision))
            {
                Gizmos.color = Color.red;
                var contact = ((float2)collision.Contact).x_y;
                var normal = ((float2)collision.Normal).x_y;
                Gizmos.DrawLine(((float2)end).x_y, contact);
                Gizmos.DrawWireSphere(contact, 0.1f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(contact, normal);
            }
            else
            {
                Gizmos.DrawWireSphere(((float2)end).x_y, 0.1f);
            }
        }
    }
}