using Common.UI;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.Ids;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlyPirates.Client.Views
{
    public class UnitCardView : MonoBehaviour
    {
        [SerializeField]
        private EventButton _button;
        [SerializeField]
        private Image _icon;
        [SerializeField]
        private TMP_Text _price;
        [SerializeField]
        private GameObject _selection;
        [SerializeField]
        private GameObject _levelRoot;
        [SerializeField]
        private TMP_Text _level;
        [SerializeField]
        private TMP_Text _name;

        private Sprite _iconSprite;
        private CancellationTokenSource _iconLoadCancellation;

        public event Action OnClick;

        public void Init(UnitId unitId) => Init(unitId, null, false, false, 1, false);

        public void Init(UnitId unitId, UnitInfoData data, bool isSelected, bool showLevel, int level, bool showPrice)
        {
            string iconKey = data == null || string.IsNullOrWhiteSpace(data.IconAddressable)
                ? $"units/{unitId.Value.ToLowerInvariant()}/icon"
                : data.IconAddressable;
            LoadIcon(iconKey);
            if (_name != null)
            {
                _name.text = unitId.Value;
            }

            if (_selection != null)
            {
                _selection.SetActive(isSelected);
            }

            if (_levelRoot != null)
            {
                _levelRoot.SetActive(showLevel);
            }
            if (_level != null && showLevel)
            {
                _level.text = level.ToString();
            }

            _price.gameObject.SetActive(showPrice);
            if (showPrice)
            {
                _price.text = (data?.Cost ?? 0).ToString();
            }

            BindClick();
        }

        private void OnDestroy()
        {
            _iconLoadCancellation?.Cancel();
            _iconLoadCancellation?.Dispose();
            _iconLoadCancellation = null;
            AddressableLoader.Release(_iconSprite);
            _iconSprite = null;
        }

        private async void LoadIcon(string key)
        {
            _iconLoadCancellation?.Cancel();
            _iconLoadCancellation?.Dispose();
            AddressableLoader.Release(_iconSprite);
            _iconSprite = null;
            _icon.sprite = null;
            var loadCancellation = new CancellationTokenSource();
            _iconLoadCancellation = loadCancellation;
            Sprite iconSprite = await AddressableLoader.Load<Sprite>(key, this, loadCancellation.Token);
            bool isCurrentLoad = _iconLoadCancellation == loadCancellation;
            if (isCurrentLoad)
            {
                _iconLoadCancellation = null;
                loadCancellation.Dispose();
            }

            if (iconSprite == null)
            {
                return;
            }

            if (!isCurrentLoad)
            {
                AddressableLoader.Release(iconSprite);
                return;
            }

            _iconSprite = iconSprite;
            _icon.sprite = iconSprite;
        }

        private void BindClick()
        {
            _button.OnClick -= HandleClick;
            _button.OnClick += HandleClick;
        }

        private void HandleClick() => OnClick?.Invoke();
    }
}
