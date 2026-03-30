using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Client.IViews;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views
{
    public class TouchCardsView : View<ICardsVM>
    {
        [SerializeField]
        private UnitCardView[] _cards;

        public override void OnInject()
        {
            var cards = ViewModel.Cards;
            for (int i = 0; i < _cards.Length; i++)
            {
                int index = i;
                var unitId = cards[index];
                _cards[index].Init(unitId, ViewModel.UnitsConfig[unitId].RumPrice);
                _cards[index].OnClick += () => ViewModel.UseCard(index);
            }
        }
    }
}