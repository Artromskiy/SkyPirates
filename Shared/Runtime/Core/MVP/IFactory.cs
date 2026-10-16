namespace SlyPirates.Shared.Core
{
	public interface IFactory { }

	public interface IFactory<T> : IFactory
	{
		T Create();
	}
	public interface IFactory<Type, Param> : IFactory
	{
		Type Create(Param parameters);
	}

}
