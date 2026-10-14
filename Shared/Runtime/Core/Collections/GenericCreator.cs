namespace SkyPirates.Shared.Core.Collections
{
	public sealed class GenericCreator
	{
		private readonly GenericCollection _genericCollection = new();
		public K Get<K>() where K : class, new()
		{
			if (!_genericCollection.TryGet<K>(out var element))
				_genericCollection.Add(element = new K());
			return element;
		}
	}
}
