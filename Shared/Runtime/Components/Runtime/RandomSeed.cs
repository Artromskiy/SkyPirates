using Delta.ECS;
using DVG.SkyPirates.Shared.Core.NewType;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
	[ComponentSchema]
	[ComponentGroup(Group.History)]
	public struct RandomSeed : INewType<int>
	{
		public int Value;

		int INewType<int>.Value { readonly get => Value; set => Value = value; }

		public static implicit operator RandomSeed(int value) => new() { Value = value };

		public static implicit operator int(RandomSeed newType) => newType.Value;
	}
}
