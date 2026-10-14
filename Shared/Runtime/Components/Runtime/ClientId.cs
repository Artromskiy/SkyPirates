using Delta.ECS;
using DVG.SkyPirates.Shared.Core.NewType;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
	[ComponentSchema]
	[ComponentGroup(Group.History)]
	public struct ClientId : INewType<int>
	{
		public int Value;

		int INewType<int>.Value { readonly get => Value; set => Value = value; }

		public static implicit operator ClientId(int value) => new() { Value = value };

		public static implicit operator int(ClientId newType) => newType.Value;
	}
}
