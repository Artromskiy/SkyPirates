using DVG.Physics;
using UnityEngine;

namespace DVG.SkyPirates.Tooling.Testing
{
    public class RayCircleTester : MonoBehaviour
    {
        [SerializeField]
        private fix _radius;

        [SerializeField]
        private Transform _rayStart;
        [SerializeField]
        private Transform _rayEnd;

        [SerializeField]
        private Transform _circleCenter;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(_circleCenter.position, (float)_radius);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(_rayStart.position, _rayEnd.position);
            var start = (fix2)((float3)_rayStart.position).xz;
            var end = (fix2)((float3)_rayEnd.position).xz;
            var center = (fix2)((float3)_circleCenter.position).xz;

            if (Spatial.Intersects(center, _radius, start, end, out var collision))
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
