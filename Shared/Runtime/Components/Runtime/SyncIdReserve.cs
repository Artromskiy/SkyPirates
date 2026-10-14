using Delta.ECS;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
	[ComponentSchema]
	public struct SyncIdReserve
	{
		public int First;
		public int Count;
		public int Current;
	}
}
