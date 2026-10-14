using SkyPirates.Shared.Core;
using SkyPirates.Shared;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.IFactories;

namespace SkyPirates.Shared.Factories
{
    public class GlobalConfigFactory : IGlobalConfigFactory
    {
        private const string Path = "Configs/GlobalConfig";
        private readonly GlobalConfig _globalConfig;

        public GlobalConfigFactory(IPathFactory<GlobalConfig> pathFactory)
        {
            _globalConfig = pathFactory.Create(Path);
        }

        public GlobalConfig Create() => _globalConfig;
    }
}
