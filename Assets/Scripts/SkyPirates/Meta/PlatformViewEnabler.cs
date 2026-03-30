using UnityEngine;

namespace DVG.SkyPirates.Tooling
{
    public class PlatformViewEnabler : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] _mobileViews;
        [SerializeField]
        private GameObject[] _standaloneViews;
        [SerializeField]
        private RuntimePlatform _testPlatform;

        private void Awake()
        {
            var platform = Application.platform;
#if UNITY_EDITOR
            platform = _testPlatform;
#endif

            foreach (var view in _mobileViews)
                view.SetActive(false);
            foreach (var view in _standaloneViews)
                view.SetActive(false);

            var mobile = platform is
                RuntimePlatform.Android or
                RuntimePlatform.IPhonePlayer or
                RuntimePlatform.WebGLPlayer;

            if (mobile) foreach (var view in _mobileViews)
                view.SetActive(true);

            else foreach (var view in _standaloneViews)
                view.SetActive(true);
        }
    }
}