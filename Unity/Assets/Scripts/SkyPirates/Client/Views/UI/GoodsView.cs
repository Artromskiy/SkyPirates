using SlyPirates.Client.IViewModels;
using SlyPirates.Client.IViews;
using SlyPirates.Client.Views.UI;
using SlyPirates.Shared.Ids;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SlyPirates.Client.Views
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
            {
                if (!goods.ContainsKey(item) && _elements.Remove(item, out var element))
                {
                    Destroy(element.gameObject);
                }
            }
        }
    }
}