using SlyPirates.Shared.Core.Ids;
using System.Collections.Generic;

namespace SlyPirates.Shared.Core.Tools
{
	public abstract class StringIdProvider<T, V> : IStringIdProvider<V>
		where T : StringIdProvider<T, V>, new()
		where V : IId

	{
		public static T Instance => new();
		public abstract IEnumerable<V> TypedIds { get; }
	}
}
