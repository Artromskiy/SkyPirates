using Delta.ECS;

namespace SkyPirates.Shared.Components.Runtime
{
	[ComponentSchema]
	[ComponentGroup(Group.History)]
	public struct SyncIdReserve
	{
		public int First;
		public int Count;
		public int Current;
	}
}
