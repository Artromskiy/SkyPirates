namespace DVG.SkyPirates.Shared.Core
{
	public interface IStructGenericAction
	{
		void Invoke<T>() where T : struct;
	}

	public interface IStructGenericActionArg
	{
		void Invoke<T>(T param) where T : struct;
	}

	public interface IStructGenericAction<K>
	{
		void Invoke<T>() where T : struct, K;
	}

	public interface IStructGenericCaller
	{
		void ForEach<T>(ref T action) where T : IStructGenericAction;
	}
	public interface IStructGenericCallerArg
	{
		void ForEach<T>(ref T action) where T : IStructGenericActionArg;
	}

	public interface IStructGenericCaller<K>
	{
		void ForEach<T>(ref T action) where T : IStructGenericAction<K>;
	}
}
