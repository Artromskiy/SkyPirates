using SlyPirates.Shared.Core;
using SlyPirates.Shared;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.Ids;
using System;

namespace SlyPirates.Client.IViewModels
{
    public interface ICardsVM : IViewModel
    {
        void UseCard(int index);
        ReadOnlySpan<UnitId> Cards { get; }
        UnitsInfoConfig UnitsConfig { get; }
    }
}
