using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Client.IViews;
using DVG.SkyPirates.Client.Views.UI;
using DVG.SkyPirates.Shared.Ids;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views
{
    public class GoodsView : View<IGoodsVM>
    {
        [SerializeField]
        private GoodsElement _goodsPrefab;
        [SerializeField]
        private RectTransform _content;
        private readonly Dictionary<GoodsId, GoodsElement> _elements = new();
        public override void OnInject()
        {

        }

        private void Update()
        {
            var goods = ViewModel.Goods;
            foreach (var item in goods)
            {
                if (!_elements.TryGetValue(item.Key, out var element))
                {
                    element = Instantiate(_goodsPrefab, _content);
                    element.Setup(item.Key);
                    _elements.Add(item.Key, element);
                }
                element.SetCount(item.Value);
            }

            foreach (var item in _elements.Keys.ToArray())
                if (!goods.ContainsKey(item) && _elements.Remove(item, out var element))
                    Destroy(element.gameObject);
        }
    }
}