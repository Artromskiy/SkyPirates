using Delta.ECS;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
	[ComponentSchema]
	[ComponentAlias("SyncIdReserve")]
	[ComponentGroup(Group.History)]
	public struct SyncIdReserve
	{
		public int First;
		public int Count;
		public int Current;
	}
}
