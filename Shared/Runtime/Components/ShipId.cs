using Delta.ECS;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Core.Ids;
using System;

namespace SlyPirates.Shared.Ids
{
    [ComponentSchema(0x7E38D1A5B4C2096FUL)]
    [ComponentAlias("ShipId")]
    [ComponentGroup(Group.History)]
    public struct ShipId : IId, IEquatable<ShipId>
    {
        public string Value;
        string IId.Value => Value;
        public readonly bool IsNone => string.IsNullOrWhiteSpace(Value) || string.Equals(Value, "None", StringComparison.Ordinal);

        public ShipId(string value) => Value = value;

        public readonly bool Equals(ShipId other) =>
            (IsNone && other.IsNone) || string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override readonly bool Equals(object obj) => obj is ShipId other && Equals(other);
        public override readonly int GetHashCode() => IsNone ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public static bool operator ==(ShipId left, ShipId right) => left.Equals(right);
        public static bool operator !=(ShipId left, ShipId right) => !left.Equals(right);
        public static implicit operator ShipId(string value) => new(value);
        public static implicit operator string(ShipId id) => id.Value;
    }
}
