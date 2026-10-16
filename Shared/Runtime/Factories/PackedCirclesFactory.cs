using SlyPirates.Shared.Core;
using SlyPirates.Shared;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IFactories;

namespace SlyPirates.Shared.Factories
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
