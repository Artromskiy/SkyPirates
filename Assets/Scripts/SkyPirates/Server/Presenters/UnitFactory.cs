using DVG.SkyPirates.Server.IFactories;
using DVG.SkyPirates.Server.Presenters;
using DVG.SkyPirates.Shared.Ids;

namespace DVG.SkyPirates.Server.Factories
{
    public class UnitFactory : IUnitFactory
    {
        private readonly IUnitViewFactory _unitViewFactory;
        private readonly IUnitModelFactory _unitModelFactory;

        public UnitFactory(IUnitViewFactory unitViewFactory, IUnitModelFactory unitModelFactory)
        {
            _unitViewFactory = unitViewFactory;
            _unitModelFactory = unitModelFactory;
        }

        public UnitPm Create((UnitId unitId, int level, int merge) parameters)
        {
            var view = _unitViewFactory.Create(parameters);
            var model = _unitModelFactory.Create(parameters);
            UnitPm unit = new(view, model);
            return unit;
        }
    }
}
