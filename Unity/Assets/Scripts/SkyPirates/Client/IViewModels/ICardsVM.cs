using SkyPirates.Shared.Core;
using SkyPirates.Shared;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.Ids;
using System;

namespace SkyPirates.Client.IViewModels
{
    public interface ICardsVM : IViewModel
    {
        void UseCard(int index);
        ReadOnlySpan<UnitId> Cards { get; }
        UnitsInfoConfig UnitsConfig { get; }
    }
}
