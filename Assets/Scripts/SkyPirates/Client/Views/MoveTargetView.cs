using DVG.MathsOld;
using DVG.SkyPirates.Client.IViews;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views
{
    public class MoveTargetView : MonoBehaviour, IMoveTargetView
    {
        [SerializeField]
        private CharacterController _controller;

        public vec3 Position => transform.position;
        public angle Rotation => transform.eulerAngles.y;

        public vec2 Direction { set; private get; }

        public void Update()
        {
            _controller.SimpleMove(Direction.x0y);
            var rot = transform.eulerAngles;
            transform.eulerAngles = new(rot.x, Direction.isZero ? rot.y : new angle(Direction).deg, rot.z);
        }
    }
}
