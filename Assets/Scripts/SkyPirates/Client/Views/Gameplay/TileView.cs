using DVG.Core;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Client.IViews;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Gameplay
{
    public class TileView : View<ITileVM>
    {
        private static readonly fix _radius = fix.One / 3;

        public override void OnInject()
        {
            transform.position = ViewModel.Position;
        }

        private void OnDrawGizmosSelected()
        {
            fix2 pos = (fix2)((float3)transform.position).xz;
            var center = pos;
            Gizmos.color = Color.magenta;
            for (int j = 0; j < Hex.Points.Length; j++)
            {
                var a = center + Hex.Points[j];
                var b = center + Hex.Points[(j + 1) % Hex.Points.Length];

                fix2 normal = Hex.Normals[j];
                var offset = fix2.Normalize(normal) * _radius;
                var newA = a + offset;
                var newB = b + offset;
                Gizmos.DrawLine((float3)a.x_y, (float3)b.x_y);
                Gizmos.DrawLine((float3)newA.x_y, (float3)newB.x_y);
                Gizmos.DrawWireSphere((float3)a.x_y, (float)_radius);
            }
        }
    }
}
