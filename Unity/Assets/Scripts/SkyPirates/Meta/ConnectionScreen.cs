using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SlyPirates.Meta
{
    public class ConnectionScreen : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField _ipField;
        [SerializeField]
        private TMP_InputField _portField;
        [SerializeField]
        private Button _connectButton;
        [SerializeField]
        private Button _playOfflineButton;
        [SerializeField]
        private Button _crewButton;

        private void Start()
        {
            _ipField.text = ClientSetupData.IP;
            _portField.text = ClientSetupData.Port;
            _connectButton.onClick.AddListener(PlayOnline);
            _playOfflineButton.onClick.AddListener(PlayOffline);
            _crewButton.onClick.AddListener(TeamSelectionScreen.Open);
        }

        private void PlayOnline()
        {
            if (!ClientSetupData.HasFullTeam)
            {
                TeamSelectionScreen.Open();
                return;
            }

            SetupData.GameMode = GameMode.Online;
            LoadScene();
        }

        private void PlayOffline()
        {
            if (!ClientSetupData.HasFullTeam)
            {
                TeamSelectionScreen.Open();
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
