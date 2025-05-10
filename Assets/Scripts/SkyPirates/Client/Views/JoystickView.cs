#nullable enable
using DVG.SkyPirates.Client.IViews;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views
{
    public class JoystickView : MonoBehaviour, IJoystickView
    {
        public float2 Direction { get; private set; }
        public bool Fixation { get; private set; }

        private void Update()
        {
            var x = Input.GetAxisRaw("Horizontal");
            var y = Input.GetAxisRaw("Vertical");
            Fixation = Input.GetKeyDown(KeyCode.Space);
            Fixation = Fixation || x != 0 || y != 0;
            Direction = float2.ClampLength(new float2(x, y), 1);
        }
    }
}
