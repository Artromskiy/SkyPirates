using DVG.SkyPirates.Shared.Core.Ids;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DVG.SkyPirates.Shared.Core.Tools
{
	public interface IStringIdProvider
	{
		Type IdType { get; }
		IEnumerable<IId> Ids { get; }
	}

	public interface IStringIdProvider<Id> : IStringIdProvider
		where Id : IId
	{
		IEnumerable<Id> TypedIds { get; }
		Type IStringIdProvider.IdType => typeof(Id);
		IEnumerable<IId> IStringIdProvider.Ids => TypedIds.OfType<IId>();
	}
}
