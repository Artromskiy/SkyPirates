using DVG.MathsOld;
using DVG.SkyPirates.Client.IViews;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views
{
    public class MoveTargetView : MonoBehaviour, IMoveTargetView
    {
        [SerializeField]
        private CharacterController _controller;

        public float3 Position => transform.position;
        public angle Rotation => transform.eulerAngles.y;

        public float2 Direction { set; private get; }

        public void Update()
        {
            float3 dir = new(Direction.x, 0, Direction.y);
            _controller.SimpleMove(dir);
            var rot = transform.eulerAngles;
            transform.eulerAngles = new(rot.x, Direction == float2.zero ? rot.y : new angle(Direction).deg, rot.z);
        }
    }
}
