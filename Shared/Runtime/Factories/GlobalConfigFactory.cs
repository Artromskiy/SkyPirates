using DVG.Core;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IFactories;

namespace DVG.SkyPirates.Shared.Factories
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
