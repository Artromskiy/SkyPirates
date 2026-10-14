using Delta;
namespace SkyPirates.Shared.Components.Runtime
{
	public static class RandomSeedExt
	{
		private static int Next(this ref RandomSeed seed)
		{
			seed.Value ^= seed.Value << 13;
			seed.Value ^= seed.Value >> 17;
			seed.Value ^= seed.Value << 5;
			return seed.Value;
		}

		public static int NextRange(this ref RandomSeed seed, int min, int max)
		{
			var value = Next(ref seed);
			int mask = value >> 31;
			value = (mask ^ value) - mask;
			return value % (max - min) + min;
		}

		public static fix NextFixRange(this ref RandomSeed seed, fix min, fix max)
		{
			return new fix(NextRange(ref seed, min.raw, max.raw + 1));
		}
	}
}
