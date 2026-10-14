using UnityEngine;

namespace DVG.SkyPirates.Meta
{
    public class GameModeEnabler : MonoBehaviour
    {
        [SerializeField]
        private GameObject _onlineStart;
        [SerializeField]
        private GameObject _offlineStart;

        private void Awake()
        {
            _onlineStart.SetActive(SetupData.GameMode == GameMode.Online);
            _offlineStart.SetActive(SetupData.GameMode == GameMode.Offline);
        }
    }
}