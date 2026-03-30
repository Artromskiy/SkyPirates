using DVG.Core;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.Ids;
using System;

namespace DVG.SkyPirates.Client.IViewModels
{
    public interface ICardsVM : IViewModel
    {
        void UseCard(int index);
        ReadOnlySpan<UnitId> Cards { get; }
        UnitsInfoConfig UnitsConfig { get; }
    }
}
