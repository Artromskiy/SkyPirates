using Common.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlyPirates.Meta
{
    public class ConnectionScreen : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField _ipField;
        [SerializeField]
        private TMP_InputField _portField;
        [SerializeField]
        private GameObject _connectionScreen;
        [SerializeField]
        private EventButton _connectButton;
        [SerializeField]
        private EventButton _playOfflineButton;
        [SerializeField]
        private EventButton _crewButton;

        private void Start()
        {
            _ipField.text = ClientSetupData.IP;
            _portField.text = ClientSetupData.Port;
            _connectButton.OnClick += PlayOnline;
            _playOfflineButton.OnClick += PlayOffline;
            _crewButton.OnClick += ()=>_connectionScreen.SetActive(true);
        }

        private void PlayOnline()
        {
            if (!ClientSetupData.HasFullTeam)
            {
                _connectionScreen.SetActive(true);
                return;
            }

            SetupData.GameMode = GameMode.Online;
            LoadScene();
        }

        private void PlayOffline()
        {
            if (!ClientSetupData.HasFullTeam)
            {
                _connectionScreen.SetActive(true);
                return;
            }

            SetupData.GameMode = GameMode.Offline;
            LoadScene();
        }

        private void LoadScene()
        {
            ClientSetupData.IP = _ipField.text;
            ClientSetupData.Port = _portField.text;
            SceneManager.LoadScene("SampleScene");
        }
    }
}
