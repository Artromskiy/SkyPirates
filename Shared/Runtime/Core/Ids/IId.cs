namespace SkyPirates.Shared.Core.Ids
{
	public interface IId
	{
		string Value { get; }
		bool IsNone { get; }
	}
}
