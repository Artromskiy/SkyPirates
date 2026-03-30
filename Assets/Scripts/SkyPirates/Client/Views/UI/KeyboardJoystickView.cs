#nullable enable
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Client.IViews;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views
{
    public class KeyboardJoystickView : View<IJoystickVM>
    {
        public override void OnInject() { }

        private void Update()
        {
            var x = Input.GetAxisRaw("Horizontal");
            var y = Input.GetAxisRaw("Vertical");
            var fixation = Input.GetKey(KeyCode.Space);
            fixation = fixation || x != 0 || y != 0;
            var direction = float2.ClampLength(new float2(x, y), 1);
            ViewModel.Joystick = (direction, fixation);
        }
    }
}