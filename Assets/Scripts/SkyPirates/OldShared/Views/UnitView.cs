using DVG.SkyPirates.Shared.IViews;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views
{
    public class UnitView : MonoBehaviour, IUnitView
    {
        public float Rotation { get => transform.eulerAngles.y; set => transform.eulerAngles = new(0, value, 0); }
        public float3 Position { get => transform.position; set => transform.position = value; }
    }
}