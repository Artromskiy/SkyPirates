using SlyPirates.Shared.Core;
using SlyPirates.Shared;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IFactories;

namespace SlyPirates.Shared.Factories
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
