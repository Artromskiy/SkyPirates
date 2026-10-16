using SlyPirates.Client.Databases;
using SlyPirates.Shared.Ids;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlyPirates.Client.Views
{
    public class UnitCardView : MonoBehaviour
    {
        [SerializeField]
        private Button _button;
        [SerializeField]
        private Image _icon;
        [SerializeField]
        private TMP_Text _price;
        [SerializeField]
        private UnitsDatabase _unitsDatabase;

        public event Action OnClick;

        public void Init(UnitId unitId, int price)
        {
            _icon.sprite = _unitsDatabase.Get(unitId).Sprite;
            _price.text = price.ToString();
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => OnClick?.Invoke());
        }
    }
}