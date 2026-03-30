using DVG.SkyPirates.Client.DI;
using DVG.SkyPirates.Shared.Ids;

namespace DVG.SkyPirates.Tooling.Controls
{
    [Inject]
    public class CactusSpawnController : SpawnController<CactusId>
    {
        protected override CactusId[] Ids { get; } = CactusId.Constants.AllIds.ToArray();
    }
}