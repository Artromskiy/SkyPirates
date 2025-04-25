using DVG.Core;
using System;

namespace DVG.SkyPirates.Client.IViews
{
    public interface ICardsView : IView
    {
        public event Action<int> OnCardClicked;
    }
}
