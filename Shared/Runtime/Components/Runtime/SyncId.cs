using Delta.ECS;
using SkyPirates.Shared.Core.NewType;

namespace SkyPirates.Shared.Components.Runtime
{
	[ComponentSchema]
	[ComponentGroup(Group.History)]
	public struct SyncId : INewType<int>
	{
		public int Value;

		int INewType<int>.Value { readonly get => Value; set => Value = value; }

		public static implicit operator SyncId(int value) => new() { Value = value };

		public static implicit operator int(SyncId newType) => newType.Value;
	}
}
