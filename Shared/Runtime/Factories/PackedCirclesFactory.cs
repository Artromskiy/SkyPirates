using DVG.Core;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IFactories;

namespace DVG.SkyPirates.Shared.Factories
{
    public class PackedCirclesFactory : IPackedCirclesFactory
    {
        private const string PathFormat = "Configs/PackedCirclesConfig";
        private readonly PackedCirclesConfig _config;

        public PackedCirclesFactory(IPathFactory<PackedCirclesConfig> pathFactory)
        {
            _config = pathFactory.Create(PathFormat);
        }

        public PackedCirclesConfig Create() => _config;
    }
}
