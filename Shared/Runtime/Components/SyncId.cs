using Delta.ECS;
using SlyPirates.Shared.Core.NewType;

namespace SlyPirates.Shared.Components.Runtime
{
	[ComponentSchema(0x5DDC86C85A5348FEUL)]
	[ComponentAlias("SyncId")]
	[ComponentGroup(Group.History)]
	public struct SyncId : INewType<int>
	{
		public int Value;

		int INewType<int>.Value { readonly get => Value; set => Value = value; }

		public static implicit operator SyncId(int value) => new() { Value = value };

		public static implicit operator int(SyncId newType) => newType.Value;
	}
}
