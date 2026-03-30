using DVG.SkyPirates.Client.DI;
using DVG.SkyPirates.Shared.Ids;

namespace DVG.SkyPirates.Tooling.Controls
{
    [Inject]
    public class TreeSpawnController : SpawnController<TreeId>
    {
        protected override TreeId[] Ids { get; } = TreeId.Constants.AllIds.ToArray();
    }
}