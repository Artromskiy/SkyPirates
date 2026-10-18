using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlyPirates.Client.Views.UI
{
    public class GoodsElement : MonoBehaviour
    {
        [SerializeField]
        private Image _icon;
        [SerializeField]
        private TMP_Text _count;

        public async void Setup(string iconAddressableKey)
        {
            Sprite iconSprite = await AddressableLoader.Load<Sprite>(iconAddressableKey, this);
            if (iconSprite == null)
            {
                return;
            }

            _icon.sprite = iconSprite;
        }

        public void SetCount(int count)
        {
            _count.text = count.ToString();
        }
    }
}
