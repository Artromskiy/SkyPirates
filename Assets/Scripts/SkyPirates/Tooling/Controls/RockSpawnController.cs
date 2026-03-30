using DVG.SkyPirates.Client.DI;
using DVG.SkyPirates.Shared.Ids;

namespace DVG.SkyPirates.Tooling.Controls
{
    [Inject]
    public class RockSpawnController : SpawnController<RockId>
    {
        protected override RockId[] Ids { get; } = RockId.Constants.AllIds.ToArray();
    }
}