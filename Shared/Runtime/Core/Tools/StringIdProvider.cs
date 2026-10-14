using DVG.SkyPirates.Shared.Core.Ids;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Core.Tools
{
	public abstract class StringIdProvider<T, V> : IStringIdProvider<V>
		where T : StringIdProvider<T, V>, new()
		where V : IId

	{
		public static T Instance => new();
		public abstract IEnumerable<V> TypedIds { get; }
	}
}
