using SkyPirates.Client.Databases;
using SkyPirates.Shared.Ids;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPirates.Client.Views.UI
{
    public class GoodsElement : MonoBehaviour
    {
        [SerializeField]
        private Image _icon;
        [SerializeField]
        private TMP_Text _count;
        [SerializeField]
        private GoodsDatabase _goodsDatabase;

        public void Setup(GoodsId goodsId)
        {
            _icon.sprite = _goodsDatabase.Get(goodsId).Sprite;
        }

        public void SetCount(int count)
        {
            _count.text = count.ToString();
        }
    }
}
