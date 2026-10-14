using UnityEngine;

namespace DVG.Misc
{
    public class FpsUnlimiter : MonoBehaviour
    {
        [SerializeField]
        private int _fpsLimit = 240;

        private void Update()
        {
            Application.targetFrameRate = _fpsLimit;
        }
    }
}
