using Delta.ECS;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
	[ComponentSchema(0x45A95BDECB4E1502UL)]
	[ComponentAlias("SyncIdReserve")]
	[ComponentGroup(Group.History)]
	public struct SyncIdReserve
	{
		public int First;
		public int Count;
		public int Current;
	}
}
