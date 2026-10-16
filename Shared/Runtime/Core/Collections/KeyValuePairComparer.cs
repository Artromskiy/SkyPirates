using System;
using System.Collections.Generic;

namespace SlyPirates.Shared.Core.Collections
{
    public sealed class KeyValuePairComparer<K, V> : EqualityComparer<KeyValuePair<K, V>>
        where K : IEquatable<K>
        where V : IEquatable<V>
    {
        public static new EqualityComparer<KeyValuePair<K, V>> Default { get; } = new KeyValuePairComparer<K, V>();

        public override bool Equals(KeyValuePair<K, V> x, KeyValuePair<K, V> y) =>
            x.Key.Equals(y.Key) && x.Value.Equals(y.Value);

        public override int GetHashCode(KeyValuePair<K, V> obj) =>
            HashCode.Combine(obj.Key, obj.Value);
    }
}
