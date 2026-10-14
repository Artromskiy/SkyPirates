using Delta.ECS;
using DVG.SkyPirates.Shared.Core.NewType;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
	[ComponentSchema]
	public struct SyncId : INewType<int>
	{
		public int Value;

		int INewType<int>.Value { readonly get => Value; set => Value = value; }

		public static implicit operator SyncId(int value) => new() { Value = value };

		public static implicit operator int(SyncId newType) => newType.Value;
	}
}
