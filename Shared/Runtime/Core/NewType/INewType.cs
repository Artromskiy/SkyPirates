namespace SlyPirates.Shared.Core.NewType
{
	public interface INewType { }

	public interface INewType<T> : INewType
	{
		T Value { get; set; }
	}
}
