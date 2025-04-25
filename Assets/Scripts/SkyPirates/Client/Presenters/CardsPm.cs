using DVG.Core;
using DVG.SkyPirates.Client.IViews;
using System;

namespace DVG.SkyPirates.Client.Presenters
{
    public class CardsPm : Presenter<ICardsView, object>
    {
        public event Action<int> OnSpawnUnit
        {
            add => View.OnCardClicked += value;
            remove => View.OnCardClicked -= value;
        }

        public CardsPm(ICardsView cardsView) : base(cardsView, null) { }
    }
}
