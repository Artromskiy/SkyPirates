using Delta.ECS;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Runtime
{
	[ComponentSchema(0x232322C77F801182UL)]
	[ComponentAlias("RandomSeed")]
	[ComponentGroup(Group.History)]
	public struct RandomSeed : INewType<int>
	{
		public int Value;

		int INewType<int>.Value { readonly get => Value; set => Value = value; }

		public static implicit operator RandomSeed(int value) => new() { Value = value };

		public static implicit operator int(RandomSeed newType) => newType.Value;
	}
}
