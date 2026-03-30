using UnityEngine;

namespace DVG.SkyPirates.Tooling.Controls
{
    public class SpawnControllersSwitcher : MonoBehaviour
    {
        [SerializeField]
        private SpawnController[] _controllers;
        private int _selectedIndex = -1;

        private void OnValidate()
        {
            if (_controllers == null || _controllers.Length == 0)
                _controllers = GetComponentsInChildren<SpawnController>();
        }

        private void Start()
        {
            UpdateControllers();
        }

        private void Update()
        {
            var one = (int)KeyCode.Alpha0;
            int? index = null;
            for (int i = 0; i <= 9; i++)
            {
                if (Input.GetKeyDown((KeyCode)(one + i)))
                    index = i;
            }
            index--;
            if (index == null)
                return;
            var newIndex = index.Value;
            newIndex = Maths.Clamp(newIndex, -1, _controllers.Length - 1);
            if (newIndex != _selectedIndex)
            {
                _selectedIndex = newIndex;
                UpdateControllers();
            }
        }

        private void UpdateControllers()
        {
            for (int i = 0; i < _controllers.Length; i++)
                _controllers[i].gameObject.SetActive(i == _selectedIndex);
        }
    }
}