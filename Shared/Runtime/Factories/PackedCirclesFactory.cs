using SkyPirates.Shared.Core;
using SkyPirates.Shared;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.IFactories;

namespace SkyPirates.Shared.Factories
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
