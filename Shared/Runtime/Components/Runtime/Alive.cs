using Delta.ECS;

using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
	[ComponentSchema]
	[ComponentAlias("Alive")]
	[ComponentGroup(Group.History)]
	public struct Alive { }
}
