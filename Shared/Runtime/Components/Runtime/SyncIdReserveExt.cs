namespace DVG.SkyPirates.Shared.Components.Runtime
{
	public static class SyncIdReserveExt
	{
		public static int RemainingCount(this SyncIdReserve syncIdReserve)
		{
			return syncIdReserve.First + syncIdReserve.Count - syncIdReserve.Current;
		}

		public static SyncId GetNext(this ref SyncIdReserve syncIdReserve)
		{
			Delta.Diagnostics.Debug.Assert(syncIdReserve.Current < syncIdReserve.First + syncIdReserve.Count);
			return syncIdReserve.Current++;
		}
	}
}
