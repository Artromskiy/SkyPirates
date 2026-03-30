using DVG.SkyPirates.Client.Databases;
using DVG.SkyPirates.Shared.Ids;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DVG.SkyPirates.Client.Views.UI
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
