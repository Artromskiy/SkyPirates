using SlyPirates.Client.IViewModels;
using SlyPirates.Client.IViews;
using SlyPirates.Shared.Ids;
using System;
using UnityEngine;

namespace SlyPirates.Client.Views
{
    public class TouchCardsView : View<ICardsVM>
    {
        [SerializeField]
        private UnitCardView[] _cards;
        [SerializeField]
        private UnitCardView _cardPrefab;

        public override void OnInject()
        {
            var cards = ViewModel.Cards;
            var cardViews = new UnitCardView[cards.Length];
            int existingCount = Math.Min(_cards.Length, cards.Length);
            Array.Copy(_cards, cardViews, existingCount);
            for (int i = existingCount; i < cards.Length; i++)
                cardViews[i] = Instantiate(_cardPrefab, transform);

            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                UnitId unitId = cards[index];
                cardViews[index].Init(unitId, ViewModel.UnitsConfig[unitId], false, false, 1, true);
                cardViews[index].OnClick += () => ViewModel.UseCard(index);
            }
        }
    }
}
