using Delta;
namespace SlyPirates.Shared
{
	public static class Constants
	{
		public const int TicksPerSecond = 16;
		public const int MaxHistoryTicks = TicksPerSecond * 8; // 8 seconds
		public const int ValidTicksCount = TicksPerSecond * 1;

		public const int TickDurationMs = 1000 / TicksPerSecond;
		public const int MaxHistoryDurationMs = MaxHistoryTicks * 1000 / TicksPerSecond;

		public static readonly fix TickTime = fix.One / TicksPerSecond;
	}
}
