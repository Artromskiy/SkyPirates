using DVG.SkyPirates.Shared.Ids;

namespace DVG.SkyPirates.Tooling.Controls
{
    public class UnitSpawnController : SpawnController<UnitId>
    {
        protected override UnitId[] Ids { get; } = UnitId.Constants.AllIds.ToArray();
    }
}