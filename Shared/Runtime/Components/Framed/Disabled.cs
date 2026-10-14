using Delta.ECS;
namespace SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("Disabled")]
    public struct Disabled
    {
        private bool _hidden;
    }
}
