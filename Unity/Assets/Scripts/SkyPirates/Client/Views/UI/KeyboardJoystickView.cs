#nullable enable
using Delta;
using SkyPirates.Client.IViewModels;
using SkyPirates.Client.IViews;
using UnityEngine;

namespace SkyPirates.Client.Views
{
    public class KeyboardJoystickView : View<IJoystickVM>
    {
        public override void OnInject() { }

        private void Update()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");
            bool fixation = Input.GetKey(KeyCode.Space);
            fixation = fixation || x != 0 || y != 0;
            var direction = float2.ClampLength(new float2(x, y), 1);
            ViewModel.Joystick = (direction, fixation);
        }
    }
}