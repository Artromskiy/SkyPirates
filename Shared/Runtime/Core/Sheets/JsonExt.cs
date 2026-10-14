using System;
using System.Linq;
using System.Text.Json.Nodes;

namespace DVG.SkyPirates.Shared.Core.Sheets
{
	public static class JsonExt
	{
		public static JsonArray ToList(this JsonArray array)
		{
			return array.DeepClone().AsArray();
		}
		public static JsonObject ToDictionary(this JsonArray array, string keyProperty)
		{
			var result = new JsonObject();

			foreach (var node in array)
			{
				if (!node!.AsObject().TryGetPropertyValue(keyProperty, out var keyNode))
					throw new InvalidOperationException($"Property '{keyProperty}' not found in element.");

				var key = keyNode?.ToString();

				if (string.IsNullOrWhiteSpace(key))
					throw new InvalidOperationException($"Property '{keyProperty}' cannot be null or empty.");

				if (result.ContainsKey(key))
					throw new InvalidOperationException($"Duplicate key '{key}' in ToDictionary('{keyProperty}').");

				result[key] = node.AsObject().DeepClone();
			}
			return result;
		}
		public static JsonObject ToSingle(this JsonArray array)
		{
			return array.First()!.DeepClone().AsObject();
		}
	}
}
