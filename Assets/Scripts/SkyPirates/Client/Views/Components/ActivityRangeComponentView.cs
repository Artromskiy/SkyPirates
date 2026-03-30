using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Runtime;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public class ActivityRangeComponentView : ComponentView
    {
        public override void OnInject()
        {

        }
        public override void Tick()
        {
            var range = ViewModel.Get<ActivityRange>().Value;
            var pos = ViewModel.Get<Position>().Value.xz;
            fix4 minMax = default;
            minMax.xy = pos - new fix2(range);
            minMax.zw = pos + new fix2(range);
            //                           xz/yw                  xz/yw
            Debug.DrawLine((float3)minMax.xy.x_y, (float3)minMax.xw.x_y, Color.red);
            Debug.DrawLine((float3)minMax.xy.x_y, (float3)minMax.zy.x_y, Color.red);
            Debug.DrawLine((float3)minMax.zw.x_y, (float3)minMax.xw.x_y, Color.red);
            Debug.DrawLine((float3)minMax.zw.x_y, (float3)minMax.zy.x_y, Color.red);

        }
    }
}
