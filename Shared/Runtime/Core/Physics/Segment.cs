using Delta;
namespace DVG.SkyPirates.Shared.Core.Physics
{
	public readonly struct Segment
	{
		public readonly fix2 Start;
		public readonly fix2 End;

		public Segment(fix2 start, fix2 end)
		{
			Start = start;
			End = end;
		}
	}
}
